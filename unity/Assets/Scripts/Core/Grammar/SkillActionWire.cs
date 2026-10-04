using System;

namespace Dovus.Core.Grammar
{
    public readonly struct SkillActionWire : IEquatable<SkillActionWire>
    {
        public enum Kind
        {
            Unknown = 0,
            Buff,
            Cc,
            Cleanse,
            Damage,
            Dash,
            Debuff,
            DrainHeal,
            Heal,
            Reflect,
            Shield,
            Summon,
            Tempo,
        }

        public Kind Value { get; }
        public string Raw { get; }

        SkillActionWire(Kind value, string raw)
        {
            Value = value;
            Raw = raw ?? string.Empty;
        }

        public static SkillActionWire Parse(string? wire)
        {
            string r = wire ?? string.Empty;
            Kind k = r switch
            {
                "buff" => Kind.Buff,
                "cc" => Kind.Cc,
                "cleanse" => Kind.Cleanse,
                "damage" => Kind.Damage,
                "dash" => Kind.Dash,
                "debuff" => Kind.Debuff,
                "drain_heal" => Kind.DrainHeal,
                "heal" => Kind.Heal,
                "reflect" => Kind.Reflect,
                "shield" => Kind.Shield,
                "summon" => Kind.Summon,
                "tempo" => Kind.Tempo,
                _ => Kind.Unknown,
            };
            return new SkillActionWire(k, r);
        }

        public override string ToString() =>
            Value != Kind.Unknown ? Wire(Value) : Raw;

        public static implicit operator string(SkillActionWire w) => w.ToString();

        public bool Equals(SkillActionWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is SkillActionWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(SkillActionWire left, SkillActionWire right) => left.Equals(right);
        public static bool operator !=(SkillActionWire left, SkillActionWire right) => !left.Equals(right);

        static string Wire(Kind k) => k switch
        {
            Kind.Buff => "buff",
            Kind.Cc => "cc",
            Kind.Cleanse => "cleanse",
            Kind.Damage => "damage",
            Kind.Dash => "dash",
            Kind.Debuff => "debuff",
            Kind.DrainHeal => "drain_heal",
            Kind.Heal => "heal",
            Kind.Reflect => "reflect",
            Kind.Shield => "shield",
            Kind.Summon => "summon",
            Kind.Tempo => "tempo",
            _ => string.Empty,
        };
    }
}
