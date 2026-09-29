using System;

namespace Dovus.Core.Mechanic
{
    /// <summary>
    /// Emici aktarım: düşmana yazılan eksi can boss hasarı, kendine/dosta yazılan artı can iyileştirmedir.
    /// Miktar gramer planındadır (JSON base_heal × silah × sıfat). Pay, kalıp vuruşunun share'idir.
    /// </summary>
    public static class DrainNumbers
    {
        public static void Read(MechanicPlan plan, out float damage, out float heal)
        {
            damage = 0f;
            heal = 0f;
            if (plan == null)
                return;
            foreach (MechanicEffect effect in plan.Effects)
            {
                if (effect.Atom != "deger" || effect.Stat != "can")
                    continue;
                if (effect.Target == "dusman" && effect.Amount < 0)
                    damage += (float)(-effect.Amount);
                else if ((effect.Target == "kendin" || effect.Target == "dost") && effect.Amount > 0)
                    heal += (float)effect.Amount;
            }
        }

        public static bool TryShare(MechanicPlan plan, float share, out float damage, out float heal)
        {
            Read(plan, out float fullDamage, out float fullHeal);
            float part = Math.Clamp(share, 0f, 1f);
            damage = fullDamage * part;
            heal = fullHeal * part;
            return fullDamage > 0.01f;
        }
    }
}
