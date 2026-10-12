using Dovus.Core.Shared;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// kural-motoru-v4.json <c>slice_boss_attacks</c>: dilim boss'unun (Ağların Kraliçesi) saldırı hasarlarının tek
    /// kaynağı, oyuncu canı 1000 ölçeğinde (O-6.9). Bacak Çakması O-6.14, Ağ Tükürüğü mermi başına O-6.15, Zehir Nefesi
    /// vuruşu ve zehri O-6.17, Sıçrayış O-6.18. Alan yoksa <c>Has*</c> false olur; okuyan taraf bunu [V4-FALLBACK] ile
    /// görünür kılar. Kod varsayılanı yoktur.
    /// </summary>
    public sealed class RuleEngineV4SliceBossAttacks
    {
        public float LegSlamDamage { get; private set; }
        public bool HasLegSlamDamage { get; private set; }
        public float WebSpitDamagePerProjectile { get; private set; }
        public bool HasWebSpitDamagePerProjectile { get; private set; }
        public float PoisonBreathDamage { get; private set; }
        public bool HasPoisonBreathDamage { get; private set; }
        public float PoisonBreathPoisonSec { get; private set; }
        public bool HasPoisonBreathPoisonSec { get; private set; }
        public float PoisonBreathPoisonPerSec { get; private set; }
        public bool HasPoisonBreathPoisonPerSec { get; private set; }
        public float PounceDamage { get; private set; }
        public bool HasPounceDamage { get; private set; }

        public static RuleEngineV4SliceBossAttacks FromJson(JsonValue root)
        {
            var a = new RuleEngineV4SliceBossAttacks();
            JsonValue n = root["slice_boss_attacks"];
            if (n.Kind != JsonKind.Object)
                return a;
            a.HasLegSlamDamage = Read(n["leg_slam_damage"], out float slam);
            a.LegSlamDamage = slam;
            a.HasWebSpitDamagePerProjectile = Read(n["web_spit_damage_per_projectile"], out float spit);
            a.WebSpitDamagePerProjectile = spit;
            a.HasPoisonBreathDamage = Read(n["poison_breath_damage"], out float breath);
            a.PoisonBreathDamage = breath;
            a.HasPoisonBreathPoisonSec = Read(n["poison_breath_poison_sec"], out float poisonSec);
            a.PoisonBreathPoisonSec = poisonSec;
            a.HasPoisonBreathPoisonPerSec = Read(n["poison_breath_poison_per_sec"], out float poisonPerSec);
            a.PoisonBreathPoisonPerSec = poisonPerSec;
            a.HasPounceDamage = Read(n["pounce_damage"], out float pounce);
            a.PounceDamage = pounce;
            return a;
        }

        static bool Read(JsonValue node, out float value)
        {
            bool has = node.Kind == JsonKind.Number;
            value = has ? node.AsFloat(0f) : 0f;
            return has;
        }
    }
}
