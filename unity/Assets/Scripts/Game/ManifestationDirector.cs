using System;
using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Layers;
using Dovus.Core.Manifestation;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Cümleyi dünyadaki yaşayan etkiye bağlar. Kapalı rün geri bildirimi T6.1'de — burada yok.
    /// T12: kapanış ödülü × ClosingDamagePerEffect → BossVitals; tür son rüne bağlı tepki.
    /// </summary>
    public sealed class ManifestationDirector : MonoBehaviour
    {
        GameClock _clock;
        SentenceEngine _engine;
        CombatTuning _combat;
        PrototypeTuning _colors;
        Transform _player;
        ActorPose _pose;
        ActorVisual _visual;
        BossReactor _boss;
        BossVitals _bossVitals;
        GroundScarField _scars;
        KinematicMotor _motor;
        DamageNumberHud _damageHud;
        BossDirector _bossDirector;
        SentenceDebugHud _debugHud;
        ReactionReadout _readout;
        FollowCamera _camera;

        readonly List<LivingEffectView> _active = new();
        readonly List<PendingClosing> _pending = new();
        readonly HashSet<LivingEffect> _closingStamped = new();

        // Cümlenin şu an sözcük aldığı etki — nokta sayısına göre değil, kimliğe göre izlenir
        // (aynı karede birden fazla nokta kaydı sayı polling'ini atlayabilir, bkz. T7.1).
        LivingEffectView _buildingView;
        int _lastWordCount;
        bool _hooked;
        bool _posedForRecovery;

        // §11 ölüm: çökme süresi bitince Revive (yavaş çekim yok).
        bool _deathPending;
        double _deathReviveAtMs;

        SkillMotor _skills;
        DamageCalculator _damageCalculator;
        ActorStatus _playerStatus;
        ActorStatus _bossStatus;
        SkillMotionDriver _motionDriver;
        StateBridgeBoard _stateBoard;
        StateBridgeView _bridgeView;
        AllyDummy _ally;

        // --- Ulti (active_modes) — 16 Eylül, güven kaygısına karşılık uçtan uca ---
        ActiveModeDirector _modeDirector;
        ActiveModeHud _modeHud;
        // --- Pasifler (Bağlama 5) — ulti gibi ama cooldown'suz, birden fazla aynı anda ---
        PassiveDirector _passiveDirector;
        PassiveHud _passiveHud;
        // --- Zincir (Bağlama 6) — son N cast elementi; Links geçmişe yazılmaz ---
        ChainDirector _chainDirector;
        ChainRules _chainRules;
        readonly Queue<int> _recentCastElements = new();
        float _pendingChainBonus = 1f;   // bir sonraki kapanış (links / finisher_mult)
        float _closingChainBonus = 1f;  // bu kapanışta ApplyClosing* çarpanı
        ChainStepResult _lastChainStep = ChainStepResult.None;
        string _lastFinisherAnnounced = string.Empty;
        // --- Zone (Bağlama 7) — element_origin ↔ zone_layer.zones ---
        ZoneDirector _zoneDirector;
        ZoneFieldView _zoneField;
        // --- Zaman (Bağlama 8) — echo + extend_lifetime; delayed_detonation/death_delay YOK ---
        TimeEffectDirector _timeEffectDirector;
        readonly List<TimeEffectField> _dueTimeFields = new();
        // --- Gerçeklik (Bağlama 11) — revive_block + erase ---
        RealityEffectDirector _realityDirector;
        // --- Ekipman (Bağlama 9) — sabit silah; seçim UI yok ---
        EquipmentItem _equippedWeapon;
        EquipmentBonusResolver _equipmentBonus;
        // --- Animasyon (Bağlama 10) — PresentationCatalog → AnimationBridge; PulseRune kalır ---
        PresentationCatalog _presentationCatalog;
        PresentationValidator _presentationValidator;
        readonly AnimationBridge _animationBridge = new();
        PentagonView _pentagonView;
        PlayerResource _playerResource;
        PlayerCooldown _playerCooldown;
        double _lastDamageDealtMs = double.NegativeInfinity;
        double _lastMovedMs = double.NegativeInfinity;
        float _modeHpDrainAccum;

        /// <summary>PrototypeBootstrap'ın atadığı sabit silah (ör. Alev Kılıcı).</summary>
        public EquipmentItem EquippedWeapon => _equippedWeapon;

        /// <summary>Bağlama 9 / MCP: son kapanışta uygulanan ekipman çarpanı (eşleşme 1.1, değilse 1).</summary>
        public float LastEquipmentMatchMult { get; private set; } = 1f;

        /// <summary>Bağlama 9 / MCP: son ApplyClosingDamage çıktısı (boss'a giden, armor öncesi).</summary>
        public float LastClosingDamageDealt { get; private set; }

        /// <summary>Bağlama 10 / MCP: son ShoutSkill AnimationType id (katalog anahtarı).</summary>
        public string LastAnimationTypeId { get; private set; } = string.Empty;

        /// <summary>Bağlama 10 / MCP: son denenen animator_state.</summary>
        public string LastAnimationState { get; private set; } = string.Empty;

        /// <summary>Bağlama 10 / MCP: Controller'da state vardı ve Play uygulandı.</summary>
        public bool LastAnimationPlayApplied { get; private set; }

        /// <summary>Bağlama 10 / MCP: frame-timer köprüsü (Play doğrulama).</summary>
        public AnimationBridge AnimationBridge => _animationBridge;

        /// <summary>Bağlama 10 / MCP: ShoutSkill içindeki ApplySkillAnimation yolunu doğrudan dener.</summary>
        public void DebugApplySkillAnimation(SkillResolution skill) => ApplySkillAnimation(skill);

        SkillMotor Skills => _skills ??= SkillMotorLoader.LoadOrDefault();

        DamageCalculator EnsureDamageCalculator()
        {
            if (_damageCalculator != null)
                return _damageCalculator;

            const string resourcePath = "ElementSystem/element-sistemi";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try
                {
                    // Play'te crit ara sıra çıksın diye seed sabit değil.
                    _damageCalculator = DamageCalculator.FromElementSystemJson(
                        asset.text,
                        seed: unchecked((int)System.DateTime.UtcNow.Ticks));
                    return _damageCalculator;
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"DamageCalculator JSON okunamadı: {e.Message}");
                }
            }

            // Resources yoksa: docs/element-sistemi.json crit_system varsayılanları
            // (base 0.05 / mult 2.0 / max 0.75) — sayı uydurma yok.
            _damageCalculator = new DamageCalculator(
                seed: unchecked((int)System.DateTime.UtcNow.Ticks),
                baseCritChance: 0.05f,
                critMultiplier: 2f,
                maxCritChance: 0.75f,
                adjectiveCritBonus: null);
            return _damageCalculator;
        }

        struct PendingClosing
        {
            public LivingEffectView View;
            public ClosingHit Closing;
            public double BangAtWorldMs;
            public List<SentenceWord> Words;
            public bool IsBasicStrike;
        }

        public LivingEffect ActiveLogic => _buildingView?.Logic;

        public int ActiveCount => _active.Count;

        /// <summary>Bağlama 5 / MCP: Bind sonrası pasif durum makinesi (null = henüz bağlanmadı).</summary>
        public PassiveDirector PassiveDirector => _passiveDirector;

        /// <summary>Bağlama 6 / MCP: Bind sonrası zincir durum makinesi.</summary>
        public ChainDirector ChainDirector => _chainDirector;

        /// <summary>Bağlama 6 / MCP: son duyurulan Finisher metni (boş = henüz yok).</summary>
        public string LastFinisherAnnounced => _lastFinisherAnnounced;

        /// <summary>Bağlama 6 / MCP: bir sonraki kapanışa bekleyen Links/finisher çarpanı.</summary>
        public float PendingChainBonus => _pendingChainBonus;

        /// <summary>Bağlama 7 / MCP: Bind sonrası zone yaşam döngüsü.</summary>
        public ZoneDirector ZoneDirector => _zoneDirector;

        /// <summary>Bağlama 8 / MCP: Bind sonrası zaman alanları (echo vb.).</summary>
        public TimeEffectDirector TimeEffectDirector => _timeEffectDirector;

        /// <summary>Bağlama 11 / MCP: revive_block + erase.</summary>
        public RealityEffectDirector RealityDirector => _realityDirector;

        /// <summary>Editör/prob: Update beklemeden cümle senkronu.</summary>
        public void ForceSync()
        {
            if (_clock == null)
                return;
            SyncFromSentence(_clock.Director.WorldTimeMs);
        }

        public void Bind(
            GameClock clock,
            PentagonInput input,
            Transform player,
            ActorPose pose,
            BossReactor boss,
            BossVitals bossVitals,
            GroundScarField scars,
            PrototypeTuning colors,
            DamageNumberHud damageHud = null,
            BossDirector bossDirector = null,
            ActorStatus playerStatus = null,
            ActorStatus bossStatus = null,
            SentenceDebugHud debugHud = null,
            ReactionReadout readout = null,
            FollowCamera camera = null,
            AllyDummy ally = null,
            ActiveModeHud modeHud = null,
            PentagonView pentagonView = null,
            PassiveHud passiveHud = null,
            EquipmentItem equippedWeapon = null,
            EquipmentBonusResolver equipmentBonus = null)
        {
            _clock = clock;
            _engine = input.Engine;
            _combat = input.Combat;
            _colors = colors;
            _player = player;
            _pose = pose;
            _visual = player != null ? player.GetComponent<ActorVisual>() : null;
            _boss = boss;
            _bossVitals = bossVitals;
            _scars = scars;
            _damageHud = damageHud;
            _bossDirector = bossDirector;
            _motor = player.GetComponent<KinematicMotor>();
            _playerStatus = playerStatus;
            _bossStatus = bossStatus;
            _debugHud = debugHud;
            _readout = readout;
            _camera = camera;
            _ally = ally;
            _modeHud = modeHud;
            _passiveHud = passiveHud;
            _pentagonView = pentagonView;
            _equippedWeapon = equippedWeapon;
            _equipmentBonus = equipmentBonus;
            _playerResource = player != null ? player.GetComponent<PlayerResource>() : null;
            _playerCooldown = player != null ? player.GetComponent<PlayerCooldown>() : null;
            _skills = SkillMotorLoader.LoadOrDefault();
            EnsurePresentationCatalog();
            _modeDirector = new ActiveModeDirector(_skills.ActiveModes);
            _passiveDirector = new PassiveDirector(_skills.Passives);
            _chainRules = LoadChainRulesOrDefault();
            _chainDirector = new ChainDirector(_skills.Chains, _chainRules);
            _recentCastElements.Clear();
            _pendingChainBonus = 1f;
            _closingChainBonus = 1f;
            _lastChainStep = ChainStepResult.None;
            _lastFinisherAnnounced = string.Empty;
            _zoneDirector = new ZoneDirector(_skills.MaxActiveZones);
            _zoneField = FindAnyObjectByType<ZoneFieldView>();
            if (_zoneField == null)
            {
                var zoneGo = new GameObject("ZoneField");
                _zoneField = zoneGo.AddComponent<ZoneFieldView>();
            }
            _zoneField.EnsureRoot();
            int timeCap = _skills.MaxActiveTimeFields > 0
                ? _skills.MaxActiveTimeFields
                : Dovus.Core.Combat.TimeEffectDirector.DefaultMaxActiveFields;
            _timeEffectDirector = new TimeEffectDirector(timeCap);
            _dueTimeFields.Clear();
            _realityDirector = new RealityEffectDirector();
            var playerVitals = player != null ? player.GetComponent<PlayerVitals>() : null;
            if (playerVitals != null)
            {
                playerVitals.SetReviveBlockedGate(() =>
                    _realityDirector != null
                    && _clock != null
                    && _realityDirector.IsReviveBlocked(_clock.Director.WorldTimeMs));
            }
            if (_playerStatus != null)
                _playerStatus.ModeDirector = _modeDirector;

            _motionDriver = player.GetComponent<SkillMotionDriver>();
            if (_motionDriver == null)
                _motionDriver = player.gameObject.AddComponent<SkillMotionDriver>();
            _motionDriver.Bind(clock, colors);

            _stateBoard = new StateBridgeBoard();
            _bridgeView = FindAnyObjectByType<StateBridgeView>();
            if (_bridgeView == null)
            {
                var bridgeGo = new GameObject("StateBridge");
                _bridgeView = bridgeGo.AddComponent<StateBridgeView>();
            }
            _bridgeView.Bind(_stateBoard);

            if (_engine != null && !_hooked)
            {
                _engine.SentenceCompleted += OnSentenceCompleted;
                _hooked = true;
            }
        }

        void OnDestroy()
        {
            if (_engine != null && _hooked)
                _engine.SentenceCompleted -= OnSentenceCompleted;
        }

        void Update()
        {
            if (_engine == null || _clock == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);

            // Kilit kesildi (§5): poz da kesilir. Kapanış patlaması kesilmez, kendi
            // zamanlamasıyla gelir (TickPendingClosings).
            if (_posedForRecovery && _engine.State.Phase != SentencePhase.Recovering)
            {
                _pose?.EndRecovery();
                _posedForRecovery = false;
            }

            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.01f)
                _lastMovedMs = worldMs;

            SyncFromSentence(worldMs);
            ApplyWindowCue();
            TickEffects(dtSec, worldMs);
            _pose?.Tick(worldMs);
            _boss?.Tick(dtSec, worldMs);
            TickPendingClosings(worldMs);
            TickBossDeath();
            TickStateBridge(worldMs);
            TickActiveMode(worldMs, dtSec);
            TickPassives(worldMs);
            TickZones(dtSec);
            TickTimeEffects(worldMs);
            _animationBridge.Tick(worldMs);
        }

        // --- Ulti (active_modes) ---

        ActiveModeContext BuildModeContext(double worldMs)
        {
            var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            float hpRatio = vitals != null && vitals.MaxHp > 0 ? (float)vitals.Hp / vitals.MaxHp : 1f;
            float allyRatio = _ally != null ? _ally.Ratio : 1f;

            int debuffCount = 0;
            if (_playerStatus != null)
            {
                foreach (StatusKind kind in _playerStatus.Board.ActiveKinds)
                    if (StatusKindUtil.IsDebuff(kind)) debuffCount++;
            }

            if (_ally != null)
            {
                _ally.EnsureStatusBoard();
                if (_ally.Board != null)
                {
                    foreach (StatusKind kind in _ally.Board.ActiveKinds)
                        if (StatusKindUtil.IsDebuff(kind)) debuffCount++;
                }
            }

            return new ActiveModeContext
            {
                HpRatio = hpRatio,
                MinTeamHpRatio = Mathf.Min(hpRatio, allyRatio),
                SecondsSinceLastDamageDealt = (worldMs - _lastDamageDealtMs) / 1000.0,
                SecondsSinceLastMoved = (worldMs - _lastMovedMs) / 1000.0,
                TeamDebuffCount = debuffCount,
            };
        }

        void TickActiveMode(double worldMs, float dtSec)
        {
            if (_modeDirector == null)
                return;

            if (_modeDirector.Active == null)
            {
                _modeHpDrainAccum = 0f;
                return;
            }

            // Sürekli maliyet: HP/sn (Öfke Patlaması) — tam sayıya birikip öyle uygulanır,
            // yoksa 60 FPS'te her kare 0'a yuvarlanan hasar hiç işlemez.
            float hpPct = _modeDirector.HpPerSecPercentCost;
            var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            if (hpPct > 0f && vitals != null && !vitals.IsDown)
            {
                _modeHpDrainAccum += vitals.MaxHp * (hpPct / 100f) * dtSec;
                int whole = Mathf.FloorToInt(_modeHpDrainAccum);
                if (whole > 0)
                {
                    _modeHpDrainAccum -= whole;
                    vitals.ApplyDamage(whole);
                }
            }

            ActiveModeContext ctx = BuildModeContext(worldMs);
            if (_modeDirector.Tick(worldMs, ctx))
            {
                _modeHud?.Hide();
                _modeHpDrainAccum = 0f;
            }
            else
            {
                _modeHud?.UpdateRemaining(_modeDirector.RemainingSec(worldMs));
            }
        }

        /// <summary>Dört-aynı-rün kapanışı geldiğinde (X-X-X-X) ulti tetiklenip tetiklenmediğine bakar.</summary>
        void TryActivateMode(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            if (_modeDirector == null || words == null || words.Count != 4 || _modeDirector.Active != null)
                return;

            int dot = (int)words[0].Rune;
            for (int i = 1; i < words.Count; i++)
                if ((int)words[i].Rune != dot)
                    return; // aynı elementin 4'lüsü değil — sıradan 4'lü cümle, ulti değil

            ActiveModeContext ctx = BuildModeContext(worldMs);
            ActiveModeNode? activated = _modeDirector.TryTrigger(dot, ctx, worldMs);
            if (activated == null)
                return;

            ActiveModeNode mode = activated.Value;
            Color tint = _colors != null ? _colors.ColorForRune((Rune)dot) : Color.white;
            _readout?.NoteSkill(mode.Name, mode.ReadAs, tint);
            _debugHud?.NoteSkillBang(mode.Name, mode.ReadAs);
            _modeHud?.ShowActivated(mode.Name, mode.ReadAs, tint);
            ApplyModeOneShotEffects(mode);
        }

        /// <summary>
        /// team_full_cleanse / team_invulnerability_sec / enemy_blind_sec / team_regen_per_sec —
        /// aktivasyon anında bir kez uygulanır (sürekli tick TickActiveMode'da değil, burada).
        /// </summary>
        void ApplyModeOneShotEffects(ActiveModeNode mode)
        {
            if (mode.GetEffectBool("team_full_cleanse") && _playerStatus != null)
                _playerStatus.Board.CleanseHostile();

            float invulnSec = mode.GetEffect("team_invulnerability_sec");
            if (invulnSec > 0f && _playerStatus != null)
                _playerStatus.Board.Apply(StatusKind.Stasis, invulnSec * 1000.0, 1f);

            float blindSec = mode.GetEffect("enemy_blind_sec");
            if (blindSec > 0f && _bossStatus != null)
                _bossStatus.Board.Apply(StatusKind.Blind, blindSec * 1000.0, 1f);

            float regenPerSec = mode.GetEffect("team_regen_per_sec");
            if (regenPerSec > 0f && mode.HasDuration && _playerStatus != null)
                _playerStatus.Board.Apply(StatusKind.Regen, mode.DurationSec * 1000.0, regenPerSec);
        }

        // --- Pasifler (Bağlama 5) ---

        void TickPassives(double worldMs)
        {
            if (_passiveDirector == null)
                return;

            _passiveDirector.Tick(worldMs);
            _passiveHud?.Sync(_passiveDirector.Active, worldMs);
        }

        /// <summary>
        /// Kapanıştaki rün dizisi bir pasifin trigger_combo'suyla eşleşirse açar.
        /// Ulti'den farkı: cooldown yok; birden fazla pasif aynı anda aktif olabilir.
        /// </summary>
        void TryTriggerPassive(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            if (_passiveDirector == null || words == null || words.Count == 0)
                return;

            var dots = new int[words.Count];
            for (int i = 0; i < words.Count; i++)
                dots[i] = (int)words[i].Rune;

            PassiveNode? triggered = _passiveDirector.TryTrigger(dots, worldMs);
            if (triggered == null)
                return;

            PassiveNode p = triggered.Value;
            Color tint = Color.cyan;
            if (_colors != null && p.TriggerCombo != null && p.TriggerCombo.Length > 0)
                tint = _colors.ColorForRune((Rune)p.TriggerCombo[0]);
            _readout?.NoteSkill(p.Id.Replace('_', ' '), p.Element, tint);
            _debugHud?.NoteSkillBang(p.Id, p.Element);
            _passiveHud?.Sync(_passiveDirector.Active, worldMs);
        }

        // --- Zone (Bağlama 7) ---

        void TickZones(float dtSec)
        {
            if (_zoneDirector == null)
                return;

            _zoneDirector.Tick(dtSec);
            UpdateZoneMovement();
            ApplyZoneCrowdControl();
            _zoneField?.Sync(_zoneDirector.ActiveZones);
        }

        /// <summary>
        /// Zone CcKind (skill.Mechanics root/slow) — hedef zone yarıçapındaysa StatusBoard yenile.
        /// Süre StatusTuning.RootMs / SlowMs (uydurma yok).
        /// </summary>
        void ApplyZoneCrowdControl()
        {
            if (_zoneDirector == null || _bossStatus == null || _boss == null)
                return;

            StatusTuning tuning = _combat != null ? _combat.Status : new StatusTuning();
            Vector3 bossPos = _boss.transform.position;
            IReadOnlyList<ZoneInstance> zones = _zoneDirector.ActiveZones;
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneInstance z = zones[i];
                if (string.IsNullOrEmpty(z.CcKind))
                    continue;

                float dx = bossPos.x - z.X;
                float dz = bossPos.z - z.Z;
                float r = z.RadiusM;
                if (dx * dx + dz * dz > r * r)
                    continue;

                if (string.Equals(z.CcKind, "root", StringComparison.Ordinal))
                    _bossStatus.Board.Apply(StatusKind.Root, tuning.RootMs, 1f);
                else if (string.Equals(z.CcKind, "slow", StringComparison.Ordinal))
                    _bossStatus.Board.Apply(StatusKind.Slow, tuning.SlowMs, tuning.SlowSpeedMult);
            }
        }

        /// <summary>
        /// Movement tiplerine hafif takip: player_directed → oyuncu; follow_target → boss.
        /// static / bilinmeyen → no-op (ZoneDirector zaten reddeder).
        /// </summary>
        void UpdateZoneMovement()
        {
            IReadOnlyList<ZoneInstance> zones = _zoneDirector.ActiveZones;
            for (int i = 0; i < zones.Count; i++)
            {
                ZoneInstance z = zones[i];
                if (string.Equals(z.Movement, ZoneMovement.PlayerDirected, StringComparison.Ordinal))
                {
                    if (_player == null) continue;
                    Vector3 p = _player.position;
                    _zoneDirector.MoveZone(z.Id, p.x, p.y, p.z);
                }
                else if (string.Equals(z.Movement, ZoneMovement.FollowTarget, StringComparison.Ordinal))
                {
                    Transform target = _boss != null ? _boss.transform : _player;
                    if (target == null) continue;
                    Vector3 t = target.position;
                    _zoneDirector.SetFollowTarget(z.Id, t.x, t.y, t.z);
                }
            }
        }

        /// <summary>
        /// Skill ElementOrigin, zone_layer.zones[].element ile eşleşirse TrySpawn + görsel Sync.
        /// Örn. Kaya → kaya_duvari (duration 10s). Radius = ManifestationTuning.ZoneDefaultRadiusM.
        /// </summary>
        void TrySpawnZoneForSkill(SkillResolution skill)
        {
            if (_zoneDirector == null || _skills == null || skill.IsEmpty)
                return;

            string origin = skill.ElementOrigin;
            if (string.IsNullOrEmpty(origin))
                return;

            ZoneNode? matched = null;
            for (int i = 0; i < _skills.Zones.Count; i++)
            {
                ZoneNode z = _skills.Zones[i];
                if (!string.Equals(z.Element, origin, StringComparison.OrdinalIgnoreCase))
                    continue;
                matched = z;
                break;
            }

            if (matched == null)
                return;

            ZoneNode zone = matched.Value;
            Vector3 pos = _player != null ? _player.position : Vector3.zero;
            float radius = _combat != null
                ? _combat.Manifestation.ZoneDefaultRadiusM
                : 3.6f;

            if (!_zoneDirector.TrySpawn(
                    zone.Element,
                    zone.Movement,
                    zone.DurationSec,
                    pos.x, pos.y, pos.z,
                    radius,
                    out _,
                    PickZoneCcKind(skill.Mechanics)))
                return;

            _zoneField?.Sync(_zoneDirector.ActiveZones);
        }

        /// <summary>mechanics dizisinden ilk root/slow — zone CC (kombo tablosu değil, skill verisi).</summary>
        static string PickZoneCcKind(string[] mechanics)
        {
            if (mechanics == null)
                return string.Empty;
            for (int i = 0; i < mechanics.Length; i++)
            {
                string m = mechanics[i];
                if (string.Equals(m, "root", StringComparison.OrdinalIgnoreCase))
                    return "root";
                if (string.Equals(m, "slow", StringComparison.OrdinalIgnoreCase))
                    return "slow";
            }
            return string.Empty;
        }

        /// <summary>
        /// Bağlama 11: ElementOrigin ↔ reality_layer (revive_block / partial_erase / full_erase).
        /// </summary>
        void TryApplyRealityForSkill(SkillResolution skill)
        {
            if (_realityDirector == null || _skills == null || skill.IsEmpty)
                return;

            string origin = skill.ElementOrigin;
            if (string.IsNullOrEmpty(origin))
                return;

            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            for (int i = 0; i < _skills.RealityEffects.Count; i++)
            {
                RealityEffectNode e = _skills.RealityEffects[i];
                if (!string.Equals(e.Element, origin, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(e.Type, RealityEffectTypes.ReviveBlock, StringComparison.Ordinal))
                {
                    float dur = e.HasDurationSec ? e.DurationSec : RealityEffectDirector.DefaultReviveBlockSec;
                    _realityDirector.ApplyReviveBlock(worldMs, dur);
                    _readout?.NoteSkill(e.Id.Replace('_', ' '), "diriliş engeli", Color.magenta);
                    return;
                }

                if (string.Equals(e.Type, RealityEffectTypes.PartialErase, StringComparison.Ordinal))
                {
                    StatusBoard board = _bossStatus != null ? _bossStatus.Board : null;
                    if (e.Targets != null && e.Targets.Count > 0)
                        _realityDirector.ApplyPartialErase(board, e.Targets);
                    else
                        _realityDirector.ApplyPartialErase(board);
                    _readout?.NoteSkill(e.Id.Replace('_', ' '), "kısmi silme", Color.magenta);
                    return;
                }

                if (string.Equals(e.Type, RealityEffectTypes.FullErase, StringComparison.Ordinal))
                {
                    StatusBoard board = _bossStatus != null ? _bossStatus.Board : null;
                    if (e.Targets != null && e.Targets.Count > 0)
                        _realityDirector.ApplyFullErase(board, e.Targets);
                    else
                        _realityDirector.ApplyFullErase(board);
                    _readout?.NoteSkill(e.Id.Replace('_', ' '), "tam silme", Color.magenta);
                    return;
                }
            }
        }

        /// <summary>
        /// Bağlama 8: ElementOrigin ↔ time_layer echo (Alev / alev_yanki).
        /// Kaynak hasarın damage_ratio kadarını delay_sec sonra uygular.
        /// </summary>
        void TryScheduleEchoForSkill(SkillResolution skill, float sourceDamage)
        {
            if (_timeEffectDirector == null || _skills == null || skill.IsEmpty || sourceDamage <= 0f)
                return;

            string origin = skill.ElementOrigin;
            if (string.IsNullOrEmpty(origin))
                return;

            for (int i = 0; i < _skills.TimeEffects.Count; i++)
            {
                TimeEffectNode e = _skills.TimeEffects[i];
                if (!string.Equals(e.Type, TimeEffectTypes.Echo, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(e.Element, origin, StringComparison.Ordinal))
                    continue;

                float delay = e.HasDelaySec ? e.DelaySec : 0f;
                float ratio = e.HasDamageRatio ? e.DamageRatio : 0f;
                double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
                _timeEffectDirector.TryScheduleEcho(
                    e.Id, e.Element, delay, ratio, sourceDamage, worldMs, out _);
                return;
            }
        }

        /// <summary>
        /// Bağlama 8: ElementOrigin ↔ extend_lifetime (Lav / lav_kalicilik).
        /// Aktif zone RemainingSec × multiplier (TrySpawn sonrası).
        /// </summary>
        void TryExtendZonesForSkill(SkillResolution skill)
        {
            if (_zoneDirector == null || _skills == null || skill.IsEmpty)
                return;

            string origin = skill.ElementOrigin;
            if (string.IsNullOrEmpty(origin))
                return;

            float multiplier = 0f;
            bool found = false;
            for (int i = 0; i < _skills.TimeEffects.Count; i++)
            {
                TimeEffectNode e = _skills.TimeEffects[i];
                if (!string.Equals(e.Type, TimeEffectTypes.ExtendLifetime, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(e.Element, origin, StringComparison.Ordinal))
                    continue;
                if (!e.HasMultiplier)
                    return;
                multiplier = e.Multiplier;
                found = true;
                break;
            }

            if (!found)
                return;

            IReadOnlyList<ZoneInstance> zones = _zoneDirector.ActiveZones;
            for (int i = zones.Count - 1; i >= 0; i--)
            {
                ZoneInstance z = zones[i];
                float next = Dovus.Core.Combat.TimeEffectDirector.ExtendRemainingSec(z.RemainingSec, multiplier);
                _zoneDirector.TrySetRemainingSec(z.Id, next);
            }

            _zoneField?.Sync(_zoneDirector.ActiveZones);
        }

        /// <summary>Bağlama 8: vadesi gelen echo alanlarını uygula (delayed_detonation/death_delay yok).</summary>
        void TickTimeEffects(double worldMs)
        {
            if (_timeEffectDirector == null)
                return;

            _dueTimeFields.Clear();
            int n = _timeEffectDirector.CollectDue(worldMs, _dueTimeFields);
            for (int i = 0; i < n; i++)
            {
                TimeEffectField field = _dueTimeFields[i];
                if (!string.Equals(field.Type, TimeEffectTypes.Echo, StringComparison.Ordinal))
                    continue;
                ApplyEchoDamage(field.ComputedEchoDamage);
            }
        }

        /// <summary>Yankı hasarı — kaynak × ratio; tekrar echo planlamaz.</summary>
        void ApplyEchoDamage(float amount)
        {
            if (amount <= 0f || _bossVitals == null || _bossVitals.IsDown)
                return;

            _damageHud?.ShowDamage(amount, isCrit: false);
            _lastDamageDealtMs = _clock != null ? _clock.Director.WorldTimeMs : _lastDamageDealtMs;

            bool killed = _bossVitals.ApplyDamage(amount);
            var bossVisual = _boss != null ? _boss.GetComponent<BossVisual>() : null;
            if (killed)
            {
                double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
                _bossDirector?.NotifyBossDown(worldMs);
                float collapseSec = _colors != null ? _colors.BossDeathCollapseSec : 0.85f;
                _boss?.BeginCollapse(collapseSec, worldMs);
                _deathReviveAtMs = worldMs + collapseSec * 1000.0;
                _deathPending = true;
                return;
            }

            bossVisual?.PlayStagger();
        }

        /// <summary>
        /// Bağlama 6: kapanışın fiil elementi (ilk rün) kuyruğa + ChainDirector.
        /// Links / finisher_mult geçmiş cast'e yazılmaz — yalnızca bir sonraki kapanışa
        /// (_pendingChainBonus). Bu kapanış önceki pending'i tüketir.
        /// </summary>
        float BeginChainClosing(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            float bonusForThis = _pendingChainBonus;
            _pendingChainBonus = 1f;
            _lastChainStep = ChainStepResult.None;

            if (_chainDirector == null || words == null || words.Count == 0)
                return bonusForThis;

            int element = (int)words[0].Rune;
            if (element < 1)
                return bonusForThis;

            _recentCastElements.Enqueue(element);
            int max = _chainRules.MaxChainLength > 0 ? _chainRules.MaxChainLength : 6;
            while (_recentCastElements.Count > max)
                _recentCastElements.Dequeue();

            _lastChainStep = _chainDirector.RegisterCast(element, worldMs);

            if (_lastChainStep.FinisherTriggered)
            {
                float fin = _chainRules.FinisherMult;
                _pendingChainBonus = fin > 0f ? fin : 1f;
            }
            else if (_lastChainStep.Matched)
            {
                float link = _lastChainStep.LinkBonus;
                _pendingChainBonus = link > 0f ? link : 1f;
            }

            return bonusForThis;
        }

        void AnnounceChainFinisherIfAny()
        {
            if (!_lastChainStep.FinisherTriggered)
                return;

            string finisher = _lastChainStep.Finisher;
            if (string.IsNullOrEmpty(finisher))
                return;

            _lastFinisherAnnounced = finisher;
            string element = _lastChainStep.Chain != null
                ? _lastChainStep.Chain.Value.Element
                : string.Empty;
            Color tint = Color.cyan;
            if (_colors != null && _lastChainStep.Chain != null)
            {
                // pattern ilk digit = çapa elementi (1..6)
                int dot = FirstPatternDigit(_lastChainStep.Chain.Value.Pattern);
                if (dot >= 1 && dot <= 6)
                    tint = _colors.ColorForRune((Rune)dot);
            }

            _readout?.NoteSkill(finisher, string.IsNullOrEmpty(element) ? "zincir" : element + " zincir", tint);
            _debugHud?.NoteSkillBang(finisher, "finisher");
        }

        static int FirstPatternDigit(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return 0;
            string[] tokens = pattern.Split('-');
            if (tokens.Length == 0)
                return 0;
            return int.TryParse(tokens[0].Trim(), out int d) ? d : 0;
        }

        static ChainRules LoadChainRulesOrDefault()
        {
            const string resourcePath = "ElementSystem/element-sistemi";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
            {
                try
                {
                    return ChainRules.FromJsonRoot(MiniJson.Parse(asset.text));
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"ChainRules JSON okunamadı: {e.Message}");
                }
            }

            return ChainRules.DefaultFromSpec;
        }

        void TickStateBridge(double worldMs)
        {
            if (_stateBoard == null || _combat == null)
                return;

            var motion = _combat.SkillMotion;
            motion.ArenaHalfSizeM = _colors != null ? _colors.ArenaHalfSizeM : motion.ArenaHalfSizeM;
            _stateBoard.Tick(worldMs, motion);
            _bridgeView?.Sync();

            if (_player == null || _motionDriver == null || _motionDriver.IsDisplacing)
                return;
            if (_playerStatus != null && _playerStatus.Board.BlocksMovement)
                return;

            Vector3 p = _player.position;
            if (_stateBoard.TryTraverse(p.x, p.z, worldMs, motion, out float dx, out float dz))
            {
                _motionDriver.WarpInstant(dx, dz);
                _readout?.NoteSkill("Portal", "köprü geçişi", new Color(0.55f, 0.4f, 1f));
            }
        }

        void TickBossDeath()
        {
            if (!_deathPending || _clock == null)
                return;

            if (_clock.Director.WorldTimeMs < _deathReviveAtMs)
                return;

            _deathPending = false;
            _bossVitals?.Revive();
            _boss?.EndCollapse();
            _bossDirector?.NotifyBossRevived(_clock.Director.WorldTimeMs);
        }

        void SyncFromSentence(double worldMs)
        {
            var state = _engine.State;
            if (state.Phase != SentencePhase.Building)
                return;

            int count = state.Words.Count;
            if (count == 0)
                return;

            // Kimliğe göre karar: elde yaşayan (Building'e ait) etki yoksa spawn et; varsa
            // sadece SetWords çağır. Sayı polling'i (count==1) EnhancedTouch'ın bir karede
            // birden fazla nokta kaydettiği durumda 0→2 sıçrayıp spawn'ı hiç tetiklemeyebilir.
            if (_buildingView == null || _buildingView.Logic == null
                || _buildingView.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                _buildingView = SpawnEffect(state.Words, worldMs);
                PulseActor(state.Words[0].Rune, state.Words, worldMs);
                _lastWordCount = count;
                return;
            }

            _buildingView.Logic.SetWords(state.Words);
            ApplySkillWorldPlan(_buildingView.Logic, state.Words);
            ApplySkillTint(_buildingView, state.Words);
            RefreshBuildingMobility(state.Words);
            if (count > _lastWordCount)
                PulseActor(state.Words[count - 1].Rune, state.Words, worldMs);

            _lastWordCount = count;
        }

        void PulseActor(Rune rune, IReadOnlyList<SentenceWord> words, double worldMs)
        {
            FaceBoss();
            _pose?.PulseRune(rune, worldMs);
            if (_visual == null)
                return;

            EffectSilhouette s;
            if (_skills != null && words != null && words.Count > 0)
            {
                SkillResolution skill = _skills.ResolveWords(words);
                s = skill.IsEmpty
                    ? SilhouetteBuilder.FromWords(words, _combat?.Manifestation)
                    : SilhouetteBuilder.FromSkill(skill, _combat?.Manifestation);
            }
            else
            {
                s = words != null && words.Count > 0
                    ? SilhouetteBuilder.FromWords(words, _combat?.Manifestation)
                    : default;
            }
            _visual.PulseRune(rune, s);
        }

        void FaceBoss()
        {
            if (_player == null || _boss == null)
                return;
            Vector3 to = _boss.transform.position - _player.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f)
                return;
            _player.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void ApplyWindowCue()
        {
            if (_buildingView == null)
                return;

            var state = _engine.State;
            if (state.Phase != SentencePhase.Building || state.ArmedWindowMs <= 0.5)
            {
                _buildingView.SetWindowCue(1f);
                return;
            }

            _buildingView.SetWindowCue((float)(state.RemainingWindowMs / state.ArmedWindowMs));
        }

        LivingEffectView SpawnEffect(IReadOnlyList<SentenceWord> words, double worldMs, bool basicStrike = false)
        {
            _ = worldMs;
            Vector3 pos = _player.position;
            Vector3 facing = ResolveAimFacing(pos);

            ManifestationTuning man = _combat.Manifestation;
            if (basicStrike)
                man = man.WithBasicStrikeProfile();

            var logic = new LivingEffect(
                words[0].Rune,
                pos.x,
                pos.z,
                facing.x,
                facing.z,
                words,
                man);

            var go = new GameObject(basicStrike ? "LivingEffect_BasicStrike" : "LivingEffect_" + words[0].Rune);
            go.transform.SetParent(transform, false);
            var view = go.AddComponent<LivingEffectView>();
            view.Bind(logic, man, _colors, basicStrike);
            if (!basicStrike)
                ApplySkillWorldPlan(logic, words);
            ApplySkillTint(view, words);
            _active.Add(view);
            return view;
        }

        /// <summary>
        /// SkillMotor.Resolve + prezentasyon → LivingEffect seyahat/silüet/bang.
        /// </summary>
        void ApplySkillWorldPlan(LivingEffect logic, IReadOnlyList<SentenceWord> words)
        {
            if (logic == null || words == null || words.Count == 0 || _skills == null)
                return;

            SkillResolution skill = _skills.ResolveWords(words);
            if (skill.IsEmpty)
                return;

            EnsurePresentationCatalog();
            ManifestationTuning man = _combat != null ? _combat.Manifestation : new ManifestationTuning();
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, _presentationCatalog, man);
            logic.ApplyPlan(plan);
        }

        /// <summary>
        /// Yüz / hız / kamera forward; boss yalnız SoftAimRangeM içindeyse soft-lock.
        /// </summary>
        Vector3 ResolveAimFacing(Vector3 pos)
        {
            Vector3 facing = _player != null ? _player.forward : Vector3.forward;
            facing.y = 0f;
            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.05f)
                facing = _motor.Velocity.normalized;
            else if (_camera != null)
            {
                float yaw = _camera.OrbitYawDeg;
                facing = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            }

            if (facing.sqrMagnitude < 0.0001f)
                facing = Vector3.forward;
            else
                facing.Normalize();

            float range = _colors != null ? _colors.SoftAimRangeM : 8f;
            if (_boss != null && range > 0.1f)
            {
                Vector3 toBoss = _boss.transform.position - pos;
                toBoss.y = 0f;
                float dist = toBoss.magnitude;
                if (dist > 0.01f && dist <= range)
                    facing = toBoss / dist;
            }

            return facing;
        }

        void ApplySkillTint(LivingEffectView view, IReadOnlyList<SentenceWord> words)
        {
            if (view == null || words == null || words.Count == 0)
                return;

            SkillFeel.ElementPalette(words, _colors, out Color line, out Color blob);
            view.SetSkillTint(line, blob);
        }

        void OnSentenceCompleted(CompletedSentence sentence)
        {
            LivingEffectView view = _buildingView;
            _buildingView = null;
            _lastWordCount = 0;

            if (sentence.Phase == SentencePhase.Aborted || !sentence.Closing.HasValue)
            {
                if (view != null && view.Logic != null)
                    view.Logic.Abort();
                return;
            }

            // Düz vuruş (T6.2) OnDotTouched + Commit'i AYNI karede çağırır: Director'ın Update'i
            // araya girmediği için Building fazı hiç görülmez ve spawn yutulur. Ödenmiş kapanış
            // dünyada mutlaka yaşamak zorunda (§8/T2), o yüzden burada doğuyor. Elde yaşayan bir
            // etki ARAMIYORUZ — önceki cümlenin hâlâ patlayan etkisine bu kapanışı bağlamak
            // yanlış hedefe ödeme yapmak olur.
            // T14: Building hiç görülmeden spawn + tek kelime = düz vuruş YALNIZCA
            // merkezin BasicStrikeDot fiiliyse. Tek Su/Hava vb. skill cümlesi jab sayılmaz.
            bool spawnedForBasicStrike = false;
            if (view == null || view.Logic == null
                || view.Logic.Phase is LivingEffectPhase.Dead or LivingEffectPhase.Fading)
            {
                int basicDot = _colors != null ? _colors.BasicStrikeDot : 1;
                spawnedForBasicStrike = sentence.Words.Count == 1
                    && (int)sentence.Words[0].Rune == basicDot;
                view = SpawnEffect(sentence.Words, _clock.Director.WorldTimeMs, spawnedForBasicStrike);
                if (spawnedForBasicStrike)
                {
                    FaceBoss();
                    _visual?.PulseBasicStrike();
                    _pose?.PulseRune(sentence.Words[0].Rune, _clock.Director.WorldTimeMs);
                }
                else
                {
                    PulseActor(sentence.Words[0].Rune, sentence.Words, _clock.Director.WorldTimeMs);
                }
            }

            // Kapanış kurulmadan önce son kelime listesi etkiye iletilir — dördüncü kelime
            // (cümlenin en pahalı sıfatı) burada gelmezse hedef silüete hiç işlemez, çünkü
            // SentenceEngine dördüncü noktada cümleyi dokunuş anında çözer ve SyncFromSentence
            // artık Building fazında değilken çalışmaz. LivingEffect.SetWords AwaitingClosing
            // fazında da kabul eder, sıra önemli değil.
            view.Logic.SetWords(sentence.Words);
            if (!spawnedForBasicStrike)
                ApplySkillWorldPlan(view.Logic, sentence.Words);

            ClosingHit closing = sentence.Closing.Value;
            view.Logic.ArmClosing(closing);

            float recoverySec = _combat.Sentence.StepForDots(closing.DotCount).RecoverySec;
            float castMult = 1f;
            SkillResolution armedSkill = SkillResolution.Empty;
            if (!spawnedForBasicStrike && _skills != null)
            {
                armedSkill = _skills.ResolveWords(sentence.Words);
                castMult = SkillMobility.CastTimeMult(armedSkill);
            }
            recoverySec *= castMult;

            double bangAt = _clock.Director.WorldTimeMs
                            + recoverySec * 1000.0
                            + _combat.Feel.PostHitSilenceMs;

            _pose?.BeginRecovery(recoverySec, _clock.Director.WorldTimeMs);
            _posedForRecovery = true;

            if (!spawnedForBasicStrike && !armedSkill.IsEmpty)
            {
                float lockSec = recoverySec + _combat.Feel.PostHitSilenceMs / 1000f;
                ApplyCastMobility(armedSkill, lockSec);
            }

            // Merkez düz vuruş: IsBasicStrike yalnızca BasicStrikeDot ile spawn edilen view.
            bool basic = view != null && view.IsBasicStrike;
            _pending.Add(new PendingClosing
            {
                View = view,
                Closing = closing,
                BangAtWorldMs = bangAt,
                Words = new List<SentenceWord>(sentence.Words),
                IsBasicStrike = basic
            });
        }

        /// <summary>Editör/prob: kapanış bang zamanını zorla işle (heal vb.).</summary>
        public void ForceTickClosings()
        {
            if (_clock == null)
                return;
            TickPendingClosings(_clock.Director.WorldTimeMs);
        }

        /// <summary>MCP: bekleyen kapanışları hemen ateşle (bang'i şimdiye çeker).</summary>
        public void ForceFirePendingClosings()
        {
            if (_clock == null)
                return;
            double now = _clock.Director.WorldTimeMs;
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingClosing p = _pending[i];
                p.BangAtWorldMs = now;
                _pending[i] = p;
            }
            TickPendingClosings(now);
        }

        /// <summary>MCP: vadesi gelen time_layer alanlarını (echo) şimdi işle.</summary>
        public void ForceTickTimeEffects()
        {
            if (_clock == null)
                return;
            TickTimeEffects(_clock.Director.WorldTimeMs);
        }

        void TickPendingClosings(double worldMs)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingClosing p = _pending[i];
                if (p.View == null || p.View.Logic == null)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                if (p.View.Logic.Phase is LivingEffectPhase.Fading or LivingEffectPhase.Dead)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                if (worldMs < p.BangAtWorldMs)
                    continue;

                FireClosing(p);
                _pending.RemoveAt(i);
            }
        }

        void FireClosing(PendingClosing p)
        {
            LivingEffect logic = p.View.Logic;
            logic.FireClosingBang();
            StampScar(p.View, p.Closing);

            // Düz vuruş: jab — skill motoru / mana / CD / zincir / pasif / ulti yok.
            // BasicStrikeDot gramer fiili (varsayılan Ateş) skill cast sayılmaz.
            bool basic = p.IsBasicStrike || (p.View != null && p.View.IsBasicStrike);
            if (basic)
            {
                _closingChainBonus = 1f; // pending zincir bonusunu yeme
                _lastChainStep = ChainStepResult.None;

                // Heal vb. tek-rün skill asla IsBasicStrike olmamalı; yanlış BasicStrikeDot
                // mend'e kilitliyse mend kaçmasın (mana/CD yine yok — jab).
                SkillResolution basicSkill = ResolvePendingSkill(p);
                if (IsHealSkill(basicSkill))
                {
                    ApplyClosingStatuses(p, basicSkill);
                    ShoutSkill(basicSkill, p.Words);
                    ApplyClosingHeal(p.Closing, basicSkill);
                    return;
                }

                ApplyBossClosingBasic(logic, p.Closing);
                float basicDealt = ApplyClosingDamage(p.Closing, SkillResolution.Empty, isBasicStrike: true, slashCommitMult: 0f);
                TryScheduleEchoForSkill(SkillResolution.Empty, basicDealt);
                SpawnClosingImpact(p);
                return;
            }

            _closingChainBonus = BeginChainClosing(p.Words, _clock.Director.WorldTimeMs);
            TryActivateMode(p.Words, _clock.Director.WorldTimeMs);
            TryTriggerPassive(p.Words, _clock.Director.WorldTimeMs);

            SkillResolution skill = ResolvePendingSkill(p);
            ApplyResourceCost(skill);
            SkillMotionPlan motionPlan = ResolveSkillMotion(skill);
            ApplySkillMotion(motionPlan, skill);
            ApplyBossClosing(logic, p.Closing, skill);
            float dealt = ApplyClosingDamage(p.Closing, skill, isBasicStrike: false, motionPlan.SlashCommitMult);
            TryScheduleEchoForSkill(skill, dealt);
            ApplyClosingStatuses(p, skill);
            ShoutSkill(skill, p.Words);
            ApplyClosingHeal(p.Closing, skill); // readout ShoutSkill'den sonra (ally +N kalsın)
            TrySpawnZoneForSkill(skill);
            TryExtendZonesForSkill(skill);
            TryApplyRealityForSkill(skill);
            ApplyCooldown(skill, p.Words, cosmeticIfDisabled: true);
            if (!motionPlan.IsEmpty)
                AnnotateMotion(skill, motionPlan);
            AnnounceChainFinisherIfAny(); // skill bang'ten sonra Finisher üstte kalsın
            SpawnClosingImpact(p);
        }

        void SpawnClosingImpact(PendingClosing p)
        {
            if (p.View == null || p.View.Logic == null)
                return;

            LivingEffect logic = p.View.Logic;
            Vector3 tip = new Vector3(logic.TipX, 0.6f, logic.TipZ);
            Vector3 origin = new Vector3(logic.OriginX, 0.55f, logic.OriginZ);
            string element = p.Words != null && p.Words.Count > 0
                ? p.Words[0].Rune.ToString()
                : "Ates";

            string impactStyle = "burst_soft";
            string trailStyle = string.Empty;
            if (!p.IsBasicStrike && _skills != null && p.Words != null)
            {
                SkillResolution skill = _skills.ResolveWords(p.Words);
                EnsurePresentationCatalog();
                if (_presentationCatalog != null && !skill.IsEmpty)
                {
                    LivingEffectPlan plan = SkillWorldPlanner.Build(
                        skill, _presentationCatalog,
                        _combat != null ? _combat.Manifestation : new ManifestationTuning());
                    if (!string.IsNullOrEmpty(plan.TrajectoryId)
                        && _presentationCatalog.TryGetTrajectory(plan.TrajectoryId, out TrajectoryNode traj))
                    {
                        trailStyle = traj.GetString("vfx_trail_type", string.Empty);
                        if (plan.TravelKind == LivingTravelKind.ExpandingRadial
                            || plan.TrajectoryId is "expanding_wave" or "radial_burst")
                            impactStyle = "pulse";
                        else if (plan.TrajectoryId is "raycast" or "instant_hit")
                            impactStyle = "pierce_hit";
                    }
                }
            }

            if (!string.IsNullOrEmpty(trailStyle))
            {
                GameObject trail = PlaceholderFactory.CreateTrail(trailStyle, element, origin, tip, transform);
                if (trail != null)
                    Destroy(trail, 1.0f);
            }

            GameObject fx = PlaceholderFactory.CreateImpact(impactStyle, element, tip, transform);
            if (fx != null)
                Destroy(fx, 1.2f);
        }

        /// <summary>
        /// Bağlama 2: base_resource_cost düşer; yetersiz mana cast'i engellemez (0'a kilit).
        /// </summary>
        void ApplyResourceCost(SkillResolution skill)
        {
            if (_playerResource == null || skill.IsEmpty)
                return;
            float cost = SkillMobility.ResourceCost(skill);
            if (cost <= 0f)
                return;
            _playerResource.Consume(cost);
        }

        /// <summary>
        /// length.mobility × verb.cast_mobility → oyuncu Slow/Root (KinematicMotor okur).
        /// </summary>
        void ApplyCastMobility(SkillResolution skill, float durationSec)
        {
            if (_playerStatus == null || skill.IsEmpty || durationSec <= 0f)
                return;

            string mob = SkillMobility.Resolve(skill);
            double ms = durationSec * 1000.0;
            StatusTuning st = _combat != null ? _combat.Status : new StatusTuning();

            if (mob == SkillMobility.Rooted)
                _playerStatus.Board.Apply(StatusKind.Root, ms, 1f);
            else if (mob == SkillMobility.SlowedMove)
                _playerStatus.Board.Apply(StatusKind.Slow, ms, st.SlowSpeedMult);
        }

        /// <summary>Building sırasında length≥3 mobiliteyi kısa yenile (cümlenin riski).</summary>
        void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words)
        {
            if (words == null || words.Count < 3 || _skills == null)
                return;
            SkillResolution skill = _skills.ResolveWords(words);
            if (skill.IsEmpty)
                return;
            ApplyCastMobility(skill, 0.45f);
        }

        /// <summary>
        /// EnforceCooldown=false: cosmeticIfDisabled ise Görev 12 kozmetik radial (birebir).
        /// true: CooldownTracker + radial gerçek kalan süre (basic dahil).
        /// </summary>
        void ApplyCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words, bool cosmeticIfDisabled)
        {
            if (skill.IsEmpty || words == null || words.Count == 0)
                return;

            bool enforce = _combat != null && _combat.EnforceCooldown;
            if (!enforce)
            {
                if (cosmeticIfDisabled)
                    PulseCosmeticCooldown(skill, words);
                return;
            }

            if (_playerCooldown == null || string.IsNullOrEmpty(skill.VerbId))
                return;

            float sec = skill.BaseCooldownSec;
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            if (!_playerCooldown.TryBeginCast(skill.VerbId, sec, worldMs))
                return;

            if (_pentagonView == null || sec <= 0f)
                return;

            _pentagonView.BeginTrackedCooldown(
                (int)words[0].Rune,
                skill.VerbId,
                sec,
                _playerCooldown,
                _clock);
        }

        /// <summary>
        /// ui_rules.cooldown_display — yalnızca görsel (EnforceCooldown=false).
        /// Fiil rünü (ilk kelime) etrafında base_cooldown_sec kadar radial dolum.
        /// </summary>
        void PulseCosmeticCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (_pentagonView == null || skill.IsEmpty || words == null || words.Count == 0)
                return;
            float sec = skill.BaseCooldownSec;
            if (sec <= 0f)
                return;
            _pentagonView.BeginCosmeticCooldown((int)words[0].Rune, sec);
        }

        SkillMotionPlan ResolveSkillMotion(SkillResolution skill)
        {
            if (skill.IsEmpty || _combat == null || _player == null)
                return SkillMotionPlan.None;

            var t = _combat.SkillMotion;
            t.ArenaHalfSizeM = _colors != null ? _colors.ArenaHalfSizeM : t.ArenaHalfSizeM;

            Vector3 face = _player.forward;
            face.y = 0f;
            if (face.sqrMagnitude < 0.0001f)
                face = Vector3.forward;

            Vector3 bossPos = _boss != null ? _boss.transform.position : Vector3.zero;
            bool bossAlive = _bossVitals == null || !_bossVitals.IsDown;

            var ctx = new SkillMotionContext(
                _player.position.x, _player.position.z,
                face.x, face.z,
                bossPos.x, bossPos.z,
                bossAlive,
                t.ArenaHalfSizeM);

            return SkillMotionMotor.Resolve(skill, ctx, t);
        }

        void ApplySkillMotion(in SkillMotionPlan plan, SkillResolution skill)
        {
            if (plan.IsEmpty || _clock == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            if (plan.Kind == SkillMotionKind.PlaceMark)
            {
                _stateBoard?.PlaceMark(plan.MarkType, plan.DestX, plan.DestZ, worldMs, _combat.SkillMotion);
                _bridgeView?.Sync();
                return;
            }

            _motionDriver?.Play(plan, worldMs);
        }

        void AnnotateMotion(SkillResolution skill, in SkillMotionPlan plan)
        {
            string tag = plan.Kind switch
            {
                SkillMotionKind.ZenitsuPass => "Zenitsu geçiş",
                SkillMotionKind.ShortBlink => "ışınlanma",
                SkillMotionKind.ForwardDash => "dash",
                SkillMotionKind.PlaceMark => "işaret",
                _ => null
            };
            if (tag == null) return;
            _debugHud?.NoteSkillBang(skill.DisplayName, tag);
        }

        SkillResolution ResolvePendingSkill(PendingClosing p)
        {
            if (_skills == null || p.Words == null || p.Words.Count == 0)
                return SkillResolution.Empty;
            return _skills.ResolveWords(p.Words);
        }

        void ShoutSkill(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (skill.IsEmpty)
                return;

            string mech = SkillFeel.MechanicShort(skill.Mechanics);
            string adj = SkillFeel.AdjectiveShort(skill);
            SkillFeel.ElementPalette(words, _colors, out Color line, out _);
            string bangNote = string.IsNullOrEmpty(adj)
                ? mech
                : (string.IsNullOrEmpty(mech) ? adj : mech + " | " + adj);
            _debugHud?.NoteSkillBang(skill.DisplayName, bangNote);
            string sub = skill.VerbName;
            if (!string.IsNullOrEmpty(mech))
                sub = string.IsNullOrEmpty(sub) ? mech : sub + "  ·  " + mech;
            if (!string.IsNullOrEmpty(adj))
                sub = string.IsNullOrEmpty(sub) ? adj : sub + "  ·  " + adj;
            _readout?.NoteSkill(skill.DisplayName, sub, line);
            SkillFeel.CameraKick(skill.VerbFamily, _camera, _colors);
            // PulseRune (PulseActor) kalır — AnimationBridge eklenir, yerine geçmez.
            ApplySkillAnimation(skill);
        }

        /// <summary>
        /// Bağlama 10: SkillResolution.AnimationType → PresentationCatalog → AnimationBridge.
        /// Quaternius'ta karşılığı yoksa SafeSetFloat gibi sessiz atlar (hata yok).
        /// </summary>
        void ApplySkillAnimation(SkillResolution skill)
        {
            LastAnimationTypeId = string.Empty;
            LastAnimationState = string.Empty;
            LastAnimationPlayApplied = false;

            if (skill.IsEmpty || _visual == null || _visual.Animator == null)
                return;

            EnsurePresentationCatalog();
            if (_presentationValidator == null || _presentationCatalog == null)
                return;

            PresentationValidationResult check = _presentationValidator.Validate(skill);
            LastAnimationTypeId = check.AnimationTypeId;
            if (!check.AnimationFound)
                return;

            if (!_presentationCatalog.TryGetAnimation(check.AnimationTypeId, out AnimationFrameNode node))
                return;

            LastAnimationState = AnimationBridge.MapToQuaterniusState(node.AnimatorState);
            double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
            LastAnimationPlayApplied = _animationBridge.Play(node, _visual.Animator, worldMs);
        }

        void EnsurePresentationCatalog()
        {
            if (_presentationCatalog != null && _presentationValidator != null)
                return;

            const string resourcePath = "Presentation/prezentasyon-katmani";
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return;

            try
            {
                _presentationCatalog = PresentationCatalog.FromJson(asset.text);
                _presentationValidator = new PresentationValidator(_presentationCatalog);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[ManifestationDirector] prezentasyon-katmani okunamadı: {e.Message}");
            }
        }

        void ApplyClosingStatuses(PendingClosing p, SkillResolution skill)
        {
            if (skill.IsEmpty)
                return;
            if (_playerStatus == null && _bossStatus == null)
                return;

            var result = StatusApplicator.ApplySkill(
                skill,
                _playerStatus != null ? _playerStatus.Board : null,
                _bossStatus != null ? _bossStatus.Board : null,
                _combat != null ? _combat.Status : new StatusTuning());

            if (result.Knockback && _bossStatus != null && _player != null)
                _bossStatus.ApplyKnockbackFrom(_player.position);

            // 16 Eylül: "skilleri attığımda bir etkileşim göremiyorum" raporu — durum
            // etkileşim tablosu (docs/element-sistemi.json status_interaction_table) mekanik
            // olarak zaten çalışıyordu, hiçbir görsel sinyali yoktu. Tetiklenen kural varsa
            // aynı tepki yazısı kanalını kullan (§10: kırmızı-turuncu yasak → AcidGreen).
            if (result.TriggeredReactions.Count > 0 && _readout != null)
            {
                StatusReactionRule rule = result.TriggeredReactions[0];
                _readout.NoteSkill(rule.Name, rule.ReadAs, _colors.AcidGreen);
                _debugHud?.NoteSkillBang(rule.Name, rule.ReadAs);
            }
        }

        /// <summary>
        /// Mend / heal / regen — daha boş olana basar (oran). Ally full ise oyuncu.
        /// Miktar: TotalEffect × ClosingDamagePerEffect (commit ile aynı birim).
        /// </summary>
        void ApplyClosingHeal(ClosingHit closing, SkillResolution skill)
        {
            if (skill.IsEmpty)
                return;
            if (!IsHealSkill(skill))
                return;

            float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
            // 16 Eylül: "Kavurucu Yara" (grievous_wounds+burn) — yanık hedefe gelen heal azalır.
            // Hedefin StatusBoard'u yoksa (ör. AllyDummy) çarpan 1f, davranış eskisiyle aynı.
            float healMult = _playerStatus != null ? _playerStatus.Board.HealEffectivenessMult : 1f;
            healMult *= _passiveDirector?.HealMult ?? 1f;
            healMult *= _closingChainBonus; // Bağlama 6: önceki link/finisher → bu kapanış
            int amount = Mathf.Max(1, Mathf.RoundToInt(closing.TotalEffect * per * healMult));
            if (amount <= 0)
                return;

            var playerVitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            bool allyNeeds = _ally != null && _ally.Hp < _ally.MaxHp;
            bool selfNeeds = playerVitals != null && !playerVitals.IsDown && playerVitals.Hp < playerVitals.MaxHp;
            if (!allyNeeds && !selfNeeds)
            {
                _readout?.NoteSkill(skill.DisplayName, "zaten full", new Color(0.7f, 0.9f, 0.75f));
                return;
            }

            bool healAlly = false;
            if (allyNeeds && selfNeeds)
            {
                float allyR = _ally.Ratio;
                float selfR = (float)playerVitals.Hp / playerVitals.MaxHp;
                // Eşitse kendine — "kendime heal" denemesi.
                healAlly = allyR < selfR;
            }
            else
                healAlly = allyNeeds;

            int healed;
            if (healAlly)
            {
                healed = _ally.ApplyHeal(amount);
                if (healed > 0)
                {
                    _damageHud?.ShowDamage(-healed);
                    _readout?.NoteSkill(skill.DisplayName, "ally +" + healed, new Color(0.4f, 1f, 0.65f));
                    _debugHud?.NoteSkillBang(skill.DisplayName, "ally +" + healed);
                }
                return;
            }

            healed = playerVitals.ApplyHeal(amount);
            if (healed > 0)
            {
                _modeDirector?.NotifyHealed(); // "healer iyileştirirse biter" (Kan Çılgınlığı)
                _damageHud?.ShowDamage(-healed);
                _readout?.NoteSkill(skill.DisplayName, "self +" + healed, new Color(0.4f, 1f, 0.65f));
                _debugHud?.NoteSkillBang(skill.DisplayName, "self +" + healed);
            }
        }

        static bool IsHealSkill(SkillResolution skill)
        {
            if (string.Equals(skill.VerbFamily, "mend", System.StringComparison.Ordinal))
                return true;
            string action = skill.Action ?? string.Empty;
            return action is "heal" or "regen" or "cleanse" or "area_cleanse" or "holy_shield";
        }

        /// <summary>
        /// Ekipman eşleşmesi: silah çekirdek elementi (Ateş/Su/…) ile skill'in fiil rünü.
        /// ElementId "1" / "1-1" / "1-2+3" → ilk sayı CoreName; yoksa ElementOrigin.
        /// </summary>
        string SkillElementForEquipment(SkillResolution skill)
        {
            int core = ParseFirstCoreId(skill.ElementId);
            if (core >= 1 && core <= 6 && _skills != null)
                return _skills.CoreName(core);
            return skill.ElementOrigin ?? string.Empty;
        }

        static int ParseFirstCoreId(string elementId)
        {
            if (string.IsNullOrEmpty(elementId))
                return 0;
            int i = 0;
            while (i < elementId.Length && !char.IsDigit(elementId[i])) i++;
            int start = i;
            while (i < elementId.Length && char.IsDigit(elementId[i])) i++;
            if (i == start)
                return 0;
            return int.Parse(elementId.Substring(start, i - start), System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Commit (§5 TotalEffect × ClosingDamagePerEffect) × skill fiil ölçeği.
        /// Heal/dash BaseDamage=0 → 0 can; status ayrı. Tür hasarı değiştirmez (§12).
        /// UseFormulaDamage=true → DamageCalculator (resistance/weakness nötr 0/1).
        /// Dönüş: boss'a uygulanan hasar (0 = yok); Bağlama 8 echo kaynağı.
        /// </summary>
        float ApplyClosingDamage(ClosingHit closing, SkillResolution skill, bool isBasicStrike, float slashCommitMult)
        {
            if (_bossVitals == null || _bossVitals.IsDown)
                return 0f;

            float outMult = 1f;
            if (_playerStatus != null)
                outMult = _playerStatus.Board.OutgoingDamageMult;
            outMult *= _modeDirector?.DamageMult ?? 1f; // ulti: Öfke Patlaması ×1.8, Kan Çılgınlığı ×2.0
            outMult *= _passiveDirector?.DamageMult ?? 1f; // pasif: alev_hiddeti ×1.15 × karanlik_sessizligi ×1.2 …
            outMult *= _closingChainBonus; // Bağlama 6: links/finisher_mult → sonraki (bu) kapanış
            // Bağlama 9: silah elementi ↔ skill'in fiil çekirdeği (ElementId ilk rün;
            // bileşik "Alev"/1-1 → Ateş). ElementOrigin bileşik adı olabilir.
            float eqMult = 1f;
            if (_equipmentBonus != null && !isBasicStrike && !skill.IsEmpty)
                eqMult = _equipmentBonus.Resolve(_equippedWeapon, SkillElementForEquipment(skill));
            outMult *= eqMult;
            LastEquipmentMatchMult = eqMult;

            bool isCrit = false;
            float damage;
            if (_combat != null && _combat.UseFormulaDamage &&
                !isBasicStrike && !skill.IsEmpty && skill.BaseDamage > 0f)
            {
                // length.damage_mult JSON'da 1.0 (anti-ladder); SkillResolution taşımıyor.
                DamageHit hit = EnsureDamageCalculator().Compute(
                    in skill,
                    lengthDamageMult: 1f,
                    resistance: 0f,
                    weaknessBonus: 1f);
                damage = hit.Amount * outMult;
                isCrit = hit.WasCrit;
            }
            else
            {
                damage = ClosingDamageMath.Compute(
                    closing.TotalEffect,
                    _combat != null ? _combat.ClosingDamagePerEffect : 1f,
                    skill,
                    isBasicStrike,
                    outMult);
            }

            // Teleport fiili BaseDamage=0; Zenitsu kesisi commit × SlashCommitMult.
            if (damage <= 0f && slashCommitMult > 0f && closing.TotalEffect > 0f)
            {
                float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
                damage = closing.TotalEffect * per * slashCommitMult * outMult;
            }

            if (damage <= 0f)
            {
                LastClosingDamageDealt = 0f;
                return 0f;
            }

            // Armor break boss'ta incoming mult
            if (_bossStatus != null)
                damage *= _bossStatus.Board.IncomingDamageMult;

            LastClosingDamageDealt = damage;
            _damageHud?.ShowDamage(damage, isCrit);
            _lastDamageDealtMs = _clock.Director.WorldTimeMs; // "dealt_damage_recently" (Öfke Patlaması)

            float lifesteal = (_modeDirector?.Lifesteal ?? 0f) + (_passiveDirector?.LifestealAdd ?? 0f);
            if (lifesteal > 0f)
            {
                var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
                int healAmt = Mathf.RoundToInt(damage * lifesteal);
                if (vitals != null && healAmt > 0)
                    vitals.ApplyHeal(healAmt); // Kan Çılgınlığı kendi hasarından beslenir — NotifyHealed BİLEREK çağrılmaz
            }

            bool killed = _bossVitals.ApplyDamage(damage);
            var bossVisual = _boss != null ? _boss.GetComponent<BossVisual>() : null;
            if (killed)
            {
                double worldMs = _clock.Director.WorldTimeMs;
                // Önce telegrafı kapat (Hide taban ölçeğe çeker), sonra çökme — sıra tersine
                // dönseydi Hide çökmeyi ezerdi.
                _bossDirector?.NotifyBossDown(worldMs);
                float collapseSec = _colors != null ? _colors.BossDeathCollapseSec : 0.85f;
                _boss?.BeginCollapse(collapseSec, worldMs);
                // Yavaş çekim kaldırıldı — revive çökme süresi kadar dünya saati sonra.
                _deathReviveAtMs = worldMs + collapseSec * 1000.0;
                _deathPending = true;
                return damage;
            }

            bossVisual?.PlayStagger();
            return damage;
        }

        // Kapanış izi (bu metot) ve seyahat izi (TickEffects, view.Scarred) iki ayrı bayrak:
        // biri seyahat çatlağının damgalanıp damgalanmadığını, diğeri kapanışın kendi izini
        // takip eder. Aynı bayrağı paylaşınca odaklı SARSINTI (`5-1`) seyahatte çatlak
        // bıraktığı için kapanış izini hiç bırakmıyordu (T7.1).
        void StampScar(LivingEffectView view, ClosingHit closing)
        {
            LivingEffect logic = view != null ? view.Logic : null;
            if (logic == null || _closingStamped.Contains(logic))
                return;

            Vector3 along = new Vector3(logic.DirX, 0f, logic.DirZ);
            Vector3 tip = new Vector3(logic.TipX, 0f, logic.TipZ);
            float scale = view.IsBasicStrike
                ? _combat.Manifestation.BasicStrikeScarScaleM * (0.7f + 0.15f * closing.DotCount)
                : _combat.Manifestation.ScarScaleM * (0.7f + 0.15f * closing.DotCount);

            ScarKind kind = view.IsBasicStrike
                ? ScarKind.Strike
                : closing.Type switch
                {
                    Rune.Aydinlik => ScarKind.Crack,
                    Rune.Ates => ScarKind.Needle,
                    Rune.Su => ScarKind.Swarm,
                    Rune.Toprak => ScarKind.Acid,
                    _ => ScarKind.Crack
                };

            // Hat boyunca çatlak: kökten uca birkaç damga
            if (kind == ScarKind.Crack && logic.Current.Focus > 0.5f)
            {
                Vector3 origin = new Vector3(logic.OriginX, 0f, logic.OriginZ);
                for (int i = 1; i <= 3; i++)
                {
                    float u = i / 3f;
                    _scars.Stamp(Vector3.Lerp(origin, tip, u), scale * 0.85f, kind, along);
                }
            }
            else
            {
                _scars.Stamp(tip, scale, kind, along);
            }

            _closingStamped.Add(logic);
        }

        /// <summary>Düz vuruş jab — yalnızca kısa sarsıntı; geri itme yok (skill tepkisi değil).</summary>
        void ApplyBossClosingBasic(LivingEffect logic, ClosingHit closing)
        {
            if (_boss == null || (_bossVitals != null && _bossVitals.IsDown))
                return;
            if (!IsClosingInRange(logic, closing))
                return;

            var man = _combat.Manifestation;
            _boss.React(
                new Vector3(logic.OriginX, 0f, logic.OriginZ),
                knockbackM: 0f,
                liftM: 0f,
                shakeSec: man.BossShakeSec * 0.35f,
                _clock.Director.WorldTimeMs);
        }

        void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill)
        {
            if (_boss == null || (_bossVitals != null && _bossVitals.IsDown))
                return;

            // Kendine yönelik fiil (mend/guard/purge) boss gövdesini boğmaz — hafif titreşim yeter.
            if (!skill.IsEmpty && StatusApplicator.IsSelfTargeted(skill))
            {
                if (!IsClosingInRange(logic, closing))
                    return;
                _boss.React(
                    new Vector3(logic.OriginX, 0f, logic.OriginZ),
                    _combat.Manifestation.BossKnockbackM * 0.08f,
                    0.04f,
                    _combat.Manifestation.BossShakeSec * 0.35f,
                    _clock.Director.WorldTimeMs);
                return;
            }

            Vector3 from = new Vector3(logic.OriginX, 0f, logic.OriginZ);
            var man = _combat.Manifestation;
            float knock = man.BossKnockbackM;
            float lift = 0f;
            float shake = man.BossShakeSec;
            double worldMs = _clock.Director.WorldTimeMs;

            // Önce SkillMotor ailesi (iş), yoksa son rün (eski silüet tepkisi).
            string family = skill.IsEmpty ? string.Empty : skill.VerbFamily;
            if (!string.IsNullOrEmpty(family))
            {
                switch (family)
                {
                    case "strike":
                        knock = man.BossKnockbackM * (1.85f + 0.4f * logic.Current.Pierce);
                        lift = 0.05f;
                        shake = man.BossShakeSec * 0.55f;
                        break;
                    case "disrupt":
                        knock = man.BossKnockbackM * 0.12f;
                        lift = 0.08f;
                        shake = man.BossShakeSec * 1.6f;
                        if (!IsClosingInRange(logic, closing))
                            return;
                        _boss.React(from, knock, lift, shake * 0.45f, worldMs);
                        _boss.React(from + new Vector3(logic.DirZ, 0f, -logic.DirX) * 0.35f,
                            knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                        _boss.React(from + new Vector3(-logic.DirZ, 0f, logic.DirX) * 0.35f,
                            knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                        return;
                    case "control":
                        if (!IsClosingInRange(logic, closing))
                            return;
                        _boss.Pin(0.7f, worldMs);
                        return;
                    case "zone":
                        knock = man.BossKnockbackM * 0.55f;
                        lift = man.BossLiftM * (1.15f + 0.35f * logic.Current.Lift);
                        shake = man.BossShakeSec * 0.9f;
                        break;
                    case "motion":
                        knock = man.BossKnockbackM * 0.9f;
                        lift = 0.12f;
                        shake = man.BossShakeSec * 0.7f;
                        break;
                    case "special":
                        knock = man.BossKnockbackM * 0.25f;
                        lift = 0.2f;
                        shake = man.BossShakeSec * 1.1f;
                        break;
                    default:
                        break;
                }

                if (!IsClosingInRange(logic, closing))
                    return;
                _boss.React(from, knock, lift, shake, worldMs);
                return;
            }

            // Tür = fiziksel tepki ekseni. Hasar miktarı burada yok (§5 + §12).
            switch (closing.Type)
            {
                case Rune.Aydinlik:
                    // Havalandırma — spec §5 açıkça yazar.
                    knock = man.BossKnockbackM * 0.55f;
                    lift = man.BossLiftM * (1.15f + 0.35f * logic.Current.Lift);
                    shake = man.BossShakeSec * 0.9f;
                    break;
                case Rune.Ates:
                    // Tek yöne derin geri tepme (§4 daralt/odakla).
                    knock = man.BossKnockbackM * (1.85f + 0.4f * logic.Current.Pierce);
                    lift = 0.05f;
                    shake = man.BossShakeSec * 0.55f;
                    break;
                case Rune.Su:
                    // Yerinde çok noktalı sarsılma, yer değiştirme az (§4 çoğalt/yay).
                    knock = man.BossKnockbackM * 0.12f;
                    lift = 0.08f;
                    shake = man.BossShakeSec * 1.6f;
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _boss.React(from, knock, lift, shake * 0.45f, worldMs);
                    _boss.React(from + new Vector3(logic.DirZ, 0f, -logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    _boss.React(from + new Vector3(-logic.DirZ, 0f, logic.DirX) * 0.35f,
                        knock * 0.6f, lift * 0.5f, shake * 0.55f, worldMs);
                    return;
                case Rune.Hava:
                    // Sabitleme — §5.
                    if (!IsClosingInRange(logic, closing))
                        return;
                    _boss.Pin(0.55f, worldMs);
                    return;
                case Rune.Toprak:
                    // Birikinti izi StampScar'da; gövde hafif sarsılır.
                    knock = man.BossKnockbackM * 0.2f;
                    lift = 0f;
                    shake = man.BossShakeSec * 0.7f;
                    break;
            }

            if (!IsClosingInRange(logic, closing))
                return;

            _boss.React(from, knock, lift, shake, worldMs);
        }

        bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            Vector3 bossPos = _boss.transform.position;
            float dx = bossPos.x - logic.TipX;
            float dz = bossPos.z - logic.TipZ;
            float reach = _combat.Manifestation.ClosingBangRadiusM;
            if (logic != null && logic.BangRadiusM > 0f)
                reach = logic.BangRadiusM;
            if (closing.Type == Rune.Aydinlik)
            {
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.5f))
                {
                    float radial = Vector2.Distance(
                        new Vector2(bossPos.x, bossPos.z),
                        new Vector2(logic.OriginX, logic.OriginZ));
                    if (radial > logic.TipDistance + reach && radial > reach)
                        return false;
                }

                return true;
            }

            if (dx * dx + dz * dz > reach * reach)
            {
                if (!logic.OverlapsBoss(bossPos.x, bossPos.z, reach * 0.35f))
                    return false;
            }

            return true;
        }

        void TickEffects(float dtSec, double worldMs)
        {
            Vector3 bossPos = _boss != null ? _boss.transform.position : Vector3.zero;
            var man = _combat.Manifestation;
            bool bossDown = _bossVitals != null && _bossVitals.IsDown;

            for (int i = _active.Count - 1; i >= 0; i--)
            {
                LivingEffectView view = _active[i];
                if (view == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }

                LivingEffect logic = view.Logic;
                logic.Tick(dtSec);
                view.TickVisual(dtSec);

                if (!bossDown && logic.Phase == LivingEffectPhase.Traveling && _boss != null && !view.TravelHitDone)
                {
                    if (logic.OverlapsBoss(bossPos.x, bossPos.z, man.TravelHitRadiusM))
                    {
                        view.TravelHitDone = true;
                        _boss.React(
                            new Vector3(logic.OriginX, 0f, logic.OriginZ),
                            man.BossKnockbackM * 0.25f,
                            0.08f * logic.Current.Lift,
                            man.BossShakeSec * 0.45f,
                            worldMs);
                    }
                }

                // Seyahat izi: odaklı sarsıntı hattı yerde hafif çatlak bırakır (T4 erken kanıt)
                if (!view.Scarred && logic.Verb == Rune.Aydinlik && logic.Current.Focus > 0.7f
                    && logic.Travel > 2.5f)
                {
                    Vector3 mid = new Vector3(
                        logic.OriginX + logic.DirX * logic.TipDistance * 0.5f,
                        0f,
                        logic.OriginZ + logic.DirZ * logic.TipDistance * 0.5f);
                    _scars.Stamp(mid, man.ScarScaleM * 0.5f, ScarKind.Crack,
                        new Vector3(logic.DirX, 0f, logic.DirZ));
                    view.Scarred = true;
                }

                if (!logic.IsAlive)
                {
                    _closingStamped.Remove(logic);
                    Destroy(view.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
