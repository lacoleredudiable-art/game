using System;

namespace Dovus.Core.Time
{
    /// <summary>
    /// Dünya (ölçeklenmiş) ve gerçek (ölçeklenmemiş) saati birlikte yönetir.
    /// Hitstop sırasında dünya zamanı durur; gerçek zaman ilerlemeye devam eder.
    /// </summary>
    public sealed class TimeDirector
    {
        double _realTimeMs;
        double _worldTimeMs;
        float _timeScale = 1f;
        double _hitstopRemainingMs;

        public double RealTimeMs => _realTimeMs;

        public double WorldTimeMs => _worldTimeMs;

        /// <summary>Anlık zaman ölçeği. Hitstop sırasında 0, aksi halde 1.</summary>
        public float TimeScale => _timeScale;

        public bool IsHitstopActive => _hitstopRemainingMs > 0;

        /// <summary>
        /// Kısa süre dünya zamanını durdurur. Üst üste gelirse süreler toplanır.
        /// </summary>
        public void TriggerHitstop(int durationMs)
        {
            if (durationMs <= 0)
                return;

            _hitstopRemainingMs += durationMs;
            _timeScale = 0f;
        }

        /// <summary>Gerçek delta ile ilerler; ölçeklenmiş dünya deltasını döner.</summary>
        public double Tick(double realDtMs)
        {
            if (realDtMs < 0)
                realDtMs = 0;

            _realTimeMs += realDtMs;

            double scaledDt;
            if (_hitstopRemainingMs > 0)
            {
                _hitstopRemainingMs = Math.Max(0, _hitstopRemainingMs - realDtMs);
                _timeScale = 0f;
                scaledDt = 0;

                if (_hitstopRemainingMs <= 0)
                    _timeScale = 1f;
            }
            else
            {
                _timeScale = 1f;
                scaledDt = realDtMs;
            }

            _worldTimeMs += scaledDt;
            return scaledDt;
        }
    }
}
