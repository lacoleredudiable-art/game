using System.Collections.Generic;
using Dovus.Core.Input;
using Dovus.Core.Element;

namespace Dovus.Core.Grammar
{
    /// <summary>Çözülmüş veya iptal edilmiş cümle kaydı.</summary>
    public sealed class CompletedSentence
    {
        public CompletedSentence(
            Rune verb,
            IReadOnlyList<SentenceWord> words,
            SentencePhase phase,
            ClosingHit? closing)
        {
            Verb = verb;
            Words = words;
            Phase = phase;
            Closing = closing;
        }

        public Rune Verb { get; }
        public IReadOnlyList<SentenceWord> Words { get; }
        public SentencePhase Phase { get; }
        public ClosingHit? Closing { get; }
        public bool PaidReward => Closing.HasValue;
    }
}
