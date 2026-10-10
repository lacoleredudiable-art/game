namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Hafif ağıra gidemez; boss itilmez (denge hasarı ayrı).</summary>
    public static class RuleEngineV4WeightRules
    {
        public static bool CanDisplace(RuleEngineV4WeightTier source, RuleEngineV4WeightTier target) =>
            (int)source >= (int)target && target != RuleEngineV4WeightTier.Boss;

        public static bool IsImmovable(RuleEngineV4WeightTier target) =>
            target == RuleEngineV4WeightTier.Boss;
    }
}
