using Dovus.Core.Grammar;
using System;

namespace Dovus.Core.Presentation
{
    public static class AnimationDamageSchedule
    {
        public static bool TryReadSingleFrame(AnimationFrameNode node, out int frame)
        {
            frame = 0;
            JsonValue d = node.DamageAppliedAtFrame;
            if (d == null || d.IsNull || d.Kind != JsonKind.Number)
                return false;
            frame = d.AsInt();
            return true;
        }

        public static bool TryReadEveryTick(
            AnimationFrameNode node,
            out int startFrame,
            out int endFrame)
        {
            startFrame = 0;
            endFrame = 0;
            JsonValue d = node.DamageAppliedAtFrame;
            if (d == null || d.IsNull || d.Kind != JsonKind.String)
                return false;
            string s = d.AsString();
            if (!string.Equals(s, "every_tick", StringComparison.Ordinal))
                return false;
            int[] active = node.ActiveFrames;
            if (active != null && active.Length >= 2)
            {
                startFrame = active[0];
                endFrame = active[1];
            }
            else
            {
                startFrame = 0;
                endFrame = Math.Max(0, node.TotalFrames - 1);
            }
            return true;
        }
    }
}
