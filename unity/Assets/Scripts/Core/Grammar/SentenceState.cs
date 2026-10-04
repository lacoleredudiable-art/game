using System.Collections.Generic;
using Dovus.Core.Input;
using Dovus.Core.Element;

namespace Dovus.Core.Grammar
{
    /// <summary>Motorun okunabilir anlık durumu.</summary>
    public sealed class SentenceState
    {
        public SentencePhase Phase { get; internal set; } = SentencePhase.Idle;
        public Rune? Verb { get; internal set; }
        public IReadOnlyList<SentenceWord> Words { get; internal set; } = System.Array.Empty<SentenceWord>();
        public double RemainingWindowMs { get; internal set; }

        /// <summary>Bu kelimeden sonra kurulan uzatma penceresinin tam süresi (dwell tavanı).</summary>
        public double ArmedWindowMs { get; internal set; }

        /// <summary>Toparlanma kilidinden kalan süre (§5). Kesilirse 0'a düşer.</summary>
        public double RemainingRecoveryMs { get; internal set; }

        public ClosingHit? LastClosing { get; internal set; }

        public int AdjectiveCount => Words.Count == 0 ? 0 : Words.Count - 1;
        public int DotCount => Words.Count;
    }
}
