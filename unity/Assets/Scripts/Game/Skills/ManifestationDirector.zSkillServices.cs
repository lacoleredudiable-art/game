using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Effects;
using Dovus.Game.Skills.Flow;
using Dovus.Game.Skills.Targeting;
using Dovus.Game.Skills.Weapons;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        SkillServicesHost _skillServicesHost;
        SkillAim _skillAim;
        LivingEffectSpawner _effectSpawner;
        WeaponLoadoutCycle _weaponLoadout;
        SentenceManifestationBridge _sentenceBridge;

        void EnsureSkillServices()
        {
            if (_skillServicesHost != null)
                return;
            _skillServicesHost = new SkillServicesHost(this);
            _skillAim = new SkillAim(_skillServicesHost);
            _effectSpawner = new LivingEffectSpawner(_skillServicesHost);
            _weaponLoadout = new WeaponLoadoutCycle(_skillServicesHost);
            _sentenceBridge = new SentenceManifestationBridge(_skillServicesHost);
        }

        bool TryArmSkillTarget(SkillResolution skill)
        {
            EnsureSkillServices();
            return _skillAim.TryArmSkillTarget(skill);
        }

        internal void FaceTarget(Transform target)
        {
            EnsureSkillServices();
            _skillAim.FaceTarget(target);
        }

        internal void CaptureBasicFacing()
        {
            EnsureSkillServices();
            _skillAim.CaptureBasicFacing();
        }

        internal Vector3 FlatBodyForward()
        {
            EnsureSkillServices();
            return _skillAim.FlatBodyForward();
        }

        internal Transform CastFacingTarget
        {
            get
            {
                EnsureSkillServices();
                return _skillAim.CastFacingTarget;
            }
        }

        sealed class SkillServicesHost
            : ISkillAimHost,
                ILivingEffectSpawnerHost,
                IWeaponLoadoutCycleHost,
                ISentenceManifestationHost
        {
            readonly ManifestationDirector _md;

            internal SkillServicesHost(ManifestationDirector md) => _md = md;

            public Transform Player => _md._player;
            public PlayerTargeting Targeting => _md._targeting;
            public BossReactor Boss => _md._boss;
            public KinematicMotor Motor => _md._motor;
            public CombatTuning Combat => _md._combat;
            public PrototypeTuning Colors => _md._colors;
            public ReactionReadout Readout => _md._readout;
            public SkillMotor Skills => _md._skills;
            public SkillNumberCatalog SkillNumbers => _md._skillNumbers;
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
            public ActorPose Pose => _md._pose;
            public ActorVisual Visual => _md._visual;
            public BossVitals BossVitals => _md._bossVitals;
            public GroundScarField Scars => _md._scars;
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
                _md.ElementPaintChanged?.Invoke(paint);

            public bool TryGetMotionBinding(string skillId, out MotionBinding binding) =>
                _md.MotionCatalog.TryGet(skillId, out binding);

            public void ClearClosingStamp(LivingEffect logic) =>
                _md.ClosingDamageCore.ClearClosingStamp(logic);

            public GameClock Clock => _md._clock;
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
}
