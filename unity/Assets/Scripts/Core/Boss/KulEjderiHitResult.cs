namespace Dovus.Core.Boss
{
    /// <summary>Bir bölgeye vuruşun sonucu: çarpan ya da neden hasar yok.</summary>
    public readonly struct KulEjderiHitResult
    {
        KulEjderiHitResult(float multiplier, bool reachable, bool closed)
        {
            Multiplier = multiplier;
            Reachable = reachable;
            Closed = closed;
        }

        /// <summary>Hasar çarpanı; hasar alınmıyorsa 0.</summary>
        public float Multiplier { get; }
        /// <summary>Bölge bu fazda/silahla vurulabilir mi (ulaşılamıyorsa çarpışmaz).</summary>
        public bool Reachable { get; }
        /// <summary>Kalp kapalı: çarpışır ama hasar almaz, gri "Kapalı" yazısı (KE-2.11).</summary>
        public bool Closed { get; }
        public bool TakesDamage => Multiplier > 0f;

        public static KulEjderiHitResult Hit(float multiplier) => new KulEjderiHitResult(multiplier, true, false);
        public static KulEjderiHitResult Unreachable() => new KulEjderiHitResult(0f, false, false);
        public static KulEjderiHitResult ClosedHeart() => new KulEjderiHitResult(0f, true, true);
    }
}
