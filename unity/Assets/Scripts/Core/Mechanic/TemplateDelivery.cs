using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;

namespace Dovus.Core.Mechanic
{
    public enum DeliveryBeatKind
    {
        SpawnActors = 1,
        Resolve = 2,
        Detonate = 3,
        Duplicate = 4,
        RepeatPrevious = 5,
        FieldTick = 6,
        Bounce = 7,
        GlideHaste = 8
    }

    /// <summary>Kalıp başladıktan sonra çalışacak tek teslim adımı. Skill kimliği yok.</summary>
    public readonly struct DeliveryBeat
    {
        public DeliveryBeat(double atSec, DeliveryBeatKind kind, float power, int count)
        {
            AtSec = atSec;
            Kind = kind;
            Power = power;
            Count = count;
        }

        public double AtSec { get; }
        public DeliveryBeatKind Kind { get; }
        public float Power { get; }
        public int Count { get; }
    }

    /// <summary>
    /// Hareket kalıbı vuruşu teslimi bitirmez. Gramer planı ve motor anahtarları
    /// aynı kuyruğu kurar: çağrı, yankı, gecikme, sekme, akış tiki.
    /// </summary>
    public sealed class TemplateDeliveryOrder
    {
        public bool SpawnActors { get; set; }
        public int ActorCount { get; set; }
        public float ActorDurationSec { get; set; }
        public float ActorHitDamage { get; set; }
        public float ActivationDelaySec { get; set; }
        public bool DelayedMark { get; set; }
        public bool RiseDelay { get; set; }
        public bool OpeningPulse { get; set; }
        public bool Duplicate { get; set; }
        public float DuplicateDelaySec { get; set; }
        /// <summary>Kopya nabzı kalıp başından: ilk etki vuruşu + DuplicateDelaySec.</summary>
        public float DuplicateAtSec { get; set; }
        public float DuplicateDamageMult { get; set; }
        public bool RepeatPrevious { get; set; }
        public int BounceExtra { get; set; }
        public float BounceDamageMult { get; set; }
        public bool Homing { get; set; }
        public bool FieldTicks { get; set; }
        public float FieldDurationSec { get; set; }
        public float FieldTickSec { get; set; }
        public float FieldTickFraction { get; set; }
        public bool GlideHaste { get; set; }
        public float GlideMagnitude { get; set; }
        public float GlideDurationSec { get; set; }
        public bool CanDrain { get; set; }
        public float AbsorbRatio { get; set; }
        public bool BossKnockback { get; set; }

        public List<DeliveryBeat> Schedule()
        {
            var beats = new List<DeliveryBeat>();
            if (SpawnActors)
            {
                beats.Add(new DeliveryBeat(
                    ActivationDelaySec,
                    DeliveryBeatKind.SpawnActors,
                    ActorHitDamage,
                    Math.Max(1, ActorCount)));
            }

            if (OpeningPulse)
            {
                beats.Add(new DeliveryBeat(ActivationDelaySec, DeliveryBeatKind.Resolve, 1f, 1));
            }
            else if (DelayedMark || RiseDelay)
            {
                beats.Add(new DeliveryBeat(
                    Math.Max(0.05, ActivationDelaySec),
                    DeliveryBeatKind.Detonate,
                    1f,
                    1));
            }

            if (GlideHaste)
            {
                beats.Add(new DeliveryBeat(0, DeliveryBeatKind.GlideHaste, GlideMagnitude, 1));
            }

            if (Duplicate)
            {
                beats.Add(new DeliveryBeat(
                    Math.Max(0.05, DuplicateAtSec > 0f ? DuplicateAtSec : DuplicateDelaySec),
                    DeliveryBeatKind.Duplicate,
                    DuplicateDamageMult <= 0f ? 1f : DuplicateDamageMult,
                    1));
            }

            if (RepeatPrevious)
            {
                beats.Add(new DeliveryBeat(
                    Math.Max(0.05, DuplicateDelaySec),
                    DeliveryBeatKind.RepeatPrevious,
                    1f,
                    1));
            }

            for (int i = 0; i < BounceExtra; i++)
            {
                beats.Add(new DeliveryBeat(
                    0.2 * (i + 1),
                    DeliveryBeatKind.Bounce,
                    BounceDamageMult <= 0f ? 1f : BounceDamageMult,
                    1));
            }

            if (FieldTicks && FieldTickSec > 0.02f && FieldDurationSec > 0.05f)
            {
                float fraction = FieldTickFraction > 0f ? FieldTickFraction : 0.33f;
                for (float t = FieldTickSec; t <= FieldDurationSec + 0.001f; t += FieldTickSec)
                    beats.Add(new DeliveryBeat(t, DeliveryBeatKind.FieldTick, fraction, 1));
            }

            return beats;
        }
    }

    public static class TemplateDelivery
    {
        const float EchoToleranceSec = 0.05f;

