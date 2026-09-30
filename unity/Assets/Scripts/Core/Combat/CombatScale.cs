namespace Dovus.Core.Combat
{
    /// <summary>
    /// Hasar, can, iyileştirme, kalkan ve DoT büyüklüğü tek katsayıyla büyür.
    /// JSON sayıları yerinde kalır; oyun bu katsayıyı veri okunurken uygular.
    /// Normal vuruş ~20-30K, güçlü skill 100K+ olsun diye. docs/durum.md
    /// </summary>
    public static class CombatScale
    {
        public const float DamageAndHp = 4000f;

        public static float Magnitude(float raw)
        {
            if (raw == 0f)
                return 0f;
            return raw * DamageAndHp;
        }

        public static int MagnitudeInt(float raw)
        {
            float scaled = Magnitude(raw);
            if (scaled >= int.MaxValue)
                return int.MaxValue;
            if (scaled <= int.MinValue)
                return int.MinValue;
            return (int)System.Math.Round(scaled);
        }
    }
}
