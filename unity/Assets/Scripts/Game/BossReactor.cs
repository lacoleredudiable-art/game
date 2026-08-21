using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Boss fiziksel tepkisi — geri tepme, sarsılma, kaldırma. Can/hasar T8.
    /// </summary>
    public sealed class BossReactor : MonoBehaviour
    {
        Vector3 _home;
        Vector3 _offset;
        float _shakeUntil;
        float _shakeAmp;
        float _liftVel;
        float _baseY;
        bool _captured;

        public void CaptureHome()
        {
            _home = transform.position;
            _baseY = _home.y;
            _captured = true;
        }

        public void React(
            Vector3 fromWorld,
            float knockbackM,
            float liftM,
            float shakeSec,
            double worldTimeMs)
        {
            if (!_captured)
                CaptureHome();

            Vector3 flat = transform.position - fromWorld;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-4f)
                flat = -transform.forward;
            flat.Normalize();

            _offset += flat * knockbackM;
            _liftVel = Mathf.Max(_liftVel, liftM * 4.5f);
            _shakeAmp = Mathf.Max(_shakeAmp, 0.12f + knockbackM * 0.05f);
            _shakeUntil = (float)worldTimeMs + shakeSec * 1000f;
        }

        /// <summary>Kabuk kapanışı: yerinde sabitle (kısa kilit).</summary>
        public void Pin(float durationSec, double worldTimeMs)
        {
            _offset = Vector3.zero;
            _liftVel = 0f;
            _shakeAmp = 0.04f;
            _shakeUntil = (float)worldTimeMs + durationSec * 1000f;
        }

        public void Tick(float dtSec, double worldTimeMs)
        {
            if (!_captured)
                CaptureHome();

            float now = (float)worldTimeMs;
            Vector3 shake = Vector3.zero;
            if (now < _shakeUntil)
            {
                float t = (_shakeUntil - now) / 1000f;
                shake = new Vector3(
                    (Mathf.PerlinNoise(now * 0.05f, 0.1f) - 0.5f) * 2f,
                    0f,
                    (Mathf.PerlinNoise(0.3f, now * 0.05f) - 0.5f) * 2f) * (_shakeAmp * t);
            }
            else
            {
                _shakeAmp = 0f;
            }

            // Kaldırma + yerçekimi
            float y = transform.position.y;
            y += _liftVel * dtSec;
            _liftVel -= 22f * dtSec;
            if (y <= _baseY)
            {
                y = _baseY;
                _liftVel = 0f;
            }

            _offset = Vector3.Lerp(_offset, Vector3.zero, 1f - Mathf.Exp(-3.2f * dtSec));
            Vector3 p = _home + _offset + shake;
            p.y = y;
            transform.position = p;
        }
    }
}
