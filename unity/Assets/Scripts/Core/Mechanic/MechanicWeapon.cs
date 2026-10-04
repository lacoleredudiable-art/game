using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Dovus.Core.Data;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Mechanic
{
    public sealed class MechanicWeapon
    {
        public MechanicWeapon(int id, string name, string type, double damageMult, double rangeMult, int[] compatibleVerbs, string path)
        {
            Id = id;
            Name = name ?? string.Empty;
            Type = type ?? string.Empty;
            DamageMult = damageMult;
            RangeMult = rangeMult;
            CompatibleVerbs = compatibleVerbs ?? Array.Empty<int>();
            Path = path ?? string.Empty;
        }

        public int Id { get; }
        public string Name { get; }
        public string Type { get; }
        public double DamageMult { get; }
        public double RangeMult { get; }
        public int[] CompatibleVerbs { get; }
        /// <summary>mechanic_grammar.weapon_delivery.path — silahın teslim yolu.</summary>
        public string Path { get; }
    }
}
