using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using Dovus.Game.Actors;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>
    /// Oyuncu cast sunumu: silahın teslim yolu (menzilli → atış klibi), state'lerin ActorVisual
    /// üzerinden oynaması (crossfade + üst gövde) ve prezentasyon <c>spawn_vfx_at_frame</c>
    /// anında el efekti. Yalnız görsel — hasar ve etki doğumu zamanlaması buradan değişmez.
    /// </summary>
    public sealed partial class ManifestationDirector
    {
        bool _castVfxHooked;
        Color _castVfxColor = Color.white;
        CastFlash _castFlash;

        void SyncVisualDelivery()
        {
            if (_visual == null)
                return;
            _visual.RangedDelivery = _equippedWeapon != null
                && string.Equals(_equippedWeapon.Type, "ranged", System.StringComparison.Ordinal);
            _visual.SetWeapon(_equippedWeapon != null ? _equippedWeapon.AnimationsKey : string.Empty);
            if (_animationBridge.StatePlayer == null)
                _animationBridge.StatePlayer = _visual.PlayAction;
        }

        void StartCastVfxTimer(SkillResolution skill, IReadOnlyList<SentenceWord> words)
        {
            if (!_castVfxHooked)
            {
                _animationBridge.SpawnVfxFrameReached += OnCastVfxFrame;
                _castVfxHooked = true;
            }

            if (words != null && words.Count > 0)
            {
                SkillFeel.ElementPalette(words, _colors, out Color line, out _);
                _castVfxColor = line;
            }

            EnsurePresentationCatalog();
            if (_presentationValidator == null || _presentationCatalog == null)
                return;
            string typeId = _presentationValidator.Validate(skill).AnimationTypeId;
            if (string.IsNullOrEmpty(typeId) || !_presentationCatalog.TryGetAnimation(typeId, out AnimationFrameNode node))
            {
                // v6.1 skill'in animation_type'ı yok: oynayan controller state'inin
                // prezentasyon karşılığı (animator_state eşlemesi) kare verisini verir.
                if (!TryFindFrameNodeForState(LastAnimationState, out node))
                    return;
            }
            _animationBridge.StartFrameTimer(node, _clock != null ? _clock.Director.WorldTimeMs : 0);
        }

        bool TryFindFrameNodeForState(string controllerState, out AnimationFrameNode found)
        {
            found = default;
            if (string.IsNullOrEmpty(controllerState))
                return false;
            string preferPrefix = _visual != null && _visual.RangedDelivery ? "cast_" : "melee_";
            bool any = false;
            foreach (var kv in _presentationCatalog.Animations)
            {
                if (AnimationBridge.MapToQuaterniusState(kv.Value.AnimatorState) != controllerState)
                    continue;
                if (!any || kv.Key.StartsWith(preferPrefix, System.StringComparison.Ordinal))
                {
                    bool preferred = kv.Key.StartsWith(preferPrefix, System.StringComparison.Ordinal);
                    found = kv.Value;
                    any = true;
                    if (preferred)
                        return true;
                }
            }
            return any;
        }

        void OnCastVfxFrame(double worldMs)
        {
            if (_visual == null)
                return;
            if (_castFlash == null)
            {
                _castFlash = _visual.GetComponent<CastFlash>();
                if (_castFlash == null)
                    _castFlash = _visual.gameObject.AddComponent<CastFlash>();
            }
            _castFlash.Play(_castVfxColor, _visual.Animator);
        }
    }
}
