using System;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim SkillMotor grameri: <c>kural-motoru-v4.json</c> içindeki <c>slice_skill_motor</c> +
    /// <see cref="RuleEngineV4Catalog.Scale"/> bağlama. Gömülü v6 JSON string yok.
    /// </summary>
    static class SliceGrammarBuilder
    {
        public static JsonValue BuildGrammarRoot(RuleEngineV4Catalog catalog, JsonValue v4Root)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            if (v4Root == null)
                throw new ArgumentNullException(nameof(v4Root));

            JsonValue motor = v4Root["slice_skill_motor"];
            if (motor.Kind != JsonKind.Object)
                throw new InvalidOperationException(
                    "kural-motoru-v4.json: slice_skill_motor nesnesi bekleniyor.");

            JsonValue root = MiniJson.Parse(GrammarJsonCache.ToJson(motor));
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
    }
}
