using System;

namespace Dovus.Core.RuleEngineV4
{
    /// <summary>
    /// Dilim dış duvarının çarpışma geometrisi: dış köşeleri <c>outer_wall_collision_radius_m</c> üzerinde,
    /// boşluksuz birleşen kutu segmentler. İç yüz segment ortasında en içtedir; oynanış sınırı bu yarıçaptır
    /// (O-7.2). Duvarı kuran kod ve arena sınırı aynı değeri buradan okur.
    /// </summary>
    public sealed class RuleEngineV4SliceArenaWall
    {
        public RuleEngineV4SliceArenaWall(RuleEngineV4SliceArena arena)
        {
            Segments = Math.Max(RuleEngineV4SliceArenaDefaults.MinWallSegmentCount, arena.OuterWallSegments);
            float radius = arena.OuterWallCollisionRadiusM;
            float arc = 2f * MathF.PI * radius / Segments;
            ThicknessM = MathF.Max(
                RuleEngineV4SliceArenaDefaults.WallThicknessMinM,
                arc * RuleEngineV4SliceArenaDefaults.WallThicknessArcScale);
            DepthM = ThicknessM * RuleEngineV4SliceArenaDefaults.WallSegmentDepthScale;
            float halfStep = MathF.PI / Segments;
            ChordM = 2f * radius * MathF.Sin(halfStep);
            InnerFaceRadiusM = radius * MathF.Cos(halfStep) - DepthM;
            CenterRadiusM = InnerFaceRadiusM + DepthM * 0.5f;
        }

        public int Segments { get; }
        public float ThicknessM { get; }
        public float DepthM { get; }
        public float ChordM { get; }
        public float CenterRadiusM { get; }
        public float InnerFaceRadiusM { get; }
    }
}
