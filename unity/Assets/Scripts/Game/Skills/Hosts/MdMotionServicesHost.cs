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
using Dovus.Core.Tuning;
using Dovus.Core;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Skills.Mechanics;
using Dovus.Game.Skills.Motion;
using Dovus.Game.Skills.Weapons;
using Dovus.Game.Skills;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills.Hosts
{
public sealed class MdMotionServicesHost
        : IMotionTemplateDriverHost,
            IMotionHitResolverHost,
            ITemplateDeliveryRuntimeHost
    {
        readonly ManifestationDirector _md;

        internal MdMotionServicesHost(ManifestationDirector md) => _md = md;

        public MotionTemplateDriver Motion => _md._motionDriver;
        public TemplateDeliveryRuntime TemplateDelivery => _md._templateDelivery;
        public MotionHitResolver MotionHitResolver => _md._motionHitResolver;
        public Transform Player => _md._player;
        public AllyDummyController Ally => _md._ally;
        public BossReactorController Boss => _md._boss;
        public GameClockHost Clock => _md._clock;
        public GameTuning Colors => _md._colors;
        public EquipmentItem EquippedWeapon => _md._equippedWeapon;
        public WeaponCombatProfile EquippedProfile => _md.EquippedProfile;
        public MotionTemplateBodyHost MotionBody => _md._motionBody;
        public PlayerTargetingController Targeting => _md._targeting;
        public HexagonInputController Input => _md._input;
        public Transform DirectorTransform => _md.transform;
        public float ClosingChainBonus => _md.CastSession.ClosingChainBonus;
        public int SlotQueryCastId
        {
            get => _md.CastSession.SlotQueryCastId;
            set => _md.CastSession.SlotQueryCastId = value;
        }
        public List<PendingClosing> PendingList => _md._pending;
        public LivingEffectView BuildingView
        {
            get => _md._buildingView;
            set => _md._buildingView = value;
        }
        public SustainedCastLock SustainedCast => _md._sustainedCast;
        public ActorStatusHost PlayerStatus => _md._playerStatus;
        public ActorStatusHost BossStatus => _md._bossStatus;
        public BossVitals BossVitals => _md._bossVitals;
        public ElementPaintNode? SelectedElementPaint => _md.SelectedElementPaint;
        public CombatTuning Combat => _md._combat;
        public MechanicGrammar MechanicEngine => _md.MechanicEngine;
        public MechanicPlan LastMechanicPlan => _md.LastMechanicPlan;
        public KinematicMotorController Motor => _md._motor;
        public Weapons.CannonBlast Cannon
        {
            get
            {
                _md.EnsureWeaponServices();
                return _md._cannonBlast;
            }
        }
        public WeaponPassiveRuntime WeaponPassives
        {
            get
            {
                _md.EnsureWeaponServices();
                return _md._weaponPassives;
            }
        }
        public bool CasterRecoilSuppressed
        {
            get => _md.CastSession.CasterRecoilSuppressed;
            set => _md.CastSession.CasterRecoilSuppressed = value;
        }
        public bool LastSkillEffectApplied
        {
            get => _md.LastSkillEffectApplied;
            set => _md.LastSkillEffectApplied = value;
        }

        public VerbExecutionData VerbData => _md._verbData;
        public SlotPassiveDirector SlotPassives => _md._slotPassives;

        public MechanicPlan MechanicPlanFor(SkillResolution skill) => _md.MechanicPlanFor(skill);
        public void PullBossToPlayerContact() => _md.PullBossToPlayerContact();
        public bool IsEnemyBody(Transform body) => _md.IsEnemyBody(body);
        public float PlayerBodyRadiusM() => _md.PlayerBodyRadiusM();
        public float FlatDistance(Vector3 a, Vector3 b) => ManifestationDirector.FlatDistance(a, b);
        public bool AoeReachedMotionHit(in MotionHit hit) => _md.AoeReachedMotionHit(hit);
        public bool TryVerbHitbox(in SkillResolution skill, out VerbHitboxSpec spec) =>
            _md.TryVerbHitbox(skill, out spec);
        public int EquippedWeaponNumber() => _md.EquippedWeaponNumber();
        public float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slash,
            float power,
            float chain) =>
            _md.ApplyClosingDamage(closing, skill, isBasicStrike, slash, power, chain);
        public void ApplyClosingHeal(ClosingHit closing, SkillResolution skill, float power, float chain) =>
            _md.ApplyClosingHeal(closing, skill, power, chain);
        public void ApplyClosingHealAmount(SkillResolution skill, int amount, Vector3? fieldCenter, float fieldRadiusM) =>
            _md.ApplyClosingHealAmount(skill, amount, fieldCenter, fieldRadiusM, null);
        public void ApplyClosingStatuses(PendingClosing pending, SkillResolution skill, bool bossReached) =>
            _md.ApplyClosingStatuses(pending, skill, bossReached);
        public void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 origin) =>
            _md.ApplyMechanicHitEffects(plan, origin);
        public void ApplyMechanicHitEffects(MechanicPlan plan, Vector3 fieldCenter, bool casterMoves) =>
            _md.ApplyMechanicHitEffects(plan, fieldCenter, casterMoves);
        public bool IsFriendlyFieldVerb(SkillResolution skill) =>
            ManifestationDirector.IsFriendlyFieldVerb(skill);
        public bool IsHealSkill(SkillResolution skill) => ManifestationDirector.IsHealSkill(skill);
        public WeaponPassiveMods HitMods(SkillResolution skill, bool isBasicStrike, bool consumeBonus) =>
            _md.HitMods(skill, isBasicStrike, consumeBonus);
        public void TryLandWeaponStun(SkillResolution skill, bool isBasicStrike) =>
            _md.TryLandWeaponStun(skill, isBasicStrike);
        public bool TryLaunchSkillExecutor(
            SkillExecutorKind kind,
            PendingClosing pending,
            SkillResolution skill,
            SkillMotionPlan motion,
            float power,
            LivingEffect capturedLogic,
            int slotCastId,
            float? activationDelayOverride) =>
            _md.TryLaunchSkillExecutor(
                kind, pending, skill, motion, power, capturedLogic, slotCastId, activationDelayOverride);
        public bool LandingFieldAllows(SkillResolution skill) => _md.LandingFieldAllows(skill);
        public bool TryBounceFriendly(float power) => _md.TryBounceFriendly(power);
        public void JsonLog(string message) => JsonEffectRuntime.JsonLog(message);
        public void DestroyUnityObject(Object obj, float delaySec) => Object.Destroy(obj, delaySec);

        public void EnsureMotionBody()
        {
            if (_md._player == null)
                return;
            if (_md._motionBody == null)
                _md._motionBody = _md._player.GetComponent<MotionTemplateBodyHost>();
            if (_md._motionBody == null)
                _md._motionBody = _md._player.gameObject.AddComponent<MotionTemplateBodyHost>();
        }

        public float BossBodyRadius() => _md.BossBodyRadius();

        public void SetTemplateAimForSweep(Transform aim) => _md._templateAim = aim;

        public HitboxVfxRegistry HitboxVfx => _md.HitboxVfx;
    }
}
