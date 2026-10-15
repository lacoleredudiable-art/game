using Dovus.Core.Actors;
using Dovus.Core.Shared;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// Bir vuruş isteği: kim, hangi salıncakla, ne kadar. Hitbox aktif penceresi her yeni salıncakta yeni
    /// <see cref="HitId"/> alır; aynı kimlik hedefe tekrar hasar vermez (çok vuruşlu alan için <see cref="RehitIntervalMs"/>).
    /// </summary>
    public readonly struct HitEvent
    {
        public HitEvent(
            string hitId,
            ActorId attackerId,
            ActorTeam attackerTeam,
            float amount,
            string? zoneId = null,
            bool dodgeable = true,
            bool allowFriendlyFire = false,
            double rehitIntervalMs = 0d)
        {
            HitId = hitId ?? string.Empty;
            AttackerId = attackerId;
            AttackerTeam = attackerTeam;
            Amount = amount;
            ZoneId = zoneId ?? HitDefaults.BodyZoneId;
            Dodgeable = dodgeable;
            AllowFriendlyFire = allowFriendlyFire;
            RehitIntervalMs = rehitIntervalMs;
        }

        /// <summary>Salıncak/alan kimliği. Boşsa tekrar vuruş süzgeci uygulanmaz.</summary>
        public string HitId { get; }
        public ActorId AttackerId { get; }
        public ActorTeam AttackerTeam { get; }
        /// <summary>DamagePipeline çıktısı (zırh dahil), bölge çarpanından önce.</summary>
        public float Amount { get; }
        public string ZoneId { get; }
        /// <summary>false ise dodge i-frame'i engelleyemez.</summary>
        public bool Dodgeable { get; }
        public bool AllowFriendlyFire { get; }
        /// <summary>0 = aynı hedefe salıncak başına bir kez. &gt;0 = bu aralıkta bir daha vurabilir (zehir alanı gibi).</summary>
        public double RehitIntervalMs { get; }
    }
}
