using Dovus.Core.Grammar;

namespace Dovus.App.Casting
{
    /// <summary>Düz vuruş (jab) yan etkileri; akış <see cref="CastPipeline.RunBasic"/> sırasını korur.</summary>
    public interface IBasicStrikePort<TCtx>
    {
        void ResolveImpactTarget(TCtx ctx);
        void ResetClosingChainBonus();
        SkillResolution ResolveSkill(TCtx ctx);
        bool IsHealSkill(SkillResolution skill);

        void ApplyClosingStatuses(TCtx ctx, SkillResolution skill);
        void ShoutSkill(SkillResolution skill, TCtx ctx);
        void ApplyClosingHeal(TCtx ctx, SkillResolution skill);

        double WorldTimeMs();
        bool BasicCadenceReady(double now);
        void NoteDeniedCadence();
        void SetLastBasicStrikeMs(double now);
        int BasicHitsNow();
        float BasicStrikeReachM();
        bool IsBossInStrikeCapsule(TCtx ctx, float reachM);
        bool EvaluateBasicInReach(float reachM);
        float BasicStrikeArcDeg();
        float BasicStrikeYawDeg();

        bool HasLivingLogic(TCtx ctx);
        void ApplyBossClosingBasic(TCtx ctx);
        float ApplyBasicStrikeDamage(TCtx ctx, float effectScale);
        void ScheduleBasicSubHits(TCtx ctx, int hits, float reach);
        void ApplyBasicExtras(float dealt, int hits);
        void TryLandWeaponStunBasic();

        void SpawnClosingImpact(TCtx ctx);
        void TryCannonBlast(TCtx ctx);
    }
}
