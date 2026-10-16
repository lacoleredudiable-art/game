using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    static class SliceGrammarSliceTables
    {
        public static JsonValue ParseRunes(IReadOnlyList<int> sliceRunes) =>
            FilterRunes(SliceGrammarCatalogData.ParseCatalog()["runes"], sliceRunes);

        public static JsonValue ParseVerbBase(IReadOnlyList<int> sliceRunes) =>
            FilterMap(SliceGrammarCatalogData.ParseCatalog()["verb_base"], sliceRunes);

        public static JsonValue ParseAdjectiveMods(IReadOnlyList<int> sliceRunes) =>
            FilterMap(SliceGrammarCatalogData.ParseCatalog()["adjective_mods"], sliceRunes);

        public static JsonValue ParseWeapons(RuleEngineV4Catalog catalog)
        {
            var list = new List<JsonValue>();
            foreach (JsonValue w in SliceGrammarCatalogData.ParseCatalog()["weapons"].AsArray())
            {
                int id = w["id"].AsInt();
                if (!catalog.TryGetWeapon(id, out RuleEngineV4Weapon v4))
                {
                    list.Add(w);
                    continue;
                }

                JsonValue copy = MiniJson.Parse(GrammarJsonCache.ToJson(w));
                copy.SetField("range_mult", JsonValue.Number(v4.RangeMult));
                list.Add(copy);
            }

            return JsonValue.Of(list);
        }

        public static JsonValue ParseSkills(IReadOnlyList<int> sliceRunes)
        {
            JsonValue root = SliceGrammarCatalogData.ParseCatalog()["skills"];
            var byVerb = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> kv in root["by_verb"].AsObject())
            {
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int verbId))
                    continue;
                if (!SliceContains(sliceRunes, verbId))
                    continue;

                var skills = new List<JsonValue>();
                foreach (JsonValue sk in kv.Value["skills"].AsArray())
                {
                    string id = sk["id"].AsString();
                    string[] parts = id.Split('-');
                    if (parts.Length == 2
                        && int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int adjId)
                        && SliceContains(sliceRunes, adjId))
                        skills.Add(sk);
                }

                byVerb[kv.Key] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["skills"] = JsonValue.Of(skills),
                });
            }

            return JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["by_verb"] = JsonValue.Of(byVerb),
            });
        }

        static JsonValue FilterRunes(JsonValue runes, IReadOnlyList<int> sliceRunes)
        {
            var kept = new List<JsonValue>();
            foreach (JsonValue rune in runes.AsArray())
            {
                int id = rune["id"].AsInt();
                if (SliceContains(sliceRunes, id))
                    kept.Add(rune);
            }

            return JsonValue.Of(kept);
        }

        static JsonValue FilterMap(JsonValue map, IReadOnlyList<int> sliceRunes)
        {
            var kept = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> kv in map.AsObject())
            {
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    continue;
                if (SliceContains(sliceRunes, id))
                    kept[kv.Key] = kv.Value;
            }

            return JsonValue.Of(kept);
        }

        static bool SliceContains(IReadOnlyList<int> sliceRunes, int id)
        {
            for (int i = 0; i < sliceRunes.Count; i++)
            {
                if (sliceRunes[i] == id)
                    return true;
            }

            return false;
        }
    }
}
