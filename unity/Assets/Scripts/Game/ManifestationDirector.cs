using System;
using System.Collections.Generic;
using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Motion;
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
        PlayerTargeting _targeting;
        Transform _armedTarget;
        Transform _castFacingTarget;
        bool _directionalAttack;
        string _armedSkillId = string.Empty;

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
        ActorStatus _playerStatus;
        ActorStatus _bossStatus;
        MotionTemplateBody _motionBody;
        AllyDummy _ally;

        // --- Slot pasifleri ---
        SlotPassiveDirector _slotPassives;
        int _slotQueryCastId;
        int _templateSlotCastId;
        bool _slotPassiveNeedsWeapon;
        int _passiveBonusDepth;
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
        public void DebugApplySkillAnimation(SkillResolution skill) => ApplySkillAnimation(skill);
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

        struct PendingClosing
        {
            public LivingEffectView View;
            public ClosingHit Closing;
            public double BangAtWorldMs;
            public List<SentenceWord> Words;
            public bool IsBasicStrike;
            public Transform Target;
            public SkillAimMode AimMode;
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
            EnsurePresentationCatalog();
            _playerStates = new PlayerStateMachine(_skills.PlayerStates);
            input.BindPlayerStates(_playerStates, () => _pending.Count > 0);
            var motorForStates = player != null ? player.GetComponent<KinematicMotor>() : null;
            motorForStates?.BindPlayerStates(_playerStates);
            _slotPassives = new SlotPassiveDirector();
            _slotPassiveNeedsWeapon = false;
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign slotDesign))
                _slotPassiveNeedsWeapon = PassiveSlotPolicy.RequiresWeaponCompatibility(
                    MiniJson.Parse(slotDesign.Json));
            _closingChainBonus = 1f;
            if (_playerStatus != null)
            {
                _playerStatus.SlotPassiveDirector = _slotPassives;
                _playerStatus.ReflectBossVitals = bossVitals;
                _playerStatus.IncomingDamageRedirect = RedirectMechanicDamage;
                _playerStatus.ReflectSink = ApplyReflectedDamage;
                _playerStatus.DamageTaken += OnPlayerDamageTaken;
                _playerStatus.DamageBlocked += _ => { NoteShieldBlockIfGuarding(); OnJsonShieldBlocked(); };
            }


            if (_engine != null && !_hooked)
            {
                _engine.SentenceCompleted += OnSentenceCompleted;
                _hooked = true;
            }
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

        void OnDestroy()
        {
            if (_engine != null && _hooked)
                _engine.SentenceCompleted -= OnSentenceCompleted;
            if (_playerStatus != null)
            {
                _playerStatus.DamageTaken -= OnPlayerDamageTaken;
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
            TickDelayedLaunches(worldMs);
            TickTemplateDelivery(worldMs);
            TickMechanics(worldMs);
            _animationBridge.Tick(worldMs);
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
                        adjective.EngineModifiers,
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
            bool isCasting = _pending.Count > 0 && !SwapDrawUnlocked(worldMs);
            bool isDrawing = _engine != null && _engine.State.Phase == SentencePhase.Building;
            bool isRecovering = _engine != null && _engine.State.Phase == SentencePhase.Recovering;

            _playerStates.SyncWorld(
                isDead, isStunned, isDodging, isRooted, isCasting, isDrawing, isRecovering);
        }

        void BeginBossDeathSequence(double worldMs)
        {
            _bossDirector?.NotifyBossDown(worldMs);
            float collapseSec = _colors != null ? _colors.BossDeathCollapseSec : 0.85f;
            _boss?.BeginCollapse(collapseSec, worldMs);
            _deathReviveAtMs = worldMs + collapseSec * 1000.0;
            _deathPending = true;
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
            if (_boss != null && FlatDistance(_player.position, _boss.transform.position) <= range)
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
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, _presentationCatalog, tuning);
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
            LivingEffectPlan plan = SkillWorldPlanner.Build(skill, _presentationCatalog, man);
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
                Transform impactTarget = p.Target;
                if (impactTarget == null || impactTarget == _player || !IsEnemyBody(impactTarget))
                {
                    CaptureBasicFacing();
                    impactTarget = _castFacingTarget;
                }
                // Zafiyet kalıbı bakışı ve kilidi bozar. Kilit boşsa boss menzildeyse o hedeftir;
                // yoksa ilk düz vuruş kapsülü ıskalayıp 0 yazar, ikincisi normal vurur.
                if ((impactTarget == null || !IsEnemyBody(impactTarget)) && _boss != null)
                    impactTarget = _boss.transform;
                FaceTarget(impactTarget);
                _closingChainBonus = 1f;

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
                double basicNow = _clock != null ? _clock.Director.WorldTimeMs : 0;
                if (!BasicCadenceReady(basicNow))
                {
                    _readout?.NoteDenied("Düz vuruş", "hazır değil");
                    return;
                }
                _lastBasicStrikeMs = basicNow;
                int basicHits = BasicHitsNow();

                float basicDealt = 0f;
                float basicReach = WeaponBasicReach(_combat.Manifestation.BasicStrikeRangeM);
                bool capsuleHit = IsBossInStrikeCapsule(logic, basicReach);
                bool inReach = BasicTargetStillInReach(impactTarget, basicReach);
                if (!inReach && _boss != null && impactTarget != _boss.transform)
                    inReach = BasicTargetStillInReach(_boss.transform, basicReach);
                float strikeArc = HitMods(SkillResolution.Empty, true, false).ArcDeg;
                float strikeDelta = BasicStrikeYawDeg(impactTarget);
                if (MeleeArc.StrikeConnects(capsuleHit, inReach, strikeDelta, strikeArc))
                {
                    if (logic != null)
                        ApplyBossClosingBasic(logic, p.Closing);
                    basicDealt = ApplyClosingDamage(
                        p.Closing, SkillResolution.Empty, isBasicStrike: true, slashCommitMult: 0f,
                        effectScale: JsonEffectRules.BasicSubHitScale(basicHits));
                    ScheduleBasicSubHits(p.Closing, basicHits, basicReach);
                    ApplyBasicExtras(basicDealt, basicHits);
                    TryLandWeaponStun(SkillResolution.Empty, true);
                }
                SpawnClosingImpact(p);
                if (logic != null)
                    TryCannonBlast(logic.TipX, logic.TipZ);
                return;
            }

            if (logic == null)
                return;

            _closingChainBonus = 1f;
            SkillResolution skill = ResolvePendingSkill(p);
            if (skill.IsEmpty || !skill.IsComplete)
            {
                _readout?.NoteDenied("2 rün gerekli");
                return;
            }

            // Dolu sayfa: sıra hasardan önce artsın. Her 3. skill bu vuruşta sayılır.
            NoteWeaponCast(skill);

            _slotQueryCastId = _slotPassives != null ? _slotPassives.OpenCast() : 0;
            try
            {
            WeaponSkillCompatibility compatibility = WeaponCompatibilityFor(skill);
            LastWeaponCompatible = compatibility.Compatible;
            LastWeaponPassiveEnabled = compatibility.PassiveEnabled;
            LastWeaponUiLabel = compatibility.UiLabel;
            if (PassiveSlotPolicy.ShouldArm(true, compatibility.PassiveEnabled, _slotPassiveNeedsWeapon))
                TryTriggerPassive(p.Words, _clock.Director.WorldTimeMs);

            ApplyResourceCost(skill);
            SkillMotionPlan motionPlan = ResolveSkillMotion(skill);
            bool templateOwnsDelivery = TryBeginMotionTemplate(skill, p);
            PortalBorderTeamHooks.NotifyCast(skill.SkillId);
            SkillExecutorRoute executorRoute = _skillExecutorRouter.Route(skill, _equippedWeapon);
            executorRoute = ApplyMechanicWorldRoute(MechanicPlanFor(skill), executorRoute);
            LastExecutorKind = executorRoute.Kind;
            ApplySelfCastEffects(skill);
            NoteJsonCast(skill, p.Closing);
            BeginMechanicPlan(
                skill,
                new Vector3(logic.DirX, 0f, logic.DirZ),
                new Vector3(logic.TipX, _player.position.y, logic.TipZ));
            if (templateOwnsDelivery)
                ArmTemplateDelivery(skill, p, motionPlan);

            bool executorStarted = !templateOwnsDelivery
                && executorRoute.Kind != SkillExecutorKind.Fallback
                && TryLaunchSkillExecutor(executorRoute.Kind, p, skill, motionPlan);
            if (executorStarted)
                ScheduleFollowUpLaunches(executorRoute.Kind, p, skill, motionPlan);
            float dealt = 0f;
            if (!executorStarted && !templateOwnsDelivery)
            {
                if (executorRoute.IsStub)
                    DebugConfig.DevLog($"[SkillExecutor] stub → LivingEffect: {executorRoute.Reason}");
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
                }
                ApplyClosingStatuses(p, skill, bossReached);
                if (bossReached)
                    ApplyMechanicHitEffects(LastMechanicPlan, new Vector3(logic.TipX, 0f, logic.TipZ));
                ApplyClosingHeal(p.Closing, skill);
            }

            ShoutSkill(skill, p.Words);
            ApplyCooldown(skill, p.Words, cosmeticIfDisabled: true);
            if (!motionPlan.IsEmpty)
                AnnotateMotion(skill, motionPlan);
            SpawnClosingImpact(p);
            LastResolvedSkillId = skill.SkillId;
            LastSkillEffectApplied = executorStarted
                || templateOwnsDelivery
                || dealt > 0f
                || IsHealSkill(skill)
                || !motionPlan.IsEmpty
                || skill.Mechanics.Length > 0;
            if (string.Equals(skill.SkillId, "1-1", StringComparison.Ordinal))
            {
                DebugConfig.DevLog(
                    $"[ElementSystem] smoke 1-1 effect applied={LastSkillEffectApplied} "
                    + $"damage={dealt:0.##}");
            }

            if (_slotPassives != null
                && _clock != null
                && _slotPassives.TryConsumeEcho(_slotQueryCastId, out float echoDelay, out float echoPower))
            {
                _passiveEchoes.Add(new PassiveEchoShot
                {
                    DueMs = _clock.Director.WorldTimeMs + echoDelay * 1000.0,
                    Power = echoPower,
                    SlotCastId = _slotQueryCastId,
                    Closing = p.Closing,
                    Skill = skill,
                    Slash = motionPlan.SlashCommitMult,
                    Chain = _closingChainBonus
                });
            }
            }
            finally
            {
                _slotPassives?.CloseCast();
                _slotQueryCastId = 0;
            }
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
            _damageHud?.ShowDamage(damage, false, BossHitPoint(), DamageTint());
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
            LivingEffect capturedLogic = null,
            int slotCastId = -1,
            float? activationDelayOverride = null)
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
            ResolveFieldTiming(skill, plan, tuning, out float durationSec, out float tickSec, out float perTickShare);
            int spawnCount = 1;
            ApplyVerbHitboxSizing(kind, skill, tuning, rangeMult, burst, ref radius, ref range, ref durationSec, ref spawnCount);
            string hitboxShape = TryVerbHitbox(skill, out VerbHitboxSpec visualSpec)
                ? visualSpec.Shape
                : "sphere";
            float hitboxAngleDeg = hitboxShape == "cone" ? visualSpec.SizeB : 0f;
            int elementId = SelectedElementPaint?.Id ?? 1;
            int.TryParse(skill.VerbId, out int verbVfxId);
            int.TryParse(skill.AdjectiveId, out int adjectiveVfxId);
            string vfxKey = _verbData?.VfxKey(
                SelectedElementPaint?.Name ?? elementId.ToString(),
                verbVfxId,
                adjectiveVfxId) ?? string.Empty;
            string vfxColorHex = SelectedElementPaint?.ColorHex ?? string.Empty;
            if (_verbData != null
                && _verbData.TryGetElementColor(elementId, out ElementVfxColor vfxColor)
                && !string.IsNullOrEmpty(vfxColor.Primary))
                vfxColorHex = vfxColor.Primary;

            Vector3 origin = _player.position;
            Vector3 direction = new(logic.DirX, 0f, logic.DirZ);
            Transform target = pending.Target;
            if (target == null && pending.AimMode == SkillAimMode.Targeted && _boss != null)
                target = _boss.transform;
            // Kendine doğan minyon/klon da düşmana yürür; hedefsiz aktör vurmaz ve boss'un içinde kalır.
            if (kind == SkillExecutorKind.Summon && _boss != null && !IsEnemyBody(target))
                target = _boss.transform;
            if (pending.AimMode == SkillAimMode.Targeted && target != null && target != _player)
            {
                Vector3 toTarget = target.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude > 0.0001f)
                    direction = toTarget.normalized;
            }
            bool friendly = IsFriendlyFieldVerb(skill) || kind == SkillExecutorKind.SelfState;
            Vector3 fieldCenter = pending.AimMode == SkillAimMode.Targeted && target != null
                ? target.position
                : friendly || hitboxShape == "cone"
                    ? origin
                    : new Vector3(logic.TipX, origin.y, logic.TipZ);
            float slashCommitMult = motionPlan.SlashCommitMult;
            float executorChainBonus = _closingChainBonus;
            string colorKey = SelectedElementPaint?.Name
                ?? (pending.Words != null && pending.Words.Count > 0
                    ? pending.Words[0].Rune.ToString()
                    : string.Empty);

            MechanicPlan mechanicPlan = MechanicPlanFor(skill);
            MechanicWorldProfile worldProfile = mechanicPlan != null
                ? MechanicWorldProfile.From(mechanicPlan)
                : null;
            if (kind == SkillExecutorKind.Summon && mechanicPlan != null)
            {
                MechanicEffect actorEffect = mechanicPlan.Effects.Find(
                    e => e.Stat is "aktor_yarat" or "klon");
                if (actorEffect != null && actorEffect.Amount > 0)
                    spawnCount = Mathf.Max(spawnCount, Mathf.RoundToInt((float)actorEffect.Amount));
            }
            float activationDelaySec = 0f;
            if (activationDelayOverride.HasValue)
                activationDelaySec = Mathf.Max(0f, activationDelayOverride.Value);
            else
            {
                if (worldProfile != null && worldProfile.RiseDelay && MechanicEngine != null)
                    activationDelaySec = Mathf.Max(
                        activationDelaySec,
                        (float)MechanicEngine.Rules.Param("rise_delay_sec"));
                if (worldProfile != null && worldProfile.DelayedMark && MechanicEngine != null)
                    activationDelaySec = Mathf.Max(
                        activationDelaySec,
                        (float)MechanicEngine.Rules.Param("mark_delay_sec"));
            }
            float tickEffectFraction = worldProfile != null && worldProfile.Continuous && MechanicEngine != null
                ? (float)MechanicEngine.Rules.Param("flow_tick_fraction")
                : 0f;
            if (tickEffectFraction <= 0f)
                tickEffectFraction = perTickShare;
            bool arcAllies = HitMods(skill, false, false).ArcAllies;
            bool statusesApplied = false;
            float accumulatedHealScale = 0f;
            int appliedHealAmount = 0;
            int castId = slotCastId >= 0 ? slotCastId : _slotQueryCastId;
            void ApplyExecutorEffect(float effectFraction)
            {
                if (effectFraction <= 0f)
                    return;
                int prevCast = _slotQueryCastId;
                bool prevRecoil = _casterRecoilSuppressed;
                _slotQueryCastId = castId;
                _casterRecoilSuppressed |= kind == SkillExecutorKind.Summon;
                try
                {
                if (!friendly)
                    ApplyBossClosing(logic, pending.Closing, skill);
                float hitDamage = ApplyClosingDamage(
                    pending.Closing,
                    skill,
                    isBasicStrike: false,
                    slashCommitMult,
                    effectFraction * effectMult,
                    executorChainBonus);
                if (!statusesApplied)
                {
                    // Dost/kendine alan düşmanca sıfat durumunu yalnız boss alanın içindeyse verir.
                    bool bossReached = !friendly
                        || BossWithin(_player != null ? _player.position : origin, radius);
                    if (worldProfile == null || !worldProfile.GuardTrigger)
                    {
                        ApplyClosingStatuses(pending, skill, bossReached);
                        if (bossReached)
                            ApplyMechanicHitEffects(mechanicPlan, fieldCenter);
                    }
                    statusesApplied = true;
                }
                if (IsHealSkill(skill) && (worldProfile == null || !worldProfile.GuardTrigger))
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
                            ? fieldCenter
                            : null;
                        ApplyClosingHealAmount(skill, delta, healCenter, radius, target);
                        appliedHealAmount += delta;
                    }
                }
                LastSkillEffectApplied = hitDamage > 0f
                    || IsHealSkill(skill)
                    || skill.Mechanics.Length > 0;
                }
                finally
                {
                    _slotQueryCastId = prevCast;
                    _casterRecoilSuppressed = prevRecoil;
                }
            }

            ApplyWeaponDelivery(
                skill, kind, target,
                ref origin, ref range, ref radius, ref hitboxShape, ref hitboxAngleDeg, ref speed);
            var context = new SkillExecutionContext(
                skill,
                _player,
                target,
                pending.AimMode,
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
                hitboxShape,
                hitboxAngleDeg,
                vfxKey,
                vfxColorHex,
                ApplyExecutorEffect,
                _clock,
                tuning,
                fieldCenter,
                applyFlatDamage: kind == SkillExecutorKind.Summon
                    ? raw =>
                    {
                        int prevCast = _slotQueryCastId;
                        _slotQueryCastId = castId;
                        try
                        {
                            float bindingDamage = MechanicEngine != null
                                ? (float)MechanicEngine.Rules.Param("minion_hit_damage")
                                : raw;
                            float dealt = ApplyMinionHit(skill, bindingDamage * effectMult);
                            bool drains = mechanicPlan != null && mechanicPlan.Effects.Exists(
                                e => e.Has("can_emen"));
                            if (drains && dealt > 0.5f && _player != null)
                            {
                                PlayerVitals vitals = _player.GetComponent<PlayerVitals>();
                                if (vitals != null)
                                    vitals.ApplyHeal(Mathf.RoundToInt(dealt));
                            }
                        }
                        finally
                        {
                            _slotQueryCastId = prevCast;
                        }
                    }
                    : null,
                spawnCount: spawnCount,
                mechanicPlan: mechanicPlan,
                activationDelaySec: activationDelaySec,
                tickEffectFraction: tickEffectFraction,
                arcAllies: arcAllies);

            var go = new GameObject($"{kind}_{skill.SkillId}");
            go.transform.SetParent(transform, false);
            ISkillExecutor executor = kind switch
            {
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
            DebugConfig.DevLog($"[SkillExecutor] {skill.SkillId} → {kind} r={radius:0.##} menzil={range:0.##} süre={durationSec:0.##} x{effectMult:0.##}");
            return true;
        }

        /// <summary>
        /// hitbox_vfx.fiil_hitbox boyutları: final = base × weapon.range_mult × sifat_override.size_mult.
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
            int.TryParse(skill.AdjectiveId, out int adjectiveId);
            int weaponId = EquippedWeaponNumber();
            float weaponScale = _verbData?.WeaponSizeMult(weaponId, rangeMult) ?? rangeMult;
            JsonValue engine = skill.EngineModifiers;
            float tableScale = _verbData?.AdjectiveSizeMult(adjectiveId) ?? 1f;
            float adjectiveScale = HitboxSizing.AdjectiveScale(tableScale, engine["hitbox_scale_mult"].AsFloat(0f));
            adjectiveScale *= _slotPassives?.HitboxSizeMultFor(_slotQueryCastId) ?? 1f;
            HitboxSize size = HitboxSizing.Resolve(spec, weaponScale, adjectiveScale);
            float lifetimeAdd = Mathf.Max(0f, engine["lifetime_add"].AsFloat(0f));
            float slotLife = _slotPassives?.LifetimeAddSecFor(_slotQueryCastId) ?? 0f;

            switch (kind)
            {
                case SkillExecutorKind.MeleeHitbox:
                    range = size.ReachM;
                    radius = size.RadiusM;
                    break;

                case SkillExecutorKind.Projectile:
                    radius = size.RadiusM;
                    range = size.ReachM;
                    break;

                case SkillExecutorKind.FieldAura:
                    radius = spec.IsRadius ? size.RadiusM : size.ReachM;
                    range = size.ReachM;
                    durationSec += slotLife;
                    break;

                case SkillExecutorKind.Movement:
                    radius = size.RadiusM;
                    float dashSec = _combat != null ? _combat.SkillMotion.DashDurationSec : 0f;
                    durationSec = Mathf.Max(size.DurationSec, dashSec);
                    break;

                case SkillExecutorKind.SelfState:
                    radius = size.RadiusM;
                    range = size.ReachM;
                    float stateSec = engine["reflect_duration_sec"].AsFloat(0f);
                    if (stateSec > 0f)
                        durationSec = stateSec + lifetimeAdd;
                    break;

                case SkillExecutorKind.Summon:
                    radius = size.RadiusM;
                    range = size.ReachM;
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
            out float tickSec,
            out float perTickShare)
        {
            durationSec = 0f;
            tickSec = tuning.ExecutorFieldTickSec;
            perTickShare = 1f;
            if (_presentationCatalog != null
                && _presentationCatalog.TryGetHitbox(plan.HitboxId, out HitboxNode hitbox))
            {
                durationSec = hitbox.GetFloat("lifetime_sec_default", 0f);
                tickSec = hitbox.GetFloat("tick_interval_sec", tickSec);
            }

            JsonValue engine = skill.EngineModifiers;
            float tickRateMult = Mathf.Max(0.01f, engine["tick_rate_mult"].AsFloat(1f));
            tickSec /= tickRateMult;
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
            float shareTick = Mathf.Clamp(tickSec, 0.01f, Mathf.Max(0.01f, durationSec));
            perTickShare = SustainedField.PerTickShare(durationSec, shareTick);
            durationSec *= WeaponDurationMult(skill);
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
            _playerResource.Consume(cost, TryTakeFreeMana());
        }

        /// <summary>
        /// mobility_cc fiil + sıfat + silah çözümü → oyuncu Slow/Root (KinematicMotor okur).
        /// </summary>
        void ApplyCastMobility(SkillResolution skill, float durationSec)
        {
            if (_playerStatus == null || skill.IsEmpty || durationSec <= 0f)
                return;

            string mob;
            if (_mobilityCc != null
                && int.TryParse(skill.VerbId, out int verbId)
                && int.TryParse(skill.AdjectiveId, out int adjectiveId))
            {
                int weaponId = 0;
                if (_equippedWeapon != null)
                {
                    string id = _equippedWeapon.Id ?? string.Empty;
                    int colon = id.LastIndexOf(':');
                    int.TryParse(colon >= 0 ? id.Substring(colon + 1) : id, out weaponId);
                }
                mob = _mobilityCc.ResolveMobility(
                    verbId, adjectiveId, weaponId, _equippedWeapon?.MobilityMod ?? 0);
            }
            else
            {
                mob = SkillMobility.Resolve(skill);
            }
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0;
            _playerStatus.GrantCastMobility(mob, now + durationSec * 1000.0);
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

            float sec = skill.BaseCooldownSec * WeaponCooldownMult();
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
            float sec = skill.BaseCooldownSec * WeaponCooldownMult();
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
                _verbData?.IFrameMsFor(skill.SkillId) ?? 0);
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
            MechanicPlan mechanic = LastMechanicPlan;
            if (mechanic != null && string.Equals(mechanic.SkillId, skill.SkillId, StringComparison.Ordinal))
            {
                string title = MechanicDescriber.ShortTitle(mechanic);
                bangNote = string.IsNullOrEmpty(bangNote) ? title : title + " | " + bangNote;
            }
            _debugHud?.NoteSkillBang(paintedName, bangNote);
            // Skill adı altıgen üstündeki SkillPreviewHud'da; büyük ReactionReadout dodge/tepki içindir.
            SkillFeel.CameraKick(skill.VerbFamily, _camera, _combat?.Feel);
            // PulseRune (PulseActor) kalır — AnimationBridge eklenir, yerine geçmez.
            SyncVisualDelivery();
            ApplySkillAnimation(skill);
            StartCastVfxTimer(skill, words);
            SfxDirector.Play(SfxLibrary.CastPrefix + skill.VerbFamily);
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
            bool targetsAlly = _ally != null && p.Target == _ally.transform;
            if (targetsAlly)
                _ally.EnsureStatusBoard();
            StatusBoard friendlyBoard = targetsAlly
                ? _ally.Board
                : _playerStatus != null ? _playerStatus.Board : null;
            if (friendlyBoard == null && bossStatus == null)
                return;

            if (_ally != null)
                _ally.EnsureStatusBoard();
            float friendlyScale = WeaponFriendlyScale();
            var result = StatusApplicator.ApplySkill(
                skill,
                friendlyBoard,
                bossStatus != null ? bossStatus.Board : null,
                _combat != null ? _combat.Status : new StatusTuning(),
                _mobilityCc,
                friendlyScale,
                cleanseCount: JsonCleanseCount(skill));
            _lastFriendlyWasAlly = targetsAlly;
            ShareFriendlyStatuses(skill, friendlyBoard);
            ApplyPurgePower(skill, result.CleansedCount);

            // v6 Zaman fiili yalnız aktör durumudur; GameClock/Time.timeScale'a dokunmaz.
            // Süre ve güç kart/JSON'dan gelir. Kart kendine hız diyorsa düşmana yavaş inmez.
            if (string.Equals(skill.Action, "tempo", StringComparison.Ordinal))
            {
                TempoCast.From(skill).Apply(
                    _playerStatus != null ? _playerStatus.Board : friendlyBoard,
                    bossStatus != null ? bossStatus.Board : null,
                    _ally != null ? _ally.Board : null,
                    friendlyScale);
            }

            ApplyArmorShred(skill, bossStatus);
            ApplySlotPassiveOnHit(bossStatus);
            if (bossStatus != null)
                ApplyElementStatusToBoss(bossStatus.Board);

            if (result.Knockback && bossStatus != null && _player != null)
                bossStatus.ApplyKnockbackFrom(_player.position);

            if (result.Pull && bossStatus != null && _player != null)
                bossStatus.ApplyPullToward(_player.position);
        }

        void ApplySlotPassiveOnHit(ActorStatus target)
        {
            if (target == null || _slotPassives == null || _slotPassives.ActiveCount == 0)
                return;
            StatusTuning tuning = _combat != null ? _combat.Status : new StatusTuning();
            int castId = _slotQueryCastId;
            float rootSec = _slotPassives.RootSecondsFor(castId);
            if (rootSec > 0f)
                target.Board.Apply(StatusKind.Root, rootSec * 1000.0, 1f, "passive:root");
            float speed = _slotPassives.SlowSpeedFor(castId);
            if (speed < 0.999f)
                target.Board.Apply(
                    StatusKind.Slow,
                    _mobilityCc?.ResolveCcDurationMs(StatusKind.Slow, 0, tuning.SlowMs) ?? tuning.SlowMs,
                    speed,
                    "passive:slow");
            float accuracy = _slotPassives.AccuracyDebuffFor(castId);
            if (accuracy > 0f)
            {
                double blindMs = BossStatusMath.BlindDurationMs(
                    _mobilityCc?.ResolveCcDurationMs(StatusKind.Blind, 0, tuning.BlindMs) ?? tuning.BlindMs,
                    _slotPassives.AccuracyLifetimeAddSecFor(castId));
                target.Board.Apply(
                    StatusKind.Blind,
                    blindMs,
                    BossStatusMath.BlindChanceFromAccuracy(accuracy),
                    "passive:blind");
            }
        }

        /// <summary>Seçili elementin boss'a giden durumu (Ateş burn, Karanlık weaken). Süre JSON'dan.</summary>
        void ApplyElementStatusToBoss(StatusBoard boss)
        {
            ElementPaintNode? paint = SelectedElementPaint;
            if (!paint.HasValue || boss == null)
                return;
            ElementPaintNode node = paint.Value;
            if (!ElementBossStatusRules.TryForBoss(node.Status, node.StatusEffect, node.StatusDurationSec, out ElementBossStatus apply))
                return;
            boss.Apply(apply.Kind, apply.DurationMs, apply.Magnitude, "element:" + node.Id);
        }

        void ApplySlotPassiveHitExtras(float dealt)
        {
            if (_passiveBonusDepth > 0 || dealt <= 0f || _slotPassives == null)
                return;
            _passiveBonusDepth++;
            try
            {
                ApplySlotBounce(dealt);
                StartSlotFlow(dealt);
            }
            finally
            {
                _passiveBonusDepth--;
            }
        }

        void ApplySlotBounce(float dealt)
        {
            int castId = _slotQueryCastId;
            int count = _slotPassives.BounceCountFor(castId);
            float mult = _slotPassives.BounceDamageMultFor(castId);
            if (count <= 0 || mult <= 0f)
                return;
            int sourceId = _boss != null ? _boss.GetInstanceID() : 0;
            var candidates = new List<PassiveBounceCandidate>();
            Vector3 from = _boss != null ? _boss.transform.position : (_player != null ? _player.position : Vector3.zero);
            Targetable[] bodies = FindObjectsByType<Targetable>(FindObjectsSortMode.None);
            for (int i = 0; i < bodies.Length; i++)
            {
                Targetable body = bodies[i];
                if (body == null || !IsEnemyBody(body.transform))
                    continue;
                if (_boss != null && (body.transform == _boss.transform || body.transform.IsChildOf(_boss.transform)))
                    continue;
                candidates.Add(new PassiveBounceCandidate(
                    body.GetInstanceID(),
                    body.DistanceFrom(from)));
            }

            List<PassiveBounceHit> hits = PassiveBounce.Plan(dealt, count, mult, sourceId, candidates);
            for (int i = 0; i < hits.Count; i++)
                ApplyPassiveBonusHit(hits[i]);
        }

        void ApplyPassiveBonusHit(PassiveBounceHit hit)
        {
            if (hit.Damage <= 0f || _bossVitals == null || _bossVitals.IsDown)
                return;
            bool bossHit = _boss == null
                || hit.TargetId == 0
                || hit.TargetId == _boss.GetInstanceID();
            if (!bossHit)
                return;
            _bossVitals.ApplyDamage(hit.Damage);
            _damageHud?.ShowDamage(hit.Damage, false, BossHitPoint(), DamageTint());
        }

        void StartSlotFlow(float dealt)
        {
            int castId = _slotQueryCastId;
            float channel = _slotPassives.ChannelSecFor(castId);
            if (channel <= 0f)
                return;
            float rate = _slotPassives.TickRateMultFor(castId);
            float baseTick = _combat != null ? _combat.Manifestation.ExecutorFieldTickSec : 1f;
            float fraction = PassiveFlowMath.DefaultTickFraction;
            if (MechanicEngine != null)
            {
                double fromJson = MechanicEngine.Rules.Param("flow_tick_fraction");
                if (fromJson > 0d)
                    fraction = (float)fromJson;
            }
            if (!PassiveFlowMath.TryPlan(channel, rate, dealt, baseTick, fraction, out PassiveFlowPlan plan))
                return;
            double now = _clock != null ? _clock.Director.WorldTimeMs : 0d;
            _passiveFlows.Start(plan, now);
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
            float chain = chainBonusOverride ?? _closingChainBonus;
            // Tılsım şifa/kalkan/güç %120. Boru bunu bir daha çarpmaz.
            float weapon = WeaponSupportPower(skill);
            float healBase = skill.BaseHeal > 0f
                ? skill.BaseHeal
                : closing.TotalEffect * per;
            return Mathf.Max(0, Mathf.RoundToInt(healBase * chain * weapon * effectScale));
        }

        void ApplyClosingHealAmount(
            SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget = null)
        {
            if (amount <= 0)
                return;
            DamageOutcome healedBy = DamagePipeline.Resolve(new DamageQuery
            {
                Heal = true,
                HealPower = amount,
                HealMultiplier = HealBuffMultiplier(skill),
                ScaleMagnitudes = true
            });
            amount = Mathf.Max(0, Mathf.RoundToInt(healedBy.Amount));
            if (_playerStatus != null)
                _playerStatus.LastThreat = healedBy.Threat;
            if (amount <= 0)
                return;

            var playerVitals = _player != null ? _player.GetComponent<PlayerVitals>() : null;
            bool spatial = fieldCenter.HasValue && fieldRadiusM > 0f;
            bool preferAlly = _ally != null && preferredTarget == _ally.transform;
            // Seçilen dost, dost menzili kapısından geçti. Silahın dar alanı onu elemez.
            bool allyInRange = preferAlly || !spatial || (_ally != null
                && FlatDistance(_ally.transform.position, fieldCenter.Value) <= fieldRadiusM);
            bool selfInRange = !spatial || (_player != null
                && FlatDistance(_player.position, fieldCenter.Value) <= fieldRadiusM);
            bool allyNeeds = _ally != null && allyInRange && _ally.Hp < _ally.MaxHp;
            bool selfNeeds = playerVitals != null && selfInRange
                && !playerVitals.IsDown && playerVitals.Hp < playerVitals.MaxHp;
            bool preferSelf = _player != null && preferredTarget == _player;
            if ((preferAlly && !allyNeeds) || (preferSelf && !selfNeeds))
            {
                _readout?.NoteSkill(skill.DisplayName, "zaten full", new Color(0.7f, 0.9f, 0.75f));
                return;
            }
            if (!allyNeeds && !selfNeeds)
            {
                _readout?.NoteSkill(skill.DisplayName, "zaten full", new Color(0.7f, 0.9f, 0.75f));
                ApplyHealOverflow(skill, amount, 0, false);
                return;
            }

            JsonEffectRules.SelectHealTargets(
                allyNeeds,
                selfNeeds,
                allyNeeds ? _ally.Ratio : 1f,
                selfNeeds ? (float)playerVitals.Hp / playerVitals.MaxHp : 1f,
                preferAlly,
                preferSelf,
                FriendlyTargetCap(skill),
                out bool healAlly,
                out bool healSelf);

            if (healAlly)
            {
                int healedAlly = _ally.ApplyHeal(amount);
                _lastFriendlyWasAlly = true;
                if (healedAlly > 0)
                {
                    _damageHud?.ShowDamage(-healedAlly);
                    _readout?.NoteSkill(skill.DisplayName, "ally +" + healedAlly, new Color(0.4f, 1f, 0.65f));
                    _debugHud?.NoteSkillBang(skill.DisplayName, "ally +" + healedAlly);
                    _ally.EnsureStatusBoard();
                    ConsumeWeaponBonus(_ally.Board);
                }
                ApplyHealOverflow(skill, amount, healedAlly, true);
            }
            if (!healSelf)
                return;

            int healed = playerVitals.ApplyHeal(amount);
            _lastFriendlyWasAlly = false;
            if (healed > 0)
            {
                ConsumeWeaponBonus(_playerStatus != null ? _playerStatus.Board : null);
                _damageHud?.ShowDamage(-healed);
                _readout?.NoteSkill(skill.DisplayName, "self +" + healed, new Color(0.4f, 1f, 0.65f));
                _debugHud?.NoteSkillBang(skill.DisplayName, "self +" + healed);
            }
            ApplyHealOverflow(skill, amount, healed, false);
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

            DamageOutcome dealt = ComputeOutgoingHit(
                closing, skill, isBasicStrike, slashCommitMult, effectScale, chainBonusOverride);
            if (dealt.Poise > 0f)
                _bossDirector?.ApplyPoiseDamage(dealt.Poise);
            float damage = dealt.Amount;
            bool isCrit = dealt.WasCrit;

            if (damage <= 0f)
            {
                LastClosingDamageDealt = 0f;
                return 0f;
            }

            if (!isBasicStrike)
                TryConsumeCounterWindow();

            RememberHitPoint(BossHitPoint());
            if (!isBasicStrike && !_jsonTickDamage)
                TryCannonBlast(_lastHitX, _lastHitZ);
            ConsumeWeaponBonus(_bossStatus != null ? _bossStatus.Board : null);
            LastClosingDamageDealt = damage;
            _damageHud?.ShowDamage(damage, isCrit, BossHitPoint(), DamageTint());

            float lifesteal = _slotPassives?.LifestealAddFor(_slotQueryCastId) ?? 0f;
            lifesteal += AdjectiveLifesteal(skill);
            lifesteal += PortalBorderTeamHooks.LifestealAdd;
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
                BeginBossDeathSequence(worldMs);
                return damage;
            }

            NotifyBossStruck(isCrit, allowHitstop: true);
            bossVisual?.PlayStagger();
            ApplySlotPassiveHitExtras(damage);
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
            float add = 0f;
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
            if (logic == null)
                return;
            NoteImpactOrigin(logic);
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
            NoteImpactOrigin(logic);
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
        /// Göğüs hizasında, gövde kenarından bakış yönüne uçlar dahil reach boyunda kapsül
        /// (<see cref="StrikeCapsule"/>). Kapsül oyuncunun arkasına taşmaz; boss'un gerçek
        /// collider'ı temas etmeli.
        /// </summary>
        bool IsBossInStrikeCapsule(LivingEffect logic, float reachM)
        {
            if (_boss == null || _player == null || logic == null)
                return false;
            ManifestationTuning man = _combat.Manifestation;
            float radius = man.BasicStrikeRadiusM;
            Vector3 dir = new Vector3(logic.DirX, 0f, logic.DirZ);
            if (dir.sqrMagnitude < 0.0001f)
                return false;
            dir.Normalize();
            StrikeCapsule.Segment(PlayerBodyRadiusM(), reachM, radius, out float nearM, out float farM);
            Vector3 chest = _player.position + Vector3.up * man.StrikeChestOffsetM;
            Vector3 low = chest + dir * nearM;
            Vector3 high = chest + dir * farM;
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

        /// <summary>
        /// Düz vuruş menzilde kilitlediği boss hâlâ menzildeyse kapsül ıskalasa da vurur.
        /// Hasar yolu hâlâ yalnız boss'a gider; ikinci düşman bu prototipte yok.
        /// </summary>
        bool BasicTargetStillInReach(Transform target, float reachM)
        {
            if (target == null || _player == null || _boss == null)
                return false;
            if (target != _boss.transform && !target.IsChildOf(_boss.transform))
                return false;
            Targetable mark = target.GetComponent<Targetable>();
            if (mark == null)
                mark = target.GetComponentInParent<Targetable>();
            if (mark != null && !mark.IsAvailable)
                return false;
            float dist = mark != null
                ? mark.DistanceFrom(_player.position)
                : FlatDistance(_player.position, target.position);
            return StrikeCapsule.EdgeInReach(dist, PlayerBodyRadiusM(), reachM);
        }

        float BasicStrikeYawDeg(Transform target)
        {
            if (_player == null || target == null)
                return 0f;
            Vector3 to = target.position - _player.position;
            to.y = 0f;
            Vector3 fwd = _player.forward;
            fwd.y = 0f;
            if (to.sqrMagnitude < 0.0001f || fwd.sqrMagnitude < 0.0001f)
                return 0f;
            return Vector3.Angle(fwd, to);
        }

        float PlayerBodyRadiusM() => _motor != null ? _motor.BodyRadiusM : 0f;

        bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            if (logic == null || _boss == null)
                return false;
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
