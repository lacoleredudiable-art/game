using System;

namespace Dovus.Core.Shared
{
    public readonly struct ElementId : IEquatable<ElementId>
    {
        public ElementId(string? value) => Value = value ?? string.Empty;

        public string Value { get; }
        public bool IsEmpty => Value.Length == 0;

        public bool Equals(ElementId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is ElementId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(ElementId left, ElementId right) => left.Equals(right);
        public static bool operator !=(ElementId left, ElementId right) => !left.Equals(right);

        public static explicit operator ElementId(string value) => new ElementId(value);
        public static implicit operator string(ElementId id) => id.Value;
    }
}
