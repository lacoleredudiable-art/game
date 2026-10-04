using Dovus.Core.Presentation;
using Dovus.Game.Skills;
using System;
using UnityEngine;

namespace Dovus.Game.Actors
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
        bool _vfxFired;
        int? _spawnVfxFrame;

        public event Action<double> SpawnVfxFrameReached;

        public bool IsPlaying => _playing;
        public AnimationFrameNode Current => _node;
        public double StartWorldMs => _startWorldMs;

        /// <summary>Son Play'de Controller'da state vardı ve Animator.Play çağrıldı.</summary>
        public bool LastPlayApplied { get; private set; }
        public string LastClipName { get; private set; } = string.Empty;
        public bool LastExactClipFound { get; private set; }
        public bool LastUsedFallbackState { get; private set; }

        /// <summary>
        /// animator_state'i Play eder (yoksa uyarı, fırlatmaz) ve frame-timer'ı
        /// <paramref name="worldMs"/> anından başlatır.
        /// </summary>
        /// <returns>State Controller'da vardıysa true.</returns>
        public bool Play(AnimationFrameNode node, Animator animator, double worldMs)
        {
            Stop();
            StartFrameTimer(node, worldMs);
            LastPlayApplied = TryPlayState(animator, node.AnimatorState);
            return LastPlayApplied;
        }

        /// <summary>
        /// Animator'a dokunmadan yalnız kare zamanlayıcısını başlatır (VFX çıkış anı için).
        /// Son Play sonuç alanlarını silmez.
        /// </summary>
        public void StartFrameTimer(AnimationFrameNode node, double worldMs)
        {
            _node = node;
            _startWorldMs = worldMs;
            _playing = true;
            _vfxFired = false;
            _spawnVfxFrame = node.SpawnVfxAtFrame;
        }

        /// <summary>
        /// Atanırsa state'ler Animator.Play yerine buradan oynar (ActorVisual crossfade +
        /// üst gövde yönlendirmesi). null → doğrudan Animator.Play.
        /// </summary>
        public Func<string, bool> StatePlayer { get; set; }

        /// <summary>
        /// v6 AnimationDatabase'in mevcut controller state'i. Frame metadata yoksa yalnız
        /// Animator oynatılır; state eksikse false/no-op.
        /// </summary>
        public bool PlayState(Animator animator, string stateName)
        {
            Stop();
            LastPlayApplied = TryPlayState(animator, stateName);
            return LastPlayApplied;
        }

        /// <summary>
        /// JSON animasyon adını controller clip listesinde arar; exact clip/state yoksa
        /// AnimationDatabase'in mevcut generic Cast* state'ine düşer.
        /// </summary>
        public bool PlayBinding(AnimationBinding binding, Animator animator)
        {
            Stop();
            AnimationClip clip = FindClip(animator, binding.DisplayName);
            LastExactClipFound = clip != null;
            LastClipName = clip != null ? clip.name : string.Empty;
            if (clip != null && TryPlayState(animator, clip.name))
            {
                LastUsedFallbackState = false;
                LastPlayApplied = true;
                return true;
            }

            LastUsedFallbackState = true;
            LastPlayApplied = TryPlayState(animator, binding.AnimatorState);
            return LastPlayApplied;
        }

        public void Stop()
        {
            _playing = false;
            _vfxFired = false;
            _spawnVfxFrame = null;
            LastPlayApplied = false;
            LastClipName = string.Empty;
            LastExactClipFound = false;
            LastUsedFallbackState = false;
        }

        static AnimationClip FindClip(Animator animator, string displayName)
        {
            if (animator == null || animator.runtimeAnimatorController == null
                || string.IsNullOrEmpty(displayName))
                return null;
            string wanted = NormalizeClipName(displayName);
            AnimationClip[] clips = animator.runtimeAnimatorController.animationClips;
            for (int i = 0; i < clips.Length; i++)
            {
                AnimationClip clip = clips[i];
                if (clip != null && NormalizeClipName(clip.name) == wanted)
                    return clip;
            }
            return null;
        }

        static string NormalizeClipName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var chars = new System.Text.StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
                if (char.IsLetterOrDigit(value[i]))
                    chars.Append(char.ToLowerInvariant(value[i]));
            return chars.ToString();
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

        bool TryPlayState(Animator animator, string stateName)
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

            if (StatePlayer != null)
                return StatePlayer(stateName);

            animator.Play(stateName, LayerIndex, 0f);
            animator.Update(0f);
            return true;
        }

        /// <summary>
        /// prezentasyon-katmani animator_state → Synty/Mixamo controller state.
        /// Üç büyü tipi + dash bilinçli ayrılır (hepsi aynı Spell_Cast olmasın).
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
                // Büyü çeşitleri: mermi=thrust, alan=savurma, self/channel=büyü cast
                "Spell_Cast_Projectile" => "CastPierce",
                "Spell_Cast_AoE" => "CastSweep",
                "Spell_Cast_Self" => "CastChannel",
                "Channel_Loop" => "CastChannel",
                "Dash" => "Dodge",
                "Instant" => "CastSlam",
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
    }
}
