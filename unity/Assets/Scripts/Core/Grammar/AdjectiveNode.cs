using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct AdjectiveNode
    {
        public AdjectiveNode(
            string id, string name, string category, string silhouetteAxis,
            float damageMult, float hitboxScaleMult, float poiseDamageMult,
            JsonValue engineModifiers, JsonValue raw)
        {
            Id = id; Name = name; Category = category; SilhouetteAxis = silhouetteAxis;
            DamageMult = damageMult; HitboxScaleMult = hitboxScaleMult; PoiseDamageMult = poiseDamageMult;
            EngineModifiers = engineModifiers; Raw = raw;
        }

        public string Id { get; }
        public string Name { get; }
        public string Category { get; }
        public string SilhouetteAxis { get; }
        public float DamageMult { get; }
        public float HitboxScaleMult { get; }
        public float PoiseDamageMult { get; }
        /// <summary>
        /// engine_modifiers'ın TAMAMI (20+ alan olabilir: trajectory_override, apply_slow,
        /// cooldown_mult, max_targets, ...). DamageMult/HitboxScaleMult/PoiseDamageMult zaten
        /// tipli; burada geri kalanına EngineModifiers["apply_slow"].AsFloat() gibi erişilir.
        /// </summary>
        public JsonValue EngineModifiers { get; }
        public JsonValue Raw { get; }
        public SkillEngineModifiers Engine => new SkillEngineModifiers(EngineModifiers);
    }
}
