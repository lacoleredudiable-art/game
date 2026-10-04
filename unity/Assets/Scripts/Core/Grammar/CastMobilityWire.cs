using System;

namespace Dovus.Core.Grammar
{
    public readonly struct CastMobilityWire : IEquatable<CastMobilityWire>
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

        CastMobilityWire(Kind value, string raw)
        {
            Value = value;
            _raw = raw ?? string.Empty;
        }

        public static CastMobilityWire Parse(string? wire)
        {
            string r = wire ?? string.Empty;
            Kind k = r switch
            {
                "free_move" => Kind.FreeMove,
                "slowed_move" => Kind.SlowedMove,
                "rooted" => Kind.Rooted,
                _ => Kind.Unknown,
            };
            return new CastMobilityWire(k, r);
        }

        public override string ToString() =>
            Value != Kind.Unknown ? Wire(Value) : Raw;

        public static implicit operator string(CastMobilityWire w) => w.ToString();

        public bool Equals(CastMobilityWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is CastMobilityWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(CastMobilityWire left, CastMobilityWire right) => left.Equals(right);
        public static bool operator !=(CastMobilityWire left, CastMobilityWire right) => !left.Equals(right);

        static string Wire(Kind k) => k switch
        {
            Kind.FreeMove => "free_move",
            Kind.SlowedMove => "slowed_move",
            Kind.Rooted => "rooted",
            _ => string.Empty,
        };
    }
}
