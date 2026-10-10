namespace Dovus.Core.RuleEngineV4
{
    public readonly struct RuleEngineV4SkillAnimBinding
    {
        public RuleEngineV4SkillAnimBinding(
            string clip,
            string fallbackAnimatorState,
            RuleEngineV4SkillAnimEvent events)
        {
            Clip = clip ?? string.Empty;
            FallbackAnimatorState = fallbackAnimatorState ?? string.Empty;
            Events = events;
        }

        public string Clip { get; }
        public string FallbackAnimatorState { get; }
        public RuleEngineV4SkillAnimEvent Events { get; }

        public bool IsEmpty => string.IsNullOrEmpty(Clip) && string.IsNullOrEmpty(FallbackAnimatorState);

        public static RuleEngineV4SkillAnimBinding Empty => new(string.Empty, string.Empty, RuleEngineV4SkillAnimEvent.None);
    }
}
