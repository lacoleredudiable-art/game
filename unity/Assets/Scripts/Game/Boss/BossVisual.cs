using Dovus.Core.Combat;
using Dovus.Game.Config;
using UnityEngine;

namespace Dovus.Game.Boss
{
    /// <summary>
    /// Boss Animator kancası. Controller state'lerine crossfade ile gider (Mixamo controller'da
    /// trigger yok); state yoksa eski trigger adına düşer, o da yoksa no-op.
    ///
    /// Tell'i veri belirler, klip değil: windup'ta saldırı klibinin hızı, darbe karesi
    /// (<see cref="PrototypeTuning.BossSlamImpactNorm"/>) tam <c>WindupMs</c> sonunda gelecek
    /// şekilde <see cref="ParamActionSpeed"/> ile ölçeklenir. Yürüme adımı yaklaşma hızına
    /// <see cref="ParamLocoSpeed"/> ile eşlenir (ayak kayması olmasın).
    /// </summary>
    public sealed class BossVisual : MonoBehaviour
    {
        public const string ParamSpeed = "Speed";
        public const string ParamLocoSpeed = "LocoSpeed";
        public const string ParamActionSpeed = "ActionSpeed";

        public const string StateLocomotion = "Locomotion";
        public const string StateSlam = "BossSlam";
        public const string StateBreath = "BossBreath";
        public const string StateRoar = "BossRoar";
        public const string StateStagger = "BossStagger";
        public const string StateDeath = "BossDeath";

        // Eski (Quaternius) controller trigger adları — state yoksa bunlara düşülür.
        public const string TriggerIdle = "Idle";
        public const string TriggerWindup = "Windup";
        public const string TriggerSlam = "Slam";
        public const string TriggerStagger = "Stagger";
        public const string TriggerDeath = "Death";

        [SerializeField] Animator _animator;
        [SerializeField] Renderer[] _hideWhenVisualPresent;

        PrototypeTuning _tuning;
        float _busyUntilUnscaled;
        float _lastStaggerUnscaled = -999f;
        float _walkClipMps = -1f;
        bool _dead;

        public Animator Animator => _animator;

        /// <summary>Saldırı/kükreme klibi oynuyor mu (stagger bunu kesmez).</summary>
        public bool IsBusy => Time.unscaledTime < _busyUntilUnscaled;

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

        public void Configure(PrototypeTuning tuning) => _tuning = tuning;

        /// <summary>0 = dur; &gt;0 = yürü, adım hızı <paramref name="groundMps"/>'ye eşlenir.</summary>
        public void SetWalk(float groundMps)
        {
            if (!Ready())
                return;
            if (groundMps <= 0.01f)
            {
                SetSpeed(0f);
                return;
            }

            _animator.SetFloat(ParamSpeed, 1f, 0.12f, Time.deltaTime);
            float clipMps = WalkClipMps();
            if (clipMps > 0.01f && HasParam(ParamLocoSpeed))
                _animator.SetFloat(ParamLocoSpeed, Mathf.Clamp(groundMps / clipMps, 0.2f, 2.5f));
        }

        public void SetSpeed(float normalized01)
        {
            if (!Ready())
                return;
            _animator.SetFloat(ParamSpeed, Mathf.Clamp01(normalized01), 0.12f, Time.deltaTime);
            if (normalized01 <= 0.01f && HasParam(ParamLocoSpeed))
                _animator.SetFloat(ParamLocoSpeed, 1f);
        }

        public void PlayIdle()
        {
            if (_dead)
                return;
            _busyUntilUnscaled = 0f;
            if (!CrossFade(StateLocomotion, BlendSec()))
                FireTrigger(TriggerIdle);
        }

        /// <summary>Windup başı: saldırı klibi, darbe karesi windup sonuna denk gelecek hızda.</summary>
        public void PlayWindup(BossAttackKind kind, int windupMs)
        {
            if (_dead || !Ready())
                return;
            // Zehir Tükürüğü ağızdan çıkar: nefes klibini paylaşır.
            bool mouth = kind is BossAttackKind.FireCone or BossAttackKind.Volley;
            string state = mouth ? StateBreath : StateSlam;
            float impactNorm = _tuning == null ? 0.42f
                : mouth ? _tuning.BossConeImpactNorm : _tuning.BossSlamImpactNorm;
            float clipLen = ClipLength(state);
            if (clipLen > 0f && HasParam(ParamActionSpeed))
            {
                float windupSec = Mathf.Max(0.05f, windupMs / 1000f);
                float speed = Mathf.Clamp(impactNorm * clipLen / windupSec, 0.25f, 4f);
                _animator.SetFloat(ParamActionSpeed, speed);
                _busyUntilUnscaled = Time.unscaledTime + windupSec + (1f - impactNorm) * clipLen;
            }

            if (!CrossFade(state, BlendSec()))
                FireTrigger(TriggerWindup);
        }

