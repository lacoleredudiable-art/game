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

        Vector3 _velocity;
        Vector3 _shakeOffset;
        float _shakeAmplitude;
        float _shakeDurationSec;
        float _shakeElapsedSec;

        public Transform Target
        {
            get => _target;
            set => _target = value;
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

            Vector3 targetVelocity = Vector3.zero;
            var motor = _target.GetComponent<KinematicMotor>();
            if (motor != null)
                targetVelocity = motor.Velocity;

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
            transform.rotation = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
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
