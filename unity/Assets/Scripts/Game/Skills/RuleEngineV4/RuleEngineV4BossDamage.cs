using Dovus.Core.Boss;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game.Skills.RuleEngineV4
{
    /// <summary>
    /// O-6.9, O-6.14–O-6.18: dilimde boss saldırı hasarlarının tek kaynağı, kural-motoru-v4.json <c>slice_boss_attacks</c>.
    /// Boss JSON'undaki ve <see cref="BossTuning"/>'deki eski ölçekli hasarlar okunmaz; JSON'da olmayan alan
    /// [V4-FALLBACK] ile yazılır.
    /// </summary>
    public static class RuleEngineV4BossDamage
    {
        public static BossAttackDamage Resolve(RuleEngineV4SliceBossAttacks json, BossTuning legacy, StatusTuning legacyStatus)
        {
            return new BossAttackDamage(
                Mathf.RoundToInt(Pick(json.HasLegSlamDamage, json.LegSlamDamage, "leg_slam_damage", legacy.Damage)),
                Mathf.RoundToInt(Pick(json.HasWebSpitDamagePerProjectile, json.WebSpitDamagePerProjectile,
                    "web_spit_damage_per_projectile", legacy.VolleyDamage)),
                Mathf.RoundToInt(Pick(json.HasPoisonBreathDamage, json.PoisonBreathDamage, "poison_breath_damage",
                    legacy.FireConeDamage)),
                Mathf.RoundToInt(Pick(json.HasPounceDamage, json.PounceDamage, "pounce_damage", legacy.PounceDamage)),
                Pick(json.HasPoisonBreathPoisonSec, json.PoisonBreathPoisonSec, "poison_breath_poison_sec",
                    (float)(legacyStatus.PoisonMs / Units.SecToMs)),
                Pick(json.HasPoisonBreathPoisonPerSec, json.PoisonBreathPoisonPerSec, "poison_breath_poison_per_sec",
                    legacyStatus.PoisonDamagePerSec));
        }

        /// <summary>
        /// O-6.17: Zehir Nefesi konisi (menzil boss merkezinden, açı TAM açı) ve ön işareti dilimde JSON'dan;
        /// <see cref="BossTuning"/> yarı açı ve ms tuttuğu için çevrilerek yazılır.
        /// </summary>
        public static void ApplyPoisonBreathShape(RuleEngineV4SliceBossAttacks json, BossTuning boss)
        {
            boss.FireConeRadiusM = Pick(json.HasPoisonBreathConeRangeM, json.PoisonBreathConeRangeM,
                "poison_breath_cone_range_m", boss.FireConeRadiusM);
            boss.FireConeArcHalfAngleDeg = Pick(json.HasPoisonBreathConeAngleDeg, json.PoisonBreathConeAngleDeg,
                "poison_breath_cone_angle_deg", boss.FireConeArcHalfAngleDeg * 2f) * 0.5f;
            boss.FireConeWindupMs = Mathf.RoundToInt(Pick(json.HasPoisonBreathWindupSec, json.PoisonBreathWindupSec,
                "poison_breath_windup_sec", (float)(boss.FireConeWindupMs / Units.SecToMs)) * (float)Units.SecToMs);
        }

        /// <summary>Y17 (13 Ekim, K): "Zehirli Ateş" ek tiki kural kitabında yok; dilimde kapalı.</summary>
        public static void DisableLegacyPoisonCombo(StatusTuning status) => status.BurnPoisonComboBonusPerSec = 0;

        /// <summary>
        /// Y17 (13 Ekim, K): dilimde zehir yalnız Zehir Nefesi tablosundan gelir; StatusTuning zehri isteyen yol
        /// uygulamaz, bunu [V4-FALLBACK] ile yazar.
        /// </summary>
        public static bool RejectLegacyPoison(StatusKind kind, string where)
        {
            if (kind != StatusKind.Poison || !RuleEngineV4Feature.Enabled)
                return false;
            Debug.LogError(
                $"[V4-FALLBACK] {where}: dilimde zehir yalnız slice_boss_attacks.poison_breath_* ile uygulanır; " +
                "StatusTuning poison uygulanmadı");
            return true;
        }

        static float Pick(bool has, float value, string key, float legacy)
        {
            if (has)
                return value;
            Debug.LogError(
                $"[V4-FALLBACK] RuleEngineV4BossDamage: kural-motoru-v4.json slice_boss_attacks.{key} yok; " +
                $"eski ölçekli değer {legacy:0.##} kullanıldı");
            return legacy;
        }
    }
}
