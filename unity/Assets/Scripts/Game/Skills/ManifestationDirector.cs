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

        public void BindTargeting(PlayerTargeting targeting)
        {
            _targeting = targeting;
            _input?.BindSkillTargetGate(TryArmSkillTarget);
            _motor?.BindCombatFacing(() => CurrentFacingTarget, () => CombatFacingLocked);
        }

        internal void WireBossVitalsEvents(BossVitals previous, BossVitals vitals)
        {
            if (previous != null)
            {
                previous.Died -= OnBossDied;
                previous.Revived -= OnBossRevivedExternally;
            }
            _bossVitals = vitals;
            if (_bossVitals != null)
            {
                _bossVitals.Died -= OnBossDied;
                _bossVitals.Died += OnBossDied;
                _bossVitals.Revived -= OnBossRevivedExternally;
                _bossVitals.Revived += OnBossRevivedExternally;
            }
        }

        internal void WireBossStatusDot(ActorStatus previous, ActorStatus bossStatus)
        {
            if (previous != null)
                previous.DamageOverTimeDealt -= OnBossDamageOverTime;
            _bossStatus = bossStatus;
            if (_bossStatus != null)
                _bossStatus.DamageOverTimeDealt += OnBossDamageOverTime;
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
            EnsureCoreServices();
            _bossDeathSequence.OnPlayerDamageBlocked(absorbed);
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
            EnsureCoreServices();
            _bossDeathSequence.OnBossRevivedExternally();
        }

        /// <summary>S4: boss'taki yanma/zehir tikinin hasar sayısı.</summary>
        void OnBossDamageOverTime(float amount)
        {
            EnsureCoreServices();
            _bossDeathSequence.OnBossDamageOverTime(amount);
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

            EnsureCoreServices();
            _sentenceSync.SyncFromSentence(worldMs);
            EnsureSkillServices();
            _effectSpawner.ApplyWindowCue();
            _effectSpawner.TickEffects(dtSec, worldMs);
            _pose?.Tick(worldMs);
            _boss?.Tick(dtSec, worldMs);
            _closingQueue.TickPendingClosings(worldMs);
            _bossDeathSequence.TickBossDeath();
            _slotPassiveRuntime.Tick(worldMs);
            _sentenceSync.SyncPlayerStateMachine(worldMs);
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
            EnsureCoreServices();
            bool channelHeld = SustainedSkillActive(worldMs);
            bool guardHeld = _playerStatus != null && _playerStatus.Board.ShieldRemaining > 0.01f;
            _closingQueue.TickCastHold(worldMs, _visual, channelHeld, guardHeld);
        }

        void TryTriggerPassive(IReadOnlyList<SentenceWord> words, double worldMs)
        {
            EnsureCoreServices();
            _slotPassiveRuntime.TryTriggerPassive(words, worldMs);
        }

        /// <summary>Yalnız <see cref="OnBossDied"/>'dan (K1 tek kanca).</summary>
        void BeginBossDeathSequence(double worldMs)
        {
            EnsureCoreServices();
            _bossDeathSequence.Begin(worldMs);
        }

        void SyncFromSentence(double worldMs)
        {
            EnsureCoreServices();
            _sentenceSync.SyncFromSentence(worldMs);
        }

        void SyncPlayerStateMachine(double worldMs)
        {
            EnsureCoreServices();
            _sentenceSync.SyncPlayerStateMachine(worldMs);
        }

        void OnSentenceCompleted(CompletedSentence sentence)
        {
            EnsureCoreServices();
            _closingQueue.OnSentenceCompleted(sentence);
        }

#if UNITY_EDITOR
        /// <summary>Editör/prob: kapanış bang zamanını zorla işle (heal vb.).</summary>
        public void ForceTickClosings()
        {
            if (_clock == null)
                return;
            EnsureCoreServices();
            _closingQueue.ForceTickClosings(_clock.Director.WorldTimeMs);
        }

        /// <summary>MCP: bekleyen kapanışları hemen ateşle (bang'i şimdiye çeker).</summary>
        public void ForceFirePendingClosings()
        {
            if (_clock == null)
                return;
            EnsureCoreServices();
            _closingQueue.ForceFirePendingClosings(_clock.Director.WorldTimeMs);
        }
#endif

        static float FlatDistance(Vector3 a, Vector3 b) =>
            PlanarMath.FlatDistance(a.x, a.z, b.x, b.z);

        float PlayerBodyRadiusM() => _motor != null ? _motor.BodyRadiusM : 0f;
    }
}