        /// <summary>Aktif pencere: klibin kalanı doğal hızda.</summary>
        public void PlaySlam()
        {
            if (_dead || !Ready())
                return;
            if (HasParam(ParamActionSpeed))
                _animator.SetFloat(ParamActionSpeed, 1f);
            else
                FireTrigger(TriggerSlam);
        }

        public void PlayStagger()
        {
            if (_dead || !Ready() || IsBusy)
                return;
            float gap = _tuning != null ? _tuning.BossStaggerMinGapSec : 0.6f;
            if (Time.unscaledTime - _lastStaggerUnscaled < gap)
                return;
            _lastStaggerUnscaled = Time.unscaledTime;
            if (!CrossFade(StateStagger, 0.05f))
                FireTrigger(TriggerStagger);
        }

        public void PlayRoar()
        {
            if (_dead || !Ready())
                return;
            float len = ClipLength(StateRoar);
            _busyUntilUnscaled = Time.unscaledTime + Mathf.Max(len, 0.1f);
            CrossFade(StateRoar, BlendSec());
        }

        public void PlayDeath()
        {
            _dead = true;
            _busyUntilUnscaled = 0f;
            if (!CrossFade(StateDeath, BlendSec()))
                FireTrigger(TriggerDeath);
        }

        public void NotifyRevived()
        {
            _dead = false;
            PlayIdle();
        }

        float BlendSec() => _tuning != null ? _tuning.BossAnimCrossFadeSec : 0.15f;

        bool Ready() =>
            _animator != null && _animator.isActiveAndEnabled && _animator.runtimeAnimatorController != null;

        bool CrossFade(string state, float blendSec)
        {
            if (!Ready())
                return false;
            int hash = Animator.StringToHash(state);
            if (!_animator.HasState(0, hash))
                return false;
            _animator.CrossFadeInFixedTime(hash, blendSec, 0, 0f);
            return true;
        }

        void FireTrigger(string trigger)
        {
            if (!Ready() || !HasParam(trigger))
                return;
            _animator.SetTrigger(trigger);
        }

        bool HasParam(string name)
        {
            var ps = _animator.parameters;
            for (int i = 0; i < ps.Length; i++)
            {
                if (ps[i].name == name)
                    return true;
            }
            return false;
        }

        /// <summary>Runtime'da state→klip eşlemesi okunamaz; bind aracının seçtiği adlarla aynı iğnelerle bulunur.</summary>
        float ClipLength(string state)
        {
            AnimationClip clip = FindClipFor(state);
            return clip != null ? clip.length : 0f;
        }

        AnimationClip FindClipFor(string state)
        {
            if (_animator == null || _animator.runtimeAnimatorController == null)
                return null;
            string[] needles = state switch
            {
                StateSlam => new[] { "slam", "jump attack", "melee thrust", "downward", "attack" },
                StateBreath => new[] { "breath", "cone", "spell cast", "2h magic", "roar" },
                StateRoar => new[] { "roar", "flex", "spell cast" },
                StateStagger => new[] { "stagger", "hit", "impact", "reaction" },
                StateDeath => new[] { "death", "dying" },
                _ => new[] { "walk" },
            };
            var clips = _animator.runtimeAnimatorController.animationClips;
            foreach (string n in needles)
            {
                for (int i = 0; i < clips.Length; i++)
                {
                    string name = clips[i].name.ToLowerInvariant().Replace('_', ' ');
                    if (name.Contains(n))
                        return clips[i];
                }
            }
            return null;
        }

        float WalkClipMps()
        {
            if (_walkClipMps >= 0f)
                return _walkClipMps;
            float fallback = _tuning != null ? _tuning.BossWalkClipMps : 1.4f;
            AnimationClip walk = FindClipFor(StateLocomotion);
            float mps = walk != null ? walk.averageSpeed.magnitude : 0f;
            if (mps > 0.05f && _animator.isHuman)
                mps *= _animator.humanScale;
            if (mps <= 0.05f)
                mps = fallback;
            _walkClipMps = mps * Mathf.Max(0.01f, _animator.transform.lossyScale.y);
            return _walkClipMps;
        }
    }
}
