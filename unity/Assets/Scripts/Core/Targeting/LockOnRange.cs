using System;

namespace Dovus.Core.Targeting
{
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
