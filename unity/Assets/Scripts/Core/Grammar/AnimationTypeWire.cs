using System;

namespace Dovus.Core.Grammar
{
    public readonly struct AnimationTypeWire : IEquatable<AnimationTypeWire>
    {
        public enum Kind
        {
            Unknown = 0,
            Empty,
        }

        public Kind Value { get; }
        readonly string _raw;

        /// <summary>Ham JSON değeri; <c>default</c> örnekte boş string (eski <c>?? string.Empty</c> davranışı).</summary>
        public string Raw => _raw ?? string.Empty;

        AnimationTypeWire(Kind value, string raw)
        {
            Value = value;
            _raw = raw ?? string.Empty;
        }

        public static AnimationTypeWire Parse(string? wire)
        {
            string r = wire ?? string.Empty;
            if (r.Length == 0)
                return new AnimationTypeWire(Kind.Empty, r);
            return new AnimationTypeWire(Kind.Unknown, r);
        }

        public override string ToString() =>
            Value == Kind.Empty ? string.Empty : Raw;

        public static implicit operator string(AnimationTypeWire w) => w.ToString();

        public bool Equals(AnimationTypeWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is AnimationTypeWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(AnimationTypeWire left, AnimationTypeWire right) => left.Equals(right);
        public static bool operator !=(AnimationTypeWire left, AnimationTypeWire right) => !left.Equals(right);
    }
}
