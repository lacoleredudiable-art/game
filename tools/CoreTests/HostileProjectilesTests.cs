using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace CoreTests;

/// <summary>Düşman mermi simülasyonu (boss PR 2): deterministik hareket, sorgular, olaylar, tavan.</summary>
[TestFixture]
public sealed class HostileProjectilesTests
{
    [Test]
    public void Tick_IntegratesAndExpires()
    {
        var sim = new HostileProjectiles();
        int id = sim.Spawn(1, 1, 0f, 0f, 2f, 0f, 0.35f, 6f, 0, 1000);
        sim.Tick(500, 500);
        Assert.That(sim.TryGet(id, out Projectile p), Is.True);
        Assert.That(p.X, Is.EqualTo(1f).Within(1e-4));
        sim.Tick(1000, 500);
        Assert.That(sim.TryGet(id, out _), Is.False, "life 1000 ms → dies at 1000");
        Assert.That(sim.AliveCount, Is.EqualTo(0));
    }

    [Test]
    public void SameInputs_SamePositions()
    {
        float[] Run()
        {
            var sim = new HostileProjectiles();
            for (int i = 0; i < 5; i++)
                sim.Spawn(1, 1, i, -i, 1.5f + i, 0.5f * i, 0.35f, 6f, 0, 5000);
            for (int t = 1; t <= 60; t++)
                sim.Tick(t * 33.3, 33.3);
            var xs = new List<float>();
            for (int i = 0; i < sim.MaxAlive; i++)
            {
                Projectile p = sim.Slot(i);
                if (p.Alive) { xs.Add(p.X); xs.Add(p.Z); }
            }
            return xs.ToArray();
        }
        Assert.That(Run(), Is.EqualTo(Run()));
    }

    [Test]
    public void CircleAndSegmentQueries_IncludeProjectileRadius()
    {
        var sim = new HostileProjectiles();
        int near = sim.Spawn(1, 1, 1.2f, 0f, 0f, 0f, 0.3f, 1f, 0, 5000);
        int far = sim.Spawn(1, 1, 5f, 5f, 0f, 0f, 0.3f, 1f, 0, 5000);
        Span<int> ids = stackalloc int[8];
        int n = sim.QueryCircle(0f, 0f, 1f, ids);
        Assert.That(n, Is.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(near));

        n = sim.QuerySegment(0f, 5.3f, 10f, 5.3f, 0.1f, ids);
        Assert.That(n, Is.EqualTo(1));
        Assert.That(ids[0], Is.EqualTo(far));
        Assert.That(sim.QueryCircle(0f, 0f, 1f, ids, team: 0), Is.EqualTo(0), "team filter");
    }

    [Test]
    public void DeleteAndReflect_RaiseEventsAndCount()
    {
        var sim = new HostileProjectiles();
        var seen = new List<ProjectileEventKind>();
        sim.Events += e => seen.Add(e.Kind);
        int a = sim.Spawn(1, 1, 0f, 0f, 1f, 0f, 0.35f, 6f, 0, 5000, targetId: 2);
        int b = sim.Spawn(1, 1, 0f, 0f, 1f, 0f, 0.35f, 6f, 0, 5000);
        Assert.That(sim.Delete(a, ProjectileEventKind.Absorbed), Is.True);
        Assert.That(sim.Delete(a, ProjectileEventKind.Absorbed), Is.False, "already gone");
        Assert.That(sim.Reflect(b, 0, 3f, -1f, 0f), Is.True);
        Assert.That(sim.TryGet(b, out Projectile p), Is.True);
        Assert.That(p.Team, Is.EqualTo((byte)0));
        Assert.That(p.Reflected, Is.True);
        Assert.That(p.VX, Is.EqualTo(-3f).Within(1e-4));
        Assert.That(sim.AbsorbedTotal, Is.EqualTo(1));
        Assert.That(sim.ReflectedTotal, Is.EqualTo(1));
        Assert.That(seen, Is.EqualTo(new[]
        {
            ProjectileEventKind.Spawned, ProjectileEventKind.Spawned,
            ProjectileEventKind.Absorbed, ProjectileEventKind.Reflected
        }));
    }

    [Test]
    public void Reflect_WithoutDirection_FlipsVelocity()
    {
        var sim = new HostileProjectiles();
        int id = sim.Spawn(1, 1, 0f, 0f, 2f, 1f, 0.35f, 6f, 0, 5000);
        sim.Reflect(id, 0, 1f);
        sim.TryGet(id, out Projectile p);
        Assert.That(p.VX, Is.EqualTo(-2f).Within(1e-4));
        Assert.That(p.VZ, Is.EqualTo(-1f).Within(1e-4));
    }

    [Test]
    public void DropTarget_CountsOncePerProjectile()
    {
        var sim = new HostileProjectiles();
        int id = sim.Spawn(1, 1, 0f, 0f, 1f, 0f, 0.35f, 6f, 0, 5000, targetId: 7, homing: true);
        Assert.That(sim.DropTarget(id), Is.True);
        Assert.That(sim.DropTarget(id), Is.False);
        sim.TryGet(id, out Projectile p);
        Assert.That(p.TargetId, Is.EqualTo(-1));
        Assert.That(p.Homing, Is.False);
        Assert.That(sim.ShroudedTotal, Is.EqualTo(1));
    }

    [Test]
    public void Cap_RejectsBeyondMaxAlive_AndPeakNeverExceeds()
    {
        var sim = new HostileProjectiles();
        Assert.That(sim.MaxAlive, Is.EqualTo(40));
        for (int i = 0; i < 45; i++)
            sim.Spawn(1, 1, 0f, 0f, 0f, 0f, 0.35f, 1f, 0, 5000);
        Assert.That(sim.AliveCount, Is.EqualTo(40));
        Assert.That(sim.PeakAlive, Is.EqualTo(40));
        Assert.That(sim.RejectedTotal, Is.EqualTo(5));
        sim.Clear();
        Assert.That(sim.AliveCount, Is.EqualTo(0));
        Assert.That(sim.Spawn(1, 1, 0f, 0f, 0f, 0f, 0.35f, 1f, 0, 5000), Is.GreaterThan(0));
    }

    [Test]
    public void TimeToImpact_ApproachingFinite_RecedingInfinite()
    {
        var sim = new HostileProjectiles();
        int a = sim.Spawn(1, 1, 0f, 0f, 2f, 0f, 0.5f, 1f, 0, 5000);
        int b = sim.Spawn(1, 1, 0f, 0f, -2f, 0f, 0.5f, 1f, 0, 5000);
        sim.TryGet(a, out Projectile pa);
        sim.TryGet(b, out Projectile pb);
        Assert.That(HostileProjectiles.TimeToImpactMs(pa, 5f, 0f, 0.5f), Is.EqualTo(2000).Within(1));
        Assert.That(double.IsPositiveInfinity(HostileProjectiles.TimeToImpactMs(pb, 5f, 0f, 0.5f)), Is.True);
    }
}
