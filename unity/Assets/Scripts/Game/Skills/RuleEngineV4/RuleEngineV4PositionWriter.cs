using Dovus.Core.Motion;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>Kalıp dışı konum yazımı — <see cref="PositionOwnership.RuleEngineV4ForcePush"/> kanalı.</summary>
    public static class RuleEngineV4PositionWriter
    {
        public static void Commit(Transform body, Vector3 worldPosition)
        {
            if (body == null)
                return;
            _ = PositionOwnership.RuleEngineV4ForcePush;
            body.position = worldPosition;
        }
    }
}
