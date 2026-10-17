using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Görev bölgesi (XZ düzlemi). Daire: taş çemberi. Kutu: gölge şeridi (HalfX/HalfZ, YawRad).</summary>
    public readonly struct TeamTaskZone
    {
        public TeamTaskZone(int id, float x, float z, float radius)
        {
            Id = id; X = x; Z = z; Radius = radius; Shape = TeamZoneShape.Circle;
            HalfX = 0f; HalfZ = 0f; YawRad = 0f;
        }

        public TeamTaskZone(int id, float x, float z, float halfX, float halfZ, float yawRad)
        {
            Id = id; X = x; Z = z; Radius = 0f; Shape = TeamZoneShape.Box;
            HalfX = halfX; HalfZ = halfZ; YawRad = yawRad;
        }

        public int Id { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public TeamZoneShape Shape { get; }
        public float HalfX { get; }
        public float HalfZ { get; }
        public float YawRad { get; }

        public bool Contains(float px, float pz)
        {
            float dx = px - X, dz = pz - Z;
            if (Shape == TeamZoneShape.Circle)
                return dx * dx + dz * dz <= Radius * Radius;
            // Kutuyu yerel eksene çevir (yaw: +Z'den saat yönü karşıtı değil, düz döndürme).
            float c = (float)Math.Cos(YawRad), s = (float)Math.Sin(YawRad);
            float lx = dx * c + dz * s;
            float lz = -dx * s + dz * c;
            return Math.Abs(lx) <= HalfX && Math.Abs(lz) <= HalfZ;
        }

        public float DistanceTo(float px, float pz)
        {
            float dx = px - X, dz = pz - Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }
}
