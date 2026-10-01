namespace Dovus.Core.Equipment
{
    /// <summary>
    /// v6.1.1 silah × fiil uyumu (weapons[].compatible_verbs + uyumsuz_cizim çarpanları).
    /// Katalogsuz kurulursa nötrdür (her fiil uyumlu, yalnız silahın kendi çarpanları).
    /// v5 element_match_bonus yolu CLEANUP-2b ile kaldırıldı.
    /// </summary>
    public sealed class EquipmentBonusResolver
    {
        readonly bool _usesVerbCompatibility;
        readonly float _compatibleDamageMult = 1f;
        readonly float _compatibleCastTimeMult = 1f;
        readonly float _incompatibleDamageMult = 1f;
        readonly float _incompatibleCastTimeMult = 1f;
        readonly string _compatibleUiColor = string.Empty;
        readonly string _incompatibleUiColor = string.Empty;
        readonly string _compatibleUiLabel = string.Empty;
        readonly string _incompatibleUiLabel = string.Empty;

        /// <summary>Katalogsuz nötr çözücü (JSON yüklenemediğinde).</summary>
        public EquipmentBonusResolver()
        {
        }

        public EquipmentBonusResolver(EquipmentCatalog catalog)
        {
            if (catalog == null)
                return;
            _usesVerbCompatibility = true;
            _compatibleDamageMult = catalog.CompatibleDamageMult;
            _compatibleCastTimeMult = catalog.CompatibleCastTimeMult;
            _incompatibleDamageMult = catalog.IncompatibleDamageMult;
            _incompatibleCastTimeMult = catalog.IncompatibleCastTimeMult;
            _compatibleUiColor = catalog.CompatibleUiColor;
            _incompatibleUiColor = catalog.IncompatibleUiColor;
            _compatibleUiLabel = catalog.CompatibleUiLabel;
            _incompatibleUiLabel = catalog.IncompatibleUiLabel;
        }

        /// <summary>
        /// v6.1.1: silah kendi çarpanlarını her fiile uygular; compatible_verbs dışında
        /// uyumsuz_cizim cezası eklenir. Sıfat bu karara katılmaz.
        /// </summary>
        public WeaponSkillCompatibility Resolve(EquipmentItem? weapon, int verbId)
        {
            if (weapon == null || weapon.Slot != EquipmentSlot.Weapon)
                return WeaponSkillCompatibility.Neutral;

            bool compatible = !_usesVerbCompatibility || weapon.IsCompatibleWithVerb(verbId);
            float ruleDamage = compatible ? _compatibleDamageMult : _incompatibleDamageMult;
            float ruleCast = compatible ? _compatibleCastTimeMult : _incompatibleCastTimeMult;
            return new WeaponSkillCompatibility(
                compatible,
                weapon.DamageMult * ruleDamage,
                weapon.CastTimeMult * ruleCast,
                weapon.PoiseMult,
                compatible,
                compatible ? _compatibleUiColor : _incompatibleUiColor,
                compatible ? _compatibleUiLabel : _incompatibleUiLabel);
        }
    }

    public readonly struct WeaponSkillCompatibility
    {
        public WeaponSkillCompatibility(
            bool compatible,
            float damageMult,
            float castTimeMult,
            float poiseMult,
            bool passiveEnabled,
            string uiColor,
            string uiLabel)
        {
            Compatible = compatible;
            DamageMult = damageMult > 0f ? damageMult : 1f;
            CastTimeMult = castTimeMult > 0f ? castTimeMult : 1f;
            PoiseMult = poiseMult > 0f ? poiseMult : 1f;
            PassiveEnabled = passiveEnabled;
            UiColor = uiColor ?? string.Empty;
            UiLabel = uiLabel ?? string.Empty;
        }

        public static WeaponSkillCompatibility Neutral { get; } =
            new WeaponSkillCompatibility(true, 1f, 1f, 1f, true, string.Empty, string.Empty);

        public bool Compatible { get; }
        public float DamageMult { get; }
        public float CastTimeMult { get; }
        public float PoiseMult { get; }
        public bool PassiveEnabled { get; }
        public string UiColor { get; }
        public string UiLabel { get; }
    }
}
