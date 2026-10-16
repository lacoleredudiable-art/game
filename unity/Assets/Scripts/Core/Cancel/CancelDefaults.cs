namespace Dovus.Core.Cancel
{
    /// <summary>İptal ve tampon sayıları. Hepsi başlangıç değeridir; kesin değer Aşama 8 ayar masasında (M-13.4).</summary>
    public static class CancelDefaults
    {
        /// <summary>O-4.12 (ÖNERİ): reddedilen girdi bu kadar saklanır.</summary>
        public const int BufferMs = 150;

        /// <summary>
        /// Y24 (kullanıcı kararı): klip sürerken gelen düz vuruş dokunuşu sıraya alınır, klip bitince başlar.
        /// Süre = Kılıç klibi (O-4.14: 0,40 s); sayı belgede ayrıca yok (tahmin).
        /// </summary>
        public const int StrikeQueueMs = 400;

        /// <summary>O-4.14 (KOD <c>BasicStrikeComboResetSec</c> 1,2 s): son vuruş başlangıcından bu yana geçen süre.</summary>
        public const int StrikeComboResetMs = 1200;

        /// <summary>O-4.14 Kılıç: 3 kesiş, her biri 0,40 s, vuruş karesi 0,20 s.</summary>
        public const int SwordStrikeCount = 3;
        public const int SwordStrikeDurationMs = 400;
        public const int SwordStrikeHitMs = 200;
    }
}
