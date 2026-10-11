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
        /// Bayrak açıkken tavanda (2. nokta) kombo o an çözülür; aynı çizgide sonraki noktalar
        /// yeni cümle başlatmaz, yok sayılır. Yeni çizgi yeni kombo başlatır.
        /// </summary>
        public static bool IgnoreDotsAfterCapInStroke => Enabled;

        /// <summary>
        /// Bayrak açıkken çözülen kombo bekleme (toparlanma + vuruş sessizliği) olmadan o an fırlatılır;
        /// hazırlık süresi v4 planının kendi prefire'ıdır.
        /// </summary>
        public static bool LaunchOnResolve => Enabled;

        /// <summary>
        /// Bayrak açıkken yalnız tek noktalı (fiil) cümle, parmak merkezde kalkınca iptal edilir.
        /// 2. noktada kombo zaten çözülmüştür; sonrasında iptal yok. Merkeze ayrı dokunuş onaylar.
        /// </summary>
        public static bool CenterReleaseAbortsSentence(bool releasedOnCenter, Grammar.SentenceState sentence) =>
            Enabled && releasedOnCenter && sentence != null
            && sentence.Phase == Grammar.SentencePhase.Building
            && sentence.Words != null && sentence.Words.Count == 1;

        /// <summary>Bayrak açıkken çizim (drawing) hareketi kökleyip yavaşlatmaz.</summary>
        public static bool DrawingKeepsMovement => Enabled;
    }
}
