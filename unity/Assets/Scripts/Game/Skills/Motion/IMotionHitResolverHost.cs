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
using Dovus.Game.Platform;
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
public interface IMotionHitResolverHost
    {
        MotionTemplateDriver Motion { get; }
        TemplateDeliveryRuntime TemplateDelivery { get; }
        GameClockHost Clock { get; }
        Transform Player { get; }
        BossReactorController Boss { get; }
        ActorStatusHost BossStatus { get; }
        BossVitals BossVitals { get; }
        MechanicPlan LastMechanicPlan { get; }
        EquipmentItem EquippedWeapon { get; }
        VerbExecutionData VerbData { get; }
        SlotPassiveDirector SlotPassives { get; }
        int SlotQueryCastId { get; set; }
        bool AoeReachedMotionHit(in MotionHit hit);
        bool TryVerbHitbox(in SkillResolution skill, out VerbHitboxSpec spec);
        int EquippedWeaponNumber();
        float PlayerBodyRadiusM();
        float FlatDistance(Vector3 a, Vector3 b);
        float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slash,
            float power,
            float chain);
        void ApplyClosingHeal(ClosingHit closing, SkillResolution skill, float power, float chain);
        void ApplyClosingHealAmount(SkillResolution skill, int amount, Vector3? fieldCenter, float fieldRadiusM);
        void ApplyClosingStatuses(PendingClosing pending, SkillResolution skill, bool bossReached);
        void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 origin);
        bool IsFriendlyFieldVerb(SkillResolution skill);
        bool IsHealSkill(SkillResolution skill);
    }
}
