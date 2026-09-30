using Dovus.Core.Combat;
using Dovus.Core.Grammar;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Prototip boss döngüsü: idle yaklaşma + YERE ÇAKMA üç ritmi (§11).
    /// Yaklaşma Home'a yazılır — transform'a değil (T7.2 geri tepme kalıcılığı).
    /// Varyant idle'da seçilir; ExchangeResolver yalnızca aktif windup/radius görür.
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

        public float AttackRadiusM => _attack?.RadiusM ?? 0f;
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
        }

        /// <summary>§11: can 0 — saldırı döngüsü durur, telegraf kapanır.</summary>
        public void NotifyBossDown(double worldMs)
        {
            _telegraph?.Hide();
            _feel?.ClearThreat();
            _visual?.PlayDeath();
            EnterIdle(worldMs);
        }

        /// <summary>§11: tam canla yeniden doğuş — idle beklemeden devam.</summary>
        public void NotifyBossRevived(double worldMs)
        {
            bool wasPhase2 = _phase2Announced;
            _phase2Announced = false;
            _poise?.Reset();
            _visual?.NotifyRevived();
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
            _telegraph?.SetProgress(p, _attack.RadiusM, _attack.Variant);
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
                _telegraph?.Slam(_attack != null ? _attack.RadiusM : 0f);
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
            _telegraph?.Recover(fade, _attack != null ? _attack.RadiusM : 0f);

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
            FacePlayer();
            _visual?.SetSpeed(0f);
            if (_attack != null)
            {
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
        /// 16 Eylül — önce SALDIRI TÜRÜ (Slam / FireCone) seçilir, Slam ise ardından
        /// üç ritminden biri. İkisi de "aynısı üst üste MaxSame...Streak'i geçemez" desenini
        /// paylaşır (§11/T13 dersi).
        /// </summary>
        bool SelectNextAttack()
        {
            if (_attack == null || _combat == null)
                return false;

            // karadul.json faz tasarımı: Faz 1 "Uyanış" (100-50% can) yalnızca slam; Faz 2
            // "Öfke" (50-0%) fire_cone'u da açar.
            bool enraged = IsEnraged();

            BossAttackKind kind = enraged
                ? BossAttackKindPicker.Pick(_lastAttackKind, _attackKindStreak, _combat.Boss.MaxSameAttackKindStreak, _rng)
                : BossAttackKind.Slam;
            if (!AttackKindAllowed(kind))
            {
                BossAttackKind alt = kind == BossAttackKind.FireCone
                    ? BossAttackKind.Slam
                    : BossAttackKind.FireCone;
                if (enraged && AttackKindAllowed(alt))
                    kind = alt;
                else
                    return false;
            }
            _attackKindStreak = BossAttackKindPicker.NextStreak(_lastAttackKind, _attackKindStreak, kind);
            _lastAttackKind = kind;

            if (kind == BossAttackKind.FireCone)
            {
                _attack.ApplyFireCone();
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

        void Approach(float dtSec)
        {
            if (_player == null || dtSec <= 0f)
                return;
            if (PlayerStealthed)
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
            Vector3 to = _player.position - home;
            to.y = 0f;
            float pad = _colors != null ? _colors.BossApproachStopPadM : 0.35f;
            float stop = _reactor.BodyRadiusM + (_playerMotor != null ? _playerMotor.BodyRadiusM : 0.5f) + pad;
            if (to.sqrMagnitude <= stop * stop)
            {
                _visual?.SetSpeed(0f);
                return;
            }

            float groundMps = _combat.Boss.ApproachSpeedMps * speedMult;
            home += to.normalized * groundMps * dtSec;
            _reactor.Home = home;
            _visual?.SetWalk(groundMps);
            TurnTowardPlayer(dtSec);
        }

        bool IsEnraged() =>
            _bossVitals != null && _bossVitals.MaxHp > 0f && (_bossVitals.Hp / _bossVitals.MaxHp) <= 0.5f;

        /// <summary>Yaklaşırken dönüş hız sınırlı; windup başındaki kilitleme (FacePlayer) anlık kalır.</summary>
        void TurnTowardPlayer(float dtSec)
        {
            if (_player == null || PlayerStealthed)
                return;
            Vector3 to = _player.position - _reactor.Home;
            to.y = 0f;
            if (to.sqrMagnitude <= 0.01f)
                return;
            float rate = _colors != null ? _colors.BossTurnRateDegPerSec : 240f;
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, Quaternion.LookRotation(to.normalized, Vector3.up), rate * dtSec);
        }

        void FacePlayer()
        {
            if (_player == null || PlayerStealthed)
                return;
            Vector3 to = _player.position - _reactor.Home;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void ResolveStrike()
        {
            if (_vitals != null && _vitals.IsDown)
                return;
            if (_attack == null || _resolver == null)
                return;

            float dist = 0f;
            float angleDeg = 0f;
            if (_player != null && _reactor != null)
            {
                Vector3 d = _player.position - _reactor.Home;
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
            bool inVolume = _attack.IsInEffectVolume(dist, angleDeg, _attack.ArcHalfAngleDeg);
            if (!BossStatusMath.VolumeHits(stealthed, inVolume, _attack.ArcHalfAngleDeg))
                inVolume = false;
            else if (_bossStatus != null
                && BossStatusMath.Misses(_bossStatus.Board, (float)_rng.NextDouble()))
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
                _engine?.Abort();
                float raw = BossStatusMath.OutgoingDamage(_attack.Damage, _bossStatus != null ? _bossStatus.Board : null);
                // Shield / stasis (skill i-frame) ActorStatus üzerinden — düz vitals bypass yok.
                if (_playerStatus != null)
                    _playerStatus.ApplyDamage(raw);
                else
                    _vitals?.ApplyDamage(Mathf.CeilToInt(raw));

                // fire_cone mekanikleri (karadul.json: grievous_wounds + burn).
                if (_attack.Kind == BossAttackKind.FireCone && _playerStatus != null && _combat != null)
                {
                    _playerStatus.Board.Apply(
                        Dovus.Core.Status.StatusKind.Burn, _combat.Status.BurnMs, _combat.Status.BurnDamagePerSec);
                    _playerStatus.Board.Apply(
                        Dovus.Core.Status.StatusKind.GrievousWounds, _combat.Status.GrievousMs, _combat.Status.GrievousHealMult);
                }
            }
        }
    }
}
