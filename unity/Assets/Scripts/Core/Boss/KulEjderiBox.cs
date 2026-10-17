using System;

namespace Dovus.Core.Boss
{
    /// <summary>Eksene hizalı kutu (arena koordinatı). Vuruş kutusunun gövdesi ve yere inen sütunu bu biçimdedir.</summary>
    public readonly struct KulEjderiBox
    {
        public KulEjderiBox(float minX, float maxX, float minY, float maxY, float minZ, float maxZ)
        {
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;
        }

        public float MinX { get; }
        public float MaxX { get; }
        public float MinY { get; }
        public float MaxY { get; }
        public float MinZ { get; }
        public float MaxZ { get; }

        /// <summary>Kutunun noktaya en yakın noktası (nokta içerideyse kendisi).</summary>
        public KulEjderiPoint Nearest(KulEjderiPoint p) => new KulEjderiPoint(
            Math.Min(MaxX, Math.Max(MinX, p.X)),
            Math.Min(MaxY, Math.Max(MinY, p.Y)),
            Math.Min(MaxZ, Math.Max(MinZ, p.Z)));

        /// <summary>KE-2.6: noktadan kutunun en yakın yüzeyine üç boyutlu uzaklık (içerideyse 0).</summary>
        public float Distance(KulEjderiPoint p)
        {
            KulEjderiPoint n = Nearest(p);
            float dx = p.X - n.X, dy = p.Y - n.Y, dz = p.Z - n.Z;
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public bool Contains(KulEjderiPoint p) =>
            p.X >= MinX && p.X <= MaxX && p.Y >= MinY && p.Y <= MaxY && p.Z >= MinZ && p.Z <= MaxZ;
    }
}
