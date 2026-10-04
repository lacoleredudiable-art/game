namespace Dovus.Core.Status
{
    /// <summary>Status tarafı yedekleri; Combat SkillNumberFallbacks ile aynı değerler (döngü kırma).</summary>
    internal static class StatusDefaults
    {
        /// <summary>element-sistemi.json kök bağışıklığı yoksa 0.5 sn — SkillNumberFallbacks.RootImmunityMs ile aynı.</summary>
        public const double RootImmunityMs = 500.0;

        public const float SelfHasteBonus = 0.5f;
        public const double TempoSyncFallbackMs = 1000.0;
        public const float TempoSyncFallbackStrength = 0.7f;

        public const float MinRadius01f = 0.01f;
        public const double SecToMs = 1000.0;
    }
}
