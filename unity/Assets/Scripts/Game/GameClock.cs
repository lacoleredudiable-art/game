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

        /// <summary>
        /// Play Sweep dünya hızı. 1 iken saat duvar zamanıdır; dövüş <c>Time.timeScale</c> yazmaz.
        /// Tarama bunu timeScale ile aynı değere çeker. Fizik adımı (fixedDeltaTime) ayrı kalır.
        /// </summary>
        public float SimulationScale { get; set; } = 1f;

        void Update()
        {
            RealDeltaMs = Time.unscaledDeltaTime * 1000.0;
            double scale = SimulationScale < 0f ? 0.0 : SimulationScale;
            WorldDeltaMs = Paused ? 0.0 : Director.Tick(RealDeltaMs * scale);
        }
    }
}
