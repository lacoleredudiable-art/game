using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;
using Dovus.Core.Status;

namespace Dovus.Core.Casting
{
    /// <summary>
    /// Unity'den bağımsız hedef adayı. DistanceM, Game katmanında hedef collider'ının en yakın
    /// noktasına ölçülür; böylece büyük boss'lar merkezlerinden dolayı haksızca menzil dışı kalmaz.
    /// </summary>
    public readonly struct TargetCandidate
    {
        public TargetCandidate(int id, TargetRelation relation, float distanceM, bool available = true)
        {
            Id = id;
            Relation = relation;
            DistanceM = Math.Max(0f, distanceM);
            Available = available;
        }

        public int Id { get; }
        public TargetRelation Relation { get; }
        public float DistanceM { get; }
        public bool Available { get; }
    }
}
