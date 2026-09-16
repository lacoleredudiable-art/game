using Dovus.Core.Grammar;
using Dovus.Core.Manifestation;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Humanoid Animator kancası. Prefab/Controller yoksa no-op (kapsül prototip çalışır).
    /// Clip isimleri docs/animasyon-omurgasi.md ile hizalı: CastPierce, CastSweep, …
    /// </summary>
    public sealed class ActorVisual : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        public const string ParamFocus = "Focus";
        public const string ParamPierce = "Pierce";
        public const string ParamSpread = "Spread";
        public const string ParamLift = "Lift";
        public const string TriggerDodge = "Dodge";
        public const string TriggerHit = "Hit";
        public const string TriggerDeath = "Death";
        public const string TriggerBasicStrike = "BasicStrike";
        public const string StateLocomotion = "Locomotion";
        static readonly int BasicStrikeHash = Animator.StringToHash(TriggerBasicStrike);

        static readonly int[] FamilyTriggers =
        {
            0,
            Animator.StringToHash("CastPierce"),
            Animator.StringToHash("CastSweep"),
            Animator.StringToHash("CastSlam"),
            Animator.StringToHash("CastChannel"),
            Animator.StringToHash("CastGuard"),
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

        /// <summary>Merkez düz vuruş — ayrı jab clip (Sword_AttackFast), skill cast ailelerinden ayrı.</summary>
        public void PulseBasicStrike()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            _animator.ResetTrigger(TriggerBasicStrike);
            _animator.SetTrigger(BasicStrikeHash);
        }

        public void Trigger(string triggerName)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null
                || string.IsNullOrEmpty(triggerName))
                return;
            _animator.SetTrigger(triggerName);
        }

        /// <summary>0 = idle, 1 ≈ koşu. Controller'da yoksa no-op.</summary>
        public void SetSpeed(float normalized01)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            _animator.SetFloat(ParamSpeed, Mathf.Clamp01(normalized01));
        }

        public void ResetToLocomotion()
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            _animator.ResetTrigger(TriggerDeath);
            _animator.ResetTrigger(TriggerHit);
            _animator.ResetTrigger(TriggerDodge);
            _animator.Play(StateLocomotion, 0, 0f);
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
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            int i = (int)family;
            if (i <= 0 || i >= FamilyTriggers.Length)
                return;
            _animator.SetTrigger(FamilyTriggers[i]);
        }

        void SafeSetFloat(string name, float value)
        {
            // Parametre Controller'da yoksa Unity uyarı basmaz; SetFloat no-op gibi davranır
            // yalnızca hash biliniyorsa. Yokluğu göze alıyoruz — clip gelince eklenir.
            _animator.SetFloat(name, value);
        }
    }
}
