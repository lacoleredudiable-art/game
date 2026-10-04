using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct ElementNode
    {
        public ElementNode(
            string id, string name, string type, string verbId, string adjectiveId,
            string skillId, string skillName, string skillJob,
            string identity, JsonValue specialMechanics, JsonValue zoneEffect, JsonValue raw)
        {
            Id = id; Name = name; Type = type; VerbId = verbId; AdjectiveId = adjectiveId;
            SkillId = skillId; SkillName = skillName; SkillJob = skillJob;
            Identity = identity; SpecialMechanics = specialMechanics; ZoneEffect = zoneEffect;
            Raw = raw;
        }

        public string Id { get; }
        public string Name { get; }
        public string Type { get; }
        public string VerbId { get; }
        public string AdjectiveId { get; }
        public string SkillId { get; }
        public string SkillName { get; }
        public string SkillJob { get; }
        /// <summary>Bileşiğin kimlik/rol metni (v5.2 element_families_detailed.bileşikler.identity).</summary>
        public string Identity { get; }
        /// <summary>Bileşiğe özel mekanik nesnesi (varsa); motor henüz uygulamıyor, veri taşıyor.</summary>
        public JsonValue SpecialMechanics { get; }
        public JsonValue ZoneEffect { get; }
        /// <summary>Ham JSON nesnesi — henüz tipli alanı olmayan gelecek alanlar için.</summary>
        public JsonValue Raw { get; }
    }
}
