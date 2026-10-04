using System;

namespace Dovus.App.Casting
{
    public static class FlatFacingMath
    {
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

            float inv = 1f / MathF.Sqrt(outX * outX + outZ * outZ);
            outX *= inv;
            outZ *= inv;
        }

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
            if (dirX * dirX + dirZ * dirZ < 0.0001f)
            {
                dirX = 0f;
                dirZ = 0f;
                return false;
            }

            float inv = 1f / MathF.Sqrt(dirX * dirX + dirZ * dirZ);
            dirX *= inv;
            dirZ *= inv;
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

        public static float FlatAngleDeg(float ax, float az, float bx, float bz)
        {
            float magA = MathF.Sqrt(ax * ax + az * az);
            float magB = MathF.Sqrt(bx * bx + bz * bz);
            if (magA < 0.0001f || magB < 0.0001f)
                return 0f;
            float dot = (ax * bx + az * bz) / (magA * magB);
            if (dot > 1f)
                dot = 1f;
            else if (dot < -1f)
                dot = -1f;
            return MathF.Acos(dot) * (180f / MathF.PI);
        }
    }
}
