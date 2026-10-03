using Dovus.App.Boss;
using Dovus.Core.Combat;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
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
    /// Hedef (oyuncu / dost / dikkat çeken yem) her idle döngüsünün başında <see cref="HostileTargets"/>
    /// ile seçilir; yaklaşma ve windup kilidi hedefe bakar, vuruş hacmi kayıtlı HER dostu sınar.
    /// </summary>
    public sealed partial class BossDirector : MonoBehaviour
    {
        BossBrain _brain;
        GameClock _clock;
        CombatTuning _combat;
        PrototypeTuning _colors;
        BossReactor _reactor;
        BossAttack _attack;
        BossPoise _poise;
        ExchangeResolver _resolver;
        DodgeState _dodge;
        SentenceEngine _engine;
        Transform _player;
        PlayerVitals _vitals;
        BossVitals _bossVitals;
        BossTelegraph _telegraph;
        CombatFeel _feel;
        KinematicMotor _playerMotor;
        ActorStatus _bossStatus;
        ActorStatus _playerStatus;
        BossVisual _visual;
        MotionTemplateBody _motionBody;
        bool _pounceLeapActive;
        float _pounceLandX;
        float _pounceLandZ;
        HostileTargets _targets;
        HostileProjectileHost _projectiles;
        Transform _target;
        int _targetId = -1;
        TargetKind _targetKind = TargetKind.Player;
        double _reverseUntilMs = -1;

        bool _playerWasDown;
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
            (_attack?.RadiusM ?? 0f) * PortalBorderTeamHooks.BossStrikeScale;
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
                    float speed = Mathf.Max(0.05f, CurrentPhaseSpeed());
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
        public HostileTargets Targets => _targets;

        /// <summary>mechanic_grammar ters_kontrol: yaklaşma hedeften uzaklaşır, windup kilidi ters yöne bakar.</summary>
        public bool IsReversed =>
            _reverseUntilMs > 0 && _clock != null && _clock.Director.WorldTimeMs < _reverseUntilMs;

        public void Bind(
            GameClock clock,
            CombatTuning combat,
            PrototypeTuning colors,
            BossReactor reactor,
            HexagonInput input,
            Transform player,
            PlayerVitals vitals,
            BossVitals bossVitals,
            BossTelegraph telegraph,
            CombatFeel feel)
        {
            _clock = clock;
            _combat = combat;
            _colors = colors;
            _reactor = reactor;
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
            _playerMotor = player.GetComponent<KinematicMotor>();
            _originHome = reactor.Home;
            TryLoadBossEncounter();
            _brain ??= new BossBrain(_rng, new BossBrainPort(this)); // tekrar Bind: eski alanlar gibi durum korunur
            _brain.EnterIdle(clock.Director.WorldTimeMs);
        }

        public void BindStatus(ActorStatus status)
        {
            _bossStatus = status;
            // Kökteki gibi: kilit bitince kısa bağışıklık, boss sonsuza dek kilitlenmesin.
            status?.Board.EnableAttackLockImmunity();
        }

        public void BindPlayerStatus(ActorStatus status) => _playerStatus = status;

        public void BindVisual(BossVisual visual) => _visual = visual;

        /// <summary>Hedef kaydı (oyuncu, dost, yemler). Bağlanmazsa boss eskisi gibi yalnız oyuncuyu kovalar.</summary>
        public void BindTargets(HostileTargets targets)
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
            _clock ??= FindAnyObjectByType<GameClock>();
            _reactor ??= GetComponent<BossReactor>();
            _attack ??= new BossAttack(_combat.Boss);
            _resolver ??= new ExchangeResolver(_combat);

            if (_colors == null)
                _colors = new PrototypeTuning();

            if (_player == null)
            {
                var p = GameObject.Find("Player");
                if (p != null)
                    _player = p.transform;
            }

            if (_vitals == null && _player != null)
                _vitals = _player.GetComponent<PlayerVitals>();

            if (_dodge == null || _engine == null)
            {
                var input = FindAnyObjectByType<HexagonInput>();
                if (input != null)
                {
                    _dodge ??= input.Dodge;
                    _engine ??= input.Engine;
                }
            }

            if (_playerMotor == null && _player != null)
                _playerMotor = _player.GetComponent<KinematicMotor>();

            if (_feel == null)
                _feel = FindAnyObjectByType<CombatFeel>();

            if (_telegraph == null)
                _telegraph = GetComponent<BossTelegraph>();

            if (_visual == null)
                _visual = GetComponent<BossVisual>();

            _motionBody ??= GetComponent<MotionTemplateBody>();

            TryLoadBossEncounter();
        }

        bool IsAglarinQueen() =>
            _colors != null && _colors.ActiveBossId == "aglarin_kralicesi";

        void TryLoadBossEncounter()
        {
            if (_bossEncounterLoaded || _colors == null)
                return;
            _bossEncounterLoaded = true;

            string bossId = _colors.ActiveBossId;
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
            float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);
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
            if (!_pounceLeapActive || _motionBody == null || _reactor == null)
                return;
            if (_motionBody.IsDisplacing)
            {
                Vector3 p = transform.position;
                p.y = _reactor.Home.y;
                _reactor.Home = p;
                return;
            }
            _reactor.SnapHome(new Vector3(_pounceLandX, _reactor.Home.y, _pounceLandZ));
            _pounceLeapActive = false;
        }

        void HandlePlayerDown(double worldMs)
        {
            bool down = _vitals != null && _vitals.IsDown;
            if (down && !_playerWasDown)
            {
                // Boss OYUNCUNUN doğuş noktasının üstünde duruyorsa zincir ölüm olur; yalnızca
                // o zaman eve çekilir. Aksi halde biriken kalıcı knockback (T7.2) korunur ve
                // boss ışınlanmaz (T8.1) — yaklaşma zaten idle'da yeniden başlıyor.
                Vector3 spawn = _vitals.SpawnPos;
                float safe = _combat.Boss.RadiusM;
                Vector3 toSpawn = _reactor.Home - spawn;
                toSpawn.y = 0f;
                if (toSpawn.sqrMagnitude < safe * safe)
                    _reactor.Home = _originHome;

                _telegraph?.Hide();
                _feel?.ClearThreat();
                _brain?.EnterIdle(worldMs);
            }

            _playerWasDown = down;
        }

        BossAttackMotion CurrentMotion() =>
            _attack == null ? BossAttackMotion.Standing : BossAttackControl.MotionOf(_attack.Kind);

        void BeginPounceLeap()
        {
            Vector3 home = _reactor.Home;
            _pounceLandX = _attack.LandingX;
            _pounceLandZ = _attack.LandingZ;
            float dx = _pounceLandX - home.x;
            float dz = _pounceLandZ - home.z;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < 0.05f)
                return;
            Vector3 fwd = new Vector3(dx / dist, 0f, dz / dist);
            transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
            float airSec = Mathf.Max(0.05f, _combat.Boss.PounceAirSec);
            float heightM = MotionFallbacks.Coded.HeightM;
            var phase = new MotionPhase(
                "pounce", "leap", airSec, "travel", "none", string.Empty, 0f,
                dist, 0f, 0f, 0f, heightM, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, null, null);
            var template = new MotionTemplate(
                "boss_pounce", "boss_pounce", 0, "boss", true, new[] { phase });
            _pounceLeapActive = true;
            _motionBody.Play(
                template,
                () => new MotionTarget(true, _pounceLandX, _pounceLandZ),
                () => false,
                null,
                _reactor.BodyRadiusM);
        }

        float CurrentPhaseSpeed()
        {
            if (_bossStatus == null)
                return 1f;
            return BossAttackControl.Evaluate(_bossStatus.Board, CurrentMotion()).PhaseSpeed;
        }

        /// <summary>
        /// Önce SALDIRI TÜRÜ, Slam ise ardından üç ritminden biri. İkisi de "aynısı üst üste
        /// MaxSame...Streak'i geçemez" desenini paylaşır (§11/T13 dersi). İzinli küme karadul.json
        /// fazlarından: faz 1 {Slam, Volley}, öfke {Slam, FireCone, Volley}; CC kapısının kestiği
        /// türler önceden elenir (Disarm Slam'i, Silence FireCone/Volley'i keser).
        /// </summary>
        bool SelectNextAttack()
        {
            if (_attack == null || _combat == null)
                return false;

            bool enraged = IsEnraged();
            Transform aim = AimTarget();
            float pounceDist = 0f;
            if (aim != null && _reactor != null)
            {
                Vector3 toAim = aim.position - _reactor.Home;
                toAim.y = 0f;
                pounceDist = toAim.magnitude;
            }

            if (!_attackSelector.TrySelect(
                    enraged,
                    _phase1AttackKinds,
                    _phase2AttackKinds,
                    k =>
                    {
                        if (k == BossAttackKind.Volley && _projectiles == null)
                            return false;
                        if (k == BossAttackKind.Pounce
                            && (aim == null || !BossAttackKindPicker.PounceInRange(pounceDist, _combat.Boss)))
                            return false;
                        return AttackKindAllowed(k);
                    },
                    _combat.Boss.MaxSameAttackKindStreak,
                    _combat.Boss.MaxSameVariantStreak,
                    _rng,
                    out BossAttackChoice choice))
                return false;

            BossAttackKind kind = choice.Kind;
            _activeAttackEntry = _attackEntriesByKind.TryGetValue(kind, out BossAttackEntry entry)
                ? entry
                : null;

            if (kind == BossAttackKind.FireCone)
            {
                _attack.ApplyFireCone();
                return true;
            }
            if (kind == BossAttackKind.Volley)
            {
                _attack.ApplyVolley(enraged);
                return true;
            }
            if (kind == BossAttackKind.WebField)
            {
                _attack.ApplyWebField();
                return true;
            }
            if (kind == BossAttackKind.Pounce)
            {
                Vector3 land = aim != null ? aim.position : _reactor.Home;
                float arenaR = _combat.SkillMotion.ArenaHalfSizeM - _combat.Boss.PounceWallMarginM;
                land = ArenaClamp.XZ(land, Mathf.Max(0f, arenaR), 0f);
                _attack.SetLanding(land.x, land.z);
                _attack.ApplyPounce();
                return true;
            }

            _attack.ApplyVariant(choice.Variant.Value);
            return true;
        }

        bool AttackKindAllowed(BossAttackKind kind)
        {
            if (_bossStatus == null)
                return true;
            return BossAttackControl.Gate(
                _bossStatus.Board,
                BossAttackControl.MotionOf(kind),
                kind,
                staggered: false).CanStart;
        }

        bool PlayerStealthed => _playerStatus != null && _playerStatus.Board.IsStealthed;

        /// <summary>Kayıt yoksa eski davranış: oyuncu, gizliyse hedef yok.</summary>
        void PickTarget()
        {
            if (_targets == null)
            {
                _target = _player;
                _targetKind = TargetKind.Player;
                _targetId = -1;
                return;
            }
            _targetId = _targets.Pick(_rng.NextDouble());
            HostileTargets.Entry e = _targetId >= 0 ? _targets.Find(_targetId) : null;
            _target = e?.Transform;
            _targetKind = e?.Kind ?? TargetKind.Player;
            if (e == null)
                _targetId = -1;
        }

        /// <summary>Hedef dönüşümü; gizli oyuncu (kayıtsız eski yol) ya da geçersiz hedefte null.</summary>
        Transform AimTarget()
        {
            if (_targets == null)
                return _player != null && !PlayerStealthed ? _player : null;
            return _target;
        }

        float AimTargetRadius()
        {
            if (_targets != null)
            {
                HostileTargets.Entry e = _targetId >= 0 ? _targets.Find(_targetId) : null;
                if (e != null && e.Kind != TargetKind.Player)
                    return e.RadiusM;
            }
            return _playerMotor != null ? _playerMotor.BodyRadiusM : 0.5f;
        }

        void Approach(float dtSec)
        {
            if (dtSec <= 0f)
                return;
            Transform aim = AimTarget();
            if (aim == null)
            {
                _visual?.SetSpeed(0f);
                return;
            }

            float speedMult = 1f;
            if (_bossStatus != null)
            {
                if (_bossStatus.EffectiveBlocksMovement)
                {
                    _visual?.SetSpeed(0f);
                    return;
                }
                speedMult = _bossStatus.EffectiveMoveSpeedMult;
                if (speedMult <= 0.01f)
                {
                    _visual?.SetSpeed(0f);
                    return;
                }
            }

            Vector3 home = _reactor.Home;
            Vector3 to = aim.position - home;
            to.y = 0f;
            bool reversed = IsReversed;
            if (!reversed)
            {
                float pad = _colors != null ? _colors.BossApproachStopPadM : 0.35f;
                float stop = _reactor.BodyRadiusM + AimTargetRadius() + pad;
                if (to.sqrMagnitude <= stop * stop)
                {
                    _visual?.SetSpeed(0f);
                    return;
                }
            }
            else if (to.sqrMagnitude <= 0.0001f)
            {
                _visual?.SetSpeed(0f);
                return;
            }

            // ters_kontrol: aynı hızla hedeften UZAĞA yürür (yaklaşma vektörü ters).
            Vector3 dir = reversed ? -to.normalized : to.normalized;
            float groundMps = _combat.Boss.ApproachSpeedMps * speedMult;
            home += dir * groundMps * dtSec;
            _reactor.Home = home;
            _visual?.SetWalk(groundMps);
            TurnToward(dir, dtSec);
        }

        bool IsEnraged() =>
            _bossVitals != null && _bossVitals.MaxHp > 0f && (_bossVitals.Hp / _bossVitals.MaxHp) <= 0.5f;

        /// <summary>Yürürken dönüş hız sınırlı (yürüme yönüne); windup başındaki kilitleme (FaceTarget) anlık kalır.</summary>
        void TurnToward(Vector3 dir, float dtSec)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f)
                return;
            float rate = _colors != null ? _colors.BossTurnRateDegPerSec : 240f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), rate * dtSec);
        }

        /// <summary>Windup kilidi hedefe; ters_kontrol açıksa 180° ters (koni/salvo geri gider, slam 360° etkilenmez).</summary>
        void FaceTarget()
        {
            Transform aim = AimTarget();
            if (aim == null)
                return;
            Vector3 to = aim.position - _reactor.Home;
            to.y = 0f;
            if (IsReversed)
                to = -to;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void ResolveStrike()
        {
            if (_vitals != null && _vitals.IsDown)
                return;
            if (_attack == null || _resolver == null)
                return;

            if (_attack.Kind == BossAttackKind.Volley)
            {
                FireVolley();
                return;
            }

            if (_attack.Kind == BossAttackKind.WebField)
                return;

            // Körlük bir vuruşta bir kez zar atar; aynı ıska bütün kurbanlara geçerli.
            bool? blindMiss = null;
            bool Blind()
            {
                blindMiss ??= _bossStatus != null
                    && BossStatusMath.Misses(_bossStatus.Board, (float)_rng.NextDouble());
                return blindMiss.Value;
            }

            ResolveOtherFriendlies(Blind);

            Vector3 strikeOrigin = StrikeVolumeOrigin();
            float dist = 0f;
            float angleDeg = 0f;
            if (_player != null)
            {
                Vector3 d = _player.position - strikeOrigin;
                d.y = 0f;
                dist = d.magnitude;
                if (dist > 0.01f)
                {
                    // FireCone dar bir yay (§ArcHalfAngleDeg) — Slam'de arc>=180 olduğu için
                    // bu açı zaten yok sayılır, davranış değişmiyor.
                    Vector3 forward = transform.forward;
                    forward.y = 0f;
                    angleDeg = Vector3.SignedAngle(forward, d, Vector3.up);
                }
            }

            bool stealthed = PlayerStealthed;
            bool inVolume = _attack.IsInEffectVolume(
                BossStrikeShape.DistanceForVolume(dist, PortalBorderTeamHooks.BossStrikeScale),
                angleDeg,
                _attack.ArcHalfAngleDeg);
            if (!BossStatusMath.VolumeHits(stealthed, inVolume, _attack.ArcHalfAngleDeg))
                inVolume = false;
            else if (Blind())
                inVolume = false;
            // F1: skill hareketi / çağırma anı i-frame'i (donmayan pencere) vuruşu boşa çıkarır;
            // dodge gibi sayılmaz ama cümle de kırılmaz.
            else if (_player != null && _player.GetComponent<PlayerDodgeRig>() is PlayerDodgeRig rig && rig.IsSkillInvulnerable)
                inVolume = false;

            int? press = _dodge?.PressTimeMs;
            // Eski basış bu telegrafa ait değil → "geç kaldın". Eşik basma anı DEĞİL, i-frame
            // sonu: telegraf başlarken dokunulmazlık hâlâ açıksa basış bu saldırıya aittir ve
            // sebebi "erken bastın" olmalı — `press < telegraphStart` bunu da yutuyordu (T8.1).
            if (press.HasValue && _dodge != null && _dodge.IframeEndMs(press.Value) < TelegraphStartMs)
                press = null;

            var input = new ExchangeInput
            {
                TelegraphStartMs = TelegraphStartMs,
                StrikeTimeMs = _brain != null && _brain.StrikeWorldMs > 0
                    ? _brain.StrikeWorldMs
                    : _attack.StrikeTimeMs(TelegraphStartMs),
                DodgePressMs = press,
                InEffectVolume = inVolume
            };

            ExchangeResult result = _resolver.Resolve(input);
            _feel?.OnExchange(result);

            if (result.Outcome == ExchangeOutcome.Hit)
            {
                float raw = BossStatusMath.OutgoingDamage(_attack.Damage, _bossStatus != null ? _bossStatus.Board : null);
                // Shield / stasis (skill i-frame) ActorStatus üzerinden — düz vitals bypass yok.
                bool landed;
                if (_playerStatus != null)
                {
                    _playerStatus.ApplyDamage(raw);
                    landed = _playerStatus.LastAppliedDamage > 0f;
                }
                else
                {
                    _vitals?.ApplyDamage(Mathf.CeilToInt(raw));
                    landed = raw > 0f;
                }
                // S6: kalkan/stasis vuruşu tamamen emdiyse cümle/kanal kesilmez.
                if (landed)
                    _engine?.Abort();

                if (_attack.Kind == BossAttackKind.FireCone && _playerStatus != null && _combat != null)
                    ApplyFireConeMechanics(_playerStatus.Board);
            }
        }

        Vector3 StrikeVolumeOrigin()
        {
            if (_attack != null && _attack.Kind == BossAttackKind.Pounce && _reactor != null)
                return new Vector3(_attack.LandingX, _reactor.Home.y, _attack.LandingZ);
            return _reactor != null ? _reactor.Home : transform.position;
        }

        void ApplyFireConeMechanics(StatusBoard board)
        {
            if (board == null || _combat == null)
                return;
            IReadOnlyList<string> mechs = _activeAttackEntry?.Mechanics;
            if (mechs == null || mechs.Count == 0)
            {
                board.Apply(StatusKind.Burn, _combat.Status.BurnMs, _combat.Status.BurnDamagePerSec);
                board.Apply(StatusKind.GrievousWounds, _combat.Status.GrievousMs, _combat.Status.GrievousHealMult);
                return;
            }
            ApplyBossMechanics(board, mechs);
        }

        void ApplyBossMechanics(StatusBoard board, IReadOnlyList<string> mechanics)
        {
            if (board == null || _combat == null || mechanics == null)
                return;
            StatusTuning t = _combat.Status;
            for (int i = 0; i < mechanics.Count; i++)
            {
                if (!StatusKindUtil.TryParse(mechanics[i], out StatusKind kind) || kind == StatusKind.None)
                    continue;
                ApplyBossMechanicKind(board, kind, t, durationSec: 0f);
            }
        }

        void ApplyBossMechanicKind(StatusBoard board, StatusKind kind, StatusTuning t, float durationSec)
        {
            double ms = durationSec > 0f ? durationSec * 1000.0 : 0;
            switch (kind)
            {
                case StatusKind.Burn:
                    board.Apply(kind, ms > 0 ? ms : t.BurnMs, t.BurnDamagePerSec);
                    break;
                case StatusKind.GrievousWounds:
                    board.Apply(kind, ms > 0 ? ms : t.GrievousMs, t.GrievousHealMult);
                    break;
                case StatusKind.Poison:
                    board.Apply(kind, ms > 0 ? ms : t.PoisonMs, t.PoisonDamagePerSec);
                    break;
                case StatusKind.Weaken:
                    board.Apply(kind, ms > 0 ? ms : t.WeakenMs, t.WeakenOutgoingMult);
                    break;
                case StatusKind.ArmorBreak:
                    board.Apply(kind, ms > 0 ? ms : t.ArmorBreakMs, t.ArmorBreakDamageTakenMult);
                    break;
                case StatusKind.Slow:
                    board.Apply(kind, ms > 0 ? ms : t.SlowMs, t.SlowSpeedMult);
                    break;
                case StatusKind.Blind:
                    board.Apply(kind, ms > 0 ? ms : t.BlindMs,
                        BossStatusMath.BlindChanceFromAccuracy(t.BlindMissChance));
                    break;
                case StatusKind.Root:
                    board.Apply(kind, ms > 0 ? ms : t.RootMs, 1f);
                    break;
                case StatusKind.Silence:
                    board.Apply(kind, ms > 0 ? ms : t.SilenceMs, 1f);
                    break;
                case StatusKind.Stun:
                    board.Apply(kind, ms > 0 ? ms : t.StunMs, 1f);
                    break;
                case StatusKind.Disarm:
                    board.Apply(kind, ms > 0 ? ms : t.DisarmMs, 1f);
                    break;
            }
        }

        void OnVolleyProjectilePlayerHit()
        {
            if (!_volleyOnHit.IsValid || _playerStatus == null || _combat == null)
                return;
            if (!StatusKindUtil.TryParse(_volleyOnHit.Id, out StatusKind kind) || kind == StatusKind.None)
                return;
            ApplyBossMechanicKind(_playerStatus.Board, kind, _combat.Status, _volleyOnHit.DurationSec);
        }

        /// <summary>
        /// Zehir Tükürüğü: hedefe (oyuncu / dost / yem) ortalanmış yelpaze. ters_kontrol açıksa
        /// yelpaze tersine döner. Körlük ıska zarı atmaz, yelpazeyi %50 açar. Hacim testi yok —
        /// isabet mermi çarpışmasında (HostileProjectileHost: i-frame geçirir, yem emer).
        /// </summary>
        void FireVolley()
        {
            if (_projectiles == null || _reactor == null)
                return;
            _volleyOnHit = _activeAttackEntry != null && _activeAttackEntry.OnHitStatus.IsValid
                ? _activeAttackEntry.OnHitStatus
                : default;
            Vector3 origin = _reactor.Home;
            Transform aim = AimTarget();
            Vector3 dir = aim != null ? aim.position - origin : transform.forward;
            dir.y = 0f;
            if (IsReversed && aim != null)
                dir = -dir;
            if (dir.sqrMagnitude < 0.0001f)
                dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();

            float spread = _attack.VolleySpreadDeg;
            bool blind = _bossStatus != null && BossStatusMath.BlindMissChance(_bossStatus.Board) > 0f;
            if (blind)
                spread *= 1.5f;
            int count = Mathf.Max(1, _attack.VolleyCount);
            float raw = BossStatusMath.OutgoingDamage(_attack.Damage, _bossStatus != null ? _bossStatus.Board : null);
            float start = count > 1 ? -spread * 0.5f : 0f;
            float step = count > 1 ? spread / (count - 1) : 0f;
            Vector3 from = origin + dir * (_reactor.BodyRadiusM + _attack.VolleyRadiusM);
            for (int i = 0; i < count; i++)
            {
                Vector3 d = Quaternion.AngleAxis(start + step * i, Vector3.up) * dir;
                _projectiles.Spawn(from, d * _attack.VolleySpeedMps, _attack.VolleyRadiusM, raw, _attack.VolleyLifeSec, _targetId);
            }
            DebugConfig.DevLog($"[Boss] Zehir Tükürüğü {count} mermi yelpaze {spread:0}°" + (blind ? " (kör)" : "") + $" hedef={CurrentTargetKind}");
        }

        /// <summary>
        /// Vuruş hacmi hedef değil, ALAN: oyuncu dışındaki her kayıtlı dost sınanır (slam 360°,
        /// koni yaya bakar). Dost: dodge yok, hasar ally_damage_mult ile; koni yanığı da geçer.
        /// Yem: hacimdeyse yok olur (aggro biter). Oyuncu yolu aşağıda, bugünkü kodun aynısı.
        /// </summary>
        void ResolveOtherFriendlies(System.Func<bool> blind)
        {
            if (_targets == null || _reactor == null)
                return;
            Vector3 strikeOrigin = StrikeVolumeOrigin();
            var entries = _targets.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                HostileTargets.Entry e = entries[i];
                if (e.Kind == TargetKind.Player || !e.IsAlive)
                    continue;
                Vector3 d = e.Transform.position - strikeOrigin;
                d.y = 0f;
                float dist = d.magnitude;
                float angleDeg = 0f;
                if (dist > 0.01f)
                {
                    Vector3 forward = transform.forward;
                    forward.y = 0f;
                    angleDeg = Vector3.SignedAngle(forward, d, Vector3.up);
                }
                bool inVolume = _attack.IsInEffectVolume(
                    BossStrikeShape.DistanceForVolume(dist, PortalBorderTeamHooks.BossStrikeScale),
                    angleDeg,
                    _attack.ArcHalfAngleDeg);
                if (!BossStatusMath.VolumeHits(e.IsStealthed, inVolume, _attack.ArcHalfAngleDeg) || blind())
                    continue;

                if (e.Kind == TargetKind.Decoy)
                {
                    e.Kill?.Invoke();
                    continue;
                }
                float raw = BossStatusMath.OutgoingDamage(_attack.Damage, _bossStatus != null ? _bossStatus.Board : null);
                e.Damage?.Invoke(raw);
                AllyDummy ally = _attack.Kind == BossAttackKind.FireCone ? e.Transform.GetComponent<AllyDummy>() : null;
                if (ally != null && _combat != null && ally.Board != null && !ally.IsDown)
                    ApplyFireConeMechanics(ally.Board);
            }
        }
    }
}
