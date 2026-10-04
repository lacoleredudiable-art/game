using System;
using Dovus.Core.Shared;

namespace Dovus.Core.Damage
{
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
}
