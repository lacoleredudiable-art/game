namespace Dovus.Core.Tuning
{
    /// <summary>
    /// Sıyırma derecelendirme eşikleri — dovus-sistemi.md §6.
    /// gap = vuruş anı − dodge basma anı. Ne kadar geç bastıysan o kadar hassas.
    /// Tepki süresi burada YOK: o bir ayar değil, çalışma anında ölçülen bir sonuç.
    /// </summary>
    public class GradeTuning
    {
        public int MukemmelGapMaxMs = 110;
        public int HarikaGapMaxMs = 200;
        public int TemizGapMaxMs = 320;
    }
}
