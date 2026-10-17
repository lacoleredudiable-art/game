using System;
using System.Collections.Generic;
using Dovus.Core.Casting;
using Dovus.Core.Shared;
using Dovus.Core.Status;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Dilim cast verisi — <c>kural-motoru-v4.json</c>; SkillMotor JSON'unda <c>hitbox_vfx</c> / <c>mobility_cc</c> yok.</summary>
    public static class SliceCastingFromV4
    {
        public static VerbExecutionData VerbExecution(RuleEngineV4Catalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            IReadOnlyList<int> runes = catalog.SliceRunes;
            JsonValue mobility = SliceGrammarFromV4.BuildMobilityCc(catalog, runes);
            var root = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["hitbox_vfx"] = SliceGrammarFromV4.BuildHitboxVfx(catalog, runes),
                ["mobility_cc"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["i_frame"] = mobility["i_frame"],
                }),
            });
            return VerbExecutionData.FromJsonRoot(root);
        }

        public static MobilityCcData MobilityCc(RuleEngineV4Catalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            IReadOnlyList<int> runes = catalog.SliceRunes;
            var root = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["mobility_cc"] = SliceGrammarFromV4.BuildMobilityCc(catalog, runes),
                ["adjective_mods"] = SliceGrammarFromV4.BuildAdjectiveMods(catalog, runes),
            });
            return MobilityCcData.FromJsonRoot(root);
        }

        public static SkillNumberCatalog SkillNumbers(RuleEngineV4Catalog catalog, JsonValue grammarRoot) =>
            SkillNumberParser.ParseSlice(grammarRoot, VerbExecution(catalog), MobilityCc(catalog));
    }
}
