namespace Dovus.Core.Boss
{
    /// <summary>Arena koordinatı (18 §0): X doğu, Y yukarı, Z kuzey; metre.</summary>
    public readonly struct KulEjderiPoint
    {
        public KulEjderiPoint(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
    }
}
