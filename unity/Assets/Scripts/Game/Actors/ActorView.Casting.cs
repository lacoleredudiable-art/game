using Dovus.Core;
using Dovus.Core.Element;
using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using Dovus.Core.Motion;
using Dovus.Game.Skills;
using Dovus.Game.Weapons;
using Dovus.Game.Assets;
using UnityEngine;


namespace Dovus.Game.Actors
{
    public sealed partial class ActorView
    {
        bool IsMoving()
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == ParamSpeed && p.type == AnimatorControllerParameterType.Float)
                    return _animator.GetFloat(ParamSpeed) >= UpperBodyMinSpeed;
            }
            return false;
        }

        bool TryPlayUpper(string stateName)
        {
            if (!stateName.StartsWith("Cast") && !stateName.StartsWith(StateBasicStrike))
                return false;
            if (_upperLayer == -2)
                _upperLayer = _animator.GetLayerIndex(UpperLayerName);
            if (_upperLayer < 0)
                return false;
            int hash = Animator.StringToHash(UpperStatePrefix + stateName);
            if (!_animator.HasState(_upperLayer, hash))
                return false;
            CrossFadeOrRestart(_upperLayer, hash);
            return true;
        }

        void CrossFadeOrRestart(int layer, int hash)
        {
            bool sameState = _animator.GetCurrentAnimatorStateInfo(layer).shortNameHash == hash
                             || (_animator.IsInTransition(layer)
                                 && _animator.GetNextAnimatorStateInfo(layer).shortNameHash == hash);
            if (CrossFadeSec > 0f && !sameState)
            {
                _animator.CrossFadeInFixedTime(hash, CrossFadeSec, layer, 0f);
                return;
            }

            _animator.Play(hash, layer, 0f);
            _animator.Update(0f);
        }

        string _motionKey = string.Empty;
        float _savedAnimatorSpeed = 1f;

        /// <summary>
        /// Kalıp fazı: state/trigger tablodan, bacak blend'i kalıbın hızından.
        /// Dönüş klibi ayrıca gövde yaw'ı ile birlikte gider.
        /// </summary>
        public void DriveMotion(
            in LocoBlend blend,
            string key,
            float animSpeed,
            bool spin,
            MotionAnimTable table,
            string weaponKey,
            int verbId,
            float refMps,
            float dampSec,
            float maxPlayback)
        {
            float clipRun = ClipRunMps(refMps);
            bool tooFast = LocoBlend.NeedsDashPose(blend.SpeedMps, clipRun);
            string playKey = LocoBlend.PresentationKey(key, blend.SpeedMps, clipRun);
            LocoBlend legs = tooFast ? default : blend;
            float cap = Mathf.Min(maxPlayback > ActorViewDefaults.MinPlaybackRate ? maxPlayback : LocoBlend.TemplatePlaybackCap, LocoBlend.TemplatePlaybackCap);
            ApplyTemplateLocomotion(legs, refMps, dampSec, cap);
            if (string.Equals(_motionKey, playKey, System.StringComparison.Ordinal))
                return;
            _motionKey = playKey ?? string.Empty;
            if (SkillClip.IsHeld(_animator))
                return;
            MotionAnimClip clip = (table ?? MotionAnimTable.BuiltIn).Resolve(_motionKey, weaponKey, verbId);
            clip = SidestepMirror.Apply(clip, blend.Strafe, _animator);
            PlayMotionClip(clip, animSpeed, spin);
        }

        public SkillClipHold SkillClip { get; } = new SkillClipHold();

        public void EndMotionAnim()
        {
            _motionKey = string.Empty;
            if (_animator != null)
                _animator.speed = _savedAnimatorSpeed > ActorViewDefaults.MinPositive ? _savedAnimatorSpeed : 1f;
        }

        void PlayMotionClip(MotionAnimClip clip, float animSpeed, bool spin)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            TryTrigger(clip.Trigger);
            bool played = PlayAction(clip.State);
            if (!played && Dovus.Core.RuleEngineV4.RuleEngineV4Feature.Enabled) // B3: dilimde yedek klip yok, eksik klip log'a düşer
                Debug.LogError($"[V4-FALLBACK] hareket klibi: Animator state yok '{clip.State}' (anahtar={clip.Key}); yedek klip oynatılmadı");
            else if (!played)
            {
                DesignWarnings.Once("motion.anim.state." + clip.State,
                    "Animator state yok: " + clip.State + ". Yedek locomotion/saldırı klibi.");
                if (!PlayAction(clip.Fallback ? MotionAnimTable.AttackFallbackState : MotionAnimTable.FallbackState))
                    PlayAction(MotionAnimTable.FallbackState);
            }

            float rate = animSpeed > ActorViewDefaults.MinPlaybackRate ? animSpeed : 1f;
            if (spin || !MotionAnimTable.IsLocomotionKey(clip.Key))
            {
                if (_savedAnimatorSpeed <= ActorViewDefaults.MinPositive)
                    _savedAnimatorSpeed = _animator.speed > ActorViewDefaults.MinPositive ? _animator.speed : 1f;
                _animator.speed = rate;
            }
            else
            {
                _animator.speed = _savedAnimatorSpeed > ActorViewDefaults.MinPositive ? _savedAnimatorSpeed : 1f;
            }
        }

        float ClipRunMps(float fallback)
        {
            if (_animator != null && _animator.isActiveAndEnabled
                && _animator.runtimeAnimatorController != null
                && HasFloat(ParamLocoRunSpeed))
            {
                float run = _animator.GetFloat(ParamLocoRunSpeed);
                if (run > ActorViewDefaults.MinPlaybackRate)
                    return run;
            }
            return fallback > ActorViewDefaults.MinPlaybackRate ? fallback : ActorViewDefaults.FallbackLocoRunMps;
        }
    }
}
