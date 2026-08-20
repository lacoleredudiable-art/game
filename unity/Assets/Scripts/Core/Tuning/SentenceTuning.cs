namespace Dovus.Core.Tuning
{
    /// <summary>Cümle kuralları — dovus-sistemi.md §5; dwell §3.</summary>
    public class SentenceTuning
    {
        public int MaxSentenceDots = 4;

        // İptal pencereleri (dünya zamanı) — dovus-sistemi.md §5
        public int VerbWindowMs = 420;
        public int Adjective1WindowMs = 360;
        public int Adjective2WindowMs = 300;

        // Tekrar yoğunlaştırma — dovus-sistemi.md §3
        public int DwellMs = 220;
        public int DwellMaxStacks = 2;

        // Cümle uzunluğu eğrisi — dovus-sistemi.md §5 tablo
        public float Dot1DurationSec = 0.25f;
        public float Dot1TotalEffect = 1.0f;
        public float Dot1EffectPerSecond = 4.0f;
        public float Dot1RecoverySec = 0.18f;

        public float Dot2DurationSec = 0.50f;
        public float Dot2TotalEffect = 2.4f;
        public float Dot2EffectPerSecond = 4.8f;
        public float Dot2RecoverySec = 0.26f;

        public float Dot3DurationSec = 0.80f;
        public float Dot3TotalEffect = 4.4f;
        public float Dot3EffectPerSecond = 5.5f;
        public float Dot3RecoverySec = 0.38f;

        public float Dot4DurationSec = 1.20f;
        public float Dot4TotalEffect = 7.0f;
        public float Dot4EffectPerSecond = 5.8f;
        public float Dot4RecoverySec = 0.55f;
    }
}
