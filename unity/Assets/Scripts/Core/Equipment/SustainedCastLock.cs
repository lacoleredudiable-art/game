using System;

using Dovus.Core.Shared;
namespace Dovus.Core.Equipment
{
    /// <summary>
    /// O10 (denetim B): kanallı / basılı tutulan skill sürerken silah değiştirme kilidi.
    /// Süre = skilin channel_sec'i ya da oynayan kalıbın uzunluğu (hangisi büyükse). Kaçış iptali temizler.
    /// </summary>
    public sealed class SustainedCastLock
    {
        double _untilMs = -1;

        public double UntilMs => _untilMs;

        public void Begin(double nowMs, double durationSec)
        {
            if (durationSec <= 0)
                return;
            _untilMs = Math.Max(_untilMs, nowMs + durationSec * Units.SecToMs);
        }

        public bool Active(double nowMs) => nowMs < _untilMs;

        public void Clear() => _untilMs = -1;
    }
}
