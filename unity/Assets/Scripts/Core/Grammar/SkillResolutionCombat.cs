namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionCombat
    {
        public SkillResolutionCombat(
            float baseDamage,
            float basePoise,
            float baseHeal,
            bool critEligible,
            string damageType)
        {
            BaseDamage = baseDamage;
            BasePoise = basePoise;
            BaseHeal = baseHeal;
            CritEligible = critEligible;
            DamageType = damageType ?? string.Empty;
        }

        public float BaseDamage { get; }
        public float BasePoise { get; }
        public float BaseHeal { get; }
        public bool CritEligible { get; }
        public string DamageType { get; }
    }
}
