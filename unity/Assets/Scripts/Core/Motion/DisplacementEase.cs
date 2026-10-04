using System;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Tek karede ışınlanan çekmeyi 0,4 sn'lik smoothstep'e yayar.
    /// Süre his varsayılanıdır (0,3–0,5 sn); JSON'da çekme süresi yok.
    /// </summary>
    public static class DisplacementEase
    {
        public const float DurationSec = 0.4f;

        public static float Smooth(float u)
        {
            u = Math.Clamp(u, 0f, 1f);
            return u * u * (MotionDefaults.Lit3f - 2f * u);
        }

        public static void Sample(
            float fromX, float fromZ,
            float toX, float toZ,
            float u,
            out float x, out float z)
        {
            float s = Smooth(u);
            x = fromX + (toX - fromX) * s;
            z = fromZ + (toZ - fromZ) * s;
        }

        /// <summary>Gövde merkezleri minSeparation'dan yakınsa dışarı iter. İç içe geçmez.</summary>
        public static void KeepSeparated(
            ref float x, ref float z,
            float stopX, float stopZ,
            float minSeparation)
        {
            if (minSeparation <= 0f)
                return;
            float dx = x - stopX;
            float dz = z - stopZ;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist >= minSeparation)
                return;
            if (dist < 0.0001f)
            {
                x = stopX + minSeparation;
                z = stopZ;
                return;
            }
            float scale = minSeparation / dist;
            x = stopX + dx * scale;
            z = stopZ + dz * scale;
        }
    }
}
