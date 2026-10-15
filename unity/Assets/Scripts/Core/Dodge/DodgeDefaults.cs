namespace Dovus.Core.Dodge
{
    /// <summary>PLAN 2B.11c: gömülü oynanış/ayar sayıları.</summary>
    public static class DodgeDefaults
    {
        public const float ArmedMultThreshold = 1.0001f;
        public const float SegmentLen2EpsilonSqr = 1e-8f;
        public const float DiscriminantFourMult = 4f;
        /// <summary>O-4.9 SlideStepMs: belgede değer yok (ölçüm); [Ö] bir kare (60 fps), telefonda doğrulanır.</summary>
        public const int SlideStepMs = 16;
        public const float SlideEpsilonM = 0.0005f;
        /// <summary>Kayma yüzey temas sorgusunun kenar payına eklenen menzil (m).</summary>
        public const float SlideProbeM = 0.05f;
    }
}
