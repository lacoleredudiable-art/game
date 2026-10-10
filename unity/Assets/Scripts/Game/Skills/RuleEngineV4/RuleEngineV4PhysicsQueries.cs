using Dovus.Core.Casting;
using Dovus.Core.Equipment;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Unity PhysX: OverlapSphere / SphereCast / OverlapCapsule (mobil: NonAlloc, sabit buffer).</summary>
    public static class RuleEngineV4PhysicsQueries
    {
        static readonly Collider[] HitBuffer = new Collider[24];
        static readonly RaycastHit[] CastHits = new RaycastHit[8];

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
            if (target == null)
                return false;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return false;
            direction.Normalize();
            int count = Physics.SphereCastNonAlloc(
                origin,
                projectileRadiusM,
                direction,
                CastHits,
                rangeM,
                Physics.AllLayers,
                QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                Transform t = CastHits[i].collider != null ? CastHits[i].collider.transform : null;
                if (t != null && (t == target || t.IsChildOf(target)))
                    return true;
            }
            return false;
        }

        public static float DefaultCasterRadius(Transform caster) =>
            caster != null && caster.TryGetComponent(out KinematicMotorController motor)
                ? motor.BodyRadiusM
                : RuleEngineV4PhysicsDefaults.SliceMinionFallbackRadiusM;
    }
}
