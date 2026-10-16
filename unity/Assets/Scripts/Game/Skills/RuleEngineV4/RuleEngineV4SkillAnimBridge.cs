using Dovus.Core.Presentation;
using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>CommandPlan → klip + animasyon olay maskesi → AnimationBridge + efekt köprüsü.</summary>
    public static class RuleEngineV4SkillAnimBridge
    {
        public static void PlayForPlan(RuleEngineV4CombatContext combat, in CommandPlan plan, RuleEngineV4SkillAnimCatalog catalog)
        {
            if (combat == null || !plan.IsValid || catalog == null)
                return;

            RuleEngineV4SkillAnimBinding binding = RuleEngineV4SkillAnimResolver.Resolve(plan, catalog);
            if (binding.IsEmpty)
                return;

            ActorView view = combat.Visual;
            if (view == null || view.Animator == null)
                return;

            combat.EnsureAnimationReady?.Invoke();
            AnimationBridge anim = combat.AnimationBridge;
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
        public static bool PlayBasicStrike(RuleEngineV4CombatContext combat, int slotRune)
        {
            if (combat == null)
                return BasicStrikeFallback("combat yok", null, slotRune, 0, RuleEngineV4SkillAnimBinding.Empty, null);
            if (!RuleEngineV4Feature.Enabled)
                return BasicStrikeFallback("v4 bayrağı kapalı", combat, slotRune, 0, RuleEngineV4SkillAnimBinding.Empty, null);
            RuleEngineV4CastHost castHost = combat.CastHost;
            if (castHost == null)
                return BasicStrikeFallback("v4 CastHost yok", combat, slotRune, 0, RuleEngineV4SkillAnimBinding.Empty, null);
            int weaponId = combat.EquippedWeaponNumber();
            if (!RuleEngineV4Slice.IsSliceWeapon(weaponId))
                return BasicStrikeFallback("silah dilimde değil", combat, slotRune, weaponId, RuleEngineV4SkillAnimBinding.Empty, null);
            RuleEngineV4SkillAnimBinding binding = castHost.CatalogAccess.Catalog.SkillAnim.Resolve(slotRune, 0, weaponId);
            ActorView view = combat.Visual;
            if (binding.IsEmpty || view == null || view.Animator == null)
                return BasicStrikeFallback(
                    binding.IsEmpty ? "skill_anim bağlaması boş" : view == null ? "ActorView yok" : "Animator yok",
                    combat, slotRune, weaponId, binding, view != null ? view.Animator : null);

            combat.EnsureAnimationReady?.Invoke();
            AnimationBridge anim = combat.AnimationBridge;
            anim.PlaySkillClip(binding.Clip, binding.FallbackAnimatorState, view.Animator);
            if (!anim.LastPlayApplied)
                return BasicStrikeFallback("klip oynatılamadı", combat, slotRune, weaponId, binding, view.Animator);
            if (anim.LastUsedFallbackState)
                return BasicStrikeFallback(
                    $"tam klip oynamadı, yedek durum '{binding.FallbackAnimatorState}' oynadı",
                    combat, slotRune, weaponId, binding, view.Animator);
            string state = anim.LastExactClipFound ? anim.LastClipName : binding.FallbackAnimatorState;
            view.SkillClip.Hold(state);
            SkillAnimVfxEventHost relay = EnsureRelay(view.Animator.gameObject, out _);
            relay.BeginBasicStrike(state, binding.Events);
            LastBasicStrikeClip = anim.LastExactClipFound ? anim.LastClipName : string.Empty;
            return true;
        }

        /// <summary>Son yeni-yol düz vuruşunun oynattığı klip (log / test).</summary>
        public static string LastBasicStrikeClip { get; private set; } = string.Empty;

        /// <summary>Dilimde düz vuruş yedeği yok (K3): düşen koşul ve değerleri LogError'a yazılır.</summary>
        static bool BasicStrikeFallback(
            string condition,
            RuleEngineV4CombatContext combat,
            int slotRune,
            int weaponId,
            in RuleEngineV4SkillAnimBinding binding,
            Animator animator)
        {
            LastBasicStrikeClip = string.Empty;
            string weaponRaw = combat?.EquippedWeapon?.Id ?? "null";
            Debug.LogError(
                $"[V4-FALLBACK] düz vuruş: {condition} silah={weaponId} (kimlik '{weaponRaw}') rün={slotRune} " +
                $"bağlama=klip '{binding.Clip}' yedek '{binding.FallbackAnimatorState}' animator={DescribeAnimator(animator, binding.Clip)}");
            return false;
        }

        static string DescribeAnimator(Animator animator, string clip)
        {
            if (animator == null)
                return "yok";
            RuntimeAnimatorController ctrl = animator.runtimeAnimatorController;
            if (ctrl == null)
                return $"'{animator.name}' controller yok";
            bool hasState = !string.IsNullOrEmpty(clip) && animator.HasState(0, Animator.StringToHash(clip));
            bool hasClip = false;
            foreach (AnimationClip c in ctrl.animationClips)
                hasClip |= c != null && c.name == clip;
            return $"'{animator.name}' controller '{ctrl.name}' ({ctrl.GetType().Name}) etkin={animator.isActiveAndEnabled} " +
                   $"klip var={hasClip} durum var={hasState}";
        }

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
