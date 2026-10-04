namespace Dovus.Game.Skills
{
    /// <summary>Skills katmanı süre dönüşümü ve paylaşılan eşikler (PLAN 2B.11d).</summary>
    public static class SkillsTimeDefaults
    {
        public const float MinPositiveSec = 0.01f;
        public const float MotionReturnHeightM = 0.6f;
        public const double SecToMs = 1000.0;
        public const float ShieldHeldEpsilon = 0.01f;
        public const float MotorVelocityEpsilonSqr = 0.01f;
    }
}
