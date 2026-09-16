namespace Dovus.Core.Grammar
{
    /// <summary>
    /// Altıgen noktaları — element-sistemi.json çekirdek sırası:
    /// 1 Ateş, 2 Su, 3 Hava, 4 Toprak, 5 Aydınlık, 6 Karanlık.
    /// 16 Eylül: enum adları artık gerçek elementle eşleşiyor (eskiden İĞNE/SÜRÜ/KABUK/
    /// ZEHİR/SARSINTI/TOPRAK gibi beşgen kalıntısı adlardı — `Rune.Toprak` dot 6/Karanlık'a
    /// denk geliyordu, gerçek Toprak dot 4'tü; kafa karıştırdığı için düzeltildi).
    /// </summary>
    public enum Rune
    {
        Ates = 1,
        Su = 2,
        Hava = 3,
        Toprak = 4,
        Aydinlik = 5,
        Karanlik = 6
    }

    public static class RuneInfo
    {
        public static string Syllable(Rune rune) => rune switch
        {
            Rune.Ates => "hi",
            Rune.Su => "hu",
            Rune.Hava => "ho",
            Rune.Toprak => "he",
            Rune.Aydinlik => "ha",
            Rune.Karanlik => "hm",
            _ => string.Empty
        };

        /// <summary>element-sistemi.json çekirdek adları.</summary>
        public static string DisplayName(Rune rune) => rune switch
        {
            Rune.Ates => "Ateş",
            Rune.Su => "Su",
            Rune.Hava => "Hava",
            Rune.Toprak => "Toprak",
            Rune.Aydinlik => "Aydınlık",
            Rune.Karanlik => "Karanlık",
            _ => "?"
        };

        public static bool TryFromDot(int dot, out Rune rune)
        {
            if (dot is >= 1 and <= PentagonLayout.DotCount)
            {
                rune = (Rune)dot;
                return true;
            }

            rune = default;
            return false;
        }
    }
}
