using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Element;
using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public sealed partial class SkillMotor
    {
        static string PassiveDescriptionFor(SkillCatalogEntry skill, RuneDefinition adjectiveRune)
        {
            string passive = skill.Passive ?? string.Empty;
            if (!passive.Contains(" sn pasif: ", StringComparison.Ordinal))
                return passive;
            return SkillTextNumbers.PassiveText(
                adjectiveRune.AdjectiveFace,
                adjectiveRune.PassiveDurationDefault);
        }

        static void ParseV61(JsonValue root, SkillMotor motor)
        {
            motor._isV61 = true;
            motor._maxComboLength = root["combo_system"]["current_max_length"].AsInt(2);

            JsonValue verbBase = root["verb_base"];
            JsonValue adjectiveMods = root["adjective_mods"];
            foreach (JsonValue obj in root["runes"].AsArray())
            {
                int id = obj["id"].AsInt();
                if (id <= 0)
                    continue;

                string key = id.ToString(CultureInfo.InvariantCulture);
                string name = obj["name"].AsString();
                string verbFace = obj["verb_face"].AsString(name);
                string adjectiveFace = obj["adjective_face"].AsString();
                string family = obj["family"].AsString();
                string targetMode = obj["target_mode"].AsString();
                var rune = new RuneDefinition(
                    id,
                    name,
                    verbFace,
                    adjectiveFace,
                    obj["adjective_prefix"].AsString(),
                    obj["verb_noun"].AsString(),
                    family,
                    obj["category"].AsString(),
                    targetMode,
                    obj["base_effect"].AsString(),
                    obj["passive_duration_default"].AsFloat(0f));
                motor._runes[id] = rune;
                motor._runeDefinitions.Add(rune);

                // Eski tüketicilerin sözlük API'si korunur; içerik v6 rünlerinden üretilir.
                motor._elements[key] = new ElementNode(
                    key, name, "core", key, key, default, string.Empty,
                    obj["base_effect"].AsString(), string.Empty,
                    JsonValue.Null, JsonValue.Null, obj);

                JsonValue stats = verbBase[key];
                string[] mechanics = ReadV61Mechanics(stats, string.Empty);
                motor._verbs[key] = new VerbNode(
                    key,
                    verbFace,
                    family,
                    stats["action"].AsString(),
                    stats["base_damage"].AsFloat(0f),
                    stats["base_poise"].AsFloat(0f),
                    stats["hitbox"].AsString(),
                    stats["cast_mobility"].AsString("free_move"),
                    mechanics,
                    string.Empty,
                    targetMode,
                    stats["base_cooldown"].AsFloat(0f),
                    stats["base_cost"].AsFloat(0f),
                    EmptyBehaviors,
                    stats,
                    JsonValue.Null,
                    stats,
                    stats["base_damage"].AsFloat(0f) > 0f,
                    string.Empty,
                    string.Empty);

                JsonValue mods = adjectiveMods[key];
                motor._adjectives[key] = new AdjectiveNode(
                    key,
                    adjectiveFace,
                    obj["category"].AsString(),
                    mods["label"].AsString(adjectiveFace),
                    mods["damage_mult"].AsFloat(1f),
                    mods["hitbox_scale_mult"].AsFloat(1f),
                    mods["poise_damage_mult"].AsFloat(1f),
                    mods,
                    obj);
            }

            foreach (var verbKv in root["skills"]["by_verb"].AsObject())
            {
                foreach (JsonValue obj in verbKv.Value["skills"].AsArray())
                {
                    string id = obj["id"].AsString();
                    if (string.IsNullOrEmpty(id))
                        continue;
                    motor._v61Skills[id] = new SkillCatalogEntry(
                        id,
                        obj["name"].AsString(),
                        obj["effect"].AsString(),
                        obj["passive"].AsString(),
                        obj["engine"],
                        obj["prose_mechanic"].AsString(),
                        obj["prose_feel"].AsString(),
                        obj["prose_visual"].AsString());
                }
            }

            foreach (JsonValue obj in root["elements"].AsArray())
            {
                int id = obj["id"].AsInt();
                if (id <= 0)
                    continue;
                motor._elementPaints.Add(new ElementPaintNode(
                    id,
                    obj["name"].AsString(),
                    obj["name_prefix"].AsString(),
                    obj["color"].AsString(),
                    obj["vfx"].AsString(),
                    obj.Has("status") ? obj["status"].AsString() : string.Empty,
                    obj.Has("status_effect") ? obj["status_effect"].AsString() : string.Empty,
                    obj.Has("status_duration") ? obj["status_duration"].AsFloat(0f) : 0f));
            }

            foreach (var group in root["ana_classes_80"]["groups"].AsObject())
            {
                foreach (JsonValue obj in group.Value["classes"].AsArray())
                {
                    IReadOnlyList<JsonValue> values = obj["runes"].AsArray();
                    var ids = new int[values.Count];
                    for (int i = 0; i < ids.Length; i++)
                        ids[i] = values[i].AsInt();
                    motor._mainClasses.Add(new MainClassNode(
                        obj["id"].AsInt(),
                        obj["name"].AsString(),
                        ids,
                        obj["category"].AsString(),
                        obj["his"].AsString()));
                }
            }

            if (motor._mainClasses.Count > 0)
                motor._defaultLoadout = new RuneLoadout(motor._mainClasses[0].RuneIds);
            else
            {
                var ids = new List<int>(RuneLoadout.SlotCount);
                foreach (var kv in motor._runes)
                {
                    if (ids.Count >= RuneLoadout.SlotCount) break;
                    ids.Add(kv.Key);
                }
                motor._defaultLoadout = new RuneLoadout(ids);
            }

            ParseStateMachine(root, motor._playerStates, motor._bossStates);

            if (motor._runes.Count != 12)
                throw new InvalidOperationException("element-sistemi v6: 12 rün beklenir.");
            if (motor._v61Skills.Count != GrammarDefaults.ExpectedV61SkillCount)
                throw new InvalidOperationException("element-sistemi v6: 144 skill beklenir.");
            if (motor._maxComboLength != 2)
                throw new InvalidOperationException("element-sistemi v6: mevcut kombo uzunluğu 2 olmalıdır.");
        }

        static string[] ReadV61Mechanics(JsonValue engine, string effectText)
        {
            var mechanics = new List<string>();
            string action = engine["action"].AsString();
            void Add(string value)
            {
                if (!string.IsNullOrEmpty(value) && !mechanics.Contains(value))
                    mechanics.Add(value);
            }

            if (action == "cc")
                Add(Dovus.Core.Status.CardEffectRules.CcKind(effectText, engine["cc_kind"].AsString()));
            else if (action == "cleanse")
                Add("cleanse");
            else if (action == "shield")
                Add("shield");
            // Zırh düşürme tek mekanizma: engine debuff_armor → ArmorSheet kırılması.
            // Ayrı armor_break durumu gelen hasarı bir de ×1,2 çarpardı.

            return mechanics.ToArray();
        }

        /// <summary>
        /// state_machine.player_states + boss_states. Bool/string karışık capability
        /// alanları (can_draw:"partial", can_move:"limited") metin olarak saklanır.
        /// </summary>
        static void ParseStateMachine(
            JsonValue root, List<PlayerStateNode> playerDst, List<BossStateNode> bossDst)
        {
            JsonValue sm = root["state_machine"];
            if (sm.Kind != JsonKind.Object)
                return;

            foreach (var kv in sm["player_states"].AsObject())
            {
                JsonValue obj = kv.Value;
                playerDst.Add(new PlayerStateNode(
                    id: kv.Key,
                    canDraw: ReadCapability(obj["can_draw"]),
                    canMove: ReadCapability(obj["can_move"]),
                    canDodge: ReadCapability(obj["can_dodge"]),
                    iFrames: obj["i_frames"].AsBool(false),
                    interruptible: obj["interruptible"].AsBool(false),
                    note: obj["note"].AsString(),
                    canSwap: ReadCapability(obj["can_swap"])));
            }

            foreach (var kv in sm["boss_states"].AsObject())
            {
                JsonValue obj = kv.Value;
                JsonValue duration = obj["duration_sec"];
                bossDst.Add(new BossStateNode(
                    id: kv.Key,
                    exitsTo: ReadStringArray(obj["exits_to"]),
                    hasDuration: duration.Kind == JsonKind.Number,
                    durationSec: duration.AsFloat(0f)));
            }
        }

        /// <summary>
        /// can_draw / can_move / can_dodge: JSON'da bool veya string olabilir
        /// ("partial", "based_on_cast_mobility", "limited", "dodge_direction").
        /// Eksikse "false".
        /// </summary>
        static string ReadCapability(JsonValue v)
        {
            if (v.Kind == JsonKind.Bool)
                return v.AsBool() ? "true" : "false";
            if (v.Kind == JsonKind.String)
            {
                string s = v.AsString();
                return string.IsNullOrEmpty(s) ? "false" : s;
            }
            return "false";
        }

        static string[] ReadStringArray(JsonValue arr)
        {
            IReadOnlyList<JsonValue> items = arr.AsArray();
            if (items.Count == 0) return Array.Empty<string>();
            var list = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
                list[i] = items[i].AsString();
            return list;
        }

        static readonly IReadOnlyDictionary<string, string> EmptyBehaviors =
            new Dictionary<string, string>(StringComparer.Ordinal);
    }
}
