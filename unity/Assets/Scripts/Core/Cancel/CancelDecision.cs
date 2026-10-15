namespace Dovus.Core.Cancel
{
    public enum CancelVerdict : byte
    {
        /// <summary>Çalışan eylem yok; gelen eylem doğrudan başlar (başka kapılar — mana, CD, CC — başka yerde).</summary>
        Start = 0,
        /// <summary>Gelen eylem çalışanı keser ve başlar.</summary>
        Cancel = 1,
        /// <summary>Reddedildi; <see cref="CancelDecision.BufferMs"/> > 0 ise saklanır.</summary>
        Deny = 2,
    }

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
