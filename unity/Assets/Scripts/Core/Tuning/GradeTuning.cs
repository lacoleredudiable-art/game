namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Sıyırma derecelendirme eşikleri — dovus-sistemi.md §6.
    /// gap = vuruş anı − dodge basma anı. Ne kadar geç bastıysan o kadar hassas.
    /// Tepki süresi burada YOK: o bir ayar değil, çalışma anında ölçülen bir sonuç.
    ///
    /// Kısıt: <see cref="TemizGapMaxMs"/> her zaman <see cref="DodgeTuning.IframeMs"/>'den
    /// KÜÇÜK kalmalı. Aksi halde SIYIRDI bandı pencerenin dışına taşar ve hiç üretilemez —
    /// pencere kapandıktan sonra gelen vuruş zaten isabet eder, derece almaz.
    /// </summary>
    [System.Serializable]
    public class GradeTuning
    {
        public int MukemmelGapMaxMs = 90;
        public int HarikaGapMaxMs = 160;
        public int TemizGapMaxMs = 220;

        public void CopyFrom(GradeTuning other)
        {
            MukemmelGapMaxMs = other.MukemmelGapMaxMs;
            HarikaGapMaxMs = other.HarikaGapMaxMs;
            TemizGapMaxMs = other.TemizGapMaxMs;
        }

        public void ResetToDefaults() => CopyFrom(new GradeTuning());
    }
}
