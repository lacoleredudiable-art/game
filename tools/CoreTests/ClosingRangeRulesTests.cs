using Dovus.App.Casting;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class ClosingRangeRulesTests
{
    [Test]
    public void IsClosingInRange_NullLogic_False()
    {
        Assert.That(
            ClosingRangeRules.IsClosingInRange(null, 0f, 0f, Rune.Ates, 2f, 0f),
            Is.False);
    }

    [Test]
    public void PlanarMath_FlatDistance_IgnoresY()
    {
        Assert.That(PlanarMath.FlatDistance(0f, 0f, 3f, 4f), Is.EqualTo(5f).Within(0.001f));
    }
}
