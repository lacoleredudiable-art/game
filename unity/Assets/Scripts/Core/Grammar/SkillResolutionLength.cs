namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionLength
    {
        public SkillResolutionLength(
            int runeCount,
            LengthRoleWire role,
            float castMult,
            LengthMobilityWire mobility,
            float resourceCostMult)
        {
            RuneCount = runeCount;
            Role = role;
            CastMult = castMult;
            Mobility = mobility;
            ResourceCostMult = resourceCostMult > 0f ? resourceCostMult : 1f;
        }

        public int RuneCount { get; }
        public LengthRoleWire Role { get; }
        public float CastMult { get; }
        public LengthMobilityWire Mobility { get; }
        public float ResourceCostMult { get; }
    }
}
