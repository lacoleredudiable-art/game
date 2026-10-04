using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Dovus.Core.Shared;
namespace Dovus.Core.Data
{
    public readonly struct TextNumber
    {
        public TextNumber(TextNumberKind kind, double value)
        {
            Kind = kind;
            Value = value;
        }

        public TextNumberKind Kind { get; }
        public double Value { get; }
    }
}
