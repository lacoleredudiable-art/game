using System;
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
using Dovus.Core.Shared;

namespace Dovus.App.Casting
{
    public sealed class CastPipeline
    {
        public event Action<CastDenialRequested> DenialRequested;
        public event Action<CastCompatibilityPublished> CompatibilityPublished;
        public event Action<CastSkillShoutRequested> SkillShoutRequested;
        public event Action<CastMotionAnnotationRequested> MotionAnnotationRequested;

        public BasicOutcome RunBasic<TCtx>(TCtx ctx, IBasicStrikePort<TCtx> port)
        {
            port.ResolveImpactTarget(ctx);
            port.ResetClosingChainBonus();
            SkillResolution basicSkill = port.ResolveSkill(ctx);
            if (port.IsHealSkill(basicSkill))
            {
                port.ApplyClosingStatuses(ctx, basicSkill);
                SkillShoutRequested?.Invoke(new CastSkillShoutRequested(basicSkill, ctx));
                port.ApplyClosingHeal(ctx, basicSkill);
                return new BasicOutcome(true, false, false, 0f, 0);
            }

            double basicNow = port.WorldTimeMs();
            if (!port.BasicCadenceReady(basicNow))
            {
                DenialRequested?.Invoke(new CastDenialRequested(CastDenialReason.BasicCadenceNotReady));
                return new BasicOutcome(false, true, false, 0f, 0);
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

            return new BasicOutcome(false, false, connected, basicDealt, basicHits);
        }

        public CastOutcome RunSkill<TCtx>(TCtx ctx, ICastPort<TCtx> port)
        {
            port.ResetClosingChainBonus();
            SkillResolution skill = port.ResolveSkill(ctx);
            if (skill.IsEmpty || !skill.IsComplete)
            {
                DenialRequested?.Invoke(new CastDenialRequested(CastDenialReason.NeedsTwoRunes));
                return new CastOutcome((SkillId)skill.Identity.Id, false, false, 0f, false, denied: true);
            }

            port.NoteWeaponCast(skill);
            port.OpenSlotCast();
            try
            {
                WeaponSkillCompatibility compatibility = port.Compatibility(skill);
                CompatibilityPublished?.Invoke(new CastCompatibilityPublished(compatibility));
                if (port.ShouldArmPassive(compatibility))
                    port.TryTriggerPassive(ctx);

                port.ApplyResourceCost(skill);
                SkillMotionPlan motionPlan = port.ResolveMotion(skill);
                port.ApplyMotionIframe(skill, in motionPlan);
                bool templateOwnsDelivery = port.TryBeginMotionTemplate(skill, ctx);
                port.NoteSustainedCast(skill);
                var resolvedId = (SkillId)skill.Identity.Id;
                port.NotifyCast(resolvedId);

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

                SkillShoutRequested?.Invoke(new CastSkillShoutRequested(skill, ctx));
                port.ApplyCooldown(skill, ctx, cosmeticIfDisabled: true);
                if (!motionPlan.IsEmpty)
                    MotionAnnotationRequested?.Invoke(new CastMotionAnnotationRequested(skill, in motionPlan));
                port.SpawnClosingImpact(ctx);
                port.SetLastResolvedSkillId(resolvedId);

                bool effectApplied = executorStarted
                    || templateOwnsDelivery
                    || dealt > 0f
                    || port.IsHealSkill(skill)
                    || !motionPlan.IsEmpty
                    || skill.Mechanics.Length > 0;
                port.SetLastSkillEffectApplied(effectApplied);

                port.TrySchedulePassiveEcho(ctx, skill, in motionPlan);

                return new CastOutcome(
                    resolvedId, executorStarted, templateOwnsDelivery, dealt, effectApplied, denied: false);
            }
            finally
            {
                port.CloseSlotCast();
                port.ResetSlotQueryCastId();
            }
        }
    }
}
