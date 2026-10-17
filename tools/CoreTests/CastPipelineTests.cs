using Dovus.App.Casting;
using Dovus.Core.Casting;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CoreTests;

[TestFixture]
public class CastPipelineTests
{
    /// <summary>RuleEngineV4SentenceHost skill yolu — v4-only CastPipeline sırası.</summary>
    static readonly string[] ExpectedSkillCastOrder =
    {
        nameof(ICastPort<object>.ResetClosingChainBonus),
        nameof(ICastPort<object>.ResolveSkill),
        nameof(ICastPort<object>.NoteWeaponCast),
        nameof(ICastPort<object>.OpenSlotCast),
        nameof(ICastPort<object>.Compatibility),
        nameof(ICastPort<object>.ShouldArmPassive),
        nameof(ICastPort<object>.ApplyResourceCost),
        nameof(ICastPort<object>.ResolveMotion),
        nameof(ICastPort<object>.ApplyMotionIframe),
        nameof(ICastPort<object>.NoteSustainedCast),
        nameof(ICastPort<object>.NotifyCast),
        nameof(ICastPort<object>.ApplySelfCastEffects),
        nameof(ICastPort<object>.NoteJsonCast),
        nameof(ICastPort<object>.TryLaunchSkill),
        nameof(ICastPort<object>.ApplyCooldown),
        nameof(ICastPort<object>.SpawnClosingImpact),
        nameof(ICastPort<object>.SetLastResolvedSkillId),
        nameof(ICastPort<object>.IsHealSkill),
        nameof(ICastPort<object>.SetLastSkillEffectApplied),
        nameof(ICastPort<object>.TrySchedulePassiveEcho),
        nameof(ICastPort<object>.CloseSlotCast),
        nameof(ICastPort<object>.ResetSlotQueryCastId),
    };

    static SkillResolution CompleteStrike(SkillId skillId = default) =>
        SkillResolution.Build(
            "1", "Ateş", "Test", skillId.IsEmpty ? (SkillId)"2-3" : skillId, "job",
            "saldiri", "Saldırı", "strike", "damage",
            10f, 15f, "projectile", "free_move", new[] { "burn" },
            "yogunlastirma", "Yoğunlaştırma", "focus",
            1f, 1f, 1f,
            2, "Temel", 1f, "free_move",
            string.Empty);

    sealed class RecordingPort : ICastPort<int>
    {
        public readonly List<string> Calls = new();
        public SkillResolution ResolveResult = CompleteStrike();
        public bool ShouldArm;
        public bool LaunchSkill;
        public float LaunchDealt;
        public bool ThrowAfterOpen;

        void Record([System.Runtime.CompilerServices.CallerMemberName] string name = "") =>
            Calls.Add(name);

        public void ResetClosingChainBonus() => Record();
        public SkillResolution ResolveSkill(int ctx)
        {
            Record();
            return ResolveResult;
        }
        public void NoteWeaponCast(SkillResolution skill) => Record();
        public int OpenSlotCast()
        {
            Record();
            return 7;
        }

        public void CloseSlotCast() => Record();
        public void ResetSlotQueryCastId() => Record();

        public WeaponSkillCompatibility Compatibility(SkillResolution skill)
        {
            Record();
            if (ThrowAfterOpen)
                throw new InvalidOperationException("boom");
            return WeaponSkillCompatibility.Neutral;
        }

        public bool ShouldArmPassive(WeaponSkillCompatibility compatibility)
        {
            Record();
            return ShouldArm;
        }

        public void TryTriggerPassive(int ctx) => Record();

        public void ApplyResourceCost(SkillResolution skill) => Record();
        public SkillMotionPlan ResolveMotion(SkillResolution skill)
        {
            Record();
            return SkillMotionPlan.None;
        }
        public void ApplyMotionIframe(SkillResolution skill, in SkillMotionPlan motion) => Record();
        public void NoteSustainedCast(SkillResolution skill) => Record();
        public void NotifyCast(SkillId skillId) => Record();
        public void ApplySelfCastEffects(SkillResolution skill) => Record();
        public void NoteJsonCast(SkillResolution skill, int ctx) => Record();

        public bool TryLaunchSkill(int ctx, SkillResolution skill, in SkillMotionPlan motion, out float dealt)
        {
            Record();
            dealt = LaunchDealt;
            return LaunchSkill;
        }

        public void ApplyCooldown(SkillResolution skill, int ctx, bool cosmeticIfDisabled) => Record();
        public void SpawnClosingImpact(int ctx) => Record();
        public void SetLastResolvedSkillId(SkillId skillId) => Record();
        public bool IsHealSkill(SkillResolution skill)
        {
            Record();
            return false;
        }

        public void SetLastSkillEffectApplied(bool applied) => Record();
        public void LogSmokeOneOne(SkillResolution skill, bool effectApplied, float dealt) => Record();
        public void TrySchedulePassiveEcho(int ctx, SkillResolution skill, in SkillMotionPlan motion) => Record();
    }

    [Test]
    public void RunSkill_V4Path_MatchesRuleEngineV4SentenceHostCallOrder()
    {
        var port = new RecordingPort();
        var pipeline = new CastPipeline();
        pipeline.RunSkill(0, port);
        Assert.That(port.Calls, Is.EqualTo(ExpectedSkillCastOrder));
    }

    [Test]
    public void RunSkill_IncompleteSkill_DeniesWithoutOpeningSlot()
    {
        var port = new RecordingPort
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
        var pipeline = new CastPipeline();
        var outcome = pipeline.RunSkill(0, port);

        Assert.That(outcome.Denied, Is.True);
        Assert.That(port.Calls, Does.Not.Contain(nameof(ICastPort<int>.OpenSlotCast)));
    }

    [Test]
    public void RunSkill_ExceptionAfterOpen_StillClosesSlot()
    {
        var port = new RecordingPort { ThrowAfterOpen = true };
        var pipeline = new CastPipeline();
        Assert.Throws<InvalidOperationException>(() => pipeline.RunSkill(0, port));
        Assert.That(port.Calls, Does.Contain(nameof(ICastPort<int>.CloseSlotCast)));
        Assert.That(port.Calls, Does.Contain(nameof(ICastPort<int>.ResetSlotQueryCastId)));
    }
}
