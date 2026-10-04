using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    public static class TemplateDelivery
    {
        const float EchoToleranceSec = 0.05f;

        public static TemplateDeliveryOrder Build(
            MechanicPlan plan,
            JsonValue engine,
            MotionTemplate template,
            MechanicRules rules,
            float fieldTickBaseSec) =>
            Build(plan, new SkillEngineModifiers(engine), template, rules, fieldTickBaseSec);

        public static TemplateDeliveryOrder Build(
            MechanicPlan plan,
            SkillEngineModifiers engine,
            MotionTemplate template,
            MechanicRules rules,
            float fieldTickBaseSec)
        {
            var order = new TemplateDeliveryOrder();

            bool actor = plan != null && plan.Effects.Exists(e => e.Stat is "aktor_yarat" or "klon");
            order.SpawnActors = actor;
            if (actor)
            {
                int count = engine.MinionCount(1);
                MechanicEffect actorEffect = plan.Effects.Find(e => e.Stat is "aktor_yarat" or "klon");
                if (actorEffect != null && actorEffect.Amount >= 1)
                    count = Math.Max(count, (int)Math.Round(actorEffect.Amount));
                order.ActorCount = Math.Max(1, count);
                float life = engine.MinionDurationSec(0f);
                if (life <= 0f && plan.Body.LifeSec > 0)
                    life = (float)plan.Body.LifeSec;
                if (life <= 0f)
                    life = TemplateDeliveryDefaults.DefaultActorDurationSec;
                life += Math.Max(0f, engine.LifetimeAdd(0f));
                order.ActorDurationSec = life;
                double hit = rules != null ? rules.Param("minion_hit_damage") : 6;
                order.ActorHitDamage = hit > 0 ? (float)hit : TemplateDeliveryDefaults.DefaultActorHitDamage;
            }

            bool rise = plan != null && plan.Body.Ramp;
            bool mark = plan != null && plan.Effects.Exists(e => e.Has("isaretli_an"));
            order.RiseDelay = rise;
            order.DelayedMark = mark;
            float delay = 0f;
            if (rise && rules != null)
                delay = Math.Max(delay, (float)rules.Param("rise_delay_sec"));
            if (mark && rules != null)
                delay = Math.Max(delay, (float)rules.Param("mark_delay_sec"));
            order.ActivationDelaySec = delay;

            order.OpeningPulse = !HasEffectHit(template);
            order.Homing = plan != null && plan.Body.Homing;

            bool duplicate = engine.DuplicateCast(false)
                || (plan != null && plan.Effects.Exists(e => e.Has("iki_kez")));
            float dupDelay = engine.DuplicateDelaySec(0f);
            if (dupDelay <= 0f && plan != null && plan.Body.CopyDelaySec > 0)
                dupDelay = (float)plan.Body.CopyDelaySec;
            order.DuplicateDelaySec = dupDelay > 0f ? dupDelay : TemplateDeliveryDefaults.DefaultDuplicateDelaySec;
            // Kopya ilk vuruştan sayılır. Kalıp o anda zaten vuruyorsa yankı kalıptadır.
            float firstHit = FirstEffectHitSec(template);
            order.DuplicateAtSec = Math.Max(0f, firstHit) + order.DuplicateDelaySec;
            order.Duplicate = duplicate
                && !(firstHit >= 0f && HasEffectHitNear(template, order.DuplicateAtSec, EchoToleranceSec));
            order.DuplicateDamageMult = engine.HasDuplicateDamageMult
                ? engine.DuplicateDamageMult(1f)
                : (plan != null && plan.Body.ChainMult > 0 ? (float)plan.Body.ChainMult : 1f);

            order.RepeatPrevious = plan != null && plan.Effects.Exists(e => e.Stat == "onceki_skill_tekrar");

            int bounces = engine.BounceTargets(0);
            if (plan != null && plan.Body.Chain > bounces)
                bounces = plan.Body.Chain;
            order.BounceExtra = Math.Max(0, bounces - 1);
            order.BounceDamageMult = engine.HasBounceDamageMult
                ? engine.BounceDamageMult(1f)
                : (plan != null ? (float)plan.Body.ChainMult : 1f);

            bool akis = plan != null && plan.Effects.Exists(e => e.Has("akis"));
            bool templateTicks = TemplateHasEvery(template);
            order.FieldTicks = akis && !templateTicks;
            float channel = engine.ChannelSec(0f);
            if (channel <= 0f && plan != null)
                channel = (float)plan.Body.LifeSec;
            order.FieldDurationSec = channel;
            float rate = Math.Max(TemplateDeliveryDefaults.MinTickSec, engine.TickRateMult(1f));
            float tickBase = fieldTickBaseSec > TemplateDeliveryDefaults.MinFieldDurationSec ? fieldTickBaseSec : 1f;
            order.FieldTickSec = tickBase / rate;
            float fraction = rules != null ? (float)rules.Param("flow_tick_fraction") : TemplateDeliveryDefaults.DefaultFlowTickFraction;
            order.FieldTickFraction = fraction > 0f ? fraction : TemplateDeliveryDefaults.DefaultFlowTickFraction;

            order.GlideHaste = plan != null && plan.Effects.Exists(e => e.Has("suzulme"));
            if (order.GlideHaste && rules != null)
            {
                order.GlideMagnitude = (float)rules.Param("glide_speed_mult");
                if (order.GlideMagnitude <= 1f)
                    order.GlideMagnitude = TemplateDeliveryDefaults.DefaultGlideMagnitude;
                order.GlideDurationSec = channel > 0f ? channel : TemplateDeliveryDefaults.DefaultGlideDurationSec;
            }

            order.CanDrain = plan != null && plan.Effects.Exists(e => e.Has("can_emen") || e.Stat == "em");
            if (plan != null)
            {
                MechanicEffect em = plan.Effects.Find(e => e.Stat == "em");
                if (em != null && em.Amount > 0)
                    order.AbsorbRatio = (float)em.Amount;
            }

            // Çeken plan boss'u iterse çekme ile itme çarpışır; çekme kazanır.
            bool pulls = plan != null && (plan.Body.Pull || plan.Effects.Exists(e => e.Stat == "cek"));
            order.BossKnockback = !pulls && plan != null && plan.Effects.Exists(e =>
                e.Target == "dusman" && (e.Stat is "can" or "it" or "tempo" or "hareket"));
            order.PushM = JsonEffectRules.PushMeters(plan, rules);
            order.Pincer = JsonEffectRules.WantsPincer(plan) && EffectHitCount(template) < 2;
            if (order.Pincer)
            {
                float pincerDelay = rules != null && rules.Param("pincer_delay_sec") > 0 ? (float)rules.Param("pincer_delay_sec") : TemplateDeliveryDefaults.DefaultPincerDelaySec;
                order.PincerAtSec = Math.Max(0f, firstHit) + pincerDelay;
                order.PincerShare = rules != null && rules.Param("pincer_second_share") > 0 ? (float)rules.Param("pincer_second_share") : 0.5f;
            }
            return order;
        }

        /// <summary>İlk etki vuruşunun kalıp başından saniyesi; yoksa -1.</summary>
        public static float FirstEffectHitSec(MotionTemplate template)
        {
            if (template == null)
                return -1f;
            float start = 0f;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                MotionHitSpec hit = phase.Hit;
                if (hit != null && hit.Payload is not ("none" or "marker"))
                    return start + phase.DurationSec * hit.At;
                start += phase.DurationSec;
            }
            return -1f;
        }

        static bool HasEffectHitNear(MotionTemplate template, float atSec, float tolSec)
        {
            float start = 0f;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                MotionHitSpec hit = phase.Hit;
                if (hit != null && hit.Payload is not ("none" or "marker")
                    && Math.Abs(start + phase.DurationSec * hit.At - atSec) <= tolSec)
                    return true;
                start += phase.DurationSec;
            }
            return false;
        }

        public static int EffectHitCount(MotionTemplate template)
        {
            if (template == null)
                return 0;
            int n = 0;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionHitSpec hit = template.Phases[i].Hit;
                if (hit != null && hit.Payload is not ("none" or "marker"))
                    n++;
            }
            return n;
        }

        public static bool HasEffectHit(MotionTemplate template)
        {
            if (template == null)
                return false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionHitSpec hit = template.Phases[i].Hit;
                if (hit == null)
                    continue;
                if (hit.Payload is "none" or "marker")
                    continue;
                return true;
            }
            return false;
        }

        static bool TemplateHasEvery(MotionTemplate template)
        {
            if (template == null)
                return false;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionHitSpec hit = template.Phases[i].Hit;
                if (hit != null && hit.EverySec > TemplateDeliveryDefaults.MinTickSec)
                    return true;
            }
            return false;
        }
    }
}
