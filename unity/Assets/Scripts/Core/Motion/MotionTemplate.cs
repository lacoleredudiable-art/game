using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Motion
{
    public sealed class MotionTemplate
    {
        public MotionTemplate(
            string id,
            string name,
            int familyId,
            string familyName,
            bool implemented,
            IReadOnlyList<MotionPhase> phases,
            string aim = "effect")
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            FamilyId = familyId;
            FamilyName = familyName ?? string.Empty;
            Implemented = implemented;
            Phases = phases ?? Array.Empty<MotionPhase>();
            Aim = string.Equals(aim, MotionAim.Enemy, StringComparison.Ordinal) ? MotionAim.Enemy : MotionAim.Effect;
        }

        public string Id { get; }
        public string Name { get; }
        public int FamilyId { get; }
        public string FamilyName { get; }
        public bool Implemented { get; }
        public IReadOnlyList<MotionPhase> Phases { get; }
        /// <summary>"enemy" hareket hedefi düşmandır; "effect" fiilin etki hedefidir.</summary>
        public string Aim { get; }

        public MotionTemplate WithPhases(IReadOnlyList<MotionPhase> phases)
        {
            if (phases == null)
                return this;
            if (phases.Count == Phases.Count)
            {
                bool same = true;
                for (int i = 0; i < phases.Count; i++)
                {
                    if (!ReferenceEquals(phases[i], Phases[i]))
                    {
                        same = false;
                        break;
                    }
                }
                if (same)
                    return this;
            }
            return new MotionTemplate(Id, Name, FamilyId, FamilyName, Implemented, phases, Aim);
        }
    }
}
