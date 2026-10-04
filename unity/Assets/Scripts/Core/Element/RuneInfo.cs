using Dovus.Core.Element;
namespace Dovus.Core.Element
{
    public static class RuneInfo
    {
        public static string Syllable(Rune rune) => "r" + ((int)rune).ToString();

        /// <summary>element-sistemi.json v6.1.1 rün adları (Türkçe UI).</summary>
        public static string DisplayName(Rune rune) => rune switch
        {
            Rune.Attack => "Saldırı",
            Rune.Heal => "İyileştirme",
            Rune.Move => "Hareket",
            Rune.Defense => "Savunma",
            Rune.Burst => "Patlama",
            Rune.Control => "Kontrol",
            Rune.Weaken => "Zayıflatma",
            Rune.Empower => "Güçlendirme",
            Rune.Cleanse => "Arındırma",
            Rune.Reflect => "Yansıma",
            Rune.Summon => "Çağırma",
            Rune.Time => "Zaman",
            _ => "?"
        };

        /// <summary>
        /// Eski <c>Rune</c> için <c>Enum.ToString()</c> çıktısı (id'den cast); sweep/log bayt uyumu.
        /// </summary>
        public static string LegacySerializationName(Rune rune) => rune switch
        {
            Rune.Attack => "Saldiri",
            Rune.Heal => "Iyilestirme",
            Rune.Move => "Hareket",
            Rune.Defense => "Savunma",
            Rune.Burst => "Patlama",
            Rune.Control => "Kontrol",
            Rune.Weaken => "Zayiflatma",
            Rune.Empower => "Guclendirme",
            Rune.Cleanse => "Arindirma",
            Rune.Reflect => "Yansima",
            Rune.Summon => "Cagirma",
            Rune.Time => "Zaman",
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
