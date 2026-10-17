using Dovus.Game.Casting;
using Dovus.Game.Config;
using NUnit.Framework;
using UnityEngine;

namespace IntegrationTests;

// Ölçü kimliği: O-4.3 (joystick bölgesi), O-12.7 (kamera sürükleme alanı)
[TestFixture]
public class StickZoneTests
{
    const int W = 2400, H = 1080;

    static bool Zone(float xFrac, float yFrac, bool mirror = false)
    {
        var t = new GameTuning();
        t.EnsureRuntimeDefaults();
        t.Input.MirrorForLeftHand = mirror;
        return HexagonLayoutScreen.IsStickZone(new Vector2(W * xFrac, H * yFrac), t.Input, W, H);
    }

    [Test]
    public void O_4_3_StickZone_LeftBottom_Inside() => Assert.That(Zone(0.20f, 0.30f), Is.True);

    [Test]
    public void O_4_3_StickZone_TopLeft_Outside() => Assert.That(Zone(0.20f, 0.80f), Is.False);

    [Test]
    public void O_4_3_StickZone_BottomCenter_Outside() => Assert.That(Zone(0.45f, 0.30f), Is.False);

    [Test]
    public void O_4_3_StickZone_Mirror_RightBottomInside_LeftBottomOutside()
    {
        Assert.That(Zone(0.80f, 0.30f, mirror: true), Is.True);
        Assert.That(Zone(0.20f, 0.30f, mirror: true), Is.False);
    }

    [Test]
    public void O_4_3_StickZone_Edges()
    {
        Assert.That(Zone(0.3499f, 0.5999f), Is.True);
        Assert.That(Zone(0.3501f, 0.30f), Is.False);
        Assert.That(Zone(0.20f, 0.6001f), Is.False);
    }
}
