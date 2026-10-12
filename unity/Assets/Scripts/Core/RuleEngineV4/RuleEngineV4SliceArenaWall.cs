using System;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim dış duvarının çarpışma geometrisi: iç yüzü <c>outer_wall_collision_radius_m</c> üzerinde (O-7.2: görünen
    /// duvarın iç yüzüyle aynı, görünmez duvar yok), segmentler dışa doğru uzanır ve dış köşede boşluksuz birleşir.
    /// Duvarı kuran kod ve arena sınırı aynı değeri buradan okur.
    /// </summary>
    public sealed class RuleEngineV4SliceArenaWall
    {
        public RuleEngineV4SliceArenaWall(RuleEngineV4SliceArena arena)
        {
            Segments = Math.Max(RuleEngineV4SliceArenaDefaults.MinWallSegmentCount, arena.OuterWallSegments);
            InnerFaceRadiusM = arena.OuterWallCollisionRadiusM;
            float arc = 2f * MathF.PI * InnerFaceRadiusM / Segments;
            ThicknessM = MathF.Max(
                RuleEngineV4SliceArenaDefaults.WallThicknessMinM,
                arc * RuleEngineV4SliceArenaDefaults.WallThicknessArcScale);
            DepthM = ThicknessM * RuleEngineV4SliceArenaDefaults.WallSegmentDepthScale;
            // İç yüzde komşu segmentler üst üste biner; boşluk kalmaması için kiriş dış yüzden ölçülür.
            ChordM = 2f * (InnerFaceRadiusM + DepthM) * MathF.Tan(MathF.PI / Segments);
            CenterRadiusM = InnerFaceRadiusM + DepthM * 0.5f;
        }

        public int Segments { get; }
        public float ThicknessM { get; }
        public float DepthM { get; }
        public float ChordM { get; }
        public float CenterRadiusM { get; }
        public float InnerFaceRadiusM { get; }

        /// <summary>Segment <paramref name="i"/> yüzünün ortası (x = sin, z = cos).</summary>
        public float SegmentAngleRad(int i) => i * 2f * MathF.PI / Segments;

        /// <summary>
        /// Görsel duvar köşeleri segment yüzleri arasındadır; böylece görsel yüzler çarpışma segmentleriyle aynı
        /// açıda ve düz durur (O-7.3: görsel iç yüz = çarpışma iç yüzü).
        /// </summary>
        public float CornerAngleRad(int j) => (j + 0.5f) * 2f * MathF.PI / Segments;

        /// <summary>Yüz uzaklığı (apothem) <paramref name="faceRadiusM"/> olan düzgün çokgenin köşe yarıçapı.</summary>
        public float CornerRadiusM(float faceRadiusM) => faceRadiusM / MathF.Cos(MathF.PI / Segments);
    }
}
