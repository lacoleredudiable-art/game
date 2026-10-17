using System;
using System.Collections;
using Dovus.Core.Motion;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Konumu yalnız MotionTemplateRunner taşır; yeni hareket öncekini keser.</summary>
    public static class RuleEngineV4MotionBridge
    {
        public static MotionTemplate BuildLinearMove(string id, string motionKind, float distanceM, float speedMps)
        {
            float sec = Mathf.Max(CastApproach.MinSec, distanceM / Mathf.Max(PositionOwnershipDefaults.MinDistM, speedMps));
            var phase = new MotionPhase(
                id,
                motionKind,
                sec,
                "target",
                "track",
                string.Empty,
                0,
                distanceM,
                0,
                1,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                null,
                null);
            return new MotionTemplate("v4_" + id, id, 0, "v4", true, new[] { phase });
        }

        public static IEnumerator CoRun(
            MotionTemplateBodyHost body,
            KinematicMotorController motor,
            MotionTemplate template,
            Func<MotionTarget> target,
            float bodyRadiusM,
            float stopGapM)
        {
            if (body == null || template == null)
                yield break;
            float dist = template.Phases.Count > 0 ? template.Phases[0].DistanceM : 0;
            float baseSpeed = dist / Mathf.Max(CastApproach.MinSec, template.Phases[0].DurationSec);
            // Koşu hızı devredilmez: uçuş/atılış kendi komut hızıyla gider (koşarken 3-9 ~17 m/s'ye çıkıyordu).
            float speed = baseSpeed;
            float meters = template.Phases[0].DistanceM;
            string motion = template.Phases[0].Motion;
            template = BuildLinearMove(template.Id, motion, meters, speed);

            body.CancelToGround();
            body.Play(template, target, () => false, null, bodyRadiusM, stopGapM);
            while (body.IsDisplacing)
                yield return null;
        }
    }
}
