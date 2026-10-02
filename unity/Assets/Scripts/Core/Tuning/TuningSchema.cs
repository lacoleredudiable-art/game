namespace Dovus.Core.Tuning
{
    /// <summary>
    /// O5 (denetim C): tuning.json şema sürümü. Kod varsayılanı değişen her PR bu sayıyı artırır
    /// (CoreTests varsayılan parmak izini sürümle birlikte sabitler; değişince test sürüm artışı ister).
    /// Eski sürümlü kayıt yüklenmez: kod varsayılanları kazanır, eski dosya yedeğe alınır.
    /// Sürüm 1 = BossDamageMigration dönemi. Sürüm 2 = denetim A/B/C varsayılanları
    /// (i-frame 260, kaçış 3,8 m, tek PERFECT 150 ms / HARİKA 190, kör %30, PERFECT bonusu 2 sn).
    /// </summary>
    public static class TuningSchema
    {
        public const int Version = 8;

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
