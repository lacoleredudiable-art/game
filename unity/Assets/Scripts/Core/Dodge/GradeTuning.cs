namespace Dovus.Core.Dodge
{
    /// <summary>
    /// Sıyırma derecelendirme eşikleri — dovus-sistemi.md §6.
    /// gap = vuruş anı − dodge basma anı. Ne kadar geç bastıysan o kadar hassas.
    /// Tepki süresi burada YOK: o bir ayar değil, çalışma anında ölçülen bir sonuç.
    ///
    /// Kısıt: <see cref="TemizGapMaxMs"/> her zaman <see cref="DodgeTuning.IframeMs"/>'den
    /// KÜÇÜK kalmalı. Aksi halde SIYIRDI bandı pencerenin dışına taşar ve hiç üretilemez —
    /// pencere kapandıktan sonra gelen vuruş zaten isabet eder, derece almaz.
    ///
    /// O2 (denetim B): en üst derece artık ayrı bir eşik değil — <see cref="DodgeTuning.PerfectWindowMs"/>
    /// (tek mükemmel pencere; yük iadesi ve sonraki vuruş bonusuyla aynı). Yazısı "PERFECT".
    /// <see cref="HarikaGapMaxMs"/> bu pencereden büyük kalmalı.
    /// </summary>
    [System.Serializable]
    public class GradeTuning
    {
        public int HarikaGapMaxMs = 190;
        public int TemizGapMaxMs = 220;

        public void CopyFrom(GradeTuning other)
        {
            HarikaGapMaxMs = other.HarikaGapMaxMs;
            TemizGapMaxMs = other.TemizGapMaxMs;
        }

        public void ResetToDefaults() => CopyFrom(new GradeTuning());
    }
}
