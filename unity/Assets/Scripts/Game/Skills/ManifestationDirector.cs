using Dovus.App.Casting;
using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Audio;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
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
        PlayerVitals _playerVitalsCache;
        Transform _playerVitalsOwner;

        /// <summary>O11: her kare / her vuruş GetComponent yerine oyuncu başına bir kez.</summary>
        PlayerVitals CachedPlayerVitals()
        {
            if (_player == null)
                return null;
            if (_playerVitalsOwner != _player || _playerVitalsCache == null)
            {
                _playerVitalsOwner = _player;
                _playerVitalsCache = _player.GetComponent<PlayerVitals>();
            }
            return _playerVitalsCache;
        }
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
        PlayerTargeting _targeting;
        Transform _armedTarget;
        Transform _castFacingTarget;
        bool _directionalAttack;
        string _armedSkillId = string.Empty;

        readonly List<LivingEffectView> _active = new();
        readonly List<PendingClosing> _pending = new();
        // Cümlenin şu an sözcük aldığı etki — nokta sayısına göre değil, kimliğe göre izlenir
        // (aynı karede birden fazla nokta kaydı sayı polling'ini atlayabilir, bkz. T7.1).
        LivingEffectView _buildingView;
        int _lastWordCount;
        bool _hooked;
        bool _posedForRecovery;

        // Boss ölümü: çökme süresi bitince Revive.
        /// <summary>K1: ölüm → çöküş → diriliş; <see cref="BossVitals.Died"/> her hasar yolundan tetikler.</summary>
        readonly BossDeathSchedule _bossDeath = new BossDeathSchedule();

        SkillMotor _skills;
        SkillFactory _skillFactory;
        ActorStatus _playerStatus;
        ActorStatus _bossStatus;
        MotionTemplateBody _motionBody;
        AllyDummy _ally;

        // --- Slot pasifleri ---
        SlotPassiveDirector _slotPassives;
        int _slotQueryCastId;
        int _templateSlotCastId;
        bool _slotPassiveNeedsWeapon;
        readonly List<PassiveEchoShot> _passiveEchoes = new();
        readonly PassiveFlowRunner _passiveFlows = new();
        PassiveHud _passiveHud;
        MobilityCcData _mobilityCc;
        SkillNumberCatalog _skillNumbers;
        // --- State machine (player_states ↔ SentencePhase / dodge / CC) ---
        PlayerStateMachine _playerStates;
        // Kapanış çarpanı (ApplyClosing*). v5 zincir katmanı kaldırıldı; hep 1.
        float _closingChainBonus = 1f;
        HexagonInput _input;
        // --- Ekipman (Bağlama 9) — sabit silah; seçim UI yok ---
        EquipmentItem _equippedWeapon;
        EquipmentBonusResolver _equipmentBonus;
        readonly SkillExecutorRouter _skillExecutorRouter = new();
        readonly List<EquipmentItem> _cycleWeapons = new();
        int _cycleWeaponIndex = -1;
        // --- Animasyon (Bağlama 10) — SkillPresentation → AnimationBridge; PulseRune kalır ---
        AnimationDatabase _animationDatabase;
        readonly HashSet<string> _missingAnimationBindings = new();
        readonly AnimationBridge _animationBridge = new();
        int _elementPaintIndex;
        HexagonView _hexagonView;
        PlayerResource _playerResource;
        PlayerCooldown _playerCooldown;
        double _lastMovedMs = double.NegativeInfinity;

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
            RefreshDefenderArmor();
            LastFactorySkill = null;
            _weaponSwap?.ReplaceActive(_equippedWeapon);

            string routeType = SkillExecutorRouter.IsRangedWeapon(_equippedWeapon)
                ? "ranged"
                : "melee";
            string numericId = _equippedWeapon.Id;
            int colon = numericId.LastIndexOf(':');
            if (colon >= 0 && colon + 1 < numericId.Length)
                numericId = numericId.Substring(colon + 1);
            DebugConfig.DevLog(
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
        public event Action<ElementPaintNode> ElementPaintChanged;

#if UNITY_EDITOR
        /// <summary>Bağlama 10 / MCP: ShoutSkill içindeki ApplySkillAnimation yolunu doğrudan dener.</summary>
        public void DebugApplySkillAnimation(SkillResolution skill)
        {
            EnsureLaunchServices();
            _skillPresentation.ApplySkillAnimation(skill);
        }
#endif

        public ElementPaintNode? CycleElementPaint()
        {
            if (_skills == null || _skills.ElementPaints.Count == 0)
                return null;
            _elementPaintIndex = (_elementPaintIndex + 1) % _skills.ElementPaints.Count;
            ElementPaintNode paint = _skills.ElementPaints[_elementPaintIndex];
            _readout?.NoteSkill("Element: " + paint.Name, "isim/VFX boya katmanı", Color.cyan);
            DebugConfig.DevLog($"[ElementSystem] element paint={paint.Id}:{paint.Name} ({paint.Vfx})");
            return paint;
        }

        public bool TrySetElementPaint(int elementId)
        {
            if (_skills == null)
                return false;
            for (int i = 0; i < _skills.ElementPaints.Count; i++)
            {
                if (_skills.ElementPaints[i].Id != elementId)
                    continue;
                _elementPaintIndex = i;
                ElementPaintNode paint = _skills.ElementPaints[i];
                ElementPaintChanged?.Invoke(paint);
                _readout?.NoteSkill("Element: " + paint.Name, "isim/VFX boya katmanı", Color.cyan);
                DebugConfig.DevLog($"[ElementSystem] element paint={paint.Id}:{paint.Name} ({paint.Vfx})");
                return true;
            }
            return false;
        }

        SkillMotor Skills => _skills ??= SkillMotorLoader.Load();

        WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill)
        {
            if (_equipmentBonus == null || _equippedWeapon == null || skill.IsEmpty)
                return WeaponSkillCompatibility.Neutral;
            return _skillFactory != null
                ? _skillFactory.EvaluateWeapon(skill, _equippedWeapon)
                : WeaponSkillCompatibility.Neutral;
        }

        public LivingEffect ActiveLogic => _buildingView?.Logic;

        public int ActiveCount => _active.Count;

        public Transform CurrentFacingTarget
        {
            get
            {
                switch (FacingKind())
                {
                    case AttackFaceKind.LockedTarget:
                        return AttackLockTarget();
                    case AttackFaceKind.Movement:
                        return _targeting != null ? _targeting.SelectedTransform : null;
                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Saldırı boyunca gövde çubuğa dönmez. Hedef varsa ona kilitlenir;
        /// yoksa bakış kalır. Saldırı dışında seçili hedef varsa eski kilit durur.
        /// </summary>
        public bool CombatFacingLocked =>
            FacingKind() != AttackFaceKind.Movement || CurrentFacingTarget != null;

        bool PerformingAttack =>
            (_engine != null && (_engine.State.Phase == SentencePhase.Building
                || _engine.State.Phase == SentencePhase.Recovering))
            || _pending.Count > 0
            || (_motionBody != null && _motionBody.IsDisplacing)
            || (_visual != null && _visual.IsAttackPose);

        AttackFaceKind FacingKind()
        {
            bool performing = PerformingAttack;
            return AttackFacingRules.Resolve(
                performing,
                performing && _directionalAttack,
                AttackLockTarget() != null);
        }

        Transform AttackLockTarget()
        {
            if (_castFacingTarget != null && _castFacingTarget != _player)
                return _castFacingTarget;
            return _targeting != null ? _targeting.SelectedTransform : null;
        }

        public SlotPassiveDirector SlotPassives => _slotPassives;

        /// <summary>state_machine.player_states — SentencePhase/dodge/CC ile senkron.</summary>
        public PlayerStateMachine PlayerStates => _playerStates;

#if UNITY_EDITOR
        /// <summary>Editör/prob: Update beklemeden cümle senkronu.</summary>
        public void ForceSync()
        {
            if (_clock == null)
                return;
            SyncFromSentence(_clock.Director.WorldTimeMs);
        }
#endif

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
            HexagonView hexagonView = null,
            PassiveHud passiveHud = null,
            EquipmentItem equippedWeapon = null,
            EquipmentBonusResolver equipmentBonus = null,
            SkillMotor skills = null,
            SkillFactory skillFactory = null,
            AnimationDatabase animationDatabase = null)
        {
            _clock = clock;
            if (_input != null)
                _input.SkillCancelledByDodge -= CancelActiveSkillForDodge;
            _input = input;
            if (_input != null)
            {
                _input.SkillCancelledByDodge -= CancelActiveSkillForDodge;
                _input.SkillCancelledByDodge += CancelActiveSkillForDodge;
            }
            _engine = input.Engine;
            _combat = input.Combat;
            _colors = colors;
            _player = player;
            _pose = pose;
            _visual = player != null ? player.GetComponent<ActorVisual>() : null;
            _boss = boss;
            if (_bossVitals != null)
            {
                _bossVitals.Died -= OnBossDied;
                _bossVitals.Revived -= OnBossRevivedExternally;
            }
            _bossVitals = bossVitals;
            if (_bossVitals != null)
            {
                _bossVitals.Died -= OnBossDied;
                _bossVitals.Died += OnBossDied;
                _bossVitals.Revived -= OnBossRevivedExternally;
                _bossVitals.Revived += OnBossRevivedExternally;
            }
            _scars = scars;
            _damageHud = damageHud;
            _bossDirector = bossDirector;
            _motor = player.GetComponent<KinematicMotor>();
            if (_playerStatus != null)
            {
                _playerStatus.DamageTaken -= OnPlayerDamageTaken;
                _playerStatus.DamageBlocked -= OnPlayerDamageBlocked;
            }
            if (_bossStatus != null)
                _bossStatus.DamageOverTimeDealt -= OnBossDamageOverTime;
            _playerStatus = playerStatus;
            _bossStatus = bossStatus;
            if (_bossStatus != null)
                _bossStatus.DamageOverTimeDealt += OnBossDamageOverTime;
            _debugHud = debugHud;
            _readout = readout;
            _camera = camera;
            _ally = ally;
            _ally?.BindStatusClock(clock, _combat != null ? _combat.Status : null);
            EnsureMotionReady();
            _passiveHud = passiveHud;
            _hexagonView = hexagonView;
            _equippedWeapon = equippedWeapon;
            RefreshDefenderArmor();
            EnsureBossArmor();
            _equipmentBonus = equipmentBonus;
            _playerResource = player != null ? player.GetComponent<PlayerResource>() : null;
            _playerCooldown = player != null ? player.GetComponent<PlayerCooldown>() : null;
            _skills = skills ?? SkillMotorLoader.Load();
            _skillFactory = skillFactory ?? new SkillFactory(_skills, _equipmentBonus);
            _animationDatabase = animationDatabase ?? LoadAnimationDatabase();
            _elementPaintIndex = 0;
            EnsureLaunchServices();
            _skillPresentation.EnsureCatalog();
            _playerStates = new PlayerStateMachine(_skills.PlayerStates);
            input.BindPlayerStates(_playerStates, () => _pending.Count > 0);
            var motorForStates = player != null ? player.GetComponent<KinematicMotor>() : null;
            motorForStates?.BindPlayerStates(_playerStates);
            _slotPassives = new SlotPassiveDirector();
            _slotPassiveNeedsWeapon = false;
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign slotDesign))
                _slotPassiveNeedsWeapon = PassiveSlotPolicy.RequiresWeaponCompatibility(slotDesign.Document);
            _closingChainBonus = 1f;
            if (_playerStatus != null)
            {
                _playerStatus.SlotPassiveDirector = _slotPassives;
                _playerStatus.ReflectBossVitals = bossVitals;
                _playerStatus.IncomingDamageRedirect = RedirectMechanicDamage;
                _playerStatus.ReflectSink = ApplyReflectedDamage;
                _playerStatus.DamageTaken += OnPlayerDamageTaken;
                _playerStatus.DamageBlocked += OnPlayerDamageBlocked;
            }


            if (_engine != null && !_hooked)
            {
                _engine.SentenceCompleted += OnSentenceCompleted;
                _hooked = true;
            }
            // Açılış görseli: elde silah + arketip controller ilk kareden doğru olsun.
            SyncVisualDelivery();
            EnsureClosingServices();
            EnsureLaunchServices();
            _skillPresentation.EnsureCatalog();
        }

        public void BindTargeting(PlayerTargeting targeting)
        {
            _targeting = targeting;
            _input?.BindSkillTargetGate(TryArmSkillTarget);
            _motor?.BindCombatFacing(() => CurrentFacingTarget, () => CombatFacingLocked);
        }

        static AnimationDatabase LoadAnimationDatabase()
        {
            return ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design)
                ? design.Animations
                : null;
        }

        /// <summary>S16: lambda değil metot — OnDestroy'da bırakılabilsin.</summary>
        void OnPlayerDamageBlocked(float absorbed)
        {
            NoteShieldBlockIfGuarding();
            OnJsonShieldBlocked();
        }

        /// <summary>K1: boss canı hangi yoldan 0'a inerse insin (DoT, yansıma, minyon, emme, takım…).</summary>
        void OnBossDied()
        {
            if (_clock == null)
                return;
            BeginBossDeathSequence(_clock.Director.WorldTimeMs);
        }

        /// <summary>
        /// Biri (Play Sweep, dev aracı) ölüm sırası bitmeden boss'u diriltirse bekleyen diriliş iptal —
        /// yoksa sonraki tur ortasında ikinci bir Revive canı fulleyebilirdi. Kendi dirilişimiz
        /// TryRevive'da önce bekleyeni kapattığı için burada no-op.
        /// </summary>
        void OnBossRevivedExternally()
        {
            if (!_bossDeath.Pending)
                return;
            _bossDeath.Cancel();
            _boss?.EndCollapse();
            if (_clock != null)
                _bossDirector?.NotifyBossRevived(_clock.Director.WorldTimeMs);
        }

        /// <summary>S4: boss'taki yanma/zehir tikinin hasar sayısı.</summary>
        void OnBossDamageOverTime(float amount)
        {
            if (amount <= 0f || _boss == null)
                return;
            _damageHud?.ShowDamage(amount, false, BossHitPoint(), DamageTint(), victimIsBoss: true);
        }

        void OnDestroy()
        {
            if (_engine != null && _hooked)
                _engine.SentenceCompleted -= OnSentenceCompleted;
            if (_input != null)
                _input.SkillCancelledByDodge -= CancelActiveSkillForDodge;
            if (_bossVitals != null)
            {
                _bossVitals.Died -= OnBossDied;
                _bossVitals.Revived -= OnBossRevivedExternally;
            }
            if (_bossStatus != null)
                _bossStatus.DamageOverTimeDealt -= OnBossDamageOverTime;
            if (_playerStatus != null)
            {
                _playerStatus.DamageTaken -= OnPlayerDamageTaken;
                _playerStatus.DamageBlocked -= OnPlayerDamageBlocked;
                _playerStatus.IncomingDamageRedirect = null;
                _playerStatus.ReflectSink = null;
            }
        }

        void Update()
        {
            if (_engine == null || _clock == null)
                return;

            double worldMs = _clock.Director.WorldTimeMs;
            float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);
            if (!PerformingAttack)
                _directionalAttack = false;

            // Kilit kesildi (§5): poz da kesilir. Kapanış patlaması kesilmez, kendi
            // zamanlamasıyla gelir (TickPendingClosings).
            if (_posedForRecovery && _engine.State.Phase != SentencePhase.Recovering)
            {
                _pose?.EndRecovery();
                _posedForRecovery = false;
            }

            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.01f)
                _lastMovedMs = worldMs;
            TickOrb(worldMs);
            TickCannonRecoil();

            SyncFromSentence(worldMs);
            ApplyWindowCue();
            TickEffects(dtSec, worldMs);
            _pose?.Tick(worldMs);
            _boss?.Tick(dtSec, worldMs);
            TickPendingClosings(worldMs);
            TickBossDeath();
            TickPassives(worldMs);
            SyncPlayerStateMachine(worldMs);
            TickWeaponSwap(worldMs);
            TickCastHold(worldMs);
            TickDelayedLaunches(worldMs);
            TickTemplateDelivery(worldMs);
            TickMechanics(worldMs);
            _animationBridge.Tick(worldMs);
        }

        /// <summary>
        /// O-anim(c): CastChannel/CastGuard döngü (hold) sinyali — var olan sürdürülen cast
        /// (<see cref="SustainedSkillActive"/>) ve kalkan (ShieldRemaining) durumlarından okunur,
        /// yeni oyun durumu eklenmez. Binder bu bool'ları Animator koşullarına bağlar.
        /// </summary>
        void TickCastHold(double worldMs)
        {
            if (_visual == null)
                return;
            bool channelHeld = SustainedSkillActive(worldMs);
            bool guardHeld = _playerStatus != null && _playerStatus.Board.ShieldRemaining > 0.01f;
            _visual.SetHoldFlags(channelHeld, guardHeld);
        }

        // --- Pasifler (Bağlama 5) ---

        void TickPassives(double worldMs)
        {
            _slotPassives?.Tick(worldMs);
            TickPassiveEchoes(worldMs);
            TickPassiveFlows(worldMs);
            if (_slotPassives != null && _slotPassives.ActiveCount > 0)
                _passiveHud?.Sync(_slotPassives.Active, worldMs);
            else
                _passiveHud?.Refresh();
        }

        /// <summary>
        /// Kapanışın sıfat rünü pasif yuvasındaysa o rünün slot pasifini açar.
        /// </summary>
        void TryTriggerPassive(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            if (words == null || words.Count == 0)
                return;

            if (words.Count >= 2 && _slotPassives != null && _engine?.Loadout != null)
            {
                int adjectiveRuneId = (int)words[1].Rune;
                if (_engine.Loadout.IsPassive(adjectiveRuneId)
                    && _skills.TryGetRune(adjectiveRuneId, out RuneDefinition rune)
                    && _skills.TryGetAdjective(adjectiveRuneId.ToString(), out AdjectiveNode adjective)
                    && _slotPassives.Activate(
                        adjectiveRuneId,
                        rune.AdjectiveFace,
                        rune.PassiveDurationDefault,
                        adjective.Engine,
                        worldMs))
                {
                    _readout?.NoteSkill(
                        rune.AdjectiveFace + " pasif",
                        rune.PassiveDurationDefault.ToString("0.#") + " sn",
                        Color.cyan);
                    _passiveHud?.Sync(_slotPassives.Active, worldMs);
                }
            }
        }

        void SyncPlayerStateMachine(double worldMs)
        {
            if (_playerStates == null)
                return;

            var vitals = CachedPlayerVitals();
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
            bool isCasting = _pending.Count > 0 && !SwapDrawUnlocked(worldMs);
            bool isDrawing = _engine != null && _engine.State.Phase == SentencePhase.Building;
            bool isRecovering = _engine != null && _engine.State.Phase == SentencePhase.Recovering;

            _playerStates.SyncWorld(
                isDead, isStunned, isDodging, isRooted, isCasting, isDrawing, isRecovering);
        }

        /// <summary>Yalnız <see cref="OnBossDied"/>'dan (K1 tek kanca).</summary>
        void BeginBossDeathSequence(double worldMs)
        {
            float collapseSec = _colors != null ? _colors.BossDeathCollapseSec : 0.85f;
            if (!_bossDeath.Begin(worldMs, collapseSec))
                return;
            _bossDirector?.NotifyBossDown(worldMs);
            _boss?.BeginCollapse(collapseSec, worldMs);
        }

        void TickBossDeath()
        {
            if (_clock == null || !_bossDeath.TryRevive(_clock.Director.WorldTimeMs))
                return;

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
            SkillResolution skill = SkillResolution.Empty;
            if (_skills != null && words != null && words.Count > 0)
                skill = ResolveSkillWords(words);
            FaceAim(skill);
            _pose?.PulseRune(rune, worldMs);
            if (_visual == null)
                return;
            SyncVisualDelivery();

            EffectSilhouette s;
            if (!skill.IsEmpty)
                s = SilhouetteBuilder.FromSkill(skill, _combat?.Manifestation);
            else if (words != null && words.Count > 0)
                s = SilhouetteBuilder.FromWords(words, _combat?.Manifestation);
            else
                s = default;

            // Skill animation_type varsa ona göre Play (element ailesi değil — her fiil ayrı clip).
            if (!skill.IsEmpty && !string.IsNullOrEmpty(skill.AnimationType))
                _visual.PulseAnimationType(skill.AnimationType, s);
            else
                _visual.PulseRune(rune, s);
        }

        /// <summary>
        /// Karakteri vuruşun gideceği yöne çevirir; boss'a yalnız soft-aim konisindeyse döner.
        /// Görsel yön ile hitbox yönü ayrışırsa oyuncu boss'a vurduğunu görüp hasar göremez.
        /// </summary>
        void FaceAim(in SkillResolution skill)
        {
            if (_player == null)
                return;
            bool directional = !skill.IsEmpty
                && TargetingRules.AimMode(skill) == SkillAimMode.Directional;
            _directionalAttack = directional;
            if (directional)
            {
                Vector3 aim = ResolveAimFacing(_player.position);
                if (aim.sqrMagnitude < 0.0001f)
                    return;
                _player.rotation = Quaternion.LookRotation(aim, Vector3.up);
                return;
            }

            FaceTarget(AttackLockTarget());
        }

        void FaceTarget(Transform target)
        {
            if (_player == null || target == null || target == _player)
                return;
            Vector3 to = target.position - _player.position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.0001f)
                _player.rotation = Quaternion.LookRotation(to.normalized, Vector3.up);
        }

        void CaptureBasicFacing()
        {
            _castFacingTarget = null;
            if (_targeting == null)
                return;
            Transform selected = _targeting.SelectedTransform;
            if (selected != null && selected != _player && IsEnemyBody(selected))
            {
                _castFacingTarget = selected;
                return;
            }

            float range = _combat != null
                ? _combat.Manifestation.BasicStrikeRangeM
                : SkillNumberFallbacks.RangeM;
            if (_targeting.TryResolveBasicEnemy(
                    StrikeCapsule.CenterRange(PlayerBodyRadiusM(), range), out Transform auto)
                && auto != null)
                _castFacingTarget = auto;
        }

        Vector3 FacingOrBody(Transform target, Vector3 pos)
        {
            if (target != null && target != _player)
            {
                Vector3 to = target.position - pos;
                to.y = 0f;
                if (to.sqrMagnitude > 0.0001f)
                    return to.normalized;
            }
            return FlatBodyForward();
        }

        Vector3 FlatBodyForward()
        {
            Vector3 facing = _player != null ? _player.forward : Vector3.forward;
            facing.y = 0f;
            if (facing.sqrMagnitude < 0.0001f)
                return Vector3.forward;
            return facing.normalized;
        }

        bool TryArmSkillTarget(SkillResolution skill)
        {
            SkillAimMode aimMode = TargetingRules.AimMode(skill);
            float range = TargetingRangeFor(skill);
            Transform target;
            TargetFailure failure;
            bool allowed = _targeting != null
                ? _targeting.TryResolve(skill, range, out target, out failure)
                : TryResolveLegacyTarget(skill, aimMode, range, out target, out failure);
            if (!allowed)
            {
                // Menzil dışı sessizce yutulmaz: hedef kalır, kalıp aradaki yolu kapanış fazıyla alır.
                if (failure == TargetFailure.OutOfRange && target != null
                    && aimMode == SkillAimMode.Targeted)
                {
                    _armedTarget = target;
                    _armedSkillId = skill.SkillId;
                    _directionalAttack = false;
                    _castFacingTarget = target != _player ? target : null;
                    FaceTarget(AttackLockTarget());
                    return true;
                }

                _armedTarget = null;
                _armedSkillId = string.Empty;
                _castFacingTarget = null;
                _readout?.NoteDenied(
                    failure == TargetFailure.OutOfRange ? "menzil dışı" : "hedef yok",
                    "mana ve soğuma harcanmadı");
                return false;
            }

            _armedTarget = target;
            _armedSkillId = skill.SkillId;
            _directionalAttack = aimMode == SkillAimMode.Directional;
            _castFacingTarget = aimMode == SkillAimMode.Targeted && target != _player
                ? target
                : null;
            if (_directionalAttack)
                FaceAim(skill);
            else
                FaceTarget(AttackLockTarget());
            return true;
        }

        bool TryResolveLegacyTarget(
            in SkillResolution skill,
            SkillAimMode aimMode,
            float range,
            out Transform target,
            out TargetFailure failure)
        {
            failure = TargetFailure.None;
            if (aimMode != SkillAimMode.Targeted || skill.TargetMode == "self_only"
                || skill.TargetMode == "self_or_ally")
            {
                target = _player;
                return true;
            }
            if (_boss != null && PlanarMath.FlatDistance(
                    _player.position.x, _player.position.z,
                    _boss.transform.position.x, _boss.transform.position.z) <= range)
            {
                target = _boss.transform;
                return true;
            }
            if (_boss != null)
            {
                target = _boss.transform;
                failure = TargetFailure.OutOfRange;
                return false;
            }
            target = null;
            failure = TargetFailure.NoTarget;
            return false;
        }

        float TargetingRangeFor(in SkillResolution skill)
        {
            if (CardEffectRules.PrefersAlly(skill.TargetMode, skill.Action))
            {
                float allyRange = _skillNumbers != null
                    ? _skillNumbers.AllySkillRangeM
                    : SkillNumberFallbacks.AllySkillRangeM;
                return MotionCastReach.GateRangeM(
                    Mathf.Max(0.05f, CardEffectRules.ResolveRange(true, allyRange, 0f)),
                    PlayerBodyRadiusM());
            }

            EnsurePresentationCatalog();
            ManifestationTuning tuning = _combat != null
                ? _combat.Manifestation
                : new ManifestationTuning();
            SkillExecutorRoute route = _skillExecutorRouter.Route(skill, _equippedWeapon);
            route = ApplyMechanicWorldRoute(MechanicPlanFor(skill), route);
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, PresentationCatalog, tuning);
            float rangeMult = _equippedWeapon != null ? _equippedWeapon.RangeMult : 1f;
            bool burst = string.Equals(skill.VerbId, "5", StringComparison.Ordinal);
            float radius = plan.BangRadiusM > 0f ? plan.BangRadiusM : tuning.TravelHitRadiusM;
            if (route.Kind == SkillExecutorKind.MeleeHitbox && !burst)
                radius = tuning.TravelHitRadiusM;
            float range = route.Kind == SkillExecutorKind.MeleeHitbox
                ? tuning.BasicStrikeRangeM * rangeMult
                : Mathf.Max(radius, plan.MaxRangeM * rangeMult);
            float duration = tuning.BangDurationSec;
            int spawnCount = 1;
            ApplyVerbHitboxSizing(
                route.Kind, skill, tuning, rangeMult, burst,
                ref radius, ref range, ref duration, ref spawnCount);
            float edge = Mathf.Max(0.05f, range);
            if (!skill.IsEmpty
                && MotionCatalog.TryGet(skill.SkillId, out MotionBinding motion)
                && motion.Implemented)
                edge = MotionCastReach.ComboEdgeReach(edge, motion.Template);
            return MotionCastReach.GateRangeM(edge, PlayerBodyRadiusM());
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
            Vector3 facing;
            if (basicStrike)
            {
                // Seçili hedef varsa ona bak, menzil dışı olsa bile. Yoksa menzildeki
                // düşman. O da yoksa mevcut bakış: çubuk vuruşun ortasında gövdeyi çevirmez.
                _directionalAttack = false;
                CaptureBasicFacing();
                facing = FlatBodyForward();
                if (_castFacingTarget != null)
                {
                    Vector3 toTarget = _castFacingTarget.position - pos;
                    toTarget.y = 0f;
                    if (toTarget.sqrMagnitude > 0.0001f)
                        facing = toTarget.normalized;
                }

                if (_player != null && facing.sqrMagnitude > 0.0001f)
                    _player.rotation = Quaternion.LookRotation(facing, Vector3.up);
            }
            else if (_directionalAttack)
                facing = ResolveAimFacing(pos);
            else
                facing = FacingOrBody(_castFacingTarget != null ? _castFacingTarget : AttackLockTarget(), pos);

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
            if (basicStrike)
                StopBasicCannonAtFirstBody(logic, pos, facing);

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
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, PresentationCatalog, man);
            logic.ApplyPlan(plan);
        }

        /// <summary>
        /// Hız varsa hız, yoksa karakterin yüzü; boss yalnız SoftAimRangeM + SoftAimConeDeg
        /// içindeyse soft-lock. Kamera yaw'ı kullanılmaz: kamera oyuncunun arkasını izlemez,
        /// durunca vuruş karakterin baktığı yerden kopup sabit dünya yönüne giderdi.
        /// </summary>
        Vector3 ResolveAimFacing(Vector3 pos)
        {
            Vector3 facing = _player != null ? _player.forward : Vector3.forward;
            facing.y = 0f;
            if (_motor != null && _motor.Velocity.sqrMagnitude > 0.05f)
                facing = _motor.Velocity.normalized;

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
            int basicRune = _colors != null ? _colors.BasicStrikeDot : 1;
            bool sentenceIsBasic = sentence.Words.Count == 1 && (int)sentence.Words[0].Rune == basicRune;
            if (view != null && BasicStrikeInput.ReplaceStaleView(sentenceIsBasic, view.IsBasicStrike))
            {
                if (view.Logic != null)
                    view.Logic.Abort();
                view = null;
            }
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
                    SyncVisualDelivery();
                    _visual?.PulseBasicStrike();
                    TryBeginBasicStrikeStep();
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

            Transform pendingTarget = spawnedForBasicStrike ? _castFacingTarget : null;
            SkillAimMode pendingAimMode = SkillAimMode.Targeted;
            if (!spawnedForBasicStrike && !armedSkill.IsEmpty)
            {
                pendingAimMode = TargetingRules.AimMode(armedSkill);
                if (!string.Equals(_armedSkillId, armedSkill.SkillId, StringComparison.Ordinal)
                    && !TryArmSkillTarget(armedSkill))
                {
                    view.Logic.Abort();
                    return;
                }
                pendingTarget = _armedTarget;
                FaceTarget(_castFacingTarget);
            }
            float atkSpd = PortalBorderTeamHooks.AttackSpeedMult;
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
                IsBasicStrike = basic,
                Target = pendingTarget,
                AimMode = pendingAimMode
            });
            _armedTarget = null;
            _armedSkillId = string.Empty;
        }

