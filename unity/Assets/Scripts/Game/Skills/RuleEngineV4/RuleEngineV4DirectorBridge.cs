using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    public sealed class RuleEngineV4DirectorBridge
    {
        readonly RuleEngineV4CastHost _castHost;
        readonly RuleEngineV4HeldCast _heldCast;

        public RuleEngineV4DirectorBridge(RuleEngineV4CombatContext combat)
        {
            _castHost = new RuleEngineV4CastHost(combat);
            combat.CastHost = _castHost;
            combat.PlaceMark = _castHost.PlaceMark;
            combat.PlaceSurfaceMark = _castHost.PlaceSurfaceMark;
            _heldCast = new RuleEngineV4HeldCast(combat, _castHost);
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
