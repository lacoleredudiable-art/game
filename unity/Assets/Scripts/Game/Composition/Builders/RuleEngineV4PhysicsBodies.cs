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
            Attach(ctx.Player, RuleEngineV4WeightTier.Medium, CompositionConstants.PlayerRadiusM);
            Attach(ctx.Boss, RuleEngineV4WeightTier.Boss, CompositionConstants.BossRadiusM);
            Attach(ctx.Ally, RuleEngineV4WeightTier.Medium, CompositionConstants.PlayerRadiusM);
            if (ctx.Ally2 != null)
                Attach(ctx.Ally2, RuleEngineV4WeightTier.Medium, CompositionConstants.PlayerRadiusM);
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
