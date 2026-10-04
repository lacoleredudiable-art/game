using Dovus.App.Boss;
using Dovus.Core.Shared;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using Dovus.Game.Team;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// Prototip boss döngüsü: idle yaklaşma + YERE ÇAKMA üç ritmi (§11).
    /// Yaklaşma Home'a yazılır — transform'a değil (T7.2 geri tepme kalıcılığı).
    /// Varyant idle'da seçilir; ExchangeResolver yalnızca aktif windup/radius görür.
    /// Hedef (oyuncu / dost / dikkat çeken yem) her idle döngüsünün başında <see cref="HostileTargetsHost"/>
    /// ile seçilir; yaklaşma ve windup kilidi hedefe bakar, vuruş hacmi kayıtlı HER dostu sınar.
    /// </summary>
    public sealed partial class BossDirector : MonoBehaviour
    {
        BossBrain _brain;
        GameClockHost _clock;
        CombatTuning _combat;
        GameTuning _colors;
        BossReactorController _reactor;
        BossAttack _attack;
        BossPoise _poise;
        ExchangeResolver _resolver;
        HexagonInputController _hexagonInput;
        DodgeState _dodge;
        SentenceEngine _engine;
        Transform _player;
        PlayerVitalsHost _vitals;
        BossVitals _bossVitals;
        BossTelegraphView _telegraph;
        CombatFeelDirector _feel;
        KinematicMotorController _playerMotor;
        ActorStatusHost _bossStatus;
        ActorStatusHost _playerStatus;
        BossView _visual;
        MotionTemplateBodyHost _motionBody;
        HostileTargetsHost _targets;
        HostileProjectileHost _projectiles;
        TeamComboAccess _team;
        Transform _target;
        int _targetId = -1;
        TargetKind _targetKind = TargetKind.Player;
        double _reverseUntilMs = -1;

        Vector3 _originHome;
        readonly System.Random _rng = new();
        readonly BossAttackSelector _attackSelector = new();

        bool _phase2Announced;

        bool _bossEncounterLoaded;
        BossAttackKind[] _phase1AttackKinds = System.Array.Empty<BossAttackKind>();
        BossAttackKind[] _phase2AttackKinds = System.Array.Empty<BossAttackKind>();
        readonly Dictionary<BossAttackKind, BossAttackEntry> _attackEntriesByKind = new();
        BossAttackEntry _activeAttackEntry;
        BossOnHitStatus _volleyOnHit;

        /// <summary>Ağ Örme windup başında kilitlenen hedef (3.4 alan sunumu).</summary>
        public Vector3 LastWebFieldTarget { get; private set; }

        /// <summary>karadul.json faz sırası: 1 = Uyanış, 2 = Öfke (can ≤ %50).</summary>
        public int BossPhase => _phase2Announced ? 2 : 1;

        /// <summary>Faz geçişi (HUD banner'ı için). Argüman yeni faz numarası.</summary>
        public event System.Action<int> BossPhaseChanged;

        public BossVitals Vitals => _bossVitals;
        public float PoiseRatio => _poise != null ? _poise.Ratio : 1f;
        public bool IsPoiseStaggered => _poise != null && _poise.IsStaggered;

        /// <summary>Windup başladı (ses/sunum).</summary>
        public event System.Action<BossAttackKind> AttackWindupStarted;

        /// <summary>Vuruş anı: etki çözüldü (şok dalgası / alev sunumu). Hasar bundan bağımsız.</summary>
        public event System.Action<BossAttackKind> AttackStruck;

        public float AttackRadiusM =>
            (_attack?.RadiusM ?? 0f) * (_team != null ? _team.Hub.BossStrikeScale : 1f);

        public void BindTeam(TeamComboAccess team) => _team = team;
        public float AttackArcHalfAngleDeg => _attack?.ArcHalfAngleDeg ?? 180f;
        public Vector3 AttackOrigin => _reactor != null ? _reactor.Home : transform.position;

        public bool IsWindingUp => _brain != null && _brain.Phase == BossBrainPhase.Windup;
        public BossAttackKind? CurrentAttackKind =>
            _brain != null && _brain.Phase is BossBrainPhase.Windup or BossBrainPhase.Active
                ? _attack?.Kind
                : null;

        /// <summary>Windup ilerlemesi 0→1 (windup dışında 0) — boss cast barı okur.</summary>
        public float WindupProgress01 =>
            _brain != null
            && _brain.Phase == BossBrainPhase.Windup
            && _attack != null
            && _attack.WindupMs > 0
                ? Mathf.Clamp01((float)(_brain.PhaseElapsedMs / _attack.WindupMs))
                : 0f;
        public int TelegraphStartMs => _brain != null ? _brain.TelegraphStartMs : 0;
        public int StrikeTimeMs
        {
            get
            {
                if (_attack == null)
                    return 0;
                if (_brain != null && _brain.StrikeWorldMs > 0
                    && _brain.Phase is BossBrainPhase.Active or BossBrainPhase.Recovery)
                    return _brain.StrikeWorldMs;
                if (_brain != null && _brain.Phase == BossBrainPhase.Windup && _attack.WindupMs > 0)
                {
                    float speed = Mathf.Max(BossDirectorDefaults.MinPhaseSpeed, CurrentPhaseSpeed());
                    double remain = System.Math.Max(0, _attack.WindupMs - _brain.PhaseElapsedMs);
                    double now = _clock != null ? _clock.Director.WorldTimeMs : _brain.PhaseStartedWorldMs;
                    return (int)(now + remain / speed);
                }
                return _attack.StrikeTimeMs(_brain != null ? _brain.TelegraphStartMs : 0);
            }
        }
        public SlamVariant? ActiveVariant => _attack?.Variant;

        /// <summary>Şu anki hedef (HUD halkası, tarama sondası). Kayıt yoksa oyuncu.</summary>
        public Transform CurrentTarget => _targets != null ? _target : _player;
        public TargetKind CurrentTargetKind => _targets != null ? _targetKind : TargetKind.Player;
        public HostileTargetsHost Targets => _targets;

        /// <summary>mechanic_grammar ters_kontrol: yaklaşma hedeften uzaklaşır, windup kilidi ters yöne bakar.</summary>
        public bool IsReversed =>
            _reverseUntilMs > 0 && _clock != null && _clock.Director.WorldTimeMs < _reverseUntilMs;

        public void Bind(
            GameClockHost clock,
            CombatTuning combat,
            GameTuning colors,
            BossReactorController reactor,
            HexagonInputController input,
            Transform player,
            PlayerVitalsHost vitals,
            BossVitals bossVitals,
            BossTelegraphView telegraph,
            CombatFeelDirector feel)
        {
            _clock = clock;
            _combat = combat;
            _colors = colors;
            _reactor = reactor;
            _hexagonInput = input;
            _attack = new BossAttack(combat.Boss);
            _poise = new BossPoise(combat.Boss.PoiseMax);
            _resolver = new ExchangeResolver(combat);
            _dodge = input.Dodge;
            _engine = input.Engine;
            _player = player;
            _vitals = vitals;
            _bossVitals = bossVitals;
            _telegraph = telegraph;
            _feel = feel;
            _playerMotor = player.GetComponent<KinematicMotorController>();
            _originHome = reactor.Home;
            TryLoadBossEncounter();
            _brain ??= new BossBrain(_rng, new BossBrainPort(this)); // tekrar Bind: eski alanlar gibi durum korunur
            _brain.EnterIdle(clock.Director.WorldTimeMs);
        }

        public void BindStatus(ActorStatusHost status)
        {
            _bossStatus = status;
            // Kökteki gibi: kilit bitince kısa bağışıklık, boss sonsuza dek kilitlenmesin.
            status?.Board.EnableAttackLockImmunity();
        }

        public void BindPlayerStatus(ActorStatusHost status) => _playerStatus = status;

        public void BindVisual(BossView visual) => _visual = visual;

        /// <summary>Hedef kaydı (oyuncu, dost, yemler). Bağlanmazsa boss eskisi gibi yalnız oyuncuyu kovalar.</summary>
        public void BindTargets(HostileTargetsHost targets)
        {
            _targets = targets;
            PickTarget();
        }

        /// <summary>ters_kontrol: verilen dünya saatine kadar kontrol ters (uzar, kısalmaz).</summary>
        public void ApplyReverse(double untilWorldMs)
        {
            if (untilWorldMs > _reverseUntilMs)
                _reverseUntilMs = untilWorldMs;
        }

        public void ClearReverse() => _reverseUntilMs = -1;

        /// <summary>Zehir Tükürüğü mermilerinin sahibi. Bağlanmazsa Volley seçilmez (eski Slam/FireCone).</summary>
        public void BindProjectiles(HostileProjectileHost host)
        {
            _projectiles = host;
            if (_projectiles != null)
                _projectiles.OnPlayerProjectileHit = OnVolleyProjectilePlayerHit;
        }

        /// <summary>
        /// Script recompile Bind alanlarını siler; Awake yeniden çağrılmaz.
        /// Eksik saf C# nesnelerini burada toparlarız.
        /// </summary>
        void EnsureRuntime()
        {
            _reactor ??= GetComponent<BossReactorController>();
            _attack ??= new BossAttack(_combat.Boss);
            _resolver ??= new ExchangeResolver(_combat);

            if (_colors == null)
                _colors = new GameTuning();

            if (_player == null)
            {
                var p = GameObject.Find("Player");
                if (p != null)
                    _player = p.transform;
            }

            if (_vitals == null && _player != null)
                _vitals = _player.GetComponent<PlayerVitalsHost>();

            if (_dodge == null || _engine == null)
            {
                if (_hexagonInput != null)
                {
                    _dodge ??= _hexagonInput.Dodge;
                    _engine ??= _hexagonInput.Engine;
                }
            }

            if (_playerMotor == null && _player != null)
                _playerMotor = _player.GetComponent<KinematicMotorController>();

            if (_telegraph == null)
                _telegraph = GetComponent<BossTelegraphView>();

            if (_visual == null)
                _visual = GetComponent<BossView>();

            _motionBody ??= GetComponent<MotionTemplateBodyHost>();

            TryLoadBossEncounter();
        }

        bool IsAglarinQueen() =>
            _colors != null && _colors.Boss.ActiveBossId == "aglarin_kralicesi";

        void TryLoadBossEncounter()
        {
            if (_bossEncounterLoaded || _colors == null)
                return;
            _bossEncounterLoaded = true;

            string bossId = _colors.Boss.ActiveBossId;
            if (string.IsNullOrWhiteSpace(bossId))
                return;

            string path = "Bosses/" + bossId.Replace('_', '-');
            _phase1AttackKinds = BossEncounterData.LoadPhaseAttackKinds(path, 1);
            _phase2AttackKinds = BossEncounterData.LoadPhaseAttackKinds(path, 2);
            IReadOnlyList<BossAttackEntry> attacks = BossEncounterData.LoadAttacks(path);
            if (attacks.Count == 0 && _phase1AttackKinds.Length == 0 && _phase2AttackKinds.Length == 0)
                return;

            _attackEntriesByKind.Clear();
            for (int i = 0; i < attacks.Count; i++)
                _attackEntriesByKind[attacks[i].Kind] = attacks[i];
        }

        /// <summary>§11: can 0 — saldırı döngüsü durur, telegraf kapanır.</summary>
        public void NotifyBossDown(double worldMs)
        {
            _telegraph?.Hide();
            _feel?.ClearThreat();
            _visual?.PlayDeath();
            ClearReverse();
            _brain?.EnterIdle(worldMs);
        }

        /// <summary>§11: tam canla yeniden doğuş — idle beklemeden devam.</summary>
        public void NotifyBossRevived(double worldMs)
        {
            bool wasPhase2 = _phase2Announced;
            _phase2Announced = false;
            _poise?.Reset();
            _visual?.NotifyRevived();
            ClearReverse();
            if (wasPhase2)
                BossPhaseChanged?.Invoke(1);
            _brain?.EnterIdle(worldMs);
        }

        /// <summary>
        /// mechanic_grammar Aynalı tempo: hazırlanmakta olan saldırıyı iptal edip yeni idle
        /// döngüsüne döner. Boss AI seçimini değiştirmez; yalnız mevcut cast state'i sarar.
        /// </summary>
        public bool CancelPreparedAttack(double worldMs)
        {
            if (_brain == null || _brain.Phase != BossBrainPhase.Windup)
                return false;
            _telegraph?.Hide();
            _feel?.ClearThreat();
            _brain.EnterIdle(worldMs);
            return true;
        }

        /// <summary>Oyuncu vuruşunun poise hasarı. 0'da sersemlik; süre BossTuning.StaggerDurationSec.</summary>
        public bool ApplyPoiseDamage(float amount)
        {
            if (_poise == null || _combat == null || amount <= 0f)
                return false;
            if (_bossVitals != null && _bossVitals.IsDown)
                return false;
            bool broke = _poise.ApplyHit(amount, _combat.Boss.StaggerDurationSec);
            if (broke)
                BeginPoiseStagger();
            return broke;
        }

        void BeginPoiseStagger()
        {
            _telegraph?.Hide();
            _feel?.ClearThreat();
            _brain?.ForceIdleWithoutEnter(markStrikeResolved: true);
            _visual?.SetSpeed(0f);
            _visual?.PlayStagger();
        }

        void Update()
        {
            EnsureRuntime();
            if (_clock == null || _reactor == null || _combat == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            float dtSec = (float)(_clock.WorldDeltaMs / BossTimeDefaults.SecToMs);
            _poise?.Tick(dtSec);
            HandlePlayerDown(worldMs);

            // Boss ölümünde çökme pozu sürerken saldırı yok (§11 noktalama).
            if (_bossVitals != null && _bossVitals.IsDown)
            {
                _telegraph?.Hide();
                _feel?.ClearThreat();
                _visual?.SetSpeed(0f);
                return;
            }

            if (_poise != null && _poise.IsStaggered)
            {
                _telegraph?.Hide();
                _feel?.ClearThreat();
                _brain?.EnsurePhaseIdleIfNot();
                _visual?.SetSpeed(0f);
                return;
            }

            if (_brain != null
                && _brain.Phase == BossBrainPhase.Windup
                && _bossStatus != null
                && _attack != null)
            {
                BossAttackGate gate = BossAttackControl.Gate(
                    _bossStatus.Board, CurrentMotion(), _attack.Kind, staggered: false);
                if (gate.CancelWindup)
                {
                    _telegraph?.Hide();
                    _feel?.ClearThreat();
                    _brain.EnterIdle(worldMs);
                    return;
                }
            }

            _brain?.Tick(worldMs, dtSec, _clock.WorldDeltaMs);
        }

        void LateUpdate()
        {
            if (!_poiseState.PounceLeapActive || _motionBody == null || _reactor == null)
                return;
            if (_motionBody.IsDisplacing)
            {
                Vector3 p = transform.position;
                p.y = _reactor.Home.y;
                _reactor.Home = p;
                return;
            }
            _reactor.SnapHome(new Vector3(_poiseState.PounceLandX, _reactor.Home.y, _poiseState.PounceLandZ));
            _poiseState.CompletePounceLeap();
        }

        BossAttackMotion CurrentMotion() =>
            _attack == null ? BossAttackMotion.Standing : BossAttackControl.MotionOf(_attack.Kind);

        bool PlayerStealthed => _playerStatus != null && _playerStatus.Board.IsStealthed;
    }
}
