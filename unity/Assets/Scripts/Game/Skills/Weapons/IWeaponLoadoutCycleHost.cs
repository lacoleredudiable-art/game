using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Weapons
{
    public interface IWeaponLoadoutCycleHost
    {
        EquipmentItem EquippedWeapon { get; set; }
        Skill LastFactorySkill { get; set; }
        SkillMotor Skills { get; }
        EquipmentBonusResolver EquipmentBonus { get; }
        SkillFactory SkillFactory { get; }
        ReactionReadout Readout { get; }
        WeaponSwapState WeaponSwap { get; }

        void RefreshDefenderArmor();
        void SyncVisualDelivery();
        void RaiseElementPaintChanged(ElementPaintNode paint);
    }
}
