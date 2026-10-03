using Dovus.Core.Grammar;

namespace Dovus.Core.Data
{
    public readonly struct SkillEngineModifiers
    {
        readonly JsonValue _raw;

        public SkillEngineModifiers(JsonValue raw) => _raw = raw;

        internal JsonValue Raw => _raw;

        public bool IsNull => _raw.IsNull;

        public bool HasElementMult => _raw.Has("element_mult");
        public bool HasCritChanceAdd => _raw.Has("crit_chance_add");
        public bool HasLifesteal => _raw.Has("lifesteal");
        public bool HasDuplicateDamageMult => _raw.Has("duplicate_damage_mult");
        public bool HasBounceDamageMult => _raw.Has("bounce_damage_mult");

        public bool Aoe(bool fallback) => _raw["aoe"].AsBool(fallback);
        public float ArmorAdd(float fallback) => _raw["armor_add"].AsFloat(fallback);
        public float ApplyLifesteal(float fallback) => _raw["apply_lifesteal"].AsFloat(fallback);
        public float BuffArmor(float fallback) => _raw["buff_armor"].AsFloat(fallback);
        public float BuffDamage(float fallback) => _raw["buff_damage"].AsFloat(fallback);
        public float BuffDurationSec(float fallback) => _raw["buff_duration_sec"].AsFloat(fallback);
        public int BounceTargets(int fallback) => _raw["bounce_targets"].AsInt(fallback);
        public float BounceDamageMult(float fallback) => _raw["bounce_damage_mult"].AsFloat(fallback);
        public float CcDurationSec(float fallback) => _raw["cc_duration_sec"].AsFloat(fallback);
        public float ChannelSec(float fallback) => _raw["channel_sec"].AsFloat(fallback);
        public int CleanseCount(int fallback) => _raw["cleanse_count"].AsInt(fallback);
        public float CritChanceAdd(float fallback) => _raw["crit_chance_add"].AsFloat(fallback);
        public float DashDistanceM(float fallback) => _raw["dash_distance_m"].AsFloat(fallback);
        public float DebuffArmor(float fallback) => _raw["debuff_armor"].AsFloat(fallback);
        public float DebuffDurationSec(float fallback) => _raw["debuff_duration_sec"].AsFloat(fallback);
        public bool DuplicateCast(bool fallback) => _raw["duplicate_cast"].AsBool(fallback);
        public float DuplicateDelaySec(float fallback) => _raw["duplicate_delay_sec"].AsFloat(fallback);
        public float DuplicateDamageMult(float fallback) => _raw["duplicate_damage_mult"].AsFloat(fallback);
        public float ElementMult(float fallback) => _raw["element_mult"].AsFloat(fallback);
        public float HitboxScaleMult(float fallback) => _raw["hitbox_scale_mult"].AsFloat(fallback);
        public string HitboxOverride(string fallback) => _raw["hitbox_override"].AsString(fallback);
        public bool IgnoreArmor(bool fallback) => _raw["ignore_armor"].AsBool(fallback);
        public float LifetimeAdd(float fallback) => _raw["lifetime_add"].AsFloat(fallback);
        public float Lifesteal(float fallback) => _raw["lifesteal"].AsFloat(fallback);
        public int MaxTargets(int fallback) => _raw["max_targets"].AsInt(fallback);
        public int MinionCount(int fallback) => _raw["minion_count"].AsInt(fallback);
        public float MinionDurationSec(float fallback) => _raw["minion_duration_sec"].AsFloat(fallback);
        public float ReflectDurationSec(float fallback) => _raw["reflect_duration_sec"].AsFloat(fallback);
        public float ReflectRatio(float fallback) => _raw["reflect_ratio"].AsFloat(fallback);
        public float SelfDamageBuff(float fallback) => _raw["self_damage_buff"].AsFloat(fallback);
        public float ShieldAbsorb(float fallback) => _raw["shield_absorb"].AsFloat(fallback);
        public float TempoDurationSec(float fallback) => _raw["tempo_duration_sec"].AsFloat(fallback);
        public float TickRateMult(float fallback) => _raw["tick_rate_mult"].AsFloat(fallback);
        public string TrajectoryOverride(string fallback) => _raw["trajectory_override"].AsString(fallback);
    }
}
