using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Nişan ışınının ilk katı çarpması bir MarkOnly yüzeyse: nokta + yüzey normali.</summary>
    public readonly struct RuleEngineV4SurfaceAim
    {
        public RuleEngineV4SurfaceAim(RuleEngineV4MarkOnlySurface surface, Vector3 point, Vector3 normal, float distanceM)
        {
            Surface = surface;
            Point = point;
            Normal = normal;
            DistanceM = distanceM;
        }

        public RuleEngineV4MarkOnlySurface Surface { get; }
        public Vector3 Point { get; }
        public Vector3 Normal { get; }
        public float DistanceM { get; }
        public bool IsValid => Surface != null;
    }
}
