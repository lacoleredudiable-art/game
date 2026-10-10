namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Azalma: hasar hariç üst üste uygulamalarda güç düşer.</summary>
    public static class RuleEngineV4DiminishStack
    {
        public static float ScaleNonDamage(int stackIndex, float value)
        {
            if (value <= 0f)
                return 0f;
            float mult = stackIndex switch
            {
                0 => 1f,
                1 => RuleEngineV4WorldPhysicsRuntime.Active.DiminishMultSecond,
                _ => RuleEngineV4WorldPhysicsRuntime.Active.DiminishMultThird,
            };
            return value * mult;
        }
    }
}
