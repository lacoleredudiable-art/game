using System;
using Dovus.Core.Actors;
using Dovus.Core.Shared;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// <see cref="IDamageable"/>'ın genel gerçeklemesi: müttefik, minyon, antrenman kuklası.
    /// Dodge i-frame ve god mode <see cref="InvulnerableCheck"/> kancasına bağlanır (kendi kurallarını burada tekrar yazmaz).
    /// </summary>
    public sealed class Combatant : IDamageable
    {
        public Combatant(ActorId id, ActorTeam team, HealthPool health, HurtZoneTable? zones = null)
        {
            Id = id;
            Team = team;
            Health = health;
            Zones = zones ?? new HurtZoneTable();
        }

        public ActorId Id { get; }
        public ActorTeam Team { get; }
        public HealthPool Health { get; }
        public HurtZoneTable Zones { get; }

        /// <summary>(nowMs, dodgeable) → true ise vuruş yutulur.</summary>
        public Func<double, bool, bool>? InvulnerableCheck { get; set; }

        public float Hp => Health.Hp;
        public float MaxHp => Health.MaxHp;
        public bool IsDead => Health.IsDead;

        public bool IsInvulnerable(double nowMs, bool dodgeable) =>
            InvulnerableCheck != null && InvulnerableCheck(nowMs, dodgeable);

        public bool TryGetZone(string? zoneId, out HurtZone zone) => Zones.TryGet(zoneId, out zone);

        public float ReceiveDamage(float amount, double nowMs) => Health.ApplyDamage(amount);
    }
}
