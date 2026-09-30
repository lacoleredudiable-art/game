using System;

namespace Dovus.Core.Combat
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

    /// <summary>
    /// Zırh kaynakları sınıf değil: silah tabanı, pasif, geçici buff, yüzde kırılma.
    /// </summary>
    public sealed class ArmorSheet
    {
        public float Base;
        public float Passive;
        public float Buff;
        public double BuffUntilMs;
        public float ShredFraction;
        public double ShredUntilMs;

        public void ApplyShred(float fractionRemoved, double nowMs, double untilMs)
        {
            if (fractionRemoved <= 0f)
                return;
            float f = Math.Min(0.95f, fractionRemoved);
            if (nowMs > ShredUntilMs)
                ShredFraction = 0f;
            if (f >= ShredFraction || untilMs >= ShredUntilMs)
                ShredFraction = Math.Max(ShredFraction, f);
            if (untilMs > ShredUntilMs)
                ShredUntilMs = untilMs;
        }

        public void GrantBuff(float flat, double untilMs)
        {
            if (flat == 0f)
                return;
            Buff = flat;
            BuffUntilMs = untilMs;
        }

        public float Effective(double nowMs)
        {
            float armor = Base + Passive;
            if (nowMs < BuffUntilMs)
                armor += Buff;
            if (nowMs < ShredUntilMs && ShredFraction > 0f)
                armor *= 1f - ShredFraction;
            return armor;
        }
    }
}
