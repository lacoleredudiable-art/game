using System;
using System.Globalization;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// equipment_system.rules.element_match_bonus — silah elementi cast edilen skill
    /// elementiyle eşleşirse çarpan; aksi halde 1. Yüzde JSON metninden türetilir
    /// (SkillMotor.ParseDefenseDropMult sayı-çıkarma deseni); elle "10" yazılmaz.
    /// </summary>
    public sealed class EquipmentBonusResolver
    {
        readonly float _matchMult;
        readonly bool _usesVerbCompatibility;
        readonly float _compatibleDamageMult = 1f;
        readonly float _compatibleCastTimeMult = 1f;
        readonly float _incompatibleDamageMult = 1f;
        readonly float _incompatibleCastTimeMult = 1f;
        readonly string _compatibleUiColor = string.Empty;
        readonly string _incompatibleUiColor = string.Empty;
        readonly string _compatibleUiLabel = string.Empty;
        readonly string _incompatibleUiLabel = string.Empty;

        public EquipmentBonusResolver(string elementMatchBonusText)
        {
            _matchMult = ParseMatchBonusMult(elementMatchBonusText);
        }

        public EquipmentBonusResolver(EquipmentCatalog catalog)
            : this(catalog?.ElementMatchBonusText ?? string.Empty)
        {
            if (catalog == null || !catalog.IsV61)
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

        /// <summary>"+%10 etki" → 1.1f. Yüzde yoksa 1f.</summary>
        public float MatchBonusMult => _matchMult;

        /// <summary>
        /// Silah elementi skill elementiyle aynıysa match çarpanı, değilse 1.
        /// Oyuncu şu an sabit tek ekipman varsayılır — seçim UI'ı yok.
        /// </summary>
        public float Resolve(string weaponElement, string skillElement)
        {
            if (string.IsNullOrEmpty(weaponElement) || string.IsNullOrEmpty(skillElement))
                return 1f;
            if (!string.Equals(weaponElement, skillElement, StringComparison.Ordinal))
                return 1f;
            return _matchMult;
        }

        public float Resolve(EquipmentItem? weapon, string skillElement)
        {
            if (weapon == null || weapon.Slot != EquipmentSlot.Weapon)
                return 1f;
            return Resolve(weapon.Element, skillElement);
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

        /// <summary>"+%10 etki" / "savunma %50 düşer" tarzı metinden ilk yüzde → 1+N/100.</summary>
        public static float ParseMatchBonusMult(string bonusText)
        {
            if (string.IsNullOrEmpty(bonusText))
                return 1f;

            int i = 0;
            while (i < bonusText.Length && !char.IsDigit(bonusText[i])) i++;
            int start = i;
            while (i < bonusText.Length && char.IsDigit(bonusText[i])) i++;
            if (i == start)
                return 1f;

            int percent = int.Parse(
                bonusText.Substring(start, i - start),
                CultureInfo.InvariantCulture);
            return 1f + percent / 100f;
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
