namespace Dovus.Game.Casting.Input
{
    /// <summary>Casting input timing and stroke gaps (PLAN 2B.11f).</summary>
    public static class CastingInputDefaults
    {
        public const double SecToMs = 1000.0;
        public const float StrokeCornerRadiusDp = 12f;
        public const float StrokeMinSegmentDp = 1.5f;
        public const long FallbackDotVibrationMs = 30L;
    }
}
