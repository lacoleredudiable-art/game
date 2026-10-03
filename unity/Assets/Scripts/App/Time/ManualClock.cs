using Dovus.Core.Shared;

namespace Dovus.App.Time
{
    /// <summary>Testler için elle sürülen saat.</summary>
    public sealed class ManualClock : IClock
    {
        double _nowMs;
        double _deltaMs;
        float _deltaSec;

        public double NowMs => _nowMs;

        public double DeltaMs => _deltaMs;

        public float DeltaSec => _deltaSec;

        public void Set(double nowMs, double deltaMs, float deltaSec)
        {
            _nowMs = nowMs;
            _deltaMs = deltaMs;
            _deltaSec = deltaSec;
        }

        public void Advance(double deltaMs, float deltaSec)
        {
            if (deltaMs < 0.0)
                deltaMs = 0.0;
            _deltaMs = deltaMs;
            _deltaSec = deltaSec;
            _nowMs += deltaMs;
        }
    }
}
