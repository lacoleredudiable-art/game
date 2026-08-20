using System.Collections.Generic;

namespace Dovus.Core.Grammar
{
    public enum SentencePhase
    {
        Idle,
        Building,
        Resolved,
        Aborted
    }

    /// <summary>Cümle içindeki bir kelime (fiil veya sıfat) + dwell yoğunluğu.</summary>
    public readonly struct SentenceWord
    {
        public SentenceWord(Rune rune, JumpKind jumpFromPrevious, int intensityStacks)
        {
            Rune = rune;
            JumpFromPrevious = jumpFromPrevious;
            IntensityStacks = intensityStacks;
        }

        public Rune Rune { get; }
        public int Dot => (int)Rune;
        public JumpKind JumpFromPrevious { get; }
        public int IntensityStacks { get; }
    }

    /// <summary>Başarılı kapanış — tür son rüne, ödül uzunluğa bağlı (§5).</summary>
    public readonly struct ClosingHit
    {
        public ClosingHit(Rune type, float totalEffect, int dotCount)
        {
            Type = type;
            TotalEffect = totalEffect;
            DotCount = dotCount;
        }

        public Rune Type { get; }
        public float TotalEffect { get; }
        public int DotCount { get; }
    }

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

    /// <summary>Motorun okunabilir anlık durumu.</summary>
    public sealed class SentenceState
    {
        public SentencePhase Phase { get; internal set; } = SentencePhase.Idle;
        public Rune? Verb { get; internal set; }
        public IReadOnlyList<SentenceWord> Words { get; internal set; } = System.Array.Empty<SentenceWord>();
        public double RemainingWindowMs { get; internal set; }
        public ClosingHit? LastClosing { get; internal set; }
        public int AdjectiveCount => Words.Count == 0 ? 0 : Words.Count - 1;
        public int DotCount => Words.Count;
    }
}
