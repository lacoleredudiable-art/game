using System;
using System.Collections.Generic;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>SkillMotor uyumluluk iskeleti — sayılar <c>kural-motoru-v4.json</c> scale/globals ile doldurulur.</summary>
    static class SliceGrammarScaffold
    {
        public static JsonValue EmptyRoot() =>
            JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["system"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["version"] = JsonValue.Of("v4-slice-inmemory"),
                }),
                ["combo_system"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["current_max_length"] = JsonValue.Number(SliceGrammarDefaults.ComboMaxLength),
                }),
                ["uyumsuz_cizim"] = BuildUyumsuzCizim(),
                ["crit_system"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["base_crit_chance"] = JsonValue.Number(0.05),
                    ["crit_multiplier"] = JsonValue.Number(2.0),
                    ["max_crit_chance"] = JsonValue.Number(0.75),
                }),
                ["weapon_skill_interaction"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["swap"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["enabled"] = JsonValue.Of(true),
                        ["when"] = JsonValue.Of("savaş_içi"),
                        ["weapons_carried"] = JsonValue.Number(2),
                        ["recovery_cancel"] = JsonValue.Of(true),
                        ["cooldown_sec"] = JsonValue.Number(1.2),
                        ["animation_sec"] = JsonValue.Number(0.25),
                        ["cancels_combo"] = JsonValue.Of(true),
                        ["dodge_cancels_swap"] = JsonValue.Of(true),
                        ["passive_changes"] = JsonValue.Of(true),
                    }),
                }),
                ["state_machine"] = BuildStateMachine(),
                ["elements"] = JsonValue.Of(new List<JsonValue>
                {
                    JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["id"] = JsonValue.Number(1),
                        ["name"] = JsonValue.Of("Ateş"),
                        ["color"] = JsonValue.Of("#ff4d1a"),
                        ["vfx"] = JsonValue.Of("alev_köz"),
                        ["name_prefix"] = JsonValue.Of("Ateşli"),
                        ["status"] = JsonValue.Of("burn"),
                        ["status_effect"] = JsonValue.Of("3 hasar/sn"),
                        ["status_duration"] = JsonValue.Number(4),
                    }),
                }),
                ["ana_classes_80"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["groups"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["slice"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                        {
                            ["classes"] = JsonValue.Of(new List<JsonValue>()),
                        }),
                    }),
                }),
                ["global_rules"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["ally_skill_range_m"] = JsonValue.Number(RuleEngineV4CatalogDefaults.RangeReferenceM),
                    ["composition_rules"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)),
                    ["resource_system"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["type"] = JsonValue.Of("mana"),
                        ["regen_delay_after_cast_sec"] = JsonValue.Number(1.5),
                        ["all_values_tunable"] = JsonValue.Of(true),
                    }),
                    ["cooldown_rules"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["max_concurrent_casts"] = JsonValue.Number(1),
                        ["all_values_tunable"] = JsonValue.Of(true),
                    }),
                    ["player_stats"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["move_speed_mps"] = JsonValue.Number(5.0),
                        ["dodge_distance_m"] = JsonValue.Number(3.8),
                        ["dodge_cooldown_sec"] = JsonValue.Number(0.42),
                        ["i_frame_on_dodge_ms"] = JsonValue.Number(260),
                    }),
                    ["boss_stats_default"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["armor"] = JsonValue.Number(100),
                        ["armor_hard"] = JsonValue.Number(150),
                        ["magic_resist"] = JsonValue.Number(0.1),
                        ["stagger_duration_sec"] = JsonValue.Number(7),
                    }),
                }),
            });

        static JsonValue BuildUyumsuzCizim() =>
            JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["effects"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["uyumlu"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["damage_mult"] = JsonValue.Number(1.0),
                        ["cast_time_mult"] = JsonValue.Number(1.0),
                        ["passive"] = JsonValue.Of("çalışır"),
                        ["ui_color"] = JsonValue.Of("yeşil"),
                    }),
                    ["uyumsuz"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["damage_mult"] = JsonValue.Number(0.8),
                        ["cast_time_mult"] = JsonValue.Number(1.2),
                        ["passive"] = JsonValue.Of("çalışmaz"),
                        ["ui_color"] = JsonValue.Of("sarı"),
                    }),
                }),
                ["ui_etiketi"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["uyumlu"] = JsonValue.Of("Uyumlu — [Silah] pasifi aktif"),
                    ["uyumsuz"] = JsonValue.Of("Uyumsuz — %80 etki"),
                }),
            });

        static JsonValue BuildStateMachine() =>
            JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["player_states"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["idle"] = Cap(true, true, true, true, false, false, ""),
                    ["drawing"] = Cap("partial", false, true, false, false, false, "Dodge çizimi iptal eder"),
                    ["casting"] = Cap(false, "based_on_cast_mobility", false, false, false, false, ""),
                    ["recovering"] = Cap(true, "limited", true, true, false, false, "Swap recovery keser"),
                    ["dodging"] = Cap(false, "dodge_direction", false, false, true, false, ""),
                    ["stunned"] = Cap(false, false, false, false, false, false, ""),
                    ["rooted"] = Cap(true, false, true, true, false, false, ""),
                    ["channeling"] = Cap(false, false, true, false, false, true, ""),
                    ["dead"] = Cap(false, false, false, false, false, false, ""),
                }),
                ["boss_states"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["idle"] = BossState(new[] { "approach", "telegraph" }),
                    ["approach"] = BossState(new[] { "telegraph" }),
                    ["telegraph"] = BossState(new[] { "active", "interrupted" }),
                    ["active"] = BossState(new[] { "recovery" }),
                    ["recovery"] = BossState(new[] { "idle", "telegraph" }),
                    ["staggered"] = BossState(new[] { "idle" }, 7f),
                }),
            });

        static JsonValue Cap(
            object draw, object move, object dodge, object swap, bool iFrames, bool interruptible, string note)
        {
            var d = new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["can_draw"] = ToCap(draw),
                ["can_move"] = ToCap(move),
                ["can_dodge"] = ToCap(dodge),
                ["can_swap"] = ToCap(swap),
                ["i_frames"] = iFrames ? JsonValue.Of(true) : JsonValue.Of(false),
                ["interruptible"] = interruptible ? JsonValue.Of(true) : JsonValue.Of(false),
            };
            if (!string.IsNullOrEmpty(note))
                d["note"] = JsonValue.Of(note);
            return JsonValue.Of(d);
        }

        static JsonValue ToCap(object value) =>
            value switch
            {
                bool b => b ? JsonValue.Of(true) : JsonValue.Of(false),
                string s => JsonValue.Of(s),
                _ => JsonValue.Of(false),
            };

        static JsonValue BossState(string[] exits, float durationSec = 0f)
        {
            var list = new List<JsonValue>();
            foreach (string e in exits)
                list.Add(JsonValue.Of(e));
            var d = new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["exits_to"] = JsonValue.Of(list),
            };
            if (durationSec > 0f)
                d["duration_sec"] = JsonValue.Number(durationSec);
            return JsonValue.Of(d);
        }

        public static void BindMainClass(JsonValue root, IReadOnlyList<int> sliceRunes)
        {
            var runeIds = new List<JsonValue>();
            for (int i = 0; i < sliceRunes.Count; i++)
                runeIds.Add(JsonValue.Number(sliceRunes[i]));
            JsonValue sliceClass = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["id"] = JsonValue.Number(1),
                ["name"] = JsonValue.Of("Dilim"),
                ["runes"] = JsonValue.Of(runeIds),
                ["category"] = JsonValue.Of("slice"),
                ["his"] = JsonValue.Of("Kılıç + Yay dilim"),
            });
            root["ana_classes_80"]["groups"]["slice"].SetField(
                "classes",
                JsonValue.Of(new List<JsonValue> { sliceClass }));
        }
    }
}
