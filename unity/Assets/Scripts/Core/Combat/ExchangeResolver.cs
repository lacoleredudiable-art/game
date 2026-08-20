using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    public enum ExchangeOutcome
    {
        Dodged,
        Hit,
        Safe
    }

    public enum HitReason
    {
        ErkenBastin,
        GecKaldin
    }

    public readonly struct ExchangeInput
    {
        public int TelegraphStartMs { get; init; }
        public int StrikeTimeMs { get; init; }
        public int? DodgePressMs { get; init; }
        public bool InEffectVolume { get; init; }
    }

    public readonly struct ExchangeResult
    {
        public ExchangeOutcome Outcome { get; init; }
        public DodgeGrade? Grade { get; init; }
        public int GapMs { get; init; }
        public int ReactionMs { get; init; }
        public HitReason? Reason { get; init; }

        public string? HitReasonText => Reason switch
        {
            HitReason.ErkenBastin => "erken bastın",
            HitReason.GecKaldin => "geç kaldın",
            _ => null
        };
    }

    /// <summary>
    /// Boss vuruş anında sıyırma/vurulma sonucunu çözer — dovus-sistemi.md §6.
    /// gap = vuruş anı − dodge basma anı; reaction = dodge basma − telegraf başlangıcı.
    /// </summary>
    public sealed class ExchangeResolver
    {
        readonly GradeTuning _grade;
        readonly DodgeTuning _dodge;

        public ExchangeResolver(CombatTuning? tuning = null)
        {
            tuning ??= new CombatTuning();
            _grade = tuning.Grade;
            _dodge = tuning.Dodge;
        }

        public ExchangeResolver(GradeTuning grade, DodgeTuning dodge)
        {
            _grade = grade;
            _dodge = dodge;
        }

        public ExchangeResult Resolve(ExchangeInput input)
        {
            if (!input.InEffectVolume)
            {
                return new ExchangeResult { Outcome = ExchangeOutcome.Safe };
            }

            if (!input.DodgePressMs.HasValue)
            {
                return new ExchangeResult
                {
                    Outcome = ExchangeOutcome.Hit,
                    Reason = HitReason.GecKaldin
                };
            }

            int press = input.DodgePressMs.Value;
            int iframeStart = press + _dodge.IframeStartMs;
            int iframeEnd = iframeStart + _dodge.IframeMs;

            if (input.StrikeTimeMs >= iframeStart && input.StrikeTimeMs < iframeEnd)
            {
                int gap = input.StrikeTimeMs - press;
                int reaction = press - input.TelegraphStartMs;
                return new ExchangeResult
                {
                    Outcome = ExchangeOutcome.Dodged,
                    Grade = GradeFromGap(gap),
                    GapMs = gap,
                    ReactionMs = reaction
                };
            }

            // Vuruş i-frame penceresinin ÖNCESİNDE kaldıysa parmak vuruştan sonra inmiştir:
            // sebep gecikmedir, dokunulmazlığın erken bitmesi değil.
            return new ExchangeResult
            {
                Outcome = ExchangeOutcome.Hit,
                Reason = input.StrikeTimeMs < iframeStart ? HitReason.GecKaldin : HitReason.ErkenBastin
            };
        }

        public DodgeGrade GradeFromGap(int gapMs)
        {
            if (gapMs <= _grade.MukemmelGapMaxMs)
                return DodgeGrade.Mukemmel;
            if (gapMs <= _grade.HarikaGapMaxMs)
                return DodgeGrade.Harika;
            if (gapMs <= _grade.TemizGapMaxMs)
                return DodgeGrade.Temiz;
            return DodgeGrade.Siyirdi;
        }

        public bool IsInvulnerableAtStrike(int dodgePressMs, int strikeTimeMs)
        {
            int iframeStart = dodgePressMs + _dodge.IframeStartMs;
            int iframeEnd = iframeStart + _dodge.IframeMs;
            return strikeTimeMs >= iframeStart && strikeTimeMs < iframeEnd;
        }
    }
}
