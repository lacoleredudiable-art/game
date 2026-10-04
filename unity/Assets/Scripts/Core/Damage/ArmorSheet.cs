using System;

namespace Dovus.Core.Damage
{
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
            float f = Math.Min(DamageDefaults.ArmorRemoveCapFraction, fractionRemoved);
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
