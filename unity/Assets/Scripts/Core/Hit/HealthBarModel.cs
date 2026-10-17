using System;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// Can barı verisi (çizim Unity'de). Ön şerit anlık orandır; hayalet şerit son vuruşun önceki oranında
    /// <see cref="HitDefaults.TrailHoldMs"/> bekler, sonra ön şeride doğru erir. İyileşmede hayalet şerit ön şeride yapışır.
    /// </summary>
    public sealed class HealthBarModel
    {
        float _ratio = 1f;
        float _trail = 1f;
        double _holdUntilMs;
        double _lastMs;
        bool _started;

        public float Ratio => _ratio;
        public float TrailRatio => _trail;

        public void Reset(float ratio)
        {
            _ratio = Clamp01(ratio);
            _trail = _ratio;
            _started = false;
        }

        /// <summary>Her karede çağrılır. Oran artmışsa hayalet şerit hemen yapışır, azalmışsa bekleme başlar.</summary>
        public void Update(float hp, float maxHp, double nowMs)
        {
            float next = maxHp > 0f ? Clamp01(hp / maxHp) : 0f;
            float dtSec = _started ? (float)Math.Max(0d, (nowMs - _lastMs) / 1000d) : 0f;
            _started = true;
            _lastMs = nowMs;

            if (next < _ratio)
            {
                if (_trail <= _ratio)
                    _trail = _ratio;
                _holdUntilMs = nowMs + HitDefaults.TrailHoldMs;
            }
            else if (next > _ratio)
            {
                _trail = Math.Max(_trail, next);
            }

            _ratio = next;
            if (_trail > _ratio && nowMs >= _holdUntilMs)
                _trail = Math.Max(_ratio, _trail - HitDefaults.TrailDrainRatioPerSec * dtSec);
            else if (_trail < _ratio)
                _trail = _ratio;
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
