using Dovus.App.Casting;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class SelfDamageBuffRulesTests
{
    [Test]
    public void DamageMultiplier_expired_buff_returns_one()
    {
        Assert.That(SelfDamageBuffRules.DamageMultiplier(1000, 500, 0.5f), Is.EqualTo(1f));
    }

    [Test]
    public void DamageMultiplier_active_buff_adds_magnitude()
    {
        Assert.That(SelfDamageBuffRules.DamageMultiplier(100, 500, 0.25f), Is.EqualTo(1.25f));
    }

    [Test]
    public void DamageMultiplier_at_exact_expiry_is_one()
    {
        Assert.That(SelfDamageBuffRules.DamageMultiplier(500, 500, 1f), Is.EqualTo(1f));
    }

    [Test]
    public void DamageMultiplier_zero_magnitude_is_one_while_active()
    {
        Assert.That(SelfDamageBuffRules.DamageMultiplier(0, 100, 0f), Is.EqualTo(1f));
    }
}
