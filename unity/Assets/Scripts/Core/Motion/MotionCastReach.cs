using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    /// <summary>Kalıbın gerçek menzili ve kenardan kenara kapı.</summary>
    public static class MotionCastReach
    {
        public static float EdgeReachM(MotionTemplate template)
        {
            if (template == null)
                return 0f;
            float best = 0f;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                float travel = phase.Motion switch
                {
                    "lunge" or "dash" or "leap" or "pull" or "blink" or "return" or "slam" => phase.DistanceM,
                    "hop" => phase.Land == "behind"
                        ? Math.Max(phase.DistanceM, phase.BehindM)
                        : Math.Max(0f, phase.ForwardM),
                    "sidestep" => Math.Max(0f, phase.ForwardM),
                    "throw" => phase.ShotM,
                    "channel" => phase.DriftM,
                    _ => 0f
                };
                float hit = 0f;
                if (phase.Hit != null && phase.Hit.Payload != "none" && phase.Hit.Payload != "marker")
                {
                    hit = phase.Hit.Anchor is "forward" or "shot"
                        ? phase.Hit.LengthM
                        : phase.Hit.RadiusM;
                }
                if (phase.Land == "behind")
                    travel = Math.Max(travel, phase.DistanceM);
                best = Math.Max(best, travel + hit);
            }
            return best;
        }

        /// <summary>
        /// DistanceM saldıran merkezinden hedef yüzeyine ölçülür.
        /// Kapıya gövde yarıçapı eklenince karşılaştırma kenardan kenara olur.
        /// </summary>
        public static float GateRangeM(float edgeReachM, float attackerRadiusM) =>
            Math.Max(0f, edgeReachM) + Math.Max(0f, attackerRadiusM);

        /// <summary>
        /// Hedefe kapanan en uzun faz. Geri çekilme menzili uzatmaz.
        /// Vuruş, kapanıştan sonra gövdenin yeni kenarından ölçülür: menzil = JSON + kapanış.
        /// </summary>
        public static float ClosingApproachM(MotionTemplate template)
        {
            if (template == null)
                return 0f;
            float best = 0f;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionPhase phase = template.Phases[i];
                float step = phase.Motion switch
                {
                    "lunge" or "dash" or "pull" or "leap" or "slam" => phase.DistanceM,
                    "hop" or "sidestep" => Math.Max(0f, phase.ForwardM),
                    "blink" when phase.Land != "behind" => phase.DistanceM,
                    _ => 0f
                };
                if (step > best)
                    best = step;
            }
            return best;
        }

        public static float ComboEdgeReach(float jsonReachM, MotionTemplate template)
        {
            float json = Math.Max(0f, jsonReachM);
            float authored = EdgeReachM(template);
            return Math.Max(json + ClosingApproachM(template), authored);
        }

        /// <summary>
        /// Kendi üstündeki küre. Yarıçap menzil diye eklenince oyuncu yarıçapıyla iki kez sayılır
        /// ve kısa silah (yumruk/kalkan 1-4) vuruşun gerisinde durur.
        /// </summary>
        public static float SelfSphereRadiusM(MotionTemplate template)
        {
            if (template == null)
                return 0f;
            float best = 0f;
            for (int i = 0; i < template.Phases.Count; i++)
            {
                MotionHitSpec hit = template.Phases[i].Hit;
                if (hit == null || hit.Payload == "none" || hit.Payload == "marker")
                    continue;
                if (hit.Anchor is not ("self" or "ring"))
                    continue;
                if (hit.RadiusM > best)
                    best = hit.RadiusM;
            }
            return best;
        }

        /// <summary>
        /// 3 m başlangıçta kapanış metre. Kalıp kenarı JSON'dan büyükse ve planlanan durak
        /// hem JSON kenarını hem kendi küresini ıskalıyorsa metre yalnız JSON kenarındandır.
        /// JSON'u zaten yeten silah (kılıç 1-4) ek hamle almaz.
        /// </summary>
        public static float ApproachMeters(
            float centerDistM,
            float playerRadiusM,
            float targetRadiusM,
            float jsonEdgeM,
            MotionTemplate template)
        {
            float templateEdge = EdgeReachM(template);
            float closing = ClosingApproachM(template);
            float edge = Math.Max(Math.Max(0f, jsonEdgeM), templateEdge);
            float meters = CastApproach.Meters(
                centerDistM, playerRadiusM, targetRadiusM, edge, closing);
            float sphere = SelfSphereRadiusM(template);
            if (sphere <= MotionTemplateCatalogDefaults.MinDistM || jsonEdgeM <= MotionTemplateCatalogDefaults.MinDistM || jsonEdgeM + 0.001f >= edge)
                return meters;
            float stop = centerDistM - meters - closing;
            bool jsonHits = CenterInReach(stop, playerRadiusM, targetRadiusM, jsonEdgeM);
            bool sphereHits = stop <= sphere + Math.Max(0f, targetRadiusM) + MotionTemplateCatalogDefaults.HitStopEpsilonM;
            if (jsonHits || sphereHits)
                return meters;
            return CastApproach.Meters(
                centerDistM, playerRadiusM, targetRadiusM, jsonEdgeM, closing);
        }

        /// <summary>Merkez mesafesi, kenardan kenara JSON menziline sığıyor mu.</summary>
        public static bool CenterInReach(
            float centerDistM,
            float attackerRadiusM,
            float targetRadiusM,
            float edgeReachM) =>
            centerDistM - Math.Max(0f, attackerRadiusM) - Math.Max(0f, targetRadiusM)
            <= Math.Max(0f, edgeReachM) + MotionTemplateCatalogDefaults.HitStopEpsilonM;

        /// <summary>
        /// Geri adım cast başındaki menzili silmesin. İkisi de doluysa daha yakın olan sayılır.
        /// </summary>
        public static float CloserCenter(float currentCenterM, float castStartCenterM)
        {
            if (castStartCenterM <= MotionTemplateCatalogDefaults.MinDistM)
                return currentCenterM;
            if (currentCenterM <= MotionTemplateCatalogDefaults.MinDistM)
                return castStartCenterM;
            return Math.Min(currentCenterM, castStartCenterM);
        }
    }
}
