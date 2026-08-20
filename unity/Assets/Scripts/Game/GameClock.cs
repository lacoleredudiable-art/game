using Dovus.Core.Time;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Core TimeDirector'ı Unity kare döngüsüne bağlar. Simülasyon ölçeklenmiş dt ile ilerler.
    /// </summary>
    public sealed class GameClock : MonoBehaviour
    {
        readonly TimeDirector _director = new();

        public TimeDirector Director => _director;

        public double WorldDeltaMs { get; private set; }

        public double RealDeltaMs { get; private set; }

        void Update()
        {
            RealDeltaMs = Time.unscaledDeltaTime * 1000.0;
            WorldDeltaMs = _director.Tick(RealDeltaMs);
        }
    }
}
