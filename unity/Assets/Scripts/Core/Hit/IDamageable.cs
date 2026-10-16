using Dovus.Core.Actors;
using Dovus.Core.Shared;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// Hasar alabilen her şeyin (oyuncu, müttefik, minyon, boss, antrenman kuklası) tek sözleşmesi.
    /// Hasar hesabı DamagePipeline'dadır; burası yalnız "uygula ve duyur" tarafıdır.
    /// </summary>
    public interface IDamageable
    {
        ActorId Id { get; }
        ActorTeam Team { get; }
        float Hp { get; }
        float MaxHp { get; }
        bool IsDead { get; }

        /// <summary>Dodge i-frame, gizli koruma, god mode gibi nedenlerle vuruş yutuluyor mu.</summary>
        bool IsInvulnerable(double nowMs, bool dodgeable);

        bool TryGetZone(string? zoneId, out HurtZone zone);

        /// <summary>Candan düşeni uygular, gerçekte düşen miktarı döner. Ölüm bilgisi <see cref="IsDead"/> ile okunur.</summary>
        float ReceiveDamage(float amount, double nowMs);
    }
}
