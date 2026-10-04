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
using Dovus.Game.Platform;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using Dovus.Game.Skills.Mechanics;
using System;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        MdMechanicsHost _mechanicsHost;
        MechanicWorldRuntime _mechanicWorld;
        JsonEffectRuntime _jsonEffects;
        VolumePayloadApplier _volumePayload;
        MechanicPortals _mechanicPortals;

        SkillResolution _jsonCastSkill = SkillResolution.Empty;
        ClosingHit _jsonCastClosing;
        int _lastStatusTransferMoved;
        bool _lastFriendlyWasAlly;
        double _tasarShieldUntilMs;
        float _overflowNextHitBonus;
        double _lastBasicStrikeMs = -1;
        bool _jsonTickDamage;

        internal MechanicGrammar MechanicEngine =>
            ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design) ? design.Mechanics : null;

        SkillResolution SkillFromPlan(MechanicPlan plan)
        {
            if (_skills == null || plan == null || plan.Verb <= 0 || plan.Adjective <= 0)
                return SkillResolution.Empty;
            return _skills.Resolve(new[] { plan.Verb, plan.Adjective });
        }

        bool HasSelfReflect(MechanicPlan plan) =>
            plan != null && !JsonEffectRules.IsWorldMirror(plan)
            && plan.Effects.Any(e => e.Stat == "yansit" && e.Target == "kendin");

        int JsonCleanseCount(in SkillResolution skill) =>
            JsonEffectRules.CleanseCount(
                !skill.IsEmpty && !skill.Engine.IsNull ? skill.Engine.CleanseCount(0) : 0,
                MechanicPlanFor(skill));

        float ShieldAbsorbFor(in SkillResolution skill)
        {
            float absorb = !skill.IsEmpty && !skill.Engine.IsNull
                ? skill.Engine.ShieldAbsorb(0f)
                : 0f;
            StatusTuning tuning = _combat != null ? _combat.Status : new StatusTuning();
            return absorb > 0f ? absorb : tuning.ShieldAbsorb;
        }

        internal int MechanicWorldLeftoverCount()
        {
            if (_mechanicWorld == null)
                return 0;
            return _mechanicWorld.LeftoverCount() + _mechanicPortals.LeftoverCount();
        }

        internal void ClearMechanicWorldSweep()
        {
            EnsureMechanicsServices();
            _mechanicWorld.ClearSweepState();
            _mechanicPortals.ClearSweepState();
        }

        void EnsureMechanicsServices()
        {
            if (_mechanicsHost != null)
                return;
            _mechanicsHost = new MdMechanicsHost(this);
            _mechanicWorld = new MechanicWorldRuntime(_mechanicsHost);
            _jsonEffects = new JsonEffectRuntime(_mechanicsHost, _mechanicWorld);
            _volumePayload = new VolumePayloadApplier(_mechanicsHost, _jsonEffects);
            _mechanicWorld.Wire(_volumePayload, _jsonEffects);
            _mechanicPortals = new MechanicPortals(_mechanicsHost);
        }

        sealed class MdMechanicsHost
            : IMechanicWorldHost,
                IJsonEffectHost,
                IMechanicPortalsHost
        {
            readonly ManifestationDirector _md;

            internal MdMechanicsHost(ManifestationDirector md) => _md = md;

            public Transform Player => _md._player;
            public AllyDummyController Ally => _md._ally;
            public BossReactorController Boss => _md._boss;
            public ActorStatusHost BossStatus => _md._bossStatus;
            public BossVitals BossVitals => _md._bossVitals;
            public BossDirector BossDirector => _md._bossDirector;
            public ActorStatusHost PlayerStatus => _md._playerStatus;
            public GameClockHost Clock => _md._clock;
            public TeamComboAccess TeamAccess => _md._team;
            public CombatTuning Combat => _md._combat;
            public SkillMotor Skills => _md._skills;
            public SlotPassiveDirector SlotPassives => _md._slotPassives;
            public int SlotQueryCastId => _md._slotQueryCastId;
            public ReactionReadoutHud Readout => _md._readout;
            public DamageNumberHud DamageHud => _md._damageHud;
            public ISentenceDebugSink DebugHud => _md._debugHud;
            public KinematicMotorController Motor => _md._motor;
            public Transform DirectorTransform => _md.transform;
            public ElementPaintNode? SelectedElementPaint => _md.SelectedElementPaint;
            public WeaponCombatProfile EquippedProfile => _md.EquippedProfile;
            public EquipmentItem EquippedWeapon => _md._equippedWeapon;
            public MechanicPlan LastMechanicPlan
            {
                get => _md.LastMechanicPlan;
                set => _md.LastMechanicPlan = value;
            }
            public MechanicGrammar MechanicEngine => _md.MechanicEngine;
            public MechanicRules JsonRules =>
                _md.MechanicEngine != null ? _md.MechanicEngine.Rules : null;
            public double JsonNow => _md._clock != null ? _md._clock.Director.WorldTimeMs : 0;
            public bool BossDisplaceable =>
                _md._boss != null
                && ForcedDisplacement.Allows(_md._bossStatus != null ? _md._bossStatus.Board : null);
            public StatusTuning JsonStatusTuning =>
                _md._combat != null ? _md._combat.Status : new StatusTuning();
            public SkillResolution JsonCastSkill
            {
                get => _md._jsonCastSkill;
                set => _md._jsonCastSkill = value;
            }
            public ClosingHit JsonCastClosing
            {
                get => _md._jsonCastClosing;
                set => _md._jsonCastClosing = value;
            }
            public float SelfDamageBuff
            {
                get => _md._selfDamageBuff;
                set => _md._selfDamageBuff = value;
            }
            public double SelfDamageBuffUntilMs
            {
                get => _md._selfDamageBuffUntilMs;
                set => _md._selfDamageBuffUntilMs = value;
            }
            public int LastStatusTransferMoved
            {
                get => _md._lastStatusTransferMoved;
                set => _md._lastStatusTransferMoved = value;
            }
            public bool LastFriendlyWasAlly
            {
                get => _md._lastFriendlyWasAlly;
                set => _md._lastFriendlyWasAlly = value;
            }
            public double TasarShieldUntilMs
            {
                get => _md._tasarShieldUntilMs;
                set => _md._tasarShieldUntilMs = value;
            }
            public float OverflowNextHitBonus
            {
                get => _md._overflowNextHitBonus;
                set => _md._overflowNextHitBonus = value;
            }
            public double LastBasicStrikeMs
            {
                get => _md._lastBasicStrikeMs;
                set => _md._lastBasicStrikeMs = value;
            }
            public bool JsonTickDamage
            {
                get => _md._jsonTickDamage;
                set => _md._jsonTickDamage = value;
            }
            public string CardEffect
            {
                get => _md._cardEffect;
                set => _md._cardEffect = value;
            }
            public bool TemplateOwnsPosition => _md._templateOwnsPosition;
            public SkillResolution DeliverySkill => _md._deliverySkill;
            public PendingClosing DeliveryPending => _md._deliveryPending;
            public SkillResolution TemplateSkill => _md._templateSkill;
            public float TemplateChain => _md._templateChain;

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
            public bool IsFriendlyFieldVerb(in SkillResolution skill) =>
                ManifestationDirector.IsFriendlyFieldVerb(skill);
            public bool BasicTargetStillInReach(Transform boss, float reach) =>
                _md.BasicTargetStillInReach(boss, reach);
            public bool HammerStunReady(double now) => _md.HammerStunReady(now);
            public void CommitHammerStun(double now, bool ready, bool had, bool applied) =>
                _md.CommitHammerStun(now, ready, had, applied);
            public void ProjectileEraseOnHit(MechanicPlan plan, Vector3 center) =>
                _md.ProjectileEraseOnHit(plan, center);
            public void BeginProjectileErase(MechanicPlan plan, Vector3 aimDir, Vector3 center) =>
                _md.BeginProjectileErase(plan, aimDir, center);
            public EraseSpec ProjectileEraseSpec(MechanicPlan plan) => _md.ProjectileEraseSpec(plan);
            public void ScheduleAfter(double now, float delaySec, Action run) =>
                _md._mechanicPortals.ScheduleAfter(now, delaySec, run);
            public void TeleportPlayer(Vector3 pos) => _md.TeleportPlayer(pos);
            public void DestroyUnityObject(UnityEngine.Object obj) => Object.Destroy(obj);
            public void DestroyUnityObject(UnityEngine.Object obj, float delaySeconds) =>
                Object.Destroy(obj, delaySeconds);

            public PlaceholderFactory Placeholders => _md.Placeholders;
        }
    }
}
