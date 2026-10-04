using Dovus.App.Casting;
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
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CoreTests;

[TestFixture]
public class CastPipelineTests
{
    /// <summary>ManifestationDirector.FireClosing skill yolu (2.4a öncesi) çağrı sırası.</summary>
    static readonly string[] ExpectedSkillCastOrder =
    {
        nameof(ICastPort<object>.ResetClosingChainBonus),
        nameof(ICastPort<object>.ResolveSkill),
        nameof(ICastPort<object>.NoteWeaponCast),
        nameof(ICastPort<object>.OpenSlotCast),
        nameof(ICastPort<object>.Compatibility),
        nameof(ICastPort<object>.PublishCompatibility),
        nameof(ICastPort<object>.ShouldArmPassive),
        nameof(ICastPort<object>.ApplyResourceCost),
        nameof(ICastPort<object>.ResolveMotion),
        nameof(ICastPort<object>.ApplyMotionIframe),
        nameof(ICastPort<object>.TryBeginMotionTemplate),
        nameof(ICastPort<object>.NoteSustainedCast),
        nameof(ICastPort<object>.NotifyCast),
        nameof(ICastPort<object>.Route),
        nameof(ICastPort<object>.ApplyMechanicWorldRoute),
        nameof(ICastPort<object>.SetLastExecutorKind),
        nameof(ICastPort<object>.ApplySelfCastEffects),
        nameof(ICastPort<object>.NoteJsonCast),
        nameof(ICastPort<object>.BeginMechanicPlan),
        nameof(ICastPort<object>.TryLaunchExecutor),
        nameof(ICastPort<object>.ApplyFallbackDelivery),
        nameof(ICastPort<object>.ShoutSkill),
        nameof(ICastPort<object>.ApplyCooldown),
        nameof(ICastPort<object>.SpawnClosingImpact),
        nameof(ICastPort<object>.SetLastResolvedSkillId),
        nameof(ICastPort<object>.IsHealSkill),
        nameof(ICastPort<object>.SetLastSkillEffectApplied),
        nameof(ICastPort<object>.TrySchedulePassiveEcho),
        nameof(ICastPort<object>.CloseSlotCast),
        nameof(ICastPort<object>.ResetSlotQueryCastId),
    };

    static SkillResolution CompleteStrike(string skillId = "2-3") =>
        SkillResolution.Build(
            "1", "Ateş", "Test", skillId, "job",
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
        public bool TemplateOwns;
        public bool LaunchExecutor;
        public float FallbackDealt;
        public bool ThrowAfterOpen;

        void Record([System.Runtime.CompilerServices.CallerMemberName] string name = "") =>
            Calls.Add(name);

        public void ResetClosingChainBonus() => Record();
        public SkillResolution ResolveSkill(int ctx)
        {
            Record();
            return ResolveResult;
        }
        public void NoteDeniedNeedsTwoRunes() => Record();

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

        public void PublishCompatibility(WeaponSkillCompatibility compatibility) => Record();
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
        public bool TryBeginMotionTemplate(SkillResolution skill, int ctx)
        {
            Record();
            return TemplateOwns;
        }

        public void NoteSustainedCast(SkillResolution skill) => Record();
        public void NotifyCast(string skillId) => Record();

        public SkillExecutorRoute Route(SkillResolution skill)
        {
            Record();
            return new SkillExecutorRoute(SkillExecutorKind.MeleeHitbox, false, string.Empty);
        }

        public SkillExecutorRoute ApplyMechanicWorldRoute(SkillResolution skill, SkillExecutorRoute route)
        {
            Record();
            return route;
        }
        public void SetLastExecutorKind(SkillExecutorKind kind) => Record();
        public void ApplySelfCastEffects(SkillResolution skill) => Record();
        public void NoteJsonCast(SkillResolution skill, int ctx) => Record();
        public void BeginMechanicPlan(int ctx, SkillResolution skill) => Record();

        public void ArmTemplateDelivery(SkillResolution skill, int ctx, in SkillMotionPlan motion) => Record();

        public bool TryLaunchExecutor(
            SkillExecutorKind kind,
            int ctx,
            SkillResolution skill,
            in SkillMotionPlan motion)
        {
            Record();
            return LaunchExecutor;
        }

        public void ScheduleFollowUps(
            SkillExecutorKind kind,
            int ctx,
            SkillResolution skill,
            in SkillMotionPlan motion) => Record();

        public float ApplyFallbackDelivery(
            int ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            in SkillExecutorRoute route)
        {
            Record();
            return FallbackDealt;
        }

        public void ShoutSkill(SkillResolution skill, int ctx) => Record();
        public void ApplyCooldown(SkillResolution skill, int ctx, bool cosmeticIfDisabled) => Record();
        public void AnnotateMotion(SkillResolution skill, in SkillMotionPlan motion) => Record();
        public void SpawnClosingImpact(int ctx) => Record();
        public void SetLastResolvedSkillId(string skillId) => Record();
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
    public void RunSkill_FallbackPath_MatchesManifestationDirectorCallOrder()
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
                "1", "Ateş", "X", "t", "job",
                "saldiri", "Saldırı", "strike", "damage",
                1f, 1f, "projectile", "free_move", Array.Empty<string>(),
                "y", "Y", "focus",
                1f, 1f, 1f,
                1, "Temel", 1f, "free_move",
                string.Empty,
                isComplete: false)
        };
        var outcome = new CastPipeline().RunSkill(0, port);
        Assert.That(outcome.Denied, Is.True);
        Assert.That(port.Calls, Is.EqualTo(new[]
        {
            nameof(ICastPort<object>.ResetClosingChainBonus),
            nameof(ICastPort<object>.ResolveSkill),
            nameof(ICastPort<object>.NoteDeniedNeedsTwoRunes),
        }));
        Assert.That(port.Calls, Does.Not.Contain(nameof(ICastPort<object>.OpenSlotCast)));
    }

    [Test]
    public void RunSkill_ExecutorPath_SchedulesFollowUpsNotFallback()
    {
        var port = new RecordingPort { LaunchExecutor = true };
        new CastPipeline().RunSkill(0, port);
        Assert.That(port.Calls, Does.Contain(nameof(ICastPort<object>.ScheduleFollowUps)));
        Assert.That(port.Calls, Does.Not.Contain(nameof(ICastPort<object>.ApplyFallbackDelivery)));
    }

    [Test]
    public void RunSkill_TemplatePath_ArmsTemplateSkipsFallback()
    {
        var port = new RecordingPort { TemplateOwns = true };
        new CastPipeline().RunSkill(0, port);
        Assert.That(port.Calls, Does.Contain(nameof(ICastPort<object>.ArmTemplateDelivery)));
        Assert.That(port.Calls, Does.Not.Contain(nameof(ICastPort<object>.ApplyFallbackDelivery)));
    }

    [Test]
    public void RunSkill_ExceptionAfterOpen_StillClosesSlot()
    {
        var port = new RecordingPort { ThrowAfterOpen = true };
        Assert.Throws<InvalidOperationException>(() => new CastPipeline().RunSkill(0, port));
        Assert.That(port.Calls, Does.Contain(nameof(ICastPort<object>.OpenSlotCast)));
        Assert.That(port.Calls, Does.Contain(nameof(ICastPort<object>.CloseSlotCast)));
        Assert.That(port.Calls, Does.Contain(nameof(ICastPort<object>.ResetSlotQueryCastId)));
    }
}
