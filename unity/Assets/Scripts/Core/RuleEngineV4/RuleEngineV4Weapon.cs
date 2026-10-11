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
        /// <summary>
        /// Hareket (3) yalnız bu sıfatla ip atıp uçar; diğer sıfatlarda kısa geri sıçrama.
        /// 0 = kural yok (her Hareket kombosu silahın kendi teslimi + taşıma).
        /// </summary>
        public int MotionRopeAdjectiveRune { get; init; }
        public float BackstepM { get; init; }
        public float BackstepSpeedMps { get; init; }

        /// <summary>Hareket fiilinde bu sıfat ipe değil geri sıçramaya düşer mi.</summary>
        public bool MotionIsBackstep(int adjectiveRune) =>
            MotionRopeAdjectiveRune > 0
            && adjectiveRune != MotionRopeAdjectiveRune
            && BackstepM > 0;

        public float RangeM(float rangeReferenceM) => RangeMult * rangeReferenceM;

        public float DamageScale(float ritmReferenceTotalSec) =>
            TotalSec / Math.Max(0.0001f, ritmReferenceTotalSec);
    }
}
