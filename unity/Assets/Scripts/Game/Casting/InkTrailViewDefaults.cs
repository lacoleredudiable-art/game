namespace Dovus.Game.Casting
{
    /// <summary>Ink trail line renderer tuning (PLAN 2B.11f).</summary>
    public static class InkTrailViewDefaults
    {
        public const float DefaultLingerSec = 0.4f;
        public const float RawMinStepDp = 4f;
        public const float FailWidthMult = 1.6f;
        public const float DefaultRawWidthScale = 0.9f;
        public const float RawHeadLerp = 0.45f;
        public const float RawHeadAlpha = 0.95f;
        public const float RawTailAlpha = 0.55f;
        public const float TangentEpsilonSqr = 0.01f;
        public const float HashNormalizeDiv = 65535f;
        public const float FailDispersionBaseDp = 8f;
        public const float FailDispersionRangeDp = 6f;
        public const int HashMultiplier = 1103515245;
        public const int HashAdd = 12345;
        public const float FadeMinLifeSec = 0.01f;
        public const float FlashWidthMix = 0.6f;
        public const float LineEndWidthMult = 0.85f;
    }
}
