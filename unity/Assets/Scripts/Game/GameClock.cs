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

        public void Bind(Dovus.Core.Tuning.SlowmoTuning tuning)
        {
            _director = new TimeDirector(tuning);
        }

        public double WorldDeltaMs { get; private set; }

        public double RealDeltaMs { get; private set; }

        void Update()
        {
            RealDeltaMs = Time.unscaledDeltaTime * 1000.0;
            WorldDeltaMs = Director.Tick(RealDeltaMs);
        }
    }
}
