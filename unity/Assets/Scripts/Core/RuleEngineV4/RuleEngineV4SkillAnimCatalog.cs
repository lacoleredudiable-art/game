using System;
using System.Collections.Generic;
using Dovus.Core.Data;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>kural-motoru-v4.json skill_anim — fiil+sıfat+silah → klip ve olay maskesi.</summary>
    public sealed class RuleEngineV4SkillAnimCatalog
    {
        readonly List<RuleEntry> _rules = new();

        RuleEngineV4SkillAnimCatalog(string defaultFallbackState)
        {
            DefaultFallbackState = defaultFallbackState ?? string.Empty;
        }

        public string DefaultFallbackState { get; }

        public static RuleEngineV4SkillAnimCatalog Empty =>
            new(RuleEngineV4SkillAnimDefaults.FallbackAnimatorState);

        public static RuleEngineV4SkillAnimCatalog FromJson(JsonValue root)
        {
            JsonValue node = root["skill_anim"];
            if (node.IsNull)
                return Empty;

            string fallback = node["fallback_animator_state"].AsString(RuleEngineV4SkillAnimDefaults.FallbackAnimatorState);
            var catalog = new RuleEngineV4SkillAnimCatalog(fallback);
            JsonValue rules = node["rules"];
            if (rules.Kind != JsonKind.Array)
                return catalog;

            foreach (JsonValue entry in rules.AsArray())
            {
                if (entry.Kind != JsonKind.Object)
                    continue;
                int weapon = entry["weapon_id"].AsInt(0);
                int verb = entry["verb_rune"].AsInt(0);
                int adj = entry["adjective_rune"].AsInt(0);
                string clip = entry["clip"].AsString();
                if (weapon <= 0 || verb <= 0 || string.IsNullOrEmpty(clip))
                    continue;
                string state = entry["fallback_animator_state"].AsString(fallback);
                RuleEngineV4SkillAnimEvent ev = ParseEvents(entry["events"]);
                JsonValue silhouette = entry["entry_silhouette"];
                int entryCell = ParseAtlasCell(silhouette["atlas_cell"]);
                float entryLife = silhouette["life_sec"].AsFloat();
                catalog._rules.Add(new RuleEntry(weapon, verb, adj, clip, state, ev, entryCell, entryLife));
            }

            return catalog;
        }

        public RuleEngineV4SkillAnimBinding Resolve(int verbRune, int adjectiveRune, int weaponId)
        {
            RuleEntry? exact = null;
            RuleEntry? verbWildcard = null;
            for (int i = 0; i < _rules.Count; i++)
            {
                RuleEntry r = _rules[i];
                if (r.WeaponId != weaponId || r.VerbRune != verbRune)
                    continue;
                if (r.AdjectiveRune == adjectiveRune)
                    exact = r;
                else if (r.AdjectiveRune == 0)
                    verbWildcard = r;
            }

            RuleEntry? pick = exact ?? verbWildcard;
            if (!pick.HasValue)
                return RuleEngineV4SkillAnimBinding.Empty;

            RuleEntry chosen = pick.Value;
            return new RuleEngineV4SkillAnimBinding(
                chosen.Clip,
                chosen.FallbackState,
                chosen.Events,
                chosen.EntrySilhouetteCell,
                chosen.EntrySilhouetteLifeSec);
        }

        /// <summary>"A".."H" → 0..7; başka her şey → yok.</summary>
        static int ParseAtlasCell(JsonValue node)
        {
            string key = node.AsString();
            if (string.IsNullOrEmpty(key) || key.Length != 1)
                return RuleEngineV4SkillAnimDefaults.NoEntrySilhouetteCell;
            int cell = char.ToUpperInvariant(key[0]) - RuleEngineV4SkillAnimDefaults.FirstAtlasCellLetter;
            return cell >= 0 && cell < RuleEngineV4SkillAnimDefaults.SilhouetteAtlasCellCount
                ? cell
                : RuleEngineV4SkillAnimDefaults.NoEntrySilhouetteCell;
        }

        static RuleEngineV4SkillAnimEvent ParseEvents(JsonValue node)
        {
            if (node.IsNull || node.Kind != JsonKind.Array)
                return RuleEngineV4SkillAnimEvent.None;
            RuleEngineV4SkillAnimEvent mask = RuleEngineV4SkillAnimEvent.None;
            foreach (JsonValue item in node.AsArray())
            {
                string key = item.AsString();
                if (string.IsNullOrEmpty(key))
                    continue;
                mask |= ParseEventKey(key);
            }
            return mask;
        }

        static RuleEngineV4SkillAnimEvent ParseEventKey(string key)
        {
            if (string.Equals(key, "Trail_On", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "TrailOn", StringComparison.OrdinalIgnoreCase))
                return RuleEngineV4SkillAnimEvent.TrailOn;
            if (string.Equals(key, "Trail_Off", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "TrailOff", StringComparison.OrdinalIgnoreCase))
                return RuleEngineV4SkillAnimEvent.TrailOff;
            if (string.Equals(key, "Impact", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "impact", StringComparison.OrdinalIgnoreCase))
                return RuleEngineV4SkillAnimEvent.Impact;
            if (string.Equals(key, "Ejder", StringComparison.OrdinalIgnoreCase)
                || string.Equals(key, "ejder", StringComparison.OrdinalIgnoreCase))
                return RuleEngineV4SkillAnimEvent.Ejder;
            return RuleEngineV4SkillAnimEvent.None;
        }

        readonly struct RuleEntry
        {
            public RuleEntry(
                int weaponId,
                int verbRune,
                int adjectiveRune,
                string clip,
                string fallbackState,
                RuleEngineV4SkillAnimEvent events,
                int entrySilhouetteCell,
                float entrySilhouetteLifeSec)
            {
                WeaponId = weaponId;
                VerbRune = verbRune;
                AdjectiveRune = adjectiveRune;
                Clip = clip;
                FallbackState = fallbackState;
                Events = events;
                EntrySilhouetteCell = entrySilhouetteCell;
                EntrySilhouetteLifeSec = entrySilhouetteLifeSec;
            }

            public int WeaponId { get; }
            public int VerbRune { get; }
            public int AdjectiveRune { get; }
            public string Clip { get; }
            public string FallbackState { get; }
            public RuleEngineV4SkillAnimEvent Events { get; }
            public int EntrySilhouetteCell { get; }
            public float EntrySilhouetteLifeSec { get; }
        }
    }
}
