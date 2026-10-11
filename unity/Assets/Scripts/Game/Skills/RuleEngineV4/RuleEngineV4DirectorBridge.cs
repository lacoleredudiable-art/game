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
        readonly RuleEngineV4HeldCast _heldCast;

        public RuleEngineV4DirectorBridge(ManifestationDirector director)
        {
            _director = director;
            _castHost = new RuleEngineV4CastHost(director);
            _heldCast = new RuleEngineV4HeldCast(director, _castHost);
        }

        internal RuleEngineV4CastHost CastHost => _castHost;

        /// <summary>Basılı tutarak hedef seçme: 2. rünle çözülen kombo parmak kalkınca çıkar.</summary>
        internal RuleEngineV4HeldCast HeldCast => _heldCast;

        public bool TryLaunch(
            PendingClosing ctx,
            SkillResolution skill,
            in SkillMotionPlan motion,
            out float dealt) =>
            _castHost.TryLaunch(ctx, skill, in motion, out dealt);

        public bool OwnsSkill(SkillResolution skill) => _castHost.OwnsSkill(skill);

        public void PlaceMark(Transform marked, float lifeSec) => _castHost.PlaceMark(marked, lifeSec);

        public void PlaceSurfaceMark(in RuleEngineV4SurfaceAim aim, float lifeSec) => _castHost.PlaceSurfaceMark(aim, lifeSec);
    }
}
