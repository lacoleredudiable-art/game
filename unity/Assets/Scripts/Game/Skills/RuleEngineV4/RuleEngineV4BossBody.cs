using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// O-6.10: dilimde boss gövde yarıçapının tek kaynağı, kural-motoru-v4.json <c>world_physics.body_radius_boss_m</c>.
    /// Dodge, boss yaklaşması, hedefleme, menzil, alan, mermi ve hareket süpürmesi bu değeri okur.
    /// </summary>
    public static class RuleEngineV4BossBody
    {
        public static float RadiusM(string where)
        {
            RuleEngineV4WorldPhysics wp = RuleEngineV4WorldPhysicsRuntime.Active;
            if (!wp.HasBodyRadiusBossM)
                Debug.LogError(
                    $"[V4-FALLBACK] {where}: kural-motoru-v4.json world_physics.body_radius_boss_m yok; " +
                    $"kod varsayılanı {wp.BodyRadiusBossM:0.00} m");
            return wp.BodyRadiusBossM;
        }
    }
}
