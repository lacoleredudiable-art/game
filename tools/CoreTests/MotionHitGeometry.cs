using System;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Yalnız testlerin kullandığı kapsül/mesafe geometrisi. Runtime'da çağıranı yoktu;
    /// CLEANUP-2 ile oyun kodundan test projesine taşındı.
    /// </summary>
    public static class MotionHitGeometry
    {
        public static float EdgeGap(
            float attackerX, float attackerZ, float attackerRadius,
            float targetX, float targetZ, float targetRadius)
        {
            float dx = attackerX - targetX;
            float dz = attackerZ - targetZ;
            float center = MathF.Sqrt(dx * dx + dz * dz);
            return center - Math.Max(0f, attackerRadius) - Math.Max(0f, targetRadius);
        }

        public static bool Overlaps(
            float originX, float originZ,
            float dirX, float dirZ,
            float lengthM, float radiusM,
            string anchor,
            float targetX, float targetZ, float targetRadius)
        {
            float allow = Math.Max(0.05f, radiusM) + Math.Max(0f, targetRadius);
            if (anchor is "forward" or "shot")
            {
                float len = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
                if (len < 0.0001f)
                {
                    dirX = 0f;
                    dirZ = 1f;
                    len = 1f;
                }
                float ex = originX + dirX / len * Math.Max(lengthM, 0.2f);
                float ez = originZ + dirZ / len * Math.Max(lengthM, 0.2f);
                return DistancePointSegment(targetX, targetZ, originX, originZ, ex, ez) <= allow;
            }

            float dx = targetX - originX;
            float dz = targetZ - originZ;
            return MathF.Sqrt(dx * dx + dz * dz) <= allow;
        }

        static float DistancePointSegment(
            float px, float pz, float ax, float az, float bx, float bz)
        {
            float abx = bx - ax;
            float abz = bz - az;
            float len2 = abx * abx + abz * abz;
            if (len2 < 0.0001f)
            {
                float dx = px - ax;
                float dz = pz - az;
                return MathF.Sqrt(dx * dx + dz * dz);
            }
            float t = ((px - ax) * abx + (pz - az) * abz) / len2;
            t = Math.Clamp(t, 0f, 1f);
            float qx = ax + abx * t;
            float qz = az + abz * t;
            float ox = px - qx;
            float oz = pz - qz;
            return MathF.Sqrt(ox * ox + oz * oz);
        }
    }
}
