namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Kural motoru v4 bayrağı. Varsayılan kapalı; eski MechanicGrammar / skill yolu değişmez.
    /// Oyun entegrasyonu sonraki dilimlerde bu bayrağa bağlanır.
    /// </summary>
    public static class RuleEngineV4Feature
    {
        public const string FlagName = "kural_motoru_v4";

        /// <summary>PR1: yalnız iskelet + test; canlı oyunda kapalı.</summary>
        public static bool Enabled { get; set; }
    }
}
