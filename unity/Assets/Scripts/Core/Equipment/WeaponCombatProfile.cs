using System;
using Dovus.Core.Grammar;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// 10 silahın teslim profili. Sayılar element-sistemi.json weapons satırından okunur.
    /// Hareket kalıbının yolunu değiştirmez; hasar, hitbox ve görsel anahtar buradadır.
    /// </summary>
    public sealed class WeaponCombatProfile
    {
        public WeaponCombatProfile(
            int id,
            string school,
            float baseArmor,
            float reachM,
            float arcDeg,
            float radiusM,
            string hitShape,
            string born,
            bool effectTravels,
            string basicKind,
            float basicReachM,
            float basicIntervalSec,
            int basicHits,
            float basicRadiusM,
            float basicAllyHeal,
            float cooldownMult,
            string passiveId,
            WeaponPassiveSpec passive,
            string swapBonusId,
            WeaponSwapBonusSpec swapBonus,
            float orbPlaceM,
            float orbMoveSec,
            float orbCooldownSec,
            float orbSpellM,
            float bossPushM = 0f,
            float recoilM = 0f,
            float orbHoldSec = 0f,
            float orbDoubleTapSec = 0f)
        {
            Id = id;
            School = school ?? string.Empty;
            BaseArmor = baseArmor > 0f ? baseArmor : 0f;
            ReachM = reachM > 0f ? reachM : 0f;
            ArcDeg = arcDeg > 0f ? arcDeg : 0f;
            RadiusM = radiusM > 0f ? radiusM : 0f;
            HitShape = hitShape ?? string.Empty;
            Born = born ?? string.Empty;
            EffectTravels = effectTravels;
            BasicKind = basicKind ?? string.Empty;
            BasicReachM = basicReachM > 0f ? basicReachM : ReachM;
            BasicIntervalSec = basicIntervalSec > 0f ? basicIntervalSec : 0f;
            BasicHits = basicHits > 0 ? basicHits : 1;
            BasicRadiusM = basicRadiusM > 0f ? basicRadiusM : 0f;
            BasicAllyHeal = basicAllyHeal > 0f ? basicAllyHeal : 0f;
            CooldownMult = cooldownMult > 0f ? cooldownMult : 1f;
            PassiveId = passiveId ?? string.Empty;
            Passive = passive ?? WeaponPassiveSpec.None;
            SwapBonusId = swapBonusId ?? string.Empty;
            SwapBonus = swapBonus ?? WeaponSwapBonusSpec.None;
            OrbPlaceM = orbPlaceM > 0f ? orbPlaceM : 0f;
            OrbMoveSec = orbMoveSec > 0f ? orbMoveSec : 0f;
            OrbCooldownSec = orbCooldownSec > 0f ? orbCooldownSec : 0f;
            OrbSpellM = orbSpellM > 0f ? orbSpellM : 0f;
            BossPushM = bossPushM > 0f ? bossPushM : 0f;
            RecoilM = recoilM > 0f ? recoilM : 0f;
            OrbHoldSec = orbHoldSec > 0f ? orbHoldSec : 0f;
            OrbDoubleTapSec = orbDoubleTapSec > 0f ? orbDoubleTapSec : 0f;
        }

        public int Id { get; }
        public string School { get; }
        public float BaseArmor { get; }
        public float ReachM { get; }
        public float ArcDeg { get; }
        public float RadiusM { get; }
        public string HitShape { get; }
        public string Born { get; }
        public bool EffectTravels { get; }
        public string BasicKind { get; }
        public float BasicReachM { get; }
        public float BasicIntervalSec { get; }
        public int BasicHits { get; }
        public float BasicRadiusM { get; }
        public float BasicAllyHeal { get; }
        public float CooldownMult { get; }
        public string PassiveId { get; }
        public WeaponPassiveSpec Passive { get; }
        public string SwapBonusId { get; }
        public WeaponSwapBonusSpec SwapBonus { get; }
        public float OrbPlaceM { get; }
        public float OrbMoveSec { get; }
        public float OrbCooldownSec { get; }
        public float OrbSpellM { get; }
        public float BossPushM { get; }
        public float RecoilM { get; }
        public float OrbHoldSec { get; }
        public float OrbDoubleTapSec { get; }

        public static WeaponCombatProfile FromRow(JsonValue row)
        {
            JsonValue basic = row["basic"];
            JsonValue passive = row["passive"];
            JsonValue bonus = row["swap_bonus"];
            JsonValue orb = row["orb"];
            string shape = row["hit_shape"].AsString();
            if (string.IsNullOrEmpty(shape))
                shape = basic["shape"].AsString();
            bool travels = row.Has("effect_travels")
                ? row["effect_travels"].AsBool(true)
                : shape is not ("seal" or "point");
            return new WeaponCombatProfile(
                row["id"].AsInt(),
                row["school"].AsString(),
                row["base_armor"].AsFloat(0f),
                row["reach_m"].AsFloat(0f),
                row["arc_deg"].AsFloat(0f),
                row["radius_m"].AsFloat(0f),
                shape,
                row["born"].AsString(),
                travels,
                basic["kind"].AsString(),
                basic["reach_m"].AsFloat(row["reach_m"].AsFloat(0f)),
                basic["interval_sec"].AsFloat(0f),
                basic["hits"].AsInt(1),
                basic["radius_m"].AsFloat(0f),
                basic["ally_heal"].AsFloat(0f),
                row["cooldown_mult"].AsFloat(1f),
                passive["id"].AsString(),
                WeaponPassiveSpec.FromJson(passive),
                bonus["id"].AsString(),
                WeaponSwapBonusSpec.FromJson(bonus),
                orb["place_m"].AsFloat(0f),
                orb["move_sec"].AsFloat(0f),
                orb["cooldown_sec"].AsFloat(0f),
                orb["spell_m"].AsFloat(0f),
                basic["boss_push_m"].AsFloat(shape == "ballistic" ? 0.5f : 0f),
                basic["recoil_m"].AsFloat(shape == "ballistic" ? 0.5f : 0f),
                orb["hold_sec"].AsFloat(orb["place_m"].AsFloat(0f) > 0f ? 0.4f : 0f),
                orb["double_tap_sec"].AsFloat(orb["place_m"].AsFloat(0f) > 0f ? 0.3f : 0f));
        }
    }

    /// <summary>Silah pasifinin JSON sayıları. Davranış <see cref="WeaponPassiveRules"/> içindedir.</summary>
    public sealed class WeaponPassiveSpec
    {
        public static readonly WeaponPassiveSpec None = new(string.Empty, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 0, 0f, 0f, 0f, 0.05f);

        public WeaponPassiveSpec(
            string id,
            float damageMult,
            float arcDeg,
            float stunSec,
            float icdSec,
            float windowSec,
            float moveWindowSec,
            float stillSec,
            float durationMult,
            float powerMult,
            int everyNth,
            float chainGapSec,
            float angleDeg,
            float critChance,
            float baseCrit)
        {
            Id = id ?? string.Empty;
            DamageMult = damageMult > 0f ? damageMult : 1f;
            ArcDeg = arcDeg;
            StunSec = stunSec;
            IcdSec = icdSec;
            WindowSec = windowSec;
            MoveWindowSec = moveWindowSec;
            StillSec = stillSec;
            DurationMult = durationMult > 0f ? durationMult : 1f;
            PowerMult = powerMult > 0f ? powerMult : 1f;
            EveryNth = everyNth;
            ChainGapSec = chainGapSec;
            AngleDeg = angleDeg;
            CritChance = critChance;
            BaseCrit = baseCrit > 0f ? baseCrit : 0.05f;
        }

        public string Id { get; }
        public float DamageMult { get; }
        public float ArcDeg { get; }
        public float StunSec { get; }
        public float IcdSec { get; }
        public float WindowSec { get; }
        public float MoveWindowSec { get; }
        public float StillSec { get; }
        public float DurationMult { get; }
        public float PowerMult { get; }
        public int EveryNth { get; }
        public float ChainGapSec { get; }
        public float AngleDeg { get; }
        public float CritChance { get; }
        public float BaseCrit { get; }

        public static WeaponPassiveSpec FromJson(JsonValue row)
        {
            if (row.IsNull || row.Kind != JsonKind.Object)
                return None;
            return new WeaponPassiveSpec(
                row["id"].AsString(),
                row["damage_mult"].AsFloat(1f),
                row["arc_deg"].AsFloat(0f),
                row["stun_sec"].AsFloat(0f),
                row["icd_sec"].AsFloat(0f),
                row["window_sec"].AsFloat(0f),
                row["move_window_sec"].AsFloat(0f),
                row["still_sec"].AsFloat(0f),
                row["duration_mult"].AsFloat(1f),
                row["power_mult"].AsFloat(1f),
                row["every_nth"].AsInt(0),
                row["chain_gap_sec"].AsFloat(0f),
                row["angle_deg"].AsFloat(0f),
                row["crit_chance"].AsFloat(0f),
                row["base_crit"].AsFloat(0.05f));
        }
    }

    public sealed class WeaponSwapBonusSpec
    {
        public static readonly WeaponSwapBonusSpec None = new(string.Empty, 2f, 1f, 0f, 0f, 0f, 0f, 1f, false, false, false);

        public WeaponSwapBonusSpec(
            string id,
            float windowSec,
            float damageMult,
            float arcDeg,
            float poiseMult,
            float shield,
            float shieldSec,
            float areaMult,
            bool ignoreArmor,
            bool cleanse,
            bool freeMana)
        {
            Id = id ?? string.Empty;
            WindowSec = windowSec > 0f ? windowSec : 2f;
            DamageMult = damageMult > 0f ? damageMult : 1f;
            ArcDeg = arcDeg;
            PoiseMult = poiseMult > 0f ? poiseMult : 1f;
            Shield = shield;
            ShieldSec = shieldSec > 0f ? shieldSec : 0f;
            AreaMult = areaMult > 0f ? areaMult : 1f;
            IgnoreArmor = ignoreArmor;
            Cleanse = cleanse;
            FreeMana = freeMana;
        }

        public string Id { get; }
        public float WindowSec { get; }
        public float DamageMult { get; }
        public float ArcDeg { get; }
        public float PoiseMult { get; }
        public float Shield { get; }
        public float ShieldSec { get; }
        public float AreaMult { get; }
        public bool IgnoreArmor { get; }
        public bool Cleanse { get; }
        public bool FreeMana { get; }
        public bool CountsAsBackstab => string.Equals(Id, "sirt", StringComparison.Ordinal);
        public bool CountsAsStill => string.Equals(Id, "sabit", StringComparison.Ordinal);
        public bool SpawnsAtLastHit => string.Equals(Id, "son_vurus", StringComparison.Ordinal);

        public static WeaponSwapBonusSpec FromJson(JsonValue row)
        {
            if (row.IsNull || row.Kind != JsonKind.Object)
                return None;
            return new WeaponSwapBonusSpec(
                row["id"].AsString(),
                row["window_sec"].AsFloat(2f),
                row["damage_mult"].AsFloat(1f),
                row["arc_deg"].AsFloat(0f),
                row["poise_mult"].AsFloat(1f),
                row["shield"].AsFloat(0f),
                row["shield_sec"].AsFloat(0f),
                row["area_mult"].AsFloat(1f),
                row["ignore_armor"].AsBool(false),
                row["cleanse"].AsBool(false),
                row["free_mana"].AsBool(false));
        }
    }
}
