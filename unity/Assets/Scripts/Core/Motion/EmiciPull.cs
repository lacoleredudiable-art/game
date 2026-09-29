using System;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Emici çekme her zaman boss'u oyuncuya yaklaştırır. Varış, oyuncunun önünde
    /// kenarların temas ettiği noktadır. Girdap merkezi bu yönü değiştiremez.
    /// </summary>
    public static class EmiciPull
    {
        public static void ContactPoint(
            float playerX, float playerZ,
            float bossX, float bossZ,
            float playerRadius,
            float bossRadius,
            float stableDirX, float stableDirZ,
            out float x, out float z)
        {
            float contact = Math.Max(0f, playerRadius) + Math.Max(0f, bossRadius);
            float dx = bossX - playerX;
            float dz = bossZ - playerZ;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            float ux;
            float uz;
            if (dist > 0.08f)
            {
                ux = dx / dist;
                uz = dz / dist;
            }
            else
            {
                float len = MathF.Sqrt(stableDirX * stableDirX + stableDirZ * stableDirZ);
                if (len < 0.001f)
                {
                    ux = 0f;
                    uz = 1f;
                }
                else
                {
                    ux = stableDirX / len;
                    uz = stableDirZ / len;
                }
            }

            x = playerX + ux * contact;
            z = playerZ + uz * contact;
        }

        /// <summary>Varış, boss'un şu ankinden oyuncuya daha yakındır. Uzağa itmez.</summary>
        public static bool MovesTowardPlayer(
            float bossX, float bossZ,
            float playerX, float playerZ,
            float destX, float destZ)
        {
            float now = Distance(bossX, bossZ, playerX, playerZ);
            float then = Distance(destX, destZ, playerX, playerZ);
            return then <= now + 0.02f;
        }

        public static float Distance(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }
}
