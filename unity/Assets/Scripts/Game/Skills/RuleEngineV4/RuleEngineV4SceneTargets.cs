using System.Collections.Generic;
using Dovus.Core.Actors;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public static class RuleEngineV4SceneTargets
    {
        public static void Collect(
            Vector3 casterPosition,
            int casterTeamId,
            IReadOnlyList<TargetableHost> live,
            RuleEngineV4TeamMarkRegistry marks,
            double worldMs,
            List<RuleEngineV4TargetCandidate> into)
        {
            into.Clear();
            marks?.Prune(worldMs);
            if (live == null)
                return;
            for (int i = 0; i < live.Count; i++)
            {
                TargetableHost host = live[i];
                if (host == null || !host.IsAvailable)
                    continue;
                bool hostile = host.TeamId != casterTeamId;
                float dist = host.DistanceFrom(casterPosition);
                bool hasMark = marks != null && marks.HasMarkOn(host.transform, worldMs);
                int id = host.TargetKey;
                if (id <= 0)
                    continue;
                into.Add(new RuleEngineV4TargetCandidate(id, hostile, dist, true, hasMark));
            }
        }

        public static bool TryResolveTransform(
            IReadOnlyList<TargetableHost> live,
            int targetId,
            out Transform transform)
        {
            transform = null;
            if (live == null || targetId <= 0)
                return false;
            for (int i = 0; i < live.Count; i++)
            {
                TargetableHost host = live[i];
                if (host == null || host.TargetKey != targetId)
                    continue;
                transform = host.transform;
                return true;
            }
            return false;
        }
    }
}
