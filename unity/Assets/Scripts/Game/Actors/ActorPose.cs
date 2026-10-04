using Dovus.Game.Config;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// T1: her rün gövdeye emirdir — prosedürel squash/stretch (sanat varlığı yok).
    /// </summary>
    public sealed class ActorPose : MonoBehaviour
    {
        [SerializeField] GameTuning _tuning = new();

        Vector3 _baseScale;
        float _poseUntilWorldMs;
        Vector3 _poseScale = Vector3.one;
        float _recoveryUntilWorldMs;
        bool _ready;

        public GameTuning Tuning
        {
            get
            {
                _tuning ??= new GameTuning();
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

        public void PulseRune(Dovus.Core.Element.Rune rune, double worldTimeMs)
        {
            if (!_ready)
                CaptureBase();

            _poseScale = rune switch
            {
                Dovus.Core.Element.Rune.Ates => Tuning.Visuals.PoseIgne,
                Dovus.Core.Element.Rune.Su => Tuning.Visuals.PoseSuru,
                Dovus.Core.Element.Rune.Aydinlik => Tuning.Visuals.PoseSarsinti,
                Dovus.Core.Element.Rune.Hava => Tuning.Visuals.PoseKabuk,
                Dovus.Core.Element.Rune.Toprak => Tuning.Visuals.PoseZehir,
                Dovus.Core.Element.Rune.Karanlik => Tuning.Visuals.PoseKabuk,
                _ => Vector3.one
            };
            _poseUntilWorldMs = (float)worldTimeMs + Tuning.Visuals.ActorPoseDurationMs;
        }

        public void BeginRecovery(float durationSec, double worldTimeMs)
        {
            if (!_ready)
                CaptureBase();
            _recoveryUntilWorldMs = (float)(worldTimeMs + durationSec * ActorsTimeDefaults.SecToMs);
            _poseScale = new Vector3(ActorPoseDefaults.RecoverySquashHorizontal, ActorPoseDefaults.RecoverySquashVertical, ActorPoseDefaults.RecoverySquashHorizontal);
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
                float breath = ActorPoseDefaults.RecoveryBreathAmplitude * Mathf.Sin(now * ActorPoseDefaults.RecoveryBreathFrequency);
                transform.localScale = Vector3.Scale(
                    _baseScale,
                    new Vector3(ActorPoseDefaults.RecoveryBreathScaleHorizontal + breath, ActorPoseDefaults.RecoveryBreathScaleVertical - breath, ActorPoseDefaults.RecoveryBreathScaleHorizontal + breath));
                return;
            }

            if (now < _poseUntilWorldMs)
            {
                float u = (_poseUntilWorldMs - now) / Tuning.Visuals.ActorPoseDurationMs;
                Vector3 s = Vector3.Lerp(Vector3.one, _poseScale, Mathf.Clamp01(u));
                transform.localScale = Vector3.Scale(_baseScale, s);
                return;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, ActorPoseDefaults.BaseScaleRelaxLerp);
        }
    }
}
