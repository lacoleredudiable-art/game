using System;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;

namespace Dovus.App.Casting
{
    public readonly struct CastStarted
    {
        public CastStarted(string skillId) => SkillId = skillId ?? string.Empty;
        public string SkillId { get; }
    }

    public readonly struct BasicOutcome
    {
        public BasicOutcome(bool healed, bool deniedCadence, bool connected, float dealt, int hits)
        {
            Healed = healed;
            DeniedCadence = deniedCadence;
            Connected = connected;
            Dealt = dealt;
            Hits = hits;
        }

        public bool Healed { get; }
        public bool DeniedCadence { get; }
        public bool Connected { get; }
        public float Dealt { get; }
        public int Hits { get; }
    }

    public readonly struct CastOutcome
    {
        // Unity'de Dovus.App ayrı assembly: init için IsExternalInit polyfill'i yok (Core'unki internal) → ctor.
        public CastOutcome(
            string skillId,
            bool executorStarted,
            bool templateOwnsDelivery,
            float dealt,
            bool effectApplied,
            bool denied)
        {
            SkillId = skillId;
            ExecutorStarted = executorStarted;
            TemplateOwnsDelivery = templateOwnsDelivery;
            Dealt = dealt;
            EffectApplied = effectApplied;
            Denied = denied;
        }

        public string SkillId { get; }
        public bool ExecutorStarted { get; }
        public bool TemplateOwnsDelivery { get; }
        public float Dealt { get; }
        public bool EffectApplied { get; }
        public bool Denied { get; }
    }

    public sealed class CastPipeline
    {
        public event Action<CastStarted> Started;
        public event Action<CastOutcome> Completed;
        public event Action<BasicOutcome> BasicCompleted;

        public BasicOutcome RunBasic<TCtx>(TCtx ctx, IBasicStrikePort<TCtx> port)
        {
            port.ResolveImpactTarget(ctx);
            port.ResetClosingChainBonus();
            SkillResolution basicSkill = port.ResolveSkill(ctx);
            if (port.IsHealSkill(basicSkill))
            {
                port.ApplyClosingStatuses(ctx, basicSkill);
                port.ShoutSkill(basicSkill, ctx);
                port.ApplyClosingHeal(ctx, basicSkill);
                var healed = new BasicOutcome(true, false, false, 0f, 0);
                BasicCompleted?.Invoke(healed);
                return healed;
            }

            double basicNow = port.WorldTimeMs();
            if (!port.BasicCadenceReady(basicNow))
            {
                port.NoteDeniedCadence();
                var deniedCadence = new BasicOutcome(false, true, false, 0f, 0);
                BasicCompleted?.Invoke(deniedCadence);
                return deniedCadence;
            }

            port.SetLastBasicStrikeMs(basicNow);
            int basicHits = port.BasicHitsNow();
            float basicReach = port.BasicStrikeReachM();
            bool capsuleHit = port.IsBossInStrikeCapsule(ctx, basicReach);
            bool inReach = port.EvaluateBasicInReach(basicReach);
            float strikeArc = port.BasicStrikeArcDeg();
            float strikeDelta = port.BasicStrikeYawDeg();

            float basicDealt = 0f;
            bool connected = false;
            if (MeleeArc.StrikeConnects(capsuleHit, inReach, strikeDelta, strikeArc))
            {
                connected = true;
                if (port.HasLivingLogic(ctx))
                    port.ApplyBossClosingBasic(ctx);
                basicDealt = port.ApplyBasicStrikeDamage(
                    ctx, JsonEffectRules.BasicSubHitScale(basicHits));
                port.ScheduleBasicSubHits(ctx, basicHits, basicReach);
                port.ApplyBasicExtras(basicDealt, basicHits);
                port.TryLandWeaponStunBasic();
            }

            port.SpawnClosingImpact(ctx);
            if (port.HasLivingLogic(ctx))
                port.TryCannonBlast(ctx);

            var outcome = new BasicOutcome(false, false, connected, basicDealt, basicHits);
            BasicCompleted?.Invoke(outcome);
            return outcome;
        }

        public CastOutcome RunSkill<TCtx>(TCtx ctx, ICastPort<TCtx> port)
        {
            port.ResetClosingChainBonus();
            SkillResolution skill = port.ResolveSkill(ctx);
            if (skill.IsEmpty || !skill.IsComplete)
            {
                port.NoteDeniedNeedsTwoRunes();
                var denied = new CastOutcome(skill.SkillId, false, false, 0f, false, denied: true);
                Completed?.Invoke(denied);
                return denied;
            }

            port.NoteWeaponCast(skill);
            port.OpenSlotCast();
            try
            {
                WeaponSkillCompatibility compatibility = port.Compatibility(skill);
                port.PublishCompatibility(compatibility);
                if (port.ShouldArmPassive(compatibility))
                    port.TryTriggerPassive(ctx);

                port.ApplyResourceCost(skill);
                SkillMotionPlan motionPlan = port.ResolveMotion(skill);
                port.ApplyMotionIframe(skill, in motionPlan);
                bool templateOwnsDelivery = port.TryBeginMotionTemplate(skill, ctx);
                port.NoteSustainedCast(skill);
                port.NotifyCast(skill.SkillId);
                Started?.Invoke(new CastStarted(skill.SkillId));

                SkillExecutorRoute executorRoute = port.Route(skill);
                executorRoute = port.ApplyMechanicWorldRoute(skill, executorRoute);
                port.SetLastExecutorKind(executorRoute.Kind);
                port.ApplySelfCastEffects(skill);
                port.NoteJsonCast(skill, ctx);
                port.BeginMechanicPlan(ctx, skill);
                if (templateOwnsDelivery)
                    port.ArmTemplateDelivery(skill, ctx, in motionPlan);

                bool executorStarted = !templateOwnsDelivery
                    && executorRoute.Kind != SkillExecutorKind.Fallback
                    && port.TryLaunchExecutor(executorRoute.Kind, ctx, skill, in motionPlan);
                if (executorStarted)
                    port.ScheduleFollowUps(executorRoute.Kind, ctx, skill, in motionPlan);

                float dealt = 0f;
                if (!executorStarted && !templateOwnsDelivery)
                    dealt = port.ApplyFallbackDelivery(ctx, skill, in motionPlan, in executorRoute);

                port.ShoutSkill(skill, ctx);
                port.ApplyCooldown(skill, ctx, cosmeticIfDisabled: true);
                if (!motionPlan.IsEmpty)
                    port.AnnotateMotion(skill, in motionPlan);
                port.SpawnClosingImpact(ctx);
                port.SetLastResolvedSkillId(skill.SkillId);

                bool effectApplied = executorStarted
                    || templateOwnsDelivery
                    || dealt > 0f
                    || port.IsHealSkill(skill)
                    || !motionPlan.IsEmpty
                    || skill.Mechanics.Length > 0;
                port.SetLastSkillEffectApplied(effectApplied);

                if (string.Equals(skill.SkillId, "1-1", StringComparison.Ordinal))
                    port.LogSmokeOneOne(skill, effectApplied, dealt);

                port.TrySchedulePassiveEcho(ctx, skill, in motionPlan);

                var outcome = new CastOutcome(
                    skill.SkillId, executorStarted, templateOwnsDelivery, dealt, effectApplied, denied: false);
                Completed?.Invoke(outcome);
                return outcome;
            }
            finally
            {
                port.CloseSlotCast();
                port.ResetSlotQueryCastId();
            }
        }
    }
}
