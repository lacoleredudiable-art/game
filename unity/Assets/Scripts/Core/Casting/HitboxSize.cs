using System;

namespace Dovus.Core.Casting
{
    public readonly struct HitboxSize
    {
        public HitboxSize(string shape, float radiusM, float reachM, float durationSec)
        {
            Shape = shape ?? string.Empty;
            RadiusM = Math.Max(CastingDefaults.MinTickSec, radiusM);
            ReachM = Math.Max(RadiusM, reachM);
            DurationSec = Math.Max(0f, durationSec);
        }

        public string Shape { get; }
        public float RadiusM { get; }
        public float ReachM { get; }
        public float DurationSec { get; }
    }
}
