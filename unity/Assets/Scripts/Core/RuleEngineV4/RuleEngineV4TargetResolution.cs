namespace Dovus.Core.RuleEngineV4
{
    public sealed class RuleEngineV4TargetResolution
    {
        public RuleEngineV4TargetResolution(
            RuleEngineV4TargetSide side,
            bool requiresLivingTarget,
            string autoProfileKey)
        {
            Side = side;
            RequiresLivingTarget = requiresLivingTarget;
            AutoProfileKey = autoProfileKey;
        }

        public RuleEngineV4TargetSide Side { get; }
        public bool RequiresLivingTarget { get; }
        /// <summary>Sıfat/fiil ölçüsü (Odaklı, İşaretli, …) — yalnız plan meta.</summary>
        public string AutoProfileKey { get; }
    }
}
