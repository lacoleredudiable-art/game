using Dovus.Core.Grammar;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Katman 1 girdisi: hedef seçimi ve menzil (fizik-komutlari.md §1.2).</summary>
    public readonly struct RuleEngineV4CastContext
    {
        /// <summary>Fiil + sıfat: iki rünlük kombo.</summary>
        public const int VerbAdjectiveComboLen = 2;

        public RuleEngineV4CastContext(
            bool hasValidTarget,
            bool targetInWeaponRange,
            bool manualTargetSelected = false,
            bool hasUsableMark = false,
            int comboLength = VerbAdjectiveComboLen)
        {
            HasValidTarget = hasValidTarget;
            TargetInWeaponRange = targetInWeaponRange;
            ManualTargetSelected = manualTargetSelected;
            HasUsableMark = hasUsableMark;
            ComboLength = comboLength;
        }

        /// <summary>Otomatik veya manuel geçerli hedef var.</summary>
        public bool HasValidTarget { get; }

        /// <summary>Hedef silah menzili içinde (dost etkisinde min(silah, 6 m) katman 1'de hesaplanır).</summary>
        public bool TargetInWeaponRange { get; }

        /// <summary>Basılı tutup seçim yapıldı (Odaklı ihtiyaç seçimini geçersiz kılmaz).</summary>
        public bool ManualTargetSelected { get; }

        /// <summary>25 m içinde kullanılabilir takım işareti (İşaretli, fiil ≠ Arındırma).</summary>
        public bool HasUsableMark { get; }

        /// <summary>Çizilen kombonun rün (nokta) sayısı; fiilin min_combo_len'iyle kıyaslanır.</summary>
        public int ComboLength { get; }

        /// <summary>Dilim testleri: geçerli hedef, menzilde, işaretli kombolar için işaret var.</summary>
        public static RuleEngineV4CastContext SliceDefault(int adjectiveRune) =>
            new(
                hasValidTarget: true,
                targetInWeaponRange: true,
                hasUsableMark: adjectiveRune == 9);

        /// <summary>Fiil+sıfat tamamlanmadıysa (dotCount &lt; <see cref="VerbAdjectiveComboLen"/>) merkezde kalkış iptal eder.</summary>
        public static bool ShouldAbortSentenceOnCenterRelease(int dotCount) =>
            dotCount < VerbAdjectiveComboLen;

        public static bool ShouldAbortSentenceOnCenterRelease(bool releasedOnCenter, SentenceState sentence) =>
            releasedOnCenter
            && sentence != null
            && sentence.Phase == SentencePhase.Building
            && sentence.Words != null
            && ShouldAbortSentenceOnCenterRelease(sentence.Words.Count);
    }
}
