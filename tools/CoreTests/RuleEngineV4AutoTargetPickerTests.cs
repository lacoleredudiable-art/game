using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4AutoTargetPickerTests
{
    static RuleEngineV4TargetResolution HostileNeed() =>
        new(RuleEngineV4TargetSide.Hostile, true, "hostile_nearest");

    static RuleEngineV4TargetResolution Odakli() =>
        new(RuleEngineV4TargetSide.Hostile, true, "odakli_need");

    [Test]
    public void NearestHostile_PicksClosest()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 8f, true, false),
            new(4, true, 3f, true, false),
            new(5, true, 5f, true, false),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            HostileNeed(), 1, 6f, 6f, 25f, false, null, list);
        Assert.That(pick.HasTarget, Is.True);
        Assert.That(pick.TargetId, Is.EqualTo(4));
        Assert.That(pick.TargetInWeaponRange, Is.True);
    }

    [Test]
    public void Odakli_WithoutManualSelection_InvalidContext()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 2f, true, false),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            Odakli(), 2, 6f, 6f, 25f, false, null, list);
        var ctx = RuleEngineV4AutoTargetPicker.ToCastContext(pick, 2, false);
        Assert.That(ctx.HasValidTarget, Is.False);
    }

    [Test]
    public void Odakli_WithManualHostile_InRange()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 4f, true, false),
            new(4, true, 10f, true, false),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            Odakli(), 2, 6f, 6f, 25f, true, 3, list);
        var ctx = RuleEngineV4AutoTargetPicker.ToCastContext(pick, 2, false);
        Assert.That(ctx.HasValidTarget, Is.True);
        Assert.That(ctx.ManualTargetSelected, Is.True);
        Assert.That(ctx.TargetInWeaponRange, Is.True);
    }

    [Test]
    public void MarkedTarget_PrefersMarkedWithin25m()
    {
        var resolution = new RuleEngineV4TargetResolution(
            RuleEngineV4TargetSide.Hostile, false, "isaretli_nearest_mark");
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 2f, true, false),
            new(4, true, 4f, true, true),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            resolution, 9, 6f, 6f, 25f, false, null, list);
        Assert.That(pick.TargetId, Is.EqualTo(4));
    }
}
