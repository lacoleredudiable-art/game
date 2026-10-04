using System;
using Dovus.Core.Grammar;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Casting
{
    /// <summary>Saf C# hareket planı — Unity transform'a Game katmanı uygular.</summary>
    public readonly struct SkillMotionPlan
    {
        public static SkillMotionPlan None { get; } = default;

        public SkillMotionPlan(
            SkillMotionKind kind,
            float destX, float destZ,
            float faceX, float faceZ,
            float durationSec,
            int iframeMs,
            float slashCommitMult,
            string markType)
        {
            Kind = kind;
            DestX = destX;
            DestZ = destZ;
            FaceX = faceX;
            FaceZ = faceZ;
            DurationSec = durationSec;
            IframeMs = iframeMs;
            SlashCommitMult = slashCommitMult;
            MarkType = markType ?? string.Empty;
        }

        public SkillMotionKind Kind { get; }
        public float DestX { get; }
        public float DestZ { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public float DurationSec { get; }
        public int IframeMs { get; }
        /// <summary>&gt;0 ise BaseDamage=0 olsa da commit × bu kadar boss hasarı.</summary>
        public float SlashCommitMult { get; }
        public string MarkType { get; }
        public bool IsEmpty => Kind == SkillMotionKind.None;
    }
}
