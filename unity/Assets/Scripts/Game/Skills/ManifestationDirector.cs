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
        // --- Animasyon (Bağlama 10) — SkillPresentation → AnimationBridge; PulseRune kalır ---
        AnimationDatabase _animationDatabase;
        readonly HashSet<string> _missingAnimationBindings = new();
        readonly AnimationBridge _animationBridge = new();
        HexagonView _hexagonView;
        PlayerResource _playerResource;
        PlayerCooldown _playerCooldown;
        double _lastMovedMs = double.NegativeInfinity;

        /// <summary>PrototypeBootstrap'ın atadığı sabit silah (ör. Alev Kılıcı).</summary>
        public EquipmentItem EquippedWeapon => _equippedWeapon;

        public void ConfigureWeaponCycle(IReadOnlyList<EquipmentItem> weapons)
        {
            EnsureSkillServices();
            _weaponLoadout.ConfigureWeaponCycle(weapons);
        }

        public EquipmentItem CycleEquippedWeapon()
        {
            EnsureSkillServices();
            return _weaponLoadout.CycleEquippedWeapon();
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
        public ElementPaintNode? SelectedElementPaint
        {
            get
            {
                EnsureSkillServices();
                int index = _weaponLoadout.ElementPaintIndex;
                return _skills != null
                    && _skills.ElementPaints.Count > 0
                    && index >= 0
                    && index < _skills.ElementPaints.Count
                        ? _skills.ElementPaints[index]
                        : null;
            }
        }
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
            EnsureSkillServices();
            return _weaponLoadout.CycleElementPaint();
        }

        public bool TrySetElementPaint(int elementId)
        {
            EnsureSkillServices();
            return _weaponLoadout.TrySetElementPaint(elementId);
        }

        SkillMotor Skills => _skills ??= SkillMotorLoader.Load();

        WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill)
        {
            EnsureSkillServices();
            return _weaponLoadout.WeaponCompatibilityFor(skill);
        }

        public LivingEffect ActiveLogic => _buildingView?.Logic;

        public int ActiveCount
        {
            get
            {
                EnsureSkillServices();
                return _effectSpawner.ActiveCount;
            }
        }

        public Transform CurrentFacingTarget
        {
            get
            {
                EnsureSkillServices();
                return _skillAim.CurrentFacingTarget;
            }
        }

        /// <summary>
        /// Saldırı boyunca gövde çubuğa dönmez. Hedef varsa ona kilitlenir;
        /// yoksa bakış kalır. Saldırı dışında seçili hedef varsa eski kilit durur.
        /// </summary>
        public bool CombatFacingLocked
        {
            get
            {
                EnsureSkillServices();
                return _skillAim.CombatFacingLocked;
            }
        }

        bool PerformingAttack =>
            (_engine != null && (_engine.State.Phase == SentencePhase.Building
                || _engine.State.Phase == SentencePhase.Recovering))
            || _pending.Count > 0
            || (_motionBody != null && _motionBody.IsDisplacing)
            || (_visual != null && _visual.IsAttackPose);

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
            EnsureSkillServices();
            _weaponLoadout.ResetElementPaintIndex();
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
            {
                EnsureSkillServices();
                _skillAim.ResetDirectionalWhenIdle();
            }

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
            EnsureSkillServices();
            _effectSpawner.ApplyWindowCue();
            _effectSpawner.TickEffects(dtSec, worldMs);
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
            EnsureSkillServices();
            _sentenceBridge.SyncBuilding(worldMs);
        }

        bool TryArmSkillTarget(SkillResolution skill)
        {
            EnsureSkillServices();
            return _skillAim.TryArmSkillTarget(skill);
        }

        internal void FaceTarget(Transform target)
        {
            EnsureSkillServices();
            _skillAim.FaceTarget(target);
        }

        internal void CaptureBasicFacing()
        {
            EnsureSkillServices();
            _skillAim.CaptureBasicFacing();
        }

        internal Vector3 FlatBodyForward()
        {
            EnsureSkillServices();
            return _skillAim.FlatBodyForward();
        }

        internal Transform CastFacingTarget
        {
            get
            {
                EnsureSkillServices();
                return _skillAim.CastFacingTarget;
            }
        }

        void OnSentenceCompleted(CompletedSentence sentence)
        {
            EnsureSkillServices();
            _sentenceBridge.OnCompleted(sentence);
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
    }
}

