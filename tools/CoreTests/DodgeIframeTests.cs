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
public class DodgeIframeTests
{
    [Test]
    public void Iframe_StartsAtDodgeStart_AndEndsExclusively()
    {
        var tuning = new DodgeTuning();
        var dodge = new DodgeState(tuning);
        dodge.Begin(1000);

        Assert.That(tuning.IframeStartMs, Is.EqualTo(0));
        Assert.That(tuning.IframeMs, Is.EqualTo(260));
        Assert.That(dodge.IsInvulnerable(1000), Is.True);
        Assert.That(dodge.IsInvulnerable(1259), Is.True);
        Assert.That(dodge.IsInvulnerable(1260), Is.False);
        Assert.That(dodge.IsInvulnerable(999), Is.False);
    }

    [Test]
    public void Charges_SpendAndRechargeOneAtATime()
    {
        var bank = new DodgeChargeBank(new DodgeTuning { MaxCharges = 2, ChargeRechargeMs = 6000 });
        Assert.That(bank.Ready, Is.EqualTo(2));
        Assert.That(bank.TrySpend(0), Is.True);
        Assert.That(bank.TrySpend(0), Is.True);
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(bank.TrySpend(0), Is.False);

        bank.Tick(5999);
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(bank.Fill01, Is.GreaterThan(0.9f));

        bank.Tick(6000);
        Assert.That(bank.Ready, Is.EqualTo(1));
        bank.Tick(12000);
        Assert.That(bank.Ready, Is.EqualTo(2));
        Assert.That(bank.Fill01, Is.EqualTo(1f));
    }

    [Test]
    public void Cancel_ReleasesTemplateOwnership_AndKeepsCooldownSpent()
    {
        var lease = new SkillCastLease();
        lease.Arm(templateOwnsPosition: true);
        Assert.That(lease.OwnsPosition, Is.True);
        Assert.That(lease.EffectsLive, Is.True);
        Assert.That(lease.CooldownSpent, Is.True);

        Assert.That(DodgeCancelRules.Allowed(dead: false, stun: false, freeze: false, knockdown: false), Is.True);
        lease.CancelForDodge();
        Assert.That(lease.OwnsPosition, Is.False);
        Assert.That(lease.EffectsLive, Is.False);
        Assert.That(lease.CooldownSpent, Is.True);
    }

    [Test]
    public void Cancel_HardCcDoesNotRelease()
    {
        var lease = new SkillCastLease();
        lease.Arm(true);

        Assert.That(DodgeCancelRules.Allowed(dead: false, stun: true, freeze: false, knockdown: false), Is.False);
        Assert.That(DodgeCancelRules.Allowed(dead: false, stun: false, freeze: true, knockdown: false), Is.False);
        Assert.That(DodgeCancelRules.Allowed(dead: false, stun: false, freeze: false, knockdown: true), Is.False);
        Assert.That(lease.OwnsPosition, Is.True);
        Assert.That(lease.EffectsLive, Is.True);
    }

    [Test]
    public void PerfectWindow_IsTheFirstSliceOfIframes()
    {
        var tuning = new DodgeTuning();
        int press = 5000;
        Assert.That(PerfectDodgeRule.InWindow(press, press, tuning.IframeStartMs, tuning.IframeMs, tuning.PerfectWindowMs), Is.True);
        Assert.That(PerfectDodgeRule.InWindow(press, press + 149, tuning.IframeStartMs, tuning.IframeMs, tuning.PerfectWindowMs), Is.True);
        Assert.That(PerfectDodgeRule.InWindow(press, press + 150, tuning.IframeStartMs, tuning.IframeMs, tuning.PerfectWindowMs), Is.False);
        Assert.That(PerfectDodgeRule.InWindow(press, press + 200, tuning.IframeStartMs, tuning.IframeMs, tuning.PerfectWindowMs), Is.False);
        Assert.That(PerfectDodgeRule.InWindow(press, press + 299, tuning.IframeStartMs, tuning.IframeMs, 0), Is.False);
    }

