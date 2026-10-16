namespace Dovus.Core.Hit
{
    /// <summary>Hasar hattı sabitleri. Oynanış sayısı değil, sözleşme adı ve his başlangıç tahminleri.</summary>
    public static class HitDefaults
    {
        /// <summary>O-1.14: vurulma bölgesi çarpışma kapsülüyle aynı, kafa/kol ayrı bölge yok. Tek bölgenin adı.</summary>
        public const string BodyZoneId = "body";

        /// <summary>Bölge hasar çarpanı varsayılanı (kâğıt: O-1.14 bölgeler arası fark yok).</summary>
        public const float NeutralZoneFactor = 1f;

        /// <summary>Can barı hayalet şeridi vuruştan sonra bu kadar ms bekler (tahmin, ölçüm: telefonda bakılacak).</summary>
        public const double TrailHoldMs = 400d;

        /// <summary>Hayalet şerit saniyede bar oranının bu kadarını tüketir (tahmin, ölçüm).</summary>
        public const float TrailDrainRatioPerSec = 0.6f;
    }
}
