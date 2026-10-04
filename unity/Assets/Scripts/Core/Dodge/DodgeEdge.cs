using System;

namespace Dovus.Core.Dodge
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

        /// <summary>
        /// Kayma, başlangıçtan istenen noktaya giden doğru gövdeye değerse yakın yüzde durur.
        /// Daireyi aşıp uzak yüzde biten bir örnek oyuncuyu arkaya ışınlamaz.
        /// </summary>
        public static void StopBeforeCrossing(
            ref float x, ref float z,
            float fromX, float fromZ,
            float bossX, float bossZ,
            float minSeparation)
        {
            if (minSeparation <= 0f)
                return;

            float abx = x - fromX;
            float abz = z - fromZ;
            float a = abx * abx + abz * abz;
            float fx = fromX - bossX;
            float fz = fromZ - bossZ;
            float c = fx * fx + fz * fz - minSeparation * minSeparation;

            if (a < 1e-8f)
            {
                if (c < 0f)
                    KeepOutside(ref x, ref z, bossX, bossZ, minSeparation, 0f, 0f);
                return;
            }

            if (c < 0f)
            {
                float side = MathF.Sqrt(fx * fx + fz * fz);
                if (side < 0.0001f)
                {
                    KeepOutside(ref x, ref z, bossX, bossZ, minSeparation, abx, abz);
                    return;
                }

                float inv = minSeparation / side;
                x = bossX + fx * inv;
                z = bossZ + fz * inv;
                return;
            }

            float b = 2f * (fx * abx + fz * abz);
            float disc = b * b - 4f * a * c;
            if (disc < 0f)
                return;

            float tEnter = (-b - MathF.Sqrt(disc)) / (2f * a);
            if (tEnter > 0f && tEnter < 1f)
            {
                x = fromX + abx * tEnter;
                z = fromZ + abz * tEnter;
            }
        }
    }
}
