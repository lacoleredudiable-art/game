namespace Dovus.App.Casting
{
    public static class SelfDamageBuffRules
    {
        public static float DamageMultiplier(double worldTimeMs, double buffUntilMs, float buffMagnitude)
        {
            if (worldTimeMs >= buffUntilMs)
                return 1f;
            return 1f + buffMagnitude;
        }
    }
}
