using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>MarkOnly yüzeydeki sabit işaret: çarpma noktasında, çapalı (itilmez, hedeflenmez).</summary>
    public sealed class RuleEngineV4SurfaceMark : MonoBehaviour
    {
        public RuleEngineV4MarkOnlySurface Surface { get; private set; }
        public Vector3 Normal { get; private set; }

        public void Configure(RuleEngineV4MarkOnlySurface surface, Vector3 normal)
        {
            Surface = surface;
            Normal = normal;
        }
    }
}
