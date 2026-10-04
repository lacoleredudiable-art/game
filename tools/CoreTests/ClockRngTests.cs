using Dovus.App.Time;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Shared;
using Dovus.Core.Time;
using NUnit.Framework;
using System;

namespace CoreTests;

[TestFixture]
public class ClockRngTests
{
    [Test]
    public void SharedCombatRng_Seeded_MatchesSystemRandom()
    {
        const int seed = 4242;
        var a = new Random(seed);
        var b = global::Dovus.Core.Shared.CombatRng.Seeded(seed);
        for (int i = 0; i < 20; i++)
            Assert.That(b.NextDouble(), Is.EqualTo(a.NextDouble()));
    }

    [Test]
    public void DamagePipeline_VarianceSeed_StillDeterministic()
    {
        var a = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            ApplyVariance = true,
            VarianceSeed = 4242
        });
        var b = DamagePipeline.Resolve(new DamageQuery
        {
            SkillPower = 100f,
            ApplyVariance = true,
            VarianceSeed = 4242
        });
        Assert.That(b.Amount, Is.EqualTo(a.Amount));
    }

    [Test]
    public void TimeDirectorClock_TracksDirectorWorldTime()
    {
        var director = new TimeDirector();
        var clock = new TimeDirectorClock(director);
        director.Tick(16.0);
        clock.Advance(16.0);
        Assert.That(clock.NowMs, Is.EqualTo(16.0));
        Assert.That(clock.DeltaMs, Is.EqualTo(16.0));
    }

    [Test]
    public void ManualClock_AdvancesNowAndDelta()
    {
        var clock = new ManualClock();
        clock.Set(100.0, 0.0, 0f);
        clock.Advance(50.0, 0.05f);
        Assert.That(clock.NowMs, Is.EqualTo(150.0));
        Assert.That(clock.DeltaMs, Is.EqualTo(50.0));
        Assert.That(clock.DeltaSec, Is.EqualTo(0.05f));
    }
}
