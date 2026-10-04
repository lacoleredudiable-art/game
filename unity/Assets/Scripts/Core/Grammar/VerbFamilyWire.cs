using System;

namespace Dovus.Core.Grammar
{
    public readonly struct VerbFamilyWire : IEquatable<VerbFamilyWire>
    {
        public enum Kind
        {
            Unknown = 0,
            Control,
            Disrupt,
            Guard,
            Mend,
            Motion,
            Purge,
            Special,
            Strike,
        }

        public Kind Value { get; }
        public string Raw { get; }

        VerbFamilyWire(Kind value, string raw)
        {
            Value = value;
            Raw = raw ?? string.Empty;
        }

        public static VerbFamilyWire Parse(string? wire)
        {
            string r = wire ?? string.Empty;
            Kind k = r switch
            {
                "control" => Kind.Control,
                "disrupt" => Kind.Disrupt,
                "guard" => Kind.Guard,
                "mend" => Kind.Mend,
                "motion" => Kind.Motion,
                "purge" => Kind.Purge,
                "special" => Kind.Special,
                "strike" => Kind.Strike,
                _ => Kind.Unknown,
            };
            return new VerbFamilyWire(k, r);
        }

        public override string ToString() =>
            Value != Kind.Unknown ? Wire(Value) : Raw;

        public static implicit operator string(VerbFamilyWire w) => w.ToString();

        public bool Equals(VerbFamilyWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is VerbFamilyWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(VerbFamilyWire left, VerbFamilyWire right) => left.Equals(right);
        public static bool operator !=(VerbFamilyWire left, VerbFamilyWire right) => !left.Equals(right);

        static string Wire(Kind k) => k switch
        {
            Kind.Control => "control",
            Kind.Disrupt => "disrupt",
            Kind.Guard => "guard",
            Kind.Mend => "mend",
            Kind.Motion => "motion",
            Kind.Purge => "purge",
            Kind.Special => "special",
            Kind.Strike => "strike",
            _ => string.Empty,
        };
    }
}
