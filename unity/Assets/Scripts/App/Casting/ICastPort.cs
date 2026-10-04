using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;

namespace Dovus.App.Casting
{
    /// <summary>Unity / Game yan etkileri; skill cast akışı <see cref="CastPipeline"/> sırasını korur.</summary>
    public interface ICastPort<TCtx>
    {
        void ResetClosingChainBonus();
        SkillResolution ResolveSkill(TCtx ctx);
        void NoteWeaponCast(SkillResolution skill);
        int OpenSlotCast();
        void CloseSlotCast();
        void ResetSlotQueryCastId();

        WeaponSkillCompatibility Compatibility(SkillResolution skill);
        bool ShouldArmPassive(WeaponSkillCompatibility compatibility);
        void TryTriggerPassive(TCtx ctx);

        void ApplyResourceCost(SkillResolution skill);
        SkillMotionPlan ResolveMotion(SkillResolution skill);
        void ApplyMotionIframe(SkillResolution skill, in SkillMotionPlan motion);
        bool TryBeginMotionTemplate(SkillResolution skill, TCtx ctx);
        void NoteSustainedCast(SkillResolution skill);

        SkillExecutorRoute Route(SkillResolution skill);
        SkillExecutorRoute ApplyMechanicWorldRoute(SkillResolution skill, SkillExecutorRoute route);
        void SetLastExecutorKind(SkillExecutorKind kind);

        void ApplySelfCastEffects(SkillResolution skill);
        void NoteJsonCast(SkillResolution skill, TCtx ctx);
        void BeginMechanicPlan(TCtx ctx, SkillResolution skill);
        void ArmTemplateDelivery(SkillResolution skill, TCtx ctx, in SkillMotionPlan motion);

        bool TryLaunchExecutor(
            SkillExecutorKind kind,
            TCtx ctx,
            SkillResolution skill,
            in SkillMotionPlan motion);

        void ScheduleFollowUps(
            SkillExecutorKind kind,
            TCtx ctx,
            SkillResolution skill,
            in SkillMotionPlan motion);

        /// <summary>Fallback teslimat; boss menzil dallanması port içinde kalır.</summary>
        float ApplyFallbackDelivery(
            TCtx ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            in SkillExecutorRoute route);

        void ApplyCooldown(SkillResolution skill, TCtx ctx, bool cosmeticIfDisabled);
        void SpawnClosingImpact(TCtx ctx);

        void SetLastResolvedSkillId(string skillId);
        bool IsHealSkill(SkillResolution skill);
        void SetLastSkillEffectApplied(bool applied);

        void LogSmokeOneOne(SkillResolution skill, bool effectApplied, float dealt);
        void TrySchedulePassiveEcho(TCtx ctx, SkillResolution skill, in SkillMotionPlan motion);
    }
}
