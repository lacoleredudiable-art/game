using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>Bir dost aday. Konum düzlem (x, z); seçim konuma bakmaz, co-op tehdidi için saklanır.</summary>
    public readonly struct HostileCandidate
    {
        public readonly int Id;
        public readonly TargetKind Kind;
        public readonly float X;
        public readonly float Z;
        public readonly bool Alive;
        public readonly bool Stealthed;
        public readonly bool Taunting;

        public HostileCandidate(int id, TargetKind kind, float x, float z, bool alive, bool stealthed, bool taunting)
        {
            Id = id;
            Kind = kind;
            X = x;
            Z = z;
            Alive = alive;
            Stealthed = stealthed;
            Taunting = taunting;
        }

        public bool Valid => Alive && !Stealthed;
    }
}
