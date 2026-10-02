using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Oyuncu→boss isabet hitstop'u: animatör hızı 0, parçacıklar durur, kamera sabit — simülasyon saati değil.
    /// </summary>
    public sealed class VisualFreeze : MonoBehaviour
    {
        struct AnimSlot
        {
            public Animator Anim;
            public float SavedSpeed;
        }

        struct ParticleSlot
        {
            public ParticleSystem Ps;
            public bool WasPlaying;
        }

        readonly List<AnimSlot> _animators = new();
        readonly List<ParticleSlot> _particles = new();
        FollowCamera _camera;
        float _untilUnscaled;
        bool _active;

        public bool IsActive => _active && Time.unscaledTime < _untilUnscaled;

        public void Bind(FollowCamera camera, params Animator[] animators)
        {
            _camera = camera;
            _animators.Clear();
            if (animators != null)
            {
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] != null)
                        _animators.Add(new AnimSlot { Anim = animators[i], SavedSpeed = 1f });
                }
            }

            _particles.Clear();
            if (animators != null)
            {
                for (int i = 0; i < animators.Length; i++)
                {
                    if (animators[i] == null)
                        continue;
                    var systems = animators[i].GetComponentsInChildren<ParticleSystem>(true);
                    for (int p = 0; p < systems.Length; p++)
                    {
                        ParticleSystem ps = systems[p];
                        if (ps != null)
                            _particles.Add(new ParticleSlot { Ps = ps, WasPlaying = false });
                    }
                }
            }
        }

        public void Trigger(float durationSec)
        {
            if (durationSec <= 0f)
                return;

            float end = Time.unscaledTime + durationSec;
            if (_active && end <= _untilUnscaled)
                return;
            _untilUnscaled = end;
            if (_active)
                return;

            _active = true;
            for (int i = 0; i < _animators.Count; i++)
            {
                AnimSlot s = _animators[i];
                if (s.Anim == null)
                    continue;
                s.SavedSpeed = s.Anim.speed <= 0.01f ? 1f : s.Anim.speed;
                s.Anim.speed = 0f;
                _animators[i] = s;
            }

            for (int i = 0; i < _particles.Count; i++)
            {
                ParticleSlot s = _particles[i];
                if (s.Ps == null)
                    continue;
                s.WasPlaying = s.Ps.isPlaying;
                if (s.WasPlaying)
                    s.Ps.Pause(true);
                _particles[i] = s;
            }

            if (_camera != null)
                _camera.VisualHoldUntilUnscaled = _untilUnscaled;
        }

        void Update()
        {
            if (!_active)
                return;
            if (Time.unscaledTime < _untilUnscaled)
                return;
            Release();
        }

        void OnDisable() => Release();

        void Release()
        {
            if (!_active)
                return;
            _active = false;

            for (int i = 0; i < _animators.Count; i++)
            {
                AnimSlot s = _animators[i];
                if (s.Anim != null)
                    s.Anim.speed = s.SavedSpeed;
            }

            for (int i = 0; i < _particles.Count; i++)
            {
                ParticleSlot s = _particles[i];
                if (s.Ps != null && s.WasPlaying)
                    s.Ps.Play(true);
            }

            if (_camera != null && _camera.VisualHoldUntilUnscaled <= Time.unscaledTime)
                _camera.VisualHoldUntilUnscaled = 0f;
        }
    }
}
