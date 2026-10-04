using Dovus.Core.Shared;
using Dovus.Core.Time;

namespace Dovus.App.Time
{
    /// <summary>Core <see cref="TimeDirector"/> dünya saatini <see cref="IClock"/> olarak sunar.</summary>
    public sealed class TimeDirectorClock : IClock
    {
        readonly TimeDirector _director;
        double _lastDeltaMs;

        public TimeDirectorClock(TimeDirector director) => _director = director;

        public double NowMs => _director.WorldTimeMs;

        public double DeltaMs => _lastDeltaMs;

        public float DeltaSec => _lastDeltaMs > 0.0 ? (float)(_lastDeltaMs / TimeDefaults.SecToMs) : 0f;

        /// <summary><see cref="TimeDirector.Tick"/> sonrası dünya deltasını kaydeder.</summary>
        public void Advance(double worldDeltaMs)
        {
            if (worldDeltaMs < 0.0)
                worldDeltaMs = 0.0;
            _lastDeltaMs = worldDeltaMs;
        }
    }
}
