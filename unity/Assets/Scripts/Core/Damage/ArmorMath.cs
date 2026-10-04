using System;

namespace Dovus.Core.Damage
{
    /// <summary>
    /// Zırh delme ve LoL azaltması.
    /// armor ≥ 0: alınan = hasar × 100 / (100 + armor)
    /// armor &lt; 0: alınan = hasar × (2 − 100 / (100 − armor))
    /// </summary>
    public static class ArmorMath
    {
        public static float AfterPenetration(float armor, float flatPen, float percentPen)
        {
            float pct = percentPen;
            if (pct < 0f)
                pct = 0f;
            if (pct > 1f)
                pct = 1f;
            float flat = flatPen > 0f ? flatPen : 0f;
            return armor * (1f - pct) - flat;
        }

        public static float Mitigate(float damage, float armor)
        {
            if (damage <= 0f)
                return 0f;
            if (armor >= 0f)
                return damage * 100f / (100f + armor);
            float denom = 100f - armor;
            if (denom < 0.001f)
                denom = 0.001f;
            float factor = 2f - 100f / denom;
            if (factor < 0f)
                factor = 0f;
            return damage * factor;
        }
    }
}
