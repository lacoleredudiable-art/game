using System;

namespace Dovus.Core.Cancel
{
    /// <summary>Zincirin bir kesişi: klip süresi ve vuruş karesi (O-4.14).</summary>
    public readonly struct StrikeStep
    {
        public StrikeStep(int durationMs, int hitMs)
        {
            int duration = durationMs < 1 ? 1 : durationMs;
            DurationMs = duration;
            HitMs = hitMs < 0 ? 0 : (hitMs > duration ? duration : hitMs);
        }

        public int DurationMs { get; }

        /// <summary>Vuruş karesi: klip başından bu kadar sonra. Öncesi hazırlık, sonrası toparlanma.</summary>
        public int HitMs { get; }
    }
}
