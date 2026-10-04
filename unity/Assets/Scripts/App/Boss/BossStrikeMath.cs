using System;

namespace Dovus.App.Boss
{
    /// <summary>
    /// Düzlem (y=0) vuruş geometrisi — Unity Vector3.SignedAngle / magnitude ile uyumlu.
    /// </summary>
    public static class BossStrikeMath
    {
        const float Rad2Deg = 360f / ((float)Math.PI * 2f);
        const float AngleEpsilon = 1E-15f;

        public static float HorizontalDistance(float dx, float dz) =>
            (float)Math.Sqrt(dx * dx + dz * dz);

        /// <summary>Vector3.SignedAngle(from, to, Vector3.up) — from/to y=0, tam vektörler (normalize edilmez).</summary>
        public static float FlatSignedAngleDeg(float fromX, float fromZ, float toX, float toZ)
        {
            float denominator = (float)Math.Sqrt((fromX * fromX + fromZ * fromZ) * (toX * toX + toZ * toZ));
            if (denominator < AngleEpsilon)
                return 0f;
            // Unity: Cross(from, to).y = from.z * to.x - from.x * to.z; Mathf.Sign(0) = +1.
            float crossY = fromZ * toX - fromX * toZ;
            float dot = fromX * toX + fromZ * toZ;
            float num2 = dot / denominator;
            if (num2 < -1f)
                num2 = -1f;
            else if (num2 > 1f)
                num2 = 1f;
            float unsigned = (float)Math.Acos(num2) * Rad2Deg;
            return unsigned * (crossY >= 0f ? 1f : -1f);
        }
    }
}
