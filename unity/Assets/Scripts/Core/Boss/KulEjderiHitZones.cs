using System;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// B10: bölge başına hasar çarpanı verisi (KE-2.8 - KE-2.10, KE-7.8, KE-8.1). Modeldeki collider'lar Unity'de
    /// <see cref="KulEjderiHitZone"/> kimliğiyle bağlanır; çarpan buradan okunur. Hangi silahın bölgeye değdiği burada
    /// değil, <see cref="KulEjderiHurtboxes"/>'ta ölçülür ([K] 17 Ekim: her silah her bölgeye vurur).
    /// </summary>
    public static class KulEjderiHitZones
    {
        /// <param name="staggered">KE-8.1 sendeleme: bütün bölgeler ×1,5.</param>
        /// <param name="collapsed">KE-7.8 Kor Mührü çökmesi: bütün bölgeler ×2,0.</param>
        public static KulEjderiHitResult Resolve(
            KulEjderiHitZone zone,
            KulEjderiPhase phase,
            bool staggered,
            bool collapsed)
        {
            if (staggered && collapsed)
                throw new InvalidOperationException(
                    "[AÇIK] KE-7.8: çökme sırasında sendeleme çarpanı (×1,5 ile ×2,0 çarpılır mı, büyüğü mü geçer) karar verilmedi.");

            float baseMultiplier;
            switch (zone)
            {
                case KulEjderiHitZone.Claw:
                    baseMultiplier = KulEjderiDefaults.ClawMultiplier;
                    break;
                case KulEjderiHitZone.Head:
                    baseMultiplier = KulEjderiDefaults.HeadMultiplier;
                    break;
                case KulEjderiHitZone.Chest:
                    if (phase != KulEjderiPhase.Phase2)
                        return KulEjderiHitResult.Unreachable();
                    baseMultiplier = KulEjderiDefaults.ChestMultiplier;
                    break;
                default:
                    return KulEjderiHitResult.Unreachable();
            }

            if (staggered)
                baseMultiplier *= KulEjderiDefaults.StaggerDamageMultiplier;
            if (collapsed)
                baseMultiplier *= KulEjderiDefaults.CollapseDamageMultiplier;
            return KulEjderiHitResult.Hit(baseMultiplier);
        }
    }
}
