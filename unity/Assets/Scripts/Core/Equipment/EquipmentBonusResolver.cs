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

        public EquipmentBonusResolver(string elementMatchBonusText)
        {
            _matchMult = ParseMatchBonusMult(elementMatchBonusText);
        }

        public EquipmentBonusResolver(EquipmentCatalog catalog)
            : this(catalog?.ElementMatchBonusText ?? string.Empty)
        {
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
}
