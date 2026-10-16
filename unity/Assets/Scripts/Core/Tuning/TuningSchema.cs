namespace Dovus.Core.Tuning
{
    /// <summary>
    /// O5 (denetim C): tuning.json şema sürümü. Kod varsayılanı değişen her PR bu sayıyı artırır
    /// (CoreTests varsayılan parmak izini sürümle birlikte sabitler; değişince test sürüm artışı ister).
    /// Eski sürümlü kayıt yüklenmez: kod varsayılanları kazanır, eski dosya yedeğe alınır.
    /// Sürüm 1 = BossDamageMigration dönemi. Sürüm 2 = denetim A/B/C varsayılanları
    /// (i-frame 260, kaçış 3,8 m, tek PERFECT 150 ms / HARİKA 190, kör %30, PERFECT bonusu 2 sn).
    /// Sürüm 9 = EnforceCooldown ve EnforceResourceCost varsayılan true (1.1b).
    /// Sürüm 10 = dodge 2×6 sn dolum + birleşik dodge ayarları (PLAN 1.3).
    /// Sürüm 11 = SkillKick* Punch FOV oranı 0–0,25 (eski 0,8–3,2 FOV'u 0'a düşürüyordu).
    /// Sürüm 12 = dodge zincir test değerleri: T_tek 500, birleşme penceresi 500, T_zincir 1300 ms (15-dodge-tasarimi §3.4).
    /// Sürüm 13 = süzülme kuyruğu 120 → 400 ms (O-4.4a): hareket birleşme penceresini örter, takla süzülmede de tutar.
    /// Sürüm 14 = O-4.9 engelde kayma ayarları (SlideMinRatio, EngineChoiceT, SlideScanPadM, SlideMaxIter, SlideStallRatio).
    /// Sürüm 15 = mükemmel dodge hak iadesi kalktı (PerfectChargeRefund silindi; oyunda iade yok, Q16 2026-10-16).
    /// </summary>
    public static class TuningSchema
    {
        public const int Version = 15;

        public enum LoadDecision
        {
            /// <summary>Kayıt güncel: değerler uygulanır.</summary>
            Apply,
            /// <summary>Kayıt eski: uygulanmaz, kod varsayılanları kalır, dosya yedeklenip yeniden yazılır.</summary>
            DiscardStale
        }

        /// <summary>Daha yeni bir build'in kaydı (ileri sürüm) uygulanır; bilinmeyen alanları JsonUtility atlar.</summary>
        public static LoadDecision Decide(int storedVersion) =>
            storedVersion < Version ? LoadDecision.DiscardStale : LoadDecision.Apply;
    }
}
