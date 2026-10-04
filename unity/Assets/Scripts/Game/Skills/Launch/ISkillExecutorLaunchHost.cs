using Dovus.Core.Combat;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Core.Equipment;
using Dovus.Game.Actors;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using UnityEngine;

namespace Dovus.Game.Skills.Launch
{
    public interface ISkillExecutorLaunchHost
    {
        Transform Player { get; }
        Transform DirectorTransform { get; }
        GameClock Clock { get; }
        CombatTuning Combat { get; }
        PresentationCatalog PresentationCatalog { get; }
        void EnsurePresentationCatalog();
        EquipmentItem EquippedWeapon { get; }
        VerbExecutionData VerbData { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        Transform BossTransform { get; }
        MechanicGrammar MechanicEngine { get; }
        float ClosingChainBonus { get; }
        int SlotQueryCastId { get; set; }
        bool CasterRecoilSuppressed { get; set; }
        bool LastSkillEffectApplied { set; }
        SlotPassiveDirector SlotPassives { get; }

        bool TryVerbHitbox(in SkillResolution skill, out VerbHitboxSpec spec);
        bool IsEnemyBody(Transform body);
        bool IsFriendlyFieldVerb(in SkillResolution skill);
        MechanicPlan MechanicPlanFor(SkillResolution skill);
        WeaponPassiveMods HitMods(in SkillResolution skill, bool isBasicStrike, bool consumeBonus);
        bool BossWithin(Vector3 center, float radiusM);
        bool IsHealSkill(SkillResolution skill);
        int EquippedWeaponNumber();

        void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill);
        float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale,
            float? chainBonusOverride);
        void ApplyClosingStatuses(PendingClosing pending, SkillResolution skill, bool bossReached);
        void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 fieldCenter);
        int CalculateClosingHealAmount(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale,
            float? chainBonusOverride);
        void ApplyClosingHealAmount(
            SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget);
        void ApplyWeaponDelivery(
            SkillResolution skill,
            SkillExecutorKind kind,
            Transform target,
            ref Vector3 origin,
            ref float range,
            ref float radius,
            ref string hitboxShape,
            ref float hitboxAngleDeg,
            ref float speed);
        void ApplySpawnIFrame(in SkillResolution skill);
        float ApplyMinionHit(SkillResolution skill, float bindingDamage);
        PlayerVitals CachedPlayerVitals();

        void ApplyVerbHitboxSizing(
            SkillExecutorKind kind,
            in SkillResolution skill,
            ManifestationTuning tuning,
            float rangeMult,
            bool burst,
            ref float radius,
            ref float range,
            ref float durationSec,
            ref int spawnCount);

        void ResolveFieldTiming(
            in SkillResolution skill,
            in LivingEffectPlan plan,
            ManifestationTuning tuning,
            out float durationSec,
            out float tickSec,
            out float perTickShare);

        void DestroyUnityObject(UnityEngine.Object obj);
        ISkillExecutor AddSummonExecutor(GameObject go);
    }
}
