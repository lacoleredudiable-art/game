namespace Dovus.Core.Cancel
{
    public readonly struct CancelDecision
    {
        public CancelDecision(CancelVerdict verdict, CancelRule rule, bool hasRule)
        {
            Verdict = verdict;
            Rule = rule;
            HasRule = hasRule;
        }

        public CancelVerdict Verdict { get; }
        public CancelRule Rule { get; }

        /// <summary>false: tabloda eşleşen satır yok (boşta ya da bilinmeyen çift → reddedilir, saklanmaz).</summary>
        public bool HasRule { get; }

        public bool Allowed => Verdict != CancelVerdict.Deny;
        public int BufferMs => Verdict == CancelVerdict.Deny && HasRule ? Rule.BufferMs : 0;
        public bool FizzlesRunning => Verdict == CancelVerdict.Cancel && Rule.FizzlesRunning;
        public bool RefundsRunningCooldown => Verdict == CancelVerdict.Cancel && Rule.RefundsRunningCooldown;
    }
}
