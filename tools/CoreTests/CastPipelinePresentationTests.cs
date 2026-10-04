using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using NUnit.Framework;
using System.Collections.Generic;

namespace CoreTests;

[TestFixture]
public class CastPipelinePresentationTests
{
    static SkillResolution CompleteStrike(SkillId skillId = default) =>
        SkillResolution.Build(
            "1", "Ateş", "Test", skillId.IsEmpty ? (SkillId)"2-3" : skillId, "job",
            "saldiri", "Saldırı", "strike", "damage",
            10f, 15f, "projectile", "free_move", new[] { "burn" },
            "yogunlastirma", "Yoğunlaştırma", "focus",
            1f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty);

    sealed class GameplayPort : ICastPort<int>
    {
        public SkillResolution ResolveResult = CompleteStrike();
        public void ResetClosingChainBonus() { }
        public SkillResolution ResolveSkill(int ctx) => ResolveResult;
        public void NoteWeaponCast(SkillResolution skill) { }
        public int OpenSlotCast() => 1;
        public void CloseSlotCast() { }
        public void ResetSlotQueryCastId() { }
        public WeaponSkillCompatibility Compatibility(SkillResolution skill) => WeaponSkillCompatibility.Neutral;
        public bool ShouldArmPassive(WeaponSkillCompatibility compatibility) => false;
        public void TryTriggerPassive(int ctx) { }
        public void ApplyResourceCost(SkillResolution skill) { }
        public SkillMotionPlan ResolveMotion(SkillResolution skill) => SkillMotionPlan.None;
        public void ApplyMotionIframe(SkillResolution skill, in SkillMotionPlan motion) { }
        public bool TryBeginMotionTemplate(SkillResolution skill, int ctx) => false;
        public void NoteSustainedCast(SkillResolution skill) { }
        public void NotifyCast(SkillId skillId) { }
        public SkillExecutorRoute Route(SkillResolution skill) =>
            new(SkillExecutorKind.MeleeHitbox, false, string.Empty);
        public SkillExecutorRoute ApplyMechanicWorldRoute(SkillResolution skill, SkillExecutorRoute route) => route;
        public void SetLastExecutorKind(SkillExecutorKind kind) { }
        public void ApplySelfCastEffects(SkillResolution skill) { }
        public void NoteJsonCast(SkillResolution skill, int ctx) { }
        public void BeginMechanicPlan(int ctx, SkillResolution skill) { }
        public void ArmTemplateDelivery(SkillResolution skill, int ctx, in SkillMotionPlan motion) { }
        public bool TryLaunchExecutor(
            SkillExecutorKind kind,
            int ctx,
            SkillResolution skill,
            in SkillMotionPlan motion) => false;
        public void ScheduleFollowUps(
            SkillExecutorKind kind,
            int ctx,
            SkillResolution skill,
            in SkillMotionPlan motion) { }
        public float ApplyFallbackDelivery(
            int ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            in SkillExecutorRoute route) => 0f;
        public void ApplyCooldown(SkillResolution skill, int ctx, bool cosmeticIfDisabled) { }
        public void SpawnClosingImpact(int ctx) { }
        public void SetLastResolvedSkillId(SkillId skillId) { }
        public bool IsHealSkill(SkillResolution skill) => false;
        public void SetLastSkillEffectApplied(bool applied) { }
        public void LogSmokeOneOne(SkillResolution skill, bool effectApplied, float dealt) { }
        public void TrySchedulePassiveEcho(int ctx, SkillResolution skill, in SkillMotionPlan motion) { }
    }

    [Test]
    public void RunSkill_PresentationEvents_FireInManifestationDirectorOrder()
    {
        var order = new List<string>();
        var pipeline = new CastPipeline();
        pipeline.CompatibilityPublished += _ => order.Add(nameof(pipeline.CompatibilityPublished));
        pipeline.SkillShoutRequested += _ => order.Add(nameof(pipeline.SkillShoutRequested));

        pipeline.RunSkill(0, new GameplayPort());

        Assert.That(order, Is.EqualTo(new[]
        {
            nameof(pipeline.CompatibilityPublished),
            nameof(pipeline.SkillShoutRequested),
        }));
    }

    [Test]
    public void RunSkill_Denied_EmitsDenialBeforeExecutor()
    {
        var reasons = new List<CastDenialReason>();
        var pipeline = new CastPipeline();
        pipeline.DenialRequested += e => reasons.Add(e.Reason);

        var port = new GameplayPort
        {
            ResolveResult = SkillResolution.Build(
                "1", "Ateş", "X", (SkillId)"t", "job",
                "saldiri", "Saldırı", "strike", "damage",
                1f, 1f, "projectile", "free_move", System.Array.Empty<string>(),
                "y", "Y", "focus",
                1f, 1f, 1f,
                1, "Temel", 1f, "free_move",
                string.Empty,
                isComplete: false)
        };
        var outcome = pipeline.RunSkill(0, port);

        Assert.That(outcome.Denied, Is.True);
        Assert.That(reasons, Is.EqualTo(new[] { CastDenialReason.NeedsTwoRunes }));
    }

    [Test]
    public void RunSkill_CompatibilityPublished_FiresOncePerCast()
    {
        int count = 0;
        var pipeline = new CastPipeline();
        pipeline.CompatibilityPublished += _ => count++;
        pipeline.RunSkill(0, new GameplayPort());
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public void RunBasic_Heal_EmitsSkillShout()
    {
        var shouted = false;
        var pipeline = new CastPipeline();
        pipeline.SkillShoutRequested += _ => shouted = true;

        var port = new HealBasicPort();
        var outcome = pipeline.RunBasic(0, port);

        Assert.That(shouted, Is.True);
        Assert.That(outcome.Connected, Is.False);
    }

    sealed class HealBasicPort : IBasicStrikePort<int>
    {
        public void ResolveImpactTarget(int ctx) { }
        public void ResetClosingChainBonus() { }
        public SkillResolution ResolveSkill(int ctx) => CompleteStrike((SkillId)"1-1");
        public bool IsHealSkill(SkillResolution skill) => true;
        public void ApplyClosingStatuses(int ctx, SkillResolution skill) { }
        public void ApplyClosingHeal(int ctx, SkillResolution skill) { }
        public double WorldTimeMs() => 0;
        public bool BasicCadenceReady(double now) => true;
        public void SetLastBasicStrikeMs(double now) { }
        public int BasicHitsNow() => 1;
        public float BasicStrikeReachM() => 1f;
        public bool IsBossInStrikeCapsule(int ctx, float reachM) => false;
        public bool EvaluateBasicInReach(float reachM) => false;
        public float BasicStrikeArcDeg() => 0f;
        public float BasicStrikeYawDeg() => 0f;
        public bool HasLivingLogic(int ctx) => false;
        public void ApplyBossClosingBasic(int ctx) { }
        public float ApplyBasicStrikeDamage(int ctx, float effectScale) => 0f;
        public void ScheduleBasicSubHits(int ctx, int hits, float reach) { }
        public void ApplyBasicExtras(float dealt, int hits) { }
        public void TryLandWeaponStunBasic() { }
        public void SpawnClosingImpact(int ctx) { }
        public void TryCannonBlast(int ctx) { }
    }
}