#if UNITY_EDITOR
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
#endif

        void TickPendingClosings(double worldMs)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingClosing p = _pending[i];
                bool basic = IsPendingBasic(p);
                if (p.View == null || p.View.Logic == null)
                {
                    // Unity Destroy sonraki karede view'ı null yapar. Zafiyet kalıbı
                    // etkiyi bang'den önce söndürürse ilk düz vuruş burada düşüp 0 yazıyordu.
                    if (BasicStrikePayoff.KeepUntilBang(basic, worldMs, p.BangAtWorldMs))
                        continue;
                    if (BasicStrikePayoff.PayWithoutView(basic, worldMs, p.BangAtWorldMs))
                        FireClosing(p);
                    _pending.RemoveAt(i);
                    continue;
                }

                if (p.View.Logic.Phase is LivingEffectPhase.Fading or LivingEffectPhase.Dead)
                {
                    // Düz vuruş kabul edildi ama etki, skill kalıbı/hitstop yüzünden
                    // bang'den önce söndüyse hasar yine vadesinde iner. İptal pending'i siler.
                    if (BasicStrikePayoff.KeepUntilBang(basic, worldMs, p.BangAtWorldMs))
                        continue;
                    if (BasicStrikePayoff.PayWithoutView(basic, worldMs, p.BangAtWorldMs))
                        FireClosing(p);
                    _pending.RemoveAt(i);
                    continue;
                }

                if (worldMs < p.BangAtWorldMs)
                    continue;

                FireClosing(p);
                _pending.RemoveAt(i);
            }
        }

        bool IsPendingBasic(PendingClosing p)
        {
            if (p.IsBasicStrike || (p.View != null && p.View.IsBasicStrike))
                return true;
            if (p.Words == null || p.Words.Count != 1)
                return false;
            int basicDot = _colors != null ? _colors.BasicStrikeDot : 1;
            return (int)p.Words[0].Rune == basicDot;
        }

        void FireClosing(PendingClosing p)
        {
            LivingEffect logic = p.View != null ? p.View.Logic : null;
            logic?.FireClosingBang();
            StampScar(p.View, p.Closing);

            // Düz vuruş: jab — skill motoru / mana / CD / zincir / pasif / ulti yok.
            // BasicStrikeDot gramer fiili (varsayılan Ateş) skill cast sayılmaz.
            bool basic = IsPendingBasic(p);
            if (basic)
            {
                _castPort ??= new CastPort(this);
                _castPort.BeginClosing(logic);
                _castPipeline.RunBasic(p, _castPort);
                return;
            }

            if (logic == null)
                return;

            _castPort ??= new CastPort(this);
            _castPort.BeginClosing(logic);
            _castPipeline.RunSkill(p, _castPort);
        }

        struct PassiveEchoShot
        {
            public double DueMs;
            public float Power;
            public int SlotCastId;
            public ClosingHit Closing;
            public SkillResolution Skill;
            public float Slash;
            public float Chain;
        }

        void TickPassiveEchoes(double worldMs)
        {
            for (int i = _passiveEchoes.Count - 1; i >= 0; i--)
            {
                PassiveEchoShot echo = _passiveEchoes[i];
                if (worldMs < echo.DueMs)
                    continue;
                _passiveEchoes.RemoveAt(i);
                int prev = _slotQueryCastId;
                _slotQueryCastId = echo.SlotCastId;
                try
                {
                    if (IsHealSkill(echo.Skill))
                        ApplyClosingHeal(echo.Closing, echo.Skill, echo.Power, echo.Chain);
                    else
                        ApplyClosingDamage(
                            echo.Closing,
                            echo.Skill,
                            isBasicStrike: false,
                            echo.Slash,
                            echo.Power,
                            echo.Chain);
                }
                finally
                {
                    _slotQueryCastId = prev;
                }
            }
        }

        void TickPassiveFlows(double worldMs)
        {
            if (_bossVitals == null || _bossVitals.IsDown)
                return;
            float damage = _passiveFlows.Collect(worldMs);
            if (damage <= 0f)
                return;
            _bossVitals.ApplyDamage(damage);
            _damageHud?.ShowDamage(damage, false, BossHitPoint(), DamageTint(), victimIsBoss: true);
        }

        void SpawnClosingImpact(PendingClosing p)
        {
            if (p.View == null || p.View.Logic == null)
                return;

            LivingEffect logic = p.View.Logic;
            Vector3 tip = new Vector3(logic.TipX, 0.6f, logic.TipZ);
            Vector3 origin = new Vector3(logic.OriginX, 0.55f, logic.OriginZ);
            string element = SelectedElementPaint?.Name
                ?? (p.Words != null && p.Words.Count > 0
                    ? p.Words[0].Rune.ToString()
                    : "Ates");

            string impactStyle = "burst_soft";
            string trailStyle = string.Empty;
            if (!p.IsBasicStrike && _skills != null && p.Words != null)
            {
                SkillResolution skill = ResolveSkillWords(p.Words);
                EnsurePresentationCatalog();
                var catalog = PresentationCatalog;
                if (catalog != null && !skill.IsEmpty)
                {
                    LivingEffectPlan plan = SkillWorldPlanner.Build(
                        skill, catalog,
                        _combat != null ? _combat.Manifestation : new ManifestationTuning());
                    if (!string.IsNullOrEmpty(plan.TrajectoryId)
                        && catalog.TryGetTrajectory(plan.TrajectoryId, out TrajectoryNode traj))
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
            LivingEffect capturedLogic = null,
            int slotCastId = -1,
            float? activationDelayOverride = null)
        {
            EnsureLaunchServices();
            return _executorLauncher.TryLaunch(
                kind, pending, skill, motionPlan, effectMult, capturedLogic, slotCastId, activationDelayOverride);
        }

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
            EnsureLaunchServices();
            _hitboxSizing.ApplyVerbHitboxSizing(
                kind, skill, tuning, rangeMult, burst, ref radius, ref range, ref durationSec, ref spawnCount);
        }

        void ApplyResourceCost(SkillResolution skill)
        {
            EnsureLaunchServices();
            _castSideEffects.ApplyResourceCost(skill);
        }

        void ApplyCastMobility(SkillResolution skill, float durationSec)
        {
            EnsureLaunchServices();
            _castSideEffects.ApplyCastMobility(skill, durationSec);
        }

        void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words)
        {
            EnsureLaunchServices();
            _castSideEffects.RefreshBuildingMobility(words);
        }

        void ApplyCooldown(SkillResolution skill, IReadOnlyList<SentenceWord> words, bool cosmeticIfDisabled)
        {
            EnsureLaunchServices();
            _castSideEffects.ApplyCooldown(skill, words, cosmeticIfDisabled);
        }

        SkillMotionPlan ResolveSkillMotion(SkillResolution skill)
        {
            EnsureLaunchServices();
            return _castSideEffects.ResolveSkillMotion(skill);
        }

        void ApplySkillMotionIframe(in SkillResolution skill, in SkillMotionPlan plan)
        {
            EnsureLaunchServices();
            _castSideEffects.ApplySkillMotionIframe(skill, plan);
        }

        void AnnotateMotion(SkillResolution skill, in SkillMotionPlan plan)
        {
            EnsureLaunchServices();
            _castSideEffects.AnnotateMotion(skill, plan);
        }

        void ShoutSkill(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            EnsureLaunchServices();
            _skillPresentation.ShoutSkill(skill, words);
        }

        void ApplySkillAnimation(SkillResolution skill)
        {
            EnsureLaunchServices();
            _skillPresentation.ApplySkillAnimation(skill);
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

        static bool IsFriendlyFieldVerb(in SkillResolution skill) =>
            skill.VerbId is "2" or "4" or "8" or "9";

        void ApplyClosingStatuses(PendingClosing p, SkillResolution skill, bool bossReached = true)
        {
            EnsureClosingServices();
            _closingStatus.Apply(p.Target, skill, bossReached);
        }

        void ApplyClosingHeal(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale = 1f,
            float? chainBonusOverride = null,
            Vector3? fieldCenter = null,
            float fieldRadiusM = 0f)
        {
            EnsureClosingServices();
            _closingHeal.Apply(closing, skill, effectScale, chainBonusOverride, fieldCenter, fieldRadiusM);
        }

        int CalculateClosingHealAmount(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale,
            float? chainBonusOverride)
        {
            EnsureClosingServices();
            return _closingHeal.CalculateAmount(closing, skill, effectScale, chainBonusOverride);
        }

        void ApplyClosingHealAmount(
            SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget = null)
        {
            EnsureClosingServices();
            _closingHeal.ApplyAmount(skill, amount, fieldCenter, fieldRadiusM, preferredTarget);
        }

        float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale = 1f,
            float? chainBonusOverride = null)
        {
            EnsureClosingServices();
            return _closingDamage.Apply(
                closing, skill, isBasicStrike, slashCommitMult, effectScale, chainBonusOverride);
        }

        void StampScar(LivingEffectView view, ClosingHit closing)
        {
            EnsureClosingServices();
            _closingDamage.StampScar(view, closing);
        }

        void ApplyBossClosingBasic(LivingEffect logic, ClosingHit closing)
        {
            EnsureClosingServices();
            _closingDamage.ApplyBossClosingBasic(logic, closing);
        }

        void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill)
        {
            EnsureClosingServices();
            _closingDamage.ApplyBossClosing(logic, closing, skill);
        }

        bool IsBossInStrikeCapsule(LivingEffect logic, float reachM)
        {
            EnsureClosingServices();
            return _closingDamage.IsBossInStrikeCapsule(logic, reachM);
        }

        bool BasicTargetStillInReach(Transform target, float reachM)
        {
            EnsureClosingServices();
            return _closingDamage.BasicTargetStillInReach(target, reachM);
        }

        float BasicStrikeYawDeg(Transform target)
        {
            EnsureClosingServices();
            return _closingDamage.BasicStrikeYawDeg(target);
        }

        bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            EnsureClosingServices();
            return _closingDamage.IsClosingInRange(logic, closing);
        }

        static bool IsHealSkill(SkillResolution skill) => ClosingHealRules.IsHealSkill(skill);

        static float FlatDistance(Vector3 a, Vector3 b) =>
            PlanarMath.FlatDistance(a.x, a.z, b.x, b.z);

        float PlayerBodyRadiusM() => _motor != null ? _motor.BodyRadiusM : 0f;

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
                    ClosingDamageCore.ClearClosingStamp(logic);
                    Destroy(view.gameObject);
                    _active.RemoveAt(i);
                }
            }
        }
    }
}
