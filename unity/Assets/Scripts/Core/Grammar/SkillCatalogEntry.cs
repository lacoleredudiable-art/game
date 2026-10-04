using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    /// <summary>element-sistemi.json <c>skills.by_verb</c> satırı — çözümleme öncesi katalog kaydı.</summary>
    public readonly struct SkillCatalogEntry
    {
        public SkillCatalogEntry(
            string id, string name, string effect, string passive, JsonValue engine,
            string proseMechanic, string proseFeel, string proseVisual)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            Effect = effect ?? string.Empty;
            Passive = passive ?? string.Empty;
            Engine = engine;
            ProseMechanic = proseMechanic ?? string.Empty;
            ProseFeel = proseFeel ?? string.Empty;
            ProseVisual = proseVisual ?? string.Empty;
        }

        public string Id { get; }
        public string Name { get; }
        public string Effect { get; }
        public string Passive { get; }
        public JsonValue Engine { get; }
        public string ProseMechanic { get; }
        public string ProseFeel { get; }
        public string ProseVisual { get; }
    }
}
