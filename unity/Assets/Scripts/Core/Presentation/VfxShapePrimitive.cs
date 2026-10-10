namespace Dovus.Core.Presentation
{
    /// <summary>efekt-motoru §2 sıfat → şekil primitif.</summary>
    public enum VfxShapePrimitive : byte
    {
        None = 0,
        CollapsePoint = 1,  // Yoğun
        TargetLock = 2,     // Odaklı
        BounceArc = 3,      // Sıçrayan
        Structure = 4,      // Sabit
        ExpandingRing = 5,  // Yayılan
        LockTrail = 6,      // Güdümlü
        TrapMark = 7,       // Tetikli
        CostFlow = 8,       // Fedakarlık
        MarkChain = 9,      // İşaretli
        Line = 10,          // Çizgisel
        LoadRing = 11,      // Ortak
        OrbitParts = 12     // Yörüngeli
    }
}
