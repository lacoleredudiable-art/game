namespace Dovus.Core.Boss
{
    /// <summary>
    /// B10: bölge başına hasar çarpanı verisi (KE-2.8 - KE-2.11, KE-8.1). Modeldeki collider'lar Unity'de
    /// <see cref="KulEjderiHitZone"/> kimliğiyle bağlanır; çarpan buradan okunur.
    /// </summary>
    public static class KulEjderiHitZones
    {
        /// <param name="staggered">KE-8.1 sendeleme: bütün bölgeler ×1,5; faz 2'de baş yere iner.</param>
        /// <param name="heartOpen">Kor Mührü açık (KE-7.8); yalnız faz 3'te anlamlı.</param>
        /// <param name="weaponRangeM">Vuran silahın menzili (faz 2 başı için, KE-2.8).</param>
        public static KulEjderiHitResult Resolve(
            KulEjderiHitZone zone,
            KulEjderiPhase phase,
            bool staggered,
            bool heartOpen,
            float weaponRangeM)
        {
            float baseMultiplier;
            switch (zone)
            {
                case KulEjderiHitZone.Claw:
                    baseMultiplier = KulEjderiDefaults.ClawMultiplier;
                    break;
                case KulEjderiHitZone.Head:
                    if (phase == KulEjderiPhase.Phase2 && !staggered
                        && weaponRangeM < KulEjderiDefaults.Phase2HeadMinWeaponRangeM)
                        return KulEjderiHitResult.Unreachable();
                    baseMultiplier = KulEjderiDefaults.HeadMultiplier;
                    break;
                case KulEjderiHitZone.Chest:
                    if (phase != KulEjderiPhase.Phase2)
                        return KulEjderiHitResult.Unreachable();
                    baseMultiplier = KulEjderiDefaults.ChestMultiplier;
                    break;
                case KulEjderiHitZone.Heart:
                    if (phase != KulEjderiPhase.Phase3)
                        return KulEjderiHitResult.Unreachable();
                    if (!heartOpen)
                        return KulEjderiHitResult.ClosedHeart();
                    baseMultiplier = KulEjderiDefaults.HeartOpenMultiplier;
                    break;
                default:
                    return KulEjderiHitResult.Unreachable();
            }

            if (staggered)
                baseMultiplier *= KulEjderiDefaults.StaggerDamageMultiplier;
            return KulEjderiHitResult.Hit(baseMultiplier);
        }
    }
}
