using System;
using System.Collections.Generic;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
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
    public sealed partial class ManifestationDirector : MonoBehaviour
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

        // Boss ölümü: çökme süresi bitince Revive.
        bool _deathPending;
        double _deathReviveAtMs;

        SkillMotor _skills;
        SkillFactory _skillFactory;
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
        ActiveModeVfx _modeVfx;
        AfterimageTrail _afterimage;
        // --- Pasifler (Bağlama 5) — ulti gibi ama cooldown'suz, birden fazla aynı anda ---
        PassiveDirector _passiveDirector;
        PassiveHud _passiveHud;
        // --- State machine (player_states ↔ SentencePhase / dodge / CC) ---
        PlayerStateMachine _playerStates;
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
        // --- Space link/tear (Karabasan / Hiçlik) ---
        SpaceDirector _spaceDirector;
        SpaceDirectorHost _spaceHost;
        // --- Zaman (Bağlama 8) — echo / extend / delayed_detonation / death_delay ---
        TimeEffectDirector _timeEffectDirector;
        readonly List<TimeEffectField> _dueTimeFields = new();
        bool _deferredBossDeath;
        HexagonInput _input;
        // --- Gerçeklik (Bağlama 11) — revive_block + erase ---
        RealityEffectDirector _realityDirector;
        // --- Ekipman (Bağlama 9) — sabit silah; seçim UI yok ---
        EquipmentItem _equippedWeapon;
        EquipmentBonusResolver _equipmentBonus;
        readonly SkillExecutorRouter _skillExecutorRouter = new();
        readonly List<EquipmentItem> _cycleWeapons = new();
        int _cycleWeaponIndex = -1;
        // --- Animasyon (Bağlama 10) — PresentationCatalog → AnimationBridge; PulseRune kalır ---
        PresentationCatalog _presentationCatalog;
        PresentationValidator _presentationValidator;
        AnimationDatabase _animationDatabase;
        readonly HashSet<string> _missingAnimationBindings = new();
        readonly AnimationBridge _animationBridge = new();
        int _elementPaintIndex;
        HexagonView _hexagonView;
        PlayerResource _playerResource;
        PlayerCooldown _playerCooldown;
        double _lastDamageDealtMs = double.NegativeInfinity;
        double _lastMovedMs = double.NegativeInfinity;
        float _modeHpDrainAccum;

        /// <summary>PrototypeBootstrap'ın atadığı sabit silah (ör. Alev Kılıcı).</summary>
        public EquipmentItem EquippedWeapon => _equippedWeapon;

        public void ConfigureWeaponCycle(IReadOnlyList<EquipmentItem> weapons)
        {
            _cycleWeapons.Clear();
            if (weapons != null)
            {
                for (int i = 0; i < weapons.Count; i++)
                {
                    EquipmentItem weapon = weapons[i];
                    if (weapon != null && weapon.Slot == EquipmentSlot.Weapon)
                        _cycleWeapons.Add(weapon);
                }
            }

            _cycleWeaponIndex = -1;
            for (int i = 0; i < _cycleWeapons.Count; i++)
            {
                if (_equippedWeapon != null
                    && string.Equals(_cycleWeapons[i].Id, _equippedWeapon.Id, StringComparison.Ordinal))
                {
                    _cycleWeaponIndex = i;
                    break;
                }
            }
        }

        public EquipmentItem CycleEquippedWeapon()
        {
            if (_cycleWeapons.Count == 0)
                return _equippedWeapon;

            _cycleWeaponIndex = (_cycleWeaponIndex + 1) % _cycleWeapons.Count;
            _equippedWeapon = _cycleWeapons[_cycleWeaponIndex];
            LastFactorySkill = null;
            _weaponSwap?.ReplaceActive(_equippedWeapon);

            string routeType = SkillExecutorRouter.IsRangedWeapon(_equippedWeapon)
                ? "ranged"
                : "melee";
            string numericId = _equippedWeapon.Id;
            int colon = numericId.LastIndexOf(':');
            if (colon >= 0 && colon + 1 < numericId.Length)
                numericId = numericId.Substring(colon + 1);
            Debug.Log(
                $"[WeaponCycle] id={numericId} name={_equippedWeapon.Name} "
                + $"type={routeType} canonicalType={_equippedWeapon.Type}");
            return _equippedWeapon;
        }

        /// <summary>v6: son kapanışta silah × uyumsuz çizim hasar çarpanı.</summary>
        public float LastEquipmentMatchMult { get; private set; } = 1f;
        public bool LastWeaponCompatible { get; private set; } = true;
        public bool LastWeaponPassiveEnabled { get; private set; } = true;
        public string LastWeaponUiLabel { get; private set; } = string.Empty;
        public string LastResolvedSkillId { get; private set; } = string.Empty;
        public bool LastSkillEffectApplied { get; private set; }
        public Skill LastFactorySkill { get; private set; }
        public SkillExecutorKind LastExecutorKind { get; private set; } = SkillExecutorKind.Fallback;

        /// <summary>Bağlama 9 / MCP: son ApplyClosingDamage çıktısı (boss'a giden, armor öncesi).</summary>
        public float LastClosingDamageDealt { get; private set; }

        /// <summary>Bağlama 10 / MCP: son ShoutSkill AnimationType id (katalog anahtarı).</summary>
        public string LastAnimationTypeId { get; private set; } = string.Empty;

        /// <summary>Bağlama 10 / MCP: son denenen animator_state.</summary>
        public string LastAnimationState { get; private set; } = string.Empty;
        public string LastAnimationClip { get; private set; } = string.Empty;
        public bool LastAnimationUsedFallback { get; private set; }

        /// <summary>Bağlama 10 / MCP: Controller'da state vardı ve Play uygulandı.</summary>
        public bool LastAnimationPlayApplied { get; private set; }

        /// <summary>Bağlama 10 / MCP: frame-timer köprüsü (Play doğrulama).</summary>
        public AnimationBridge AnimationBridge => _animationBridge;
        public ElementPaintNode? SelectedElementPaint =>
            _skills != null
            && _skills.ElementPaints.Count > 0
            && _elementPaintIndex >= 0
            && _elementPaintIndex < _skills.ElementPaints.Count
                ? _skills.ElementPaints[_elementPaintIndex]
                : null;

        /// <summary>Bağlama 10 / MCP: ShoutSkill içindeki ApplySkillAnimation yolunu doğrudan dener.</summary>
        public void DebugApplySkillAnimation(SkillResolution skill) => ApplySkillAnimation(skill);

        public ElementPaintNode? CycleElementPaint()
        {
            if (_skills == null || _skills.ElementPaints.Count == 0)
                return null;
            _elementPaintIndex = (_elementPaintIndex + 1) % _skills.ElementPaints.Count;
            ElementPaintNode paint = _skills.ElementPaints[_elementPaintIndex];
            _readout?.NoteSkill("Element: " + paint.Name, "isim/VFX boya katmanı", Color.cyan);
            Debug.Log($"[ElementSystem] element paint={paint.Id}:{paint.Name} ({paint.Vfx})");
            return paint;
        }

        SkillMotor Skills => _skills ??= SkillMotorLoader.LoadOrDefault();

        WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill)
        {
            if (_equipmentBonus == null || _equippedWeapon == null || skill.IsEmpty)
                return WeaponSkillCompatibility.Neutral;
            return _skillFactory != null
                ? _skillFactory.EvaluateWeapon(skill, _equippedWeapon)
                : WeaponSkillCompatibility.Neutral;
        }

        DamageCalculator EnsureDamageCalculator()
        {
            if (_damageCalculator != null)
                return _damageCalculator;

            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
            {
                try
                {
                    // Play'te crit ara sıra çıksın diye seed sabit değil.
                    _damageCalculator = DamageCalculator.FromElementSystemJson(
                        design.Json,
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

        /// <summary>state_machine.player_states — SentencePhase/dodge/CC ile senkron.</summary>
        public PlayerStateMachine PlayerStates => _playerStates;

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
            HexagonInput input,
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
            HexagonView hexagonView = null,
            PassiveHud passiveHud = null,
            EquipmentItem equippedWeapon = null,
            EquipmentBonusResolver equipmentBonus = null,
            SkillMotor skills = null,
            SkillFactory skillFactory = null,
            AnimationDatabase animationDatabase = null)
        {
            _clock = clock;
            _input = input;
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
            _afterimage = player != null ? player.GetComponent<AfterimageTrail>() : null;
            _modeVfx = FindAnyObjectByType<ActiveModeVfx>();
            if (_modeVfx == null)
            {
                var vfxGo = new GameObject("ActiveModeVfx");
                _modeVfx = vfxGo.AddComponent<ActiveModeVfx>();
            }
            Transform canvasRoot = modeHud != null ? modeHud.transform.parent : null;
            if (canvasRoot == null)
            {
                var canvas = FindAnyObjectByType<Canvas>();
                canvasRoot = canvas != null ? canvas.transform : null;
            }
            _modeVfx.Bind(player, canvasRoot);
            _hexagonView = hexagonView;
            _equippedWeapon = equippedWeapon;
            _equipmentBonus = equipmentBonus;
            _playerResource = player != null ? player.GetComponent<PlayerResource>() : null;
            _playerCooldown = player != null ? player.GetComponent<PlayerCooldown>() : null;
            _skills = skills ?? SkillMotorLoader.LoadOrDefault();
            _skillFactory = skillFactory ?? new SkillFactory(_skills, _equipmentBonus);
            _animationDatabase = animationDatabase ?? LoadAnimationDatabase();
            _elementPaintIndex = 0;
            EnsurePresentationCatalog();
            _playerStates = new PlayerStateMachine(_skills.PlayerStates);
            input.BindPlayerStates(_playerStates, () => _pending.Count > 0);
            var motorForStates = player != null ? player.GetComponent<KinematicMotor>() : null;
            motorForStates?.BindPlayerStates(_playerStates);
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
            var playerVitals = player != null ? player.GetComponent<PlayerVitals>() : null;
            int linkCap = _skills.MaxActiveLinks > 0 ? _skills.MaxActiveLinks : 3;
            SpaceLayerTuning spaceTuning = _combat != null ? _combat.SpaceLayer : new SpaceLayerTuning();
            _spaceDirector = new SpaceDirector(linkCap, spaceTuning);
            _spaceHost = FindAnyObjectByType<SpaceDirectorHost>();
            if (_spaceHost == null)
            {
                var spaceGo = new GameObject("SpaceDirector");
                _spaceHost = spaceGo.AddComponent<SpaceDirectorHost>();
            }
            _spaceHost.Bind(
                _spaceDirector,
                spaceTuning,
                player,
                boss != null ? boss.transform : null,
                bossVitals,
                playerVitals,
                ally);
            if (_playerStatus != null)
                _playerStatus.SpaceLinkBreak = () => _spaceHost?.NotifyOwnerDamaged(SpaceDirectorHost.ActorPlayer);
            int timeCap = _skills.MaxActiveTimeFields > 0
                ? _skills.MaxActiveTimeFields
                : Dovus.Core.Combat.TimeEffectDirector.DefaultMaxActiveFields;
            _timeEffectDirector = new TimeEffectDirector(timeCap);
            _dueTimeFields.Clear();
            _realityDirector = new RealityEffectDirector();
            if (playerVitals != null)
            {
                playerVitals.SetReviveBlockedGate(() =>
                    _realityDirector != null
                    && _clock != null
                    && _realityDirector.IsReviveBlocked(_clock.Director.WorldTimeMs));
            }
            if (_playerStatus != null)
            {
                _playerStatus.ModeDirector = _modeDirector;
                _playerStatus.PassiveDirector = _passiveDirector;
                _playerStatus.ReflectBossVitals = bossVitals;
            }

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

        static AnimationDatabase LoadAnimationDatabase()
        {
            return ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design)
                ? design.Animations
                : null;
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
            SyncDashCooldownMult();
            SyncPlayerStateMachine(worldMs);
            TickWeaponSwap(worldMs);
            TickDelayedLaunches(worldMs);
            TickZones(dtSec);
            _spaceHost?.Tick(dtSec);
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
                ClearModePresentation();
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

            SyncModePresentation();
            TickModeTaunt();

            ActiveModeContext ctx = BuildModeContext(worldMs);
            if (_modeDirector.Tick(worldMs, ctx))
            {
                _modeHud?.Hide();
                _modeHpDrainAccum = 0f;
                ClearModePresentation();
            }
            else
            {
                _modeHud?.UpdateRemaining(_modeDirector.RemainingSec(worldMs));
            }
        }

        void SyncModePresentation()
        {
            if (_modeDirector?.Active == null)
                return;

            int after = _modeDirector.AfterimageCount;
            if (_afterimage != null)
                _afterimage.CountOverride = after > 0 ? after : -1;

            // VFX her kare Show etmek yerine yalnız aktifken pulse — Show idempotent.
            ActiveModeNode mode = _modeDirector.Active.Value;
            Color tint = _colors != null
                ? _colors.ColorForRune((Rune)Mathf.Clamp(mode.TriggerDot, 1, 6))
                : Color.cyan;
            _modeVfx?.Show(mode.VisualAura, mode.VisualScreenEdges, tint);
        }

        void ClearModePresentation()
        {
            if (_afterimage != null)
                _afterimage.CountOverride = -1;
            _modeVfx?.Hide();
        }

        /// <summary>Aşılmaz Duvar taunt_radius_m — boss Taunt durumu (tek oyuncu; strip + okunur).</summary>
        void TickModeTaunt()
        {
            float radius = _modeDirector?.TauntRadiusM ?? 0f;
            if (radius <= 0f || _bossStatus == null || _player == null || _boss == null)
                return;

            float dist = Vector3.Distance(
                new Vector3(_player.position.x, 0f, _player.position.z),
                new Vector3(_boss.transform.position.x, 0f, _boss.transform.position.z));
            if (dist > radius)
                return;

            // Süreyi yenile — mod açıkken taunt düşmesin.
            double ms = _combat != null ? _combat.Status.TauntMs : 2000;
            _bossStatus.Board.Apply(StatusKind.Taunt, ms, 1f);
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
            SyncModePresentation();
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

        /// <summary>
        /// ElementOrigin ↔ space_layer invisible_link (Karabasan) / tear (Hiçlik).
        /// </summary>
        void TrySpawnSpaceForSkill(SkillResolution skill)
        {
            if (_spaceDirector == null || _skills == null || skill.IsEmpty)
                return;

            string origin = skill.ElementOrigin;
            if (string.IsNullOrEmpty(origin))
                return;

            SpaceLayerTuning tune = _combat != null ? _combat.SpaceLayer : new SpaceLayerTuning();
            Vector3 playerPos = _player != null ? _player.position : Vector3.zero;
            Vector3 bossPos = _boss != null ? _boss.transform.position : playerPos + Vector3.forward * 2f;

            for (int i = 0; i < _skills.SpaceEffects.Count; i++)
            {
                SpaceEffectNode e = _skills.SpaceEffects[i];
                if (!string.Equals(e.Element, origin, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(e.Type, SpaceEffectTypes.InvisibleLink, StringComparison.Ordinal))
                {
                    float dur = e.HasDurationSec ? e.DurationSec : 2f;
                    if (_spaceDirector.TrySpawnLink(
                            e.Id, dur,
                            playerPos.x, playerPos.y, playerPos.z,
                            bossPos.x, bossPos.y, bossPos.z,
                            SpaceDirectorHost.ActorPlayer, SpaceDirectorHost.ActorBoss,
                            tune.LinkDrainPerTick, tune.LinkHealPerTick, tune.LinkMaxRangeM,
                            out _))
                        _spaceHost?.SyncVisuals();
                    return;
                }

                if (string.Equals(e.Type, SpaceEffectTypes.Tear, StringComparison.Ordinal))
                {
                    float dur = e.HasDurationSec ? e.DurationSec : 3f;
                    float dmg = e.HasDamageOnCross ? e.DamageOnCross : 30f;
                    // Yırtık bang ucunda / oyuncu-boss ortasında.
                    Vector3 mid = Vector3.Lerp(playerPos, bossPos, 0.55f);
                    if (_spaceDirector.TrySpawnTear(e.Id, dur, mid.x, 0f, mid.z, dmg, out _))
                        _spaceHost?.SyncVisuals();
                    return;
                }
            }
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

                // Readout'a yazma — ShoutSkill DisplayName'i ezmeyelim (eski: effect id
                // "karabasan koruma silme" 2/3/4'lüyü aynı gösteriyordu). Yan etki debug'da.
                if (string.Equals(e.Type, RealityEffectTypes.ReviveBlock, StringComparison.Ordinal))
                {
                    float dur = e.HasDurationSec ? e.DurationSec : RealityEffectDirector.DefaultReviveBlockSec;
                    _realityDirector.ApplyReviveBlock(worldMs, dur);
                    _debugHud?.NoteSkillBang(skill.DisplayName, "diriliş engeli");
                    return;
                }

                if (string.Equals(e.Type, RealityEffectTypes.PartialErase, StringComparison.Ordinal))
                {
                    StatusBoard board = _bossStatus != null ? _bossStatus.Board : null;
                    if (e.Targets != null && e.Targets.Count > 0)
                        _realityDirector.ApplyPartialErase(board, e.Targets);
                    else
                        _realityDirector.ApplyPartialErase(board);
                    _debugHud?.NoteSkillBang(skill.DisplayName, "kısmi silme");
                    return;
                }

                if (string.Equals(e.Type, RealityEffectTypes.FullErase, StringComparison.Ordinal))
                {
                    StatusBoard board = _bossStatus != null ? _bossStatus.Board : null;
                    if (e.Targets != null && e.Targets.Count > 0)
                        _realityDirector.ApplyFullErase(board, e.Targets);
                    else
                        _realityDirector.ApplyFullErase(board);
                    _debugHud?.NoteSkillBang(skill.DisplayName, "tam silme");
                    return;
                }
            }
        }

        void SyncDashCooldownMult()
        {
            if (_input?.Dodge == null)
                return;
            float mode = _modeDirector?.DashCooldownMult ?? 1f;
            float passive = _passiveDirector?.DashCooldownMult ?? 1f;
            _input.Dodge.CooldownMult = mode * passive;
        }

        void SyncPlayerStateMachine(double worldMs)
        {
            if (_playerStates == null)
                return;

            var vitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            bool isDead = vitals != null && vitals.IsDown;
            bool isStunned = false;
            bool isRooted = false;
            if (_playerStatus != null)
            {
                var board = _playerStatus.Board;
                isStunned = board.Has(StatusKind.Stun) || board.Has(StatusKind.Stasis) || board.Has(StatusKind.Fear);
                isRooted = board.Has(StatusKind.Root);
            }

            int worldMsInt = (int)worldMs;
            bool isDodging = _input?.Dodge != null && _input.Dodge.IsActive(worldMsInt);
            bool isCasting = _pending.Count > 0;
            bool isDrawing = _engine != null && _engine.State.Phase == SentencePhase.Building;
            bool isRecovering = _engine != null && _engine.State.Phase == SentencePhase.Recovering;

            _playerStates.SyncWorld(
                isDead, isStunned, isDodging, isRooted, isCasting, isDrawing, isRecovering);
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
        /// Karabasan delayed_detonation — bang hasarını delay_sec sonra uygular.
        /// true = hasar ertelendi (şimdi ApplyClosingDamage yazılmasın).
        /// Yalnızca sıfat <c>trigger_profile=delayed_detonation</c> (Geciktirme) iken —
        /// aksi halde her 1-6 katlaması aynı 2 sn gecikmeyi alırdı (3'lü/4'lü fark yok).
        /// </summary>
        bool TryDeferDamageAsDelayedDetonation(SkillResolution skill, float pendingDamage)
        {
            if (_timeEffectDirector == null || _skills == null || skill.IsEmpty || pendingDamage <= 0f)
                return false;

            if (!SkillWantsDelayedDetonation(skill))
                return false;

            string origin = skill.ElementOrigin;
            if (string.IsNullOrEmpty(origin))
                return false;

            for (int i = 0; i < _skills.TimeEffects.Count; i++)
            {
                TimeEffectNode e = _skills.TimeEffects[i];
                if (!string.Equals(e.Type, TimeEffectTypes.DelayedDetonation, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(e.Element, origin, StringComparison.OrdinalIgnoreCase))
                    continue;

                float delay = e.HasDelaySec ? e.DelaySec : 0f;
                double worldMs = _clock != null ? _clock.Director.WorldTimeMs : 0;
                return _timeEffectDirector.TryScheduleDelayedDetonation(
                    e.Id, e.Element, delay, worldMs, out _, pendingDamage);
            }

            return false;
        }

        /// <summary>
        /// Sıfat engine_modifiers.trigger_profile == delayed_detonation (JSON: geciktirme).
        /// </summary>
        static bool SkillWantsDelayedDetonation(SkillResolution skill)
        {
            if (skill.IsEmpty || skill.EngineModifiers.IsNull)
                return false;
            if (!skill.EngineModifiers.Has("trigger_profile"))
                return false;
            string profile = skill.EngineModifiers["trigger_profile"].AsString();
            return string.Equals(profile, TimeEffectTypes.DelayedDetonation, StringComparison.Ordinal);
        }

        /// <summary>
        /// Cehennem death_delay — ölüm beyanını delay_sec erteler (çökme/revive sonra).
        /// </summary>
        bool TryDeferBossDeath(SkillResolution skill, double worldMs)
        {
            if (_timeEffectDirector == null || _skills == null || skill.IsEmpty)
                return false;

            string origin = skill.ElementOrigin;
            if (string.IsNullOrEmpty(origin))
                return false;

            for (int i = 0; i < _skills.TimeEffects.Count; i++)
            {
                TimeEffectNode e = _skills.TimeEffects[i];
                if (!string.Equals(e.Type, TimeEffectTypes.DeathDelay, StringComparison.Ordinal))
                    continue;
                if (!string.Equals(e.Element, origin, StringComparison.Ordinal))
                    continue;

                float delay = e.HasDelaySec ? e.DelaySec : 0f;
                if (!_timeEffectDirector.TryScheduleDeathDelay(
                        e.Id, e.Element, delay, worldMs, out _))
                    return false;
                _deferredBossDeath = true;
                return true;
            }

            return false;
        }

        void BeginBossDeathSequence(double worldMs)
        {
            _bossDirector?.NotifyBossDown(worldMs);
            float collapseSec = _colors != null ? _colors.BossDeathCollapseSec : 0.85f;
            _boss?.BeginCollapse(collapseSec, worldMs);
            _deathReviveAtMs = worldMs + collapseSec * 1000.0;
            _deathPending = true;
            _deferredBossDeath = false;
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

        /// <summary>Bağlama 8: vadesi gelen echo / delayed_detonation / death_delay.</summary>
        void TickTimeEffects(double worldMs)
        {
            if (_timeEffectDirector == null)
                return;

            _dueTimeFields.Clear();
            int n = _timeEffectDirector.CollectDue(worldMs, _dueTimeFields);
            for (int i = 0; i < n; i++)
            {
                TimeEffectField field = _dueTimeFields[i];
                if (string.Equals(field.Type, TimeEffectTypes.Echo, StringComparison.Ordinal))
                {
                    ApplyEchoDamage(field.ComputedEchoDamage);
                    continue;
                }

                if (string.Equals(field.Type, TimeEffectTypes.DelayedDetonation, StringComparison.Ordinal))
                {
                    ApplyEchoDamage(field.ComputedDetonationDamage);
                    continue;
                }

                if (string.Equals(field.Type, TimeEffectTypes.DeathDelay, StringComparison.Ordinal)
                    && _deferredBossDeath
                    && _bossVitals != null
                    && _bossVitals.IsDown)
                {
                    BeginBossDeathSequence(worldMs);
                }
            }
        }

        /// <summary>Yankı / gecikmeli patlama hasarı — kaynak; tekrar echo planlamaz.</summary>
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
                BeginBossDeathSequence(worldMs);
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
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
            {
                try
                {
                    return ChainRules.FromJsonRoot(MiniJson.Parse(design.Json));
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
            SkillResolution skill = SkillResolution.Empty;
            if (_skills != null && words != null && words.Count > 0)
            {
                skill = ResolveSkillWords(words);
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

            // Skill animation_type varsa ona göre Play (element ailesi değil — her fiil ayrı clip).
            if (!skill.IsEmpty && !string.IsNullOrEmpty(skill.AnimationType))
                _visual.PulseAnimationType(skill.AnimationType, s);
            else
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

            SkillResolution skill = ResolveSkillWords(words);
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
                float cone = _colors != null ? _colors.SoftAimConeDeg : 70f;
                if (dist > 0.01f && dist <= range
                    && Vector3.Angle(facing, toBoss) <= cone)
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
                armedSkill = ResolveSkillWords(sentence.Words);
                castMult = SkillMobility.CastTimeMult(armedSkill);
                castMult *= WeaponCompatibilityFor(armedSkill).CastTimeMult;
            }
            castMult *= _modeDirector?.CastTimeMult ?? 1f;
            float atkSpd = _modeDirector?.AttackSpeedMult ?? 1f;
            if (atkSpd > 0f)
                castMult /= atkSpd;
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

                float basicDealt = 0f;
                if (IsBossInStrikeCapsule(logic, _combat.Manifestation.BasicStrikeRangeM))
                {
                    ApplyBossClosingBasic(logic, p.Closing);
                    basicDealt = ApplyClosingDamage(p.Closing, SkillResolution.Empty, isBasicStrike: true, slashCommitMult: 0f);
                }
                TryScheduleEchoForSkill(SkillResolution.Empty, basicDealt);
                SpawnClosingImpact(p);
                return;
            }

            _closingChainBonus = BeginChainClosing(p.Words, _clock.Director.WorldTimeMs);
            TryActivateMode(p.Words, _clock.Director.WorldTimeMs);
            SkillResolution skill = ResolvePendingSkill(p);
            if (skill.IsEmpty || !skill.IsComplete)
            {
                _readout?.NoteDenied("2 rün gerekli");
                return;
            }

            WeaponSkillCompatibility compatibility = WeaponCompatibilityFor(skill);
            LastWeaponCompatible = compatibility.Compatible;
            LastWeaponPassiveEnabled = compatibility.PassiveEnabled;
            LastWeaponUiLabel = compatibility.UiLabel;
            if (compatibility.PassiveEnabled)
                TryTriggerPassive(p.Words, _clock.Director.WorldTimeMs);

            ApplyResourceCost(skill);
            SkillMotionPlan motionPlan = ResolveSkillMotion(skill);
            SkillExecutorRoute executorRoute = _skillExecutorRouter.Route(skill, _equippedWeapon);
            LastExecutorKind = executorRoute.Kind;
            // Hareket executor'ı dash'i kendisi başlatır (Sıçrama/Kopyalama tekrarları için).
            if (executorRoute.Kind != SkillExecutorKind.Movement)
                ApplySkillMotion(motionPlan, skill);
            ApplySelfCastEffects(skill);

            bool executorStarted = executorRoute.Kind != SkillExecutorKind.Fallback
                && TryLaunchSkillExecutor(executorRoute.Kind, p, skill, motionPlan);
            if (executorStarted)
                ScheduleFollowUpLaunches(executorRoute.Kind, p, skill, motionPlan);
            else if (executorRoute.Kind == SkillExecutorKind.Movement)
                ApplySkillMotion(motionPlan, skill);
            float dealt = 0f;
            if (!executorStarted)
            {
                if (executorRoute.IsStub)
                    Debug.Log($"[SkillExecutor] stub → LivingEffect: {executorRoute.Reason}");
                LastExecutorKind = SkillExecutorKind.Fallback;
                ApplyBossClosing(logic, p.Closing, skill);
                bool bossReached = _boss != null && IsClosingInRange(logic, p.Closing);
                if (bossReached)
                {
                    dealt = ApplyClosingDamage(
                        p.Closing,
                        skill,
                        isBasicStrike: false,
                        motionPlan.SlashCommitMult);
                    TryScheduleEchoForSkill(skill, dealt);
                }
                ApplyClosingStatuses(p, skill, bossReached);
                ApplyClosingHeal(p.Closing, skill);
            }

            ShoutSkill(skill, p.Words);
            TrySpawnZoneForSkill(skill);
            TryExtendZonesForSkill(skill);
            TrySpawnSpaceForSkill(skill);
            TryApplyRealityForSkill(skill);
            ApplyCooldown(skill, p.Words, cosmeticIfDisabled: true);
            if (!motionPlan.IsEmpty)
                AnnotateMotion(skill, motionPlan);
            AnnounceChainFinisherIfAny(); // skill bang'ten sonra Finisher üstte kalsın
            SpawnClosingImpact(p);
            LastResolvedSkillId = skill.SkillId;
            LastSkillEffectApplied = executorStarted
                || dealt > 0f
                || IsHealSkill(skill)
                || !motionPlan.IsEmpty
                || skill.Mechanics.Length > 0;
            if (string.Equals(skill.SkillId, "1-1", StringComparison.Ordinal))
            {
                Debug.Log(
                    $"[ElementSystem] smoke 1-1 effect applied={LastSkillEffectApplied} "
                    + $"damage={dealt:0.##}");
            }
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
                SkillResolution skill = ResolveSkillWords(p.Words);
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

        bool TryLaunchSkillExecutor(
            SkillExecutorKind kind,
            PendingClosing pending,
            SkillResolution skill,
            in SkillMotionPlan motionPlan,
            float effectMult = 1f,
            LivingEffect capturedLogic = null)
        {
            LivingEffect logic = capturedLogic
                ?? (pending.View != null ? pending.View.Logic : null);
            if (_player == null || logic == null)
                return false;

            EnsurePresentationCatalog();
            ManifestationTuning tuning = _combat != null
                ? _combat.Manifestation
                : new ManifestationTuning();
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, _presentationCatalog, tuning);

            float rangeMult = _equippedWeapon != null ? _equippedWeapon.RangeMult : 1f;
            bool burst = string.Equals(skill.VerbId, "5", StringComparison.Ordinal);
            float radius = plan.BangRadiusM > 0f ? plan.BangRadiusM : tuning.TravelHitRadiusM;
            // Tek hedefli yakın vuruş kapsülü bang yarıçapıyla şişmez; yalnız Patlama alandır.
            if (kind == SkillExecutorKind.MeleeHitbox && !burst)
                radius = tuning.TravelHitRadiusM;
            float range = kind == SkillExecutorKind.MeleeHitbox
                ? tuning.BasicStrikeRangeM * rangeMult
                : Mathf.Max(radius, plan.MaxRangeM * rangeMult);
            float speed = plan.SpeedMps > 0f ? plan.SpeedMps : tuning.NeedleSpeedMps;
            ResolveFieldTiming(skill, plan, tuning, out float durationSec, out float tickSec);
            int spawnCount = 1;
            ApplyVerbHitboxSizing(kind, skill, tuning, rangeMult, burst, ref radius, ref range, ref durationSec, ref spawnCount);

            Vector3 origin = _player.position;
            Vector3 direction = new(logic.DirX, 0f, logic.DirZ);
            Transform target = _boss != null ? _boss.transform : null;
            bool friendly = IsFriendlyFieldVerb(skill) || kind == SkillExecutorKind.SelfState;
            // Düşmana alan boss'un üstünde değil, etkinin dünyada vardığı uçta açılır.
            Vector3 fieldCenter = friendly
                ? origin
                : new Vector3(logic.TipX, origin.y, logic.TipZ);
            float slashCommitMult = motionPlan.SlashCommitMult;
            float executorChainBonus = _closingChainBonus;
            string colorKey = SelectedElementPaint?.Name
                ?? (pending.Words != null && pending.Words.Count > 0
                    ? pending.Words[0].Rune.ToString()
                    : string.Empty);

            bool echoScheduled = false;
            bool statusesApplied = false;
            float accumulatedHealScale = 0f;
            int appliedHealAmount = 0;
            void ApplyExecutorEffect(float effectFraction)
            {
                if (effectFraction <= 0f)
                    return;

                if (!friendly)
                    ApplyBossClosing(logic, pending.Closing, skill);
                float hitDamage = ApplyClosingDamage(
                    pending.Closing,
                    skill,
                    isBasicStrike: false,
                    slashCommitMult,
                    effectFraction * effectMult,
                    executorChainBonus);
                // Bir projectile/melee tek hit'tir. Tick field'da echo katmanını çoğaltmamak
                // için yalnız ilk gerçek hasar kaynak olur.
                if (!echoScheduled && hitDamage > 0f)
                {
                    TryScheduleEchoForSkill(skill, hitDamage);
                    echoScheduled = true;
                }
                if (!statusesApplied)
                {
                    // Dost/kendine alan düşmanca sıfat durumunu yalnız boss alanın içindeyse verir.
                    bool bossReached = !friendly
                        || BossWithin(_player != null ? _player.position : origin, radius);
                    ApplyClosingStatuses(pending, skill, bossReached);
                    statusesApplied = true;
                }
                if (IsHealSkill(skill))
                {
                    accumulatedHealScale = Mathf.Min(1f, accumulatedHealScale + effectFraction);
                    int targetTotal = CalculateClosingHealAmount(
                        pending.Closing,
                        skill,
                        accumulatedHealScale,
                        executorChainBonus);
                    int delta = Mathf.Max(0, targetTotal - appliedHealAmount);
                    if (delta > 0)
                    {
                        Vector3? healCenter = kind == SkillExecutorKind.FieldAura && friendly
                            ? origin
                            : null;
                        ApplyClosingHealAmount(skill, delta, healCenter, radius);
                        appliedHealAmount += delta;
                    }
                }
                LastSkillEffectApplied = hitDamage > 0f
                    || IsHealSkill(skill)
                    || skill.Mechanics.Length > 0;
            }

            var context = new SkillExecutionContext(
                skill,
                _player,
                target,
                origin,
                direction,
                tuning.BangDurationSec,
                tuning.ExecutorMeleeWindowOpen01,
                tuning.ExecutorMeleeWindowClose01,
                radius,
                range,
                speed,
                durationSec,
                tickSec,
                burst,
                friendly,
                colorKey,
                ApplyExecutorEffect,
                _clock,
                tuning,
                fieldCenter,
                startMotion: kind == SkillExecutorKind.Movement
                    ? () => ApplySkillMotion(ResolveSkillMotion(skill), skill)
                    : null,
                applyFlatDamage: kind == SkillExecutorKind.Summon
                    ? raw => ApplyMinionHit(skill, raw * effectMult)
                    : null,
                spawnCount: spawnCount);

            var go = new GameObject($"{kind}_{skill.SkillId}");
            go.transform.SetParent(transform, false);
            ISkillExecutor executor = kind switch
            {
                SkillExecutorKind.MeleeHitbox => go.AddComponent<MeleeHitboxExecutor>(),
                SkillExecutorKind.Projectile => go.AddComponent<ProjectileExecutor>(),
                SkillExecutorKind.FieldAura => go.AddComponent<FieldAuraExecutor>(),
                SkillExecutorKind.Movement => go.AddComponent<MovementExecutor>(),
                SkillExecutorKind.SelfState => go.AddComponent<SelfStateExecutor>(),
                SkillExecutorKind.Summon => go.AddComponent<SummonExecutor>(),
                _ => null
            };
            if (executor == null)
            {
                Destroy(go);
                return false;
            }

            if (kind == SkillExecutorKind.Summon)
                ApplySpawnIFrame(skill);
            executor.Execute(context);
            Debug.Log($"[SkillExecutor] {skill.SkillId} → {kind} r={radius:0.##} menzil={range:0.##} süre={durationSec:0.##} x{effectMult:0.##}");
            return true;
        }

        /// <summary>
        /// hitbox_vfx.fiil_hitbox boyutları: final = base × weapon.range_mult × sıfat hitbox_scale_mult
        /// (hitbox_formula). Saldırı/Patlama his turundaki tuning menzilinde kalır.
        /// Süreli fiiller süreyi engine'den alır (+ lifetime_add).
        /// </summary>
        void ApplyVerbHitboxSizing(
            SkillExecutorKind kind,
            in SkillResolution skill,
            ManifestationTuning tuning,
            float rangeMult,
            bool burst,
            ref float radius,
            ref float range,
            ref float durationSec,
            ref int spawnCount)
        {
            if (!TryVerbHitbox(skill, out VerbHitboxSpec spec))
                return;
            float scale = skill.HitboxScaleMult > 0f ? skill.HitboxScaleMult : 1f;
            JsonValue engine = skill.EngineModifiers;
            float lifetimeAdd = Mathf.Max(0f, engine["lifetime_add"].AsFloat(0f));
            bool tunedReach = skill.VerbId is "1" or "5";

            switch (kind)
            {
                case SkillExecutorKind.MeleeHitbox when !burst && !tunedReach && spec.SizeB > 0f:
                    range = spec.SizeA * rangeMult * scale;
                    radius = spec.SizeB * 0.5f * scale;
                    break;

                case SkillExecutorKind.Movement:
                    if (spec.SizeB > 0f)
                        radius = spec.SizeB * 0.5f * scale;
                    float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                    durationSec = Mathf.Max(spec.DurationSec, dashSec);
                    break;

                case SkillExecutorKind.SelfState:
                    radius = spec.SizeA * scale;
                    float stateSec = engine["reflect_duration_sec"].AsFloat(0f);
                    if (stateSec > 0f)
                        durationSec = stateSec + lifetimeAdd;
                    break;

                case SkillExecutorKind.Summon:
                    radius = spec.SizeA;
                    float minionSec = engine["minion_duration_sec"].AsFloat(0f);
                    if (minionSec > 0f)
                        durationSec = minionSec + lifetimeAdd;
                    spawnCount = Mathf.Max(1, engine["minion_count"].AsInt(1));
                    break;
            }
        }

        void ResolveFieldTiming(
            in SkillResolution skill,
            in LivingEffectPlan plan,
            ManifestationTuning tuning,
            out float durationSec,
            out float tickSec)
        {
            durationSec = 0f;
            tickSec = tuning.ExecutorFieldTickSec;
            if (_presentationCatalog != null
                && _presentationCatalog.TryGetHitbox(plan.HitboxId, out HitboxNode hitbox))
            {
                durationSec = hitbox.GetFloat("lifetime_sec_default", 0f);
                tickSec = hitbox.GetFloat("tick_interval_sec", tickSec);
            }

            JsonValue engine = skill.EngineModifiers;
            if (durationSec <= 0f && !engine.IsNull)
            {
                durationSec = Mathf.Max(
                    engine["channel_sec"].AsFloat(0f),
                    Mathf.Max(
                        engine["cc_duration_sec"].AsFloat(0f),
                        Mathf.Max(
                            engine["buff_duration_sec"].AsFloat(0f),
                            engine["tempo_duration_sec"].AsFloat(0f))));
            }

            if (durationSec <= 0f)
            {
                StatusTuning status = _combat != null ? _combat.Status : new StatusTuning();
                durationSec = skill.VerbId switch
                {
                    "2" => status.RegenMs / 1000f,
                    "4" => status.ShieldMs / 1000f,
                    "6" => status.RootMs / 1000f,
                    "8" => status.HasteMs / 1000f,
                    "12" => status.SlowMs / 1000f,
                    _ => tuning.BangDurationSec
                };
            }

            durationSec = Mathf.Max(tuning.BangDurationSec, durationSec);
            tickSec = Mathf.Clamp(tickSec, 0.01f, durationSec);
        }

        static bool IsFriendlyFieldVerb(in SkillResolution skill) =>
            skill.VerbId is "2" or "4" or "8" or "9";

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
            SkillResolution skill = ResolveSkillWords(words);
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

            if (_hexagonView == null || sec <= 0f)
                return;

            _hexagonView.BeginTrackedCooldown(
                words[0].Dot,
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
            if (_hexagonView == null || skill.IsEmpty || words == null || words.Count == 0)
                return;
            float sec = skill.BaseCooldownSec;
            if (sec <= 0f)
                return;
            _hexagonView.BeginCosmeticCooldown(words[0].Dot, sec);
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

            return SkillMotionMotor.Resolve(
                skill, ctx, t,
                _skills != null ? _skills.SpaceEffects : null,
                _verbData?.IFrameMsFor(skill.SkillId) ?? 0);
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

        SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words)
        {
            if (_skills == null || words == null || words.Count == 0)
                return SkillResolution.Empty;
            if (_skills.IsV61 && words.Count == 2 && _skillFactory != null)
            {
                int elementId = SelectedElementPaint?.Id ?? 0;
                try
                {
                    LastFactorySkill = _skillFactory.CreateFromWords(
                        words, _equippedWeapon, elementId);
                    return LastFactorySkill.Resolution;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SkillFactory] pair çözülemedi: {e.Message}");
                    return SkillResolution.Empty;
                }
            }
            return _skills.ResolveWords(words);
        }

        SkillResolution ResolvePendingSkill(PendingClosing p)
        {
            if (_skills == null || p.Words == null || p.Words.Count == 0)
                return SkillResolution.Empty;
            return ResolveSkillWords(p.Words);
        }

        void ShoutSkill(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (skill.IsEmpty)
                return;

            string mech = SkillFeel.MechanicShort(skill.Mechanics);
            string adj = SkillFeel.AdjectiveShort(skill);
            string paintedName = LastFactorySkill != null
                && string.Equals(LastFactorySkill.Id, skill.SkillId, StringComparison.Ordinal)
                    ? LastFactorySkill.DisplayName
                    : skill.DisplayName;
            ElementPaintNode? paint = SelectedElementPaint;
            if ((LastFactorySkill == null || LastFactorySkill.Id != skill.SkillId)
                && paint.HasValue && !string.IsNullOrEmpty(paint.Value.NamePrefix))
                paintedName = paint.Value.NamePrefix + " " + paintedName;
            string bangNote = string.IsNullOrEmpty(adj)
                ? mech
                : (string.IsNullOrEmpty(mech) ? adj : mech + " | " + adj);
            _debugHud?.NoteSkillBang(paintedName, bangNote);
            // Skill adı altıgen üstündeki SkillPreviewHud'da; büyük ReactionReadout dodge/tepki içindir.
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
            LastAnimationClip = string.Empty;
            LastAnimationUsedFallback = false;

            if (skill.IsEmpty || _visual == null || _visual.Animator == null)
                return;

            if (_skills != null && _skills.IsV61)
            {
                int verbId = int.TryParse(skill.VerbId, out int parsed) ? parsed : 0;
                string weaponKey = _equippedWeapon?.AnimationsKey ?? string.Empty;
                string bindingKey = weaponKey + ":" + verbId;
                if (_animationDatabase != null
                    && _animationDatabase.TryGet(weaponKey, verbId, out AnimationBinding binding))
                {
                    LastAnimationTypeId = bindingKey;
                    LastAnimationState = binding.AnimatorState;
                    LastAnimationPlayApplied =
                        _animationBridge.PlayBinding(binding, _visual.Animator);
                    LastAnimationClip = _animationBridge.LastClipName;
                    LastAnimationUsedFallback = _animationBridge.LastUsedFallbackState;
                }
                else if (_missingAnimationBindings.Add(bindingKey))
                {
                    Debug.LogWarning($"[AnimationDatabase] binding yok, cast no-op: {bindingKey}");
                }
                return;
            }

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
            // ActorVisual.Play yolu — animation_type doğrudan state'e (çift kaynak senkron).
            if (!string.IsNullOrEmpty(check.AnimationTypeId))
            {
                EffectSilhouette axes = default;
                _visual.PulseAnimationType(check.AnimationTypeId, axes);
                LastAnimationState = ActorVisual.AnimationTypeToState(check.AnimationTypeId);
                LastAnimationPlayApplied = true;
                return;
            }

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

        void ApplyClosingStatuses(PendingClosing p, SkillResolution skill, bool bossReached = true)
        {
            if (skill.IsEmpty)
                return;
            ActorStatus bossStatus = bossReached ? _bossStatus : null;
            if (_playerStatus == null && bossStatus == null)
                return;

            var result = StatusApplicator.ApplySkill(
                skill,
                _playerStatus != null ? _playerStatus.Board : null,
                bossStatus != null ? bossStatus.Board : null,
                _combat != null ? _combat.Status : new StatusTuning());

            // v6 Zaman fiili yalnız aktör durumudur; GameClock/Time.timeScale'a dokunmaz.
            if (string.Equals(skill.Action, "tempo", StringComparison.Ordinal))
            {
                JsonValue engine = skill.EngineModifiers;
                double durationMs = engine["tempo_duration_sec"].AsFloat(0f) * 1000.0;
                float enemySlow = engine["enemy_slow"].AsFloat(0f);
                float selfHaste = engine["self_haste"].AsFloat(0f);
                if (durationMs > 0 && enemySlow > 0f && enemySlow <= 1f && bossStatus != null)
                    bossStatus.Board.Apply(StatusKind.Slow, durationMs, enemySlow);
                if (durationMs > 0 && selfHaste > 0f && _playerStatus != null)
                    _playerStatus.Board.Apply(StatusKind.Haste, durationMs, 1f + selfHaste);
            }

            if (result.Knockback && bossStatus != null && _player != null)
                bossStatus.ApplyKnockbackFrom(_player.position);

            if (result.Pull && bossStatus != null && _player != null)
                bossStatus.ApplyPullToward(_player.position);

            // 16 Eylül: "skilleri attığımda bir etkileşim göremiyorum" raporu — durum
            // etkileşim tablosu (docs/element-sistemi.json status_interaction_table) mekanik
            // olarak zaten çalışıyordu, hiçbir görsel sinyali yoktu. Tetiklenen kural varsa
            // aynı tepki yazısı kanalını mevcut AcidGreen vurgusuyla kullan.
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
        void ApplyClosingHeal(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale = 1f,
            float? chainBonusOverride = null,
            Vector3? fieldCenter = null,
            float fieldRadiusM = 0f)
        {
            int amount = CalculateClosingHealAmount(
                closing,
                skill,
                effectScale,
                chainBonusOverride);
            ApplyClosingHealAmount(skill, amount, fieldCenter, fieldRadiusM);
        }

        int CalculateClosingHealAmount(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale,
            float? chainBonusOverride)
        {
            if (skill.IsEmpty || !IsHealSkill(skill) || effectScale <= 0f)
                return 0;
            float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
            // 16 Eylül: "Kavurucu Yara" (grievous_wounds+burn) — yanık hedefe gelen heal azalır.
            // Hedefin StatusBoard'u yoksa (ör. AllyDummy) çarpan 1f, davranış eskisiyle aynı.
            float healMult = _playerStatus != null ? _playerStatus.Board.HealEffectivenessMult : 1f;
            healMult *= _passiveDirector?.HealMult ?? 1f;
            healMult *= chainBonusOverride ?? _closingChainBonus;
            healMult *= WeaponCompatibilityFor(skill).DamageMult;
            float healBase = skill.BaseHeal > 0f
                ? skill.BaseHeal
                : closing.TotalEffect * per;
            return Mathf.Max(0, Mathf.RoundToInt(healBase * healMult * effectScale));
        }

        void ApplyClosingHealAmount(
            SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM)
        {
            if (amount <= 0)
                return;

            var playerVitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            bool spatial = fieldCenter.HasValue && fieldRadiusM > 0f;
            bool allyInRange = !spatial || (_ally != null
                && FlatDistance(_ally.transform.position, fieldCenter.Value) <= fieldRadiusM);
            bool selfInRange = !spatial || (_player != null
                && FlatDistance(_player.position, fieldCenter.Value) <= fieldRadiusM);
            bool allyNeeds = _ally != null && allyInRange && _ally.Hp < _ally.MaxHp;
            bool selfNeeds = playerVitals != null && selfInRange
                && !playerVitals.IsDown && playerVitals.Hp < playerVitals.MaxHp;
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
                if (_modeDirector != null && _modeDirector.NotifyHealed())
                {
                    _modeHud?.Hide();
                    ClearModePresentation();
                }
                _damageHud?.ShowDamage(-healed);
                _readout?.NoteSkill(skill.DisplayName, "self +" + healed, new Color(0.4f, 1f, 0.65f));
                _debugHud?.NoteSkillBang(skill.DisplayName, "self +" + healed);
            }
        }

        static float FlatDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static bool IsHealSkill(SkillResolution skill)
        {
            if (string.Equals(skill.VerbFamily, "mend", System.StringComparison.Ordinal))
                return true;
            string action = skill.Action ?? string.Empty;
            return action is "heal" or "regen" or "cleanse" or "area_cleanse" or "holy_shield";
        }

        /// <summary>
        /// Commit (§5 TotalEffect × ClosingDamagePerEffect) × skill fiil ölçeği.
        /// Heal/dash BaseDamage=0 → 0 can; status ayrı. Tür hasarı değiştirmez (§12).
        /// UseFormulaDamage=true → DamageCalculator (resistance/weakness nötr 0/1).
        /// Dönüş: boss'a uygulanan hasar (0 = yok); Bağlama 8 echo kaynağı.
        /// </summary>
        float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale = 1f,
            float? chainBonusOverride = null)
        {
            if (_bossVitals == null || _bossVitals.IsDown)
                return 0f;
            if (effectScale <= 0f)
                return 0f;

            float outMult = 1f;
            if (_playerStatus != null)
                outMult = _playerStatus.Board.OutgoingDamageMult;
            outMult *= _modeDirector?.DamageMult ?? 1f; // ulti: Öfke Patlaması ×1.8, Kan Çılgınlığı ×2.0
            outMult *= _passiveDirector?.DamageMult ?? 1f; // pasif: alev_hiddeti ×1.15 × karanlik_sessizligi ×1.2 …
            outMult *= SelfDamageBuffMult(); // Güçlendirme buff_damage / Yükseltme self_damage_buff
            outMult *= chainBonusOverride ?? _closingChainBonus;
            float eqMult = 1f;
            if (_equipmentBonus != null && !isBasicStrike && !skill.IsEmpty)
            {
                WeaponSkillCompatibility compatibility = WeaponCompatibilityFor(skill);
                eqMult = compatibility.DamageMult;
                LastWeaponCompatible = compatibility.Compatible;
                LastWeaponPassiveEnabled = compatibility.PassiveEnabled;
                LastWeaponUiLabel = compatibility.UiLabel;
            }
            outMult *= eqMult;
            LastEquipmentMatchMult = eqMult;

            bool isCrit = false;
            float damage;
            float extraCrit = ExtraCritChanceAdd(skill);
            if (_combat != null && _combat.UseFormulaDamage &&
                !isBasicStrike && !skill.IsEmpty && skill.BaseDamage > 0f)
            {
                // length.damage_mult JSON'da 1.0 (anti-ladder); SkillResolution taşımıyor.
                DamageHit hit = EnsureDamageCalculator().Compute(
                    in skill,
                    lengthDamageMult: 1f,
                    resistance: 0f,
                    weaknessBonus: 1f,
                    extraCritChanceAdd: extraCrit);
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
                if (extraCrit > 0f && damage > 0f)
                {
                    DamageHit critHit = EnsureDamageCalculator().ApplyExtraCrit(damage, extraCrit);
                    damage = critHit.Amount;
                    isCrit = critHit.WasCrit;
                }
            }

            // Teleport fiili BaseDamage=0; Zenitsu kesisi commit × SlashCommitMult.
            if (damage <= 0f && slashCommitMult > 0f && closing.TotalEffect > 0f)
            {
                float per = _combat != null ? _combat.ClosingDamagePerEffect : 1f;
                damage = closing.TotalEffect * per * slashCommitMult * outMult;
            }

            damage *= effectScale;

            if (damage <= 0f)
            {
                LastClosingDamageDealt = 0f;
                return 0f;
            }

            // Armor break boss'ta incoming mult
            if (_bossStatus != null)
                damage *= _bossStatus.Board.IncomingDamageMult;

            // Karabasan: bang hasarı delay_sec sonra (delayed_detonation).
            if (!isBasicStrike && TryDeferDamageAsDelayedDetonation(skill, damage))
            {
                LastClosingDamageDealt = damage;
                return damage; // echo kaynağı; CollectDue uygular — şimdi yazma
            }

            LastClosingDamageDealt = damage;
            _damageHud?.ShowDamage(damage, isCrit);
            _lastDamageDealtMs = _clock.Director.WorldTimeMs; // "dealt_damage_recently" (Öfke Patlaması)

            float lifesteal = (_modeDirector?.Lifesteal ?? 0f) + (_passiveDirector?.LifestealAdd ?? 0f);
            lifesteal += AdjectiveLifesteal(skill);
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
                if (!isBasicStrike && TryDeferBossDeath(skill, worldMs))
                    return damage;
                BeginBossDeathSequence(worldMs);
                return damage;
            }

            bossVisual?.PlayStagger();
            return damage;
        }

        static float AdjectiveLifesteal(in SkillResolution skill)
        {
            if (skill.IsEmpty || skill.EngineModifiers.IsNull)
                return 0f;
            // v6 adjective_mods.2 "lifesteal"; eski katalog "apply_lifesteal".
            JsonValue mods = skill.EngineModifiers;
            float v = mods.Has("lifesteal") ? mods["lifesteal"].AsFloat(0f) : mods["apply_lifesteal"].AsFloat(0f);
            return Math.Max(0f, v);
        }

        float ExtraCritChanceAdd(in SkillResolution skill)
        {
            float add = _passiveDirector?.CritChanceAdd ?? 0f;
            if (!skill.IsEmpty && !skill.EngineModifiers.IsNull &&
                skill.EngineModifiers.Has("crit_chance_add"))
                add += Math.Max(0f, skill.EngineModifiers["crit_chance_add"].AsFloat(0f));
            return add;
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

        static readonly Collider[] StrikeHits = new Collider[24];

        /// <summary>
        /// Oyuncudan bakış yönünde reach uzunluğunda, TravelHitRadiusM kalınlığında kapsül.
        /// Kapsül oyuncunun arkasına taşmaz; boss'un gerçek collider'ı temas etmeli.
        /// </summary>
        bool IsBossInStrikeCapsule(LivingEffect logic, float reachM)
        {
            if (_boss == null || _player == null || logic == null)
                return false;
            float radius = _combat.Manifestation.TravelHitRadiusM;
            Vector3 dir = new Vector3(logic.DirX, 0f, logic.DirZ);
            if (dir.sqrMagnitude < 0.0001f)
                return false;
            dir.Normalize();
            Vector3 low = _player.position + Vector3.up * radius + dir * radius;
            Vector3 high = _player.position + Vector3.up * radius + dir * Mathf.Max(radius, reachM);
            int count = Physics.OverlapCapsuleNonAlloc(
                low, high, radius, StrikeHits, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Transform bossT = _boss.transform;
            for (int i = 0; i < count; i++)
            {
                Transform hit = StrikeHits[i].transform;
                if (hit == bossT || hit.IsChildOf(bossT))
                    return true;
            }
            return false;
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
