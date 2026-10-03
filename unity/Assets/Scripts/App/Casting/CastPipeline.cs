using System;
using Dovus.Core.Combat;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;

namespace Dovus.App.Casting
{
    public readonly struct CastStarted
    {
        public CastStarted(string skillId) => SkillId = skillId ?? string.Empty;
        public string SkillId { get; }
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
