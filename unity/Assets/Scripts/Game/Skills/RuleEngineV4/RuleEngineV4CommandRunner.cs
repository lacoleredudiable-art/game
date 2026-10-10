using Dovus.Core.RuleEngineV4;
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4CommandRunner
    {
        /// <summary>PR2 yedek: zamanlama yok; v4 yolu WorldCommandRunHost kullanır.</summary>
        public static float RunSync(ManifestationDirector director, CommandPlan plan, Transform focus)
        {
            float dealt = 0;
            bool deliveryOk = EvaluateDelivery(director, plan, focus);
            foreach (PhysicsCommand cmd in plan.Commands)
            {
                switch (cmd.Kind)
                {
                    case PhysicsCommandKind.MenzileYuru:
                    case PhysicsCommandKind.OnSure:
                        break;
                    case PhysicsCommandKind.HasarVer:
                        if (deliveryOk
                            && RuleEngineV4CommandAccess.TryDamage(cmd, out float dmg, out float dmgMult))
                            dealt += RuleEngineV4WorldHost.ApplyDamage(director, focus, dmg * dmgMult);
                        break;
                    case PhysicsCommandKind.SifaVer:
                        if ((deliveryOk || focus == director.MechanicsPlayer)
                            && RuleEngineV4CommandAccess.TryHeal(cmd, out float heal, out float healMult))
                            RuleEngineV4WorldHost.ApplyHeal(director, focus, heal * healMult);
                        break;
                    case PhysicsCommandKind.IsaretKoy:
                        if (RuleEngineV4CommandAccess.TryMark(cmd, out float life))
                            director._castPort?.RuleEngineV4Bridge.PlaceMark(focus, life);
                        break;
                    default:
                        DebugConfig.DevLog($"[RuleEngineV4] sync stub: {cmd.Kind}");
                        break;
                }
            }

            return dealt;
        }

        public static bool EvaluateDelivery(ManifestationDirector director, CommandPlan plan, Transform focus)
        {
            Transform caster = director.MechanicsPlayer;
            if (caster == null)
                return false;
            float casterR = RuleEngineV4WorldPhysicsUtil.BodyRadius(caster);
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
                    && RuleEngineV4PhysicsQueries.MeleeHitsTarget(caster, focus, meleeRange, casterR))
                    return true;
                if (RuleEngineV4CommandAccess.TryMissile(cmd, out float missileRange, out _))
                {
                    Vector3 origin = caster.position + Vector3.up * RuleEngineV4PhysicsDefaults.ProjectileOriginHeightM;
                    if (RuleEngineV4PhysicsQueries.SphereCastFirstTarget(
                            origin,
                            focus.position - origin,
                            missileRange,
                            RuleEngineV4PhysicsDefaults.ProjectileRadiusM,
                            out Transform hit)
                        && (hit == focus || hit.IsChildOf(focus)))
                        return true;
                }
                if (RuleEngineV4CommandAccess.TryArea(cmd, out float areaR))
                {
                    var hits = RuleEngineV4PhysicsQueries.CollectAreaHits(
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
