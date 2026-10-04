using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Core.Casting;
using Dovus.Core.Manifestation;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Casting;
using Dovus.Game.Skills.Weapons;
using Dovus.Core.Equipment;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
namespace Dovus.Game.Skills.Motion
{
public interface ITemplateDeliveryRuntimeHost
    {
        GameClockHost Clock { get; }
        EquipmentItem EquippedWeapon { get; }
        CombatTuning Combat { get; }
        MechanicGrammar MechanicEngine { get; }
        MechanicPlan LastMechanicPlan { get; }
        MotionTemplateDriver Motion { get; }
        Weapons.CannonBlast Cannon { get; }
        WeaponPassiveRuntime WeaponPassives { get; }
        Transform Player { get; }
        BossReactorController Boss { get; }
        BossVitals BossVitals { get; }
        ActorStatusHost PlayerStatus { get; }
        ActorStatusHost BossStatus { get; }
        KinematicMotorController Motor { get; }
        int SlotQueryCastId { get; set; }
        bool CasterRecoilSuppressed { get; set; }
        bool LastSkillEffectApplied { get; set; }
        WeaponPassiveMods HitMods(SkillResolution skill, bool isBasicStrike, bool consumeBonus);
        void TryLandWeaponStun(SkillResolution skill, bool isBasicStrike);
        MechanicPlan MechanicPlanFor(SkillResolution skill);
        bool TryLaunchSkillExecutor(
            SkillExecutorKind kind,
            PendingClosing pending,
            SkillResolution skill,
            SkillMotionPlan motion,
            float power,
            LivingEffect capturedLogic,
            int slotCastId,
            float? activationDelayOverride);
        float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slash,
            float power,
            float chain);
        void ApplyClosingHeal(ClosingHit closing, SkillResolution skill, float power, float chain);
        void ApplyClosingStatuses(PendingClosing pending, SkillResolution skill, bool bossReached);
        void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 fieldCenter, bool casterMoves);
        bool IsFriendlyFieldVerb(SkillResolution skill);
        bool IsHealSkill(SkillResolution skill);
        bool LandingFieldAllows(SkillResolution skill);
        bool TryBounceFriendly(float power);
        void JsonLog(string message);
    }
}
