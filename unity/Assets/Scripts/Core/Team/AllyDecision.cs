using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    public readonly struct AllyDecision
    {
        public AllyDecision(AllyActionKind kind, float x, float z, int targetId)
        {
            Kind = kind; X = x; Z = z; TargetId = targetId;
        }

        public AllyActionKind Kind { get; }
        /// <summary>MoveTo için varış, Attack için bakılacak nokta, Dodge için kaçış yönü hedefi.</summary>
        public float X { get; }
        public float Z { get; }
        public int TargetId { get; }

        public static AllyDecision Idle => new AllyDecision(AllyActionKind.Idle, 0f, 0f, -1);
        public static AllyDecision Move(float x, float z) => new AllyDecision(AllyActionKind.MoveTo, x, z, -1);
        public static AllyDecision Strike(AllyTarget t) => new AllyDecision(AllyActionKind.Attack, t.X, t.Z, t.Id);
        public static AllyDecision DodgeTo(float x, float z) => new AllyDecision(AllyActionKind.Dodge, x, z, -1);
    }
}
