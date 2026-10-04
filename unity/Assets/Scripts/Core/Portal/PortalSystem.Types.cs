using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core.Motion;
using Dovus.Core.Status;

namespace Dovus.Core.Portal
{
    public sealed partial class PortalSystem
    {
        sealed class Door
        {
            public int Id;
            public int Link;
            public float X;
            public float Z;
            public float Fx;
            public float Fz;
            public string Skill;
            public float Until;
            public bool ShotsOnly;
            public bool Gate;
            public bool Grow;
            public bool PendingArrival;
            public int Owner;
        }

        struct Anchor
        {
            public bool Alive;
            public float X;
            public float Z;
            public float Until;
        }

        readonly struct WaitMove
        {
            public WaitMove(int waitFor, int actorId, float x, float y, float z, float radius, string skill, bool transfer, bool teleport = false)
            {
                WaitFor = waitFor;
                ActorId = actorId;
                X = x;
                Y = y;
                Z = z;
                Radius = radius;
                Skill = skill;
                Transfer = transfer;
                Teleport = teleport;
            }

            public int WaitFor { get; }
            public int ActorId { get; }
            public float X { get; }
            public float Y { get; }
            public float Z { get; }
            public float Radius { get; }
            public string Skill { get; }
            public bool Transfer { get; }
            public bool Teleport { get; }
        }

        readonly struct Buff
        {
            public Buff(int actorId, float until, float move, float damage, float taken, float miss)
            {
                ActorId = actorId;
                Until = until;
                Move = move;
                Damage = damage;
                Taken = taken;
                Miss = miss;
            }

            public int ActorId { get; }
            public float Until { get; }
            public float Move { get; }
            public float Damage { get; }
            public float Taken { get; }
            public float Miss { get; }
        }

        sealed class Rise
        {
            public int ActorId;
            public float At;
            public float Radius;
            public float Y;
            public string Skill = string.Empty;
            public bool Done;
        }

        sealed class Gather
        {
            public float At;
            public bool Done;
            public string Skill = string.Empty;
            public HashSet<int> Skip;
        }
    }
}
