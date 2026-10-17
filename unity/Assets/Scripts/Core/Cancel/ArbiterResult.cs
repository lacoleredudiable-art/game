namespace Dovus.Core.Cancel
{
    public readonly struct ArbiterResult
    {
        public ArbiterResult(ArbiterOutcome outcome, ActionPlan plan, ActionKind cutKind, CancelDecision decision, bool fromBuffer)
        {
            Outcome = outcome;
            Plan = plan;
            CutKind = cutKind;
            Decision = decision;
            FromBuffer = fromBuffer;
        }

        public ArbiterOutcome Outcome { get; }

        /// <summary>Başlayan eylemin planı (yalnız Started/Cancelled).</summary>
        public ActionPlan Plan { get; }

        /// <summary>Kesilen eylemin türü (yalnız Cancelled).</summary>
        public ActionKind CutKind { get; }

        public CancelDecision Decision { get; }

        /// <summary>Başlangıç tampondan geldi (girdi daha önce verilmişti).</summary>
        public bool FromBuffer { get; }

        public bool DidStart => Outcome == ArbiterOutcome.Started || Outcome == ArbiterOutcome.Cancelled;
    }
}
