using Dovus.Game.Config;
using UnityEngine;

using Dovus.Core.Shared;
namespace Dovus.Game.Actors
{
    /// <summary>
    /// T1: her rün gövdeye emirdir — prosedürel squash/stretch (sanat varlığı yok).
    /// </summary>
    public sealed class ActorPoseView : MonoBehaviour
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
                Dovus.Core.Element.Rune.Attack => Tuning.Visuals.PoseIgne,
                Dovus.Core.Element.Rune.Heal => Tuning.Visuals.PoseSuru,
                Dovus.Core.Element.Rune.Burst => Tuning.Visuals.PoseSarsinti,
                Dovus.Core.Element.Rune.Move => Tuning.Visuals.PoseKabuk,
                Dovus.Core.Element.Rune.Defense => Tuning.Visuals.PoseZehir,
                Dovus.Core.Element.Rune.Control => Tuning.Visuals.PoseKabuk,
                _ => Vector3.one
            };
            _poseUntilWorldMs = (float)worldTimeMs + Tuning.Visuals.ActorPoseDurationMs;
        }

        public void BeginRecovery(float durationSec, double worldTimeMs)
        {
            if (!_ready)
                CaptureBase();
            _recoveryUntilWorldMs = (float)(worldTimeMs + durationSec * Units.SecToMs);
            _poseScale = new Vector3(ActorPoseViewDefaults.RecoverySquashHorizontal, ActorPoseViewDefaults.RecoverySquashVertical, ActorPoseViewDefaults.RecoverySquashHorizontal);
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
                float breath = ActorPoseViewDefaults.RecoveryBreathAmplitude * Mathf.Sin(now * ActorPoseViewDefaults.RecoveryBreathFrequency);
                transform.localScale = Vector3.Scale(
                    _baseScale,
                    new Vector3(ActorPoseViewDefaults.RecoveryBreathScaleHorizontal + breath, ActorPoseViewDefaults.RecoveryBreathScaleVertical - breath, ActorPoseViewDefaults.RecoveryBreathScaleHorizontal + breath));
                return;
            }

            if (now < _poseUntilWorldMs)
            {
                float u = (_poseUntilWorldMs - now) / Tuning.Visuals.ActorPoseDurationMs;
                Vector3 s = Vector3.Lerp(Vector3.one, _poseScale, Mathf.Clamp01(u));
                transform.localScale = Vector3.Scale(_baseScale, s);
                return;
            }

            transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, ActorPoseViewDefaults.BaseScaleRelaxLerp);
        }
    }
}
