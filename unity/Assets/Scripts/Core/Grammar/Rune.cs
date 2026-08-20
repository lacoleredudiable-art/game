namespace Dovus.Core.Grammar
{
    /// <summary>Beşgen noktaları — dovus-sistemi.md §4.</summary>
    public enum Rune
    {
        Igne = 1,
        Suru = 2,
        Kabuk = 3,
        Zehir = 4,
        Sarsinti = 5
    }

    public static class RuneInfo
    {
        public static string Syllable(Rune rune) => rune switch
        {
            Rune.Igne => "hi",
            Rune.Suru => "hu",
            Rune.Kabuk => "ho",
            Rune.Zehir => "he",
            Rune.Sarsinti => "ha",
            _ => string.Empty
        };

        public static bool TryFromDot(int dot, out Rune rune)
        {
            if (dot is >= 1 and <= 5)
            {
                rune = (Rune)dot;
                return true;
            }

            rune = default;
            return false;
        }
    }
}
