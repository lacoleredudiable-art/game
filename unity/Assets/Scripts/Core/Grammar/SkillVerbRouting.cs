using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    /// <summary>Fiil kimliğiyle yönlendirme — string "2" karşılaştırmaları yerine.</summary>
    public static class SkillVerbRouting
    {
        public static bool IsFieldAuraVerb(RuneId verb)
        {
            if (!RuneIdGrammar.TryAsRune(verb, out Rune rune))
                return false;
            return rune is Rune.Heal or Rune.Defense or Rune.Empower or Rune.Cleanse;
        }

        public static bool IsBurstVerb(RuneId verb) =>
            RuneIdGrammar.TryAsRune(verb, out Rune rune) && rune == Rune.Burst;
    }
}
