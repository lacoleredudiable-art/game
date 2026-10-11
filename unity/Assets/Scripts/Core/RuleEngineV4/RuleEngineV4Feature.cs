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

        /// <summary>Bayrak açıkken can/hasar kural-motoru-v4.json scale'inde; CombatScale çarpanı uygulanmaz.</summary>
        public static bool ScaleCombatMagnitudes => !Enabled;

        /// <summary>Bayrak açıkken cümle fiil + sıfat ile sınırlı; kapalıyken eski tavan.</summary>
        public static int SentenceDotCap(int legacyMax) =>
            Enabled ? RuleEngineV4CastContext.VerbAdjectiveComboLen : legacyMax;

        /// <summary>
        /// Bayrak açıkken tavana ulaşan cümle kendiliğinden çözülmez: fazla noktalar yok sayılır,
        /// merkez dokunuşu ya da pencere sonu onaylar, merkezde kalkan parmak iptal eder.
        /// </summary>
        public static bool HoldSentenceAtCapacity => Enabled;

        /// <summary>
        /// Bayrak açıkken çizim sürerken (en az bir nokta) parmak merkezde kalkarsa cümle iptal edilir.
        /// Merkeze ayrı dokunuş (çizim yok) bu yoldan geçmez; onaylamayı sürdürür.
        /// </summary>
        public static bool CenterReleaseAbortsSentence(bool releasedOnCenter, Grammar.SentenceState sentence) =>
            Enabled && releasedOnCenter && sentence != null
            && sentence.Phase == Grammar.SentencePhase.Building
            && sentence.Words != null && sentence.Words.Count >= 1;

        /// <summary>Bayrak açıkken çizim (drawing) hareketi kökleyip yavaşlatmaz.</summary>
        public static bool DrawingKeepsMovement => Enabled;
    }
}
