using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Passives
{
    public readonly struct PassiveBounceCandidate
    {
        public PassiveBounceCandidate(int id, float distanceM)
        {
            Id = id;
            DistanceM = distanceM;
        }

        public int Id { get; }
        public float DistanceM { get; }
    }
}
