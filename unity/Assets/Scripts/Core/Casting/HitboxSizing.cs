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

    /// <summary>hitbox_formula: final = base × weapon.range_mult × adjective.size_mult.</summary>
    public static class HitboxSizing
    {
        /// <summary>
        /// Tablo (hitbox_vfx.sifat_override.size_mult) ile skill'in kendi hitbox_scale_mult'u
        /// ayrı yazılabiliyor. Kapı ve vuruş, ikisinin büyüğünü kullanır; küçük olan cast'i
        /// JSON'un izin verdiği kenar menzilinin altında kesmesin.
        /// </summary>
        public static float AdjectiveScale(float tableSizeMult, float skillHitboxScale)
        {
            float table = tableSizeMult > 0f ? tableSizeMult : 1f;
            if (skillHitboxScale > 0f)
                return Math.Max(table, skillHitboxScale);
            return table;
        }

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
                float radialSize = spec.SizeA * scale;
                return new HitboxSize(spec.Shape, radialSize, radialSize, spec.DurationSec);
            }

            float reach = spec.SizeA * scale;
            // Koninin SizeB'si genişlik değil açı (°): menzil yarıçaptır, açı ayrıca taşınır.
            bool angular = string.Equals(spec.Shape, "cone", StringComparison.Ordinal);
            float radius = spec.SizeB > 0f && !angular ? spec.SizeB * scale : reach;
            // Genişlik olarak yazılmış B: kapsül çapı B, OverlapCapsule yarıçapı B/2.
            if (spec.SizeBIsWidth && !angular)
                radius *= 0.5f;
            return new HitboxSize(spec.Shape, radius, reach, spec.DurationSec);
        }
    }
}
