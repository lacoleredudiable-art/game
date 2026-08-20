using Dovus.Core.Time;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class TimeDirectorTests
{
    const float Factor = 0.22f;
    const int RampDownMs = 55;
    const int HoldMs = 190;
    const int RampUpMs = 420;
    const int TotalSlowmoMs = RampDownMs + HoldMs + RampUpMs;

    TimeDirector _director = null!;

    [SetUp]
    public void SetUp()
    {
        _director = new TimeDirector(new SlowmoTuning
        {
            Factor = Factor,
            RampDownMs = RampDownMs,
            HoldMs = HoldMs,
            RampUpMs = RampUpMs
        });
    }

    static void Advance(TimeDirector director, double realMs, double stepMs = 1)
    {
        double remaining = realMs;
        while (remaining > 0)
        {
            double dt = remaining >= stepMs ? stepMs : remaining;
            director.Tick(dt);
            remaining -= dt;
        }
    }

    [Test]
    public void SlowmoRamp_ReturnsToOneAfterFullDuration()
    {
        _director.TriggerSlowmo();

        Assert.That(_director.TimeScale, Is.EqualTo(1f).Within(0.0001f));

        Advance(_director, RampDownMs);
        Assert.That(_director.TimeScale, Is.EqualTo(Factor).Within(0.0001f));

        Advance(_director, HoldMs);
        Assert.That(_director.TimeScale, Is.EqualTo(Factor).Within(0.0001f));

        Advance(_director, RampUpMs);
        Assert.That(_director.TimeScale, Is.EqualTo(1f).Within(0.0001f));
        Assert.That(_director.IsSlowmoActive, Is.False);
    }

    [Test]
    public void SlowmoScale_StaysWithinFactorAndOne()
    {
        _director.TriggerSlowmo();

        for (int i = 0; i <= TotalSlowmoMs; i++)
        {
            _director.Tick(1);
            Assert.That(_director.TimeScale, Is.InRange(Factor, 1f),
                $"scale out of range at real t={i}ms, phase active={_director.IsSlowmoActive}");
        }
    }

    [Test]
    public void HitstopDuringSlowmo_PausesSlowmoThenResumes()
    {
        const int hitstopMs = 90;

        _director.TriggerSlowmo();
        Advance(_director, RampDownMs + HoldMs / 2);
        double worldBeforeHitstop = _director.WorldTimeMs;
        float scaleBeforeHitstop = _director.TimeScale;

        Assert.That(scaleBeforeHitstop, Is.EqualTo(Factor).Within(0.0001f));

        _director.TriggerHitstop(hitstopMs);
        Assert.That(_director.TimeScale, Is.EqualTo(0f));
        Assert.That(_director.IsHitstopActive, Is.True);

        Advance(_director, hitstopMs / 2);
        Assert.That(_director.TimeScale, Is.EqualTo(0f));
        Assert.That(_director.WorldTimeMs, Is.EqualTo(worldBeforeHitstop));

        Advance(_director, hitstopMs / 2);
        Assert.That(_director.IsHitstopActive, Is.False);
        Assert.That(_director.TimeScale, Is.EqualTo(Factor).Within(0.0001f));

        double worldAfterResume = _director.WorldTimeMs;
        Advance(_director, 10);
        Assert.That(_director.WorldTimeMs, Is.GreaterThan(worldAfterResume));
    }

    [Test]
    public void RealClock_AdvancesIndependentlyOfWorldClock()
    {
        _director.TriggerSlowmo();
        Advance(_director, RampDownMs + 10);

        double realAtHold = _director.RealTimeMs;
        double worldAtHold = _director.WorldTimeMs;
        Assert.That(_director.TimeScale, Is.EqualTo(Factor).Within(0.0001f));

        Advance(_director, 50);
        Assert.That(_director.RealTimeMs, Is.EqualTo(realAtHold + 50).Within(0.0001));
        Assert.That(_director.WorldTimeMs, Is.EqualTo(worldAtHold + 50 * Factor).Within(0.05));

        _director.TriggerHitstop(80);
        double realAtHitstop = _director.RealTimeMs;
        double worldAtHitstop = _director.WorldTimeMs;

        Advance(_director, 80);
        Assert.That(_director.RealTimeMs, Is.EqualTo(realAtHitstop + 80).Within(0.0001));
        Assert.That(_director.WorldTimeMs, Is.EqualTo(worldAtHitstop).Within(0.0001));
    }

    [Test]
    public void HitstopDuringHitstop_StacksDuration()
    {
        _director.TriggerHitstop(40);
        Advance(_director, 20);
        Assert.That(_director.IsHitstopActive, Is.True);

        _director.TriggerHitstop(30);
        Advance(_director, 40);
        Assert.That(_director.IsHitstopActive, Is.True);

        Advance(_director, 10);
        Assert.That(_director.IsHitstopActive, Is.False);
        Assert.That(_director.TimeScale, Is.EqualTo(1f));
    }

    [Test]
    public void SlowmoDuringSlowmo_RestartsFromRampDown()
    {
        _director.TriggerSlowmo();
        Advance(_director, RampDownMs + HoldMs / 2);

        _director.TriggerSlowmo();
        Assert.That(_director.TimeScale, Is.EqualTo(1f).Within(0.0001f));

        Advance(_director, RampDownMs);
        Assert.That(_director.TimeScale, Is.EqualTo(Factor).Within(0.0001f));
    }

    [Test]
    public void SlowmoDuringHitstop_StartsAfterHitstopEnds()
    {
        _director.TriggerHitstop(50);
        _director.TriggerSlowmo();

        Advance(_director, 50);
        Assert.That(_director.IsHitstopActive, Is.False);
        Assert.That(_director.IsSlowmoActive, Is.True);
        Assert.That(_director.TimeScale, Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void Tick_ReturnsScaledDelta()
    {
        _director.TriggerSlowmo();
        Advance(_director, RampDownMs + 10);

        double scaled = _director.Tick(10);
        Assert.That(_director.TimeScale, Is.EqualTo(Factor).Within(0.0001f));
        Assert.That(scaled, Is.EqualTo(10 * Factor).Within(0.0001));
    }
}
