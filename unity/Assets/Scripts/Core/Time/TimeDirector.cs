using System;

namespace Dovus.Core.Time
{
    /// <summary>
    /// Dünya (ölçeklenmiş) ve gerçek (ölçeklenmemiş) saati birlikte yönetir. Hitstop —
    /// dovus-sistemi.md §7. Yavaş çekim 30 Ağustos 2026'da kaldırıldı: co-op'ta paylaşılan
    /// dünya saatini tek oyuncunun dodge'una göre yavaşlatmak senkron problemi doğuruyordu
    /// (bkz. durum.md). Hitstop kısa ve yereldir, aynı sorunu taşımaz.
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
        /// Kısa süre ölçeği ~0'a çeker. Üst üste gelirse süreler toplanır.
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
                _timeScale = _hitstopRemainingMs > 0 ? 0f : 1f;
                scaledDt = 0;
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
