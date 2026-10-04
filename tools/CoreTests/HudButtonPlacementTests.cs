using Dovus.Core.Layout;
using NUnit.Framework;

namespace CoreTests;

[TestFixture]
public class HudButtonPlacementTests
{
    static Circle2[] SingleForbidden(float x, float y, float r) =>
        new[] { new Circle2(x, y, r) };

    [Test]
    public void ClearPosition_Unchanged()
    {
        float x = 400f;
        float y = 200f;
        float beforeX = x;
        float beforeY = y;
        var forbidden = SingleForbidden(50f, 50f, 20f);
        HudButtonPlacement.ResolveAwayFromForbidden(
            ref x,
            ref y,
            18f,
            forbidden,
            12f,
            new Circle2(300f, 300f, 30f),
            100f,
            100f);
        Assert.That(x, Is.EqualTo(beforeX));
        Assert.That(y, Is.EqualTo(beforeY));
    }

    [Test]
    public void OverlappingPosition_SeparatesWithGap()
    {
        float x = 100f;
        float y = 100f;
        var forbidden = SingleForbidden(100f, 100f, 40f);
        const float gap = 12f;
        const float buttonR = 20f;
        HudButtonPlacement.ResolveAwayFromForbidden(
            ref x,
            ref y,
            buttonR,
            forbidden,
            gap,
            new Circle2(200f, 200f, 25f),
            0f,
            0f);

        var button = new Circle2(x, y, buttonR);
        Assert.That(HudButtonPlacement.EdgeGap(button, forbidden[0]), Is.GreaterThanOrEqualTo(gap - 0.01f));
    }

    [Test]
    public void MirroredOrbit_PrefersAwayFromPanel()
    {
        float x = 120f;
        float y = 80f;
        var forbidden = new[]
        {
            new Circle2(100f, 100f, 50f),
            new Circle2(200f, 200f, 30f),
        };
        float x0 = x;
        float y0 = y;
        HudButtonPlacement.ResolveAwayFromForbidden(
            ref x,
            ref y,
            16f,
            forbidden,
            10f,
            new Circle2(150f, 150f, 22f),
            100f,
            100f);

        Assert.That(x != x0 || y != y0, Is.True);
        var button = new Circle2(x, y, 16f);
        for (int i = 0; i < forbidden.Length; i++)
            Assert.That(HudButtonPlacement.EdgeGap(button, forbidden[i]), Is.GreaterThanOrEqualTo(9.5f));
    }
}
