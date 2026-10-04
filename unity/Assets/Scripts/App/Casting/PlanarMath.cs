using System;

namespace Dovus.App.Casting
{
    public static class PlanarMath
    {
        public static float FlatDistance(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return MathF.Sqrt(dx * dx + dz * dz);
        }
    }
}
