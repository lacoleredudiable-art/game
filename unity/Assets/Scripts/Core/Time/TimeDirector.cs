using System;

namespace Dovus.Core.Time
{
    /// <summary>
    /// Dünya (ölçeklenmiş) ve gerçek (ölçeklenmemiş) saati birlikte yönetir.
    /// Hitstop sırasında dünya zamanı durur; gerçek zaman ilerlemeye devam eder.
    /// </summary>
    public sealed class TimeDirector
    {
        double _worldTimeMs;
        double _hitstopRemainingMs;

        public double WorldTimeMs => _worldTimeMs;

        /// <summary>
        /// Kısa süre dünya zamanını durdurur. Üst üste gelirse süreler toplanır.
        /// Oyun kodu çağırmaz (MP: dünya donması yok).
        /// </summary>
        public void TriggerHitstop(int durationMs)
        {
            if (durationMs <= 0)
                return;

            _hitstopRemainingMs += durationMs;
        }

        /// <summary>Gerçek delta ile ilerler; ölçeklenmiş dünya deltasını döner.</summary>
        public double Tick(double realDtMs)
        {
            if (realDtMs < 0)
                realDtMs = 0;

            double scaledDt;
            if (_hitstopRemainingMs > 0)
            {
                _hitstopRemainingMs = Math.Max(0, _hitstopRemainingMs - realDtMs);
                scaledDt = 0;
            }
            else
            {
                scaledDt = realDtMs;
            }

            _worldTimeMs += scaledDt;
            return scaledDt;
        }
    }
}
