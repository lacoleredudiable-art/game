using Dovus.Core.Casting;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4WorldPhysicsUtil
    {
        static readonly RaycastHit[] Hits = new RaycastHit[16];

        public static float BodyRadius(Transform t)
        {
            if (t == null)
                return RuleEngineV4PhysicsDefaults.SliceMinionFallbackRadiusM;
            if (t.TryGetComponent(out RuleEngineV4PhysicsBodyHost body))
                return body.BodyRadiusM;
            if (t.TryGetComponent(out TargetableHost mark))
                return mark.MarkerRadius(0f);
            if (t.TryGetComponent(out KinematicMotorController motor))
                return motor.BodyRadiusM;
            return RuleEngineV4PhysicsDefaults.SliceMinionFallbackRadiusM;
        }

        public static RuleEngineV4WeightTier Weight(Transform t)
        {
            if (t != null && t.TryGetComponent(out RuleEngineV4PhysicsBodyHost body))
                return body.WeightTier;
            return RuleEngineV4WeightTier.Medium;
        }

        public static float EdgeDistance(Vector3 from, Transform target)
        {
            if (target == null)
                return float.MaxValue;
            TargetableHost mark = target.GetComponentInParent<TargetableHost>();
            if (mark != null)
                return mark.DistanceFrom(from);
            float r = BodyRadius(target);
            Vector3 flat = target.position - from;
            flat.y = 0f;
            return Mathf.Max(0f, flat.magnitude - r);
        }

        public static bool InMeleeReach(Vector3 from, Transform target, float casterR, float reachM)
        {
            TargetableHost mark = target != null ? target.GetComponentInParent<TargetableHost>() : null;
            float dist = mark != null ? mark.DistanceFrom(from) : EdgeDistance(from, target) + casterR;
            return StrikeCapsule.EdgeInReach(dist, casterR, reachM);
        }

        public static Vector3 MoveWithWalls(Vector3 from, Vector3 to, float radiusM, Transform ignore)
        {
            if (ignore != null && ignore.TryGetComponent(out KinematicMotorController motor))
                return motor.Tuning != null
                    ? SweepAndSlide(from, to, radiusM)
                    : to;
            return SweepAndSlide(from, to, radiusM);
        }

        static Vector3 SweepAndSlide(Vector3 from, Vector3 to, float radiusM)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < 0.0001f)
                return from;
            Vector3 dir = delta / dist;
            float radius = Mathf.Max(KinematicMotorControllerDefaults.CapsuleRadiusFloorM, radiusM);
            Vector3 p1 = from + Vector3.up * (radius + KinematicMotorControllerDefaults.CapsuleProbeLiftM);
            Vector3 p2 = from + Vector3.up * KinematicMotorControllerDefaults.CapsuleTopLiftM;
            int hits = Physics.CapsuleCastNonAlloc(
                p1, p2, radius, dir, Hits, dist, ~0, QueryTriggerInteraction.Ignore);
            if (hits <= 0)
                return to;
            float best = dist;
            Vector3 bestNormal = Vector3.zero;
            for (int i = 0; i < hits; i++)
            {
                if (Hits[i].collider == null)
                    continue;
                if (Hits[i].distance < best)
                {
                    best = Hits[i].distance;
                    bestNormal = Hits[i].normal;
                }
            }
            Vector3 stop = from + dir * Mathf.Max(0f, best - KinematicMotorControllerDefaults.MoveStopInsetM);
            bestNormal.y = 0f;
            if (bestNormal.sqrMagnitude < 0.0001f)
                return stop;
            bestNormal.Normalize();
            float remain = dist - best;
            if (remain <= 0.001f)
                return stop;
            Vector3 slide = Vector3.ProjectOnPlane(dir * remain, bestNormal);
            return stop + slide;
        }
    }
}
