using System;
using Dovus.Core.Grammar;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Kapanış hasarı: §5 TotalEffect × ClosingDamagePerEffect (commit),
    /// üzerine skill fiil ölçeği (element-sistemi base_damage_value).
    /// Tür (İğne/Sürü/…) çarpmaz — §12. Uzunluk damage_mult JSON'da 1.0 (anti-ladder).
    /// </summary>
    public static class ClosingDamageMath
    {
        /// <summary>element-sistemi saldiri.base_damage_value — ölçek 1.0.</summary>
        public const float VerbDamageReference = 40f;

        /// <param name="totalEffect">ClosingHit.TotalEffect (§5 tablo).</param>
        /// <param name="closingDamagePerEffect">CombatTuning.ClosingDamagePerEffect.</param>
        /// <param name="skill">SkillMotor çözümü; basic strike / empty → yalnız commit.</param>
        /// <param name="isBasicStrike">Merkez jab — skill ölçeği yok.</param>
        /// <param name="outgoingDamageMult">Weaken vb. (1 = normal).</param>
        public static float Compute(
            float totalEffect,
            float closingDamagePerEffect,
            SkillResolution skill,
            bool isBasicStrike,
            float outgoingDamageMult = 1f)
        {
            if (totalEffect <= 0f || closingDamagePerEffect <= 0f)
                return 0f;

            float commit = totalEffect * closingDamagePerEffect;
            float outMult = outgoingDamageMult > 0f ? outgoingDamageMult : 1f;

            if (isBasicStrike || skill.IsEmpty)
                return commit * outMult;

            // Heal / dash / shield: can yemez; status yolu ayrı.
            if (skill.BaseDamage <= 0f)
                return 0f;

            float adj = skill.DamageMult > 0f ? skill.DamageMult : 1f;
            float verbScale = skill.BaseDamage / VerbDamageReference;
            return commit * verbScale * adj * outMult;
        }
    }
}
