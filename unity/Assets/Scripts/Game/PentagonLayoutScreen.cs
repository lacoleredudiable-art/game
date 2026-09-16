using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Altıgen noktalarının ekran pikseli konumları. Dünyaya bağlı değil (§2).
    /// Nokta sırası 1→6 saat yönünde; komşu = ±1 (Core PentagonLayout ile uyumlu).
    /// </summary>
    public static class PentagonLayoutScreen
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
                ? 1f - tuning.PentagonCenterXNorm
                : tuning.PentagonCenterXNorm;
            // Norm, safe rect içinde yorumlanır (taşma / home bar).
            float x = safe.xMin + xNorm * safe.width;
            float y = safe.yMin + tuning.PentagonCenterYNorm * safe.height;
            return new Vector2(x, y);
        }

        public static float RadiusPx(PrototypeTuning tuning) => DpToPixels(tuning.PentagonRadiusDp);

        /// <summary>dot 1..6 — üstten başlayıp saat yönünde.</summary>
        public static Vector2 DotPx(int dot, PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = RadiusPx(tuning);
            float startDeg = 90f;
            float step = tuning.MirrorForLeftHand ? 60f : -60f;
            float deg = startDeg + (dot - 1) * step;
            float rad = deg * Mathf.Deg2Rad;
            return c + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
        }

        /// <summary>
        /// Dodge düğmesi: altıgenin dışında, safe + çizim yarısı içine kırpılır.
        /// </summary>
        public static Vector2 DodgeButtonPx(PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Rect safe = SafeRectPx();
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float dx = DpToPixels(tuning.DodgeButtonOffsetXDp);
            if (tuning.MirrorForLeftHand)
                dx = -dx;

            Vector2 p = c + new Vector2(dx, DpToPixels(tuning.DodgeButtonOffsetYDp));
            float edge = DodgeButtonRadiusPx(tuning) + DpToPixels(tuning.DodgeButtonScreenMarginDp);
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

        public static float DotHitRadiusPx(PrototypeTuning tuning) => DpToPixels(tuning.DotHitRadiusDp);

        public static float CenterHitRadiusPx(PrototypeTuning tuning) => DpToPixels(tuning.CenterHitRadiusDp);

        public static float DpToPixels(float dp)
        {
            float dpi = Screen.dpi > 0f ? Screen.dpi : 160f;
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
