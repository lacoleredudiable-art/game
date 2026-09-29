using System;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Tek hasar fonksiyonu. Sıra:
    /// (a) taban = skill gücü × saldırı gücü
    /// (b) kritik
    /// (c) ek yüzde buff / pasif / rün çarpanları (bir kez)
    /// (d) zırh delme, düz ve yüzde
    /// (e) zırh azaltması (LoL)
    /// (f) son azaltma (kalkan, alınan hasar −X%), çarpımlı, en fazla %90
    /// (g) ±%5 sapma (testte sabit atış)
    /// İyileştirme zırha girmez; yalnız iyileştirme buff'ı.
    /// Poise yanında taşınır, bu fonksiyon onu yeniden ölçeklemez.
    /// </summary>
    public static class DamagePipeline
    {
        public const float DefaultCritChance = 0.10f;
        public const float DefaultCritMultiplier = 1.5f;
        public const float VarianceHalf = 0.05f;
        public const float MinDamageTakenFactor = 0.10f;

        static readonly Random Shared = new Random();

        public static DamageOutcome Resolve(DamageQuery query)
        {
            if (query == null)
                return default;

            if (query.Heal)
                return ResolveHeal(query);

            if (query.Invulnerable || query.SkillPower <= 0f)
                return new DamageOutcome(0f, false, 0f, query.Poise, query.Armor, 0f, 0f, 1f);

            float attack = query.AttackPower > 0f ? query.AttackPower : 1f;
            float amount = query.SkillPower * attack;
            if (query.ScaleMagnitudes)
                amount = CombatScale.Magnitude(amount);

            bool crit = false;
            Random rng = query.VarianceSeed >= 0 ? new Random(query.VarianceSeed) : Shared;
            if (query.CanCrit && amount > 0f)
            {
                float chance = query.CritChance < 0f ? DefaultCritChance : query.CritChance;
                if (chance < 0f)
                    chance = 0f;
                if (chance > 1f)
                    chance = 1f;
                float roll = query.CritRoll01 ?? (float)rng.NextDouble();
                if (chance > 0f && roll < chance)
                {
                    float mult = query.CritMultiplier > 0f ? query.CritMultiplier : DefaultCritMultiplier;
                    amount *= mult;
                    crit = true;
                }
            }

            float bonus = 1f + query.AdditivePercent;
            if (bonus < 0f)
                bonus = 0f;
            float factors = query.Multiplier < 0f ? 0f : query.Multiplier;
            // Buff aşaması. LandMultiplier yalnız vuruşun gücü varken çağrılır
            // (ıskalama, yenilmezlik ve iyileştirme tüketmez).
            float land = 1f;
            if (query.LandMultiplier != null && amount > 0f)
            {
                land = query.LandMultiplier();
                if (land < 0f)
                    land = 0f;
            }
            amount *= bonus * factors * land;

            float preArmor = amount;
            float armor = ArmorMath.AfterPenetration(query.Armor, query.ArmorPenFlat, query.ArmorPenPercent);
            amount = ArmorMath.Mitigate(amount, armor);

            float taken = query.DamageTakenFactor;
            if (taken <= 0f)
                taken = 1f;
            if (taken < MinDamageTakenFactor)
                taken = MinDamageTakenFactor;
            amount *= taken;

            float shieldPool = query.Shield;
            if (shieldPool < 0f)
                shieldPool = 0f;
            if (query.ScaleMagnitudes)
                shieldPool = CombatScale.Magnitude(shieldPool);
            float absorbedScaled = amount > 0f ? Math.Min(shieldPool, amount) : 0f;
            amount -= absorbedScaled;
            float absorbedUnscaled = query.ScaleMagnitudes && CombatScale.DamageAndHp > 0f
                ? absorbedScaled / CombatScale.DamageAndHp
                : absorbedScaled;

            float variance = 1f;
            if (query.ApplyVariance && amount > 0f)
            {
                float roll = query.VarianceRoll01 ?? (float)rng.NextDouble();
                if (roll < 0f)
                    roll = 0f;
                if (roll > 1f)
                    roll = 1f;
                variance = 1f + (roll * (VarianceHalf * 2f) - VarianceHalf);
                amount *= variance;
            }

            if (amount < 0f)
                amount = 0f;

            float threatMult = query.ThreatMultiplier > 0f ? query.ThreatMultiplier : 1f;
            return new DamageOutcome(
                amount, crit, amount * threatMult, query.Poise, armor, absorbedUnscaled, preArmor, variance);
        }

        public static DamageOutcome ResolveHeal(DamageQuery query)
        {
            if (query == null || query.HealPower <= 0f)
                return default;

            float mult = query.HealMultiplier > 0f ? query.HealMultiplier : 1f;
            float amount = query.HealPower * mult;
            if (query.ScaleMagnitudes)
                amount = CombatScale.Magnitude(amount);
            if (amount < 0f)
                amount = 0f;
            float threatMult = query.ThreatMultiplier > 0f ? query.ThreatMultiplier : 1f;
            return new DamageOutcome(amount, false, amount * threatMult, 0f, 0f, 0f, amount, 1f);
        }
    }

    public sealed class DamageQuery
    {
        public float SkillPower;
        public float AttackPower = 1f;
        public float AdditivePercent;
        public float Multiplier = 1f;
        /// <summary>Buff aşamasında bir kez. Mükemmel sıyırmanın sonraki vuruşu buradan gelir.</summary>
        public Func<float>? LandMultiplier;
        public bool CanCrit;
        public float CritChance = DamagePipeline.DefaultCritChance;
        public float CritMultiplier = DamagePipeline.DefaultCritMultiplier;
        public float? CritRoll01;
        public float Armor;
        public float ArmorPenFlat;
        public float ArmorPenPercent;
        public float DamageTakenFactor = 1f;
        public float Shield;
        public bool Invulnerable;
        public bool ApplyVariance;
        public int VarianceSeed = -1;
        public float? VarianceRoll01;
        public float Poise;
        public float ThreatMultiplier = 1f;
        public bool ScaleMagnitudes;
        public bool Heal;
        public float HealPower;
        public float HealMultiplier = 1f;
    }

    public readonly struct DamageOutcome
    {
        public DamageOutcome(
            float amount,
            bool wasCrit,
            float threat,
            float poise,
            float armorAfterPen,
            float shieldAbsorbed,
            float preArmor,
            float variance)
        {
            Amount = amount;
            WasCrit = wasCrit;
            Threat = threat;
            Poise = poise;
            ArmorAfterPen = armorAfterPen;
            ShieldAbsorbed = shieldAbsorbed;
            PreArmor = preArmor;
            Variance = variance;
        }

        public float Amount { get; }
        public bool WasCrit { get; }
        public float Threat { get; }
        public float Poise { get; }
        public float ArmorAfterPen { get; }
        /// <summary>Kalkan havuzundan düşülecek miktar. Ölçek açıksa havuz birimi (küçük sayı).</summary>
        public float ShieldAbsorbed { get; }
        public float PreArmor { get; }
        public float Variance { get; }
    }
}
