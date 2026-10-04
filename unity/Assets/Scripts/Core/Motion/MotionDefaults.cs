namespace Dovus.Core.Motion
{
    /// <summary>PLAN 2B.11c: gömülü oynanış/ayar sayıları.</summary>
    public static class MotionDefaults
    {
        public const float GroundSnapEpsilonM = 0.0005f;
        public const float MinDistM = 0.01f;
        public const float GroundReleaseDeltaM = 0.02f;
        public const float FixedStepFallbackSec = 0.02f;
        public const float MinDashM = 0.05f;
        public const float DebugSpeedThresholdMps = 1.5f;
        public const float SmoothStepThree = 3f;
        public const float VerticalCurveFourMult = 4f;
        public const int CombosPerWeapon = 144;
        public const int TotalSweepComboCount = 288;
    }
}
