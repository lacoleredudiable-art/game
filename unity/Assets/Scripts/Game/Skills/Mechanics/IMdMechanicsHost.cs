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
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using Dovus.Game.Team;
using System;
using UnityEngine;
namespace Dovus.Game.Skills.Mechanics
{
public interface IMdMechanicsHost
    {
        Transform Player { get; }
        AllyDummy Ally { get; }
        BossReactor Boss { get; }
        ActorStatus BossStatus { get; }
        BossVitals BossVitals { get; }
        BossDirector BossDirector { get; }
        ActorStatus PlayerStatus { get; }
        GameClock Clock { get; }
        TeamComboAccess TeamAccess { get; }
        CombatTuning Combat { get; }
        SkillMotor Skills { get; }
        SlotPassiveDirector SlotPassives { get; }
        int SlotQueryCastId { get; }
        ReactionReadout Readout { get; }
        DamageNumberHud DamageHud { get; }
        SentenceDebugHud DebugHud { get; }
        KinematicMotor Motor { get; }
        Transform DirectorTransform { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        WeaponCombatProfile EquippedProfile { get; }
        EquipmentItem EquippedWeapon { get; }
        MechanicPlan LastMechanicPlan { get; set; }
        MechanicGrammar MechanicEngine { get; }
        MechanicRules JsonRules { get; }
        double JsonNow { get; }
        double JsonParam(string key, double fallback);
        bool BossDisplaceable { get; }
        StatusTuning JsonStatusTuning { get; }
        SkillResolution JsonCastSkill { get; set; }
        ClosingHit JsonCastClosing { get; set; }
        float SelfDamageBuff { get; set; }
        double SelfDamageBuffUntilMs { get; set; }
        int LastStatusTransferMoved { get; set; }
        bool LastFriendlyWasAlly { get; set; }
        double TasarShieldUntilMs { get; set; }
        float OverflowNextHitBonus { get; set; }
        double LastBasicStrikeMs { get; set; }
        bool JsonTickDamage { get; set; }
        string CardEffect { get; set; }
        bool TemplateOwnsPosition { get; }
        SkillResolution DeliverySkill { get; }
        PendingClosing DeliveryPending { get; }
        SkillResolution TemplateSkill { get; }
        float TemplateChain { get; }
        MechanicPlan MechanicPlanFor(in SkillResolution skill);
        int JsonCleanseCount(in SkillResolution skill);
        float ShieldAbsorbFor(in SkillResolution skill);
        bool HasSelfReflect(MechanicPlan plan);
        SkillResolution SkillFromPlan(MechanicPlan plan);
        float PlayerBodyRadiusM();
        float BossBodyRadius();
        float WeaponFriendlyScale();
        PlayerVitals CachedPlayerVitals();
        Vector3 ClampToArena(Vector3 pos);
        float FlatDistance(Vector3 a, Vector3 b);
        float ApplyClosingDamage(ClosingHit closing, in SkillResolution skill, bool isBasicStrike, float slash, float power, float chain);
        void ApplyClosingHeal(
            ClosingHit closing,
            in SkillResolution skill,
            float effectScale,
            float? chainBonusOverride,
            Vector3? fieldCenter,
            float fieldRadiusM);
        void ApplyClosingHealAmount(
            in SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget);
        int CalculateClosingHealAmount(ClosingHit closing, in SkillResolution skill, float power, float chain);
        bool IsHealSkill(in SkillResolution skill);
        bool IsFriendlyFieldVerb(in SkillResolution skill);
        bool BasicTargetStillInReach(Transform boss, float reach);
        bool HammerStunReady(double now);
        void CommitHammerStun(double now, bool ready, bool had, bool applied);
        void ProjectileEraseOnHit(MechanicPlan plan, Vector3 center);
        void BeginProjectileErase(MechanicPlan plan, Vector3 aimDir, Vector3 center);
        EraseSpec ProjectileEraseSpec(MechanicPlan plan);
        void ScheduleAfter(double now, float delaySec, Action run);
        void TeleportPlayer(Vector3 pos);
        void DestroyUnityObject(UnityEngine.Object obj);
        void DestroyUnityObject(UnityEngine.Object obj, float delaySeconds);
    }
}
