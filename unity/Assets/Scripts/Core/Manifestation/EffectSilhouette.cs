namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// Yaşayan etkinin şekil eksenleri. Sayı çarpanı değil — gözle okunan silüet (T3).
    /// </summary>
    public readonly struct EffectSilhouette
    {
        public EffectSilhouette(float focus, float pierce, float spread, float lift)
        {
            Focus = focus;
            Pierce = pierce;
            Spread = spread;
            Lift = lift;
        }

        /// <summary>0 = halka/yay, 1 = tek hat.</summary>
        public float Focus { get; }

        /// <summary>İğne keskinliği / derinlik hissi.</summary>
        public float Pierce { get; }

        /// <summary>Sürü yoğunluğu (çoklu gövde).</summary>
        public float Spread { get; }

        /// <summary>Kinetik kaldırma.</summary>
        public float Lift { get; }

        public static EffectSilhouette Lerp(EffectSilhouette a, EffectSilhouette b, float t)
        {
            if (t <= 0f) return a;
            if (t >= 1f) return b;
            return new EffectSilhouette(
                a.Focus + (b.Focus - a.Focus) * t,
                a.Pierce + (b.Pierce - a.Pierce) * t,
                a.Spread + (b.Spread - a.Spread) * t,
                a.Lift + (b.Lift - a.Lift) * t);
        }

        public EffectSilhouette Clamped()
        {
            return new EffectSilhouette(
                Clamp01(Focus),
                Clamp01(Pierce),
                Clamp01(Spread),
                Clamp01(Lift));
        }

        static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
