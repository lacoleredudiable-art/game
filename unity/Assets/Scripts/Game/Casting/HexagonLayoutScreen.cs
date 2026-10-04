using Dovus.Core.Input;
using Dovus.Game.Config;
using UnityEngine;

namespace Dovus.Game.Casting
{
    /// <summary>
    /// Altıgen noktalarının ekran pikseli konumları. Dünyaya bağlı değil (§2).
    /// Nokta sırası 1→6 saat yönünde; komşu = ±1 (Core HexagonLayout ile uyumlu).
    /// </summary>
    public static class HexagonLayoutScreen
    {
        /// <summary>Android notch / home indicator — UI clamp buna göre.</summary>
        public static Rect SafeRectPx()
        {
            Rect sa = Screen.safeArea;
            if (sa.width < 1f || sa.height < 1f)
                return new Rect(0f, 0f, Screen.width, Screen.height);
            return sa;
        }

        public static Vector2 CenterPx(GameTuning tuning, int screenWidth, int screenHeight)
        {
            Rect safe = SafeRectPx();
            float xNorm = tuning.Input.MirrorForLeftHand
                ? 1f - tuning.Input.HexagonCenterXNorm
                : tuning.Input.HexagonCenterXNorm;
            // Norm, safe rect içinde yorumlanır (taşma / home bar).
            float x = safe.xMin + xNorm * safe.width;
            float y = safe.yMin + tuning.Input.HexagonCenterYNorm * safe.height;
            return new Vector2(x, y);
        }

        /// <summary>
        /// İstenen yarıçapı safe-area + dodge boşluğuna sığacak şekilde kısar.
        /// Büyük dp / yüksek DPI telefonda alt rünlerin kesilmesini engeller.
        /// </summary>
        public static float FittedRadiusPx(GameTuning tuning, int screenWidth, int screenHeight)
        {
            float desired = DpToPixels(tuning.Input.HexagonRadiusDp);
            Rect safe = SafeRectPx();
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float dotR = DotHitRadiusPx(tuning);
            float dodgeR = DodgeButtonRadiusPx(tuning);
            float margin = DpToPixels(HexagonLayoutScreenDefaults.FitMarginDp);
            float dodgePad = dodgeR + DpToPixels(Mathf.Max(HexagonLayoutScreenDefaults.MinDodgeClearanceDp, tuning.Input.DodgeClearanceDp));

            float bottomRoom = c.y - safe.yMin - margin - dotR;
            float topRoom = safe.yMax - c.y - margin - dotR;
            float sideRoom = tuning.Input.MirrorForLeftHand
                ? c.x - safe.xMin - margin - dotR
                : safe.xMax - c.x - margin - dotR;

            // Dodge sağ-alt dışarıda; yarıçap + dodgePad kenara sığmalı.
            float maxR = Mathf.Min(bottomRoom - dodgePad * HexagonLayoutScreenDefaults.DodgePadRadiusMult, topRoom, sideRoom - dodgePad);
            // Çizim koridoru için taban: komşu kenar boşluğu ≥ ~36dp (radius − 2·dotR).
            float floor = DpToPixels(Mathf.Max(HexagonLayoutScreenDefaults.MinHexRadiusFloorDp, tuning.Input.DotHitRadiusDp * 2f + HexagonLayoutScreenDefaults.DotHitRadiusFloorPaddingDp));
            if (maxR < floor)
                maxR = floor;
            return Mathf.Clamp(desired, floor, maxR);
        }

        public static float RadiusPx(GameTuning tuning) =>
            FittedRadiusPx(tuning, Screen.width, Screen.height);

        /// <summary>dot 1..6 — üstten başlayıp saat yönünde.</summary>
        public static Vector2 DotPx(int dot, GameTuning tuning, int screenWidth, int screenHeight)
        {
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = FittedRadiusPx(tuning, screenWidth, screenHeight);
            float startDeg = 90f;
            float step = tuning.Input.MirrorForLeftHand ? 60f : -60f;
            float deg = startDeg + (dot - 1) * step;
            float rad = deg * Mathf.Deg2Rad;
            return c + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
        }

