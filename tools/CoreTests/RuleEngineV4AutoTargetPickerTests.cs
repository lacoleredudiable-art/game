using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4AutoTargetPickerTests
{
    static RuleEngineV4TargetResolution HostileNeed() =>
        new(RuleEngineV4TargetSide.Hostile, true, "hostile_nearest");

    static RuleEngineV4TargetResolution OdakliHostile() =>
        new(RuleEngineV4TargetSide.Hostile, true, "odakli_need");

    static RuleEngineV4TargetResolution OdakliFriendly() =>
        new(RuleEngineV4TargetSide.Friendly, true, "odakli_need");

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
            HostileNeed(), 1, 1, 6f, 6f, 25f, false, null, list);
        Assert.That(pick.HasTarget, Is.True);
        Assert.That(pick.TargetId, Is.EqualTo(4));
        Assert.That(pick.TargetInWeaponRange, Is.True);
    }

    [Test]
    public void Odakli_Zarar_PicksLowestHpHostile()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 2f, true, false, hpRatio: 0.8f),
            new(4, true, 10f, true, false, hpRatio: 0.2f),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            OdakliHostile(), 2, 1, 6f, 6f, 25f, false, null, list);
        var ctx = RuleEngineV4AutoTargetPicker.ToCastContext(pick, 2, false);
        Assert.That(ctx.HasValidTarget, Is.True);
        Assert.That(ctx.ManualTargetSelected, Is.False);
        Assert.That(pick.TargetId, Is.EqualTo(4));
        Assert.That(pick.TargetInWeaponRange, Is.False);
    }

    [Test]
    public void Odakli_Sifa_PicksLowestHpFriendly()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(10, false, 1f, true, false, hpRatio: 0.9f),
            new(11, false, 5f, true, false, hpRatio: 0.15f),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            OdakliFriendly(), 2, 2, 6f, 6f, 25f, false, null, list);
        Assert.That(pick.TargetId, Is.EqualTo(11));
    }

    [Test]
    public void Odakli_Arindirma_PicksHighestPurifyNeed()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(10, false, 1f, true, false, hpRatio: 0.5f, purifyNeedScore: 1f),
            new(11, false, 5f, true, false, hpRatio: 0.9f, purifyNeedScore: 4f),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            OdakliFriendly(), 2, 9, 6f, 6f, 25f, false, null, list);
        Assert.That(pick.TargetId, Is.EqualTo(11));
    }

    [Test]
    public void Odakli_IgnoresManualTargetLock()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 4f, true, false, hpRatio: 0.1f),
            new(4, true, 2f, true, false, hpRatio: 0.9f),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            OdakliHostile(), 2, 1, 6f, 6f, 25f, true, 4, list);
        Assert.That(pick.TargetId, Is.EqualTo(3));
        Assert.That(pick.ManualTargetUsed, Is.False);
    }

    static List<RuleEngineV4TargetCandidate> LockScene(float lockedDistanceM, bool lockedAvailable = true) =>
        new()
        {
            new(3, true, 2f, true, false),
            new(4, true, lockedDistanceM, lockedAvailable, false),
        };

    [Test]
    public void ManualLock_InRange_WinsOverNearest()
    {
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            HostileNeed(), 1, 1, 6f, 6f, 25f, true, 4, LockScene(5f));
        Assert.That(pick.TargetId, Is.EqualTo(4));
        Assert.That(pick.TargetInWeaponRange, Is.True);
        Assert.That(pick.ManualTargetUsed, Is.True);
        Assert.That(RuleEngineV4AutoTargetPicker.ToCastContext(pick, 1, false).ManualTargetSelected, Is.True);
    }

    [Test]
    public void ManualLock_OutOfRange_FallsBackToAuto()
    {
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            HostileNeed(), 1, 1, 6f, 6f, 25f, true, 4, LockScene(9f));
        Assert.That(pick.TargetId, Is.EqualTo(3));
        Assert.That(pick.ManualTargetUsed, Is.False);
    }

    [Test]
    public void ManualLock_Unavailable_FallsBackToAuto()
    {
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            HostileNeed(), 1, 1, 6f, 6f, 25f, true, 4, LockScene(5f, lockedAvailable: false));
        Assert.That(pick.TargetId, Is.EqualTo(3));
        Assert.That(pick.ManualTargetUsed, Is.False);
    }

    [Test]
    public void ManualLock_FriendlyId_NotUsedForHostilePick()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 2f, true, false),
            new(10, false, 1f, true, false),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            HostileNeed(), 1, 1, 6f, 6f, 25f, true, 10, list);
        Assert.That(pick.TargetId, Is.EqualTo(3));
        Assert.That(pick.ManualTargetUsed, Is.False);
    }

    [Test]
    public void Odakli_LockedInRange_StillFollowsOwnRule()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 5f, true, false, hpRatio: 0.2f),
            new(4, true, 1f, true, false, hpRatio: 0.9f),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            OdakliHostile(), 2, 1, 6f, 6f, 25f, true, 4, list);
        Assert.That(pick.TargetId, Is.EqualTo(3), "Odaklı en düşük can");
        Assert.That(pick.ManualTargetUsed, Is.False);
    }

    [Test]
    public void Odakli_Cagirma_PicksLowestHpHostile()
    {
        var list = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 4f, true, false, hpRatio: 0.6f),
            new(4, true, 8f, true, false, hpRatio: 0.25f),
        };
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            OdakliHostile(), 2, 11, 6f, 6f, 25f, false, null, list);
        Assert.That(pick.TargetId, Is.EqualTo(4));
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
            resolution, 9, 1, 6f, 6f, 25f, false, null, list);
        Assert.That(pick.TargetId, Is.EqualTo(4));
    }
}
