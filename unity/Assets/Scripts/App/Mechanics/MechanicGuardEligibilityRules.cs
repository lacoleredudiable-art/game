namespace Dovus.App.Mechanics
{
    /// <summary>MechanicWorldRuntime.TickGuardTriggers — tetik penceresi bitti mi, ally/oyuncu düşük mü.</summary>
    public static class MechanicGuardEligibilityRules
    {
        public static bool ShouldFire(bool expired, bool allyLow, bool playerLow) =>
            !expired && (allyLow || playerLow);

        public static bool AllyLow(float ratio, double threshold) => ratio <= threshold;

        public static bool PlayerLow(float hp, float maxHp, double threshold) =>
            maxHp > 0f && hp / maxHp <= threshold;
    }
}