        public static TemplateDeliveryOrder Build(
            MechanicPlan plan,
            JsonValue engine,
            MotionTemplate template,
            MechanicRules rules,
            float fieldTickBaseSec)
        {
            var order = new TemplateDeliveryOrder();
            if (engine.IsNull)
                engine = default;

            bool actor = plan != null && plan.Effects.Exists(e => e.Stat is "aktor_yarat" or "klon");
            order.SpawnActors = actor;
            if (actor)
            {
                int count = engine["minion_count"].AsInt(1);
                MechanicEffect actorEffect = plan.Effects.Find(e => e.Stat is "aktor_yarat" or "klon");
                if (actorEffect != null && actorEffect.Amount >= 1)
                    count = Math.Max(count, (int)Math.Round(actorEffect.Amount));
                order.ActorCount = Math.Max(1, count);
                float life = engine["minion_duration_sec"].AsFloat(0f);
                if (life <= 0f && plan.Body.LifeSec > 0)
                    life = (float)plan.Body.LifeSec;
                if (life <= 0f)
                    life = 5f;
                life += Math.Max(0f, engine["lifetime_add"].AsFloat(0f));
                order.ActorDurationSec = life;
                double hit = rules != null ? rules.Param("minion_hit_damage") : 6;
                order.ActorHitDamage = hit > 0 ? (float)hit : 6f;
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

            bool duplicate = engine["duplicate_cast"].AsBool(false)
                || (plan != null && plan.Effects.Exists(e => e.Has("iki_kez")));
            float dupDelay = engine["duplicate_delay_sec"].AsFloat(0f);
            if (dupDelay <= 0f && plan != null && plan.Body.CopyDelaySec > 0)
                dupDelay = (float)plan.Body.CopyDelaySec;
            order.DuplicateDelaySec = dupDelay > 0f ? dupDelay : 0.3f;
            // Kopya ilk vuruştan sayılır. Kalıp o anda zaten vuruyorsa yankı kalıptadır.
            float firstHit = FirstEffectHitSec(template);
            order.DuplicateAtSec = Math.Max(0f, firstHit) + order.DuplicateDelaySec;
            order.Duplicate = duplicate
                && !(firstHit >= 0f && HasEffectHitNear(template, order.DuplicateAtSec, EchoToleranceSec));
            order.DuplicateDamageMult = engine.Has("duplicate_damage_mult")
                ? engine["duplicate_damage_mult"].AsFloat(1f)
                : (plan != null && plan.Body.ChainMult > 0 ? (float)plan.Body.ChainMult : 1f);

            order.RepeatPrevious = plan != null && plan.Effects.Exists(e => e.Stat == "onceki_skill_tekrar");

            int bounces = engine["bounce_targets"].AsInt(0);
            if (plan != null && plan.Body.Chain > bounces)
                bounces = plan.Body.Chain;
            order.BounceExtra = Math.Max(0, bounces - 1);
            order.BounceDamageMult = engine.Has("bounce_damage_mult")
                ? engine["bounce_damage_mult"].AsFloat(1f)
                : (plan != null ? (float)plan.Body.ChainMult : 1f);

            bool akis = plan != null && plan.Effects.Exists(e => e.Has("akis"));
            bool templateTicks = TemplateHasEvery(template);
            order.FieldTicks = akis && !templateTicks;
            float channel = engine["channel_sec"].AsFloat(0f);
            if (channel <= 0f && plan != null)
                channel = (float)plan.Body.LifeSec;
            order.FieldDurationSec = channel;
            float rate = Math.Max(0.01f, engine["tick_rate_mult"].AsFloat(1f));
            float tickBase = fieldTickBaseSec > 0.05f ? fieldTickBaseSec : 1f;
            order.FieldTickSec = tickBase / rate;
            float fraction = rules != null ? (float)rules.Param("flow_tick_fraction") : 0.33f;
            order.FieldTickFraction = fraction > 0f ? fraction : 0.33f;

            order.GlideHaste = plan != null && plan.Effects.Exists(e => e.Has("suzulme"));
            if (order.GlideHaste && rules != null)
            {
                order.GlideMagnitude = (float)rules.Param("glide_speed_mult");
                if (order.GlideMagnitude <= 1f)
                    order.GlideMagnitude = 1.5f;
                order.GlideDurationSec = channel > 0f ? channel : 3f;
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
                if (hit != null && hit.EverySec > 0.01f)
                    return true;
            }
            return false;
        }
    }

    /// <summary>Minyon ve klon boss'un ve oyuncunun gövdesinin dışında durur.</summary>
    public static class ActorSpacing
    {
        public static void PushOutside(ref float x, ref float z, float ox, float oz, float minSep)
        {
            if (minSep <= 0f)
                return;
            float dx = x - ox;
            float dz = z - oz;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist >= minSep)
                return;
            if (dist < 0.0001f)
            {
                x = ox + minSep;
                z = oz;
                return;
            }

            float scale = minSep / dist;
            x = ox + dx * scale;
            z = oz + dz * scale;
        }
    }
}
