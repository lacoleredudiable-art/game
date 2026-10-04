using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Composition;
using Dovus.Core.Tuning;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Closing;
using Dovus.Game.Team;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        ClosingStatusApplier _closingStatus;
        ClosingHealResolver _closingHeal;
        ClosingDamageResolver _closingDamage;
        ClosingHost _closingHost;

        void EnsureClosingServices()
        {
            if (_closingHost != null)
                return;
            _closingHost = new ClosingHost(this);
            _closingStatus = new ClosingStatusApplier(_closingHost);
            _closingHeal = new ClosingHealResolver(_closingHost);
            _closingDamage = new ClosingDamageResolver(_closingHost);
        }

        sealed class ClosingHost
            : IClosingStatusHost, IClosingHealHost, IClosingDamageHost
        {
            readonly ManifestationDirector _md;

            internal ClosingHost(ManifestationDirector md) => _md = md;

            public Transform Player => _md._player;
            public AllyDummyController Ally => _md._ally;
            public ActorStatusHost PlayerStatus => _md._playerStatus;
            public ActorStatusHost BossStatus => _md._bossStatus;
            public CombatTuning Combat => _md._combat;
            public MobilityCcData MobilityCc => _md._mobilityCc;
            public SlotPassiveDirector SlotPassives => _md._slotPassives;
            public PassiveFlowRunner PassiveFlows
            {
                get
                {
                    _md.EnsureCoreServices();
                    return _md._slotPassiveRuntime.PassiveFlows;
                }
            }
            public int SlotQueryCastId => _md._slotQueryCastId;
            public GameClockHost Clock => _md._clock;
            public TeamComboAccess TeamAccess => _md._team;
            public MechanicGrammar MechanicEngine => _md.MechanicEngine;
            public BossVitals BossVitals => _md._bossVitals;
            Transform IClosingStatusHost.Boss =>
                _md._boss != null ? _md._boss.transform : null;
            BossReactorController IClosingDamageHost.Boss => _md._boss;
            public BossDirector BossDirector => _md._bossDirector;
            public DamageNumberHud DamageHud => _md._damageHud;
            public ElementPaintNode? SelectedElementPaint => _md.SelectedElementPaint;
            public bool LastFriendlyWasAlly { set => _md._lastFriendlyWasAlly = value; }
            public float ClosingChainBonus => _md._closingChainBonus;
            public ReactionReadoutHud Readout => _md._readout;
            public SentenceDebugHud DebugHud => _md._debugHud;
            public KinematicMotorController Motor => _md._motor;
            public GroundScarFieldView Scars => _md._scars;
            public bool JsonTickDamage => _md._jsonTickDamage;
            public float LastHitX => _md._lastHitX;
            public float LastHitZ => _md._lastHitZ;
            public float LastClosingDamageDealt
            {
                get => _md.LastClosingDamageDealt;
                set => _md.LastClosingDamageDealt = value;
            }

            public ClosingStatusApplier StatusApplier => _md._closingStatus;

            public float WeaponFriendlyScale() => _md.WeaponFriendlyScale();
            public int JsonCleanseCount(SkillResolution skill) => _md.JsonCleanseCount(skill);
            public void ShareFriendlyStatuses(SkillResolution skill, StatusBoard board) =>
                _md.ShareFriendlyStatuses(skill, board);
            public void ApplyPurgePower(SkillResolution skill, int cleansed) =>
                _md.ApplyPurgePower(skill, cleansed);
            public void ApplyArmorShred(SkillResolution skill, ActorStatusHost boss) =>
                _md.ApplyArmorShred(skill, boss);
            public bool IsEnemyBody(Transform body) => _md.IsEnemyBody(body);
            public Vector3? BossHitPoint() => _md.BossHitPoint();
            public Color? DamageTint() => _md.DamageTint();
            public PlayerVitalsHost CachedPlayerVitals() => _md.CachedPlayerVitals();
            public float WeaponSupportPower(SkillResolution skill) => _md.WeaponSupportPower(skill);
            public float HealBuffMultiplier(SkillResolution skill) => _md.HealBuffMultiplier(skill);
            public int FriendlyTargetCap(SkillResolution skill) => _md.FriendlyTargetCap(skill);
            public void ConsumeWeaponBonus(StatusBoard board) => _md.ConsumeWeaponBonus(board);
            public void ApplyHealOverflow(SkillResolution skill, int amount, int healed, bool ally) =>
                _md.ApplyHealOverflow(skill, amount, healed, ally);
            public DamageOutcome ComputeOutgoingHit(
                ClosingHit closing,
                SkillResolution skill,
                bool isBasicStrike,
                float slashCommitMult,
                float effectScale,
                float? chainBonusOverride) =>
                _md.ComputeOutgoingHit(closing, skill, isBasicStrike, slashCommitMult, effectScale, chainBonusOverride);
            public void TryConsumeCounterWindow() => _md.TryConsumeCounterWindow();
            public void RememberHitPoint(Vector3? point) => _md.RememberHitPoint(point);
            public void TryCannonBlast(float x, float z) => _md.TryCannonBlast(x, z);
            public void NotifyBossStruck(bool isCrit, bool allowHitstop) =>
                _md.NotifyBossStruck(isCrit, allowHitstop);
            public void NoteImpactOrigin(LivingEffect logic) => _md.NoteImpactOrigin(logic);
            public float PlayerBodyRadiusM() => _md._motor != null ? _md._motor.BodyRadiusM : 0f;
        }

        internal ClosingDamageResolver ClosingDamageCore
        {
            get
            {
                EnsureClosingServices();
                return _closingDamage;
            }
        }

        void SpawnClosingImpact(PendingClosing p)
        {
            EnsureCoreServices();
            _closingQueue.SpawnClosingImpact(p);
        }

        SkillResolution ResolveSkillWords(IReadOnlyList<SentenceWord> words)
        {
            EnsureCoreServices();
            return _closingQueue.ResolveSkillWords(words);
        }

        SkillResolution ResolvePendingSkill(PendingClosing p)
        {
            EnsureCoreServices();
            return _closingQueue.ResolvePendingSkill(p);
        }

        internal static bool IsFriendlyFieldVerb(in SkillResolution skill) =>
            skill.VerbId is "2" or "4" or "8" or "9";

        void ApplyClosingStatuses(PendingClosing p, SkillResolution skill, bool bossReached = true)
        {
            EnsureClosingServices();
            _closingStatus.Apply(p.Target, skill, bossReached);
        }

        void ApplyClosingHeal(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale = 1f,
            float? chainBonusOverride = null,
            Vector3? fieldCenter = null,
            float fieldRadiusM = 0f)
        {
            EnsureClosingServices();
            _closingHeal.Apply(closing, skill, effectScale, chainBonusOverride, fieldCenter, fieldRadiusM);
        }

        int CalculateClosingHealAmount(
            ClosingHit closing,
            SkillResolution skill,
            float effectScale,
            float? chainBonusOverride)
        {
            EnsureClosingServices();
            return _closingHeal.CalculateAmount(closing, skill, effectScale, chainBonusOverride);
        }

        void ApplyClosingHealAmount(
            SkillResolution skill,
            int amount,
            Vector3? fieldCenter,
            float fieldRadiusM,
            Transform preferredTarget = null)
        {
            EnsureClosingServices();
            _closingHeal.ApplyAmount(skill, amount, fieldCenter, fieldRadiusM, preferredTarget);
        }

        float ApplyClosingDamage(
            ClosingHit closing,
            SkillResolution skill,
            bool isBasicStrike,
            float slashCommitMult,
            float effectScale = 1f,
            float? chainBonusOverride = null)
        {
            EnsureClosingServices();
            return _closingDamage.Apply(
                closing, skill, isBasicStrike, slashCommitMult, effectScale, chainBonusOverride);
        }

        void StampScar(LivingEffectView view, ClosingHit closing)
        {
            EnsureClosingServices();
            _closingDamage.StampScar(view, closing);
        }

        void ApplyBossClosingBasic(LivingEffect logic, ClosingHit closing)
        {
            EnsureClosingServices();
            _closingDamage.ApplyBossClosingBasic(logic, closing);
        }

        void ApplyBossClosing(LivingEffect logic, ClosingHit closing, SkillResolution skill)
        {
            EnsureClosingServices();
            _closingDamage.ApplyBossClosing(logic, closing, skill);
        }

        bool IsBossInStrikeCapsule(LivingEffect logic, float reachM)
        {
            EnsureClosingServices();
            return _closingDamage.IsBossInStrikeCapsule(logic, reachM);
        }

        bool BasicTargetStillInReach(Transform target, float reachM)
        {
            EnsureClosingServices();
            return _closingDamage.BasicTargetStillInReach(target, reachM);
        }

        float BasicStrikeYawDeg(Transform target)
        {
            EnsureClosingServices();
            return _closingDamage.BasicStrikeYawDeg(target);
        }

        bool IsClosingInRange(LivingEffect logic, ClosingHit closing)
        {
            EnsureClosingServices();
            return _closingDamage.IsClosingInRange(logic, closing);
        }

        static bool IsHealSkill(SkillResolution skill) => ClosingHealRules.IsHealSkill(skill);
    }
}
