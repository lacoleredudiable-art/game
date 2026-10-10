using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Manifestation;
using Dovus.Core.Mechanic;
using Dovus.Core.Presentation;
using Dovus.Game.Actors;
using Dovus.Game.Audio;
using Dovus.Game.Data;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using System;
using System.Collections.Generic;
using Dovus.Game.Assets;
using UnityEngine;

namespace Dovus.Game.Skills.Presentation
{
    public sealed class SkillPresentation
    {
        readonly ISkillPresentationHost _host;
        PresentationCatalog _catalog;
        PresentationValidator _validator;

        public SkillPresentation(ISkillPresentationHost host) => _host = host;

        public PresentationCatalog Catalog => _catalog;
        public PresentationValidator Validator => _validator;

        public void EnsureCatalog()
        {
            if (_catalog != null && _validator != null)
                return;

            const string resourcePath = "Presentation/prezentasyon-katmani";
            var asset = AssetLoader.Load<TextAsset>(resourcePath, null);
            if (asset == null || string.IsNullOrWhiteSpace(asset.text))
                return;

            try
            {
                _catalog = PresentationCatalog.FromJson(asset.text);
                _validator = new PresentationValidator(_catalog);
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    $"[ManifestationDirector] prezentasyon-katmani okunamadı: {e.Message}");
            }
        }

        public void ShoutSkill(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (skill.IsEmpty)
                return;

            string mech = SkillFeel.MechanicShort(skill.Mechanics);
            string adj = SkillFeel.AdjectiveShort(skill);
            string paintedName = _host.LastFactorySkill != null
                && string.Equals(_host.LastFactorySkill.Id, skill.Identity.Id, StringComparison.Ordinal)
                    ? _host.LastFactorySkill.DisplayName
                    : skill.Identity.DisplayName;
            ElementPaintNode? paint = _host.SelectedElementPaint;
            if ((_host.LastFactorySkill == null || _host.LastFactorySkill.Id != skill.Identity.Id)
                && paint.HasValue && !string.IsNullOrEmpty(paint.Value.NamePrefix))
                paintedName = paint.Value.NamePrefix + " " + paintedName;
            string bangNote = string.IsNullOrEmpty(adj)
                ? mech
                : (string.IsNullOrEmpty(mech) ? adj : mech + " | " + adj);
            MechanicPlan mechanic = _host.LastMechanicPlan;
            if (mechanic != null && string.Equals(mechanic.SkillId, skill.Identity.Id, StringComparison.Ordinal))
            {
                string title = MechanicDescriber.ShortTitle(mechanic);
                bangNote = string.IsNullOrEmpty(bangNote) ? title : title + " | " + bangNote;
            }
            _host.DebugHud?.NoteSkillBang(paintedName, bangNote);
            SkillFeel.CameraKick(skill.Presentation.VerbFamily, _host.Camera, _host.Combat?.Feel);
            _host.SyncVisualDelivery();
            ApplySkillAnimation(skill);
            // Yol bağımsız VFX: eski motion + kural_motoru_v4 aynı shout'tan uyanış/rün alır.
            Transform actor = _host.Visual != null ? _host.Visual.transform : null;
            RuleDrivenVfxSink.BeginSkill(actor, skill, _host.EquippedWeapon);
            _host.StartCastVfxTimer(skill, words);
            _host.Sfx?.Play(SfxLibrary.CastPrefix + skill.Presentation.VerbFamily);
        }

        public void ApplySkillAnimation(SkillResolution skill)
        {
            _host.LastAnimationTypeId = string.Empty;
            _host.LastAnimationState = string.Empty;
            _host.LastAnimationPlayApplied = false;
            _host.LastAnimationClip = string.Empty;
            _host.LastAnimationUsedFallback = false;

            if (skill.IsEmpty || _host.Visual == null || _host.Visual.Animator == null)
                return;

            if (RuleEngineV4Feature.Enabled
                && int.TryParse(skill.Identity.Verb, out int v4Verb)
                && int.TryParse(skill.Identity.Adjective, out int v4Adj)
                && RuleEngineV4Slice.IsSliceCombo(v4Verb, v4Adj)
                && _host.EquippedWeapon != null
                && int.TryParse(_host.EquippedWeapon.Id, out int weaponNum)
                && RuleEngineV4Slice.IsSliceWeapon(weaponNum))
                return;

            if (_host.Skills != null && _host.Skills.IsV61)
            {
                int verbId = int.TryParse(skill.Identity.Verb, out int parsed) ? parsed : 0;
                string weaponKey = _host.EquippedWeapon?.AnimationsKey ?? string.Empty;
                string bindingKey = weaponKey + ":" + verbId;
                if (_host.AnimationDatabase != null
                    && _host.AnimationDatabase.TryGet(weaponKey, verbId, out AnimationBinding binding))
                {
                    _host.LastAnimationTypeId = bindingKey;
                    _host.LastAnimationState = binding.AnimatorState;
                    _host.LastAnimationPlayApplied =
                        _host.AnimationBridge.PlayBinding(binding, _host.Visual.Animator);
                    _host.LastAnimationClip = _host.AnimationBridge.LastClipName;
                    _host.LastAnimationUsedFallback = _host.AnimationBridge.LastUsedFallbackState;
                }
                else if (_host.MissingAnimationBindings.Add(bindingKey))
                {
                    Debug.LogWarning($"[AnimationDatabase] binding yok, cast no-op: {bindingKey}");
                }
                return;
            }

            EnsureCatalog();
            if (_validator == null || _catalog == null)
                return;

            PresentationValidationResult check = _validator.Validate(skill);
            _host.LastAnimationTypeId = check.AnimationTypeId;
            if (!check.AnimationFound)
                return;

            if (!_catalog.TryGetAnimation(check.AnimationTypeId, out AnimationFrameNode node))
                return;

            _host.LastAnimationState = AnimationBridge.MapToQuaterniusState(node.AnimatorState);
            if (!string.IsNullOrEmpty(check.AnimationTypeId))
            {
                EffectSilhouette axes = default;
                _host.Visual.PulseAnimationType(check.AnimationTypeId, axes);
                _host.LastAnimationState = ActorView.AnimationTypeToState(check.AnimationTypeId);
                _host.LastAnimationPlayApplied = true;
                return;
            }

            double worldMs = _host.Clock != null ? _host.Clock.Director.WorldTimeMs : 0;
            _host.LastAnimationPlayApplied = _host.AnimationBridge.Play(node, _host.Visual.Animator, worldMs);
        }
    }
}
