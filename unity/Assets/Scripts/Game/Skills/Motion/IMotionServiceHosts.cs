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
    public interface IMotionTemplateDriverHost
    {
        Transform Player { get; }
        AllyDummy Ally { get; }
        BossReactor Boss { get; }
        GameClock Clock { get; }
        GameTuning Colors { get; }
        EquipmentItem EquippedWeapon { get; }
        MotionTemplateBody MotionBody { get; }
        PlayerTargeting Targeting { get; }
        HexagonInput Input { get; }
        Transform DirectorTransform { get; }
        float ClosingChainBonus { get; }
        int SlotQueryCastId { get; set; }
        MotionHitResolver MotionHitResolver { get; }
        MechanicPlan MechanicPlanFor(SkillResolution skill);
        void PullBossToPlayerContact();
        bool IsEnemyBody(Transform body);
        float PlayerBodyRadiusM();
        float FlatDistance(Vector3 a, Vector3 b);
        WeaponCombatProfile EquippedProfile { get; }
        List<PendingClosing> PendingList { get; }
        LivingEffectView BuildingView { get; set; }
        SustainedCastLock SustainedCast { get; }
        ActorStatus PlayerStatus { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        void DestroyUnityObject(Object obj, float delaySec);
        void EnsureMotionBody();
        float BossBodyRadius();
        void SetTemplateAimForSweep(Transform aim);
    }

    public interface IMotionHitResolverHost
    {
        MotionTemplateDriver Motion { get; }
        TemplateDeliveryRuntime TemplateDelivery { get; }
        GameClock Clock { get; }
        Transform Player { get; }
        BossReactor Boss { get; }
        ActorStatus BossStatus { get; }
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

    public interface ITemplateDeliveryRuntimeHost
    {
        GameClock Clock { get; }
        EquipmentItem EquippedWeapon { get; }
        CombatTuning Combat { get; }
        MechanicGrammar MechanicEngine { get; }
        MechanicPlan LastMechanicPlan { get; }
        MotionTemplateDriver Motion { get; }
        Weapons.CannonBlast Cannon { get; }
        WeaponPassiveRuntime WeaponPassives { get; }
        Transform Player { get; }
        BossReactor Boss { get; }
        BossVitals BossVitals { get; }
        ActorStatus PlayerStatus { get; }
        ActorStatus BossStatus { get; }
        KinematicMotor Motor { get; }
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
