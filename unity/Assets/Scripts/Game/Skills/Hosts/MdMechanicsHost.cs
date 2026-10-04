using Dovus.Core.Boss;
using Dovus.Core.Passives;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Motion;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Skills.Mechanics;
using Dovus.Game.Skills.State;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dovus.Game.Skills.Hosts
{
    public sealed class MdMechanicsHost : IMdMechanicsHost
    {
        readonly ManifestationDirector _md;

        public MdMechanicsHost(ManifestationDirector md) => _md = md;

        public SkillWorldState World => _md.SkillWorld;
        public CastSessionState Cast => _md.CastSession;

        public Transform Player => _md.MechanicsPlayer;
        public AllyDummyController Ally => _md.MechanicsAlly;
        public BossReactorController Boss => _md.MechanicsBoss;
        public ActorStatusHost BossStatus => _md.MechanicsBossStatus;
        public ActorStatusHost PlayerStatus => _md.MechanicsPlayerStatus;
        public GameClockHost Clock => _md.MechanicsClock;
        public CombatTuning Combat => _md.MechanicsCombat;
        public MechanicGrammar MechanicEngine => _md.MechanicEngine;
        public MechanicRules JsonRules => MechanicEngine != null ? MechanicEngine.Rules : null;
        public double JsonNow => Clock != null ? Clock.Director.WorldTimeMs : 0;
        public Transform DirectorTransform => _md.transform;
        public TeamComboAccess TeamAccess => _md.MechanicsTeam;
        public PlaceholderFactory Placeholders => _md.Placeholders;

        public BossVitals BossVitals => _md.MechanicsBossVitals;
        public BossDirector BossDirector => _md.MechanicsBossDirector;
        public SkillMotor Skills => _md.MechanicsSkills;
        public SlotPassiveDirector SlotPassives => _md.MechanicsSlotPassives;
        public int SlotQueryCastId => Cast.SlotQueryCastId;
        public ReactionReadoutHud Readout => _md.MechanicsReadout;
        public DamageNumberHud DamageHud => _md.MechanicsDamageHud;
        public ISentenceDebugSink DebugHud => _md.MechanicsDebugHud;
        public KinematicMotorController Motor => _md.MechanicsMotor;
        public ElementPaintNode? SelectedElementPaint => _md.SelectedElementPaint;
        public WeaponCombatProfile EquippedProfile => _md.EquippedProfile;
        public EquipmentItem EquippedWeapon => _md.MechanicsEquippedWeapon;

        public MechanicPlan LastMechanicPlan
        {
            get => _md.LastMechanicPlan;
            set => _md.LastMechanicPlan = value;
        }

        public bool BossDisplaceable =>
            Boss != null && ForcedDisplacement.Allows(BossStatus != null ? BossStatus.Board : null);

        public StatusTuning JsonStatusTuning => Combat != null ? Combat.Status : new StatusTuning();

        public bool TemplateOwnsPosition => _md.TemplateOwnsPositionFlag;
        public SkillResolution DeliverySkill => Cast.DeliverySkill;
        public PendingClosing DeliveryPending => Cast.DeliveryPending;
        public SkillResolution TemplateSkill => _md.TemplateSkillRef;
        public float TemplateChain => _md.TemplateChainRef;
        public double LastBasicStrikeMs => World.LastBasicStrikeMs;

        public double JsonParam(string key, double fallback)
        {
            MechanicRules rules = JsonRules;
            double v = rules != null ? rules.Param(key) : 0;
            return v > 0 ? v : fallback;
        }

        public MechanicPlan MechanicPlanFor(in SkillResolution skill) => _md.MechanicPlanFor(skill);
        public int JsonCleanseCount(in SkillResolution skill) => _md.JsonCleanseCount(skill);
        public float ShieldAbsorbFor(in SkillResolution skill) => _md.ShieldAbsorbFor(skill);
        public bool HasSelfReflect(MechanicPlan plan) => _md.HasSelfReflect(plan);
        public SkillResolution SkillFromPlan(MechanicPlan plan) => _md.SkillFromPlan(plan);
        public float PlayerBodyRadiusM() => _md.PlayerBodyRadiusM();
        public float BossBodyRadius() => _md.BossBodyRadius();
        public float WeaponFriendlyScale() => _md.WeaponFriendlyScale();
        public PlayerVitalsHost CachedPlayerVitals() => _md.CachedPlayerVitals();
        public Vector3 ClampToArena(Vector3 pos) => _md.ClampToArena(pos);
        public float FlatDistance(Vector3 a, Vector3 b) => ManifestationDirector.FlatDistance(a, b);

        public float ApplyClosingDamage(
            ClosingHit closing,
            in SkillResolution skill,
            bool isBasicStrike,
            float slash,
            float power,
            float chain) =>
            _md.ApplyClosingDamage(closing, skill, isBasicStrike, slash, power, chain);

        public void ApplyClosingHeal(
            ClosingHit closing,
            in SkillResolution skill,
            float effectScale,
            float? chainBonusOverride,
            Vector3? fieldCenter,
            float fieldRadiusM) =>
            _md.ApplyClosingHeal(closing, skill, effectScale, chainBonusOverride, fieldCenter, fieldRadiusM);

        public void ApplyClosingHealAmount(
            in SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget) =>
            _md.ApplyClosingHealAmount(skill, amount, fieldCenter, fieldRadiusM, preferredTarget);

        public int CalculateClosingHealAmount(
            ClosingHit closing,
            in SkillResolution skill,
            float power,
            float chain) =>
            _md.CalculateClosingHealAmount(closing, skill, power, chain);

        public bool IsHealSkill(in SkillResolution skill) => ManifestationDirector.IsHealSkill(skill);
        public bool IsFriendlyFieldVerb(in SkillResolution skill) => ManifestationDirector.IsFriendlyFieldVerb(skill);
        public bool BasicTargetStillInReach(Transform boss, float reach) => _md.BasicTargetStillInReach(boss, reach);
        public bool HammerStunReady(double now) => _md.HammerStunReady(now);
        public void CommitHammerStun(double now, bool ready, bool had, bool applied) =>
            _md.CommitHammerStun(now, ready, had, applied);
        public void ProjectileEraseOnHit(MechanicPlan plan, Vector3 center) => _md.ProjectileEraseOnHit(plan, center);
        public void BeginProjectileErase(MechanicPlan plan, Vector3 aimDir, Vector3 center) =>
            _md.BeginProjectileErase(plan, aimDir, center);
        public EraseSpec ProjectileEraseSpec(MechanicPlan plan) => _md.ProjectileEraseSpec(plan);
        public void ScheduleAfter(double now, float delaySec, Action run) =>
            _md.ScheduleMechanicPortalAfter(now, delaySec, run);
        public void TeleportPlayer(Vector3 pos) => _md.TeleportPlayer(pos);
        public void DestroyUnityObject(UnityEngine.Object obj) => Object.Destroy(obj);
        public void DestroyUnityObject(UnityEngine.Object obj, float delaySeconds) => Object.Destroy(obj, delaySeconds);
    }
}
