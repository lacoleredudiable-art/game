using System;

namespace Dovus.Core.Targeting
{
    /// <summary>
    /// Kilit alma mesafesi (silah menzili). Bırakma mesafesi yoktur: alınan kilit menzilden bağımsız sürer,
    /// yalnız hedef ölünce/kaybolunca düşer (G-10, Mustafa 16 Ekim: A).
    /// </summary>
    public readonly struct LockOnRange
    {
        public LockOnRange(float acquireM)
        {
            AcquireM = Math.Max(0f, acquireM);
        }

        public float AcquireM { get; }
    }
}
