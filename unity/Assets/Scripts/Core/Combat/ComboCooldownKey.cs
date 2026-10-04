using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Kombo soğuma anahtarı — çözülmüş skill kimliği (ör. "8-9"); boşsa fiil id yedeği.
    /// </summary>
    public static class ComboCooldownKey
    {
        public static string For(in SkillResolution skill)
        {
            if (!string.IsNullOrEmpty(skill.SkillId))
                return skill.SkillId;
            return skill.VerbId ?? string.Empty;
        }
    }
}
