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
                1 => 0.5f,
                _ => 0.25f,
            };
            return value * mult;
        }
    }
}
