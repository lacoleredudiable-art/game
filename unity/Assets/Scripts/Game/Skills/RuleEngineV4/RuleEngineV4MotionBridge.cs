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
            float sec = Mathf.Max(CastApproach.MinSec, distanceM / Mathf.Max(0.01f, speedMps));
            var phase = new MotionPhase(
                id,
                motionKind,
                sec,
                "target",
                "track",
                string.Empty,
                0f,
                distanceM,
                0f,
                1f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                0f,
                null,
                null);
            return new MotionTemplate("v4_" + id, id, 0, "v4", true, new[] { phase });
        }

        public static IEnumerator CoRun(
            MotionTemplateBodyHost body,
            KinematicMotorController motor,
            MotionTemplate template,
            Func<MotionTarget> target,
            float bodyRadiusM)
        {
            if (body == null || template == null)
                yield break;
            float incoming = motor != null ? motor.Velocity.magnitude : 0f;
            float dist = template.Phases.Count > 0 ? template.Phases[0].DistanceM : 0f;
            float baseSpeed = dist / Mathf.Max(CastApproach.MinSec, template.Phases[0].DurationSec);
            float speed = RuleEngineV4MotionHandoff.EffectiveSpeedMps(baseSpeed, incoming);
            float meters = template.Phases[0].DistanceM;
            string motion = template.Phases[0].Motion;
            template = BuildLinearMove(template.Id, motion, meters, speed);

            body.CancelToGround();
            body.Play(template, target, () => false, null, bodyRadiusM);
            while (body.IsDisplacing)
                yield return null;
        }
    }
}
