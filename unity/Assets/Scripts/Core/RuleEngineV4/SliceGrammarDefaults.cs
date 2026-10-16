using System;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim cümle grameri: prose/skills gövdesi gömülü; oynanış sayıları kural-motoru-v4.json scale/globals ile bağlanır.
    /// Tek dış JSON: <c>kural-motoru-v4.json</c>.
    /// </summary>
    public static partial class SliceGrammarDefaults
    {
        public const int ComboMaxLength = 2;

        static string PayloadJson =>
            PayloadJsonPart01()
            + PayloadJsonPart02()
            + PayloadJsonPart03()
            + PayloadJsonPart04()
            + PayloadJsonPart05();

        private static partial string PayloadJsonPart01();
        private static partial string PayloadJsonPart02();
        private static partial string PayloadJsonPart03();
        private static partial string PayloadJsonPart04();
        private static partial string PayloadJsonPart05();

        /// <summary>SkillMotor / EquipmentCatalog için dilim gramer JSON'u üretir.</summary>
        public static string BuildGrammarJson(string kuralMotoruV4Json)
        {
            if (string.IsNullOrWhiteSpace(kuralMotoruV4Json))
                throw new ArgumentException("kural-motoru-v4 JSON boş.", nameof(kuralMotoruV4Json));
            RuleEngineV4Catalog catalog = RuleEngineV4Catalog.FromJson(kuralMotoruV4Json);
            JsonValue root = BuildGrammarRoot(catalog);
            return GrammarJsonCache.ToJson(root);
        }

        public static JsonValue BuildGrammarRoot(RuleEngineV4Catalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException(nameof(catalog));
            JsonValue root = MiniJson.Parse(PayloadJson);
            BindScale(root, catalog.Scale);
            return root;
        }

        public static JsonValue BuildGrammarRoot(string kuralMotoruV4Json) =>
            BuildGrammarRoot(RuleEngineV4Catalog.FromJson(kuralMotoruV4Json));

        static void BindScale(JsonValue root, RuleEngineV4Scale scale)
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

            JsonValue combo = root["combo_system"];
            combo.SetField("current_max_length", JsonValue.Number(ComboMaxLength));

            JsonValue system = root["system"];
            system.SetField("version", JsonValue.Of("v4-slice-inmemory"));
        }
    }
}
