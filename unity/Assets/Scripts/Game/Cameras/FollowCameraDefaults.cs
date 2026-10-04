namespace Dovus.Game.Cameras
{
    /// <summary>FollowCamera takip ve sarsıntı (PLAN 2B.11e).</summary>
    public static class FollowCameraDefaults
    {
        public const float PunchDecay = 6f;
        public const float FallbackShakePxToM = 0.01f;
        public const float MinPositiveSmoothSec = 0.01f;
        public const float LookAheadBlend = 0.35f;
        public const float FovClampMinDeg = 35f;
        public const float FovClampMaxDeg = 85f;
        public const float SoftLockDistanceWeightStart = 0.72f;
        public const float MinTargetAlongM = 0.02f;
        public const float MinCollisionSphereRadiusM = 0.05f;
        public const float MinSmoothedAlongDistM = 0.01f;
        public const float ShoulderSignEpsilon = 0.01f;
        public const float PlayerViewportHeightM = 0.9f;
        public const float PlayerSideViewportLeft = 0.42f;
        public const float PlayerSideViewportRight = 0.58f;
        public const float PlayerSideWeightEdge = 0.35f;
        public const float PlayerSideWeightCenter = 0.7f;
        public const float PlayerLowViewportThreshold = 0.55f;
        public const float MinLocalDepthM = 0.05f;
        public const float ActorRectHeightM = 0.9f;
        public const float ActorRectHalfWidthM = 0.45f;
        public const float BossRectHalfWidthM = 1.2f;
        public const float DebugOffsetM = 0.35f;
        public const float ShakeAngleRateHz = 42f;
        public const float ShakeCosHarmonicMult = 1.3f;
        public const float ShakeAmplitudeFalloffMult = 0.35f;
    }
}
