using System;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;

namespace Dovus.Core.Casting
{
    public readonly struct SkillExecutorRoute
    {
        public SkillExecutorRoute(SkillExecutorKind kind, bool isStub, string reason)
        {
            Kind = kind;
            IsStub = isStub;
            Reason = reason ?? string.Empty;
        }

        public SkillExecutorKind Kind { get; }
        public bool IsStub { get; }
        public string Reason { get; }
    }
}
