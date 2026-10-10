using System;

namespace Dovus.Core.RuleEngineV4
{
    public sealed class RuleEngineV4Weapon
    {
        public int Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public float RangeMult { get; init; }
        public WeaponDeliveryClass Delivery { get; init; }
        public float PrefireSec { get; init; }
        public float RecoverySec { get; init; }
        public float TotalSec { get; init; }
        public bool PrefireMoves { get; init; }
        public float TravelSpeedMps { get; init; }
        public bool TravelSkillAllowed { get; init; }
        public int HitParts { get; init; }

        public float RangeM(float rangeReferenceM) => RangeMult * rangeReferenceM;

        public float DamageScale(float ritmReferenceTotalSec) =>
            TotalSec / Math.Max(0.0001f, ritmReferenceTotalSec);
    }
}
