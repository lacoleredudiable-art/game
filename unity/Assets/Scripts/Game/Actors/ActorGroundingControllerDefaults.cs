namespace Dovus.Game.Actors
{
    /// <summary>Ayak / kalça kilidi: silah yürüme seti ve Loko kliplerinin kök kayması.</summary>
    public static class ActorGroundingControllerDefaults
    {
        /// <summary>
        /// O-1.23 payı: ayak kilidi bir karede görseli en çok bu kadar (dünya m) kaydırır.
        /// Yay Loko ayak salınımı sınırsız düzeltilince gövde uçuyor gibi görünür, mesh duvara girer.
        /// </summary>
        public const float MaxFootLockStepM = 0.03f;

        /// <summary>
        /// Loko_Yay_Run kalçayı kökten ~2,5 m öne kaçırıyordu: mesh kamera merkezinden uçar,
        /// döngüde geri düşer (ping / duvardan geçmiş gibi). Bu eşiğin üstü her kare düzeltilir.
        /// </summary>
        public const float HipDriftCorrectMinM = 0.05f;
    }
}
