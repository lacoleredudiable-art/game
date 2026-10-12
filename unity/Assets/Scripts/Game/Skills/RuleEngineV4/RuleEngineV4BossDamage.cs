using Dovus.Core.Boss;
using Dovus.Core.RuleEngineV4;
using Dovus.Core.Shared;
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
