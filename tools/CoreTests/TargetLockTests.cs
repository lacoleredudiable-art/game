using System.Collections.Generic;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Targeting;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class TargetLockTests
{
    // Mesafe sayıları test verisidir (kaynakta yok); kuralın kendisi sayıdan bağımsız.
    static readonly LockOnRange Range = new(10f);

    const int Head = 1, Body = 2, Wing = 3;

    static LockOnCandidate Enemy(int id, float dist = 5f, float bearing = 0f, bool available = true,
        IReadOnlyList<LockPoint> points = null) => new(id, true, available, dist, bearing, points);

    static LockOnCandidate Ally(int id, float dist = 5f, float bearing = 0f) => new(id, false, true, dist, bearing);

    static LockPoint[] BossPoints(float headM = 4f, bool wingAvailable = true) => new[]
    {
        new LockPoint(Head, LockPointKind.Head, headM),
        new LockPoint(Body, LockPointKind.Body, 2f),
        new LockPoint(Wing, LockPointKind.Wing, 3f, wingAvailable),
    };

    [Test]
    public void TryAcquire_InRange_Locks_AndDefaultsToBodyPoint()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Enemy(7, points: BossPoints()) };
        Assert.That(sut.TryAcquire(7, list, Range), Is.True);
        Assert.That(sut.IsLocked, Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(7));
        Assert.That(sut.PointId, Is.EqualTo(Body));
    }

    [Test]
    public void TryAcquire_OutOfRange_Unavailable_Or_Unknown_Fails()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate>
        {
            Enemy(1, dist: 10.5f), Enemy(2, available: false),
        };
        Assert.That(sut.TryAcquire(1, list, Range), Is.False);
        Assert.That(sut.TryAcquire(2, list, Range), Is.False);
        Assert.That(sut.TryAcquire(99, list, Range), Is.False);
        Assert.That(sut.IsLocked, Is.False);
    }

    [Test]
    public void TryAcquireNearestHostile_IgnoresAllies_PicksNearest_TieByLowId()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Ally(1, 1f), Enemy(5, 4f), Enemy(4, 4f), Enemy(6, 9f) };
        Assert.That(sut.TryAcquireNearestHostile(list, Range), Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(4));
    }

    [Test]
    public void TryAcquireNearestHostile_NoEnemyInRange_Fails()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Ally(1, 1f), Enemy(5, 11f) };
        Assert.That(sut.TryAcquireNearestHostile(list, Range), Is.False);
    }

    [Test]
    public void Update_TargetDies_ReleasesWithTargetGone()
    {
        var sut = new TargetLock();
        sut.TryAcquire(7, new List<LockOnCandidate> { Enemy(7) }, Range);
        bool still = sut.Update(new List<LockOnCandidate> { Enemy(7, available: false) });
        Assert.That(still, Is.False);
        Assert.That(sut.IsLocked, Is.False);
        Assert.That(sut.LastRelease, Is.EqualTo(LockReleaseReason.TargetGone));
    }

    [Test]
    public void Update_TargetMissingFromList_Releases()
    {
        var sut = new TargetLock();
        sut.TryAcquire(7, new List<LockOnCandidate> { Enemy(7) }, Range);
        sut.Update(new List<LockOnCandidate>());
        Assert.That(sut.LastRelease, Is.EqualTo(LockReleaseReason.TargetGone));
    }

    [Test]
    public void Update_G10_KeepsLockAtAnyDistance_OnlyDeadOrGoneDrops()
    {
        var sut = new TargetLock();
        sut.TryAcquire(7, new List<LockOnCandidate> { Enemy(7, 9f) }, Range);
        Assert.That(sut.Update(new List<LockOnCandidate> { Enemy(7, 12.5f) }), Is.True);
        Assert.That(sut.Update(new List<LockOnCandidate> { Enemy(7, 500f) }), Is.True);
        Assert.That(sut.IsLocked, Is.True);
        Assert.That(sut.Update(new List<LockOnCandidate> { Enemy(7, 500f, available: false) }), Is.False);
        Assert.That(sut.LastRelease, Is.EqualTo(LockReleaseReason.TargetGone));
    }

    [Test]
    public void Release_ClearsLock_WithManualReason()
    {
        var sut = new TargetLock();
        sut.TryAcquire(7, new List<LockOnCandidate> { Enemy(7) }, Range);
        sut.Release();
        Assert.That(sut.IsLocked, Is.False);
        Assert.That(sut.PointId, Is.EqualTo(TargetLock.NoPoint));
        Assert.That(sut.LastRelease, Is.EqualTo(LockReleaseReason.Manual));
    }

    [Test]
    public void SwitchTarget_GoesRightThenLeft_ByBearing()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate>
        {
            Enemy(1, bearing: -40f), Enemy(2, bearing: 0f), Enemy(3, bearing: 30f),
        };
        sut.TryAcquire(2, list, Range);
        Assert.That(sut.SwitchTarget(+1, list, Range), Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(3));
        Assert.That(sut.SwitchTarget(-1, list, Range), Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(2));
        Assert.That(sut.SwitchTarget(-1, list, Range), Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(1));
    }

    [Test]
    public void SwitchTarget_WrapsAround_AtTheEnd()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Enemy(1, bearing: -40f), Enemy(3, bearing: 30f) };
        sut.TryAcquire(3, list, Range);
        Assert.That(sut.SwitchTarget(+1, list, Range), Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(1));
        Assert.That(sut.SwitchTarget(-1, list, Range), Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(3));
    }

    [Test]
    public void SwitchTarget_SkipsAllies_Dead_And_OutOfRange_AndNoOpWhenAlone()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate>
        {
            Enemy(1, bearing: 0f), Ally(2, bearing: 10f), Enemy(3, bearing: 20f, available: false),
            Enemy(4, dist: 11f, bearing: 30f),
        };
        sut.TryAcquire(1, list, Range);
        Assert.That(sut.SwitchTarget(+1, list, Range), Is.False);
        Assert.That(sut.TargetId, Is.EqualTo(1));
    }

    [Test]
    public void SwitchTarget_EqualBearing_IsStableByIdOrder()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Enemy(1), Enemy(2), Enemy(3) };
        sut.TryAcquire(1, list, Range);
        sut.SwitchTarget(+1, list, Range);
        Assert.That(sut.TargetId, Is.EqualTo(2));
        sut.SwitchTarget(+1, list, Range);
        Assert.That(sut.TargetId, Is.EqualTo(3));
    }

    [Test]
    public void SwitchTarget_ResetsPointToDefaultOfNewTarget()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate>
        {
            Enemy(1, bearing: 0f, points: BossPoints()), Enemy(2, bearing: 10f, points: BossPoints()),
        };
        sut.TryAcquire(1, list, Range);
        sut.TrySelectPoint(Wing, list);
        sut.SwitchTarget(+1, list, Range);
        Assert.That(sut.PointId, Is.EqualTo(Body));
    }

    [Test]
    public void CyclePoint_WalksAvailablePoints_AndSkipsDestroyedOnes()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Enemy(7, points: BossPoints(wingAvailable: false)) };
        sut.TryAcquire(7, list, Range);               // Body
        Assert.That(sut.CyclePoint(+1, list), Is.True);
        Assert.That(sut.PointId, Is.EqualTo(Head));   // Wing atlanır, baştan sarar
        Assert.That(sut.CyclePoint(+1, list), Is.True);
        Assert.That(sut.PointId, Is.EqualTo(Body));
        Assert.That(sut.CyclePoint(-1, list), Is.True);
        Assert.That(sut.PointId, Is.EqualTo(Head));
    }

    [Test]
    public void CyclePoint_SinglePointOrNoPoints_DoesNothing()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate>
        {
            Enemy(7, points: new[] { new LockPoint(Body, LockPointKind.Body, 1f) }),
            Enemy(8),
        };
        sut.TryAcquire(7, list, Range);
        Assert.That(sut.CyclePoint(+1, list), Is.False);
        sut.TryAcquire(8, list, Range);
        Assert.That(sut.PointId, Is.EqualTo(TargetLock.NoPoint));
        Assert.That(sut.CyclePoint(+1, list), Is.False);
    }

    [Test]
    public void Update_SelectedPointDestroyed_FallsBackToBody_WithoutReleasingTarget()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Enemy(7, points: BossPoints()) };
        sut.TryAcquire(7, list, Range);
        sut.TrySelectPoint(Wing, list);
        var after = new List<LockOnCandidate> { Enemy(7, points: BossPoints(wingAvailable: false)) };
        Assert.That(sut.Update(after), Is.True);
        Assert.That(sut.TargetId, Is.EqualTo(7));
        Assert.That(sut.PointId, Is.EqualTo(Body));
    }

    [Test]
    public void TrySelectPoint_RejectsUnknownOrUnavailablePoint()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Enemy(7, points: BossPoints(wingAvailable: false)) };
        sut.TryAcquire(7, list, Range);
        Assert.That(sut.TrySelectPoint(Wing, list), Is.False);
        Assert.That(sut.TrySelectPoint(99, list), Is.False);
        Assert.That(sut.PointId, Is.EqualTo(Body));
    }

    [Test]
    public void ReacquireSameTarget_KeepsSelectedPoint()
    {
        var sut = new TargetLock();
        var list = new List<LockOnCandidate> { Enemy(7, points: BossPoints()) };
        sut.TryAcquire(7, list, Range);
        sut.TrySelectPoint(Head, list);
        Assert.That(sut.TryAcquire(7, list, Range), Is.True);
        Assert.That(sut.PointId, Is.EqualTo(Head));
    }

    [Test]
    public void ResolveAim_HighHead_FallsBackToBodyCenter_O518()
    {
        LockPoint[] pts = BossPoints(headM: LockOnDefaults.HighHeadThresholdM + 0.5f);
        Assert.That(LockPointRules.TryResolveAim(pts, Head, LockOnDefaults.HighHeadThresholdM, out LockPoint aim), Is.True);
        Assert.That(aim.Id, Is.EqualTo(Body));
    }

    [Test]
    public void ResolveAim_HeadAtThreshold_StaysHead()
    {
        LockPoint[] pts = BossPoints(headM: LockOnDefaults.HighHeadThresholdM);
        LockPointRules.TryResolveAim(pts, Head, LockOnDefaults.HighHeadThresholdM, out LockPoint aim);
        Assert.That(aim.Id, Is.EqualTo(Head));
    }

    [Test]
    public void ResolveAim_NoPoints_ReturnsFalse_AndUnavailableSelectionUsesDefault()
    {
        Assert.That(LockPointRules.TryResolveAim(new LockPoint[0], Body, 10f, out _), Is.False);
        LockPoint[] pts = BossPoints(wingAvailable: false);
        LockPointRules.TryResolveAim(pts, Wing, 10f, out LockPoint aim);
        Assert.That(aim.Id, Is.EqualTo(Body));
    }

    [Test]
    public void ToManualTarget_FeedsAutoTargetPicker_LockedEnemyWinsOverNearest()
    {
        var sut = new TargetLock();
        var lockList = new List<LockOnCandidate> { Enemy(3, 8f), Enemy(4, 3f) };
        sut.TryAcquire(3, lockList, Range);
        sut.ToManualTarget(out bool manual, out int? manualId);

        var pickList = new List<RuleEngineV4TargetCandidate>
        {
            new(3, true, 8f, true, false), new(4, true, 3f, true, false),
        };
        var hostile = new RuleEngineV4TargetResolution(RuleEngineV4TargetSide.Hostile, true, "hostile_nearest");
        RuleEngineV4TargetPick pick = RuleEngineV4AutoTargetPicker.Pick(
            hostile, 1, 1, 10f, 10f, 25f, manual, manualId, pickList);
        Assert.That(pick.TargetId, Is.EqualTo(3));
        Assert.That(pick.ManualTargetUsed, Is.True);

        sut.Release();
        sut.ToManualTarget(out manual, out manualId);
        Assert.That(manual, Is.False);
        Assert.That(manualId, Is.Null);
        pick = RuleEngineV4AutoTargetPicker.Pick(hostile, 1, 1, 10f, 10f, 25f, manual, manualId, pickList);
        Assert.That(pick.TargetId, Is.EqualTo(4));
    }
}
