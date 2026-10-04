using Dovus.Core.Element;
namespace Dovus.Core.Element
{
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
