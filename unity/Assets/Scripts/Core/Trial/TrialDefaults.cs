namespace Dovus.Core.Trial
{
    /// <summary>
    /// Deneme döngüsü süreleri: 10-olculer.md O-17.1, O-17.2, O-17.3 (hepsi ÖNERİ; ölçümle ayarlanır).
    /// </summary>
    public static class TrialDefaults
    {
        /// <summary>O-17.1: oyuncu bu mesafeye yaklaşınca boss uyanır (m).</summary>
        public const float WakeRadiusM = 6f;

        /// <summary>O-17.1: uyanış süresi (sn).</summary>
        public const float WakeSec = 2f;

        /// <summary>O-17.2: ölüm animasyonu, sonra düğmeler (sn).</summary>
        public const float DeathToMenuSec = 2f;

        /// <summary>O-17.2: "Tekrar"a basınca arenada olma süresi (sn).</summary>
        public const float RetryToArenaSec = 3f;

        /// <summary>O-17.3: boss ölümünden sonuç ekranına (sn).</summary>
        public const float VictoryToResultSec = 2f;

        /// <summary>
        /// 13-vizyon §3 [K]: ölünce diriliş yok. Sınır yine de ayarlanabilir; 0 = diriliş yok.
        /// </summary>
        public const int MaxRevives = 0;

        /// <summary>Silah seçiminde taşınan silah sayısı (AGENTS.md: Kılıç + Yay, iki silah).</summary>
        public const int LoadoutSlots = 2;
    }
}
