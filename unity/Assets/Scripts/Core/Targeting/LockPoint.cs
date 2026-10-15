namespace Dovus.Core.Targeting
{
    public enum LockPointKind
    {
        Body = 0,
        Head = 1,
        Wing = 2,
        Other = 3,
    }

    /// <summary>
    /// Bir hedef üzerindeki kilit noktası (kafa / gövde / kanat…). Yalnız nişan ve kamera bakış noktasıdır;
    /// vurulma bölgesi değildir (O-1.14: kafa, kol ayrı hurtbox yok). Yükseklik Game katmanında modelden ölçülür.
    /// </summary>
    public readonly struct LockPoint
    {
        public LockPoint(int id, LockPointKind kind, float heightM, bool available = true)
        {
            Id = id;
            Kind = kind;
            HeightM = heightM;
            Available = available;
        }

        public int Id { get; }
        public LockPointKind Kind { get; }
        /// <summary>Zeminden yükseklik (m).</summary>
        public float HeightM { get; }
        /// <summary>Gizli, yok edilmiş ya da erişilemeyen nokta false olur.</summary>
        public bool Available { get; }
    }
}
