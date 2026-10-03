namespace Dovus.Game
{
    /// <summary>Registry tutuşu hangi elde (ayar paneli + grip-check).</summary>
    public static class WeaponGripHands
    {
        public static bool PrimaryIsRight(string weaponKey) => weaponKey switch
        {
            "yay" or "kitap" or "kalkan" => false,
            _ => true,
        };

        public static bool UsesRightProp(WeaponVisualRegistry.PropEntry entry) =>
            entry?.RightHandPrefab != null;

        public static bool UsesLeftProp(WeaponVisualRegistry.PropEntry entry) =>
            entry?.LeftHandPrefab != null;
    }
}
