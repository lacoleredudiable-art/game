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
using Dovus.Core.Shared;

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
        void NoteSustainedCast(SkillResolution skill);
        void NotifyCast(SkillId skillId);

        void ApplySelfCastEffects(SkillResolution skill);
        void NoteJsonCast(SkillResolution skill, TCtx ctx);

        /// <summary>v4 dilim teslimi; eski executor yolu yok.</summary>
        bool TryLaunchSkill(TCtx ctx, SkillResolution skill, in SkillMotionPlan motion, out float dealt);

        void ApplyCooldown(SkillResolution skill, TCtx ctx, bool cosmeticIfDisabled);
        void SpawnClosingImpact(TCtx ctx);

        void SetLastResolvedSkillId(SkillId skillId);
        bool IsHealSkill(SkillResolution skill);
        void SetLastSkillEffectApplied(bool applied);

        void LogSmokeOneOne(SkillResolution skill, bool effectApplied, float dealt);
        void TrySchedulePassiveEcho(TCtx ctx, SkillResolution skill, in SkillMotionPlan motion);
    }
}
