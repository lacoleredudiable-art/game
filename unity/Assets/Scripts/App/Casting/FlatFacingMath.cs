using System;

namespace Dovus.App.Casting
{
    /// <summary>
    /// Düzlem (y=0) yön matematiği — UnityEngine.Vector3 ile BİREBİR aynı sonuç verecek şekilde yazıldı:
    /// normalize = bileşen / büyüklük (Vector3.Normalize, kEpsilon 1e-5), açı = Vector3.Angle formülü.
    /// </summary>
    public static class FlatFacingMath
    {
        const float NormalizeEpsilon = 1E-05f;          // Vector3.kEpsilon
        const float AngleEpsilon = 1E-15f;              // Vector3.kEpsilonNormalSqrt
        const float Rad2Deg = 360f / ((float)Math.PI * 2f); // Mathf.Rad2Deg

        /// <summary>Vector3.normalized (y=0): büyüklük kEpsilon'dan büyükse bileşen/büyüklük, değilse sıfır.</summary>
        public static void Normalize(ref float x, ref float z)
        {
            float mag = (float)Math.Sqrt(x * x + z * z);
            if (mag > NormalizeEpsilon)
            {
                x = x / mag;
                z = z / mag;
            }
            else
            {
                x = 0f;
                z = 0f;
            }
        }

        /// <summary>Bakış (y sıfırlanmış): çok kısaysa dünya ileri (0,1), değilse normalize.</summary>
        public static void FlatBodyForward(float forwardX, float forwardZ, out float outX, out float outZ)
        {
            outX = forwardX;
            outZ = forwardZ;
            if (outX * outX + outZ * outZ < 0.0001f)
            {
                outX = 0f;
                outZ = 1f;
                return;
            }

            Normalize(ref outX, ref outZ);
        }

        /// <summary>Hedefe düzlem yön; uzaklık karesi 0.0001'den büyük değilse false.</summary>
        public static bool TryFlatToTarget(
            float posX,
            float posZ,
            float targetX,
            float targetZ,
            out float dirX,
            out float dirZ)
        {
            dirX = targetX - posX;
            dirZ = targetZ - posZ;
            if (!(dirX * dirX + dirZ * dirZ > 0.0001f))
            {
                dirX = 0f;
                dirZ = 0f;
                return false;
            }

            Normalize(ref dirX, ref dirZ);
            return true;
        }

        public static void FacingOrBody(
            bool hasTarget,
            float targetX,
            float targetZ,
            float posX,
            float posZ,
            float bodyForwardX,
            float bodyForwardZ,
            out float facingX,
            out float facingZ)
        {
            if (hasTarget
                && TryFlatToTarget(posX, posZ, targetX, targetZ, out facingX, out facingZ))
                return;

            FlatBodyForward(bodyForwardX, bodyForwardZ, out facingX, out facingZ);
        }

        /// <summary>Vector3.Angle (y=0) ile aynı formül.</summary>
        public static float FlatAngleDeg(float ax, float az, float bx, float bz)
        {
            float denominator = (float)Math.Sqrt((ax * ax + az * az) * (bx * bx + bz * bz));
            if (denominator < AngleEpsilon)
                return 0f;
            float dot = (ax * bx + az * bz) / denominator;
            if (dot < -1f)
                dot = -1f;
            else if (dot > 1f)
                dot = 1f;
            return (float)Math.Acos(dot) * Rad2Deg;
        }
    }
}
