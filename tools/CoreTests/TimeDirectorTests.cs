using Dovus.Core.Time;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class TimeDirectorTests
{
    TimeDirector _director = null!;

    [SetUp]
    public void SetUp()
    {
        _director = new TimeDirector();
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
    public void Hitstop_StopsWorldClockUntilDurationEnds()
    {
        const int hitstopMs = 90;
        double worldBeforeHitstop = _director.WorldTimeMs;

        _director.TriggerHitstop(hitstopMs);
        Assert.That(_director.TimeScale, Is.EqualTo(0f));
        Assert.That(_director.IsHitstopActive, Is.True);

        Advance(_director, hitstopMs / 2);
        Assert.That(_director.WorldTimeMs, Is.EqualTo(worldBeforeHitstop));

        Advance(_director, hitstopMs / 2);
        Assert.That(_director.IsHitstopActive, Is.False);
        Assert.That(_director.TimeScale, Is.EqualTo(1f));

        Advance(_director, 10);
        Assert.That(_director.WorldTimeMs, Is.EqualTo(worldBeforeHitstop + 10));
    }

    [Test]
    public void RealClock_AdvancesDuringHitstop()
    {
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
    public void NonPositiveHitstop_IsIgnored()
    {
        _director.TriggerHitstop(0);
        _director.TriggerHitstop(-10);

        Assert.That(_director.IsHitstopActive, Is.False);
        Assert.That(_director.Tick(16), Is.EqualTo(16));
    }

    [Test]
    public void Tick_ReturnsZeroDuringHitstop_ThenRealtimeDelta()
    {
        _director.TriggerHitstop(10);

        Assert.That(_director.Tick(5), Is.Zero);
        Assert.That(_director.Tick(5), Is.Zero);
        Assert.That(_director.Tick(10), Is.EqualTo(10));
    }
}
