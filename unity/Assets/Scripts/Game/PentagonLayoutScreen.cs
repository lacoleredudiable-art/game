using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Beşgen noktalarının ekran pikseli konumları. Dünyaya bağlı değil (§2).
    /// Nokta sırası 1→5 saat yönünde; komşu = ±1 (Core PentagonLayout ile uyumlu).
    /// </summary>
    public static class PentagonLayoutScreen
    {
        public static Vector2 CenterPx(PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            float xNorm = tuning.MirrorForLeftHand
                ? 1f - tuning.PentagonCenterXNorm
                : tuning.PentagonCenterXNorm;
            return new Vector2(xNorm * screenWidth, tuning.PentagonCenterYNorm * screenHeight);
        }

        public static float RadiusPx(PrototypeTuning tuning) => DpToPixels(tuning.PentagonRadiusDp);

        /// <summary>dot 1..5 — üstten başlayıp saat yönünde.</summary>
        public static Vector2 DotPx(int dot, PrototypeTuning tuning, int screenWidth, int screenHeight)
        {
            Vector2 c = CenterPx(tuning, screenWidth, screenHeight);
            float r = RadiusPx(tuning);
            // Üst = 1, sonra 2,3,4,5. Aynalamada açı yönü tersine çevrilir ki komşuluk bozulmasın.
            float startDeg = 90f;
            float step = tuning.MirrorForLeftHand ? 72f : -72f;
            float deg = startDeg + (dot - 1) * step;
            float rad = deg * Mathf.Deg2Rad;
            return c + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * r;
        }

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
    }
}
