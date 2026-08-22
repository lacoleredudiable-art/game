using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Yumuşak takip + hafif önden bakış. T8 sarsıntı/yumruk için AddShake API'si.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] PrototypeTuning _tuning = new();

        KinematicMotor _targetMotor;
        Camera _cam;
        Vector3 _velocity;
        Vector3 _shakeOffset;
        float _shakeAmplitude;
        float _shakeDurationSec;
        float _shakeElapsedSec;
        float _baseFov = 60f;
        float _fovKick;
        float _rollDeg;
        float _punchT;
        float _punchDecay = 6f;

        public Transform Target
        {
            get => _target;
            set
            {
                _target = value;
                _targetMotor = _target != null ? _target.GetComponent<KinematicMotor>() : null;
            }
        }

        public PrototypeTuning Tuning
        {
            get
            {
                _tuning ??= new PrototypeTuning();
                return _tuning;
            }
            set => _tuning = value;
        }

        void Awake()
        {
            _tuning ??= new PrototypeTuning();
            _cam = GetComponent<Camera>();
            if (_cam != null)
                _baseFov = _cam.fieldOfView;
            if (_targetMotor == null && _target != null)
                _targetMotor = _target.GetComponent<KinematicMotor>();
        }

        /// <summary>
        /// Sıyırma/vurulma yumruğu: FOV sıçraması, kısa roll, sarsıntı. Animasyon
        /// ölçeklenmemiş saatle söner — dünya yavaşken bile keskin (§8).
        /// </summary>
        public void Punch(float fovKick, float rollDeg, float shakePx, float decay)
        {
            _fovKick = fovKick;
            _rollDeg = rollDeg;
            _punchT = 1f;
            _punchDecay = Mathf.Max(0.5f, decay);
            float duration = 2f / _punchDecay;
            AddShake(shakePx * 0.01f, duration);
        }

        public void AddShake(float amplitudeM, float durationSec)
        {
            if (durationSec <= 0f)
                return;

            _shakeAmplitude = amplitudeM;
            _shakeDurationSec = durationSec;
            _shakeElapsedSec = 0f;
        }

        void LateUpdate()
        {
            if (_target == null)
                return;

            AdvanceShake();

            Vector3 targetVelocity = _targetMotor != null ? _targetMotor.Velocity : Vector3.zero;

            Vector3 flatVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            Vector3 lookAhead = flatVelocity.sqrMagnitude > 0.0001f
                ? flatVelocity.normalized * _tuning.LookAheadM
                : Vector3.zero;
            Vector3 desired = _target.position + _tuning.CameraOffset + lookAhead + _shakeOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _velocity,
                _tuning.FollowSmoothTimeSec);

            Vector3 lookTarget = _target.position + lookAhead * 0.35f + Vector3.up * 1.2f;
            Quaternion look = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);

            AdvancePunch();
            transform.rotation = look * Quaternion.Euler(0f, 0f, _rollDeg * _punchT);
            if (_cam != null)
                _cam.fieldOfView = _baseFov * (1f - _fovKick * _punchT);
        }

        void AdvancePunch()
        {
            if (_punchT <= 0f)
            {
                _punchT = 0f;
                return;
            }

            _punchT = Mathf.Max(0f, _punchT - Time.unscaledDeltaTime * _punchDecay);
        }

        void AdvanceShake()
        {
            if (_shakeDurationSec <= 0f)
            {
                _shakeOffset = Vector3.zero;
                return;
            }

            _shakeElapsedSec += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_shakeElapsedSec / _shakeDurationSec);
            float falloff = 1f - t;
            float angle = _shakeElapsedSec * 42f;
            _shakeOffset = new Vector3(
                Mathf.Sin(angle) * _shakeAmplitude * falloff,
                Mathf.Cos(angle * 1.3f) * _shakeAmplitude * 0.35f * falloff,
                0f);

            if (_shakeElapsedSec >= _shakeDurationSec)
            {
                _shakeDurationSec = 0f;
                _shakeOffset = Vector3.zero;
            }
        }
    }
}
