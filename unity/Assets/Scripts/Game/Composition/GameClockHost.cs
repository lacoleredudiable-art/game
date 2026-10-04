using Dovus.App.Time;
using Dovus.Core.Shared;
using Dovus.Core.Time;
using UnityEngine;

namespace Dovus.Game.Composition
{
    /// <summary>
    /// Core TimeDirector'ı Unity kare döngüsüne bağlar. Simülasyon ölçeklenmiş dt ile ilerler.
    /// Saat, kendisini okuyan her davranıştan önce ilerlemeli — sıra bu yüzden sabitlendi.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class GameClockHost : MonoBehaviour
    {
        TimeDirector _director;
        TimeDirectorClock _worldClock;

        public TimeDirector Director => _director ??= new TimeDirector();

        public IClock World
        {
            get
            {
                if (_worldClock == null)
                    _worldClock = new TimeDirectorClock(Director);
                return _worldClock;
            }
        }

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
            RealDeltaMs = FrameDelta.ClampMs(Time.unscaledDeltaTime * CompositionTimeDefaults.SecToMs);
            double scale = SimulationScale < 0f ? 0.0 : SimulationScale;
            WorldDeltaMs = Paused ? 0.0 : Director.Tick(RealDeltaMs * scale);
            if (_worldClock == null)
                _worldClock = new TimeDirectorClock(Director);
            _worldClock.Advance(WorldDeltaMs);
        }
    }
}
