namespace Dovus.Game.Weapons
{
    /// <summary>
    /// Silah <c>animations_key</c> (docs/element-sistemi.json weapons[]) → sunum arketipi.
    /// Yalnız görsel eşleme; hasar/zamanlama buna dokunmaz. Arketipler Mixamo/Archetypes/&lt;Folder&gt;
    /// klasörleriyle birebir (bkz. MixamoArchetypeBind).
    /// </summary>
    public static class WeaponArchetypeMap
    {
        public const string SwordShield = "SwordShield";
        public const string Hammer = "Hammer";
        public const string Fist = "Fist";
        public const string Bow = "Bow";
        public const string Caster = "Caster";
        public const string Gun = "Gun";

        /// <summary>animations_key → arketip. Bilinmeyen silah SwordShield'a düşer (en nötr duruş).</summary>
        public static string ArchetypeFor(string weaponAnimationsKey) => weaponAnimationsKey switch
        {
            "kilic" => SwordShield,
            "kalkan" => SwordShield,
            "cekic" => Hammer,
            "yumruk" => Fist,
            "yay" => Bow,
            "asa" => Caster,
            "kitap" => Caster,
            "kure" => Caster,
            "tilsim" => Caster,
            "top" => Gun,
            _ => SwordShield
        };

        public static readonly string[] All = { SwordShield, Hammer, Fist, Bow, Caster, Gun };
    }
}
