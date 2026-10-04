using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    /// <summary>skill.zone_effect JSON — yalnız Core okur.</summary>
    public readonly struct SkillZoneEffectReader
    {
        readonly JsonValue _raw;

        public SkillZoneEffectReader(JsonValue raw) => _raw = raw;

        public bool IsEmpty => _raw.IsNull;

        public float RadiusM(float fallback = 0f) => _raw["radius_m"].AsFloat(fallback);
        public float DurationSec(float fallback = 0f) => _raw["duration_sec"].AsFloat(fallback);
    }
}
