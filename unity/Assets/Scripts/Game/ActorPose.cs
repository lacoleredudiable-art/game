using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// T1: her rün gövdeye emirdir — prosedürel squash/stretch (sanat varlığı yok).
    /// </summary>
    public sealed class ActorPose : MonoBehaviour
    {
        [SerializeField] PrototypeTuning _tuning = new();

        Vector3 _baseScale;
        float _poseUntilWorldMs;
        Vector3 _poseScale = Vector3.one;
        float _recoveryUntilWorldMs;
        bool _ready;

        public PrototypeTuning Tuning
        {
            get
            {
                _tuning ??= new PrototypeTuning();
                return _tuning;
            }
            set => _tuning = value;
        }

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
                Dovus.Core.Grammar.Rune.Ates => Tuning.PoseIgne,
                Dovus.Core.Grammar.Rune.Su => Tuning.PoseSuru,
                Dovus.Core.Grammar.Rune.Aydinlik => Tuning.PoseSarsinti,
                Dovus.Core.Grammar.Rune.Hava => Tuning.PoseKabuk,
                Dovus.Core.Grammar.Rune.Toprak => Tuning.PoseZehir,
                Dovus.Core.Grammar.Rune.Karanlik => Tuning.PoseKabuk,
                _ => Vector3.one
            };
            _poseUntilWorldMs = (float)worldTimeMs + Tuning.ActorPoseDurationMs;
        }

        public void BeginRecovery(float durationSec, double worldTimeMs)
        {
            if (!_ready)
                CaptureBase();
            _recoveryUntilWorldMs = (float)(worldTimeMs + durationSec * 1000.0);
            _poseScale = new Vector3(1.08f, 0.82f, 1.08f);
            _poseUntilWorldMs = _recoveryUntilWorldMs;
        }

        /// <summary>
        /// Toparlanma kilidi kesildi (§5: düz vuruş / yeni fiil / dodge). Nefes nefese poz
        /// kilitle birlikte biter, yoksa oyuncu yeni cümleyi çizerken hâlâ toparlanıyor görünür.
        /// </summary>
        public void EndRecovery()
        {
            _recoveryUntilWorldMs = 0f;
            _poseUntilWorldMs = 0f;
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
                float u = (_poseUntilWorldMs - now) / Tuning.ActorPoseDurationMs;
                Vector3 s = Vector3.Lerp(Vector3.one, _poseScale, Mathf.Clamp01(u));
                transform.localScale = Vector3.Scale(_baseScale, s);
                return;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 0.25f);
        }
    }
}
