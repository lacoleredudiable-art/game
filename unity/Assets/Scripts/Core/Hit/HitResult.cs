using Dovus.Core.Shared;

namespace Dovus.Core.Hit
{
    public readonly struct HitResult
    {
        public HitResult(
            bool applied,
            HitRejectReason reason,
            ActorId targetId,
            string zoneId,
            float requested,
            float dealt,
            float hpBefore,
            float hpAfter,
            bool killed)
        {
            Applied = applied;
            Reason = reason;
            TargetId = targetId;
            ZoneId = zoneId;
            Requested = requested;
            Dealt = dealt;
            HpBefore = hpBefore;
            HpAfter = hpAfter;
            Killed = killed;
        }

        public bool Applied { get; }
        public HitRejectReason Reason { get; }
        public ActorId TargetId { get; }
        public string ZoneId { get; }
        /// <summary>Bölge çarpanından sonra istenen hasar.</summary>
        public float Requested { get; }
        /// <summary>Candan gerçekten düşen (can bittiyse Requested'den az olabilir).</summary>
        public float Dealt { get; }
        public float HpBefore { get; }
        public float HpAfter { get; }
        public bool Killed { get; }

        public static HitResult Rejected(HitRejectReason reason, ActorId targetId, string zoneId, float hp) =>
            new HitResult(false, reason, targetId, zoneId, 0f, 0f, hp, hp, false);
    }
}
