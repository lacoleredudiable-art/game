using System;
using Dovus.Core.Grammar;

namespace Dovus.App.Casting
{
    public static class ClosingHealRules
    {
        public static bool IsHealSkill(SkillResolution skill)
        {
            if (string.Equals(skill.VerbFamily, "mend", StringComparison.Ordinal))
                return true;
            string action = skill.Action ?? string.Empty;
            return action is "heal" or "regen" or "cleanse" or "area_cleanse" or "holy_shield";
        }

        public static float AdjectiveLifesteal(in SkillResolution skill)
        {
            if (skill.IsEmpty || skill.Engine.IsNull)
                return 0f;
            var mods = skill.Engine;
            float v = mods.HasLifesteal ? mods.Lifesteal(0f) : mods.ApplyLifesteal(0f);
            return Math.Max(0f, v);
        }

        public static float ExtraCritChanceAdd(in SkillResolution skill)
        {
            if (skill.IsEmpty || skill.Engine.IsNull || !skill.Engine.HasCritChanceAdd)
                return 0f;
            return Math.Max(0f, skill.Engine.CritChanceAdd(0f));
        }

        public static int ComputeHealAmount(
            float totalEffect,
            SkillResolution skill,
            float effectScale,
            float chainBonus,
            float closingDamagePerEffect,
            float weaponSupportPower)
        {
            if (skill.IsEmpty || !IsHealSkill(skill) || effectScale <= 0f)
                return 0;
            float healBase = skill.BaseHeal > 0f
                ? skill.BaseHeal
                : totalEffect * closingDamagePerEffect;
            double scaled = healBase * chainBonus * weaponSupportPower * effectScale;
            return Math.Max(0, (int)Math.Round(scaled) /* = Mathf.RoundToInt (yarıda çifte) */);
        }
    }
}
