namespace Dovus.Core.Dodge
{
    /// <summary>
    /// Sonraki vuruş çarpanı. Hasar borusu bir kez <see cref="Consume(double)"/> çağırır;
    /// ikinci okuma 1 döner. S1: süreli kurulur (<see cref="Arm(float,double,double)"/>); süre dolunca
    /// çarpan sessizce düşer, dakikalar sonraki vuruşa taşınmaz.
    /// </summary>
    public sealed class NextHitBuff
    {
        public float Multiplier { get; private set; } = 1f;
        public double ExpiresAtMs { get; private set; } = double.PositiveInfinity;

        public bool IsArmed => Multiplier > DodgeDefaults.ArmedMultThreshold;

        public bool IsArmedAt(double nowMs) => IsArmed && nowMs < ExpiresAtMs;

        /// <summary>Süresiz kurulum (eski çağıranlar ve testler).</summary>
        public void Arm(float multiplier) => Arm(multiplier, 0.0, double.PositiveInfinity);

        public void Arm(float multiplier, double nowMs, double windowMs)
        {
            if (multiplier < 1f)
                multiplier = 1f;
            Multiplier = multiplier;
            ExpiresAtMs = windowMs > 0.0 && !double.IsPositiveInfinity(windowMs)
                ? nowMs + windowMs
                : double.PositiveInfinity;
        }

        /// <summary>Süreye bakmadan tüketir.</summary>
        public float Consume() => Consume(double.NegativeInfinity);

        /// <summary>Süresi dolmuşsa 1 döner (ve kurulumu siler).</summary>
        public float Consume(double nowMs)
        {
            float value = nowMs < ExpiresAtMs ? Multiplier : 1f;
            Multiplier = 1f;
            ExpiresAtMs = double.PositiveInfinity;
            return value;
        }
    }
}
