using System;
using Dovus.Core.Grammar;
using Dovus.Core.Presentation;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Presentation AnimationFrameNode → Animator.Play + worldMs frame-timer.
    /// ManifestationDirector.ShoutSkill üzerinden bağlanır (Bağlama 10).
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

        /// <summary>Son Play'de Controller'da state vardı ve Animator.Play çağrıldı.</summary>
        public bool LastPlayApplied { get; private set; }

        /// <summary>
        /// animator_state'i Play eder (yoksa uyarı, fırlatmaz) ve frame-timer'ı
        /// <paramref name="worldMs"/> anından başlatır.
        /// </summary>
        /// <returns>State Controller'da vardıysa true.</returns>
        public bool Play(AnimationFrameNode node, Animator animator, double worldMs)
        {
            Stop();
            _node = node;
            _startWorldMs = worldMs;
            _playing = true;
            _damageFired = false;
            _vfxFired = false;
            ParseDamageSchedule(node);
            _spawnVfxFrame = node.SpawnVfxAtFrame;

            LastPlayApplied = TryPlayState(animator, node.AnimatorState);
            return LastPlayApplied;
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
            LastPlayApplied = false;
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

        static bool TryPlayState(Animator animator, string stateName)
        {
            if (animator == null || !animator.isActiveAndEnabled
                || animator.runtimeAnimatorController == null)
            {
                // SafeSetFloat deseni: sessiz atla, hata fırlatma.
                return false;
            }

            if (string.IsNullOrEmpty(stateName))
                return false;

            stateName = MapToQuaterniusState(stateName);

            // Controller'da yoksa sessiz atla (Quaternius ↔ JSON eşlemesi).
            if (!HasState(animator, stateName))
                return false;

            animator.Play(stateName, LayerIndex, 0f);
            animator.Update(0f);
            return true;
        }

        /// <summary>
        /// prezentasyon-katmani animator_state → Quaternius Player controller state.
        /// </summary>
        public static string MapToQuaterniusState(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
                return stateName;

            return stateName switch
            {
                "Melee_Thrust" => "CastPierce",
                "Melee_Slash" => "CastSweep",
                "Melee_Punch" => "CastSlam",
                "Spell_Cast_Projectile" => "CastPierce",
                "Spell_Cast_AoE" => "CastSlam",
                "Spell_Cast_Self" => "CastChannel",
                "Channel_Loop" => "CastChannel",
                "Dash" => "CastGuard",
                "Instant" => "CastChannel",
                "Summon" => "CastChannel",
                _ => stateName
            };
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
