namespace Dovus.Core.Tuning
{
    /// <summary>Sıyırma derecelendirme eşikleri — dovus-sistemi.md §6.</summary>
    public class GradeTuning
    {
        // gap eşikleri (ms)
        public int MukemmelGapMaxMs = 110;
        public int HarikaGapMaxMs = 200;
        public int TemizGapMaxMs = 320;

        // Ekranda gösterilen tepki süresi — dovus-sistemi.md §6
        public float ReactionDisplaySec = 0.45f;
    }
}
