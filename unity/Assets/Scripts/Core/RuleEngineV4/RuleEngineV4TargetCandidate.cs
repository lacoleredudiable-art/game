namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Sahne adayı (Unity yok). DistanceM Game katmanında collider bounds ile ölçülür.</summary>
    public readonly struct RuleEngineV4TargetCandidate
    {
        public RuleEngineV4TargetCandidate(
            int id,
            bool isHostileToCaster,
            float distanceM,
            bool available,
            bool hasTeamMark,
            float hpRatio = 1f,
            float purifyNeedScore = 0f)
        {
            Id = id;
            IsHostileToCaster = isHostileToCaster;
            DistanceM = distanceM;
            Available = available;
            HasTeamMark = hasTeamMark;
            HpRatio = hpRatio;
            PurifyNeedScore = purifyNeedScore;
        }

        public int Id { get; }
        public bool IsHostileToCaster { get; }
        public float DistanceM { get; }
        public bool Available { get; }
        public bool HasTeamMark { get; }
        /// <summary>0–1; düşük = şifa / düşman zararında öncelik.</summary>
        public float HpRatio { get; }
        /// <summary>Arındırma ihtiyacı; yüksek = öncelik.</summary>
        public float PurifyNeedScore { get; }
    }
}
