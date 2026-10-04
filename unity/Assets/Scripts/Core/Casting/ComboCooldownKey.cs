using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Casting
{
    /// <summary>
    /// Kombo soğuma anahtarı — çözülmüş skill kimliği (ör. 8-9); boşsa fiil id yedeği.
    /// </summary>
    public static class ComboCooldownKey
    {
        public static string For(in SkillResolution skill)
        {
            if (!skill.Identity.Id.IsEmpty)
                return skill.Identity.Id;
            return skill.Identity.Verb;
        }
    }
}