        /// <summary>
        /// Dodge: hex'in sağ-alt köşesinde, rün halkasının dışında.
        /// Hava/Toprak ile arasında DodgeClearanceDp çizim/parmak boşluğu.
        /// </summary>
        public static Vector2 DodgeButtonPx(GameTuning tuning, int screenWidth, int screenHeight)
        {
            Rect safe = SafeRectPx();
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = FittedRadiusPx(tuning, screenWidth, screenHeight);
            float dodgeR = DodgeButtonRadiusPx(tuning);
            float gap = DpToPixels(Mathf.Max(HexagonLayoutScreenDefaults.MinHudGapDp, tuning.Input.DodgeClearanceDp));
            float side = tuning.Input.MirrorForLeftHand ? -1f : 1f;

            // Sağa + aşağı: halkaya yapışmaz, skill çizimini kesmez.
            Vector2 p = new Vector2(
                c.x + side * (r + dodgeR + gap),
                c.y - (r * HexagonLayoutScreenDefaults.HudVerticalOffsetMult));

            float dx = DpToPixels(tuning.Input.DodgeButtonOffsetXDp);
            float dy = DpToPixels(tuning.Input.DodgeButtonOffsetYDp);
            if (tuning.Input.MirrorForLeftHand)
                dx = -dx;
            p += new Vector2(dx, dy);

            float edge = dodgeR + DpToPixels(tuning.Input.DodgeButtonScreenMarginDp);
            float mid = screenWidth * 0.5f;

            float minX = Mathf.Max(safe.xMin + edge, tuning.Input.MirrorForLeftHand ? edge : mid + edge);
            float maxX = Mathf.Min(safe.xMax - edge, tuning.Input.MirrorForLeftHand ? mid - edge : screenWidth - edge);
            if (minX > maxX)
            {
                minX = safe.xMin + edge;
                maxX = safe.xMax - edge;
            }

            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, safe.yMin + edge, safe.yMax - edge);
            return p;
        }

        public static float DodgeButtonRadiusPx(GameTuning tuning) =>
            DpToPixels(tuning.Input.DodgeButtonRadiusDp);

        /// <summary>Lock-on: dodge'un üstünde, aynı sağ-alt küme içinde.</summary>
        public static Vector2 LockOnButtonPx(GameTuning tuning, int screenWidth, int screenHeight)
        {
            Vector2 dodge = DodgeButtonPx(tuning, screenWidth, screenHeight);
            float lockR = LockOnButtonRadiusPx(tuning);
            float dodgeR = DodgeButtonRadiusPx(tuning);
            float gap = DpToPixels(HexagonLayoutScreenDefaults.LockOnGapDp);
            Vector2 p = dodge + new Vector2(
                DpToPixels(tuning.Input.LockOnButtonOffsetXDp),
                dodgeR + lockR + gap + DpToPixels(tuning.Input.LockOnButtonOffsetYDp));

            ResolveHudButtonAwayFromHexPanel(
                ref p,
                lockR,
                gap,
                dodge,
                dodgeR,
                tuning,
                screenWidth,
                screenHeight);

            Rect safe = SafeRectPx();
            float edge = lockR + DpToPixels(tuning.Input.DodgeButtonScreenMarginDp);
            ClampHudButtonToSafe(ref p, safe, edge);
            ResolveHudButtonAwayFromHexPanel(
                ref p,
                lockR,
                gap,
                dodge,
                dodgeR,
                tuning,
                screenWidth,
                screenHeight);
            ClampHudButtonToSafe(ref p, safe, edge);
            EnforceHudButtonClearance(
                ref p,
                lockR,
                gap,
                dodge,
                dodgeR,
                tuning,
                screenWidth,
                screenHeight);
            return p;
        }

        public static float LockOnButtonRadiusPx(GameTuning tuning) =>
            DpToPixels(tuning.Input.LockOnButtonRadiusDp);

