namespace Dovus.Core.Combat
{
    /// <summary>İ-frame'in başındaki kısa pencere. Vuruş bu aralıkta düşerse mükemmel sıyırma.</summary>
    public static class PerfectDodgeRule
    {
        public static bool InWindow(
            int dodgePressMs,
            int strikeMs,
            int iframeStartOffsetMs,
            int iframeMs,
            int perfectWindowMs)
        {
            if (iframeMs <= 0 || perfectWindowMs <= 0)
                return false;

            int start = dodgePressMs + iframeStartOffsetMs;
            int iframeEnd = start + iframeMs;
            int perfectEnd = start + perfectWindowMs;
            if (perfectEnd > iframeEnd)
                perfectEnd = iframeEnd;
            return strikeMs >= start && strikeMs < perfectEnd;
        }
    }

    /// <summary>
    /// Sonraki vuruş çarpanı. Hasar borusu bir kez <see cref="Consume"/> çağırır;
    /// ikinci okuma 1 döner.
    /// </summary>
    public sealed class NextHitBuff
    {
        public float Multiplier { get; private set; } = 1f;

        public bool IsArmed => Multiplier > 1.0001f;

        public void Arm(float multiplier)
        {
            if (multiplier < 1f)
                multiplier = 1f;
            Multiplier = multiplier;
        }

        public float Consume()
        {
            float value = Multiplier;
            Multiplier = 1f;
            return value;
        }
    }
}
