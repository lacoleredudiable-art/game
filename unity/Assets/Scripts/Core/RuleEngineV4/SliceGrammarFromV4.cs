using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>Dilim SkillMotor JSON — yalnız <c>kural-motoru-v4.json</c> (slice_runes, verbs, adjectives, weapons, scale, globals).</summary>
    static partial class SliceGrammarFromV4
    {
        public static JsonValue BuildTables(RuleEngineV4Catalog catalog, JsonValue v4Root)
        {
            IReadOnlyList<int> sliceRunes = catalog.SliceRunes;
            var root = SliceGrammarScaffold.EmptyRoot();
            root.SetField("runes", BuildRunes(catalog, sliceRunes));
            root.SetField("verb_base", BuildVerbBase(catalog, sliceRunes));
            root.SetField("adjective_mods", BuildAdjectiveMods(catalog, sliceRunes));
            root.SetField("weapons", BuildWeapons(catalog, sliceRunes));
            root.SetField("skills", BuildSkills(catalog, sliceRunes));
            root.SetField("hitbox_vfx", BuildHitboxVfx(catalog, sliceRunes));
            root.SetField("mobility_cc", BuildMobilityCc(catalog, sliceRunes));
            SliceGrammarScaffold.BindMainClass(root, sliceRunes);
            SliceGrammarDefaults.BindScale(root, catalog.Scale);
            return root;
        }

        static JsonValue BuildRunes(RuleEngineV4Catalog catalog, IReadOnlyList<int> sliceRunes)
        {
            var list = new List<JsonValue>();
            foreach (int id in sliceRunes)
            {
                if (!catalog.TryGetVerb(id, out RuleEngineV4Verb verb)
                    || !catalog.TryGetAdjective(id, out RuleEngineV4Adjective adjective))
                    continue;
                VerbMeta meta = VerbMeta.For(verb.Key);
                AdjMeta adj = AdjMeta.For(adjective.Key);
                list.Add(JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["id"] = JsonValue.Number(id),
                    ["name"] = JsonValue.Of(meta.DisplayName),
                    ["verb_face"] = JsonValue.Of(meta.DisplayName),
                    ["adjective_face"] = JsonValue.Of(adj.DisplayName),
                    ["adjective_prefix"] = JsonValue.Of(adj.Prefix),
                    ["verb_noun"] = JsonValue.Of(meta.Noun),
                    ["family"] = JsonValue.Of(meta.Family),
                    ["category"] = JsonValue.Of(meta.Category),
                    ["target_mode"] = JsonValue.Of(meta.TargetMode),
                    ["base_effect"] = JsonValue.Of(meta.BaseEffect),
                    ["passive_duration_default"] = JsonValue.Number(meta.PassiveDurationSec),
                }));
            }

            return JsonValue.Of(list);
        }

        static JsonValue BuildVerbBase(RuleEngineV4Catalog catalog, IReadOnlyList<int> sliceRunes)
        {
            RuleEngineV4Scale scale = catalog.Scale;
            RuleEngineV4Globals g = catalog.Globals;
            var map = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (int id in sliceRunes)
            {
                if (!catalog.TryGetVerb(id, out RuleEngineV4Verb verb))
                    continue;
                string key = id.ToString(CultureInfo.InvariantCulture);
                VerbMeta meta = VerbMeta.For(verb.Key);
                var row = new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["action"] = JsonValue.Of(meta.Action),
                    ["base_damage"] = JsonValue.Number(meta.Action == "damage" || meta.Action == "cc"
                        ? scale.BaseDamage
                        : 0f),
                    ["base_cost"] = JsonValue.Number(scale.ManaCostPerSkill),
                    ["base_cooldown"] = JsonValue.Number(scale.CooldownSec),
                    ["hitbox"] = JsonValue.Of(meta.Hitbox),
                    ["cast_mobility"] = JsonValue.Of("free_move"),
                };
                if (meta.Action == "heal")
                    row["base_heal"] = JsonValue.Number(scale.BaseHeal);
                if (meta.Action == "damage" || meta.Action == "cc")
                    row["base_poise"] = JsonValue.Number(scale.BasePoise);
                if (meta.Action == "dash")
                    row["dash_distance_m"] = JsonValue.Number(scale.RangeReferenceM);
                if (meta.Action == "cc")
                {
                    row["cc_kind"] = JsonValue.Of("root");
                    row["cc_duration_sec"] = JsonValue.Number(g.KontrolDurationSec);
                }
                if (meta.Action == "cleanse")
                    row["cleanse_count"] = JsonValue.Number(1);
                if (meta.Action == "shield")
                    row["shield_absorb"] = JsonValue.Number(scale.PlayerMaxHp * 0.05f);
                map[key] = JsonValue.Of(row);
            }

            return JsonValue.Of(map);
        }

        static JsonValue BuildAdjectiveMods(RuleEngineV4Catalog catalog, IReadOnlyList<int> sliceRunes)
        {
            RuleEngineV4Globals g = catalog.Globals;
            var map = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (int id in sliceRunes)
            {
                if (!catalog.TryGetAdjective(id, out RuleEngineV4Adjective adjective))
                    continue;
                string key = id.ToString(CultureInfo.InvariantCulture);
                AdjMeta adj = AdjMeta.For(adjective.Key);
                var row = new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["label"] = JsonValue.Of(adj.DisplayName),
                    ["damage_mult"] = JsonValue.Number(1f),
                    ["hitbox_scale_mult"] = JsonValue.Number(1f),
                    ["poise_damage_mult"] = JsonValue.Number(1f),
                };
                switch (adjective.Key)
                {
                    case "yogun":
                        row["damage_mult"] = JsonValue.Number(g.YogunPowerMult);
                        row["hitbox_scale_mult"] = JsonValue.Number(0.55f);
                        row["poise_damage_mult"] = JsonValue.Number(1.2f);
                        break;
                    case "odakli":
                        row["ignore_armor"] = JsonValue.Of(true);
                        row["damage_mult"] = JsonValue.Number(1.15f);
                        row["max_targets"] = JsonValue.Number(1);
                        break;
                    case "sicrayan":
                        row["bounce_targets"] = JsonValue.Number(g.SicrayanBounceCount);
                        row["bounce_damage_mult"] = JsonValue.Number(g.SicrayanBounceMult);
                        row["hitbox_scale_mult"] = JsonValue.Number(1.1f);
                        break;
                    case "sabit":
                        row["poise_damage_mult"] = JsonValue.Number(1.5f);
                        row["lifetime_add"] = JsonValue.Number(2.0f);
                        row["cast_mobility"] = JsonValue.Of("rooted");
                        break;
                    case "gudumlu":
                        row["apply_root_sec"] = JsonValue.Number(g.KontrolDurationSec);
                        row["apply_slow"] = JsonValue.Number(0.3f);
                        break;
                    case "isaretli":
                        row["ignore_armor"] = JsonValue.Of(true);
                        row["damage_mult"] = JsonValue.Number(1.15f);
                        row["max_targets"] = JsonValue.Number(1);
                        break;
                }

                map[key] = JsonValue.Of(row);
            }

            return JsonValue.Of(map);
        }

        static JsonValue BuildWeapons(RuleEngineV4Catalog catalog, IReadOnlyList<int> sliceRunes)
        {
            var list = new List<JsonValue>();
            foreach (int weaponId in RuleEngineV4Slice.Weapons)
            {
                if (!catalog.TryGetWeapon(weaponId, out RuleEngineV4Weapon weapon))
                    continue;
                list.Add(WeaponRow(weapon, catalog.Scale, sliceRunes));
            }

            return JsonValue.Of(list);
        }

        static JsonValue WeaponRow(RuleEngineV4Weapon weapon, RuleEngineV4Scale scale, IReadOnlyList<int> sliceRunes)
        {
            var compat = new List<JsonValue>();
            foreach (int r in sliceRunes)
                compat.Add(JsonValue.Number(r));

            float reach = scale.RangeReferenceM * weapon.RangeMult;
            bool missile = weapon.Delivery == WeaponDeliveryClass.Missile;
            string name = weapon.Key switch
            {
                "yay" => "Yay",
                "kilic" => "Kılıç",
                "cekiç" => "Çekiç",
                _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(weapon.Key),
            };
            string type = missile ? "ranged" : "medium";
            string basicKind = missile ? "volley" : "slash";
            float arc = weapon.Key == "kilic" ? 144f : 0f;
            int hits = missile ? 1 : 3;
            float interval = missile ? 0.5f : 0.28f;

            var passive = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            var swapBonus = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            if (weapon.Key == "yay")
            {
                passive["id"] = JsonValue.Of("kosu_atisi");
                passive["move_window_sec"] = JsonValue.Number(1);
                passive["crit_chance"] = JsonValue.Number(0.25);
                passive["base_crit"] = JsonValue.Number(0.05);
                swapBonus["id"] = JsonValue.Of("zirh_yok");
                swapBonus["window_sec"] = JsonValue.Number(2);
                swapBonus["ignore_armor"] = JsonValue.Of(true);
            }
            else if (weapon.Key == "kilic")
            {
                passive["id"] = JsonValue.Of("genis_yay");
                passive["arc_deg"] = JsonValue.Number(144);
                swapBonus["id"] = JsonValue.Of("genis");
                swapBonus["window_sec"] = JsonValue.Number(2);
                swapBonus["arc_deg"] = JsonValue.Number(180);
            }

            return JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["id"] = JsonValue.Number(weapon.Id),
                ["name"] = JsonValue.Of(name),
                ["type"] = JsonValue.Of(type),
                ["school"] = JsonValue.Of("fiziksel"),
                ["damage_mult"] = JsonValue.Number(missile ? 0.8f : 1f),
                ["cast_time_mult"] = JsonValue.Number(missile ? 0.7f : 0.9f),
                ["poise_mult"] = JsonValue.Number(missile ? 0.6f : 1.1f),
                ["range_mult"] = JsonValue.Number(weapon.RangeMult),
                ["mobility_mod"] = JsonValue.Number(missile ? 1 : 0),
                ["base_armor"] = JsonValue.Number(0),
                ["reach_m"] = JsonValue.Number(reach),
                ["arc_deg"] = JsonValue.Number(arc),
                ["hit_shape"] = JsonValue.Of(missile ? "arrow" : "arc"),
                ["born"] = JsonValue.Of(missile ? "saplanan" : "onunde_yay"),
                ["effect_travels"] = JsonValue.Of(true),
                ["cooldown_mult"] = JsonValue.Number(1),
                ["identity_passive"] = JsonValue.Of(weapon.Key == "yay" ? "Koşu atışı kritik %25" : "Geniş yay 144 derece"),
                ["compatible_verbs"] = JsonValue.Of(compat),
                ["animations_key"] = JsonValue.Of(weapon.Key),
                ["basic"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["kind"] = JsonValue.Of(basicKind),
                    ["reach_m"] = JsonValue.Number(reach),
                    ["shape"] = JsonValue.Of(missile ? "arrow" : "arc"),
                    ["interval_sec"] = JsonValue.Number(interval),
                    ["hits"] = JsonValue.Number(hits),
                    ["radius_m"] = JsonValue.Number(missile ? 0.12f : 0.5f),
                }),
                ["passive"] = JsonValue.Of(passive),
                ["swap_bonus"] = JsonValue.Of(swapBonus),
            });
        }

        static JsonValue BuildSkills(RuleEngineV4Catalog catalog, IReadOnlyList<int> sliceRunes)
        {
            JsonValue verbBase = BuildVerbBase(catalog, sliceRunes);
            JsonValue adjMods = BuildAdjectiveMods(catalog, sliceRunes);
            var byVerb = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (int verbId in sliceRunes)
            {
                if (!catalog.TryGetVerb(verbId, out RuleEngineV4Verb verb))
                    continue;
                string verbKey = verbId.ToString(CultureInfo.InvariantCulture);
                VerbMeta vMeta = VerbMeta.For(verb.Key);
                JsonValue verbRow = verbBase[verbKey];
                var skills = new List<JsonValue>();
                foreach (int adjId in sliceRunes)
                {
                    if (!catalog.TryGetAdjective(adjId, out RuleEngineV4Adjective adjective))
                        continue;
                    string adjKey = adjId.ToString(CultureInfo.InvariantCulture);
                    AdjMeta aMeta = AdjMeta.For(adjective.Key);
                    JsonValue engine = MergeEngine(verbRow, adjMods[adjKey]);
                    string id = verbKey + "-" + adjKey;
                    float passiveSec = 5f;
                    if (catalog.TryGetVerb(adjId, out RuleEngineV4Verb adjVerbRune))
                        passiveSec = VerbMeta.For(adjVerbRune.Key).PassiveDurationSec;
                    string effect = SkillEffectText(verb.Key, adjective.Key, catalog.Globals.KontrolDurationSec);
                    skills.Add(JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["id"] = JsonValue.Of(id),
                        ["name"] = JsonValue.Of(aMeta.Prefix + " " + vMeta.Noun),
                        ["adjective"] = JsonValue.Of(aMeta.DisplayName),
                        ["effect"] = JsonValue.Of(effect),
                        ["passive"] = JsonValue.Of((int)passiveSec + " sn pasif: " + aMeta.DisplayName),
                        ["engine"] = engine,
                        ["prose_mechanic"] = JsonValue.Of(effect),
                        ["prose_feel"] = JsonValue.Of(aMeta.DisplayName),
                        ["prose_visual"] = JsonValue.Of(verb.Key + "-" + adjective.Key),
                    }));
                }

                byVerb[verbKey] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["skills"] = JsonValue.Of(skills),
                });
            }

            return JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["by_verb"] = JsonValue.Of(byVerb),
            });
        }

        static JsonValue MergeEngine(JsonValue verbRow, JsonValue adjRow)
        {
            var merged = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, JsonValue> kv in verbRow.AsObject())
                merged[kv.Key] = kv.Value;
            foreach (KeyValuePair<string, JsonValue> kv in adjRow.AsObject())
                merged[kv.Key] = kv.Value;
            return JsonValue.Of(merged);
        }

        static JsonValue BuildHitboxVfx(RuleEngineV4Catalog catalog, IReadOnlyList<int> sliceRunes)
        {
            var fiil = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            var sifat = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            var weaponSize = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (int id in sliceRunes)
            {
                if (!catalog.TryGetVerb(id, out RuleEngineV4Verb verb))
                    continue;
                string key = id.ToString(CultureInfo.InvariantCulture);
                VerbMeta meta = VerbMeta.For(verb.Key);
                var hit = new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["shape"] = JsonValue.Of(meta.HitboxShape),
                    ["base_size"] = JsonValue.Of(meta.HitboxSizeLabel),
                    ["duration"] = JsonValue.Of("anlık"),
                    ["multi"] = JsonValue.Of(false),
                };
                if (meta.HitboxShape == "line")
                    hit["cross_section"] = JsonValue.Of("width");
                fiil[key] = JsonValue.Of(hit);
            }

            foreach (int id in sliceRunes)
            {
                if (!catalog.TryGetAdjective(id, out RuleEngineV4Adjective adjective))
                    continue;
                string key = id.ToString(CultureInfo.InvariantCulture);
                JsonValue mods = BuildAdjectiveMods(catalog, sliceRunes)[key];
                sifat[key] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["size_mult"] = mods["hitbox_scale_mult"],
                    ["damage_mult"] = mods["damage_mult"],
                });
            }

            foreach (int weaponId in RuleEngineV4Slice.Weapons)
            {
                if (catalog.TryGetWeapon(weaponId, out RuleEngineV4Weapon w))
                    weaponSize[weaponId.ToString(CultureInfo.InvariantCulture)] = JsonValue.Number(w.RangeMult);
            }

            return JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["fiil_hitbox"] = JsonValue.Of(fiil),
                ["sifat_override"] = JsonValue.Of(sifat),
                ["weapon_size_mult"] = JsonValue.Of(weaponSize),
                ["element_color"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)),
            });
        }

        static JsonValue BuildMobilityCc(RuleEngineV4Catalog catalog, IReadOnlyList<int> sliceRunes)
        {
            var fiilMob = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            var sifatMob = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (int id in sliceRunes)
            {
                string key = id.ToString(CultureInfo.InvariantCulture);
                fiilMob[key] = JsonValue.Of("free_move");
                sifatMob[key] = JsonValue.Of("degistirmez");
            }

            var ccPriority = new List<JsonValue>
            {
                CcRow(1, "stun", 2),
                CcRow(2, "root", 3),
                CcRow(3, "silence", 3),
            };

            return JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
            {
                ["fiil_base_mobility"] = JsonValue.Of(fiilMob),
                ["sifat_mobility_override"] = JsonValue.Of(sifatMob),
                ["weapon_mobility_mod"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)),
                ["cc_priority"] = JsonValue.Of(ccPriority),
                ["cc_priority_table"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["stun_root"] = JsonValue.Of("stun (root gizli)"),
                    ["stun_slow"] = JsonValue.Of("stun"),
                    ["root_slow"] = JsonValue.Of("root (slow gizli)"),
                    ["slow_blind"] = JsonValue.Of("slow + blind"),
                }),
                ["cc_duration_mult"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["base"] = JsonValue.Number(1.0),
                    ["Yoğun"] = JsonValue.Number(0.7),
                    ["Bağlama"] = JsonValue.Number(1.5),
                    ["Sabitleme"] = JsonValue.Number(2.0),
                }),
                ["poise"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["hafif"] = JsonValue.Number(10),
                    ["orta"] = JsonValue.Number(25),
                    ["agir"] = JsonValue.Number(50),
                    ["kesme_esigi"] = JsonValue.Of("gelen_hasar > poise ise iptal"),
                }),
                ["cc_stacking"] = JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                {
                    ["same_cc"] = JsonValue.Of("süre_uzar"),
                    ["different_cc"] = JsonValue.Of("ikisi_de_uygulanır"),
                }),
                ["i_frame"] = JsonValue.Of(new List<JsonValue>
                {
                    JsonValue.Of(new Dictionary<string, JsonValue>(StringComparer.Ordinal)
                    {
                        ["source"] = JsonValue.Of("dodge"),
                        ["duration_ms"] = JsonValue.Number(260),
                        ["condition"] = JsonValue.Of("her_zaman"),
                    }),
                }),
            });
        }

    }
}
