using Dovus.Core.Shared;

namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionEffects
    {
        public SkillResolutionEffects(JsonValue special, JsonValue zoneEffect)
        {
            Special = new SkillSpecialReader(special);
            Zone = new SkillZoneEffectReader(zoneEffect);
        }

        public SkillSpecialReader Special { get; }
        public SkillZoneEffectReader Zone { get; }
    }
}
