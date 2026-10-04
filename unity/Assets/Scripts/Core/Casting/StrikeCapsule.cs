using System;

namespace Dovus.Core.Casting
{
    /// <summary>
    /// Düz vuruş kapsülü, bakış ekseni boyunca ölçülür. Saldıranın gövde kenarından başlar,
    /// uçlar dahil toplam boyu reach kadardır. Menzil kenardan kenara: saldıranın kenarı →
    /// hedefin collider kenarı.
    /// </summary>
    public static class StrikeCapsule
    {
        /// <summary>Uç küre merkezlerinin gövde merkezinden uzaklığı (OverlapCapsule point0/point1).</summary>
        public static void Segment(float bodyRadiusM, float reachM, float radiusM, out float nearM, out float farM)
        {
            float r = Math.Max(CastingDefaults.MinTick01f, radiusM);
            float length = Math.Max(2f * r, reachM);
            float edge = Math.Max(0f, bodyRadiusM);
            nearM = edge + r;
            farM = edge + length - r;
        }

        /// <summary>Merkezden hedef kenarına ölçülen mesafeyi kenardan kenara çevirip dener.</summary>
        public static bool EdgeInReach(float centerToTargetEdgeM, float bodyRadiusM, float reachM) =>
            centerToTargetEdgeM - Math.Max(0f, bodyRadiusM) <= reachM;

        /// <summary>Merkezden ölçen hedef aramalarına verilecek menzil (kenar menzili + gövde).</summary>
        public static float CenterRange(float bodyRadiusM, float reachM) =>
            reachM + Math.Max(0f, bodyRadiusM);
    }
}
