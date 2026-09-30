using System;

namespace Dovus.Core.Portal
{
    /// <summary>
    /// 8-1: boss kapıdan geçince 2 sn vuruş şekli %30 küçük (yarıçap, boy, en × 0,7).
    /// </summary>
    public readonly struct BossStrikeShape
    {
        public BossStrikeShape(float radius, float length, float width)
        {
            Radius = radius;
            Length = length;
            Width = width;
        }

        public float Radius { get; }
        public float Length { get; }
        public float Width { get; }

        public static BossStrikeShape Scale(float radius, float length, float width, float scale)
        {
            float m = scale > 0f ? scale : 1f;
            return new BossStrikeShape(radius * m, length * m, width * m);
        }

        /// <summary>
        /// Mevcut daire/koni kontrolü yarıçapı olduğu gibi okur.
        /// Küçük şekil, mesafeyi 1/ölçek ile büyütmekle aynıdır.
        /// </summary>
        public static float DistanceForVolume(float distance, float scale)
        {
            float m = scale > 0.001f ? scale : 1f;
            return distance / m;
        }

        public bool CircleHits(float distance) => distance <= Radius;

        public bool LineHits(float along, float side) =>
            along >= 0f && along <= Length && MathF.Abs(side) <= Width * 0.5f;
    }
}
