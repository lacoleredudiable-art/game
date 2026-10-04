using System;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// K1: boss ölüm → çöküş → diriliş zamanlaması (saf). <see cref="BossVitals.Died"/> hangi hasar
    /// yolundan gelirse gelsin <see cref="Begin"/> çağrılır; <see cref="TryRevive"/> süre dolunca bir kez true.
    /// İkinci bir Begin bekleyen dirilişi uzatmaz (aynı ölüm).
    /// </summary>
    public sealed class BossDeathSchedule
    {
        double _reviveAtMs;

        public bool Pending { get; private set; }
        public double ReviveAtMs => _reviveAtMs;

        /// <summary>Yeni ölüm başladıysa true; zaten bekliyorsa false.</summary>
        public bool Begin(double nowMs, double collapseSec)
        {
            if (Pending)
                return false;
            Pending = true;
            _reviveAtMs = nowMs + Math.Max(0.0, collapseSec) * BossDefaults.SecToMs;
            return true;
        }

        /// <summary>Diriliş zamanı geldiyse bekleyeni kapatır ve true döner.</summary>
        public bool TryRevive(double nowMs)
        {
            if (!Pending || nowMs < _reviveAtMs)
                return false;
            Pending = false;
            return true;
        }

        public void Cancel() => Pending = false;
    }
}
