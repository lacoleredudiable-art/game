namespace Dovus.Game.Boss
{
    /// <summary>Boss telegraf zamanlama ve ölçek çarpanları — BossTelegraph tek kaynak (PLAN 2B.11).</summary>
    public static class BossTelegraphDefaults
    {
        public const float MinRadiusProgress = 0.12f;

        public const float WebLineWidthBase = 0.14f;
        public const float WebLineWidthPerProgress = 0.2f;

        public const float SlamLineWidthBase = 0.22f;
        public const float SlamLineWidthPerProgress = 0.18f;

        public const float ChargeDiscWidthBase = 0.16f;
        public const float ChargeDiscWidthPerProgress = 0.17f;

        public const float ChargePoseProgressMult = 1.35f;

        public const float HotDiscAlpha = 0.34f;
        public const float SlamPoseWarmupSec = 0.02f;

        public const float SlamSquashPoseMult = 0.5f;

        public const float SfxSpatialBlend = 0.35f;

        public const float PlanarDirEpsilonSqr = 0.0001f;
    }

}
