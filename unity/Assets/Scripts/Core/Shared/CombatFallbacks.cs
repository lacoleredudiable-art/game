namespace Dovus.Core.Shared
{
    /// <summary>PLAN 2B.26: savaş/motion yollarında paylaşılan geometri yedekleri.</summary>
    public static class CombatFallbacks
    {
        public const float BossBodyRadiusFallbackM = 0.85f;
        public const float ArenaHalfSizeFallbackM = 50f;
        /// <summary>Boss gövde yarıçapı okunamadığında motion vuruş payı yedek (0.6 m).</summary>
        public const float MotionBossBodyRadiusFallbackM = 0.6f;
    }
}
