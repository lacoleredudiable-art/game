using System;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;

namespace Dovus.App.Casting
{
    public readonly struct BasicOutcome
    {
        public BasicOutcome(bool healed, bool deniedCadence, bool connected, float dealt, int hits)
        {
            Healed = healed;
            DeniedCadence = deniedCadence;
            Connected = connected;
            Dealt = dealt;
            Hits = hits;
        }

        public bool Healed { get; }
        public bool DeniedCadence { get; }
        public bool Connected { get; }
        public float Dealt { get; }
        public int Hits { get; }
    }
}
