namespace Dovus.Core.Combat
{
    /// <summary>
    /// JSON <c>ignore_armor</c> zırhı tamamen silmez. Bayrak varsa delme %50'dir.
    /// Bayrak yoksa mevcut delme (pasif düz/yüzde) olduğu gibi kalır.
    /// Boruya elle verilen %100 delme bu kapıdan geçmez.
    /// </summary>
    public static class ArmorPierce
    {
        public const float IgnoreArmorCap = 0.5f;

        public static float ApplyIgnoreArmor(float existingPen, bool ignoreArmor)
        {
            if (existingPen < 0f)
                existingPen = 0f;
            if (!ignoreArmor)
                return existingPen;
            return IgnoreArmorCap;
        }
    }
}
