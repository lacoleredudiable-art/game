using System;
using Dovus.Core.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Shared;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// Mevcut <see cref="BossVitals"/>'ı hasar sözleşmesine bağlar; BossVitals'a dokunulmaz.
    /// Çok parçalı boss için <see cref="Zones"/>'a bölge eklenir.
    /// </summary>
    public sealed class BossVitalsDamageable : IDamageable
    {
        readonly BossVitals _vitals;

        public BossVitalsDamageable(ActorId id, BossVitals vitals, HurtZoneTable? zones = null)
        {
            Id = id;
            _vitals = vitals;
            Zones = zones ?? new HurtZoneTable();
        }

        public ActorId Id { get; }
        public ActorTeam Team => ActorTeam.Hostile;
        public HurtZoneTable Zones { get; }
        public Func<double, bool, bool>? InvulnerableCheck { get; set; }

        public float Hp => _vitals.Hp;
        public float MaxHp => _vitals.MaxHp;
        public bool IsDead => _vitals.IsDown;

        public bool IsInvulnerable(double nowMs, bool dodgeable) =>
            InvulnerableCheck != null && InvulnerableCheck(nowMs, dodgeable);

        public bool TryGetZone(string? zoneId, out HurtZone zone) => Zones.TryGet(zoneId, out zone);

        public float ReceiveDamage(float amount, double nowMs)
        {
            float before = _vitals.Hp;
            _vitals.ApplyDamage(amount);
            return before - _vitals.Hp;
        }
    }
}
