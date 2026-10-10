using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Skills;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>CommandPlan → klip + animasyon olay maskesi → AnimationBridge + efekt köprüsü.</summary>
    public static class RuleEngineV4SkillAnimBridge
    {
        public static void PlayForPlan(ManifestationDirector director, in CommandPlan plan, RuleEngineV4SkillAnimCatalog catalog)
        {
            if (director == null || !plan.IsValid || catalog == null)
                return;

            RuleEngineV4SkillAnimBinding binding = RuleEngineV4SkillAnimResolver.Resolve(plan, catalog);
            if (binding.IsEmpty)
                return;

            ActorView view = director._visual;
            if (view == null || view.Animator == null)
                return;

            director.EnsureLaunchServices();
            director._animationBridge.PlaySkillClip(
                binding.Clip,
                binding.FallbackAnimatorState,
                view.Animator);

            SkillAnimVfxEventHost relay = EnsureRelay(view.Animator.gameObject);
            relay.SetEventMask(binding.Events);
        }

        static SkillAnimVfxEventHost EnsureRelay(GameObject animatorGo)
        {
            SkillAnimVfxEventHost relay = animatorGo.GetComponent<SkillAnimVfxEventHost>();
            if (relay == null)
                relay = animatorGo.AddComponent<SkillAnimVfxEventHost>();
            RuleDrivenVfxDirector director = animatorGo.GetComponentInParent<RuleDrivenVfxDirector>();
            if (director == null)
                director = animatorGo.GetComponentInParent<ActorView>()?.GetComponent<RuleDrivenVfxDirector>();
            relay.Bind(director);
            return relay;
        }
    }
}
