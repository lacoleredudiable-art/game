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
            if (dist > EmiciPullDefaults.MinBossSeparationM)
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
            return then <= now + EmiciPullDefaults.PullDistEpsilonM;
        }

        public static float Distance(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>
        /// Girdap yalnız içindeki gövdeyi çeker (adjective 2 govde). Dışarıdaki hareketle yarışmaz.
        /// </summary>
        public static bool VortexActs(bool vortexProfile, bool bossInside) =>
            vortexProfile && bossInside;

        /// <summary>
        /// Temas noktası oyuncunun o anki yerinden hesaplanır. Çekme sürerken hız sıfırlanmaz;
        /// yoksa her kare ease başa sarar ve boss yerinde kalır.
        /// </summary>
        public static void Retarget(
            bool alreadyPulling,
            float homeX, float homeZ,
            float playerX, float playerZ,
            float playerRadius, float bossRadius,
            ref float stableX, ref float stableZ,
            ref float speed,
            out float toX, out float toZ,
            out bool pulling)
        {
            float dx = homeX - playerX;
            float dz = homeZ - playerZ;
            if (dx * dx + dz * dz > EmiciPullDefaults.HomeOffsetDistSqrMin)
            {
                stableX = dx;
                stableZ = dz;
            }
            ContactPoint(
                playerX, playerZ, homeX, homeZ,
                playerRadius, bossRadius,
                stableX, stableZ,
                out toX, out toZ);
            float dist = Distance(homeX, homeZ, toX, toZ);
            if (!alreadyPulling)
                speed = dist / DisplacementEase.DurationSec;
            pulling = dist > EmiciPullDefaults.PullDistEpsilonM;
        }

        /// <summary>Varışa doğru sabit hız. Hedefi geçmez; içerdeyse temas noktasına kadar dışarı yürür, ışınlanmaz.</summary>
        public static void StepToward(
            ref float homeX, ref float homeZ,
            float toX, float toZ,
            float speed, float dt,
            out bool arrived)
        {
            float dx = toX - homeX;
            float dz = toZ - homeZ;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            float step = MathF.Max(0f, speed) * MathF.Max(0f, dt);
            if (dist <= EmiciPullDefaults.PullDistEpsilonM || dist <= step)
            {
                homeX = toX;
                homeZ = toZ;
                arrived = true;
                return;
            }
            homeX += dx / dist * step;
            homeZ += dz / dist * step;
            arrived = false;
        }
    }
}
