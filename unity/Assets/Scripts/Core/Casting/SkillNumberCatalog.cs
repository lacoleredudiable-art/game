using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Status;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

using Dovus.Core.Shared;
namespace Dovus.Core.Casting
{
    /// <summary>
    /// Skill sayıları (hasar, süre, soğuma, mana, menzil, yarıçap) tek JSON'dan.
    /// Alan yoksa bir kez uyarır ve <see cref="SkillNumberFallbacks"/> kullanır.
    /// </summary>
    public sealed class SkillNumberCatalog
    {
        readonly Dictionary<int, VerbNumbers> _verbs = new();

        SkillNumberCatalog()
        {
        }

        public float VerbDamageReference { get; private set; } = SkillNumberFallbacks.VerbDamageReference;
        public float GlobalCooldownSec { get; private set; } = SkillNumberFallbacks.GlobalCooldownSec;
        public int MaxConcurrentCasts { get; private set; } = SkillNumberFallbacks.MaxConcurrentCasts;
        public float MaxMana { get; private set; } = SkillNumberFallbacks.MaxMana;
        public float ManaRegenPerSec { get; private set; } = SkillNumberFallbacks.ManaRegenPerSec;
        public float ManaRegenDelaySec { get; private set; } = SkillNumberFallbacks.ManaRegenDelaySec;
        public double RootImmunityMs { get; private set; } = SkillNumberFallbacks.RootImmunityMs;
        public float AllySkillRangeM { get; private set; } = SkillNumberFallbacks.AllySkillRangeM;

        public static SkillNumberCatalog FromJson(string json) =>
            FromDocument(ElementSystemDocument.Parse(json));

        public static SkillNumberCatalog FromDocument(ElementSystemDocument doc) =>
            FromJsonRoot(doc.Root);

        public static SkillNumberCatalog FromJsonRoot(JsonValue root)
        {
            var catalog = new SkillNumberCatalog();
            JsonValue verbs = root["verb_base"];
            VerbExecutionData hitboxes = VerbExecutionData.FromJsonRoot(root);
            MobilityCcData mobility = MobilityCcData.FromJsonRoot(root);
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

                catalog._verbs[id] = new VerbNumbers(damage, cool, cost, duration, range, radius);
            }

            catalog._ccMs = ReadCcDurations(root["mobility_cc"]["cc_priority"]);
            return catalog;
        }

        Dictionary<StatusKind, int> _ccMs = new();

        public bool TryGetVerb(int verbId, out float damage, out float cooldownSec, out float manaCost,
            out float durationSec, out float rangeM, out float radiusM)
        {
            if (_verbs.TryGetValue(verbId, out VerbNumbers n))
            {
                damage = n.Damage;
                cooldownSec = n.CooldownSec;
                manaCost = n.ManaCost;
                durationSec = n.DurationSec;
                rangeM = n.RangeM;
                radiusM = n.RadiusM;
                return true;
            }

            damage = SkillNumberFallbacks.Damage;
            cooldownSec = SkillNumberFallbacks.CooldownSec;
            manaCost = SkillNumberFallbacks.ManaCost;
            durationSec = 0f;
            rangeM = SkillNumberFallbacks.RangeM;
            radiusM = SkillNumberFallbacks.RadiusM;
            return false;
        }

        public float RadiusM(int verbId) =>
            _verbs.TryGetValue(verbId, out VerbNumbers n) ? n.RadiusM : SkillNumberFallbacks.RadiusM;

        public float RangeM(int verbId) =>
            _verbs.TryGetValue(verbId, out VerbNumbers n) ? n.RangeM : SkillNumberFallbacks.RangeM;

        /// <summary>
        /// Düz vuruş menzili ve kapsül yarıçapı = fiil 1 hitbox'ı (hitbox_vfx). Alan yoksa tuning yedeği kalır
        /// ve katalog zaten bir kez uyarmıştır.
        /// </summary>
        public void ApplyBasicStrikeRange(ManifestationTuning tuning)
        {
            if (tuning == null)
                return;
            tuning.BasicStrikeRangeM = RangeM(1);
            tuning.BasicStrikeRadiusM = RadiusM(1);
        }

