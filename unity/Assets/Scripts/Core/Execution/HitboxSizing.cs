using System;

namespace Dovus.Core.Execution
{
    public readonly struct HitboxSize
    {
        public HitboxSize(string shape, float radiusM, float reachM, float durationSec)
        {
            Shape = shape ?? string.Empty;
            RadiusM = Math.Max(0.01f, radiusM);
            ReachM = Math.Max(RadiusM, reachM);
            DurationSec = Math.Max(0f, durationSec);
        }

        public string Shape { get; }
        public float RadiusM { get; }
        public float ReachM { get; }
        public float DurationSec { get; }
    }

    /// <summary>hitbox_formula: final = base × weapon.range_mult × adjective.size_mult.</summary>
    public static class HitboxSizing
    {
        public static HitboxSize Resolve(
            in VerbHitboxSpec spec,
            float weaponSizeMult,
            float adjectiveSizeMult)
        {
            float weapon = weaponSizeMult > 0f ? weaponSizeMult : 1f;
            float adjective = adjectiveSizeMult > 0f ? adjectiveSizeMult : 1f;
            float scale = weapon * adjective;

            if (spec.IsRadius)
            {
                float radius = spec.SizeA * scale;
                return new HitboxSize(spec.Shape, radius, radius, spec.DurationSec);
            }

            float reach = spec.SizeA * scale;
            float radius = spec.SizeB > 0f ? spec.SizeB * 0.5f * scale : reach;
            return new HitboxSize(spec.Shape, radius, reach, spec.DurationSec);
        }
    }
}
