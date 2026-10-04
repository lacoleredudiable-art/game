using Dovus.Core.Grammar;
using System.Collections.Generic;

namespace Dovus.App.Casting
{
    /// <summary>Düz vuruş pending kapanışı — ManifestationDirector.IsPendingBasic ile birebir.</summary>
    public static class PendingBasicStrikeRules
    {
        public static bool IsPendingBasic(
            bool isBasicStrikeFlag,
            bool viewIsBasicStrike,
            IReadOnlyList<SentenceWord> words,
            int basicStrikeDot)
        {
            if (isBasicStrikeFlag || viewIsBasicStrike)
                return true;
            if (words == null || words.Count != 1)
                return false;
            return (int)words[0].Rune == basicStrikeDot;
        }
    }
}
