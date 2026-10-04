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
public interface IMotionTemplateDriverHost
    {
        Transform Player { get; }
        AllyDummyController Ally { get; }
        BossReactorController Boss { get; }
        GameClockHost Clock { get; }
        GameTuning Colors { get; }
        EquipmentItem EquippedWeapon { get; }
        MotionTemplateBodyHost MotionBody { get; }
        PlayerTargetingController Targeting { get; }
        HexagonInputController Input { get; }
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
        ActorStatusHost PlayerStatus { get; }
        ElementPaintNode? SelectedElementPaint { get; }
        void DestroyUnityObject(Object obj, float delaySec);
        void EnsureMotionBody();
        float BossBodyRadius();
        void SetTemplateAimForSweep(Transform aim);
    }
}
