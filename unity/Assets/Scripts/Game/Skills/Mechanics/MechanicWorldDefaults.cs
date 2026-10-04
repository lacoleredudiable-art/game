namespace Dovus.Game.Skills.Mechanics
{
    /// <summary>Mekanik dünya gövde/alan ölçüleri — MechanicWorldRuntime tek kaynak (PLAN 2B.11).</summary>
    public static class MechanicWorldDefaults
    {
        public const float MinBodyRadiusM = 0.5f;
        public const float MinThicknessM = 0.05f;
        public const float BodySizeLengthMult = 2f;
        public const float BodyHeightHalfMult = 0.5f;
        public const float DecoyHeightScale = 1.8f;
        public const float DecoyRadiusHalfMult = 0.5f;

        public const float ExecutorFieldAlphaFallback = 0.6f;
        public const float LinkLineWidthFactor = 0.1f;
        public const float LinkLineMinWidthM = 0.02f;

        public const double LinkTickMinMs = 50.0;
        public const double LinkTickRateMin = 0.01;

        public const float PlayerMoveEpsilonSqr = 0.01f;
        public const float PlanarDirEpsilonSqr = 0.0001f;
    }

}
