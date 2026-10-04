using System;
using Dovus.Core.Shared;
using System.Collections.Generic;
using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Team
{
    public sealed partial class TeamComboSystem
    {
        sealed class Mine
        {
            public int Owner;
            public float X;
            public float Z;
            public float Until;
            public float BossRadius;
            public bool Spent;
        }

        sealed class Rope
        {
            public int Owner;
            public float Ax, Az, Bx, Bz;
            public float Until;
            public bool Spent;
        }

        sealed class Mark
        {
            public float At;
            public bool Done;
            public HashSet<int> Hitters;
        }

        sealed class Link
        {
            public int A;
            public int B;
            public float Ax, Az, Bx, Bz;
            public float Until;
            public float BurnAcc;
        }

        sealed class Turret
        {
            public float X;
            public float Z;
            public float Until;
            public float Acc;
        }

        struct Ball
        {
            public bool Active;
            public int Holder;
            public int Passes;
            public float Until;
        }

        struct Hang
        {
            public bool Active;
            public int Owner;
            public float Until;
        }

        sealed class HasteRopeState
        {
            public int A;
            public int B;
            public float Length;
            public float Mult;
            public bool Broken;
        }
    }
}
