using Dovus.Core.Element;
using Dovus.Core.Shared;
using System.Globalization;

namespace Dovus.Core.Grammar
{
    public static class RuneIdGrammar
    {
        public static RuneId FromRune(Rune rune) =>
            new RuneId(((int)rune).ToString(CultureInfo.InvariantCulture));

        public static bool TryAsRune(RuneId id, out Rune rune)
        {
            if (int.TryParse(id.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                return RuneInfo.TryFromId(n, out rune);
            rune = default;
            return false;
        }
    }
}
