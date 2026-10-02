using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Portal;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Prototip boss döngüsü: idle yaklaşma + YERE ÇAKMA üç ritmi (§11).
    /// Yaklaşma Home'a yazılır — transform'a değil (T7.2 geri tepme kalıcılığı).
    /// Varyant idle'da seçilir; ExchangeResolver yalnızca aktif windup/radius görür.
    /// Hedef (oyuncu / dost / dikkat çeken yem) her idle döngüsünün başında <see cref="HostileTargets"/>
    /// ile seçilir; yaklaşma ve windup kilidi hedefe bakar, vuruş hacmi kayıtlı HER dostu sınar.
    /// </summary>
    public sealed class BossDirector : MonoBehaviour
    {
        enum Phase
        {
            Idle,
            Windup,
            Active,
            Recovery
        }

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
        HostileTargets _targets;
        HostileProjectileHost _projectiles;
        Transform _target;
        int _targetId = -1;
        TargetKind _targetKind = TargetKind.Player;
        double _reverseUntilMs = -1;

        Phase _phase = Phase.Idle;
        double _phaseStartedWorldMs;
        double _phaseElapsedMs;
        double _idleUntilWorldMs;
        int _telegraphStartMs;
        int _strikeWorldMs;
        bool _strikeResolved;
        bool _playerWasDown;
        Vector3 _originHome;
        readonly System.Random _rng = new();

        SlamVariant? _lastVariant;
        int _variantStreak;
        BossAttackKind? _lastAttackKind;
        int _attackKindStreak;

        bool _phase2Announced;

        bool _bossEncounterLoaded;
        BossAttackKind[] _phase1AttackKinds = System.Array.Empty<BossAttackKind>();
        BossAttackKind[] _phase2AttackKinds = System.Array.Empty<BossAttackKind>();
        readonly Dictionary<BossAttackKind, BossEncounterData.BossAttackEntry> _attackEntriesByKind = new();
        BossEncounterData.BossAttackEntry _activeAttackEntry;
        BossEncounterData.BossOnHitStatus _volleyOnHit;

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

        public bool IsWindingUp => _phase == Phase.Windup;
        public BossAttackKind? CurrentAttackKind => _phase is Phase.Windup or Phase.Active ? _attack?.Kind : null;

        /// <summary>Windup ilerlemesi 0→1 (windup dışında 0) — boss cast barı okur.</summary>
        public float WindupProgress01 =>
            _phase == Phase.Windup && _attack != null && _attack.WindupMs > 0
                ? Mathf.Clamp01((float)(_phaseElapsedMs / _attack.WindupMs))
                : 0f;
        public int TelegraphStartMs => _telegraphStartMs;
        public int StrikeTimeMs
        {
            get
            {
                if (_attack == null)
                    return 0;
                if (_strikeWorldMs > 0 && _phase is Phase.Active or Phase.Recovery)
                    return _strikeWorldMs;
                if (_phase == Phase.Windup && _attack.WindupMs > 0)
                {
                    float speed = Mathf.Max(0.05f, CurrentPhaseSpeed());
                    double remain = System.Math.Max(0, _attack.WindupMs - _phaseElapsedMs);
                    double now = _clock != null ? _clock.Director.WorldTimeMs : _phaseStartedWorldMs;
                    return (int)(now + remain / speed);
                }
                return _attack.StrikeTimeMs(_telegraphStartMs);
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
            EnterIdle(clock.Director.WorldTimeMs);
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

            TryLoadBossEncounter();
        }

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
            IReadOnlyList<BossEncounterData.BossAttackEntry> attacks = BossEncounterData.LoadAttacks(path);
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
            EnterIdle(worldMs);
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
            EnterIdle(worldMs);
        }

        /// <summary>
        /// mechanic_grammar Aynalı tempo: hazırlanmakta olan saldırıyı iptal edip yeni idle
        /// döngüsüne döner. Boss AI seçimini değiştirmez; yalnız mevcut cast state'i sarar.
        /// </summary>
        public bool CancelPreparedAttack(double worldMs)
        {
            if (_phase != Phase.Windup)
                return false;
            _telegraph?.Hide();
            _feel?.ClearThreat();
            EnterIdle(worldMs);
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
            _phase = Phase.Idle;
            _strikeResolved = true;
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
                if (_phase != Phase.Idle)
                    _phase = Phase.Idle;
                _visual?.SetSpeed(0f);
                return;
            }

            if (_phase == Phase.Windup && _bossStatus != null && _attack != null)
            {
                BossAttackGate gate = BossAttackControl.Gate(
                    _bossStatus.Board, CurrentMotion(), _attack.Kind, staggered: false);
                if (gate.CancelWindup)
                {
                    _telegraph?.Hide();
                    _feel?.ClearThreat();
                    EnterIdle(worldMs);
                    return;
                }
            }

            switch (_phase)
            {
                case Phase.Idle:
                    TickIdle(worldMs, dtSec);
                    break;
                case Phase.Windup:
                    TickWindup(worldMs, _clock.WorldDeltaMs);
                    break;
                case Phase.Active:
                    TickActive(worldMs);
                    break;
                case Phase.Recovery:
                    TickRecovery(worldMs, _clock.WorldDeltaMs);
                    break;
            }
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
                EnterIdle(worldMs);
            }

            _playerWasDown = down;
        }

        void TickIdle(double worldMs, float dtSec)
        {
            _telegraph?.Hide();
            _feel?.ClearThreat();

            if (_vitals != null && _vitals.IsDown)
            {
                _idleUntilWorldMs = worldMs + _combat.Boss.IdleMinMs;
                return;
            }

            if (!_phase2Announced && IsEnraged())
            {
                _phase2Announced = true;
                _visual?.PlayRoar();
                BossPhaseChanged?.Invoke(2);
            }

            // Kükreme klibi bitene kadar yürümez / saldırmaz (sunum; saldırı sırası değişmez).
            if (_visual != null && _visual.IsBusy)
            {
                _visual.SetSpeed(0f);
                _idleUntilWorldMs = System.Math.Max(_idleUntilWorldMs, worldMs + _combat.Boss.IdleMinMs);
                return;
            }

            if (_targets != null && _targets.ShouldRetarget(_targetId))
                PickTarget();

            Approach(dtSec);

            if (worldMs >= _idleUntilWorldMs)
            {
                if (_bossStatus != null
                    && !BossAttackControl.Evaluate(_bossStatus.Board, BossAttackMotion.Standing).CanStart)
                    return;
                // Saldırı (ve Slam ise varyantı) telegraf başlamadan seçilir — tell windup'ta okunur (§11).
                if (!SelectNextAttack())
                    return;
                if (_bossStatus != null && _attack != null
                    && !BossAttackControl.Gate(
                        _bossStatus.Board, CurrentMotion(), _attack.Kind, staggered: false).CanStart)
                    return;
                EnterWindup(worldMs);
            }
        }

        void TickWindup(double worldMs, double worldDtMs)
        {
            if (_attack == null)
                return;

            _phaseElapsedMs += System.Math.Max(0, worldDtMs) * CurrentPhaseSpeed();
            float p = _attack.WindupMs > 0
                ? (float)(_phaseElapsedMs / _attack.WindupMs)
                : 1f;
            _telegraph?.SetProgress(p, AttackRadiusM, _attack.Variant);
            _feel?.ShowThreat(p);

            if (_phaseElapsedMs >= _attack.WindupMs)
                EnterActive(worldMs);
        }

        void TickActive(double worldMs)
        {
            if (!_strikeResolved)
            {
                // ResolveStrike NRE olsa bile Active'de kilitlenmeyelim.
                _strikeResolved = true;
                try
                {
                    ResolveStrike();
                }
                catch (System.Exception e)
                {
                    Debug.LogException(e);
                }
                _telegraph?.Slam(AttackRadiusM);
                if (_attack != null)
                    AttackStruck?.Invoke(_attack.Kind);
            }

            if (_attack != null && worldMs >= _attack.ActiveEndMs(_telegraphStartMs))
                EnterRecovery(worldMs);
        }

        void TickRecovery(double worldMs, double worldDtMs)
        {
            _feel?.ClearThreat();
            _phaseElapsedMs += System.Math.Max(0, worldDtMs) * CurrentPhaseSpeed();
            float fade = _attack != null && _attack.RecoveryMs > 0
                ? 1f - (float)(_phaseElapsedMs / _attack.RecoveryMs)
                : 0f;
            _telegraph?.Recover(fade, AttackRadiusM);

            if (_attack != null && _phaseElapsedMs >= _attack.RecoveryMs)
                EnterIdle(worldMs);
        }

        void EnterIdle(double worldMs)
        {
            _phase = Phase.Idle;
            _phaseStartedWorldMs = worldMs;
            // T10: panelin Min/Max slider'ları BAĞIMSIZ hareket eder; Min > Max olursa
            // Random.Next negatif aralıkla ArgumentOutOfRangeException fırlatır (tüm boss
            // döngüsünü kilitler). Min/Max burada garantiye alınıyor, slider'lara dokunulmadı.
            int lo = System.Math.Min(_combat.Boss.IdleMinMs, _combat.Boss.IdleMaxMs);
            int hi = System.Math.Max(_combat.Boss.IdleMinMs, _combat.Boss.IdleMaxMs);
            int wait = _rng.Next(lo, hi + 1);
            _idleUntilWorldMs = worldMs + wait;
            // Hedef saldırı başına bir kez: bu idle'da ona yürünür, windup ona kilitlenir.
            PickTarget();
            _telegraph?.Hide();
            if (_bossVitals == null || !_bossVitals.IsDown)
                _visual?.PlayIdle();
        }

        void EnterWindup(double worldMs)
        {
            _phase = Phase.Windup;
            _phaseStartedWorldMs = worldMs;
            _phaseElapsedMs = 0;
            _strikeWorldMs = 0;
            _telegraphStartMs = (int)worldMs;
            _strikeResolved = false;
            FaceTarget();
            _visual?.SetSpeed(0f);
            if (_attack != null)
            {
                if (_attack.Kind == BossAttackKind.WebField)
                {
                    Transform aim = AimTarget();
                    LastWebFieldTarget = aim != null ? aim.position : _reactor.Home;
                }
                _telegraph?.SetShape(_attack.ArcHalfAngleDeg);
                _visual?.PlayWindup(_attack.Kind, _attack.WindupMs);
                AttackWindupStarted?.Invoke(_attack.Kind);
            }
        }

        void EnterActive(double worldMs)
        {
            _phase = Phase.Active;
            _phaseStartedWorldMs = worldMs;
            _phaseElapsedMs = 0;
            _strikeWorldMs = (int)worldMs;
            _visual?.PlaySlam();
        }

        void EnterRecovery(double worldMs)
        {
            _phase = Phase.Recovery;
            _phaseStartedWorldMs = worldMs;
            _phaseElapsedMs = 0;
        }

        BossAttackMotion CurrentMotion() =>
            _attack == null ? BossAttackMotion.Standing : BossAttackControl.MotionOf(_attack.Kind);

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
            System.ReadOnlySpan<BossAttackKind> phaseKinds = enraged
                ? (_phase2AttackKinds.Length > 0
                    ? _phase2AttackKinds
                    : BossAttackKindPicker.AllowedFor(true))
                : (_phase1AttackKinds.Length > 0
                    ? _phase1AttackKinds
                    : BossAttackKindPicker.AllowedFor(false));
            System.Span<BossAttackKind> allowed = stackalloc BossAttackKind[phaseKinds.Length];
            int n = 0;
            Transform aim = AimTarget();
            float pounceDist = 0f;
            if (aim != null && _reactor != null)
            {
                Vector3 toAim = aim.position - _reactor.Home;
                toAim.y = 0f;
                pounceDist = toAim.magnitude;
            }
            for (int i = 0; i < phaseKinds.Length; i++)
            {
                BossAttackKind k = phaseKinds[i];
                if (k == BossAttackKind.Volley && _projectiles == null)
                    continue;
                if (k == BossAttackKind.Pounce
                    && (aim == null || !BossAttackKindPicker.PounceInRange(pounceDist, _combat.Boss)))
                    continue;
                if (AttackKindAllowed(k))
                    allowed[n++] = k;
            }
            if (n == 0)
                return false;

            BossAttackKind kind = BossAttackKindPicker.Pick(
                _lastAttackKind, _attackKindStreak, _combat.Boss.MaxSameAttackKindStreak, _rng, allowed.Slice(0, n));
            _attackKindStreak = BossAttackKindPicker.NextStreak(_lastAttackKind, _attackKindStreak, kind);
            _lastAttackKind = kind;
            _activeAttackEntry = _attackEntriesByKind.TryGetValue(kind, out BossEncounterData.BossAttackEntry entry)
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

            SlamVariant picked = SlamVariantPicker.Pick(
                _lastVariant,
                _variantStreak,
                _combat.Boss.MaxSameVariantStreak,
                _rng);
            _variantStreak = SlamVariantPicker.NextStreak(_lastVariant, _variantStreak, picked);
            _lastVariant = picked;
            _attack.ApplyVariant(picked);
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
            if (press.HasValue && _dodge != null && _dodge.IframeEndMs(press.Value) < _telegraphStartMs)
                press = null;

            var input = new ExchangeInput
            {
                TelegraphStartMs = _telegraphStartMs,
                StrikeTimeMs = _strikeWorldMs > 0 ? _strikeWorldMs : _attack.StrikeTimeMs(_telegraphStartMs),
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
