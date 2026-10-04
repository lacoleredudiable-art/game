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
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills;
using Dovus.Game.Team;
using UnityEngine;

namespace Dovus.Game.Skills.Hosts
{
public sealed class MdCastPort : ICastPort<PendingClosing>, IBasicStrikePort<PendingClosing>
    {
        readonly ManifestationDirector _md;
        Transform _basicImpactTarget;
        LivingEffect _closingLogic;

        /// <summary>FireClosing başında yakalanan logic (eski koddaki yerel değişkenle aynı örnek).</summary>
        internal void BeginClosing(LivingEffect logic) => _closingLogic = logic;

        internal MdCastPort(ManifestationDirector md) => _md = md;

        public void ResetClosingChainBonus() => _md.CastSession.ClosingChainBonus = 1f;

        public SkillResolution ResolveSkill(PendingClosing ctx) => _md.ResolvePendingSkill(ctx);

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

        public bool TryBeginMotionTemplate(SkillResolution skill, PendingClosing ctx) =>
            _md.TryBeginMotionTemplate(skill, ctx);

        public void NoteSustainedCast(SkillResolution skill) => _md.NoteSustainedCast(skill);

        public void NotifyCast(SkillId skillId) => _md.TeamHub.NotifyCast(skillId);

        public SkillExecutorRoute Route(SkillResolution skill) =>
            _md._skillExecutorRouter.Route(skill, _md._equippedWeapon);

        public SkillExecutorRoute ApplyMechanicWorldRoute(
            SkillResolution skill,
            SkillExecutorRoute route) =>
            _md.ApplyMechanicWorldRoute(_md.MechanicPlanFor(skill), route);

        public void SetLastExecutorKind(SkillExecutorKind kind) => _md.LastExecutorKind = kind;

        public void ApplySelfCastEffects(SkillResolution skill) => _md.ApplySelfCastEffects(skill);

        public void NoteJsonCast(SkillResolution skill, PendingClosing ctx) =>
            _md.NoteJsonCast(skill, ctx.Closing);

        public void BeginMechanicPlan(PendingClosing ctx, SkillResolution skill)
        {
            LivingEffect logic = _closingLogic;
            if (logic == null)
                return;
            _md.BeginMechanicPlan(
                skill,
                new Vector3(logic.DirX, 0f, logic.DirZ),
                new Vector3(logic.TipX, _md._player.position.y, logic.TipZ));
        }

        public void ArmTemplateDelivery(
            SkillResolution skill,
            PendingClosing ctx,
            in SkillMotionPlan motion) =>
            _md.ArmTemplateDelivery(skill, ctx, motion);

        public bool TryLaunchExecutor(
            SkillExecutorKind kind,
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion)
        {
            _md.EnsureLaunchServices();
            return _md._executorLauncher.TryLaunch(kind, ctx, skill, motion);
        }

        public void ScheduleFollowUps(
            SkillExecutorKind kind,
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion) =>
            _md.ScheduleFollowUpLaunches(kind, ctx, skill, motion);

        public float ApplyFallbackDelivery(
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            in SkillExecutorRoute route)
        {
            LivingEffect logic = _closingLogic;
            if (logic == null)
                return 0f;

            if (route.IsStub)
                DebugConfig.DevLog($"[SkillExecutor] stub → LivingEffect: {route.Reason}");
            _md.LastExecutorKind = SkillExecutorKind.Fallback;
            _md.ApplyBossClosing(logic, ctx.Closing, skill);
            bool bossReached = _md._boss != null && _md.IsClosingInRange(logic, ctx.Closing);
            float dealt = 0f;
            if (bossReached)
            {
                dealt = _md.ApplyClosingDamage(
                    ctx.Closing,
                    skill,
                    isBasicStrike: false,
                    motion.SlashCommitMult);
            }

            _md.ApplyClosingStatuses(ctx, skill, bossReached);
            if (bossReached)
                _md.ApplyMechanicHitEffects(_md.LastMechanicPlan, new Vector3(logic.TipX, 0f, logic.TipZ));
            _md.ApplyClosingHeal(ctx.Closing, skill);
            return dealt;
        }

        public void ApplyCooldown(SkillResolution skill, PendingClosing ctx, bool cosmeticIfDisabled)
        {
            _md.EnsureLaunchServices();
            _md._castSideEffects.ApplyCooldown(skill, ctx.Words, cosmeticIfDisabled);
        }

        public void SpawnClosingImpact(PendingClosing ctx) => _md.SpawnClosingImpact(ctx);

        public void SetLastResolvedSkillId(SkillId skillId) => _md.LastResolvedSkillId = skillId.Value;

        public bool IsHealSkill(SkillResolution skill) => _md.CastPortIsHealSkill(skill);

        public void SetLastSkillEffectApplied(bool applied) => _md.LastSkillEffectApplied = applied;

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
