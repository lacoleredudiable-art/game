using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Boss Animator kancası. Prefab/Controller yoksa no-op.
    /// Clip sözleşmesi: docs/animasyon-omurgasi.md — Idle, Windup, Slam, Stagger, Death.
    /// </summary>
    public sealed class BossVisual : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        public const string TriggerIdle = "Idle";
        public const string TriggerWindup = "Windup";
        public const string TriggerSlam = "Slam";
        public const string TriggerStagger = "Stagger";
        public const string TriggerDeath = "Death";

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

        public void SetSpeed(float normalized01)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null)
                return;
            _animator.SetFloat(ParamSpeed, Mathf.Clamp01(normalized01));
        }

        public void PlayIdle() => Fire(TriggerIdle);
        public void PlayWindup() => Fire(TriggerWindup);
        public void PlaySlam() => Fire(TriggerSlam);
        public void PlayStagger() => Fire(TriggerStagger);
        public void PlayDeath() => Fire(TriggerDeath);

        void Fire(string trigger)
        {
            if (_animator == null || !_animator.isActiveAndEnabled || _animator.runtimeAnimatorController == null
                || string.IsNullOrEmpty(trigger))
                return;
            _animator.SetTrigger(trigger);
        }
    }
}
