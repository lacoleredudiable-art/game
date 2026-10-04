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
    public sealed partial class ActorView : MonoBehaviour
    {
        public void SetLocomotion(float worldSpeedMps, float normalizeRefMps, float dampSec, float maxPlaybackMult)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            if (!HasFloat(ParamLocoRunSpeed) || !HasFloat(ParamLocoPlayback))
            {
                SetSpeed(worldSpeedMps / Mathf.Max(ActorViewDefaults.MinPositive, normalizeRefMps), dampSec);
                return;
            }

            float run = Mathf.Max(ActorViewDefaults.MinPositive, _animator.GetFloat(ParamLocoRunSpeed));
            float model = worldSpeedMps / Mathf.Max(ActorViewDefaults.MinPositive, _animator.transform.lossyScale.y);
            if (model < run * ActorViewDefaults.LocoRunSpeedRatioFloor)
                model = 0f;
            float playback = Mathf.Clamp(model / run, 1f, Mathf.Max(1f, maxPlaybackMult));
            if (dampSec > 0f && Time.deltaTime > 0f)
            {
                _animator.SetFloat(ParamSpeed, Mathf.Min(model, run), dampSec, Time.deltaTime);
                _animator.SetFloat(ParamLocoPlayback, playback, dampSec, Time.deltaTime);
            }
            else
            {
                _animator.SetFloat(ParamSpeed, Mathf.Min(model, run));
                _animator.SetFloat(ParamLocoPlayback, playback);
            }
        }

        bool HasFloat(string name)
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Float)
                    return true;
            }
            return false;
        }

        void ApplyStrikeSpeedBeforeAction()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            if (_strikeSpeedParamCached < 0)
                _strikeSpeedParamCached = HasFloat(ParamStrikeSpeed) ? 1 : 0;
            if (_strikeSpeedParamCached == 1)
                _animator.SetFloat(ParamStrikeSpeed, BasicStrikeAnimSpeed);
        }

        /// <summary>Aksiyon state'ine crossfade süresi (0 = sert kesim).</summary>
        public float CrossFadeSec { get; set; }

        public void ResetToLocomotion()
        {
            PlayState(StateLocomotion);
        }

        public static string AnimationTypeToState(string animationTypeId)
        {
            if (string.IsNullOrEmpty(animationTypeId))
                return "CastChannel";

            return animationTypeId switch
            {
                "cast_projectile" => "CastPierce",
                "cast_aoe" => "CastSweep",
                "cast_self" => "CastChannel",
                "channel" => "CastChannel",
                "melee_thrust" => "CastPierce",
                "melee_slash" => "CastSweep",
                "melee_punch" => "CastSlam",
                "dash" => StateDodge,
                "instant" => "CastSlam",
                "summon" => "CastChannel",
                _ => AnimationBridge.MapToQuaterniusState(animationTypeId)
            };
        }

        void ApplyAxes(EffectSilhouette s)
        {
            SafeSetFloat(ParamFocus, s.Focus);
            SafeSetFloat(ParamPierce, s.Pierce);
            SafeSetFloat(ParamSpread, s.Spread);
            SafeSetFloat(ParamLift, s.Lift);
        }

        void FireFamily(CastBodyFamily family)
        {
            int i = (int)family;
            if (i <= 0 || i >= FamilyStates.Length)
                return;
            PlayState(FamilyStates[i]);
        }

        void PlayState(string stateName) => PlayAction(stateName);

        /// <summary>
        /// Aksiyon state'i oynatır. Menzilli teslimde mermi cast'i atış klibine düşer; karakter
        /// hareket ederken Cast/vuruş state'i varsa üst gövde katmanında oynar (bacaklar
        /// Locomotion'da kalır). State yoksa false.
        /// </summary>
        public bool PlayAction(string stateName)
        {
            if (_animator == null || !_animator.isActiveAndEnabled
                || _animator.runtimeAnimatorController == null
                || string.IsNullOrEmpty(stateName))
                return false;

            if (RangedDelivery && stateName == StateCastPierce
                && _animator.HasState(0, Animator.StringToHash(StateCastShoot)))
                stateName = StateCastShoot;

            if (IsMoving() && TryPlayUpper(stateName))
                return true;

            int hash = Animator.StringToHash(stateName);
            if (!_animator.HasState(0, hash))
                return false;

            CrossFadeOrRestart(0, hash);
            ClearUpper();
            return true;
        }

        void ClearUpper()
        {
            if (_upperLayer == -2)
                _upperLayer = _animator.GetLayerIndex(UpperLayerName);
            if (_upperLayer < 0)
                return;
            int empty = Animator.StringToHash("Empty");
            if (_animator.HasState(_upperLayer, empty)
                && _animator.GetCurrentAnimatorStateInfo(_upperLayer).shortNameHash != empty)
                _animator.CrossFadeInFixedTime(empty, CrossFadeSec, _upperLayer, 0f);
        }

    }
}
