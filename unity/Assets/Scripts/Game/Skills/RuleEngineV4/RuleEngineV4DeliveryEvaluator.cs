using Dovus.Core.RuleEngineV4;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public sealed class RuleEngineV4DeliveryEvaluator
    {
        readonly RuleEngineV4PhysicsServices _physics;

        public RuleEngineV4DeliveryEvaluator(RuleEngineV4PhysicsServices physics) =>
            _physics = physics;

        public bool EvaluateDelivery(ManifestationDirector director, CommandPlan plan, Transform focus)
        {
            Transform caster = director.MechanicsPlayer;
            if (caster == null)
                return false;
            float casterR = _physics.BodyRadius(caster);
            bool needsTarget = plan.Target?.RequiresLivingTarget ?? true;
            if (!needsTarget)
                return true;
            if (focus == null)
                return false;
            if (focus == caster)
                return true;

            foreach (PhysicsCommand cmd in plan.Commands)
            {
                if (RuleEngineV4CommandAccess.TryMeleeRange(cmd, out float meleeRange)
                    && _physics.MeleeHitsTarget(caster, focus, meleeRange, casterR))
                    return true;
                if (RuleEngineV4CommandAccess.TryMissile(cmd, out float missileRange, out _))
                {
                    Vector3 origin = caster.position + Vector3.up * RuleEngineV4UnitySceneDefaults.ProjectileOriginHeightM;
                    if (_physics.SphereCastFirstTarget(
                            origin,
                            focus.position - origin,
                            missileRange,
                            RuleEngineV4WorldPhysicsDefaults.ProjectileRadiusM,
                            out Transform hit)
                        && (hit == focus || hit.IsChildOf(focus)))
                        return true;
                }
                if (RuleEngineV4CommandAccess.TryArea(cmd, out float areaR))
                {
                    var hits = _physics.CollectAreaHits(
                        caster, areaR, RuleEngineV4UnitySceneDefaults.AreaDeliveryScanCap);
                    for (int i = 0; i < hits.Count; i++)
                    {
                        if (hits[i] == focus || hits[i].IsChildOf(focus))
                            return true;
                    }
                }
            }

            return false;
        }
    }
}
