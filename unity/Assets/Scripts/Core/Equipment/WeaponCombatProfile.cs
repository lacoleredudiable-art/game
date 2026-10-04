using System;

using Dovus.Core.Shared;
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
                orb["hold_sec"].AsFloat(orb["place_m"].AsFloat(0f) > 0f ? WeaponCombatProfileDefaults.DefaultHoldSec : 0f),
                orb["double_tap_sec"].AsFloat(orb["place_m"].AsFloat(0f) > 0f ? WeaponCombatProfileDefaults.DefaultDoubleTapSec : 0f));
        }
    }
}
