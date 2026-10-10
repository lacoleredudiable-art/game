using Dovus.Core.RuleEngineV4;
using Dovus.Game.Diagnostics;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4CommandRunner
    {
        public static float RunSync(ManifestationDirector director, CommandPlan plan, Transform target)
        {
            float dealt = 0f;
            bool deliveryOk = EvaluateDelivery(director, plan, target);
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
                            dealt += RuleEngineV4WorldHost.ApplyDamage(director, target, dmg * dmgMult);
                        break;
                    case PhysicsCommandKind.SifaVer:
                        if ((deliveryOk || target == director.MechanicsPlayer)
                            && RuleEngineV4CommandAccess.TryHeal(cmd, out float heal, out float healMult))
                            RuleEngineV4WorldHost.ApplyHeal(director, target, heal * healMult);
                        break;
                    case PhysicsCommandKind.IsaretKoy:
                        if (RuleEngineV4CommandAccess.TryMark(cmd, out float life))
                            director._castPort?.RuleEngineV4Bridge.PlaceMark(target, life);
                        break;
                    default:
                        DebugConfig.DevLog($"[RuleEngineV4] PR2 stub: {cmd.Kind}");
                        break;
                }
            }

            return dealt;
        }

        static bool EvaluateDelivery(ManifestationDirector director, CommandPlan plan, Transform target)
        {
            Transform caster = director.MechanicsPlayer;
            if (caster == null)
                return false;
            float casterR = RuleEngineV4PhysicsQueries.DefaultCasterRadius(caster);
            bool needsTarget = plan.Target?.RequiresLivingTarget ?? true;
            if (!needsTarget)
                return true;
            if (target == null)
                return false;
            if (target == caster)
                return true;

            foreach (PhysicsCommand cmd in plan.Commands)
            {
                if (RuleEngineV4CommandAccess.TryMeleeRange(cmd, out float meleeRange)
                    && RuleEngineV4PhysicsQueries.MeleeHitsTarget(caster, target, meleeRange, casterR))
                    return true;
                if (RuleEngineV4CommandAccess.TryMissile(cmd, out float missileRange, out _)
                    && RuleEngineV4PhysicsQueries.SphereCastHitsTarget(
                        caster.position + Vector3.up * RuleEngineV4PhysicsDefaults.ProjectileOriginHeightM,
                        target.position - caster.position,
                        missileRange,
                        RuleEngineV4PhysicsDefaults.ProjectileRadiusM,
                        target))
                    return true;
                if (RuleEngineV4CommandAccess.TryArea(cmd, out float areaR))
                {
                    float dist = Vector3.Distance(
                        new Vector3(caster.position.x, 0f, caster.position.z),
                        new Vector3(target.position.x, 0f, target.position.z));
                    if (dist <= areaR + casterR)
                        return true;
                }
            }

            return true;
        }
    }
}
