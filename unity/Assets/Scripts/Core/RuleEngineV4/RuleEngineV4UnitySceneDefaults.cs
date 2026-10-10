namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Unity v4 yürütücü sabitleri (Skills float ratchet dışı).</summary>
    public static class RuleEngineV4UnitySceneDefaults
    {
        public const float MeleeArcRadiusScale = 0.6f;
        public const float MinBodyRadiusM = 0.1f;
        public const float StrikeChestOffsetM = 0.9f;
        public const float ProjectileOriginHeightM = 1f;
        public const float SliceMinionFallbackRadiusM = 0.5f;
        public const int AreaDeliveryScanCap = 99;
        public const int StatusHardCcWeight = 3;
        public const int StatusSoftCcWeight = 2;
        public const int StatusDebuffWeight = 1;
        public const float StructureForwardOffsetM = 1.5f;
        public const float StructureGroundYM = 0.25f;
        public const float StructureScaleDepthMult = 3f;
        public const float StructureScaleHalfMult = 0.5f;
    }
}
