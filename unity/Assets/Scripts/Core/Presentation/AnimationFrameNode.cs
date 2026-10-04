using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Presentation
{
    public readonly struct AnimationFrameNode
    {
        public AnimationFrameNode(
            string id,
            int totalFrames,
            int totalDurationMs,
            int startupFrames,
            int[] activeFrames,
            int recoveryFrames,
            int[]? cancelWindow,
            JsonValue damageAppliedAtFrame,
            int? spawnVfxAtFrame,
            string animatorState)
        {
            Id = id ?? string.Empty;
            TotalFrames = totalFrames;
            TotalDurationMs = totalDurationMs;
            StartupFrames = startupFrames;
            ActiveFrames = activeFrames ?? new[] { 0, 0 };
            RecoveryFrames = recoveryFrames;
            CancelWindow = cancelWindow;
            DamageAppliedAtFrame = damageAppliedAtFrame ?? JsonValue.Null;
            SpawnVfxAtFrame = spawnVfxAtFrame;
            AnimatorState = animatorState ?? string.Empty;
        }

        public string Id { get; }
        public int TotalFrames { get; }
        public int TotalDurationMs { get; }
        public int StartupFrames { get; }
        /// <summary>[start, end] dahil aralık.</summary>
        public int[] ActiveFrames { get; }
        public int RecoveryFrames { get; }
        /// <summary>Yoksa null (channel/summon).</summary>
        public int[]? CancelWindow { get; }
        /// <summary>Sayı, "every_tick" veya null — tip zorlanmaz.</summary>
        public JsonValue DamageAppliedAtFrame { get; }
        public int? SpawnVfxAtFrame { get; }
        public string AnimatorState { get; }
    }
}
