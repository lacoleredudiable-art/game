using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
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
    public void Promote_KeepsDisplacementContinuous()
    {
        var tuning = Tuning();
        tuning.CombinedDistanceMult = 1.6f;
        tuning.CombinedDurationMult = 1.4f;
        var dodge = new DodgeState(tuning);
        dodge.Begin(1000);
        float before = dodge.GetDisplacementRatio(1100) * dodge.DistanceMultiplier;
        dodge.PromoteToCombined(1100);
        float after = dodge.GetDisplacementRatio(1100) * dodge.DistanceMultiplier;
        Assert.That(after, Is.EqualTo(before).Within(1e-4f));
        float end = dodge.GetDisplacementRatio(1000 + tuning.StartupMs + dodge.MoveDurationMs) * dodge.DistanceMultiplier;
        Assert.That(end, Is.EqualTo(1.6f).Within(1e-4f));
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
