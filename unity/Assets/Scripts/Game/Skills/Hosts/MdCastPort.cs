using Dovus.App.Casting;
using Dovus.Core.Boss;
using Dovus.Core.Casting;
using Dovus.Core.Damage;
using Dovus.Core.Dodge;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Hud;
using Dovus.Core.Input;
using Dovus.Core.Manifestation;
using Dovus.Core.Passives;
using Dovus.Core.Shared;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Game.Skills;
using Dovus.Game.Team;
using UnityEngine;

namespace Dovus.Game.Skills.Hosts
{
public sealed class MdCastPort : ICastPort<PendingClosing>, IBasicStrikePort<PendingClosing>
    {
        readonly RuleEngineV4SentenceHost _md;
        Transform _basicImpactTarget;
        LivingEffect _closingLogic;
        SkillResolution _lastCastSkill;
        float _lastCastDealt;
        RuleEngineV4DirectorBridge _ruleEngineV4;
        internal RuleEngineV4DirectorBridge RuleEngineV4Bridge =>
            _ruleEngineV4 ??= new RuleEngineV4DirectorBridge(RuleEngineV4ManifestationAdapter.Bind(_md));

        /// <summary>FireClosing başında yakalanan logic (eski koddaki yerel değişkenle aynı örnek).</summary>
        internal void BeginClosing(LivingEffect logic) => _closingLogic = logic;

        internal MdCastPort(RuleEngineV4SentenceHost md) => _md = md;

        internal void NotifyRuleEngineV4Dealt(float dealt) => _lastCastDealt = dealt;

        public void ResetClosingChainBonus() => _md.CastSession.ClosingChainBonus = 1f;

        public SkillResolution ResolveSkill(PendingClosing ctx)
        {
            _lastCastSkill = _md.ResolvePendingSkill(ctx);
            _lastCastDealt = 0f;
            return _lastCastSkill;
        }

        public void NoteWeaponCast(SkillResolution skill) => _md.NoteWeaponCast(skill);

        public int OpenSlotCast() =>
            _md.CastSession.SlotQueryCastId = _md._slotPassives != null ? _md._slotPassives.OpenCast() : 0;

        public void CloseSlotCast() => _md._slotPassives?.CloseCast();

        public void ResetSlotQueryCastId() => _md.CastSession.SlotQueryCastId = 0;

        public WeaponSkillCompatibility Compatibility(SkillResolution skill) =>
            _md.WeaponCompatibilityFor(skill);

        public bool ShouldArmPassive(WeaponSkillCompatibility compatibility) =>
            PassiveSlotPolicy.ShouldArm(true, compatibility.PassiveEnabled, _md._slotPassiveNeedsWeapon);

        public void TryTriggerPassive(PendingClosing ctx) =>
            _md.TryTriggerPassive(ctx.Words, _md._clock.Director.WorldTimeMs);

        public void ApplyResourceCost(SkillResolution skill)
        {
            _md.EnsureLaunchServices();
            _md._castSideEffects.ApplyResourceCost(skill);
        }

        public SkillMotionPlan ResolveMotion(SkillResolution skill)
        {
            _md.EnsureLaunchServices();
            return _md._castSideEffects.ResolveSkillMotion(skill);
        }

        public void ApplyMotionIframe(SkillResolution skill, in SkillMotionPlan motion)
        {
            _md.EnsureLaunchServices();
            _md._castSideEffects.ApplySkillMotionIframe(skill, motion);
        }

        public void NoteSustainedCast(SkillResolution skill) => _md.NoteSustainedCast(skill);

        public void NotifyCast(SkillId skillId) => _md.TeamHub.NotifyCast(skillId);

        public void ApplySelfCastEffects(SkillResolution skill) => _md.ApplySelfCastEffects(skill);

        public void NoteJsonCast(SkillResolution skill, PendingClosing ctx) =>
            _md.NoteJsonCast(skill, ctx.Closing);

        public bool TryLaunchSkill(
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            out float dealt)
        {
            dealt = 0f;
            _md.EnsureLaunchServices();
            if (!RuleEngineV4Bridge.OwnsSkill(skill))
            {
                Debug.LogError(
                    $"[V4-FALLBACK] MdCastPort: dilim dışı skill {skill.Identity.Id}; eski executor yolu kaldırıldı.");
                return false;
            }
            if (!RuleEngineV4Bridge.TryLaunch(ctx, skill, in motion, out dealt))
                return false;
            _lastCastDealt = dealt;
            return true;
        }

        public void ApplyCooldown(SkillResolution skill, PendingClosing ctx, bool cosmeticIfDisabled)
        {
            _md.EnsureLaunchServices();
            _md._castSideEffects.ApplyCooldown(skill, ctx.Words, cosmeticIfDisabled);
        }

        public void SpawnClosingImpact(PendingClosing ctx) => _md.SpawnClosingImpact(ctx);

