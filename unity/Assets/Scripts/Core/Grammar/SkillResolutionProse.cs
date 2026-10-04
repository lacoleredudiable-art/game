namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionProse
    {
        public SkillResolutionProse(string passiveDescription, string feel, string visual)
        {
            PassiveDescription = passiveDescription ?? string.Empty;
            Feel = feel ?? string.Empty;
            Visual = visual ?? string.Empty;
        }

        public string PassiveDescription { get; }
        public string Feel { get; }
        public string Visual { get; }
    }
}
