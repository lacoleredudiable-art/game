using System;

namespace Dovus.Core.Grammar
{
    public readonly struct HitboxWire : IEquatable<HitboxWire>
    {
        public enum Kind
        {
            Unknown = 0,
            EmbeddedAnimation,
            DashLine,
            GroundRing,
            Projectile,
            Self,
            SelfAura,
            SelfOrAlly,
            Target,
        }

        public Kind Value { get; }
        public string Raw { get; }

        HitboxWire(Kind value, string raw)
        {
            Value = value;
            Raw = raw ?? string.Empty;
        }

        public static HitboxWire Parse(string? wire)
        {
            string r = wire ?? string.Empty;
            Kind k = r switch
            {
                "animasyona_gömülü" => Kind.EmbeddedAnimation,
                "dash_line" => Kind.DashLine,
                "ground_ring" => Kind.GroundRing,
                "projectile" => Kind.Projectile,
                "self" => Kind.Self,
                "self_aura" => Kind.SelfAura,
                "self_or_ally" => Kind.SelfOrAlly,
                "target" => Kind.Target,
                _ => Kind.Unknown,
            };
            return new HitboxWire(k, r);
        }

        public override string ToString() =>
            Value != Kind.Unknown ? Wire(Value) : Raw;

        public static implicit operator string(HitboxWire w) => w.ToString();

        public bool Equals(HitboxWire other) =>
            Value == other.Value && string.Equals(Raw, other.Raw, StringComparison.Ordinal);
        public override bool Equals(object? obj) => obj is HitboxWire other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Value, Raw);
        public static bool operator ==(HitboxWire left, HitboxWire right) => left.Equals(right);
        public static bool operator !=(HitboxWire left, HitboxWire right) => !left.Equals(right);

        static string Wire(Kind k) => k switch
        {
            Kind.EmbeddedAnimation => "animasyona_gömülü",
            Kind.DashLine => "dash_line",
            Kind.GroundRing => "ground_ring",
            Kind.Projectile => "projectile",
            Kind.Self => "self",
            Kind.SelfAura => "self_aura",
            Kind.SelfOrAlly => "self_or_ally",
            Kind.Target => "target",
            _ => string.Empty,
        };
    }
}
