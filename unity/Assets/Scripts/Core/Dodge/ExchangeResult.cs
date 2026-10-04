using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Dodge
{
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
}
