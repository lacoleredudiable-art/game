using Dovus.App.Casting;

namespace Dovus.App.Boss
{
    public readonly struct ApproachStep
    {
        public ApproachStep(bool stop, float newHomeX, float newHomeZ, float walkMps, float dirX, float dirZ)
        {
            Stop = stop;
            NewHomeX = newHomeX;
            NewHomeZ = newHomeZ;
            WalkMps = walkMps;
            DirX = dirX;
            DirZ = dirZ;
        }

        public bool Stop { get; }
        public float NewHomeX { get; }
        public float NewHomeZ { get; }
        public float WalkMps { get; }
        public float DirX { get; }
        public float DirZ { get; }
    }

    public static class BossApproachRules
    {
        public static bool BlocksMovement(bool bossStatusPresent, bool effectiveBlocksMovement) =>
            bossStatusPresent && effectiveBlocksMovement;

        public static bool BlocksFromSpeedMult(bool bossStatusPresent, float effectiveMoveSpeedMult) =>
            bossStatusPresent && effectiveMoveSpeedMult <= BossDefaults.MinDistM;

        public static float StopDistanceM(float bodyRadiusM, float aimRadiusM, float stopPadM) =>
            bodyRadiusM + aimRadiusM + stopPadM;

        public static ApproachStep ComputeStep(
            float homeX,
            float homeZ,
            float aimX,
            float aimZ,
            float bodyRadiusM,
            float aimRadiusM,
            float stopPadM,
            bool reversed,
            float approachSpeedMps,
            float moveSpeedMult,
            float dtSec)
        {
            float toX = aimX - homeX;
            float toZ = aimZ - homeZ;
            if (!reversed)
            {
                float stop = StopDistanceM(bodyRadiusM, aimRadiusM, stopPadM);
                if (toX * toX + toZ * toZ <= stop * stop)
                    return new ApproachStep(true, homeX, homeZ, 0f, 0f, 0f);
            }
            else if (toX * toX + toZ * toZ <= 0.0001f)
                return new ApproachStep(true, homeX, homeZ, 0f, 0f, 0f);

            float dirX = toX;
            float dirZ = toZ;
            if (reversed)
            {
                dirX = -dirX;
                dirZ = -dirZ;
            }

            FlatFacingMath.Normalize(ref dirX, ref dirZ);
            float groundMps = approachSpeedMps * moveSpeedMult;
            float newX = homeX + dirX * groundMps * dtSec;
            float newZ = homeZ + dirZ * groundMps * dtSec;
            return new ApproachStep(false, newX, newZ, groundMps, dirX, dirZ);
        }
    }
}
