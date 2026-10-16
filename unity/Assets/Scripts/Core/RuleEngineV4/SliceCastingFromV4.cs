using Dovus.Core.Casting;
using Dovus.Core.Shared;
using Dovus.Core.Status;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Dilim cast verisi — <c>kural-motoru-v4.json</c>; SkillMotor JSON'unda <c>hitbox_vfx</c> / <c>mobility_cc</c> yok.</summary>
    public static class SliceCastingFromV4
    {
        public static VerbExecutionData VerbExecution(RuleEngineV4Catalog catalog) =>
            VerbExecutionData.ForSlice(catalog);

        public static MobilityCcData MobilityCc(RuleEngineV4Catalog catalog) =>
            MobilityCcData.ForSlice(catalog);

        public static SkillNumberCatalog SkillNumbers(RuleEngineV4Catalog catalog, JsonValue grammarRoot) =>
            SkillNumberParser.ParseSlice(grammarRoot, VerbExecution(catalog), MobilityCc(catalog));
    }
}
