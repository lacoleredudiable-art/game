using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// T1: her rün gövdeye emirdir — prosedürel squash/stretch (sanat varlığı yok).
    /// </summary>
    public sealed class ActorPose : MonoBehaviour
    {
        Vector3 _baseScale;
        float _poseUntilWorldMs;
        Vector3 _poseScale = Vector3.one;
        float _recoveryUntilWorldMs;
        bool _ready;

        public void CaptureBase()
        {
            _baseScale = transform.localScale;
            if (_baseScale.sqrMagnitude < 1e-6f)
                _baseScale = Vector3.one;
            _ready = true;
        }

        public void PulseRune(Dovus.Core.Grammar.Rune rune, double worldTimeMs)
        {
            if (!_ready)
                CaptureBase();

            _poseScale = rune switch
            {
                Dovus.Core.Grammar.Rune.Igne => new Vector3(0.78f, 0.88f, 1.35f),
                Dovus.Core.Grammar.Rune.Suru => new Vector3(1.35f, 0.9f, 1.1f),
                Dovus.Core.Grammar.Rune.Sarsinti => new Vector3(1.2f, 0.55f, 1.2f),
                Dovus.Core.Grammar.Rune.Kabuk => new Vector3(1.15f, 1.05f, 1.15f),
                Dovus.Core.Grammar.Rune.Zehir => new Vector3(1.05f, 0.95f, 1.25f),
                _ => Vector3.one
            };
            _poseUntilWorldMs = (float)worldTimeMs + 180f;
        }

        public void BeginRecovery(float durationSec, double worldTimeMs)
        {
            if (!_ready)
                CaptureBase();
            _recoveryUntilWorldMs = (float)(worldTimeMs + durationSec * 1000.0);
            _poseScale = new Vector3(1.08f, 0.82f, 1.08f);
            _poseUntilWorldMs = _recoveryUntilWorldMs;
        }

        public void Tick(double worldTimeMs)
        {
            if (!_ready)
                return;

            float now = (float)worldTimeMs;
            if (now < _recoveryUntilWorldMs)
            {
                float breath = 0.04f * Mathf.Sin(now * 0.02f);
                transform.localScale = Vector3.Scale(
                    _baseScale,
                    new Vector3(1.05f + breath, 0.88f - breath, 1.05f + breath));
                return;
            }

            if (now < _poseUntilWorldMs)
            {
                float u = (_poseUntilWorldMs - now) / 180f;
                Vector3 s = Vector3.Lerp(Vector3.one, _poseScale, Mathf.Clamp01(u));
                transform.localScale = Vector3.Scale(_baseScale, s);
                return;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 0.25f);
        }
    }
}
