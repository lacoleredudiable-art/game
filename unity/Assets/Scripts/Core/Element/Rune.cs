using Dovus.Core.Element;
namespace Dovus.Core.Element
{
    /// <summary>
    /// element-sistemi v6.1.1 çift-yüzlü rün kimlikleri. Ekrandaki altı nokta rün kimliği
    /// değildir; <see cref="RuneLoadout"/> seçili 6 rünü slotlara eşler.
    /// </summary>
    public enum Rune
    {
        Saldiri = 1,
        Iyilestirme = 2,
        Hareket = 3,
        Savunma = 4,
        Patlama = 5,
        Kontrol = 6,
        Zayiflatma = 7,
        Guclendirme = 8,
        Arindirma = 9,
        Yansima = 10,
        Cagirma = 11,
        Zaman = 12,

        // Kaynak uyumluluğu: eski görsel/motor sınıfları ilk altı id'yi bu adlarla kullanıyor.
        // Yeni çözümleme ve UI bu alias'ları değil v6 adlarını gösterir.
        Ates = Saldiri,
        Su = Iyilestirme,
        Hava = Hareket,
        Toprak = Savunma,
        Aydinlik = Patlama,
        Karanlik = Kontrol
    }

    public static class RuneInfo
    {
        public static string Syllable(Rune rune) => "r" + ((int)rune).ToString();

        /// <summary>element-sistemi.json v6.1.1 rün adları.</summary>
        public static string DisplayName(Rune rune) => rune switch
        {
            Rune.Saldiri => "Saldırı",
            Rune.Iyilestirme => "İyileştirme",
            Rune.Hareket => "Hareket",
            Rune.Savunma => "Savunma",
            Rune.Patlama => "Patlama",
            Rune.Kontrol => "Kontrol",
            Rune.Zayiflatma => "Zayıflatma",
            Rune.Guclendirme => "Güçlendirme",
            Rune.Arindirma => "Arındırma",
            Rune.Yansima => "Yansıma",
            Rune.Cagirma => "Çağırma",
            Rune.Zaman => "Zaman",
            _ => "?"
        };

        public static bool TryFromId(int id, out Rune rune)
        {
            if (id is >= 1 and <= 12)
            {
                rune = (Rune)id;
                return true;
            }

            rune = default;
            return false;
        }

        /// <summary>Eski/test varsayılanı: slot 1..6 doğrudan ilk altı rüne gider.</summary>
        public static bool TryFromDot(int dot, out Rune rune)
        {
            return RuneLoadout.Sequential.TryResolveSlot(dot, out rune);
        }

        public static bool TryFromDot(int dot, RuneLoadout loadout, out Rune rune)
        {
            return (loadout ?? RuneLoadout.Sequential).TryResolveSlot(dot, out rune);
        }
    }
}
