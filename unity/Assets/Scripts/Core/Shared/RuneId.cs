using System;

namespace Dovus.Core.Shared
{
    public readonly struct RuneId : IEquatable<RuneId>
    {
        public RuneId(string? value) => Value = value ?? string.Empty;

        public string Value { get; }
        public bool IsEmpty => Value.Length == 0;

        public bool Equals(RuneId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is RuneId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(RuneId left, RuneId right) => left.Equals(right);
        public static bool operator !=(RuneId left, RuneId right) => !left.Equals(right);

        public static explicit operator RuneId(string value) => new RuneId(value);
        public static implicit operator string(RuneId id) => id.Value;
    }
}
