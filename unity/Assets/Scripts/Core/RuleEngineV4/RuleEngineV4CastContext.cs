namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Katman 1 girdisi: hedef seçimi ve menzil (fizik-komutlari.md §1.2).</summary>
    public readonly struct RuleEngineV4CastContext
    {
        public RuleEngineV4CastContext(
            bool hasValidTarget,
            bool targetInWeaponRange,
            bool manualTargetSelected = false,
            bool hasUsableMark = false)
        {
            HasValidTarget = hasValidTarget;
            TargetInWeaponRange = targetInWeaponRange;
            ManualTargetSelected = manualTargetSelected;
            HasUsableMark = hasUsableMark;
        }

        /// <summary>Otomatik veya manuel geçerli hedef var.</summary>
        public bool HasValidTarget { get; }

        /// <summary>Hedef silah menzili içinde (dost etkisinde min(silah, 6 m) katman 1'de hesaplanır).</summary>
        public bool TargetInWeaponRange { get; }

        /// <summary>Basılı tutup seçim yapıldı; Odaklı otomatik ölçüsü kapanır.</summary>
        public bool ManualTargetSelected { get; }

        /// <summary>25 m içinde kullanılabilir takım işareti (İşaretli, fiil ≠ Arındırma).</summary>
        public bool HasUsableMark { get; }

        /// <summary>Dilim testleri: geçerli hedef, menzilde, işaretli kombolar için işaret var.</summary>
        public static RuleEngineV4CastContext SliceDefault(int adjectiveRune) =>
            new(
                hasValidTarget: true,
                targetInWeaponRange: true,
                hasUsableMark: adjectiveRune == 9);
    }
}
