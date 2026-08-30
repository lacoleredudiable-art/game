using Dovus.Core.Time;
using NUnit.Framework;

namespace CoreTests;

/// <summary>
/// Yavaş çekim 30 Ağustos 2026'da kaldırıldı (co-op'ta paylaşılan dünya saatini tek
/// oyuncunun dodge'una göre yavaşlatmak senkron sorunu doğuruyordu, bkz. durum.md).
/// TimeDirector artık yalnızca hitstop taşıyor.
/// </summary>
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
    public void NoHitstop_TimeScaleStaysAtOne_AndClocksMatch()
    {
        Assert.That(_director.TimeScale, Is.EqualTo(1f));

        Advance(_director, 100);
        Assert.That(_director.RealTimeMs, Is.EqualTo(100).Within(0.0001));
        Assert.That(_director.WorldTimeMs, Is.EqualTo(100).Within(0.0001));
    }

    [Test]
    public void TriggerHitstop_FreezesWorldClockButNotRealClock()
    {
        _director.TriggerHitstop(80);
        Assert.That(_director.TimeScale, Is.EqualTo(0f));
        Assert.That(_director.IsHitstopActive, Is.True);

        Advance(_director, 40);
        Assert.That(_director.TimeScale, Is.EqualTo(0f));
        Assert.That(_director.IsHitstopActive, Is.True);
        Assert.That(_director.RealTimeMs, Is.EqualTo(40).Within(0.0001));
        Assert.That(_director.WorldTimeMs, Is.EqualTo(0).Within(0.0001));

        Advance(_director, 40);
        Assert.That(_director.IsHitstopActive, Is.False);
        Assert.That(_director.TimeScale, Is.EqualTo(1f));
        Assert.That(_director.WorldTimeMs, Is.EqualTo(0).Within(0.0001));

        Advance(_director, 10);
        Assert.That(_director.WorldTimeMs, Is.EqualTo(10).Within(0.0001));
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
    public void TriggerHitstop_ZeroOrNegativeDuration_IsNoOp()
    {
        _director.TriggerHitstop(0);
        Assert.That(_director.IsHitstopActive, Is.False);

        _director.TriggerHitstop(-10);
        Assert.That(_director.IsHitstopActive, Is.False);
    }

    [Test]
    public void Tick_ReturnsScaledDelta()
    {
        double scaled = _director.Tick(10);
        Assert.That(scaled, Is.EqualTo(10).Within(0.0001));

        _director.TriggerHitstop(5);
        scaled = _director.Tick(10);
        Assert.That(scaled, Is.EqualTo(0));
    }
}
