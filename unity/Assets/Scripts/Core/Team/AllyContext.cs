using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    public readonly struct AllyContext
    {
        public AllyContext(
            int selfId, float x, float z, float radius, AllyRole role, float attackRangeM,
            float playerX, float playerZ,
            IReadOnlyList<AllyDanger> dangers,
            TeamTaskZone? assignedZone,
            IReadOnlyList<TeamTaskZone> shelters,
            AllyTarget? clawTarget, AllyTarget? headTarget)
        {
            SelfId = selfId; X = x; Z = z; Radius = radius; Role = role; AttackRangeM = attackRangeM;
            PlayerX = playerX; PlayerZ = playerZ;
            Dangers = dangers ?? Array.Empty<AllyDanger>();
            AssignedZone = assignedZone;
            Shelters = shelters ?? Array.Empty<TeamTaskZone>();
            ClawTarget = clawTarget; HeadTarget = headTarget;
        }

        public int SelfId { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public AllyRole Role { get; }
        public float AttackRangeM { get; }
        public float PlayerX { get; }
        public float PlayerZ { get; }
        public IReadOnlyList<AllyDanger> Dangers { get; }
        /// <summary>Görevde bu dosta ayrılan bölge (<see cref="TeamTaskPlanner.AssignAllies"/>); görev yoksa null.</summary>
        public TeamTaskZone? AssignedZone { get; }
        /// <summary>Arena çapında saldırıda sığınılacak taş gölgeleri.</summary>
        public IReadOnlyList<TeamTaskZone> Shelters { get; }
        public AllyTarget? ClawTarget { get; }
        public AllyTarget? HeadTarget { get; }
    }
}
