using System.Collections.Generic;
using Dovus.Core.Casting;
using Dovus.Core.Motion;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Unity PhysX + gövde ölçümü (instance facade — mutable static ratchet).</summary>
    public sealed class RuleEngineV4PhysicsServices
    {
        readonly RaycastHit[] _capsuleHits = new RaycastHit[16];
        readonly Collider[] _hitBuffer = new Collider[24];
        readonly RaycastHit[] _castHits = new RaycastHit[64];

        public float BodyRadius(Transform t)
        {
            if (t == null)
                return RuleEngineV4UnitySceneDefaults.SliceMinionFallbackRadiusM;
            if (t.TryGetComponent(out RuleEngineV4PhysicsBodyHost body))
                return body.BodyRadiusM;
            if (t.TryGetComponent(out TargetableHost mark))
                return mark.MarkerRadius(0);
            if (t.TryGetComponent(out KinematicMotorController motor))
                return motor.BodyRadiusM;
            return RuleEngineV4UnitySceneDefaults.SliceMinionFallbackRadiusM;
        }

        public RuleEngineV4WeightTier Weight(Transform t)
        {
            if (t != null && t.TryGetComponent(out RuleEngineV4PhysicsBodyHost body))
                return body.WeightTier;
            return RuleEngineV4WeightTier.Medium;
        }

        public float EdgeDistance(Vector3 from, Transform body)
        {
            if (body == null)
                return float.MaxValue;
            TargetableHost mark = body.GetComponentInParent<TargetableHost>();
            if (mark != null)
                return mark.DistanceFrom(from);
            float r = BodyRadius(body);
            Vector3 flat = body.position - from;
            flat.y = 0;
            return Mathf.Max(0, flat.magnitude - r);
        }

        public bool InMeleeReach(Vector3 from, Transform body, float casterR, float reachM)
        {
            TargetableHost mark = body != null ? body.GetComponentInParent<TargetableHost>() : null;
            float dist = mark != null ? mark.DistanceFrom(from) : EdgeDistance(from, body) + casterR;
            return StrikeCapsule.EdgeInReach(dist, casterR, reachM);
        }

        public Vector3 MoveWithWalls(Vector3 from, Vector3 to, float radiusM, Transform ignore)
        {
            if (ignore != null && ignore.TryGetComponent(out KinematicMotorController motor))
                return motor.Tuning != null
                    ? SweepAndSlide(from, to, radiusM)
                    : to;
            return SweepAndSlide(from, to, radiusM);
        }

        /// <summary>Düz çizgide ilk katı collider'a kadar gidilebilecek mesafe (tetikler sayılmaz).</summary>
        public float ClearDistance(Vector3 from, Vector3 dir, float meters, float radiusM, Transform self)
        {
            float radius = Mathf.Max(KinematicMotorControllerDefaults.CapsuleRadiusFloorM, radiusM);
            Vector3 p1 = from + Vector3.up * (radius + KinematicMotorControllerDefaults.CapsuleProbeLiftM);
            Vector3 p2 = from + Vector3.up * KinematicMotorControllerDefaults.CapsuleTopLiftM;
            int hits = Physics.CapsuleCastNonAlloc(
                p1, p2, radius, dir, _capsuleHits, meters, ~0, QueryTriggerInteraction.Ignore);
            float best = meters;
            bool blocked = false;
            for (int i = 0; i < hits; i++)
            {
                Collider col = _capsuleHits[i].collider;
                if (col == null || _capsuleHits[i].distance >= best)
                    continue;
                if (self != null && col.transform.IsChildOf(self))
                    continue;
                if (_capsuleHits[i].distance <= 0 && _capsuleHits[i].point == Vector3.zero)
                    continue;
                best = _capsuleHits[i].distance;
                blocked = true;
            }
            if (blocked)
                best -= KinematicMotorControllerDefaults.MoveStopInsetM;
            return Mathf.Max(0, best);
        }

        public bool MeleeHitsTarget(
            Transform caster,
            Transform focus,
            float reachM,
            float casterRadiusM)
        {
            if (caster == null || focus == null)
                return false;
            TargetableHost mark = focus.GetComponentInParent<TargetableHost>();
            if (mark != null)
            {
                float dist = mark.DistanceFrom(caster.position);
                return StrikeCapsule.EdgeInReach(dist, casterRadiusM, reachM);
            }

            Vector3 dir = focus.position - caster.position;
            dir.y = 0;
            if (dir.sqrMagnitude < 0.0001f)
                return true;
            dir.Normalize();
            float radius = Mathf.Max(
                RuleEngineV4UnitySceneDefaults.MinBodyRadiusM,
                casterRadiusM * RuleEngineV4UnitySceneDefaults.MeleeArcRadiusScale);
            StrikeCapsule.Segment(casterRadiusM, reachM, radius, out float nearM, out float farM);
            Vector3 chest = caster.position + Vector3.up * RuleEngineV4UnitySceneDefaults.StrikeChestOffsetM;
            Vector3 low = chest + dir * nearM;
            Vector3 high = chest + dir * farM;
            int count = Physics.OverlapCapsuleNonAlloc(
                low, high, radius, _hitBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Transform hit = _hitBuffer[i].transform;
                if (hit == focus || hit.IsChildOf(focus))
                    return true;
            }
            return false;
        }

        public bool SphereCastFirstTarget(
            Vector3 origin,
            Vector3 direction,
            float rangeM,
            float projectileRadiusM,
            out Transform hitRoot)
        {
            hitRoot = null;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.0001f)
                return false;
            direction.Normalize();
            var halfExtents = new Vector3(
                projectileRadiusM,
                RuleEngineV4WorldPhysicsDefaults.ProjectileColumnHalfHeightM,
                projectileRadiusM);
            int count = Physics.BoxCastNonAlloc(
                origin,
                halfExtents,
                direction,
                _castHits,
                Quaternion.LookRotation(direction),
                rangeM,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (_castHits[i].collider == null)
                    continue;
                if (_castHits[i].distance <= 0 && _castHits[i].point == Vector3.zero)
                    continue;
                if (!TryResolveTargetRoot(_castHits[i].collider.transform, out Transform root))
                    continue;
                if (_castHits[i].distance < best)
                {
                    best = _castHits[i].distance;
                    hitRoot = root;
                }
            }
            return hitRoot != null;
        }

        public List<Transform> CollectAreaHits(Transform caster, float radiusM, int maxTargets)
        {
            var results = new List<Transform>();
            if (caster == null || radiusM <= 0 || maxTargets <= 0)
                return results;
            Vector3 center = caster.position;
            int count = Physics.OverlapSphereNonAlloc(
                center, radiusM, _hitBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Vector3 forward = caster.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();
            float casterR = BodyRadius(caster);
            for (int i = 0; i < count; i++)
            {
                if (!TryResolveTargetRoot(_hitBuffer[i].transform, out Transform root))
                    continue;
                if (root == caster)
                    continue;
                Vector3 to = root.position - center;
                to.y = 0;
                if (to.sqrMagnitude < 0.0001f)
                    continue;
                if (Vector3.Dot(forward, to.normalized) <= 0)
                    continue;
                float edge = EdgeDistance(center, root) - casterR;
                if (edge > radiusM)
                    continue;
                if (!results.Contains(root))
                    results.Add(root);
                if (results.Count >= maxTargets)
                    break;
            }
            return results;
        }

        public Transform FindBounceTarget(
            Vector3 from,
            Vector3 forward,
            float searchRadiusM,
            HashSet<Transform> exclude)
        {
            forward.y = 0;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();
            int count = Physics.OverlapSphereNonAlloc(
                from, searchRadiusM, _hitBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Transform best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!TryResolveTargetRoot(_hitBuffer[i].transform, out Transform root))
                    continue;
                if (exclude != null && exclude.Contains(root))
                    continue;
                TargetableHost mark = root.GetComponentInParent<TargetableHost>();
                if (mark == null || !mark.IsAvailable)
                    continue;
                Vector3 to = root.position - from;
                to.y = 0;
                float dist = to.magnitude;
                if (dist < PositionOwnershipDefaults.MinDistM)
                    continue;
                float dot = Vector3.Dot(forward, to / dist);
                if (dot < Mathf.Cos(RuleEngineV4WorldPhysicsDefaults.BounceAngleConeDeg * Mathf.Deg2Rad))
                    continue;
                float score = dist - dot * 2;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = root;
                }
            }
            return best;
        }

        Vector3 SweepAndSlide(Vector3 from, Vector3 to, float radiusM)
        {
            Vector3 delta = to - from;
            delta.y = 0;
            float dist = delta.magnitude;
            if (dist < 0.0001f)
                return from;
            Vector3 dir = delta / dist;
            float radius = Mathf.Max(KinematicMotorControllerDefaults.CapsuleRadiusFloorM, radiusM);
            Vector3 p1 = from + Vector3.up * (radius + KinematicMotorControllerDefaults.CapsuleProbeLiftM);
            Vector3 p2 = from + Vector3.up * KinematicMotorControllerDefaults.CapsuleTopLiftM;
            int hits = Physics.CapsuleCastNonAlloc(
                p1, p2, radius, dir, _capsuleHits, dist, ~0, QueryTriggerInteraction.Ignore);
            if (hits <= 0)
                return to;
            float best = dist;
            Vector3 bestNormal = Vector3.zero;
            for (int i = 0; i < hits; i++)
            {
                if (_capsuleHits[i].collider == null)
                    continue;
                if (_capsuleHits[i].distance < best)
                {
                    best = _capsuleHits[i].distance;
                    bestNormal = _capsuleHits[i].normal;
                }
            }
            Vector3 stop = from + dir * Mathf.Max(0, best - KinematicMotorControllerDefaults.MoveStopInsetM);
            bestNormal.y = 0;
            if (bestNormal.sqrMagnitude < 0.0001f)
                return stop;
            bestNormal.Normalize();
            float remain = dist - best;
            if (remain <= 0.001f)
                return stop;
            Vector3 slide = Vector3.ProjectOnPlane(dir * remain, bestNormal);
            return stop + slide;
        }

        static bool TryResolveTargetRoot(Transform hit, out Transform root)
        {
            root = null;
            if (hit == null)
                return false;
            TargetableHost mark = hit.GetComponentInParent<TargetableHost>();
            if (mark == null)
                return false;
            root = mark.transform;
            return true;
        }
    }
}
