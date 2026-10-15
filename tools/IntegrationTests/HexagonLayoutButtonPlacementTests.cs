using System;
using Dovus.Core.Input;
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
    public void Swap_DoesNotOverlapHexPanel_OnTypicalScreens()
    {
        var tuning = new GameTuning();
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

    static void AssertNoHudOverlap(GameTuning tuning, int w, int h, bool mirror)
    {
        tuning.Input.MirrorForLeftHand = mirror;
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
            HexagonLayoutScreen.WeaponSwapButtonPx(tuning, w, h),
            HexagonLayoutScreen.WeaponSwapButtonRadiusPx(tuning),
            forbidden,
            gap,
            $"swap {w}x{h} mirror={mirror}");
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

    // Arayüz sadeleştirme: altıgen + dodge birlikte biraz sağa (0,78 → 0,81); yarıçap küçülmemeli.
    [Test]
    public void HexagonShiftedRight_RadiusKept_DodgeInsideSafe()
    {
        var shifted = new GameTuning();
        shifted.EnsureRuntimeDefaults();
        var old = new GameTuning();
        old.EnsureRuntimeDefaults();
        old.Input.HexagonCenterXNorm = 0.78f;
        HexagonLayoutScreen.DebugDpiOverride = 400f;
        HexagonLayoutScreen.FitShortSideDp = 0f;

        Assert.That(shifted.Input.HexagonCenterXNorm, Is.GreaterThan(0.78f), "altıgen sağa kaymadı");
        foreach (var (w, h) in Screens)
        {
            Screen.SetResolution(w, h, true);
            Assert.That(
                HexagonLayoutScreen.CenterPx(shifted, w, h).x,
                Is.GreaterThan(HexagonLayoutScreen.CenterPx(old, w, h).x),
                $"merkez sağa kaymadı {w}x{h}");
            Assert.That(
                HexagonLayoutScreen.FittedRadiusPx(shifted, w, h),
                Is.EqualTo(HexagonLayoutScreen.FittedRadiusPx(old, w, h)).Within(0.5f),
                $"yarıçap küçüldü {w}x{h}");
            Vector2 dodge = HexagonLayoutScreen.DodgeButtonPx(shifted, w, h);
            float dodgeR = HexagonLayoutScreen.DodgeButtonRadiusPx(shifted);
            Assert.That(dodge.x + dodgeR, Is.LessThanOrEqualTo(HexagonLayoutScreen.SafeRectPx().xMax + 0.5f),
                $"dodge safe dışına taştı {w}x{h}");
        }
    }
}
