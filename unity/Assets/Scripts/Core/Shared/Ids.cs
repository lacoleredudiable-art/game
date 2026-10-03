using System;

namespace Dovus.Core.Shared
{
    public readonly struct SkillId : IEquatable<SkillId>
    {
        public SkillId(string? value) => Value = value ?? string.Empty;

        public string Value { get; }
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