    [Test]
    public void PerfectRefund_ReturnsOneCharge_WithoutPassingMax()
    {
        var bank = new DodgeChargeBank(new DodgeTuning { MaxCharges = 2, ChargeRechargeMs = 6000 });
        bank.TrySpend(0);
        bank.TrySpend(0);
        Assert.That(bank.Ready, Is.EqualTo(0));

        bank.Refund(1f);
        Assert.That(bank.Ready, Is.EqualTo(1));
        bank.Refund(1f);
        bank.Refund(1f);
        Assert.That(bank.Ready, Is.EqualTo(2));
    }

    [Test]
    public void RefundFraction_FillsHalfACharge()
    {
        var bank = new DodgeChargeBank(new DodgeTuning { MaxCharges = 2, ChargeRechargeMs = 6000 });
        bank.TrySpend(0);
        bank.TrySpend(0);
        bank.Refund(0.5f);
        Assert.That(bank.Ready, Is.EqualTo(0));
        Assert.That(bank.Fill01, Is.EqualTo(0.5f).Within(0.02f));
    }

    [Test]
    public void NextHitBuff_ConsumesOnce()
    {
        var buff = new NextHitBuff();
        Assert.That(buff.Multiplier, Is.EqualTo(1f));
        Assert.That(buff.IsArmed, Is.False);

        buff.Arm(1.3f);
        Assert.That(buff.Consume(), Is.EqualTo(1.3f).Within(0.0001f));
        Assert.That(buff.Consume(), Is.EqualTo(1f));
        Assert.That(buff.IsArmed, Is.False);
    }

    [Test]
    public void Edge_PushOutOfBoss_LeavesOutsidePoints()
    {
        float x = 0f;
        float z = 0f;
        DodgeEdge.KeepOutside(ref x, ref z, 0f, 0f, 2f, 1f, 0f);
        Assert.That(x, Is.EqualTo(-2f).Within(0.001f));
        Assert.That(z, Is.EqualTo(0f).Within(0.001f));

        float inside = 1f;
        float zIn = 0f;
        DodgeEdge.KeepOutside(ref inside, ref zIn, 0f, 0f, 2f, 1f, 0f);
        Assert.That(inside, Is.EqualTo(2f).Within(0.001f));

        float outside = 5f;
        float zOut = 1f;
        DodgeEdge.KeepOutside(ref outside, ref zOut, 0f, 0f, 2f, 1f, 0f);
        Assert.That(outside, Is.EqualTo(5f).Within(0.001f));
        Assert.That(zOut, Is.EqualTo(1f).Within(0.001f));
    }

    [Test]
    public void Edge_DodgeTowardBoss_StopsOnTheNearFace()
    {
        float x = 0f;
        float z = 4f;
        DodgeEdge.StopBeforeCrossing(ref x, ref z, 0f, 0f, 0f, 1.8f, 1.5f);
        Assert.That(x, Is.EqualTo(0f).Within(0.02f));
        Assert.That(z, Is.EqualTo(0.3f).Within(0.02f));
        Assert.That(z, Is.LessThan(1.8f));

        float awayX = 0f;
        float awayZ = -4f;
        DodgeEdge.StopBeforeCrossing(ref awayX, ref awayZ, 0f, 0f, 0f, 1.8f, 1.5f);
        Assert.That(awayZ, Is.EqualTo(-4f).Within(0.001f));

        float shortX = 0f;
        float shortZ = 0.2f;
        DodgeEdge.StopBeforeCrossing(ref shortX, ref shortZ, 0f, 0f, 0f, 1.8f, 1.5f);
        Assert.That(shortZ, Is.EqualTo(0.2f).Within(0.001f));
    }

    [Test]
    public void Direction_UsesStick_OrBackpedal()
    {
        DodgeDirection.Resolve(0f, 1f, 1f, 0f, out float x, out float z);
        Assert.That(x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(z, Is.EqualTo(1f).Within(0.001f));

        DodgeDirection.Resolve(0f, 0f, 0f, 1f, out x, out z);
        Assert.That(x, Is.EqualTo(0f).Within(0.001f));
        Assert.That(z, Is.EqualTo(-1f).Within(0.001f));
    }
}
