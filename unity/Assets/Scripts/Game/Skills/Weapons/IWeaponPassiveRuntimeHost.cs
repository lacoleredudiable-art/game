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
public interface IWeaponPassiveRuntimeHost
    {
        Transform Player { get; }
        AllyDummy Ally { get; }
        BossReactor Boss { get; }
        ActorStatus BossStatus { get; }
        KinematicMotor Motor { get; }
        MotionTemplateBody MotionBody { get; }
        PlayerTargeting Targeting { get; }
        GameClock Clock { get; }
        EquipmentItem EquippedWeapon { get; }
        WeaponCombatProfile EquippedProfile { get; }
        WeaponSwapState WeaponSwap { get; }
        SentenceEngine Engine { get; }
        ActorVisual Visual { get; }
        double WorldTimeMs { get; }
        double LastMovedMs { get; }
        bool PerformingAttack { get; }
        bool WeaponIgnoresArmor { get; set; }
        OrbAnchor Orb { get; }
        bool CasterRecoilSuppressed { get; set; }
        bool RecoilInTemplate { get; }
        float LastHitX { get; set; }
        float LastHitZ { get; set; }
        Vector3 FlatBodyForward();
        WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill);
        float PlayerBodyRadiusM();
        float BossBodyRadius();
        void GrantShortShield(float points, float durationSec);
        void ResetBasicChain();
        void EndMotionAnim();
        void AbortRecoveringSentence();
        void SetSwapInstantDrawUntil(double worldMs);
    }
}