        /// <summary>
        /// CC sürelerini JSON cc_priority'den canlı ayara yazar.
        /// Alan yoksa StatusTuning'deki adlı yedek kalır.
        /// </summary>
        public void ApplyCcDurations(StatusTuning tuning)
        {
            if (tuning == null)
                return;
            void Set(StatusKind kind, Action<int> write)
            {
                if (_ccMs.TryGetValue(kind, out int ms) && ms > 0)
                    write(ms);
            }

            Set(StatusKind.Stun, v => tuning.StunMs = v);
            Set(StatusKind.Root, v => tuning.RootMs = v);
            Set(StatusKind.Silence, v => tuning.SilenceMs = v);
            Set(StatusKind.Slow, v => tuning.SlowMs = v);
            Set(StatusKind.Blind, v => tuning.BlindMs = v);
            Set(StatusKind.Disarm, v => tuning.DisarmMs = v);
            Set(StatusKind.Taunt, v => tuning.TauntMs = v);
        }

        static Dictionary<StatusKind, int> ReadCcDurations(JsonValue rows)
        {
            var map = new Dictionary<StatusKind, int>();
            foreach (JsonValue row in rows.AsArray())
            {
                if (!StatusKindUtil.TryParse(row["cc"].AsString(), out StatusKind kind)
                    || kind == StatusKind.None)
                    continue;
                if (row["duration_sec"].Kind != JsonKind.Number)
                {
                    DesignWarnings.Once(
                        "mobility_cc.cc_priority." + kind,
                        "element-sistemi.json CC süresi yok (" + kind + "); StatusTuning yedeği kalır.");
                    continue;
                }

                map[kind] = (int)Math.Round(row["duration_sec"].AsFloat(0f) * CastingDefaults.SecToMs);
            }

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

        readonly struct VerbNumbers
        {
            public VerbNumbers(float damage, float cooldownSec, float manaCost, float durationSec, float rangeM, float radiusM)
            {
                Damage = damage;
                CooldownSec = cooldownSec;
                ManaCost = manaCost;
                DurationSec = durationSec;
                RangeM = rangeM;
                RadiusM = radiusM;
            }

            public float Damage { get; }
            public float CooldownSec { get; }
            public float ManaCost { get; }
            public float DurationSec { get; }
            public float RangeM { get; }
            public float RadiusM { get; }
        }
    }

    /// <summary>
    /// JSON alanı yokken kullanılan tek yer. Yeni his sayısı buraya eklenmez;
    /// bunlar yalnızca eksik alan yedeğidir.
    /// </summary>
    public static class SkillNumberFallbacks
    {
        public const float VerbDamageReference = 40f;
        public const float Damage = 0f;
        public const float CooldownSec = 1f;
        public const float ManaCost = 10f;
        public const float GlobalCooldownSec = 0.3f;
        public const int MaxConcurrentCasts = 1;
        public const float MaxMana = 100f;
        public const float ManaRegenPerSec = 8f;
        public const float ManaRegenDelaySec = 1.5f;
        public const float RangeM = 2.4f;
        public const float RadiusM = 1.15f;
        public const double RootImmunitySec = 0.5;
        public const double RootImmunityMs = RootImmunitySec * 1000.0;
        /// <summary>Tempo süresi JSON'da yoksa tek yedek: 1 sn.</summary>
        public const double TempoSyncFallbackMs = 1000.0;
        /// <summary>Tempo gücü JSON'da yoksa tek yedek (hareketin %70'i).</summary>
        public const float TempoSyncFallbackStrength = 0.7f;
        /// <summary>Kart "haste" der ama JSON sayı vermezse +%50.</summary>
        public const float SelfHasteBonus = 0.5f;
        /// <summary>Dost hedefi skill menzili JSON'da yoksa 6 m.</summary>
        public const float AllySkillRangeM = 6f;
    }
}
