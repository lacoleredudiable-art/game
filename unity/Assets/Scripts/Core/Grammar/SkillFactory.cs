using System;
using System.Collections.Generic;
using Dovus.Core.Equipment;

namespace Dovus.Core.Grammar
{
    /// <summary>
    /// Binding sıra adım 3/5: tek SkillMotor'dan fiil×sıfat Skill üretir.
    /// Liste hardcode edilmez; 12×12 JSON satırı ve seçili build'de 6×6 türetilir.
    /// </summary>
    public sealed class SkillFactory
    {
        readonly SkillMotor _motor;
        readonly EquipmentBonusResolver _equipment;

        public SkillFactory(SkillMotor motor, EquipmentBonusResolver equipment)
        {
            _motor = motor ?? throw new ArgumentNullException(nameof(motor));
            _equipment = equipment ?? new EquipmentBonusResolver(string.Empty);
        }

        public Skill Create(
            int verbRuneId,
            int adjectiveRuneId,
            EquipmentItem weapon = null,
            int elementPaintId = 0)
        {
            SkillResolution resolution = _motor.Resolve(new[] { verbRuneId, adjectiveRuneId });
            if (resolution.IsEmpty || !resolution.IsComplete)
                throw new InvalidOperationException($"Skill çözülemedi: {verbRuneId}-{adjectiveRuneId}");

            WeaponSkillCompatibility compatibility = EvaluateWeapon(resolution, weapon);
            ElementPaintNode? paint = FindElement(elementPaintId);
            string displayName = resolution.DisplayName;
            if (paint.HasValue && !string.IsNullOrEmpty(paint.Value.NamePrefix))
                displayName = paint.Value.NamePrefix + " " + displayName;

            return new Skill(resolution, compatibility, paint, displayName);
        }

        public Skill CreateFromWords(
            IReadOnlyList<SentenceWord> words,
            EquipmentItem weapon = null,
            int elementPaintId = 0)
        {
            if (words == null || words.Count != 2)
                throw new ArgumentException("v6 skill tam iki rün olmalıdır.", nameof(words));
            return Create((int)words[0].Rune, (int)words[1].Rune, weapon, elementPaintId);
        }

        public IReadOnlyList<Skill> CreateForBuild(
            RuneLoadout loadout,
            EquipmentItem weapon = null,
            int elementPaintId = 0)
        {
            if (loadout == null)
                throw new ArgumentNullException(nameof(loadout));
            var result = new List<Skill>(RuneLoadout.SlotCount * RuneLoadout.SlotCount);
            for (int verbSlot = 1; verbSlot <= RuneLoadout.SlotCount; verbSlot++)
            for (int adjectiveSlot = 1; adjectiveSlot <= RuneLoadout.SlotCount; adjectiveSlot++)
            {
                result.Add(Create(
                    loadout.RuneIdAtSlot(verbSlot),
                    loadout.RuneIdAtSlot(adjectiveSlot),
                    weapon,
                    elementPaintId));
            }
            return result;
        }

        public WeaponSkillCompatibility EvaluateWeapon(
            in SkillResolution resolution,
            EquipmentItem weapon)
        {
            return int.TryParse(resolution.VerbId, out int verbId)
                ? _equipment.Resolve(weapon, verbId)
                : WeaponSkillCompatibility.Neutral;
        }

        ElementPaintNode? FindElement(int id)
        {
            if (id <= 0)
                return null;
            IReadOnlyList<ElementPaintNode> elements = _motor.ElementPaints;
            for (int i = 0; i < elements.Count; i++)
                if (elements[i].Id == id)
                    return elements[i];
            return null;
        }
    }

    public sealed class Skill
    {
        public Skill(
            SkillResolution resolution,
            WeaponSkillCompatibility weapon,
            ElementPaintNode? element,
            string displayName)
        {
            Resolution = resolution;
            Weapon = weapon;
            Element = element;
            DisplayName = displayName ?? resolution.DisplayName;
        }

        public SkillResolution Resolution { get; }
        public WeaponSkillCompatibility Weapon { get; }
        public ElementPaintNode? Element { get; }
        public string DisplayName { get; }
        public string Id => Resolution.SkillId;
        public float EffectiveDamageMult => Resolution.DamageMult * Weapon.DamageMult;
        public float EffectiveCastTimeMult => Weapon.CastTimeMult;
        public bool PassiveEnabled => Weapon.PassiveEnabled;
        public string UiColor => Weapon.UiColor;
    }
}
