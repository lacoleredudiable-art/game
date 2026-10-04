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
using Dovus.Core.Manifestation;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Weapons;
using UnityEngine;
namespace Dovus.Game.Skills.Weapons
{
public interface IOrbControllerHost
    {
        Transform Player { get; }
        PlayerTargetingController Targeting { get; }
        BossReactorController Boss { get; }
        GameClockHost Clock { get; }
        WeaponCombatProfile EquippedProfile { get; }
        Vector3 FlatBodyForward();
    }
}
