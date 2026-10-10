namespace Dovus.Core.RuleEngineV4
{
    /// <summary>JSON yedek sayıları (kural-motoru-v4.json ile uyumlu; spec'te yok notu PR1).</summary>
    public static class RuleEngineV4CatalogDefaults
    {
        public const float BasePoise = 50f;
        public const float RangeReferenceM = 6f;
        public const float YogunChargeSec = 0.35f;
        public const float YogunPowerMult = 1.35f;
        public const float YayilanRadiusM = 4f;
        public const float YayilanPowerMult = 0.85f;
        public const float RitmReferenceTotalSec = 0.55f;
        public const float KontrolDurationSec = 1.5f;
        public const float KorumaYogunBlockSec = 0.45f;
        public const float TetikliWaitSec = 5f;
        public const float IsaretUseRangeM = 25f;
        public const float YansitmaRatio = 0.85f;
        public const float IsaretLifeSec = 20f;
        public const float ZamanLookbackSec = 3f;
        public const float KuvvetPushM = 3f;
        public const float YansitmaDurationSec = 2f;
        public const float YansitmaYogunDurationSec = 0.45f;
        public const float ConjureLifeSec = 5f;
        public const float ConjureYogunLifeSec = 2.5f;
        public const float WeaponTotalSecFallback = 0.55f;
    }
}
