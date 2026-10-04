using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct V61SkillNode
    {
        public V61SkillNode(
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
