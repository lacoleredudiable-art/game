using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Game.Composition;
using Dovus.Game.Skills.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    static class RuleEngineV4PhysicsBodies
    {
        public static void AttachIfEnabled(WorldContext ctx)
        {
            if (ctx.Tuning == null || !ctx.Tuning.RuleEngineV4.SliceScene)
                return;
            var wp = RuleEngineV4WorldPhysicsRuntime.Active;
            Attach(ctx.Player, RuleEngineV4WeightTier.Light, wp.BodyRadiusPlayerM);
            Attach(ctx.Boss, RuleEngineV4WeightTier.Boss, wp.BodyRadiusBossM);
            Attach(ctx.Ally, RuleEngineV4WeightTier.Light, wp.BodyRadiusPlayerM);
            if (ctx.Ally2 != null)
                Attach(ctx.Ally2, RuleEngineV4WeightTier.Light, wp.BodyRadiusPlayerM);
        }

        static void Attach(GameObject go, RuleEngineV4WeightTier tier, float radiusM)
        {
            if (go == null)
                return;
            var body = go.GetComponent<RuleEngineV4PhysicsBodyHost>();
            if (body == null)
                body = go.AddComponent<RuleEngineV4PhysicsBodyHost>();
            body.WeightTier = tier;
            body.BodyRadiusM = radiusM;
        }
    }
}
