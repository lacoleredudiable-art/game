using System;
using System.Collections.Generic;
using Dovus.Core.Boss;
using NUnit.Framework;

namespace CoreTests;

/// <summary>KE-4.1 küçük tehdit ritmi.</summary>
[TestFixture]
public sealed class KulEjderiThreatSchedulerTests
{
    [TestCase(1, 1)]
    [TestCase(2, 1)]
    [TestCase(3, 2)]
    [TestCase(4, 2)]
    [TestCase(5, 3)]
    public void KE_4_1_Concurrent_count_follows_player_count(int players, int expected)
    {
        Assert.That(KulEjderiThreatScheduler.ConcurrentCount(players), Is.EqualTo(expected));
    }

    [Test]
    public void KE_4_1_Spawns_every_3_to_4_seconds_and_nothing_before_start()
    {
        var scheduler = new KulEjderiThreatScheduler(new Random(1));
        var output = new List<KulEjderiSmallThreat>();
        Assert.That(scheduler.Tick(10000, 1, false, false, output), Is.EqualTo(0));

        scheduler.Start(0);
        var spawnTimes = new List<double>();
        for (double t = 0; t <= 60000; t += 20)
        {
            if (scheduler.Tick(t, 1, false, false, output) > 0)
                spawnTimes.Add(t);
        }

        Assert.That(spawnTimes.Count, Is.GreaterThan(10));
        Assert.That(spawnTimes[0], Is.InRange((double)KulEjderiDefaults.SmallThreatIntervalMinMs, (double)KulEjderiDefaults.SmallThreatIntervalMaxMs));
        for (int i = 1; i < spawnTimes.Count; i++)
        {
            double gap = spawnTimes[i] - spawnTimes[i - 1];
            Assert.That(gap, Is.InRange((double)KulEjderiDefaults.SmallThreatIntervalMinMs, (double)KulEjderiDefaults.SmallThreatIntervalMaxMs + 20));
        }
    }

    [Test]
    public void KE_4_1_Suppressed_scheduler_produces_nothing_and_waits_a_full_interval_after()
    {
        var scheduler = new KulEjderiThreatScheduler(new Random(2));
        var output = new List<KulEjderiSmallThreat>();
        scheduler.Start(0);

        for (double t = 0; t <= 30000; t += 20)
            Assert.That(scheduler.Tick(t, 1, true, false, output), Is.EqualTo(0));
        Assert.That(output, Is.Empty);

        Assert.That(scheduler.Tick(30020, 1, false, false, output), Is.EqualTo(0));
        double first = -1;
        for (double t = 30040; t <= 40000 && first < 0; t += 20)
        {
            if (scheduler.Tick(t, 1, false, false, output) > 0)
                first = t;
        }

        Assert.That(first - 30000, Is.GreaterThanOrEqualTo(KulEjderiDefaults.SmallThreatIntervalMinMs));
    }

    [Test]
    public void KE_3_7_Enraged_interval_is_2_seconds()
    {
        var scheduler = new KulEjderiThreatScheduler(new Random(3));
        var output = new List<KulEjderiSmallThreat>();
        scheduler.Start(0);
        // İlk tehdit eski aralıkla planlıdır; sonrakiler 2 s.
        var times = new List<double>();
        for (double t = 0; t <= 30000; t += 20)
        {
            if (scheduler.Tick(t, 1, false, true, output) > 0)
                times.Add(t);
        }

        for (int i = 1; i < times.Count; i++)
            Assert.That(times[i] - times[i - 1], Is.EqualTo(KulEjderiDefaults.EnrageSmallThreatIntervalMs).Within(20));
    }

    [Test]
    public void KE_4_1_Three_players_spawn_two_threats_at_once_with_design_values()
    {
        var scheduler = new KulEjderiThreatScheduler(new Random(4));
        var output = new List<KulEjderiSmallThreat>();
        scheduler.Start(0);

        for (double t = 0; t <= 4000; t += 20)
            scheduler.Tick(t, 3, false, false, output);

        Assert.That(output.Count, Is.EqualTo(2));
        foreach (KulEjderiSmallThreat threat in output)
        {
            switch (threat.Kind)
            {
                case KulEjderiSmallThreatKind.CrackFlame:
                    Assert.That(threat.TelegraphMs, Is.EqualTo(1000));
                    Assert.That(threat.DamagePct, Is.EqualTo(15f));
                    break;
                case KulEjderiSmallThreatKind.FallingAsh:
                    Assert.That(threat.TelegraphMs, Is.EqualTo(1200));
                    Assert.That(threat.DamagePct, Is.EqualTo(12f));
                    break;
                default:
                    Assert.That(threat.TelegraphMs, Is.EqualTo(900));
                    Assert.That(threat.DamagePct, Is.EqualTo(10f));
                    break;
            }
        }
    }
}
