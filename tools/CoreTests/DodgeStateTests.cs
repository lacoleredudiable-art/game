using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class DodgeStateTests
{
    [Test]
    public void CooldownMult_Zero_AllowsImmediateReDodge()
    {
        var tuning = new DodgeTuning { CooldownMs = 420 };
        var dodge = new DodgeState(tuning) { CooldownMult = 0f };
        dodge.Begin(1000);
        Assert.That(dodge.IsOnCooldown(1000), Is.False);
        Assert.That(dodge.IsOnCooldown(1001), Is.False);
    }

    [Test]
    public void CooldownMult_Half_ShortensWindow()
    {
        var tuning = new DodgeTuning { CooldownMs = 400 };
        var dodge = new DodgeState(tuning) { CooldownMult = 0.5f };
        dodge.Begin(0);
        Assert.That(dodge.IsOnCooldown(199), Is.True);
        Assert.That(dodge.IsOnCooldown(200), Is.False);
    }
}
