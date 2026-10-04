namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionCosts
    {
        public SkillResolutionCosts(float baseCooldownSec, float baseResourceCost)
        {
            BaseCooldownSec = baseCooldownSec;
            BaseResourceCost = baseResourceCost;
        }

        public float BaseCooldownSec { get; }
        public float BaseResourceCost { get; }
    }
}
