using System;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim SkillMotor: <c>kural-motoru-v4.json</c> (<c>slice_runes</c>, silahlar, scale) +
    /// dilim kataloğu; gizli v6 JSON / base64 yok.
    /// </summary>
    static class SliceGrammarBuilder
    {
        public static JsonValue BuildGrammarRoot(RuleEngineV4Catalog catalog, JsonValue v4Root) =>
            BuildFromRuleEngineV4(catalog, v4Root);

        public static JsonValue BuildGrammarRoot(string kuralMotoruV4Json)
        {
            JsonValue v4Root = MiniJson.Parse(kuralMotoruV4Json);
            return BuildGrammarRoot(RuleEngineV4Catalog.FromJson(kuralMotoruV4Json), v4Root);
        }

        static JsonValue BuildFromRuleEngineV4(RuleEngineV4Catalog catalog, JsonValue v4Root)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (v4Root == null)
                throw new ArgumentNullException(nameof(v4Root));

            IReadOnlyList<int> sliceRunes = catalog.SliceRunes;
            if (sliceRunes.Count == 0)
                throw new InvalidOperationException("kural-motoru-v4.json: slice_runes boş.");

            JsonValue root = SliceGrammarCompatLexicon.ParseStatic();
            root.SetField("runes", SliceGrammarSliceTables.ParseRunes(sliceRunes));
            root.SetField("verb_base", SliceGrammarSliceTables.ParseVerbBase(sliceRunes));
            root.SetField("adjective_mods", SliceGrammarSliceTables.ParseAdjectiveMods(sliceRunes));
            root.SetField("weapons", SliceGrammarSliceTables.ParseWeapons(catalog));
            root.SetField("skills", SliceGrammarSliceTables.ParseSkills(sliceRunes));

            SliceGrammarDefaults.BindScale(root, catalog.Scale);
            root["system"].SetField("version", JsonValue.Of("v4-slice-inmemory"));
            root["combo_system"].SetField(
                "current_max_length",
                JsonValue.Number(SliceGrammarDefaults.ComboMaxLength));
            return root;
        }
    }
}
