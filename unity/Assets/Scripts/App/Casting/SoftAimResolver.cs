namespace Dovus.App.Casting
{
    /// <summary>
    /// Yumuşak nişan: bakış (ya da hareket yönü) koni içindeyse boss'a döner. Eski
    /// ManifestationDirector.ResolveAimFacing ile birebir (hız yönü önce normalize edilir, sonra bakış bir kez daha normalize edilir).
    /// </summary>
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
                float vx = velocityX;
                float vz = velocityZ;
                FlatFacingMath.Normalize(ref vx, ref vz);
                facingX = vx;
                facingZ = vz;
            }

            if (facingX * facingX + facingZ * facingZ < 0.0001f)
            {
                facingX = 0f;
                facingZ = 1f;
            }
            else
            {
                FlatFacingMath.Normalize(ref facingX, ref facingZ);
            }

            if (!hasBoss || !(softAimRangeM > CastingDefaults.Min10f))
                return;

            float toBossX = bossX - posX;
            float toBossZ = bossZ - posZ;
            float dist = (float)System.Math.Sqrt(toBossX * toBossX + toBossZ * toBossZ);
            if (dist > CastingDefaults.MinTick01f && dist <= softAimRangeM
                && FlatFacingMath.FlatAngleDeg(facingX, facingZ, toBossX, toBossZ) <= softAimConeDeg)
            {
                facingX = toBossX / dist;
                facingZ = toBossZ / dist;
            }
        }
    }
}
