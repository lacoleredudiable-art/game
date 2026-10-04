namespace Dovus.App.Boss
{
    /// <summary>PLAN 2B.11c: gömülü oynanış/ayar sayıları.</summary>
    public static class BossDefaults
    {
        public const double RollClampMax = 0.999999999;
        public const float MinDistM = 0.01f;
        public const float MinAirborneSec = 0.05f;
        public const float AllyTargetWeight = 0.3f;
        public const float BlindSpreadMult = 1.5f;
        public const float DefaultMaxHp = 120f;
        public const float SegmentLen2EpsilonSqr = 1e-8f;
        public const float FallbackBossMaxHp = 22000f;
        public const float AllyReviveSec = 8f;
    }
}
