using System;
using System.Collections.Generic;

namespace Dovus.App.Sweep
{
    public readonly struct SweepComboEntry
    {
        public SweepComboEntry(int verb, int adj, string weapon, float startDistM)
        {
            Verb = verb;
            Adj = adj;
            Weapon = weapon ?? string.Empty;
            StartDistM = startDistM;
        }

        public int Verb { get; }
        public int Adj { get; }
        public string Weapon { get; }
        public float StartDistM { get; }
    }
}
