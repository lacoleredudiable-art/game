namespace Dovus.Core.RuleEngineV4
{
    public readonly struct RuleEngineV4TargetPick
    {
        public RuleEngineV4TargetPick(
            bool hasTarget,
            bool useSelf,
            int targetId,
            bool targetInWeaponRange,
            bool manualTargetUsed)
        {
            HasTarget = hasTarget;
            UseSelf = useSelf;
            TargetId = targetId;
            TargetInWeaponRange = targetInWeaponRange;
            ManualTargetUsed = manualTargetUsed;
        }

        public bool HasTarget { get; }
        public bool UseSelf { get; }
        public int TargetId { get; }
        public bool TargetInWeaponRange { get; }
        public bool ManualTargetUsed { get; }
    }
}
