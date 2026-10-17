namespace Dovus.Core.Boss
{
    /// <summary>
    /// Zamanlayıcının ürettiği küçük tehdit. Yeri (çatlak, oyuncu konumu) arena katmanı seçer; burada yalnız
    /// tür, ön işaret süresi, hasar ve alan ölçüsü vardır.
    /// </summary>
    public readonly struct KulEjderiSmallThreat
    {
        public KulEjderiSmallThreat(
            KulEjderiSmallThreatKind kind,
            double startMs,
            int telegraphMs,
            float damagePct,
            KulEjderiShape shape,
            float radiusM,
            float lengthM)
        {
            Kind = kind;
            StartMs = startMs;
            TelegraphMs = telegraphMs;
            DamagePct = damagePct;
            Shape = shape;
            RadiusM = radiusM;
            LengthM = lengthM;
        }

        public KulEjderiSmallThreatKind Kind { get; }
        public double StartMs { get; }
        public int TelegraphMs { get; }
        public float DamagePct { get; }
        public KulEjderiShape Shape { get; }
        /// <summary>Daire için yarıçap; şerit için genişlik.</summary>
        public float RadiusM { get; }
        /// <summary>Yalnız şerit için uzunluk; diğerlerinde 0.</summary>
        public float LengthM { get; }
    }
}
