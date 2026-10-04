using System;
using System.Collections.Generic;
using Dovus.Core.Element;
using Dovus.Core.Equipment;

namespace Dovus.Core.Grammar
{
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
        public bool PassiveEnabled => Weapon.PassiveEnabled;
        public string UiColor => Weapon.UiColor;
    }
}
