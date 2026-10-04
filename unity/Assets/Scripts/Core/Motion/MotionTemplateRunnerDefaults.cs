namespace Dovus.Core.Motion
{
    /// <summary>PLAN 2B.11c: gömülü oynanış/ayar sayıları.</summary>
    public static class MotionTemplateRunnerDefaults
    {
        public const float FacingDistSqrMin = 0.0004f;
        public const float MinDistM = 0.01f;
        public const float ClearDistEpsilonM = 0.02f;
        public const float StepDistSqrMin = 0.04f;
        public const float MinRadiusM = 0.05f;
        public const float StopGapM = 0.15f;
        public const float PlantHitRadiusM = 0.25f;
        public const float PlantHitLengthM = 0.2f;
        public const float LateralFrac = 0.35f;
        public const float OutgoingMultCap = 0.999f;
        public const float SegmentLen2EpsilonSqr = 1e-8f;
        public const float MaxDeltaPerTickMult = 40f;
        public const float JumpHeightFourMult = 4f;
    }
}
