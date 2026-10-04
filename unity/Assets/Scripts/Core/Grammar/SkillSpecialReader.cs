using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    /// <summary>skill.special / fiil special JSON — yalnız Core okur; Game JsonValue görmesin.</summary>
    public readonly struct SkillSpecialReader
    {
        readonly JsonValue _raw;

        public SkillSpecialReader(JsonValue raw) => _raw = raw;

        public bool IsEmpty => _raw.IsNull;

        public float HealValue(float fallback = 0f) => _raw["heal_value"].AsFloat(fallback);
        public float ShieldAmount(float fallback = 0f) => _raw["shield_amount"].AsFloat(fallback);
    }
}
