using System;

namespace Dovus.Core.Shared
{
    public readonly struct WeaponId : IEquatable<WeaponId>
    {
        public WeaponId(string? value) => Value = value ?? string.Empty;

        public string Value { get; }
        public bool IsEmpty => Value.Length == 0;

        public bool Equals(WeaponId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is WeaponId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(WeaponId left, WeaponId right) => left.Equals(right);
        public static bool operator !=(WeaponId left, WeaponId right) => !left.Equals(right);

        public static explicit operator WeaponId(string value) => new WeaponId(value);
        public static implicit operator string(WeaponId id) => id.Value;
    }
}
