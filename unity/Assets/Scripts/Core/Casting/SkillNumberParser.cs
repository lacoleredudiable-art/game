using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Core.Shared;

namespace Dovus.Core.Casting
{
    public static class SkillNumberParser
    {
        public static SkillNumberCatalog Parse(JsonValue root) =>
            ParseSlice(root, VerbExecutionData.FromJsonRoot(root), MobilityCcData.FromJsonRoot(root));

        public static SkillNumberCatalog ParseSlice(
            JsonValue root, VerbExecutionData hitboxes, MobilityCcData mobility)
        {
            var catalog = new SkillNumberCatalog();
            JsonValue verbs = root["verb_base"];
            catalog.RootImmunityMs = mobility.RootImmunityMs;
            catalog.AllySkillRangeM = NeedFloat(
                root["global_rules"],
                "ally_skill_range_m",
                "global_rules.ally_skill_range_m",
                SkillNumberFallbacks.AllySkillRangeM);

            JsonValue cooldown = root["global_rules"]["cooldown_rules"];
            catalog.GlobalCooldownSec = NeedFloat(
                cooldown, "global_cooldown_sec", "global_rules.cooldown_rules.global_cooldown_sec",
                SkillNumberFallbacks.GlobalCooldownSec);
            catalog.MaxConcurrentCasts = NeedInt(
                cooldown, "max_concurrent_casts", "global_rules.cooldown_rules.max_concurrent_casts",
                SkillNumberFallbacks.MaxConcurrentCasts);

            JsonValue mana = root["global_rules"]["resource_system"];
            catalog.MaxMana = NeedFloat(
                mana, "max_mana", "global_rules.resource_system.max_mana",
                SkillNumberFallbacks.MaxMana);
            catalog.ManaRegenPerSec = NeedFloat(
                mana, "regen_per_sec", "global_rules.resource_system.regen_per_sec",
                SkillNumberFallbacks.ManaRegenPerSec);
            catalog.ManaRegenDelaySec = NeedFloat(
                mana, "regen_delay_after_cast_sec", "global_rules.resource_system.regen_delay_after_cast_sec",
                SkillNumberFallbacks.ManaRegenDelaySec);

            catalog.VerbDamageReference = NeedFloat(
                verbs["1"], "base_damage", "verb_base.1.base_damage",
                SkillNumberFallbacks.VerbDamageReference);

            for (int id = 1; id <= 12; id++)
            {
                string key = id.ToString(CultureInfo.InvariantCulture);
                JsonValue row = verbs[key];
                string prefix = "verb_base." + key;
                float damage = NeedFloat(row, "base_damage", prefix + ".base_damage", SkillNumberFallbacks.Damage);
                float cost = NeedFloat(row, "base_cost", prefix + ".base_cost", SkillNumberFallbacks.ManaCost);
                float cool = NeedFloat(row, "base_cooldown", prefix + ".base_cooldown", SkillNumberFallbacks.CooldownSec);
                float duration = FirstDuration(row);
                float range = SkillNumberFallbacks.RangeM;
                float radius = SkillNumberFallbacks.RadiusM;
                if (hitboxes.TryGetHitbox(id, out VerbHitboxSpec spec) && !spec.IsEmpty)
                {
                    HitboxSize size = HitboxSizing.Resolve(spec, 1f, 1f);
                    range = row.Has("dash_distance_m")
                        ? row["dash_distance_m"].AsFloat(size.ReachM)
                        : size.ReachM;
                    radius = size.RadiusM;
                }
                else
                {
                    DesignWarnings.Once(
                        prefix + ".hitbox",
                        "element-sistemi.json " + prefix + " hitbox boyutu yok; yedek menzil/yarıçap kullanıldı.");
                }

                catalog.ImportVerb(
                    id,
                    damage,
                    cool,
                    cost,
                    duration,
                    range,
                    radius);
            }

            catalog.ImportCcDurations(ReadCcDurations(mobility));
            return catalog;
        }

        static Dictionary<StatusKind, int> ReadCcDurations(MobilityCcData mobility)
        {
            var map = new Dictionary<StatusKind, int>();
            foreach (KeyValuePair<StatusKind, int> kv in mobility.CcDurationsMs)
                map[kv.Key] = kv.Value;
            return map;
        }

        static float FirstDuration(JsonValue row)
        {
            string[] keys =
            {
                "cc_duration_sec",
                "debuff_duration_sec",
                "buff_duration_sec",
                "tempo_duration_sec",
                "reflect_duration_sec",
                "minion_duration_sec"
            };
            for (int i = 0; i < keys.Length; i++)
            {
                if (row.Has(keys[i]) && row[keys[i]].Kind == JsonKind.Number)
                    return row[keys[i]].AsFloat(0f);
            }

            return 0f;
        }

        static float NeedFloat(JsonValue obj, string field, string warnKey, float fallback)
        {
            if (obj.Has(field) && obj[field].Kind == JsonKind.Number)
                return obj[field].AsFloat(fallback);
            DesignWarnings.Once(
                warnKey,
                "element-sistemi.json '" + warnKey + "' yok; yedek " +
                fallback.ToString(CultureInfo.InvariantCulture) + " kullanıldı.");
            return fallback;
        }

        static int NeedInt(JsonValue obj, string field, string warnKey, int fallback)
        {
            if (obj.Has(field) && obj[field].Kind == JsonKind.Number)
                return obj[field].AsInt(fallback);
            DesignWarnings.Once(
                warnKey,
                "element-sistemi.json '" + warnKey + "' yok; yedek " + fallback + " kullanıldı.");
            return fallback;
        }
    }
}
