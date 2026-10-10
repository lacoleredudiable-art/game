using System.Collections.Generic;
using Dovus.Core.Casting;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Unity PhysX: kenar ölçümü TargetableHost ile; mermi ilk çarpan; alan ön yarım.</summary>
    public static class RuleEngineV4PhysicsQueries
    {
        static readonly Collider[] HitBuffer = new Collider[24];
        static readonly RaycastHit[] CastHits = new RaycastHit[64];

        public static bool MeleeHitsTarget(
            Transform caster,
            Transform target,
            float reachM,
            float casterRadiusM)
        {
            if (caster == null || target == null)
                return false;
            TargetableHost mark = target.GetComponentInParent<TargetableHost>();
            if (mark != null)
            {
                float dist = mark.DistanceFrom(caster.position);
                return StrikeCapsule.EdgeInReach(dist, casterRadiusM, reachM);
            }

            Vector3 dir = target.position - caster.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return true;
            dir.Normalize();
            float radius = Mathf.Max(
                RuleEngineV4PhysicsDefaults.MinBodyRadiusM,
                casterRadiusM * RuleEngineV4PhysicsDefaults.MeleeArcRadiusScale);
            StrikeCapsule.Segment(casterRadiusM, reachM, radius, out float nearM, out float farM);
            Vector3 chest = caster.position + Vector3.up * RuleEngineV4PhysicsDefaults.StrikeChestOffsetM;
            Vector3 low = chest + dir * nearM;
            Vector3 high = chest + dir * farM;
            int count = Physics.OverlapCapsuleNonAlloc(
                low, high, radius, HitBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Transform hit = HitBuffer[i].transform;
                if (hit == target || hit.IsChildOf(target))
                    return true;
            }
            return false;
        }

        public static bool SphereCastHitsTarget(
            Vector3 origin,
            Vector3 direction,
            float rangeM,
            float projectileRadiusM,
            Transform target)
        {
            return SphereCastFirstTarget(origin, direction, rangeM, projectileRadiusM, out Transform hit)
                   && target != null
                   && (hit == target || hit.IsChildOf(target));
        }

        /// <summary>
        /// Mermi: üstten bakışta yol üzerindeki ilk Targetable (trigger dahil). Yakın vuruş ve alan gibi
        /// yükseklikten bağımsızdır; kısa gövdeli yaratık göğüs hizasındaki merminin altında kalmaz.
        /// </summary>
        public static bool SphereCastFirstTarget(
            Vector3 origin,
            Vector3 direction,
            float rangeM,
            float projectileRadiusM,
            out Transform target)
        {
            target = null;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return false;
            direction.Normalize();
            var halfExtents = new Vector3(
                projectileRadiusM, RuleEngineV4PhysicsDefaults.ProjectileColumnHalfHeightM, projectileRadiusM);
            int count = Physics.BoxCastNonAlloc(
                origin,
                halfExtents,
                direction,
                CastHits,
                Quaternion.LookRotation(direction),
                rangeM,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (CastHits[i].collider == null)
                    continue;
                // Başlangıçta zaten örtüşen (zemin, atıcının dibindeki gövde) çarpma sayılmaz.
                if (CastHits[i].distance <= 0f && CastHits[i].point == Vector3.zero)
                    continue;
                if (!TryResolveTargetRoot(CastHits[i].collider.transform, out Transform root))
                    continue;
                if (CastHits[i].distance < best)
                {
                    best = CastHits[i].distance;
                    target = root;
                }
            }
            return target != null;
        }

        /// <summary>Alan: ön yarım küre (caster.forward), kenar mesafesi ≤ radius.</summary>
        public static List<Transform> CollectAreaHits(Transform caster, float radiusM, int maxTargets)
        {
            var results = new List<Transform>();
            if (caster == null || radiusM <= 0f || maxTargets <= 0)
                return results;
            Vector3 center = caster.position;
            int count = Physics.OverlapSphereNonAlloc(
                center, radiusM, HitBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Vector3 forward = caster.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();
            float casterR = RuleEngineV4WorldPhysicsUtil.BodyRadius(caster);
            for (int i = 0; i < count; i++)
            {
                if (!TryResolveTargetRoot(HitBuffer[i].transform, out Transform root))
                    continue;
                if (root == caster)
                    continue;
                Vector3 to = root.position - center;
                to.y = 0f;
                if (to.sqrMagnitude < 0.0001f)
                    continue;
                if (Vector3.Dot(forward, to.normalized) <= 0f)
                    continue;
                float edge = RuleEngineV4WorldPhysicsUtil.EdgeDistance(center, root) - casterR;
                if (edge > radiusM)
                    continue;
                if (!results.Contains(root))
                    results.Add(root);
                if (results.Count >= maxTargets)
                    break;
            }
            return results;
        }

        public static Transform FindBounceTarget(
            Vector3 from,
            Vector3 forward,
            float searchRadiusM,
            HashSet<Transform> exclude)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            forward.Normalize();
            int count = Physics.OverlapSphereNonAlloc(
                from, searchRadiusM, HitBuffer, Physics.AllLayers, QueryTriggerInteraction.Collide);
            Transform best = null;
            float bestScore = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!TryResolveTargetRoot(HitBuffer[i].transform, out Transform root))
                    continue;
                if (exclude != null && exclude.Contains(root))
                    continue;
                TargetableHost mark = root.GetComponentInParent<TargetableHost>();
                if (mark == null || !mark.IsAvailable)
                    continue;
                Vector3 to = root.position - from;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.01f)
                    continue;
                float dot = Vector3.Dot(forward, to / dist);
                if (dot < Mathf.Cos(RuleEngineV4WorldPhysicsDefaults.BounceAngleConeDeg * Mathf.Deg2Rad))
                    continue;
                float score = dist - dot * 2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = root;
                }
            }
            return best;
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

        public static float DefaultCasterRadius(Transform caster) =>
            RuleEngineV4WorldPhysicsUtil.BodyRadius(caster);
    }
}
