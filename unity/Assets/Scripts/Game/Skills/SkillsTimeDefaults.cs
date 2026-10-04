namespace Dovus.Game.Skills
{
    /// <summary>Skills katmanı süre dönüşümü ve paylaşılan eşikler (PLAN 2B.11d).</summary>
    public static class SkillsTimeDefaults
    {
        public const float MinPositiveSec = 0.01f;
        public const float ShieldHeldEpsilon = 0.01f;
        public const float MotorVelocityEpsilonSqr = 0.01f;
    }
}
