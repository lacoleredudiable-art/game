using System.Collections.Generic;
using Dovus.Core.Team;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class AllyBrainTests
{
    static AllyContext Ctx(
        float x = 0, float z = 0, AllyRole role = AllyRole.Melee,
        AllyDanger[]? dangers = null, TeamTaskZone? zone = null, TeamTaskZone[]? shelters = null,
        AllyTarget? claw = null, AllyTarget? head = null, float px = 0, float pz = 0) =>
        new AllyContext(7, x, z, 0.4f, role, 2f, px, pz, dangers, zone, shelters, claw, head);

    [Test]
    public void Danger_ReactsOnlyAfterReactionDelay_ThenLeavesCircleByShortestPath()
    {
        var brain = new AllyBrain();
        var d = new[] { new AllyDanger(1, 0f, 0f, 4f, 1.0, false) };
        var c = Ctx(x: 2f, z: 0f, dangers: d);
        Assert.That(brain.Decide(10.0, c).Kind, Is.EqualTo(AllyActionKind.Idle));      // yeni gördü, 0,3 s dolmadı
        Assert.That(brain.Decide(10.29, c).Kind, Is.EqualTo(AllyActionKind.Idle));
        var go = brain.Decide(10.3, c);
        Assert.That(go.Kind, Is.EqualTo(AllyActionKind.MoveTo));
        Assert.That(go.X, Is.GreaterThan(4f));                                          // +X yönünde dışarı
        Assert.That(go.Z, Is.EqualTo(0f).Within(1e-4f));
    }

    [Test]
    public void Danger_OutsideCircle_IsIgnored()
    {
        var brain = new AllyBrain();
        var d = new[] { new AllyDanger(1, 0f, 0f, 4f, 1.0, false) };
        var c = Ctx(x: 20f, z: 0f, dangers: d, px: 20f);
        Assert.That(brain.Decide(1.0, c).Kind, Is.EqualTo(AllyActionKind.Idle));
        Assert.That(brain.Decide(2.0, c).Kind, Is.EqualTo(AllyActionKind.Idle));
    }

    [Test]
    public void Danger_EveryFifthWarningIsLate()
    {
        var brain = new AllyBrain();
        int lateCount = 0;
        for (int i = 1; i <= 10; i++)
        {
            var d = new[] { new AllyDanger(i, 0f, 0f, 4f, 1.0, false) };
            var c = Ctx(x: 1f, dangers: d);
            double t0 = i * 100.0;
            brain.Decide(t0, c);
            // 0,3 s sonra hâlâ kımıldamıyorsa geç kalmış demektir.
            if (brain.Decide(t0 + 0.31, c).Kind == AllyActionKind.Idle) lateCount++;
        }
        Assert.That(lateCount, Is.EqualTo(2)); // 5. ve 10.
    }

    [Test]
    public void ArenaWide_GoesToNearestShelter_OrDodgesLate()
    {
        var shelter = new TeamTaskZone(0, 9f, 4f, 1.5f, 3f, 0f);
        var wave = new[] { new AllyDanger(1, 0f, 0f, 99f, 2.0, true) };

        var brain = new AllyBrain();
        brain.Decide(0.0, Ctx(dangers: wave, shelters: new[] { shelter }));
        var move = brain.Decide(0.4, Ctx(dangers: wave, shelters: new[] { shelter }));
        Assert.That(move.Kind, Is.EqualTo(AllyActionKind.MoveTo));
        Assert.That(move.X, Is.EqualTo(9f));

        var brain2 = new AllyBrain();
        var early = new[] { new AllyDanger(1, 0f, 0f, 99f, 1.0, true) };
        brain2.Decide(0.0, Ctx(dangers: early));
        Assert.That(brain2.Decide(0.4, Ctx(dangers: early)).Kind, Is.EqualTo(AllyActionKind.Idle)); // henüz erken
        var late = new[] { new AllyDanger(1, 0f, 0f, 99f, 0.1, true) };
        Assert.That(brain2.Decide(0.9, Ctx(dangers: late)).Kind, Is.EqualTo(AllyActionKind.Dodge));
    }

    [Test]
    public void Task_WalksToAssignedZone_AndHoldsInside()
    {
        var brain = new AllyBrain();
        var zone = new TeamTaskZone(2, 0f, -10f, 2f);
        var walk = brain.Decide(0.0, Ctx(x: 5f, z: 0f, zone: zone));
        Assert.That(walk.Kind, Is.EqualTo(AllyActionKind.MoveTo));
        Assert.That(walk.Z, Is.EqualTo(-10f));
        Assert.That(brain.Decide(1.0, Ctx(x: 0.5f, z: -9.5f, zone: zone)).Kind, Is.EqualTo(AllyActionKind.Idle));
    }

    [Test]
    public void Danger_BeatsTask()
    {
        var brain = new AllyBrain();
        var zone = new TeamTaskZone(2, 0f, -10f, 2f);
        var d = new[] { new AllyDanger(1, 0f, -10f, 3f, 1.0, false) };
        var c = Ctx(x: 0f, z: -10f, zone: zone, dangers: d);
        brain.Decide(0.0, c);
        Assert.That(brain.Decide(0.4, c).Kind, Is.EqualTo(AllyActionKind.MoveTo));
    }

    [Test]
    public void Idle_MeleeHitsClaw_RangedHitsHead()
    {
        var brain = new AllyBrain();
        var claw = new AllyTarget(10, 1f, 0f);
        var head = new AllyTarget(11, 0f, 12f);
        var melee = brain.Decide(0.0, Ctx(role: AllyRole.Melee, claw: claw, head: head));
        Assert.That(melee.Kind, Is.EqualTo(AllyActionKind.Attack));
        Assert.That(melee.TargetId, Is.EqualTo(10));

        var ranged = brain.Decide(0.0, Ctx(role: AllyRole.Ranged, x: 0f, z: 0f, claw: claw, head: head));
        Assert.That(ranged.Kind, Is.EqualTo(AllyActionKind.MoveTo)); // menzil 2 m, baş 12 m uzakta: yaklaşır
        Assert.That(ranged.Z, Is.GreaterThan(0f));
        Assert.That(ranged.Z, Is.LessThan(12f));
    }

    [Test]
    public void Idle_FollowsPlayerOnlyWhenFar()
    {
        var brain = new AllyBrain();
        Assert.That(brain.Decide(0.0, Ctx(x: 0f, z: 0f, px: 3f, pz: 0f)).Kind, Is.EqualTo(AllyActionKind.Idle));
        var far = brain.Decide(0.0, Ctx(x: 0f, z: 0f, px: 12f, pz: 0f));
        Assert.That(far.Kind, Is.EqualTo(AllyActionKind.MoveTo));
        Assert.That(far.X, Is.EqualTo(12f));
    }
}
