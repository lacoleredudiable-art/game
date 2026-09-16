using System;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Presentation AnimationFrameNode → Animator.Play + worldMs frame-timer.
    /// Bootstrap / ManifestationDirector'a bağlanmaz (Faz 6).
    /// </summary>
    public sealed class AnimationBridge
    {
        const int LayerIndex = 0;

        AnimationFrameNode _node;
        double _startWorldMs;
        bool _playing;
        bool _damageFired;
        bool _vfxFired;
        int _nextEveryTickFrame;
        int _everyTickEndFrame;
        int? _damageFrame;
        bool _damageEveryTick;
        int? _spawnVfxFrame;

        public event Action<double> DamageFrameReached;
        public event Action<double> SpawnVfxFrameReached;

        public bool IsPlaying => _playing;
        public AnimationFrameNode Current => _node;
        public double StartWorldMs => _startWorldMs;

        /// <summary>
        /// animator_state'i Play eder (yoksa uyarı, fırlatmaz) ve frame-timer'ı
        /// <paramref name="worldMs"/> anından başlatır.
        /// </summary>
        public void Play(AnimationFrameNode node, Animator animator, double worldMs)
        {
            Stop();
            _node = node;
            _startWorldMs = worldMs;
            _playing = true;
            _damageFired = false;
            _vfxFired = false;
            ParseDamageSchedule(node);
            _spawnVfxFrame = node.SpawnVfxAtFrame;

            TryPlayState(animator, node.AnimatorState);
        }

        public void Stop()
        {
            _playing = false;
            _damageFired = false;
            _vfxFired = false;
            _nextEveryTickFrame = 0;
            _everyTickEndFrame = 0;
            _damageFrame = null;
            _damageEveryTick = false;
            _spawnVfxFrame = null;
        }

        /// <summary>worldMs'e göre kare olaylarını ateşler. Animasyon bitince IsPlaying false.</summary>
        public void Tick(double worldMs)
        {
            if (!_playing)
                return;

            int totalFrames = Math.Max(1, _node.TotalFrames);
            int totalDurationMs = Math.Max(0, _node.TotalDurationMs);
            double elapsedMs = worldMs - _startWorldMs;

            if (_spawnVfxFrame.HasValue && !_vfxFired)
            {
                double at = FrameToElapsedMs(_spawnVfxFrame.Value, totalFrames, totalDurationMs);
                if (elapsedMs + 1e-6 >= at)
                {
                    _vfxFired = true;
                    SpawnVfxFrameReached?.Invoke(_startWorldMs + at);
                }
            }

            if (_damageEveryTick)
            {
                while (_nextEveryTickFrame <= _everyTickEndFrame)
                {
                    double at = FrameToElapsedMs(_nextEveryTickFrame, totalFrames, totalDurationMs);
                    if (elapsedMs + 1e-6 < at)
                        break;
                    _nextEveryTickFrame++;
                    DamageFrameReached?.Invoke(_startWorldMs + at);
                }
            }
            else if (_damageFrame.HasValue && !_damageFired)
            {
                double at = FrameToElapsedMs(_damageFrame.Value, totalFrames, totalDurationMs);
                if (elapsedMs + 1e-6 >= at)
                {
                    _damageFired = true;
                    DamageFrameReached?.Invoke(_startWorldMs + at);
                }
            }

            if (elapsedMs + 1e-6 >= totalDurationMs)
                _playing = false;
        }

        /// <summary>
        /// Kare → süre: frame * (total_duration_ms / total_frames).
        /// Saniye cinsinden: dönüş / 1000.
        /// </summary>
        public static double FrameToElapsedMs(int frame, int totalFrames, int totalDurationMs)
        {
            if (totalFrames <= 0 || totalDurationMs <= 0)
                return 0.0;
            return frame * ((double)totalDurationMs / totalFrames);
        }

        public static double FrameToElapsedSeconds(int frame, int totalFrames, int totalDurationMs) =>
            FrameToElapsedMs(frame, totalFrames, totalDurationMs) / 1000.0;

        static void TryPlayState(Animator animator, string stateName)
        {
            if (animator == null || !animator.isActiveAndEnabled
                || animator.runtimeAnimatorController == null)
            {
                Debug.LogWarning(
                    "[AnimationBridge] Animator yok/kapalı — Play atlandı.");
                return;
            }

            if (string.IsNullOrEmpty(stateName))
            {
                Debug.LogWarning(
                    "[AnimationBridge] animator_state boş — Play atlandı.");
                return;
            }

            // SafeSetFloat deseni: Controller'da yoksa uyarı, hata fırlatma.
            if (!HasState(animator, stateName))
            {
                Debug.LogWarning(
                    $"[AnimationBridge] Animator state yok: '{stateName}' — Play atlandı.");
                return;
            }

            animator.Play(stateName, LayerIndex, 0f);
            animator.Update(0f);
        }

        static bool HasState(Animator animator, string stateName)
        {
            int hash = Animator.StringToHash(stateName);
            int layers = animator.layerCount;
            for (int i = 0; i < layers; i++)
            {
                if (animator.HasState(i, hash))
                    return true;
            }
            return false;
        }

        void ParseDamageSchedule(AnimationFrameNode node)
        {
            _damageFrame = null;
            _damageEveryTick = false;
            _nextEveryTickFrame = 0;
            _everyTickEndFrame = 0;

            JsonValue d = node.DamageAppliedAtFrame;
            if (d == null || d.IsNull)
                return;

            if (d.Kind == JsonKind.String)
            {
                string s = d.AsString();
                if (string.Equals(s, "every_tick", StringComparison.Ordinal))
                {
                    _damageEveryTick = true;
                    int[] active = node.ActiveFrames;
                    if (active != null && active.Length >= 2)
                    {
                        _nextEveryTickFrame = active[0];
                        _everyTickEndFrame = active[1];
                    }
                    else
                    {
                        _nextEveryTickFrame = 0;
                        _everyTickEndFrame = Math.Max(0, node.TotalFrames - 1);
                    }
                }
                return;
            }

            if (d.Kind == JsonKind.Number)
                _damageFrame = d.AsInt();
        }
    }
}
