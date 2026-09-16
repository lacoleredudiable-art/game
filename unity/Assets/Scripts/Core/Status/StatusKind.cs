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
        Fear
    }

    public static class StatusKindUtil
    {
        public static bool TryParse(string id, out StatusKind kind)
        {
            kind = id switch
            {
                "stun" => StatusKind.Stun,
                "root" => StatusKind.Root,
                "silence" => StatusKind.Silence,
                "knockback" => StatusKind.Knockback,
                "slow" => StatusKind.Slow,
                "blind" => StatusKind.Blind,
                "disarm" => StatusKind.Disarm,
                "taunt" => StatusKind.Taunt,
                "burn" => StatusKind.Burn,
                "armor_break" => StatusKind.ArmorBreak,
                "grievous_wounds" => StatusKind.GrievousWounds,
                "weaken" => StatusKind.Weaken,
                "poison" => StatusKind.Poison,
                "shield" => StatusKind.Shield,
                "haste" => StatusKind.Haste,
                "damage_reduction" => StatusKind.DamageReduction,
                "regen" => StatusKind.Regen,
                "stasis" => StatusKind.Stasis,
                "fear" => StatusKind.Fear,
                "cleanse" => StatusKind.None, // eylem, durum değil
                _ => StatusKind.None
            };
            return kind != StatusKind.None || id == "cleanse";
        }

        public static bool IsHardCc(StatusKind k) =>
            k is StatusKind.Stun or StatusKind.Root or StatusKind.Silence or StatusKind.Knockback
                or StatusKind.Stasis or StatusKind.Fear;

        public static bool IsSoftCc(StatusKind k) =>
            k is StatusKind.Slow or StatusKind.Blind or StatusKind.Disarm or StatusKind.Taunt;

        public static bool IsDebuff(StatusKind k) =>
            k is StatusKind.Burn or StatusKind.ArmorBreak or StatusKind.GrievousWounds or StatusKind.Weaken
                or StatusKind.Poison;

        public static bool IsBuff(StatusKind k) =>
            k is StatusKind.Shield or StatusKind.Haste or StatusKind.DamageReduction or StatusKind.Regen;
    }
}
