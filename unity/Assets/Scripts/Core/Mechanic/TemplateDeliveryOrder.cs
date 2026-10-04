using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
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
        /// <summary>konum:it>dusman itme mesafesi (0 = genel BossKnockbackM).</summary>
        public float PushM { get; set; }
        public bool Pincer { get; set; }
        public float PincerAtSec { get; set; }
        public float PincerShare { get; set; }

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
                    Math.Max(TemplateDeliveryDefaults.MinBeatDelaySec, ActivationDelaySec),
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
                    Math.Max(TemplateDeliveryDefaults.MinBeatDelaySec, DuplicateAtSec > 0f ? DuplicateAtSec : DuplicateDelaySec),
                    DeliveryBeatKind.Duplicate,
                    DuplicateDamageMult <= 0f ? 1f : DuplicateDamageMult,
                    1));
            }

            if (RepeatPrevious)
            {
                beats.Add(new DeliveryBeat(
                    Math.Max(TemplateDeliveryDefaults.MinBeatDelaySec, DuplicateDelaySec),
                    DeliveryBeatKind.RepeatPrevious,
                    1f,
                    1));
            }

            for (int i = 0; i < BounceExtra; i++)
            {
                beats.Add(new DeliveryBeat(
                    TemplateDeliveryDefaults.BounceBeatDelaySec * (i + 1),
                    DeliveryBeatKind.Bounce,
                    BounceDamageMult <= 0f ? 1f : BounceDamageMult,
                    1));
            }

            if (Pincer)
                beats.Add(new DeliveryBeat(Math.Max(TemplateDeliveryDefaults.MinBeatDelaySec, PincerAtSec), DeliveryBeatKind.Pincer, PincerShare > 0f ? PincerShare : 0.5f, 1));

            if (FieldTicks && FieldTickSec > TemplateDeliveryDefaults.MinFieldTickSec && FieldDurationSec > TemplateDeliveryDefaults.MinFieldDurationSec)
            {
                float fraction = FieldTickFraction > 0f ? FieldTickFraction : TemplateDeliveryDefaults.DefaultFlowTickFraction;
                for (float t = FieldTickSec; t <= FieldDurationSec + 0.001f; t += FieldTickSec)
                    beats.Add(new DeliveryBeat(t, DeliveryBeatKind.FieldTick, fraction, 1));
            }

            return beats;
        }
    }
}
