namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// SkillResolution + prezentasyon kataloğundan LivingEffect’e inen dünya planı.
    /// Kombo tablosu yok — fiil hitbox / sıfat trajectory+scale.
    /// </summary>
    public readonly struct LivingEffectPlan
    {
        public static LivingEffectPlan Empty { get; } = default;

        public LivingEffectPlan(
            EffectSilhouette silhouette,
            LivingTravelKind travelKind,
            float speedMps,
            float maxRangeM,
            float bangRadiusM,
            float lifetimeAddSec,
            string trajectoryId,
            string hitboxId,
            bool hasPlan)
        {
            Silhouette = silhouette;
            TravelKind = travelKind;
            SpeedMps = speedMps;
            MaxRangeM = maxRangeM;
            BangRadiusM = bangRadiusM;
            LifetimeAddSec = lifetimeAddSec;
            TrajectoryId = trajectoryId ?? string.Empty;
            HitboxId = hitboxId ?? string.Empty;
            HasPlan = hasPlan;
        }

        public EffectSilhouette Silhouette { get; }
        public LivingTravelKind TravelKind { get; }
        public float SpeedMps { get; }
        public float MaxRangeM { get; }
        /// <summary>Kapanış isabet yarıçapı (hitbox × scale); 0 = tuning varsayılanı.</summary>
        public float BangRadiusM { get; }
        public float LifetimeAddSec { get; }
        public string TrajectoryId { get; }
        public string HitboxId { get; }
        public bool HasPlan { get; }
    }
}
