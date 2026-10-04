using System;

namespace Dovus.Core.Shared
{
    public readonly struct SkillId : IEquatable<SkillId>
    {
        readonly string? _value;

        public SkillId(string? value) => _value = value;

        public string Value => _value ?? string.Empty;
        public bool IsEmpty => Value.Length == 0;

        public bool Equals(SkillId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is SkillId other && Equals(other);
        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value;

        public static bool operator ==(SkillId left, SkillId right) => left.Equals(right);
        public static bool operator !=(SkillId left, SkillId right) => !left.Equals(right);

        public static explicit operator SkillId(string value) => new SkillId(value);
        public static implicit operator string(SkillId id) => id.Value;
    }
}
