using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class DodgeChainTests
{
    static DodgeTuning Tuning() => new DodgeTuning
    {
        MaxCharges = 2,
        ChargeRechargeMs = 6000,
        DoubleTapWindowMs = 200,
        CombinedIframeMs = 520,
        IframeMs = 260,
        IframeStartMs = 0,
        DurationMs = 250,
        StartupMs = 10,
        GlideTailMs = 120
    };

    [Test]
    public void Recharge_IsSixSecondsPerCharge()
    {
        var bank = new DodgeChargeBank(Tuning());
        bank.TrySpend(0);
        bank.TrySpend(0);
        Assert.That(bank.Ready, Is.EqualTo(0));

        bank.Tick(5999);
        Assert.That(bank.Ready, Is.EqualTo(0));

        bank.Tick(6000);
        Assert.That(bank.Ready, Is.EqualTo(1));
        bank.Tick(12000);
        Assert.That(bank.Ready, Is.EqualTo(2));
    }

    [Test]
    public void DoubleTapInsideWindow_SpendsTwoCharges_CombinedIframe520()
    {
        var tuning = Tuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);

        Assert.That(DodgeChain.TryConsumePress(1000, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(bank.Ready, Is.EqualTo(1));

        Assert.That(DodgeChain.TryConsumePress(1150, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.UpgradedCombined));
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(dodge.IsCombined, Is.True);
        Assert.That(dodge.IsInvulnerable(1519), Is.True);
        Assert.That(dodge.IsInvulnerable(1520), Is.False);
    }

    [Test]
    public void DoubleTapOutsideWindow_TwoSeparateDodges()
    {
        var tuning = Tuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning);

        Assert.That(DodgeChain.TryConsumePress(0, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(DodgeChain.TryConsumePress(250, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(dodge.IsCombined, Is.False);
        Assert.That(dodge.PressTimeMs, Is.EqualTo(250));
        Assert.That(dodge.IsInvulnerable(509), Is.True);
        Assert.That(dodge.IsInvulnerable(510), Is.False);
    }

    [Test]
    public void SingleCharge_DoubleTap_SecondDenied_NormalDodgeOnly()
    {
        var tuning = Tuning();
        var dodge = new DodgeState(tuning);
        var bank = new DodgeChargeBank(tuning, startReady: 1);

        Assert.That(DodgeChain.TryConsumePress(0, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.StartedNew));
        Assert.That(DodgeChain.TryConsumePress(50, dodge, bank, tuning), Is.EqualTo(DodgeChain.PressOutcome.DeniedNoCharge));
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(dodge.IsCombined, Is.False);
        Assert.That(dodge.IsInvulnerable(259), Is.True);
    }

    [Test]
    public void PerfectRefund_OncePerCombinedDodgePress()
    {
        var tuning = Tuning();
        var bank = new DodgeChargeBank(tuning);
        bank.TrySpend(0);
        bank.TrySpend(0);
        Assert.That(bank.Ready, Is.EqualTo(0));

        bank.Refund(tuning.PerfectChargeRefund);
        Assert.That(bank.Ready, Is.EqualTo(1));
        bank.Refund(tuning.PerfectChargeRefund);
        Assert.That(bank.Ready, Is.EqualTo(2));
    }
}
