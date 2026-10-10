using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    public enum WeaponDeliveryClass
    {
        Melee,
        Missile,
        Area
    }

    public sealed class RuleEngineV4Scale
    {
        public float PlayerMaxHp { get; init; }
        public float BaseDamage { get; init; }
        public float BaseHeal { get; init; }
        public float BasePoise { get; init; }
        public float RangeReferenceM { get; init; }
    }

    public sealed class RuleEngineV4Globals
    {
        public float PrefireCapSec { get; init; }
        public float FriendlyRangeCapM { get; init; }
        public bool ClipInnerMeasuresToRange { get; init; }
        public float YogunChargeSec { get; init; }
        public float YogunPowerMult { get; init; }
        public float YogunDurationMult { get; init; }
        public float YayilanRadiusM { get; init; }
        public int YayilanTargetCap { get; init; }
        public float YayilanPowerMult { get; init; }
        public int SicrayanBounceCount { get; init; }
        public float SicrayanBounceMult { get; init; }
        public float SicrayanSearchM { get; init; }
        public float GudumluLockCapSec { get; init; }
        public float GudumluBlockSec { get; init; }
        public float SabitStructureLifeSec { get; init; }
        public float RitmReferenceTotalSec { get; init; }
        public float KontrolDurationSec { get; init; }
        public float KorumaYogunBlockSec { get; init; }
        public float KorumaDefaultBlockSec { get; init; }
        public float TetikliWaitSec { get; init; }
        public float IsaretUseRangeM { get; init; }
        public float IsaretLifeSec { get; init; }
        public int IsaretMaxPerPlayer { get; init; }
        public float ZamanLookbackSec { get; init; }
        public float KuvvetPushM { get; init; }
        public float YansitmaRatio { get; init; }
        public float YansitmaDurationSec { get; init; }
        public float YansitmaYogunDurationSec { get; init; }
        public float ConjureLifeSec { get; init; }
        public float ConjureYogunLifeSec { get; init; }
    }

    public sealed class RuleEngineV4Verb
    {
        public int Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public bool Hostile { get; init; }
    }

    public sealed class RuleEngineV4Adjective
    {
        public int Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Body { get; init; } = string.Empty;
        public bool ChargeOnHostileOnly { get; init; }
    }

    public sealed class RuleEngineV4Weapon
    {
        public int Id { get; init; }
        public string Key { get; init; } = string.Empty;
        public float RangeMult { get; init; }
        public WeaponDeliveryClass Delivery { get; init; }
        public float PrefireSec { get; init; }
        public float RecoverySec { get; init; }
        public float TotalSec { get; init; }
        public bool PrefireMoves { get; init; }
        public float TravelSpeedMps { get; init; }
        public bool TravelSkillAllowed { get; init; }
        public int HitParts { get; init; }

        public float RangeM(float rangeReferenceM) => RangeMult * rangeReferenceM;

        public float DamageScale(float ritmReferenceTotalSec) =>
            TotalSec / Math.Max(0.0001f, ritmReferenceTotalSec);
    }

    /// <summary>kural-motoru-v4.json — sayılar kodda gömülmez.</summary>
    public sealed class RuleEngineV4Catalog
    {
        readonly Dictionary<int, RuleEngineV4Verb> _verbs = new();
        readonly Dictionary<int, RuleEngineV4Adjective> _adjectives = new();
        readonly Dictionary<int, RuleEngineV4Weapon> _weapons = new();

        RuleEngineV4Catalog(
            RuleEngineV4Scale scale,
            RuleEngineV4Globals globals)
        {
            Scale = scale;
            Globals = globals;
        }

        public RuleEngineV4Scale Scale { get; }
        public RuleEngineV4Globals Globals { get; }

        public bool TryGetVerb(int id, out RuleEngineV4Verb verb) => _verbs.TryGetValue(id, out verb!);
        public bool TryGetAdjective(int id, out RuleEngineV4Adjective adjective) => _adjectives.TryGetValue(id, out adjective!);
        public bool TryGetWeapon(int id, out RuleEngineV4Weapon weapon) => _weapons.TryGetValue(id, out weapon!);

        public static RuleEngineV4Catalog FromJson(string json)
        {
            JsonValue root = MiniJson.Parse(json);
            var scaleNode = root["scale"];
            var globalsNode = root["globals"];
            var catalog = new RuleEngineV4Catalog(
                new RuleEngineV4Scale
                {
                    PlayerMaxHp = F(scaleNode["player_max_hp"], 1000),
                    BaseDamage = F(scaleNode["base_damage"], 100),
                    BaseHeal = F(scaleNode["base_heal"], 100),
                    BasePoise = F(scaleNode["base_poise"], 50),
                    RangeReferenceM = F(scaleNode["range_reference_m"], 6),
                },
                new RuleEngineV4Globals
                {
                    PrefireCapSec = F(globalsNode["prefire_cap_sec"], 1),
                    FriendlyRangeCapM = F(globalsNode["friendly_range_cap_m"], 6),
                    ClipInnerMeasuresToRange = globalsNode["clip_inner_measures_to_range"].AsBool(true),
                    YogunChargeSec = F(globalsNode["yogun_charge_sec"], 0.35f),
                    YogunPowerMult = F(globalsNode["yogun_power_mult"], 1.35f),
                    YogunDurationMult = F(globalsNode["yogun_duration_mult"], 0.5f),
                    YayilanRadiusM = F(globalsNode["yayilan_radius_m"], 4),
                    YayilanTargetCap = globalsNode["yayilan_target_cap"].AsInt(5),
                    YayilanPowerMult = F(globalsNode["yayilan_power_mult"], 0.85f),
                    SicrayanBounceCount = globalsNode["sicrayan_bounce_count"].AsInt(2),
                    SicrayanBounceMult = F(globalsNode["sicrayan_bounce_mult"], 0.5f),
                    SicrayanSearchM = F(globalsNode["sicrayan_search_m"], 6),
                    GudumluLockCapSec = F(globalsNode["gudumlu_lock_cap_sec"], 8),
                    GudumluBlockSec = F(globalsNode["gudumlu_lock_block_sec"], 3),
                    SabitStructureLifeSec = F(globalsNode["sabit_structure_life_sec"], 6),
                    RitmReferenceTotalSec = F(globalsNode["ritm_reference_total_sec"], 0.55f),
                    KontrolDurationSec = F(globalsNode["kontrol_duration_sec"], 1.5f),
                    KorumaYogunBlockSec = F(globalsNode["koruma_yogun_block_sec"], 0.45f),
                    KorumaDefaultBlockSec = F(globalsNode["koruma_default_block_sec"], 2f),
                    TetikliWaitSec = F(globalsNode["tetikli_wait_sec"], 5f),
                    IsaretUseRangeM = F(globalsNode["isaret_use_range_m"], 25f),
                    IsaretLifeSec = F(globalsNode["isaret_life_sec"], 20f),
                    IsaretMaxPerPlayer = globalsNode["isaret_max_per_player"].AsInt(4),
                    ZamanLookbackSec = F(globalsNode["zaman_lookback_sec"], 3f),
                    KuvvetPushM = F(globalsNode["kuvvet_push_m"], 3f),
                    YansitmaRatio = F(globalsNode["yansitma_ratio"], 0.85f),
                    YansitmaDurationSec = F(globalsNode["yansitma_duration_sec"], 2f),
                    YansitmaYogunDurationSec = F(globalsNode["yansitma_yogun_duration_sec"], 0.45f),
                    ConjureLifeSec = F(globalsNode["conjure_life_sec"], 5f),
                    ConjureYogunLifeSec = F(globalsNode["conjure_yogun_life_sec"], 2.5f),
                });

            foreach (KeyValuePair<string, JsonValue> kv in root["verbs"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    catalog._verbs[id] = ParseVerb(kv.Value, id);

            foreach (KeyValuePair<string, JsonValue> kv in root["adjectives"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    catalog._adjectives[id] = ParseAdjective(kv.Value, id);

            foreach (KeyValuePair<string, JsonValue> kv in root["weapons"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    catalog._weapons[id] = ParseWeapon(kv.Value, id);

            return catalog;
        }

        static RuleEngineV4Verb ParseVerb(JsonValue node, int id) =>
            new()
            {
                Id = id,
                Key = node["key"].AsString(),
                Hostile = node["hostile"].AsBool(node["side"].AsString() == "hostile"),
            };

        static RuleEngineV4Adjective ParseAdjective(JsonValue node, int id) =>
            new()
            {
                Id = id,
                Key = node["key"].AsString(),
                Body = node["body"].AsString(),
                ChargeOnHostileOnly = node["charge_on_hostile_only"].AsBool(false),
            };

        static RuleEngineV4Weapon ParseWeapon(JsonValue node, int id)
        {
            string delivery = node["delivery"].AsString();
            WeaponDeliveryClass cls = delivery switch
            {
                "missile" => WeaponDeliveryClass.Missile,
                "area" => WeaponDeliveryClass.Area,
                _ => WeaponDeliveryClass.Melee,
            };
            return new RuleEngineV4Weapon
            {
                Id = id,
                Key = node["key"].AsString(),
                RangeMult = F(node["range_mult"], 1),
                Delivery = cls,
                PrefireSec = F(node["prefire_sec"], 0),
                RecoverySec = F(node["recovery_sec"], 0),
                TotalSec = F(node["total_sec"], 0.55f),
                PrefireMoves = node["prefire_moves"].AsBool(false),
                TravelSpeedMps = F(node["travel_speed_mps"], 10),
                TravelSkillAllowed = node["travel_skill_allowed"].AsBool(false),
                HitParts = node["hit_parts"].AsInt(1),
            };
        }

        static float F(JsonValue node, float fallback) =>
            node.IsNull ? fallback : (float)node.AsDouble(fallback);
    }
}
