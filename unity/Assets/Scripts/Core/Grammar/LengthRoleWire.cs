using System;

namespace Dovus.Core.Grammar
{
    public readonly struct LengthRoleWire : IEquatable<LengthRoleWire>
    {
        public enum Kind
        {
            Unknown = 0,
            VerbPreview,
            TwoRuneSkill,
            Basic,
        }

        public Kind Value { get; }
        public string Raw { get; }

        LengthRoleWire(Kind value, string raw)
        {
            Value = value;
            Raw = raw ?? string.Empty;
        }

        public static LengthRoleWire Parse(string? wire)
        {
            string r = wire ?? string.Empty;
            Kind k = r switch
            {
                "Fiil önizleme" => Kind.VerbPreview,
                "2-rün skill" => Kind.TwoRuneSkill,
                "Temel" => Kind.Basic,
                _ => Kind.Unknown,
            };
            return new LengthRoleWire(k, r);
        }

        public override string ToString() =>
            Value != Kind.Unknown ? Wire(Value) : Raw;

        public static implicit operator string(LengthRoleWire w) => w.ToString();

        public bool Equals(LengthRoleWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is LengthRoleWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(LengthRoleWire left, LengthRoleWire right) => left.Equals(right);
        public static bool operator !=(LengthRoleWire left, LengthRoleWire right) => !left.Equals(right);

        static string Wire(Kind k) => k switch
        {
            Kind.VerbPreview => "Fiil önizleme",
            Kind.TwoRuneSkill => "2-rün skill",
            Kind.Basic => "Temel",
            _ => string.Empty,
        };
    }
}
