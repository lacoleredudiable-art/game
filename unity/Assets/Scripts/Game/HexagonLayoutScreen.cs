using UnityEngine;

namespace Dovus.Game
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

        public static Vector2 CenterPx(PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Rect safe = SafeRectPx();
            float xNorm = tuning.MirrorForLeftHand
                ? 1f - tuning.HexagonCenterXNorm
                : tuning.HexagonCenterXNorm;
            // Norm, safe rect içinde yorumlanır (taşma / home bar).
            float x = safe.xMin + xNorm * safe.width;
            float y = safe.yMin + tuning.HexagonCenterYNorm * safe.height;
            return new Vector2(x, y);
        }

        /// <summary>
        /// İstenen yarıçapı safe-area + dodge boşluğuna sığacak şekilde kısar.
        /// Büyük dp / yüksek DPI telefonda alt rünlerin kesilmesini engeller.
        /// </summary>
        public static float FittedRadiusPx(PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            float desired = DpToPixels(tuning.HexagonRadiusDp);
            Rect safe = SafeRectPx();
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float dotR = DotHitRadiusPx(tuning);
            float dodgeR = DodgeButtonRadiusPx(tuning);
            float margin = DpToPixels(10f);
            float dodgePad = dodgeR + DpToPixels(Mathf.Max(16f, tuning.DodgeClearanceDp));

            float bottomRoom = c.y - safe.yMin - margin - dotR;
            float topRoom = safe.yMax - c.y - margin - dotR;
            float sideRoom = tuning.MirrorForLeftHand
                ? c.x - safe.xMin - margin - dotR
                : safe.xMax - c.x - margin - dotR;

            // Dodge sağ-alt dışarıda; yarıçap + dodgePad kenara sığmalı.
            float maxR = Mathf.Min(bottomRoom - dodgePad * 0.45f, topRoom, sideRoom - dodgePad);
            // Çizim koridoru için taban: komşu kenar boşluğu ≥ ~36dp (radius − 2·dotR).
            float floor = DpToPixels(Mathf.Max(72f, tuning.DotHitRadiusDp * 2f + 36f));
            if (maxR < floor)
                maxR = floor;
            return Mathf.Clamp(desired, floor, maxR);
        }

        public static float RadiusPx(PrototypeTuning tuning) =>
            FittedRadiusPx(tuning, Screen.width, Screen.height);

        /// <summary>dot 1..6 — üstten başlayıp saat yönünde.</summary>
        public static Vector2 DotPx(int dot, PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = FittedRadiusPx(tuning, screenWidth, screenHeight);
            float startDeg = 90f;
            float step = tuning.MirrorForLeftHand ? 60f : -60f;
            float deg = startDeg + (dot - 1) * step;
            float rad = deg * Mathf.Deg2Rad;
            return c + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
        }

        /// <summary>
        /// Dodge: hex'in sağ-alt köşesinde, rün halkasının dışında.
        /// Hava/Toprak ile arasında DodgeClearanceDp çizim/parmak boşluğu.
        /// </summary>
        public static Vector2 DodgeButtonPx(PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Rect safe = SafeRectPx();
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = FittedRadiusPx(tuning, screenWidth, screenHeight);
            float dodgeR = DodgeButtonRadiusPx(tuning);
            float gap = DpToPixels(Mathf.Max(24f, tuning.DodgeClearanceDp));
            float side = tuning.MirrorForLeftHand ? -1f : 1f;

            // Sağa + aşağı: halkaya yapışmaz, skill çizimini kesmez.
            Vector2 p = new Vector2(
                c.x + side * (r + dodgeR + gap),
                c.y - (r * 0.55f));

            float dx = DpToPixels(tuning.DodgeButtonOffsetXDp);
            float dy = DpToPixels(tuning.DodgeButtonOffsetYDp);
            if (tuning.MirrorForLeftHand)
                dx = -dx;
            p += new Vector2(dx, dy);

            float edge = dodgeR + DpToPixels(tuning.DodgeButtonScreenMarginDp);
            float mid = screenWidth * 0.5f;

            float minX = Mathf.Max(safe.xMin + edge, tuning.MirrorForLeftHand ? edge : mid + edge);
            float maxX = Mathf.Min(safe.xMax - edge, tuning.MirrorForLeftHand ? mid - edge : screenWidth - edge);
            if (minX > maxX)
            {
                minX = safe.xMin + edge;
                maxX = safe.xMax - edge;
            }

            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, safe.yMin + edge, safe.yMax - edge);
            return p;
        }

        public static float DodgeButtonRadiusPx(PrototypeTuning tuning) =>
            DpToPixels(tuning.DodgeButtonRadiusDp);

        /// <summary>Lock-on: dodge'un üstünde, aynı sağ-alt küme içinde.</summary>
        public static Vector2 LockOnButtonPx(PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Vector2 dodge = DodgeButtonPx(tuning, screenWidth, screenHeight);
            float lockR = LockOnButtonRadiusPx(tuning);
            float dodgeR = DodgeButtonRadiusPx(tuning);
            float gap = DpToPixels(12f);
            Vector2 p = dodge + new Vector2(
                DpToPixels(tuning.LockOnButtonOffsetXDp),
                dodgeR + lockR + gap + DpToPixels(tuning.LockOnButtonOffsetYDp));
            Rect safe = SafeRectPx();
            float edge = lockR + DpToPixels(tuning.DodgeButtonScreenMarginDp);
            p.x = Mathf.Clamp(p.x, safe.xMin + edge, safe.xMax - edge);
            p.y = Mathf.Clamp(p.y, safe.yMin + edge, safe.yMax - edge);
            return p;
        }

        public static float LockOnButtonRadiusPx(PrototypeTuning tuning) =>
            DpToPixels(tuning.LockOnButtonRadiusDp);

        /// <summary>
        /// Silah swap: dodge'un altıgene göre simetriği (sol-alt), çizim yarısında kalır.
        /// </summary>
        public static Vector2 WeaponSwapButtonPx(PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Rect safe = SafeRectPx();
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = FittedRadiusPx(tuning, screenWidth, screenHeight);
            float swapR = WeaponSwapButtonRadiusPx(tuning);
            float gap = DpToPixels(Mathf.Max(24f, tuning.DodgeClearanceDp));
            float side = tuning.MirrorForLeftHand ? 1f : -1f;

            Vector2 p = new Vector2(
                c.x + side * (r + swapR + gap),
                c.y - (r * 0.55f));

            float dx = DpToPixels(tuning.WeaponSwapButtonOffsetXDp);
            float dy = DpToPixels(tuning.WeaponSwapButtonOffsetYDp);
            if (tuning.MirrorForLeftHand)
                dx = -dx;
            p += new Vector2(dx, dy);

            float edge = swapR + DpToPixels(tuning.DodgeButtonScreenMarginDp);
            float mid = screenWidth * 0.5f;
            float minX = tuning.MirrorForLeftHand ? safe.xMin + edge : mid + edge;
            float maxX = tuning.MirrorForLeftHand ? mid - edge : safe.xMax - edge;
            if (minX > maxX)
            {
                minX = safe.xMin + edge;
                maxX = safe.xMax - edge;
            }

            p.x = Mathf.Clamp(p.x, minX, maxX);
            p.y = Mathf.Clamp(p.y, safe.yMin + edge, safe.yMax - edge);
            return p;
        }

        public static float WeaponSwapButtonRadiusPx(PrototypeTuning tuning) =>
            DpToPixels(tuning.WeaponSwapButtonRadiusDp);

        public static float DotHitRadiusPx(PrototypeTuning tuning) => DpToPixels(tuning.DotHitRadiusDp);

        public static float CenterHitRadiusPx(PrototypeTuning tuning) => DpToPixels(tuning.CenterHitRadiusDp);

        /// <summary>
        /// Kısa kenar bu dp'den azsa HUD orantılı küçülür (0 = kapalı). <see cref="PrototypeTuning.HudFitShortSideDp"/>
        /// ile beslenir; yatay telefonda kısa kenar ~430 dp, altıgen tepsisi ise ~360 dp.
        /// </summary>
        public static float FitShortSideDp { get; set; }

        /// <summary>Editörde telefon DPI'ını taklit etmek için (0 = gerçek Screen.dpi).</summary>
        public static float DebugDpiOverride { get; set; }

        public static float PixelsPerDp
        {
            get
            {
                float dpi = DebugDpiOverride > 0f ? DebugDpiOverride : Screen.dpi > 0f ? Screen.dpi : 160f;
                float perDp = dpi / 160f;
                if (FitShortSideDp > 1f)
                    perDp = Mathf.Min(perDp, Mathf.Min(Screen.width, Screen.height) / FitShortSideDp);
                return perDp;
            }
        }

        public static float DpToPixels(float dp) => dp * PixelsPerDp;

        /// <summary>HUD sığdırmasından bağımsız fiziksel dp (sanal çubuk: parmak mesafesi sabit kalmalı).</summary>
        public static float PhysicalDpToPixels(float dp)
        {
            float dpi = DebugDpiOverride > 0f ? DebugDpiOverride : Screen.dpi > 0f ? Screen.dpi : 160f;
            return dp * (dpi / 160f);
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
    }
}
