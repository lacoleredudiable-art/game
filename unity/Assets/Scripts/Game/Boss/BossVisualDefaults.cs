namespace Dovus.Game.Boss
{
    /// <summary>BossVisual animasyon yedekleri (PLAN 2B.11e).</summary>
    public static class BossVisualDefaults
    {
        public const float StaggerCooldownSentinelSec = 999f;
        public const float MinLocoSpeedMps = 0.01f;
        public const float LocoSpeedDampSec = 0.12f;
        public const float LocoSpeedClampMin = 0.2f;
        public const float LocoSpeedClampMax = 2.5f;
        public const float FallbackImpactNorm = 0.42f;
        public const float MinWindupSec = 0.05f;
        public const float StrikeSpeedClampMin = 0.25f;
        public const float StrikeSpeedClampMax = 4f;
        public const float FallbackStaggerMinGapSec = 0.6f;
        public const float StaggerCrossFadeSec = 0.05f;
        public const float MinBusySec = 0.1f;
        public const float FallbackAnimCrossFadeSec = 0.15f;
        public const float FallbackWalkClipMps = 1.4f;
        public const float MinMeasuredWalkMps = 0.05f;
        public const float MinPositiveScale = 0.01f;
    }
}
