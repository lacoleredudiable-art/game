namespace Dovus.Core.Boss
{
    /// <summary>Bir silahın bir bölgeye ulaşıp ulaşmadığı ve vuruşun nasıl gösterileceği (KE-2.6, KE-2.12).</summary>
    public readonly struct KulEjderiReach
    {
        KulEjderiReach(bool reachable, bool viaColumn, KulEjderiPoint hitPoint, float distanceM)
        {
            Reachable = reachable;
            ViaColumn = viaColumn;
            HitPoint = hitPoint;
            DistanceM = distanceM;
        }

        public bool Reachable { get; }
        /// <summary>Silah gerçek gövdeye değil yere inen sütuna değdi: sunum kor hilalini silahtan
        /// <see cref="HitPoint"/>'e uçurur (KE-2.12).</summary>
        public bool ViaColumn { get; }
        /// <summary>Gerçek gövdenin vurana en yakın noktası: parlama ve hasar sayısı burada çıkar, havada değil.</summary>
        public KulEjderiPoint HitPoint { get; }
        /// <summary>Gerçek gövdenin en yakın yüzeyine uzaklık.</summary>
        public float DistanceM { get; }

        public static KulEjderiReach Body(KulEjderiPoint hitPoint, float distanceM) =>
            new KulEjderiReach(true, false, hitPoint, distanceM);

        public static KulEjderiReach Column(KulEjderiPoint hitPoint, float distanceM) =>
            new KulEjderiReach(true, true, hitPoint, distanceM);

        public static KulEjderiReach OutOfReach(float distanceM) =>
            new KulEjderiReach(false, false, default, distanceM);
    }
}
