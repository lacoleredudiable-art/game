using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Skills.State;
using Dovus.Game.Casting;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Skills.Weapons
{
    /// <summary>Silah pasifleri — durum CastSessionState; davranış WeaponServicesHost üzerinden.</summary>
    public interface IWeaponPassiveRuntimeHost
    {
        CastSessionState Cast { get; }
        Transform Player { get; }
        AllyDummyController Ally { get; }
        BossReactorController Boss { get; }
        ActorStatusHost BossStatus { get; }
        KinematicMotorController Motor { get; }
        MotionTemplateBodyHost MotionBody { get; }
        PlayerTargetingController Targeting { get; }
        GameClockHost Clock { get; }
        EquipmentItem EquippedWeapon { get; }
        WeaponCombatProfile EquippedProfile { get; }
        WeaponSwapState WeaponSwap { get; }
        SentenceEngine Engine { get; }
        ActorView Visual { get; }
        OrbAnchor Orb { get; }
    }
}
