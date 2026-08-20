namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Cümle uzunluğuna göre bedel ve ödül — dovus-sistemi.md §5 tablosu.
    /// EffectPerSecond saklanmaz, türetilir: saklanırsa süre ayarlandığında yalan söyler.
    /// </summary>
    public class SentenceStep
    {
        public float DurationSec;
        public float TotalEffect;
        public float RecoverySec;

        public float EffectPerSecond => DurationSec > 0f ? TotalEffect / DurationSec : 0f;
    }

    /// <summary>Cümle kuralları — dovus-sistemi.md §5; bekletme §3.</summary>
    public class SentenceTuning
    {
        public int MaxSentenceDots = 4;

        // İptal pencereleri, DÜNYA zamanıyla ölçülür — dovus-sistemi.md §5.
        // Index 0 = fiil, 1 = birinci sıfat, 2 = ikinci sıfat.
        public int[] CancelWindowMs = { 420, 360, 300 };

        // Tekrar yoğunlaştırma — dovus-sistemi.md §3
        public int DwellMs = 220;
        public int DwellMaxStacks = 2;

        /// <summary>Index 0 = tek noktalı cümle, index 3 = dört noktalı.</summary>
        public SentenceStep[] Steps =
        {
            new SentenceStep { DurationSec = 0.25f, TotalEffect = 1.0f, RecoverySec = 0.18f },
            new SentenceStep { DurationSec = 0.50f, TotalEffect = 2.4f, RecoverySec = 0.26f },
            new SentenceStep { DurationSec = 0.80f, TotalEffect = 4.4f, RecoverySec = 0.38f },
            new SentenceStep { DurationSec = 1.20f, TotalEffect = 7.0f, RecoverySec = 0.55f },
        };

        /// <summary>Nokta sayısına göre adım (1..MaxSentenceDots).</summary>
        public SentenceStep StepForDots(int dots)
        {
            if (dots < 1) dots = 1;
            if (dots > Steps.Length) dots = Steps.Length;
            return Steps[dots - 1];
        }

        /// <summary>Sıradaki noktayı basmak için kalan pencere; sıfat yuvası dolduysa 0.</summary>
        public int CancelWindowForDots(int dotsSoFar)
        {
            var index = dotsSoFar - 1;
            if (index < 0 || index >= CancelWindowMs.Length) return 0;
            return CancelWindowMs[index];
        }
    }
}
