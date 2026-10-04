namespace Dovus.Core.Dodge
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
}
