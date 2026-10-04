using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionIdentity
    {
        public SkillResolutionIdentity(
            ElementId element,
            string elementName,
            string displayName,
            SkillId id,
            string skillJob,
            RuneId verb,
            string verbName,
            RuneId adjective,
            string adjectiveName,
            string flavorElement,
            string elementOrigin)
        {
            Element = element;
            ElementName = elementName ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Id = id;
            SkillJob = skillJob ?? string.Empty;
            Verb = verb;
            VerbName = verbName ?? string.Empty;
            Adjective = adjective;
            AdjectiveName = adjectiveName ?? string.Empty;
            FlavorElement = flavorElement ?? string.Empty;
            ElementOrigin = elementOrigin ?? string.Empty;
        }

        public ElementId Element { get; }
        public string ElementName { get; }
        public string DisplayName { get; }
        public SkillId Id { get; }
        public string SkillJob { get; }
        public RuneId Verb { get; }
        public string VerbName { get; }
        public RuneId Adjective { get; }
        public string AdjectiveName { get; }
        public string FlavorElement { get; }
        public string ElementOrigin { get; }
    }
}
