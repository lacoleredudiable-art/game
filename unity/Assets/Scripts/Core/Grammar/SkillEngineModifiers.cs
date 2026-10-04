using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct SkillEngineModifiers
    {
        readonly JsonValue _raw;

        public SkillEngineModifiers(JsonValue raw) => _raw = raw;

        /// <summary><c>default</c> örnek (ör. <c>SkillResolution.Empty</c>) için JSON null; eski <c>EngineModifiers != null</c> kontrolünün karşılığı.</summary>
        JsonValue J => _raw ?? JsonValue.Null;

        internal JsonValue Raw => J;

        public bool IsNull => J.IsNull;

        public bool HasElementMult => J.Has("element_mult");
        public bool HasCritChanceAdd => J.Has("crit_chance_add");
        public bool HasLifesteal => J.Has("lifesteal");
        public bool HasDuplicateDamageMult => J.Has("duplicate_damage_mult");
        public bool HasBounceDamageMult => J.Has("bounce_damage_mult");

        public bool Aoe(bool fallback) => J["aoe"].AsBool(fallback);
        public float ArmorAdd(float fallback) => J["armor_add"].AsFloat(fallback);
        public float ApplyLifesteal(float fallback) => J["apply_lifesteal"].AsFloat(fallback);
        public float BuffArmor(float fallback) => J["buff_armor"].AsFloat(fallback);
        public float BuffDamage(float fallback) => J["buff_damage"].AsFloat(fallback);
        public float BuffDurationSec(float fallback) => J["buff_duration_sec"].AsFloat(fallback);
        public int BounceTargets(int fallback) => J["bounce_targets"].AsInt(fallback);
        public float BounceDamageMult(float fallback) => J["bounce_damage_mult"].AsFloat(fallback);
        public float CcDurationSec(float fallback) => J["cc_duration_sec"].AsFloat(fallback);
        public float ChannelSec(float fallback) => J["channel_sec"].AsFloat(fallback);
        public int CleanseCount(int fallback) => J["cleanse_count"].AsInt(fallback);
        public float CritChanceAdd(float fallback) => J["crit_chance_add"].AsFloat(fallback);
        public float DashDistanceM(float fallback) => J["dash_distance_m"].AsFloat(fallback);
        public float DebuffArmor(float fallback) => J["debuff_armor"].AsFloat(fallback);
        public float DebuffDurationSec(float fallback) => J["debuff_duration_sec"].AsFloat(fallback);
        public bool DuplicateCast(bool fallback) => J["duplicate_cast"].AsBool(fallback);
        public float DuplicateDelaySec(float fallback) => J["duplicate_delay_sec"].AsFloat(fallback);
        public float DuplicateDamageMult(float fallback) => J["duplicate_damage_mult"].AsFloat(fallback);
        public float ElementMult(float fallback) => J["element_mult"].AsFloat(fallback);
        public float HitboxScaleMult(float fallback) => J["hitbox_scale_mult"].AsFloat(fallback);
        public string HitboxOverride(string fallback) => J["hitbox_override"].AsString(fallback);
        public bool IgnoreArmor(bool fallback) => J["ignore_armor"].AsBool(fallback);
        public float LifetimeAdd(float fallback) => J["lifetime_add"].AsFloat(fallback);
        public float Lifesteal(float fallback) => J["lifesteal"].AsFloat(fallback);
        public int MaxTargets(int fallback) => J["max_targets"].AsInt(fallback);
        public int MinionCount(int fallback) => J["minion_count"].AsInt(fallback);
        public float MinionDurationSec(float fallback) => J["minion_duration_sec"].AsFloat(fallback);
        public float ReflectDurationSec(float fallback) => J["reflect_duration_sec"].AsFloat(fallback);
        public float ReflectRatio(float fallback) => J["reflect_ratio"].AsFloat(fallback);
        public float SelfDamageBuff(float fallback) => J["self_damage_buff"].AsFloat(fallback);
        public float ShieldAbsorb(float fallback) => J["shield_absorb"].AsFloat(fallback);
        public float TempoDurationSec(float fallback) => J["tempo_duration_sec"].AsFloat(fallback);
        public float TickRateMult(float fallback) => J["tick_rate_mult"].AsFloat(fallback);
        public string TrajectoryOverride(string fallback) => J["trajectory_override"].AsString(fallback);

        public PortalOp PortalOp() => SkillMechanicOpParse.ParsePortalOp(J["portal_op"].AsString());

        public TeamOp TeamOp() => SkillMechanicOpParse.ParseTeamOp(J["team_op"].AsString());

        public bool Has(string field) => J.Has(field);

        public float ReadFloat(string field, float fallback = 0f) => J[field].AsFloat(fallback);

        public bool ReadBool(string field, bool fallback = false) => J[field].AsBool(fallback);

        public int ReadInt(string field, int fallback = 0) => J[field].AsInt(fallback);

        public string ReadString(string field, string fallback = "") => J[field].AsString(fallback);

        /// <summary>Core/test: key erişimi. Game katmanı kullanmaz.</summary>
        public JsonValue Field(string field) => J[field];
    }
}
