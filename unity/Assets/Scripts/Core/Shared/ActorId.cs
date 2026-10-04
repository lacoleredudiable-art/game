using System;

namespace Dovus.Core.Shared
{
    public readonly struct ActorId : IEquatable<ActorId>
    {
        public ActorId(string? value) => Value = value ?? string.Empty;

        public string Value { get; }
        public bool IsEmpty => Value.Length == 0;

        public bool Equals(ActorId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is ActorId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ActorId left, ActorId right) => left.Equals(right);
        public static bool operator !=(ActorId left, ActorId right) => !left.Equals(right);

        public static explicit operator ActorId(string value) => new ActorId(value);
        public static implicit operator string(ActorId id) => id.Value;
    }
}
