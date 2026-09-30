using System;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Top düz atışı ışının ucunda değil, ışın üzerindeki ilk gövdede patlar.
    /// Mesafe silah menziliyle sınırlıdır.
    /// </summary>
    public static class CannonShot
    {
        public static bool TryImpact(
            float originX,
            float originZ,
            float dirX,
            float dirZ,
            float reachM,
            float targetX,
            float targetZ,
            float targetRadiusM,
            out float hitX,
            out float hitZ,
            out float hitDistM)
        {
            hitX = originX;
            hitZ = originZ;
            hitDistM = 0f;
            float reach = Math.Max(0f, reachM);
            float mag = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
            if (mag < 0.0001f || reach <= 0f)
                return false;
            dirX /= mag;
            dirZ /= mag;

            float ox = targetX - originX;
            float oz = targetZ - originZ;
            float along = ox * dirX + oz * dirZ;
            float radius = Math.Max(0f, targetRadiusM);
            float perp2 = ox * ox + oz * oz - along * along;
            float rad2 = radius * radius;
            if (perp2 > rad2)
                return false;
            float half = MathF.Sqrt(Math.Max(0f, rad2 - perp2));
            float t = along - half;
            if (t < 0f)
                t = along + half;
            if (t < 0f || t > reach)
                return false;
            hitDistM = t;
            hitX = originX + dirX * t;
            hitZ = originZ + dirZ * t;
            return true;
        }
    }
}
