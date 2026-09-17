namespace Dovus.Core.Manifestation
{
    /// <summary>
    /// Prezentasyon motion_curve / fiil hitbox’tan türetilen seyahat tipi.
    /// LegacyVerb = eski words[0].Rune yolu (plan yokken).
    /// </summary>
    public enum LivingTravelKind : byte
    {
        LegacyVerb = 0,
        Linear = 1,
        Instant = 2,
        ExpandingRadial = 3,
        Static = 4
    }
}
