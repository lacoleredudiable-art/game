using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public sealed class RuleEngineV4WorldPhysicsTests
{
    [Test]
    public void Weight_LightCannotPushHeavy()
    {
        Assert.That(RuleEngineV4WeightRules.CanDisplace(RuleEngineV4WeightTier.Light, RuleEngineV4WeightTier.Heavy), Is.False);
        Assert.That(RuleEngineV4WeightRules.IsImmovable(RuleEngineV4WeightTier.Boss), Is.True);
    }

    [Test]
    public void MotionHandoff_CapsAtOnePointFiveX()
    {
        float speed = RuleEngineV4MotionHandoff.EffectiveSpeedMps(10f, 20f);
        Assert.That(speed, Is.EqualTo(15f));
    }

    [Test]
    public void Diminish_ThirdStackIsQuarter()
    {
        Assert.That(RuleEngineV4DiminishStack.ScaleNonDamage(2, 100f), Is.EqualTo(25f));
        Assert.That(RuleEngineV4DiminishStack.ScaleNonDamage(0, 100f), Is.EqualTo(100f));
    }

    [Test]
    public void StructureLimits_FivePerPlayerTwentyFiveGlobal()
    {
        var limits = new RuleEngineV4StructureLimits();
        for (int i = 0; i < 5; i++)
            Assert.That(limits.TryPlace(0), Is.True);
        Assert.That(limits.TryPlace(0), Is.False);
    }
}
