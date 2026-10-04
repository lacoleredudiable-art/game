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
    public readonly struct MechanicHitbox
    {
        public MechanicHitbox(string shape, double sizeA, double sizeB, bool timed)
        {
            Shape = shape ?? string.Empty;
            SizeA = sizeA;
            SizeB = sizeB;
            Timed = timed;
        }

        public string Shape { get; }
        public double SizeA { get; }
        public double SizeB { get; }
        public bool Timed { get; }
    }
}
