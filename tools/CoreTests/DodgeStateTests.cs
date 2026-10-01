using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class DodgeStateTests
{
    /// <summary>S2: tek kapı haklardır — aynı karede ikinci basış hak varsa kabul edilir.</summary>
    [Test]
    public void Charges_AreTheOnlyGate_BackToBackDodgeAllowedWithCharge()
    {
        var tuning = new DodgeTuning();
        var charges = new DodgeChargeBank(tuning);
        Assert.That(charges.TrySpend(0), Is.True);
        Assert.That(charges.TrySpend(1), Is.True, "2 hak: arka arkaya ikinci kaçış serbest");
        Assert.That(charges.TrySpend(2), Is.False, "hak bitti");
    }

    [Test]
    public void Begin_StartsIframeAtPress()
    {
        var dodge = new DodgeState(new DodgeTuning());
        dodge.Begin(1000);
        Assert.That(dodge.IsInvulnerable(1000), Is.True);
        Assert.That(dodge.IsInvulnerable(1259), Is.True);
        Assert.That(dodge.IsInvulnerable(1260), Is.False);
    }
}
