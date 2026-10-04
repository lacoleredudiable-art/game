namespace Dovus.Core.Status
{
    /// <summary>
    /// element-sistemi.json mechanics listesi ile birebir id'ler.
    /// </summary>
    public enum StatusKind : byte
    {
        None = 0,
        // hard_cc
        Stun,
        Root,
        Silence,
        Knockback,
        // soft_cc
        Slow,
        Blind,
        Disarm,
        Taunt,
        // debuff
        Burn,
        ArmorBreak,
        GrievousWounds,
        Weaken,
        /// <summary>16 Eylül: element-sistemi.json mechanics'te vardı, enum'da yoktu (bilinen açık).</summary>
        Poison,
        // buff
        Shield,
        Haste,
        DamageReduction,
        Regen,
        // special
        Stasis,
        /// <summary>Görünmez / hedef dışı — gizlilik / stealth_haste.</summary>
        Stealth,
        Fear
    }
}
