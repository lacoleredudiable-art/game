using System;
using Dovus.Core.Layout;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using NUnit.Framework;
using UnityEngine;

namespace IntegrationTests;

[TestFixture]
public class HexagonLayoutButtonPlacementTests
{
    static readonly (int w, int h)[] Screens =
    {
        (2400, 1080),
        (1920, 1080),
        (2340, 1080),
        (1600, 720),
    };

    [Test]
    public void LockOnAndSwap_DoNotOverlapHexPanel_OnTypicalScreens()
    {
        var tuning = new PrototypeTuning();
        tuning.EnsureRuntimeDefaults();
        HexagonLayoutScreen.DebugDpiOverride = 400f;
        HexagonLayoutScreen.FitShortSideDp = 0f;

        foreach (var (w, h) in Screens)
        {
            Screen.SetResolution(w, h, true);
            AssertNoHudOverlap(tuning, w, h, mirror: false);
            AssertNoHudOverlap(tuning, w, h, mirror: true);
        }
    }

    static void AssertNoHudOverlap(PrototypeTuning tuning, int w, int h, bool mirror)
    {
        tuning.MirrorForLeftHand = mirror;
        float gap = HexagonLayoutScreen.DpToPixels(12f);
        Vector2 panel = HexagonLayoutScreen.CenterPx(tuning, w, h);
        float fitted = HexagonLayoutScreen.FittedRadiusPx(tuning, w, h);
        float dotR = HexagonLayoutScreen.DotHitRadiusPx(tuning);
        Vector2 dodge = HexagonLayoutScreen.DodgeButtonPx(tuning, w, h);
        float dodgeR = HexagonLayoutScreen.DodgeButtonRadiusPx(tuning);

        var forbidden = new Circle2[8];
        forbidden[0] = new Circle2(panel.x, panel.y, fitted + dotR);
        for (int dot = 1; dot <= 6; dot++)
        {
            Vector2 dotPx = HexagonLayoutScreen.DotPx(dot, tuning, w, h);
            forbidden[dot] = new Circle2(dotPx.x, dotPx.y, dotR);
        }

        forbidden[7] = new Circle2(dodge.x, dodge.y, dodgeR);

        AssertButtonClear(
            HexagonLayoutScreen.LockOnButtonPx(tuning, w, h),
            HexagonLayoutScreen.LockOnButtonRadiusPx(tuning),
            forbidden,
            gap,
            $"lock-on {w}x{h} mirror={mirror}");

        AssertButtonClear(
            HexagonLayoutScreen.WeaponSwapButtonPx(tuning, w, h),
            HexagonLayoutScreen.WeaponSwapButtonRadiusPx(tuning),
            forbidden,
            gap,
            $"swap {w}x{h} mirror={mirror}");

        Vector2 lockC = HexagonLayoutScreen.LockOnButtonPx(tuning, w, h);
        Vector2 swapC = HexagonLayoutScreen.WeaponSwapButtonPx(tuning, w, h);
        float lockSwapEdge = HudButtonPlacement.EdgeGap(
            new Circle2(lockC.x, lockC.y, HexagonLayoutScreen.LockOnButtonRadiusPx(tuning)),
            new Circle2(swapC.x, swapC.y, HexagonLayoutScreen.WeaponSwapButtonRadiusPx(tuning)));
        Assert.That(lockSwapEdge, Is.GreaterThanOrEqualTo(0f), $"lock-on/swap overlap {w}x{h} mirror={mirror} edge={lockSwapEdge:0.##}");
    }

    static void AssertButtonClear(Vector2 center, float radius, Circle2[] forbidden, float gap, string label)
    {
        var button = new Circle2(center.x, center.y, radius);
        for (int i = 0; i < forbidden.Length; i++)
        {
            float edge = HudButtonPlacement.EdgeGap(button, forbidden[i]);
            Assert.That(
                edge,
                Is.GreaterThanOrEqualTo(gap - 0.5f),
                $"{label}: forbidden[{i}] edge={edge:0.##} gap={gap:0.##}");
        }
    }
}
