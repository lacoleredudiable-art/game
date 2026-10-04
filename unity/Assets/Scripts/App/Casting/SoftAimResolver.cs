namespace Dovus.App.Casting
{
    public static class SoftAimResolver
    {
        public static void Resolve(
            float playerForwardX,
            float playerForwardZ,
            float velocityX,
            float velocityZ,
            float velocitySqrThreshold,
            bool hasBoss,
            float bossX,
            float bossZ,
            float posX,
            float posZ,
            float softAimRangeM,
            float softAimConeDeg,
            out float facingX,
            out float facingZ)
        {
            facingX = playerForwardX;
            facingZ = playerForwardZ;
            if (velocityX * velocityX + velocityZ * velocityZ > velocitySqrThreshold)
            {
                facingX = velocityX;
                facingZ = velocityZ;
            }

            FlatFacingMath.FlatBodyForward(facingX, facingZ, out facingX, out facingZ);

            if (!hasBoss || softAimRangeM <= 0.1f)
                return;

            float toBossX = bossX - posX;
            float toBossZ = bossZ - posZ;
            float dist = PlanarMath.FlatDistance(posX, posZ, bossX, bossZ);
            if (dist <= 0.01f || dist > softAimRangeM)
                return;

            float angle = FlatFacingMath.FlatAngleDeg(facingX, facingZ, toBossX, toBossZ);
            if (angle > softAimConeDeg)
                return;

            facingX = toBossX / dist;
            facingZ = toBossZ / dist;
        }
    }
}
