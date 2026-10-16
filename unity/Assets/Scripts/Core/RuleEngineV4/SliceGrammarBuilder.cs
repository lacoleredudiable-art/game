using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim SkillMotor: v4 <c>slice_runes</c> ile fiil×sıfat ızgarası, sayılar
    /// <see cref="RuleEngineV4Catalog.Scale"/>; sabit prose <see cref="SliceGrammarMotorScaffold"/>.
    /// </summary>
    static class SliceGrammarBuilder
    {
        public static JsonValue BuildGrammarRoot(RuleEngineV4Catalog catalog, JsonValue v4Root)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (v4Root == null)
                throw new ArgumentNullException(nameof(v4Root));

            JsonValue root = SliceGrammarMotorScaffold.ParseRoot();
            IReadOnlyList<int> sliceRunes = catalog.SliceRunes;
            if (sliceRunes.Count == 0)
                throw new InvalidOperationException("kural-motoru-v4.json: slice_runes boş.");

            TrimMap(root, "verb_base", sliceRunes);
            TrimMap(root, "adjective_mods", sliceRunes);
            TrimRunes(root, sliceRunes);
            RebuildSkillGrid(root, sliceRunes);

            SliceGrammarDefaults.BindScale(root, catalog.Scale);
            root["system"].SetField("version", JsonValue.Of("v4-slice-inmemory"));
            root["combo_system"].SetField(
                "current_max_length",
                JsonValue.Number(SliceGrammarDefaults.ComboMaxLength));
            return root;
        }

        public static JsonValue BuildGrammarRoot(string kuralMotoruV4Json)
        {
            JsonValue v4Root = MiniJson.Parse(kuralMotoruV4Json);
            return BuildGrammarRoot(RuleEngineV4Catalog.FromJson(kuralMotoruV4Json), v4Root);
        }

        static void TrimMap(JsonValue root, string field, IReadOnlyList<int> sliceRunes)
        {
            root.SetField(field, JsonValue.Of(KeepKeys(root[field], sliceRunes)));
        }

        static Dictionary<string, JsonValue> KeepKeys(JsonValue obj, IReadOnlyList<int> sliceRunes)
        {
            var kept = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> kv in obj.AsObject())
            {
                if (!int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    continue;
                if (SliceRunesContains(sliceRunes, id))
                    kept[kv.Key] = kv.Value;
            }

            return kept;
        }

        static void TrimRunes(JsonValue root, IReadOnlyList<int> sliceRunes)
        {
            var kept = new List<JsonValue>();
            foreach (JsonValue rune in root["runes"].AsArray())
            {
                int id = rune["id"].AsInt();
                if (SliceRunesContains(sliceRunes, id))
                    kept.Add(rune);
            }

            root.SetField("runes", JsonValue.Of(kept));
        }

        static void RebuildSkillGrid(JsonValue root, IReadOnlyList<int> sliceRunes)
        {
            IReadOnlyDictionary<string, JsonValue> scaffoldByVerb = root["skills"]["by_verb"].AsObject();
            var byVerb = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            JsonValue verbBase = root["verb_base"];
            JsonValue adjectiveMods = root["adjective_mods"];

            foreach (int verbId in sliceRunes)
            {
                string verbKey = verbId.ToString(CultureInfo.InvariantCulture);
                scaffoldByVerb.TryGetValue(verbKey, out JsonValue scaffoldGroup);
                var skills = new List<JsonValue>();
                foreach (int adjId in sliceRunes)
                {
                    string skillId = verbKey + "-" + adjId.ToString(CultureInfo.InvariantCulture);
                    if (scaffoldGroup.Kind == JsonKind.Object
                        && TryFindSkill(scaffoldGroup, skillId, out JsonValue existing))
                    {
                        skills.Add(existing);
                        continue;
                    }

                    skills.Add(SynthesizeSkill(skillId, verbId, adjId, verbBase, adjectiveMods, root));
                }

                byVerb[verbKey] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["skills"] = JsonValue.Of(skills),
                });
            }

            root["skills"].SetField("by_verb", JsonValue.Of(byVerb));
        }

        static bool TryFindSkill(JsonValue scaffoldGroup, string skillId, out JsonValue skill)
        {
            foreach (JsonValue sk in scaffoldGroup["skills"].AsArray())
            {
                if (sk["id"].AsString() == skillId)
                {
                    skill = sk;
                    return true;
                }
            }

            skill = JsonValue.Null;
            return false;
        }

        static JsonValue SynthesizeSkill(
            string skillId,
            int verbId,
            int adjId,
            JsonValue verbBase,
            JsonValue adjectiveMods,
            JsonValue root)
        {
            string verbKey = verbId.ToString(CultureInfo.InvariantCulture);
            string adjKey = adjId.ToString(CultureInfo.InvariantCulture);
            JsonValue engine = MergeEngine(verbBase[verbKey], adjectiveMods[adjKey], verbId, adjId);
            RuneFaces faces = FindRuneFaces(root, verbId, adjId);
            return JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["id"] = JsonValue.Of(skillId),
                ["name"] = JsonValue.Of(faces.SkillName),
                ["adjective"] = JsonValue.Of(faces.AdjectiveFace),
                ["effect"] = JsonValue.Of(faces.BaseEffect),
                ["passive"] = JsonValue.Of(faces.PassiveText),
                ["engine"] = engine,
                ["prose_mechanic"] = JsonValue.Of(faces.BaseEffect),
                ["prose_feel"] = JsonValue.Of(string.Empty),
                ["prose_visual"] = JsonValue.Of(string.Empty),
            });
        }

        static JsonValue MergeEngine(JsonValue verbRow, JsonValue adjRow, int verbId, int adjId)
        {
            var engine = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> kv in verbRow.AsObject())
                engine[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, JsonValue> kv in adjRow.AsObject())
                engine[kv.Key] = kv.Value;
            engine["label"] = adjRow["label"];

            if (verbId == 2 && adjId == 2)
            {
                engine["action"] = JsonValue.Of("drain_heal");
                engine["lifesteal"] = JsonValue.Number(0.35);
            }

            return JsonValue.Of(engine);
        }

        static RuneFaces FindRuneFaces(JsonValue root, int verbId, int adjId)
        {
            JsonValue verbRune = FindRune(root, verbId);
            JsonValue adjRune = FindRune(root, adjId);
            string prefix = adjRune["adjective_prefix"].AsString();
            string noun = verbRune["verb_noun"].AsString();
            int passiveSec = (int)verbRune["passive_duration_default"].AsDouble(5);
            string adjFace = adjRune["adjective_face"].AsString();
            return new RuneFaces(
                prefix + " " + noun,
                adjFace,
                verbRune["base_effect"].AsString(),
                passiveSec + " sn pasif: " + adjFace);
        }

        static JsonValue FindRune(JsonValue root, int id)
        {
            foreach (JsonValue rune in root["runes"].AsArray())
            {
                if (rune["id"].AsInt() == id)
                    return rune;
            }

            throw new InvalidOperationException("dilim grameri: rün " + id + " yok.");
        }

        static bool SliceRunesContains(IReadOnlyList<int> sliceRunes, int id)
        {
            for (int i = 0; i < sliceRunes.Count; i++)
            {
                if (sliceRunes[i] == id)
                    return true;
            }

            return false;
        }

        readonly struct RuneFaces
        {
            public RuneFaces(string skillName, string adjectiveFace, string baseEffect, string passiveText)
            {
                SkillName = skillName;
                AdjectiveFace = adjectiveFace;
                BaseEffect = baseEffect;
                PassiveText = passiveText;
            }

            public string SkillName { get; }
            public string AdjectiveFace { get; }
            public string BaseEffect { get; }
            public string PassiveText { get; }
        }
    }
}
