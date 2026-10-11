using Dovus.Core.Presentation;
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
            AnimationBridge anim = director._animationBridge;
            anim.PlaySkillClip(
                binding.Clip,
                binding.FallbackAnimatorState,
                view.Animator);
            if (anim.LastExactClipFound && anim.LastPlayApplied)
                view.SkillClip.Hold(anim.LastClipName);
            else
                view.SkillClip.Release();

            if (RuleEngineV4CommandAccess.TryBackstep(plan, out float hopSec))
                DodgeClipPlayback.ApplyBackstep(view.Animator, hopSec);

            SkillAnimVfxEventHost relay = EnsureRelay(view.Animator.gameObject, out RuleDrivenVfxDirector vfx);
            relay.SetEventMask(binding.Events);
            vfx?.ArmEntrySilhouette(
                binding.EntrySilhouetteCell,
                binding.EntrySilhouetteLifeSec,
                binding.EntrySilhouetteFadeInSec,
                binding.EntrySilhouetteFadeOutSec,
                VfxPlanResolver.VerbColor(plan.VerbRune));
        }

        /// <summary>
        /// Yeni yolda düz vuruş klibi silah setinden: skill_anim[silah][düz vuruş yuvası]
        /// (Kılıç → Kilic_VUR, Yay → Yay_VUR); eski yumruk/döngü state'ine düşmez.
        /// Klip skill ile aynı olsa da Trail_On / Trail_Off dinlenmez (kor izi yok).
        /// </summary>
        public static bool PlayBasicStrike(ManifestationDirector director, int slotRune)
        {
            if (director == null || !RuleEngineV4Feature.Enabled)
                return false;
            RuleEngineV4DirectorBridge bridge = director._castPort?.RuleEngineV4Bridge;
            if (bridge == null || bridge.CastHost == null)
                return false;
            int weaponId = director.EquippedWeaponNumber();
            if (!RuleEngineV4Slice.IsSliceWeapon(weaponId))
                return false;
            RuleEngineV4SkillAnimBinding binding = bridge.CastHost.CatalogAccess.Catalog.SkillAnim.Resolve(slotRune, 0, weaponId);
            ActorView view = director._visual;
            if (binding.IsEmpty || view == null || view.Animator == null)
                return false;

            director.EnsureLaunchServices();
            AnimationBridge anim = director._animationBridge;
            anim.PlaySkillClip(binding.Clip, binding.FallbackAnimatorState, view.Animator);
            if (!anim.LastPlayApplied)
                return false;
            string state = anim.LastExactClipFound ? anim.LastClipName : binding.FallbackAnimatorState;
            view.SkillClip.Hold(state);
            SkillAnimVfxEventHost relay = EnsureRelay(view.Animator.gameObject, out _);
            relay.BeginBasicStrike(state, binding.Events);
            LastBasicStrikeClip = anim.LastExactClipFound ? anim.LastClipName : string.Empty;
            return true;
        }

        /// <summary>Son yeni-yol düz vuruşunun oynattığı klip (log / test).</summary>
        public static string LastBasicStrikeClip { get; private set; } = string.Empty;

        static SkillAnimVfxEventHost EnsureRelay(GameObject animatorGo, out RuleDrivenVfxDirector director)
        {
            SkillAnimVfxEventHost relay = animatorGo.GetComponent<SkillAnimVfxEventHost>();
            if (relay == null)
                relay = animatorGo.AddComponent<SkillAnimVfxEventHost>();
            director = animatorGo.GetComponentInParent<RuleDrivenVfxDirector>();
            if (director == null)
                director = animatorGo.GetComponentInParent<ActorView>()?.GetComponent<RuleDrivenVfxDirector>();
            relay.Bind(director);
            return relay;
        }
    }
}
