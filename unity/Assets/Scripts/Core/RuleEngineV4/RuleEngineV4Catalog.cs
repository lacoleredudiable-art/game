using System;
using System.Collections.Generic;
using System.Globalization;
using Dovus.Core.Data;
using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>kural-motoru-v4.json — sayılar kodda gömülmez.</summary>
    public sealed class RuleEngineV4Catalog
    {
        readonly Dictionary<int, RuleEngineV4Verb> _verbs = new();
        readonly Dictionary<int, RuleEngineV4Adjective> _adjectives = new();
        readonly Dictionary<int, RuleEngineV4Weapon> _weapons = new();

        RuleEngineV4Catalog(
            RuleEngineV4Scale scale,
            RuleEngineV4Globals globals,
            RuleEngineV4WorldPhysics worldPhysics,
            RuleEngineV4SkillAnimCatalog skillAnim,
            RuleEngineV4SliceArena sliceArena)
        {
            Scale = scale;
            Globals = globals;
            WorldPhysics = worldPhysics;
            SkillAnim = skillAnim;
            SliceArena = sliceArena;
        }

        public RuleEngineV4Scale Scale { get; }
        public RuleEngineV4Globals Globals { get; }
        public RuleEngineV4WorldPhysics WorldPhysics { get; }
        public RuleEngineV4SkillAnimCatalog SkillAnim { get; }
        public RuleEngineV4SliceArena SliceArena { get; }

        public bool TryGetVerb(int id, out RuleEngineV4Verb verb) => _verbs.TryGetValue(id, out verb!);
        public bool TryGetAdjective(int id, out RuleEngineV4Adjective adjective) => _adjectives.TryGetValue(id, out adjective!);
        public bool TryGetWeapon(int id, out RuleEngineV4Weapon weapon) => _weapons.TryGetValue(id, out weapon!);

        public static RuleEngineV4Catalog FromJson(string json)
        {
            JsonValue root = MiniJson.Parse(json);
            var scaleNode = root["scale"];
            var globalsNode = root["globals"];
            RuleEngineV4WorldPhysics worldPhysics = ParseWorldPhysics(root["world_physics"]);
            RuleEngineV4SkillAnimCatalog skillAnim = RuleEngineV4SkillAnimCatalog.FromJson(root);
            RuleEngineV4SliceArena sliceArena = RuleEngineV4SliceArena.FromJson(root);
            var catalog = new RuleEngineV4Catalog(
                new RuleEngineV4Scale
                {
                    PlayerMaxHp = F(scaleNode["player_max_hp"], 1000),
                    BaseDamage = F(scaleNode["base_damage"], 100),
                    BaseHeal = F(scaleNode["base_heal"], 100),
                    BasePoise = F(scaleNode["base_poise"], RuleEngineV4CatalogDefaults.BasePoise),
                    RangeReferenceM = F(scaleNode["range_reference_m"], RuleEngineV4CatalogDefaults.RangeReferenceM),
                },
                new RuleEngineV4Globals
                {
                    PrefireCapSec = F(globalsNode["prefire_cap_sec"], 1),
                    FriendlyRangeCapM = F(globalsNode["friendly_range_cap_m"], 6),
                    ClipInnerMeasuresToRange = globalsNode["clip_inner_measures_to_range"].AsBool(true),
                    YogunChargeSec = F(globalsNode["yogun_charge_sec"], RuleEngineV4CatalogDefaults.YogunChargeSec),
                    YogunPowerMult = F(globalsNode["yogun_power_mult"], RuleEngineV4CatalogDefaults.YogunPowerMult),
                    YogunDurationMult = F(globalsNode["yogun_duration_mult"], 0.5f),
                    YayilanRadiusM = F(globalsNode["yayilan_radius_m"], RuleEngineV4CatalogDefaults.YayilanRadiusM),
                    YayilanTargetCap = globalsNode["yayilan_target_cap"].AsInt(5),
                    YayilanPowerMult = F(globalsNode["yayilan_power_mult"], RuleEngineV4CatalogDefaults.YayilanPowerMult),
                    SicrayanBounceCount = globalsNode["sicrayan_bounce_count"].AsInt(2),
                    SicrayanBounceMult = F(globalsNode["sicrayan_bounce_mult"], 0.5f),
                    SicrayanSearchM = F(globalsNode["sicrayan_search_m"], 6),
                    GudumluLockCapSec = F(globalsNode["gudumlu_lock_cap_sec"], 8),
                    GudumluBlockSec = F(globalsNode["gudumlu_lock_block_sec"], 3),
                    SabitStructureLifeSec = F(globalsNode["sabit_structure_life_sec"], 6),
                    RitmReferenceTotalSec = F(globalsNode["ritm_reference_total_sec"], RuleEngineV4CatalogDefaults.RitmReferenceTotalSec),
                    KontrolDurationSec = F(globalsNode["kontrol_duration_sec"], RuleEngineV4CatalogDefaults.KontrolDurationSec),
                    KorumaYogunBlockSec = F(globalsNode["koruma_yogun_block_sec"], RuleEngineV4CatalogDefaults.KorumaYogunBlockSec),
                    KorumaDefaultBlockSec = F(globalsNode["koruma_default_block_sec"], 2f),
                    TetikliWaitSec = F(globalsNode["tetikli_wait_sec"], RuleEngineV4CatalogDefaults.TetikliWaitSec),
                    IsaretUseRangeM = F(globalsNode["isaret_use_range_m"], RuleEngineV4CatalogDefaults.IsaretUseRangeM),
                    IsaretLifeSec = F(globalsNode["isaret_life_sec"], RuleEngineV4CatalogDefaults.IsaretLifeSec),
                    IsaretMaxPerPlayer = globalsNode["isaret_max_per_player"].AsInt(4),
                    ZamanLookbackSec = F(globalsNode["zaman_lookback_sec"], RuleEngineV4CatalogDefaults.ZamanLookbackSec),
                    KuvvetPushM = F(globalsNode["kuvvet_push_m"], RuleEngineV4CatalogDefaults.KuvvetPushM),
                    YansitmaRatio = F(globalsNode["yansitma_ratio"], RuleEngineV4CatalogDefaults.YansitmaRatio),
                    YansitmaDurationSec = F(globalsNode["yansitma_duration_sec"], RuleEngineV4CatalogDefaults.YansitmaDurationSec),
                    YansitmaYogunDurationSec = F(globalsNode["yansitma_yogun_duration_sec"], RuleEngineV4CatalogDefaults.YansitmaYogunDurationSec),
                    ConjureLifeSec = F(globalsNode["conjure_life_sec"], RuleEngineV4CatalogDefaults.ConjureLifeSec),
                    ConjureYogunLifeSec = F(globalsNode["conjure_yogun_life_sec"], RuleEngineV4CatalogDefaults.ConjureYogunLifeSec),
                },
                worldPhysics,
                skillAnim,
                sliceArena);

            foreach (KeyValuePair<string, JsonValue> kv in root["verbs"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    catalog._verbs[id] = ParseVerb(kv.Value, id);

            foreach (KeyValuePair<string, JsonValue> kv in root["adjectives"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    catalog._adjectives[id] = ParseAdjective(kv.Value, id);

            foreach (KeyValuePair<string, JsonValue> kv in root["weapons"].AsObject())
                if (int.TryParse(kv.Key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    catalog._weapons[id] = ParseWeapon(kv.Value, id);

            RuleEngineV4WorldPhysicsRuntime.Bind(catalog.WorldPhysics);
            return catalog;
        }

        static RuleEngineV4WorldPhysics ParseWorldPhysics(JsonValue node)
        {
            RuleEngineV4WorldPhysics d = RuleEngineV4WorldPhysics.Default;
            if (node.IsNull)
                return d;
            return new RuleEngineV4WorldPhysics
            {
                BodyRadiusPlayerM = F(node["body_radius_player_m"], d.BodyRadiusPlayerM),
                BodyRadiusBossM = F(node["body_radius_boss_m"], d.BodyRadiusBossM),
                BodyRadiusCreatureM = F(node["body_radius_creature_m"], d.BodyRadiusCreatureM),
                BodyRadiusStructureM = F(node["body_radius_structure_m"], d.BodyRadiusStructureM),
                DashSpeedMps = F(node["dash_speed_mps"], d.DashSpeedMps),
                ApproachSpeedMps = F(node["approach_speed_mps"], d.ApproachSpeedMps),
                MotionCarryRatio = F(node["motion_carry_ratio"], d.MotionCarryRatio),
                MotionSpeedMaxMult = F(node["motion_speed_max_mult"], d.MotionSpeedMaxMult),
                ProjectileRadiusM = F(node["projectile_radius_m"], d.ProjectileRadiusM),
                DefaultMissileSpeedMps = F(node["default_missile_speed_mps"], d.DefaultMissileSpeedMps),
                ProtectionInterceptWidthM = F(node["protection_intercept_width_m"], d.ProtectionInterceptWidthM),
                StructureMaxPerPlayer = node["structure_max_per_player"].AsInt(d.StructureMaxPerPlayer),
                StructureMaxGlobal = node["structure_max_global"].AsInt(d.StructureMaxGlobal),
                BounceAngleConeDeg = F(node["bounce_angle_cone_deg"], d.BounceAngleConeDeg),
                DiminishMultSecond = F(node["diminish_mult_second"], d.DiminishMultSecond),
                DiminishMultThird = F(node["diminish_mult_third"], d.DiminishMultThird),
                ProjectileColumnHalfHeightM = F(node["projectile_column_half_height_m"], d.ProjectileColumnHalfHeightM),
            };
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
                TotalSec = F(node["total_sec"], RuleEngineV4CatalogDefaults.WeaponTotalSecFallback),
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
