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
        public void NoteSustainedCast(SkillResolution skill) { }
        public void NotifyCast(SkillId skillId) { }
        public void ApplySelfCastEffects(SkillResolution skill) { }
        public void NoteJsonCast(SkillResolution skill, int ctx) { }
        public bool TryLaunchSkill(int ctx, SkillResolution skill, in SkillMotionPlan motion, out float dealt)
        {
            dealt = 0f;
            return false;
        }
        public void ApplyCooldown(SkillResolution skill, int ctx, bool cosmeticIfDisabled) { }
        public void SpawnClosingImpact(int ctx) { }
        public void SetLastResolvedSkillId(SkillId skillId) { }
        public bool IsHealSkill(SkillResolution skill) => false;
        public void SetLastSkillEffectApplied(bool applied) { }
        public void LogSmokeOneOne(SkillResolution skill, bool effectApplied, float dealt) { }
        public void TrySchedulePassiveEcho(int ctx, SkillResolution skill, in SkillMotionPlan motion) { }
    }

    [Test]
    public void RunSkill_PresentationEvents_FireInRuleEngineV4SentenceHostOrder()
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
}
