namespace Dovus.Core.Boss
{
    /// <summary>Bir bölgeye vuruşun sonucu: çarpan ya da neden hasar yok.</summary>
    public readonly struct KulEjderiHitResult
    {
        KulEjderiHitResult(float multiplier, bool reachable)
        {
            Multiplier = multiplier;
            Reachable = reachable;
        }

        /// <summary>Hasar çarpanı; hasar alınmıyorsa 0.</summary>
        public float Multiplier { get; }
        /// <summary>Bölge bu fazda var mı (yoksa çarpışmaz). Silahın ulaşıp ulaşmadığı <see cref="KulEjderiHurtboxes"/>'ta.</summary>
        public bool Reachable { get; }
        public bool TakesDamage => Multiplier > 0f;

        public static KulEjderiHitResult Hit(float multiplier) => new KulEjderiHitResult(multiplier, true);
        public static KulEjderiHitResult Unreachable() => new KulEjderiHitResult(0f, false);
    }
}