        /// <summary>
        /// Silah swap: dodge'un altıgene göre simetriği (sol-alt), çizim yarısında kalır.
        /// </summary>
        public static Vector2 WeaponSwapButtonPx(GameTuning tuning, int screenWidth, int screenHeight)
        {
            Rect safe = SafeRectPx();
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = FittedRadiusPx(tuning, screenWidth, screenHeight);
            float swapR = WeaponSwapButtonRadiusPx(tuning);
            float gap = DpToPixels(Mathf.Max(HexagonLayoutScreenDefaults.MinHudGapDp, tuning.Input.DodgeClearanceDp));
            float side = tuning.Input.MirrorForLeftHand ? 1f : -1f;

            Vector2 p = new Vector2(
                c.x + side * (r + swapR + gap),
                c.y - (r * HexagonLayoutScreenDefaults.HudVerticalOffsetMult));

            float dx = DpToPixels(tuning.Input.WeaponSwapButtonOffsetXDp);
            float dy = DpToPixels(tuning.Input.WeaponSwapButtonOffsetYDp);
            if (tuning.Input.MirrorForLeftHand)
                dx = -dx;
            p += new Vector2(dx, dy);

            float edge = swapR + DpToPixels(tuning.Input.DodgeButtonScreenMarginDp);
            float mid = screenWidth * 0.5f;
            float minX = tuning.Input.MirrorForLeftHand ? safe.xMin + edge : mid + edge;
            float maxX = tuning.Input.MirrorForLeftHand ? mid - edge : safe.xMax - edge;
            if (minX > maxX)
            {
                minX = safe.xMin + edge;
                maxX = safe.xMax - edge;
            }

            Vector2 dodge = DodgeButtonPx(tuning, screenWidth, screenHeight);
            float dodgeR = DodgeButtonRadiusPx(tuning);
            float panelGap = DpToPixels(HexagonLayoutScreenDefaults.LockOnGapDp);
            ResolveHudButtonAwayFromHexPanel(
                ref p,
                swapR,
                panelGap,
                dodge,
                dodgeR,
                tuning,
                screenWidth,
                screenHeight);
            ClampHudButtonToSafe(ref p, safe, edge, minX, maxX);
            ResolveHudButtonAwayFromHexPanel(
                ref p,
                swapR,
                panelGap,
                dodge,
                dodgeR,
                tuning,
                screenWidth,
                screenHeight);
            ClampHudButtonToSafe(ref p, safe, edge, minX, maxX);
            EnforceHudButtonClearance(
                ref p,
                swapR,
                panelGap,
                dodge,
                dodgeR,
                tuning,
                screenWidth,
                screenHeight);
            return p;
        }

        public static float WeaponSwapButtonRadiusPx(GameTuning tuning) =>
            DpToPixels(tuning.Input.WeaponSwapButtonRadiusDp);

        public static float DotHitRadiusPx(GameTuning tuning) => DpToPixels(tuning.Input.DotHitRadiusDp);

        public static float CenterHitRadiusPx(GameTuning tuning) => DpToPixels(tuning.Input.CenterHitRadiusDp);

        /// <summary>
        /// Kısa kenar bu dp'den azsa HUD orantılı küçülür (0 = kapalı). <see cref="GameTuning.HudFitShortSideDp"/>
        /// ile beslenir; yatay telefonda kısa kenar ~430 dp, altıgen tepsisi ise ~360 dp.
        /// </summary>
        public static float FitShortSideDp { get; set; }

        /// <summary>Editörde telefon DPI'ını taklit etmek için (0 = gerçek Screen.dpi).</summary>
        public static float DebugDpiOverride { get; set; }

        public static float PixelsPerDp
        {
            get
            {
                float dpi = DebugDpiOverride > 0f ? DebugDpiOverride : Screen.dpi > 0f ? Screen.dpi : HexagonLayoutScreenDefaults.BaselineDpi;
                float perDp = dpi / HexagonLayoutScreenDefaults.BaselineDpi;
                if (FitShortSideDp > 1f)
                    perDp = Mathf.Min(perDp, Mathf.Min(Screen.width, Screen.height) / FitShortSideDp);
                return perDp;
            }
        }

