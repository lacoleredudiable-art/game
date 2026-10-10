namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Odaklı (sıfat 2): ihtiyaç tabanlı tek hedef — kural-kitabi-v4 §3.2.</summary>
    public static class RuleEngineV4OdakliRules
    {
        public static RuleEngineV4TargetSide PickSide(int verbId, RuleEngineV4TargetSide resolutionSide) =>
            verbId switch
            {
                3 => RuleEngineV4TargetSide.Hostile,
                4 or 10 => RuleEngineV4TargetSide.Self,
                _ => resolutionSide,
            };

        /// <summary>true ise <paramref name="candidate"/> şu ana kadarki seçimden daha iyi.</summary>
        public static bool Beats(int verbId, in RuleEngineV4TargetCandidate candidate, in RuleEngineV4TargetCandidate current)
        {
            if (verbId == 9)
            {
                if (candidate.PurifyNeedScore > current.PurifyNeedScore)
                    return true;
                if (candidate.PurifyNeedScore < current.PurifyNeedScore)
                    return false;
                if (candidate.HpRatio < current.HpRatio)
                    return true;
                if (candidate.HpRatio > current.HpRatio)
                    return false;
                return candidate.DistanceM < current.DistanceM;
            }

            if (verbId == 2 || verbId == 8 || verbId == 12)
            {
                if (candidate.HpRatio < current.HpRatio)
                    return true;
                if (candidate.HpRatio > current.HpRatio)
                    return false;
                return candidate.DistanceM < current.DistanceM;
            }

            // Zarar, Kuvvet, Kontrol, Çağırma, Hareket (düşman): en düşük canlı düşman.
            if (candidate.HpRatio < current.HpRatio)
                return true;
            if (candidate.HpRatio > current.HpRatio)
                return false;
            return candidate.DistanceM < current.DistanceM;
        }
    }
}
