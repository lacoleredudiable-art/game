using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public sealed class RuleEngineV4DirectorBridge
    {
        readonly ManifestationDirector _director;
        readonly RuleEngineV4CastHost _castHost;

        public RuleEngineV4DirectorBridge(ManifestationDirector director)
        {
            _director = director;
            _castHost = new RuleEngineV4CastHost(director);
        }

        internal RuleEngineV4CastHost CastHost => _castHost;

        public bool TryLaunch(
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            out float dealt) =>
            _castHost.TryLaunch(ctx, skill, in motion, out dealt);

        public void PlaceMark(Transform target, float lifeSec) => _castHost.PlaceMark(target, lifeSec);
    }
}
