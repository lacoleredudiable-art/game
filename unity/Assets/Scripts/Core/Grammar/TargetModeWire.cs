using System;

namespace Dovus.Core.Grammar
{
    public readonly struct TargetModeWire : IEquatable<TargetModeWire>
    {
        public enum Kind
        {
            Unknown = 0,
            EnemyOnly,
            SelfOnly,
            SelfOrAlly,
        }

        public Kind Value { get; }
        readonly string _raw;

        /// <summary>Ham JSON değeri; <c>default</c> örnekte boş string (eski <c>?? string.Empty</c> davranışı).</summary>
        public string Raw => _raw ?? string.Empty;

        TargetModeWire(Kind value, string raw)
        {
            Value = value;
            _raw = raw ?? string.Empty;
        }

        public static TargetModeWire Parse(string? wire)
        {
            string r = wire ?? string.Empty;
            Kind k = r switch
            {
                "enemy_only" => Kind.EnemyOnly,
                "self_only" => Kind.SelfOnly,
                "self_or_ally" => Kind.SelfOrAlly,
                _ => Kind.Unknown,
            };
            return new TargetModeWire(k, r);
        }

        public override string ToString() =>
            Value != Kind.Unknown ? Wire(Value) : Raw;

        public static implicit operator string(TargetModeWire w) => w.ToString();

        public bool Equals(TargetModeWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is TargetModeWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(TargetModeWire left, TargetModeWire right) => left.Equals(right);
        public static bool operator !=(TargetModeWire left, TargetModeWire right) => !left.Equals(right);

        static string Wire(Kind k) => k switch
        {
            Kind.EnemyOnly => "enemy_only",
            Kind.SelfOnly => "self_only",
            Kind.SelfOrAlly => "self_or_ally",
            _ => string.Empty,
        };
    }
}
