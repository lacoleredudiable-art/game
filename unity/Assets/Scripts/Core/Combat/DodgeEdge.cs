using System;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Dodge bitişi boss gövdesinin içinde kalamaz. Kalıp kenar payı ile aynı kural:
    /// merkezler minSeparation'dan yakınsa dış yüzeye itilir.
    /// </summary>
    public static class DodgeEdge
    {
        public static void KeepOutside(
            ref float x, ref float z,
            float bossX, float bossZ,
            float minSeparation,
            float travelX, float travelZ)
        {
            if (minSeparation <= 0f)
                return;

            float dx = x - bossX;
            float dz = z - bossZ;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist >= minSeparation)
                return;

            if (dist < 0.0001f)
            {
                float travelMag = MathF.Sqrt(travelX * travelX + travelZ * travelZ);
                if (travelMag < 0.0001f)
                {
                    x = bossX + minSeparation;
                    z = bossZ;
                    return;
                }

                float inv = minSeparation / travelMag;
                x = bossX - travelX * inv;
                z = bossZ - travelZ * inv;
                return;
            }

            float scale = minSeparation / dist;
            x = bossX + dx * scale;
            z = bossZ + dz * scale;
        }
    }
}
