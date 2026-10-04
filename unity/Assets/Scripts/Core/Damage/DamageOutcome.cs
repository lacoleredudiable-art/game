using System;
using Dovus.Core.Shared;

namespace Dovus.Core.Damage
{
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
