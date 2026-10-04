namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Cümle uzunluğuna göre bedel ve ödül — dovus-sistemi.md §5 tablosu.
    /// EffectPerSecond saklanmaz, türetilir: saklanırsa süre ayarlandığında yalan söyler.
    /// </summary>
    [System.Serializable]
    public class SentenceStep
    {
        public float DurationSec;
        public float TotalEffect;
        public float RecoverySec;

        public float EffectPerSecond => DurationSec > 0f ? TotalEffect / DurationSec : 0f;

        public void CopyFrom(SentenceStep other)
        {
            DurationSec = other.DurationSec;
            TotalEffect = other.TotalEffect;
            RecoverySec = other.RecoverySec;
        }
    }
}
