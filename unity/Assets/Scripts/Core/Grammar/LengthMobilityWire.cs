using System;

namespace Dovus.Core.Grammar
{
    public readonly struct LengthMobilityWire : IEquatable<LengthMobilityWire>
    {
        public enum Kind
        {
            Unknown = 0,
            FreeMove,
            SlowedMove,
            Rooted,
        }

        public Kind Value { get; }
        readonly string _raw;

        /// <summary>Ham JSON değeri; <c>default</c> örnekte boş string (eski <c>?? string.Empty</c> davranışı).</summary>
        public string Raw => _raw ?? string.Empty;

        LengthMobilityWire(Kind value, string raw)
        {
            Value = value;
            _raw = raw ?? string.Empty;
        }

        public static LengthMobilityWire Parse(string? wire) =>
            new(CastMobilityWire.Parse(wire).Value switch
            {
                CastMobilityWire.Kind.FreeMove => Kind.FreeMove,
                CastMobilityWire.Kind.SlowedMove => Kind.SlowedMove,
                CastMobilityWire.Kind.Rooted => Kind.Rooted,
                _ => Kind.Unknown,
            }, wire ?? string.Empty);

        public override string ToString() =>
            Value != Kind.Unknown ? Wire(Value) : Raw;

        public static implicit operator string(LengthMobilityWire w) => w.ToString();

        public bool Equals(LengthMobilityWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is LengthMobilityWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(LengthMobilityWire left, LengthMobilityWire right) => left.Equals(right);
        public static bool operator !=(LengthMobilityWire left, LengthMobilityWire right) => !left.Equals(right);

        static string Wire(Kind k) => k switch
        {
            Kind.FreeMove => "free_move",
            Kind.SlowedMove => "slowed_move",
            Kind.Rooted => "rooted",
            _ => string.Empty,
        };
    }
}
