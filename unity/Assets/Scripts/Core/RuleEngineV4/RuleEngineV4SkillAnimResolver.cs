namespace Dovus.Core.RuleEngineV4
{
    /// <summary>CommandPlan kimliği → animasyon köprüsü verisi (saf C#).</summary>
    public static class RuleEngineV4SkillAnimResolver
    {
        public static RuleEngineV4SkillAnimBinding Resolve(in CommandPlan plan, RuleEngineV4SkillAnimCatalog catalog)
        {
            if (plan == null || catalog == null || plan.VerbRune <= 0 || plan.WeaponId <= 0)
                return RuleEngineV4SkillAnimBinding.Empty;
            return catalog.Resolve(plan.VerbRune, plan.AdjectiveRune, plan.WeaponId);
        }
    }
}
