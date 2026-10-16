using System;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim cümle grameri: <see cref="SliceGrammarBuilder"/> + <c>kural-motoru-v4.json</c> scale.
    /// </summary>
    public static class SliceGrammarDefaults
    {
        public const int ComboMaxLength = 2;

        /// <summary>SkillMotor / EquipmentCatalog için dilim gramer JSON'u üretir.</summary>
        public static string BuildGrammarJson(string kuralMotoruV4Json)
        {
            if (string.IsNullOrWhiteSpace(kuralMotoruV4Json))
                throw new ArgumentException("kural-motoru-v4 JSON boş.", nameof(kuralMotoruV4Json));
            JsonValue root = BuildGrammarRoot(kuralMotoruV4Json);
            return GrammarJsonCache.ToJson(root);
        }

        public static JsonValue BuildGrammarRoot(RuleEngineV4Catalog catalog, JsonValue v4Root) =>
            SliceGrammarBuilder.BuildGrammarRoot(catalog, v4Root);

        public static JsonValue BuildGrammarRoot(string kuralMotoruV4Json) =>
            SliceGrammarBuilder.BuildGrammarRoot(kuralMotoruV4Json);

        internal static void BindScale(JsonValue root, RuleEngineV4Scale scale)
        {
            JsonValue global = root["global_rules"];
            JsonValue player = global["player_stats"];
            player.SetField("max_hp", JsonValue.Number(scale.PlayerMaxHp));
            player.SetField("max_mana", JsonValue.Number(scale.ManaPool));

            JsonValue boss = global["boss_stats_default"];
            boss.SetField("max_hp", JsonValue.Number(scale.BossMaxHp));
            boss.SetField("poise_bar", JsonValue.Number(scale.BossPoiseBar));

            JsonValue resource = global["resource_system"];
            resource.SetField("max_mana", JsonValue.Number(scale.ManaPool));
            resource.SetField("regen_per_sec", JsonValue.Number(scale.ManaRegenPerSec));

            JsonValue cooldown = global["cooldown_rules"];
            cooldown.SetField("global_cooldown_sec", JsonValue.Number(scale.CooldownSec));
        }
    }
}
