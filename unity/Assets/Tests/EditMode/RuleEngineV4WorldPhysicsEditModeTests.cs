using Dovus.Core.RuleEngineV4;
using NUnit.Framework;

namespace Dovus.Tests.EditMode
{
    public sealed class RuleEngineV4WorldPhysicsEditModeTests
    {
        [SetUp]
        public void SetUp() => RuleEngineV4WorldPhysicsRuntime.Bind(RuleEngineV4WorldPhysics.Default);

        [Test]
        public void Anchored_IsImmovableLikeBoss()
        {
            Assert.That(RuleEngineV4WeightRules.IsImmovable(RuleEngineV4WeightTier.Anchored), Is.True);
            Assert.That(
                RuleEngineV4WeightRules.CanDisplace(RuleEngineV4WeightTier.Heavy, RuleEngineV4WeightTier.Anchored),
                Is.False);
        }

        [Test]
        public void MotionHandoff_UsesJsonCap()
        {
            float speed = RuleEngineV4MotionHandoff.EffectiveSpeedMps(10f, 20f);
            Assert.That(speed, Is.EqualTo(15f));
        }
    }
}
