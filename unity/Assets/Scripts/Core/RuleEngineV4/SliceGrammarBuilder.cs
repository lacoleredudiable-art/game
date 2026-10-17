using System;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim SkillMotor: yalnız <c>kural-motoru-v4.json</c> (<c>slice_runes</c>, fiiller, sıfatlar, silahlar, scale).
    /// </summary>
    static class SliceGrammarBuilder
    {
        public static JsonValue BuildGrammarRoot(RuleEngineV4Catalog catalog, JsonValue v4Root) =>
            SliceGrammarFromV4.BuildTables(catalog, v4Root);

        public static JsonValue BuildGrammarRoot(string kuralMotoruV4Json)
        {
            JsonValue v4Root = MiniJson.Parse(kuralMotoruV4Json);
            return BuildGrammarRoot(RuleEngineV4Catalog.FromJson(kuralMotoruV4Json), v4Root);
        }
    }
}
