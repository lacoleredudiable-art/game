using Dovus.Core.Time;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Core TimeDirector'ı Unity kare döngüsüne bağlar. Simülasyon ölçeklenmiş dt ile ilerler.
    /// Saat, kendisini okuyan her davranıştan önce ilerlemeli — sıra bu yüzden sabitlendi.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameClock : MonoBehaviour
    {
        TimeDirector _director;

        public TimeDirector Director => _director ??= new TimeDirector();

        public double WorldDeltaMs { get; private set; }

        public double RealDeltaMs { get; private set; }

        /// <summary>Menü (build seçimi) açıkken dünya saati durur; gerçek saat UI için akar.</summary>
        public bool Paused { get; set; }

        void Update()
        {
            RealDeltaMs = Time.unscaledDeltaTime * 1000.0;
            WorldDeltaMs = Paused ? 0.0 : Director.Tick(RealDeltaMs);
        }
    }
}
