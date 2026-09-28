using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Humanoid Animator kancası. Mixamo/Synty controller state'lerine
    /// <see cref="Animator.Play"/> ile gider (SetTrigger yok — controller'da trigger şart değil).
    /// </summary>
    public sealed class ActorVisual : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        public const string ParamFocus = "Focus";
        public const string ParamPierce = "Pierce";
        public const string ParamSpread = "Spread";
        public const string ParamLift = "Lift";
        public const string StateDodge = "Dodge";
        public const string StateHit = "Hit";
        public const string StateDeath = "Death";
        public const string StateBasicStrike = "BasicStrike";
        public const string StateLocomotion = "Locomotion";

        // Eski sabit isimleri — Trigger*() çağrıları artık Play(state).
        public const string TriggerDodge = StateDodge;
        public const string TriggerHit = StateHit;
        public const string TriggerDeath = StateDeath;
        public const string TriggerBasicStrike = StateBasicStrike;

        static readonly string[] FamilyStates =
        {
            null,
            "CastPierce",
            "CastSweep",
            "CastSlam",
            "CastChannel",
            "CastGuard",
        };

        [SerializeField] Animator _animator;
        [SerializeField] Renderer[] _hideWhenVisualPresent;

        public Animator Animator => _animator;

        public void Bind(Animator animator, params Renderer[] hideWhenPresent)
        {
            _animator = animator;
            _hideWhenVisualPresent = hideWhenPresent;
            if (_animator != null && _hideWhenVisualPresent != null)
            {
                for (int i = 0; i < _hideWhenVisualPresent.Length; i++)
                {
                    if (_hideWhenVisualPresent[i] != null)
                        _hideWhenVisualPresent[i].enabled = false;
                }
            }
        }

        public void PulseRune(Rune rune, EffectSilhouette silhouette)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            CastBodyFamily family = CastBodyMapper.FromVerbAndSilhouette(rune, silhouette);
            ApplyAxes(silhouette);
            FireFamily(family);
        }

        public void PulseFamily(CastBodyFamily family, EffectSilhouette silhouette)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            ApplyAxes(silhouette);
            FireFamily(family);
        }

        /// <summary>
        /// element-sistemi <c>animation_type</c> → controller state (skill başına farklı clip).
        /// </summary>
        public void PulseAnimationType(string animationTypeId, EffectSilhouette silhouette)
        {
            if (_animator == null || !_animator.isActiveAndEnabled)
                return;

            ApplyAxes(silhouette);
            string state = AnimationTypeToState(animationTypeId);
            PlayState(state);
        }

        /// <summary>Merkez düz vuruş — jab clip.</summary>
        public void PulseBasicStrike()
        {
            PlayState(StateBasicStrike);
        }

        public void Trigger(string triggerOrStateName)
        {
            // Eski API: isim state olarak Play edilir (Mixamo'da trigger yok).
            PlayState(triggerOrStateName);
        }

        /// <summary>0 = idle (sabit), 1 ≈ koşu. Küçük stick gürültüsü idle fidget’e sızmasın.</summary>
        public void SetSpeed(float normalized01) => SetSpeed(normalized01, 0f);

        /// <summary>Sönümlü Speed; <paramref name="dampSec"/> 0 ise anında.</summary>
        public void SetSpeed(float normalized01, float dampSec)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            float s = Mathf.Clamp01(normalized01);
            if (s < 0.08f)
                s = 0f;
            if (dampSec > 0f && Time.deltaTime > 0f)
                _animator.SetFloat(ParamSpeed, s, dampSec, Time.deltaTime);
            else
                _animator.SetFloat(ParamSpeed, s);
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

        void PlayState(string stateName)
        {
            if (_animator == null || !_animator.isActiveAndEnabled
                || _animator.runtimeAnimatorController == null
                || string.IsNullOrEmpty(stateName))
                return;

            int hash = Animator.StringToHash(stateName);
            if (!_animator.HasState(0, hash))
                return;

            bool sameState = _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == hash
                             || (_animator.IsInTransition(0)
                                 && _animator.GetNextAnimatorStateInfo(0).shortNameHash == hash);
            if (CrossFadeSec > 0f && !sameState)
            {
                _animator.CrossFadeInFixedTime(hash, CrossFadeSec, 0, 0f);
                return;
            }

            _animator.Play(hash, 0, 0f);
            _animator.Update(0f);
        }

        void SafeSetFloat(string name, float value)
        {
            foreach (var p in _animator.parameters)
            {
                if (p.name == name && p.type == AnimatorControllerParameterType.Float)
                {
                    _animator.SetFloat(name, value);
                    return;
                }
            }
        }
    }
}
