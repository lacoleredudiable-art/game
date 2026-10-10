namespace Dovus.Core.Presentation
{
    /// <summary>efekt-motoru §3 silah → taşıyıcı (teslim görünüşü).</summary>
    public enum VfxCarrierKind : byte
    {
        None = 0,
        FistRing = 1,
        EmberArrow = 2,
        LetterCluster = 3,
        FlowingEmberTrail = 4, // Kılıç akan kor izi
        PalmCone = 5,
        GroundCrack = 6,
        CannonBall = 7,
        BeamQuad = 8,
        SealDecal = 9,
        ShieldShock = 10
    }

    /// <summary>efekt-motoru §3 teslim sınıfı (yalnız görüntü).</summary>
    public enum VfxDeliveryClass : byte
    {
        None = 0,
        Melee = 1,
        Projectile = 2,
        Area = 3
    }
}
