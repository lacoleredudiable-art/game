using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Gövde yarıçapı + ağırlık katmanı (dunya-fizigi.md).</summary>
    public sealed class RuleEngineV4PhysicsBodyHost : MonoBehaviour
    {
        public RuleEngineV4WeightTier WeightTier = RuleEngineV4WeightTier.Light;
        public float BodyRadiusM = 0.5f;
    }
}
