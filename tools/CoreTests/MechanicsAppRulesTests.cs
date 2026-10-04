using Dovus.App.Mechanics;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class MechanicsAppRulesTests
{
    [Test]
    public void PortalTransition_ResetsInsideWhenLeavingBothGates()
    {
        Assert.That(PortalTransitionRules.ShouldResetInside(false, false), Is.True);
        Assert.That(PortalTransitionRules.ShouldResetInside(true, false), Is.False);
    }

    [Test]
    public void PortalTransition_TeleportsWhenOutsideCooldownAndInGate()
    {
        Assert.That(PortalTransitionRules.ShouldTeleport(true, true, false), Is.False);
        Assert.That(PortalTransitionRules.ShouldTeleport(false, false, true), Is.True);
    }

    [Test]
    public void DamageRedirect_SplitMatchesBossStatusMath()
    {
        float left = MechanicDamageRedirectRules.ApplyShare(100f, 0.25f, out float redirected);
        Assert.That(redirected, Is.EqualTo(25f));
        Assert.That(left, Is.EqualTo(75f));
    }

    [Test]
    public void GuardEligibility_FiresWhenLowAndNotExpired()
    {
        Assert.That(MechanicGuardEligibilityRules.ShouldFire(false, true, false), Is.True);
        Assert.That(MechanicGuardEligibilityRules.ShouldFire(true, true, false), Is.False);
    }

    [Test]
    public void GuardEligibility_PlayerLowRatio()
    {
        Assert.That(MechanicGuardEligibilityRules.PlayerLow(2f, 10f, 0.25), Is.True);
        Assert.That(MechanicGuardEligibilityRules.PlayerLow(5f, 10f, 0.25), Is.False);
    }
}