        public static float DpToPixels(float dp) => dp * PixelsPerDp;

        /// <summary>HUD sığdırmasından bağımsız fiziksel dp (sanal çubuk: parmak mesafesi sabit kalmalı).</summary>
        public static float PhysicalDpToPixels(float dp)
        {
            float dpi = DebugDpiOverride > 0f ? DebugDpiOverride : Screen.dpi > 0f ? Screen.dpi : HexagonLayoutScreenDefaults.BaselineDpi;
            return dp * (dpi / HexagonLayoutScreenDefaults.BaselineDpi);
        }

        public static bool IsRightHalf(Vector2 screenPos, bool mirrorForLeftHand, int screenWidth)
        {
            float mid = screenWidth * 0.5f;
            return mirrorForLeftHand ? screenPos.x < mid : screenPos.x >= mid;
        }

        /// <summary>Safe üst inset — HUD margin ile birlikte.</summary>
        public static float SafeTopInsetPx()
        {
            Rect safe = SafeRectPx();
            return Mathf.Max(0f, Screen.height - safe.yMax);
        }

        public static float SafeLeftInsetPx()
        {
            Rect safe = SafeRectPx();
            return Mathf.Max(0f, safe.xMin);
        }

        static void ClampHudButtonToSafe(ref Vector2 p, Rect safe, float edge, float minX = float.NaN, float maxX = float.NaN)
        {
            if (float.IsNaN(minX))
                minX = safe.xMin + edge;
            if (float.IsNaN(maxX))
                maxX = safe.xMax - edge;
            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, safe.yMin + edge, safe.yMax - edge);
        }

        static Circle2[] BuildHexHudForbidden(
            GameTuning tuning,
            int screenWidth,
            int screenHeight,
            Vector2 dodgeCenter,
            float dodgeRadius)
        {
            Vector2 panelCenter = CenterPx(tuning, screenWidth, screenHeight);
            float fitted = FittedRadiusPx(tuning, screenWidth, screenHeight);
            float dotR = DotHitRadiusPx(tuning);
            var forbidden = new Circle2[8];
            forbidden[0] = new Circle2(panelCenter.x, panelCenter.y, fitted + dotR);
            for (int dot = 1; dot <= 6; dot++)
            {
                Vector2 dotPx = DotPx(dot, tuning, screenWidth, screenHeight);
                forbidden[dot] = new Circle2(dotPx.x, dotPx.y, dotR);
            }

            forbidden[7] = new Circle2(dodgeCenter.x, dodgeCenter.y, dodgeRadius);
            return forbidden;
        }

        static void ResolveHudButtonAwayFromHexPanel(
            ref Vector2 p,
            float buttonRadius,
            float gap,
            Vector2 dodgeCenter,
            float dodgeRadius,
            GameTuning tuning,
            int screenWidth,
            int screenHeight)
        {
            Circle2[] forbidden = BuildHexHudForbidden(tuning, screenWidth, screenHeight, dodgeCenter, dodgeRadius);
            var dodgeOrbit = forbidden[7];
            float x = p.x;
            float y = p.y;
            HudButtonPlacement.ResolveAwayFromForbidden(
                ref x,
                ref y,
                buttonRadius,
                forbidden,
                gap,
                dodgeOrbit,
                forbidden[0].X,
                forbidden[0].Y);
            p = new Vector2(x, y);
        }

        static void EnforceHudButtonClearance(
            ref Vector2 p,
            float buttonRadius,
            float gap,
            Vector2 dodgeCenter,
            float dodgeRadius,
            GameTuning tuning,
            int screenWidth,
            int screenHeight)
        {
            Circle2[] forbidden = BuildHexHudForbidden(tuning, screenWidth, screenHeight, dodgeCenter, dodgeRadius);
            float x = p.x;
            float y = p.y;
            HudButtonPlacement.EnforceClearance(ref x, ref y, buttonRadius, forbidden, gap);
            p = new Vector2(x, y);
        }
    }
}
