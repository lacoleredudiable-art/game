namespace Dovus.Core.Cancel
{
    /// <summary>
    /// Bir eylemin faz süreleri (ms). Süreler klipten/JSON'dan gelir; tablo yalnız kararı verir.
    /// <see cref="Token"/> çağıranın anlamıdır (ör. hangi skill); Core yorumlamaz.
    /// </summary>
    public readonly struct ActionPlan
    {
        public ActionPlan(ActionKind kind, int token, int windupMs, int activeMs, int recoveryMs)
        {
            Kind = kind;
            Token = token;
            WindupMs = windupMs < 0 ? 0 : windupMs;
            ActiveMs = activeMs < 0 ? 0 : activeMs;
            RecoveryMs = recoveryMs < 0 ? 0 : recoveryMs;
        }

        public ActionKind Kind { get; }
        public int Token { get; }
        public int WindupMs { get; }
        public int ActiveMs { get; }
        public int RecoveryMs { get; }
        public int TotalMs => WindupMs + ActiveMs + RecoveryMs;

        /// <summary>Başlangıçtan geçen süreye göre faz; süre dolduysa <see cref="ActionPhase.None"/>.</summary>
        public ActionPhase PhaseAt(int elapsedMs)
        {
            if (elapsedMs < 0 || elapsedMs >= TotalMs)
                return ActionPhase.None;
            if (elapsedMs < WindupMs)
                return ActionPhase.Windup;
            if (elapsedMs < WindupMs + ActiveMs)
                return ActionPhase.Active;
            return ActionPhase.Recovery;
        }
    }
}
