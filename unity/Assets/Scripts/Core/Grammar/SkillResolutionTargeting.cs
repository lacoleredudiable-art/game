using System.Collections.Generic;

namespace Dovus.Core.Grammar
{
    public readonly struct SkillResolutionTargeting
    {
        public SkillResolutionTargeting(
            TargetModeWire mode,
            CastMobilityWire castMobility,
            IReadOnlyDictionary<string, string>? behaviors)
        {
            Mode = mode;
            CastMobility = castMobility;
            Behaviors = behaviors ?? SkillResolution.EmptyBehaviors;
        }

        public TargetModeWire Mode { get; }
        public CastMobilityWire CastMobility { get; }
        public IReadOnlyDictionary<string, string> Behaviors { get; }
    }
}