        public void SetLastResolvedSkillId(SkillId skillId) => _md.LastResolvedSkillId = skillId.Value;

        public bool IsHealSkill(SkillResolution skill) => _md.CastPortIsHealSkill(skill);

        public void SetLastSkillEffectApplied(bool applied)
        {
            _md.LastSkillEffectApplied = applied;
            if (_lastCastSkill.Identity.Verb.Value == "1" && _lastCastSkill.Identity.Adjective.Value == "1")
                LogSmokeOneOne(_lastCastSkill, applied, _lastCastDealt);
        }

        public void LogSmokeOneOne(SkillResolution skill, bool effectApplied, float dealt)
        {
            DebugConfig.DevLog(
                $"[ElementSystem] smoke 1-1 effect applied={effectApplied} "
                + $"damage={dealt:0.##}");
        }

        public void TrySchedulePassiveEcho(
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion)
        {
            if (_md._slotPassives == null
                || _md._clock == null
                || !_md._slotPassives.TryConsumeEcho(_md.CastSession.SlotQueryCastId, out float echoDelay, out float echoPower))
                return;

            _md.EnsureCoreServices();
            _md._slotPassiveRuntime.SchedulePassiveEcho(
                ctx,
                skill,
                motion.SlashCommitMult,
                _md.CastSession.ClosingChainBonus,
                echoDelay,
                echoPower,
                _md.CastSession.SlotQueryCastId,
                _md._clock.Director.WorldTimeMs);
        }

        public void ResolveImpactTarget(PendingClosing ctx)
        {
            Transform impactTarget = ctx.Target;
            if (impactTarget == null || impactTarget == _md._player || !_md.IsEnemyBody(impactTarget))
            {
                _md.CaptureBasicFacing();
                impactTarget = _md.CastFacingTarget;
            }
            if ((impactTarget == null || !_md.IsEnemyBody(impactTarget)) && _md._boss != null)
                impactTarget = _md._boss.transform;
            _md.FaceTarget(impactTarget);
            _basicImpactTarget = impactTarget;
        }

        public double WorldTimeMs() =>
            _md._clock != null ? _md._clock.Director.WorldTimeMs : 0;

        public bool BasicCadenceReady(double now) => _md.BasicCadenceReady(now);

        public void SetLastBasicStrikeMs(double now) => _md.SkillWorld.LastBasicStrikeMs = now;

        public int BasicHitsNow() => _md.BasicHitsNow();

        public float BasicStrikeReachM() =>
            _md.WeaponBasicReach(_md._combat.Manifestation.BasicStrikeRangeM);

        public bool IsBossInStrikeCapsule(PendingClosing ctx, float reachM)
        {
            LivingEffect logic = _closingLogic;
            return _md.IsBossInStrikeCapsule(logic, reachM);
        }

        public bool EvaluateBasicInReach(float reachM)
        {
            bool inReach = _md.BasicTargetStillInReach(_basicImpactTarget, reachM);
            if (!inReach && _md._boss != null && _basicImpactTarget != _md._boss.transform)
                inReach = _md.BasicTargetStillInReach(_md._boss.transform, reachM);
            return inReach;
        }

        public float BasicStrikeArcDeg() =>
            _md.HitMods(SkillResolution.Empty, true, false).ArcDeg;

        public float BasicStrikeYawDeg() => _md.BasicStrikeYawDeg(_basicImpactTarget);

        public bool HasLivingLogic(PendingClosing ctx) =>
            _closingLogic != null;

        public void ApplyBossClosingBasic(PendingClosing ctx) =>
            _md.ApplyBossClosingBasic(_closingLogic, ctx.Closing);

        public float ApplyBasicStrikeDamage(PendingClosing ctx, float effectScale) =>
            _md.ApplyClosingDamage(
                ctx.Closing,
                SkillResolution.Empty,
                isBasicStrike: true,
                slashCommitMult: 0f,
                effectScale: effectScale);

        public void ScheduleBasicSubHits(PendingClosing ctx, int hits, float reach) =>
            _md.ScheduleBasicSubHits(ctx.Closing, hits, reach);

        public void ApplyBasicExtras(float dealt, int hits) => _md.ApplyBasicExtras(dealt, hits);

        public void TryLandWeaponStunBasic() => _md.TryLandWeaponStun(SkillResolution.Empty, true);

        public void ApplyClosingStatuses(PendingClosing ctx, SkillResolution skill) =>
            _md.ApplyClosingStatuses(ctx, skill);

        public void ApplyClosingHeal(PendingClosing ctx, SkillResolution skill) =>
            _md.ApplyClosingHeal(ctx.Closing, skill);

        public void TryCannonBlast(PendingClosing ctx)
        {
            LivingEffect logic = _closingLogic;
            _md.TryCannonBlast(logic.TipX, logic.TipZ);
        }
    }
}
