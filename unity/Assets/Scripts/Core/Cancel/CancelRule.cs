namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Tek satır: "<see cref="Running"/> eylemi <see cref="Phases"/> fazındayken <see cref="Incoming"/> gelirse ne olur".
    /// </summary>
    public readonly struct CancelRule
    {
        public CancelRule(
            string id,
            ActionKind running,
            ActionPhase phases,
            ActionKind incoming,
            bool cancels,
            int bufferMs,
            bool fizzlesRunning,
            bool refundsRunningCooldown,
            RuleStatus status,
            string source)
        {
            Id = id;
            Running = running;
            Phases = phases;
            Incoming = incoming;
            Cancels = cancels;
            BufferMs = bufferMs < 0 ? 0 : bufferMs;
            FizzlesRunning = fizzlesRunning;
            RefundsRunningCooldown = refundsRunningCooldown;
            Status = status;
            Source = source;
        }

        public string Id { get; }
        public ActionKind Running { get; }
        public ActionPhase Phases { get; }
        public ActionKind Incoming { get; }

        /// <summary>true: gelen eylem çalışanı keser ve başlar. false: reddedilir.</summary>
        public bool Cancels { get; }

        /// <summary>Reddedilen girdinin saklanma süresi (ms); 0 = saklanmaz. Yalnız <see cref="Cancels"/> false iken anlamlı.</summary>
        public int BufferMs { get; }

        /// <summary>Kesilen eylemin etkisi hiç doğmaz (hazırlıkta kesilen skill atılmaz).</summary>
        public bool FizzlesRunning { get; }

        /// <summary>Kesilen skill'in bekleme süresi geri verilir (13 §9: hazırlık kesilirse hak yanar, CD yanmaz).</summary>
        public bool RefundsRunningCooldown { get; }

        public RuleStatus Status { get; }

        /// <summary>Belge kimliği (ör. "13 §9", "O-3.5") — kural neden böyle.</summary>
        public string Source { get; }

        public bool Matches(ActionKind running, ActionPhase phase, ActionKind incoming) =>
            Running == running && Incoming == incoming && (Phases & phase) != 0;
    }
}
