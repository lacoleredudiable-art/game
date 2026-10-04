using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Damage;
using Dovus.Core.Dodge;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Hud;
using Dovus.Core.Input;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Passives;
using Dovus.Core.Presentation;
using Dovus.Core.Shared;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Effects;
using Dovus.Game.Skills.Flow;
using Dovus.Game.Skills.Targeting;
using Dovus.Game.Skills.Weapons;
using Dovus.Game.Skills;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Hosts
{
public sealed class MdSkillServicesHost
        : ISkillAimHost,
            ILivingEffectSpawnerHost,
            IWeaponLoadoutCycleHost,
            ISentenceManifestationHost
    {
        readonly ManifestationDirector _md;

        internal MdSkillServicesHost(ManifestationDirector md) => _md = md;

        public Transform Player => _md._player;
        public PlayerTargetingController Targeting => _md._targeting;
        public BossReactorController Boss => _md._boss;
        public KinematicMotorController Motor => _md._motor;
        public CombatTuning Combat => _md._combat;
        public GameTuning Colors => _md._colors;
        public ReactionReadoutHud Readout => _md._readout;
        public SkillMotor Skills => _md._skills;
        public ISkillRepository SkillNumbers => _md._skillNumbers;
        public EquipmentItem EquippedWeapon
        {
            get => _md._equippedWeapon;
            set => _md._equippedWeapon = value;
        }
        public SkillExecutorRouter SkillExecutorRouter => _md._skillExecutorRouter;
        public bool PerformingAttack => _md.PerformingAttack;
        public bool IsEnemyBody(Transform body) => _md.IsEnemyBody(body);
        public float PlayerBodyRadiusM() => _md.PlayerBodyRadiusM();
        public SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words) =>
            _md.ResolveSkillWords(words);

        public void EnsurePresentationCatalog() => _md.EnsurePresentationCatalog();
        public PresentationCatalog PresentationCatalog => _md.PresentationCatalog;
        public MechanicPlan MechanicPlanFor(SkillResolution skill) => _md.MechanicPlanFor(skill);
        public SkillExecutorRoute ApplyMechanicWorldRoute(MechanicPlan plan, SkillExecutorRoute route) =>
            _md.ApplyMechanicWorldRoute(plan, route);

        public void ApplyVerbHitboxSizing(
            SkillExecutorKind kind,
            in SkillResolution skill,
            ManifestationTuning tuning,
            float rangeMult,
            bool burst,
            ref float radius,
            ref float range,
            ref float durationSec,
            ref int spawnCount) =>
            _md.ApplyVerbHitboxSizing(
                kind, skill, tuning, rangeMult, burst, ref radius, ref range, ref durationSec, ref spawnCount);

        public Transform DirectorTransform => _md.transform;
        public SentenceEngine Engine => _md._engine;
        public ActorPoseView Pose => _md._pose;
        public ActorView Visual => _md._visual;
        public BossVitals BossVitals => _md._bossVitals;
        public GroundScarFieldView Scars => _md._scars;
        public LivingEffectView BuildingView
        {
            get => _md._buildingView;
            set => _md._buildingView = value;
        }
        public SkillAim Aim => _md._skillAim;

        public void SyncVisualDelivery() => _md.SyncVisualDelivery();
        public void StopBasicCannonAtFirstBody(LivingEffect logic, Vector3 origin, Vector3 facing) =>
            _md.StopBasicCannonAtFirstBody(logic, origin, facing);
        public void RefreshBuildingMobility(IReadOnlyList<SentenceWord> words) =>
            _md.RefreshBuildingMobility(words);
        public void DestroyUnityObject(Object obj) => Object.Destroy(obj);

        public Skill LastFactorySkill
        {
            get => _md.LastFactorySkill;
            set => _md.LastFactorySkill = value;
        }
        public EquipmentBonusResolver EquipmentBonus => _md._equipmentBonus;
        public SkillFactory SkillFactory => _md._skillFactory;
        public WeaponSwapState WeaponSwap => _md._weaponSwap;
        public void RefreshDefenderArmor() => _md.RefreshDefenderArmor();
        public void RaiseElementPaintChanged(ElementPaintNode paint) =>
            _md.RaiseElementPaintChanged(paint);

        public bool TryGetMotionBinding(SkillId skillId, out MotionBinding binding) =>
            _md.MotionCatalog.TryGet(skillId, out binding);

        public void ClearClosingStamp(LivingEffect logic) =>
            _md.ClosingDamageCore.ClearClosingStamp(logic);

        public GameClockHost Clock => _md._clock;
        public TeamComboAccess TeamAccess => _md._team;
        public LivingEffectSpawner EffectSpawner => _md._effectSpawner;
        public int LastWordCount
        {
            get => _md._lastWordCount;
            set => _md._lastWordCount = value;
        }
        public bool PosedForRecovery
        {
            get => _md._posedForRecovery;
            set => _md._posedForRecovery = value;
        }
        public List<PendingClosing> Pending => _md.PendingList;
        public void TryBeginBasicStrikeStep() => _md.TryBeginBasicStrikeStep();
        public WeaponSkillCompatibility WeaponCompatibilityFor(SkillResolution skill) =>
            _md.WeaponCompatibilityFor(skill);
        public bool TryArmSkillTarget(SkillResolution skill) => _md.TryArmSkillTarget(skill);
        public void FaceTarget(Transform target) => _md.FaceTarget(target);
        public void ApplyCastMobility(SkillResolution skill, float durationSec) =>
            _md.ApplyCastMobility(skill, durationSec);
    }
}
