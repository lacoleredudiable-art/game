namespace Dovus.Game.Casting
{
    /// <summary>Syllable dot audio feedback (PLAN 2B.11f).</summary>
    public static class SyllableFeedbackDefaults
    {
        public const float DotFireHz = 440f;
        public const float DotLightHz = 494f;
        public const float DotLightningHz = 370f;
        public const float DotWaterHz = 330f;
        public const float DotDarkHz = 294f;
        public const float DotEarthHz = 262f;
        public const float SentencePitchStep = 0.09f;
        public const float PitchClampMin = 0.85f;
        public const float PitchClampMax = 1.55f;
        public const float DotOneShotVolume = 0.7f;
        public const long FallbackDotVibrationMs = 30L;
        public const float DenyHz = 120f;
        public const float DenyPitch = 0.85f;
        public const float DenyOneShotVolume = 0.45f;
        public const int DenyHapticMs = 20;
        public const float ClipSampleAmp = 0.55f;
    }
}
