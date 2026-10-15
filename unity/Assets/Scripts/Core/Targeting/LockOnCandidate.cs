using System;
using System.Collections.Generic;

namespace Dovus.Core.Targeting
{
    /// <summary>Kilitlenebilir sahne adayı (Unity yok). Mesafe ve bakış açısı Game katmanında ölçülür.</summary>
    public readonly struct LockOnCandidate
    {
        static readonly LockPoint[] NoPoints = Array.Empty<LockPoint>();

        public LockOnCandidate(
            int id,
            bool isHostile,
            bool available,
            float distanceM,
            float bearingDeg,
            IReadOnlyList<LockPoint> points = null)
        {
            Id = id;
            IsHostile = isHostile;
            Available = available;
            DistanceM = distanceM;
            BearingDeg = bearingDeg;
            Points = points ?? NoPoints;
        }

        public int Id { get; }
        public bool IsHostile { get; }
        public bool Available { get; }
        public float DistanceM { get; }
        /// <summary>Kameranın baktığı yöne göre işaretli açı (derece); sağ pozitif. Hedef değiştirme sırası buradan gelir.</summary>
        public float BearingDeg { get; }
        public IReadOnlyList<LockPoint> Points { get; }
    }

    /// <summary>Kilit alma ve bırakma mesafeleri; bırakma ≥ alma (aradaki fark titreşimi önler).</summary>
    public readonly struct LockOnRange
    {
        public LockOnRange(float acquireM, float releaseM)
        {
            AcquireM = Math.Max(0f, acquireM);
            ReleaseM = Math.Max(AcquireM, releaseM);
        }

        public float AcquireM { get; }
        public float ReleaseM { get; }
    }
}
