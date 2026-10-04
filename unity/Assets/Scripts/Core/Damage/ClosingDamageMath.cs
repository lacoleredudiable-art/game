using System;
using Dovus.Core.Grammar;
using Dovus.Core.Casting;

namespace Dovus.Core.Damage
{
    /// <summary>
    /// Kapanış hasarı: §5 TotalEffect × ClosingDamagePerEffect (commit),
    /// üzerine skill fiil ölçeği (element-sistemi base_damage_value).
    /// Tür (İğne/Sürü/…) çarpmaz — §12. Uzunluk damage_mult JSON'da 1.0 (anti-ladder).
    /// </summary>
    public static class ClosingDamageMath
    {
        /// <summary>
        /// JSON verb_base.1.base_damage yoksa ölçek. Canlı yol katalogdan gelen değeri geçirir.
        /// </summary>
        public const float VerbDamageReference = SkillNumberFallbacks.VerbDamageReference;

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
            float outgoingDamageMult = 1f,
            float verbDamageReference = 0f)
        {
            if (totalEffect <= 0f || closingDamagePerEffect <= 0f)
                return 0f;

            float commit = totalEffect * closingDamagePerEffect;
            float outMult = outgoingDamageMult > 0f ? outgoingDamageMult : 1f;

            if (isBasicStrike || skill.IsEmpty)
                return commit * outMult;

            // Heal / dash / shield: can yemez; status yolu ayrı.
            if (skill.Combat.BaseDamage <= 0f)
                return 0f;

            float adj = skill.Scaling.DamageMult > 0f ? skill.Scaling.DamageMult : 1f;
            float reference = verbDamageReference > 0f ? verbDamageReference : VerbDamageReference;
            float verbScale = skill.Combat.BaseDamage / reference;
            return commit * verbScale * adj * outMult;
        }
    }
}
