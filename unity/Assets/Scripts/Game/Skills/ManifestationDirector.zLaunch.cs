using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills.Launch;
using Dovus.Game.Skills.Presentation;
using Dovus.App.Casting;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        LaunchServicesHost _launchHost;
        SkillExecutorLauncher _executorLauncher;
        HitboxSizingApplier _hitboxSizing;
        CastSideEffects _castSideEffects;
        SkillPresentation _skillPresentation;

        void EnsureLaunchServices()
        {
            if (_launchHost != null)
                return;
            _launchHost = new LaunchServicesHost(this);
            _hitboxSizing = new HitboxSizingApplier(_launchHost);
            _executorLauncher = new SkillExecutorLauncher(_launchHost, _hitboxSizing);
            _castSideEffects = new CastSideEffects(_launchHost);
            _skillPresentation = new SkillPresentation(_launchHost);
        }

        PresentationCatalog PresentationCatalog
        {
            get
            {
                EnsureLaunchServices();
                _skillPresentation.EnsureCatalog();
                return _skillPresentation.Catalog;
            }
        }

        void EnsurePresentationCatalog()
        {
            EnsureLaunchServices();
            _skillPresentation.EnsureCatalog();
        }

        sealed class LaunchServicesHost
            : ISkillExecutorLaunchHost,
                ICastSideEffectsHost,
                ISkillPresentationHost,
                IWeaponDurationMultHost
        {
            readonly ManifestationDirector _md;

            internal LaunchServicesHost(ManifestationDirector md) => _md = md;

            public Transform Player => _md._player;
            public Transform DirectorTransform => _md.transform;
            public GameClock Clock => _md._clock;
            public CombatTuning Combat => _md._combat;
            public PresentationCatalog PresentationCatalog => _md._skillPresentation.Catalog;
            public void EnsurePresentationCatalog() => _md._skillPresentation.EnsureCatalog();
            public EquipmentItem EquippedWeapon => _md._equippedWeapon;
            public VerbExecutionData VerbData => _md._verbData;
            public ElementPaintNode? SelectedElementPaint => _md.SelectedElementPaint;
            public Transform BossTransform => _md._boss != null ? _md._boss.transform : null;
            public MechanicGrammar MechanicEngine => _md.MechanicEngine;
            public float ClosingChainBonus => _md._closingChainBonus;
            public int SlotQueryCastId
            {
                get => _md._slotQueryCastId;
                set => _md._slotQueryCastId = value;
            }
            public bool CasterRecoilSuppressed
            {
                get => _md._casterRecoilSuppressed;
                set => _md._casterRecoilSuppressed = value;
            }
            public bool LastSkillEffectApplied
            {
                set => _md.LastSkillEffectApplied = value;
            }
            public SlotPassiveDirector SlotPassives => _md._slotPassives;

            public bool TryVerbHitbox(in SkillResolution skill, out VerbHitboxSpec spec) =>
                _md.TryVerbHitbox(skill, out spec);
            public bool IsEnemyBody(Transform body) => _md.IsEnemyBody(body);
            public bool IsFriendlyFieldVerb(in SkillResolution skill) =>
                skill.VerbId is "2" or "4" or "8" or "9";
            public MechanicPlan MechanicPlanFor(SkillResolution skill) => _md.MechanicPlanFor(skill);
            public WeaponPassiveMods HitMods(in SkillResolution skill, bool isBasicStrike, bool consumeBonus) =>
                _md.HitMods(skill, isBasicStrike, consumeBonus);
            public bool BossWithin(Vector3 center, float radiusM) => _md.BossWithin(center, radiusM);
            public bool IsHealSkill(SkillResolution skill) => ClosingHealRules.IsHealSkill(skill);
            public int EquippedWeaponNumber() => _md.EquippedWeaponNumber();

            public void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill) =>
                _md.ApplyBossClosing(logic, closing, skill);
            public float ApplyClosingDamage(
                ClosingHit closing,
                SkillResolution skill,
                bool isBasicStrike,
                float slashCommitMult,
                float effectScale,
                float? chainBonusOverride) =>
                _md.ApplyClosingDamage(
                    closing, skill, isBasicStrike, slashCommitMult, effectScale, chainBonusOverride);
            public void ApplyClosingStatuses(PendingClosing pending, SkillResolution skill, bool bossReached) =>
                _md.ApplyClosingStatuses(pending, skill, bossReached);
            public void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 fieldCenter) =>
                _md.ApplyMechanicHitEffects(plan, fieldCenter);
            public int CalculateClosingHealAmount(
                ClosingHit closing,
                SkillResolution skill,
                float effectScale,
                float? chainBonusOverride) =>
                _md.CalculateClosingHealAmount(closing, skill, effectScale, chainBonusOverride);
            public void ApplyClosingHealAmount(
                SkillResolution skill,
                int amount,
                Vector3? fieldCenter,
                float fieldRadiusM,
                Transform preferredTarget) =>
                _md.ApplyClosingHealAmount(skill, amount, fieldCenter, fieldRadiusM, preferredTarget);
            public void ApplyWeaponDelivery(
                SkillResolution skill,
                SkillExecutorKind kind,
                Transform target,
                ref Vector3 origin,
                ref float range,
                ref float radius,
                ref string hitboxShape,
                ref float hitboxAngleDeg,
                ref float speed) =>
                _md.ApplyWeaponDelivery(
                    skill, kind, target, ref origin, ref range, ref radius, ref hitboxShape, ref hitboxAngleDeg, ref speed);
            public void ApplySpawnIFrame(in SkillResolution skill) => _md.ApplySpawnIFrame(skill);
            public float ApplyMinionHit(SkillResolution skill, float bindingDamage) =>
                _md.ApplyMinionHit(skill, bindingDamage);
            public PlayerVitals CachedPlayerVitals() => _md.CachedPlayerVitals();

            public void ApplyVerbHitboxSizing(
                SkillExecutorKind kind,
                in SkillResolution skill,
                ManifestationTuning tuning,
                float rangeMult,
                bool burst,
                ref float radius,
                ref float range,
                ref float durationSec,
                ref int spawnCount) =>
                _md._hitboxSizing.ApplyVerbHitboxSizing(
                    kind, skill, tuning, rangeMult, burst, ref radius, ref range, ref durationSec, ref spawnCount);

            public void ResolveFieldTiming(
                in SkillResolution skill,
                in LivingEffectPlan plan,
                ManifestationTuning tuning,
                out float durationSec,
                out float tickSec,
                out float perTickShare) =>
                _md._hitboxSizing.ResolveFieldTiming(skill, plan, tuning, out durationSec, out tickSec, out perTickShare);

            public void DestroyUnityObject(Object obj) => Destroy(obj);
            public ISkillExecutor AddSummonExecutor(GameObject go) => go.AddComponent<SummonExecutor>();

            public PlayerResource PlayerResource => _md._playerResource;
            public ActorStatus PlayerStatus => _md._playerStatus;
            public MobilityCcData MobilityCc => _md._mobilityCc;
            public SkillMotor Skills => _md._skills;
            public SkillFactory SkillFactory => _md._skillFactory;
            public HexagonView HexagonView => _md._hexagonView;
            public PlayerCooldown PlayerCooldown => _md._playerCooldown;
            public BossVitals BossVitals => _md._bossVitals;
            public PrototypeTuning Colors => _md._colors;
            public Skill LastFactorySkill
            {
                get => _md.LastFactorySkill;
                set => _md.LastFactorySkill = value;
            }
            public MechanicPlan LastMechanicPlan => _md.LastMechanicPlan;
            public SentenceDebugHud DebugHud => _md._debugHud;
            public bool TryTakeFreeMana() => _md.TryTakeFreeMana();
            public float WeaponCooldownMult() => _md.WeaponCooldownMult();
            public SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words) =>
                _md.ResolveSkillWords(words);

            public ActorVisual Visual => _md._visual;
            public AnimationBridge AnimationBridge => _md._animationBridge;
            public AnimationDatabase AnimationDatabase => _md._animationDatabase;
            public FollowCamera Camera => _md._camera;
            public HashSet<string> MissingAnimationBindings => _md._missingAnimationBindings;
            public string LastAnimationTypeId
            {
                set => _md.LastAnimationTypeId = value;
            }
            public string LastAnimationState
            {
                set => _md.LastAnimationState = value;
            }
            public bool LastAnimationPlayApplied
            {
                set => _md.LastAnimationPlayApplied = value;
            }
            public string LastAnimationClip
            {
                set => _md.LastAnimationClip = value;
            }
            public bool LastAnimationUsedFallback
            {
                set => _md.LastAnimationUsedFallback = value;
            }
            public void SyncVisualDelivery() => _md.SyncVisualDelivery();
            public void StartCastVfxTimer(SkillResolution skill, IReadOnlyList<SentenceWord> words) =>
                _md.StartCastVfxTimer(skill, words);

            public float WeaponDurationMult(in SkillResolution skill) => _md.WeaponDurationMult(skill);
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
    }
}
