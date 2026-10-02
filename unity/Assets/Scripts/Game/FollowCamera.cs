using Dovus.Core.Combat;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Yumuşak takip + hafif önden bakış. T8 sarsıntı/yumruk için AddShake API'si.
    /// </summary>
    public sealed class FollowCamera : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] Transform _bossTarget;
        [SerializeField] PrototypeTuning _tuning = new();

        KinematicMotor _targetMotor;
        Camera _cam;
        BossDirector _bossDirector;
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
        float _resolvedYawDeg;
        float _yawVelocity;
        float _resolvedDistanceM;
        float _distanceVelocity;
        float _windupPullback;
        float _windupVelocity;
        bool _windupHooked;
        Quaternion _aimRotation;

        /// <summary>
        /// 16 Eylül: "kamera sabit, döndüremiyorum" bug raporu. Oyuncu etrafında yatay dönüş —
        /// CombatFeel/§8 tuning'e dokunmadan; 0 iken davranış birebir eskisiyle aynı (T8/T8.1
        /// tuning'i bozmuyoruz). <see cref="CameraOrbitInput"/> tarafından sürülür.
        /// </summary>
        public float OrbitYawDeg { get; set; }
        /// <summary>Omuz ofsetinin yatay eksende eğimi (+ = kamera yükselir, aşağı bakar). 0 = eski kadraj.</summary>
        public float OrbitPitchDeg { get; set; }
        /// <summary>Kamera soft-lock'u uygulandıktan sonraki yaw; kamera-göreli hareket bunu kullanır.</summary>
        public float MovementYawDeg => _resolvedYawDeg;

        public Transform BossTarget
        {
            get => _bossTarget;
            set => _bossTarget = value;
        }

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
            {
                _baseFov = _cam.fieldOfView;
                _aimRotation = transform.rotation;
            }
            _resolvedYawDeg = OrbitYawDeg;
            _resolvedDistanceM = _tuning.CameraDistanceM;
            if (_targetMotor == null && _target != null)
                _targetMotor = _target.GetComponent<KinematicMotor>();
        }

        void OnDisable() => UnhookBossDirector();

        /// <summary>Boss windup telegrafı — yalnız sunum; boss zamanlamasına dokunulmaz.</summary>
        public void BindBossDirector(BossDirector director)
        {
            if (_bossDirector == director)
                return;
            UnhookBossDirector();
            _bossDirector = director;
            if (_bossDirector != null)
            {
                _bossDirector.AttackWindupStarted += OnBossWindupStarted;
                _windupHooked = true;
            }
        }

        void UnhookBossDirector()
        {
            if (_windupHooked && _bossDirector != null)
                _bossDirector.AttackWindupStarted -= OnBossWindupStarted;
            _windupHooked = false;
            _bossDirector = null;
        }

        void OnBossWindupStarted(BossAttackKind kind)
        {
            if (!IsBigWindupTelegraph(kind))
                return;
            _windupPullback = 1f;
        }

        bool IsBigWindupTelegraph(BossAttackKind kind)
        {
            if (kind == BossAttackKind.Slam)
                return true;
            if (_bossDirector == null)
                return kind == BossAttackKind.FireCone;
            return kind == BossAttackKind.FireCone
                || _bossDirector.AttackRadiusM >= _tuning.CameraWindupMinRadiusM;
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
            // §8 sarsıntıyı PİKSEL veriyor, kamera METRE ile sarsılıyor; dönüşüm spec'te yok (T8.1).
            float pxToM = _tuning != null ? _tuning.CameraShakePxToM : 0.01f;
            AddShake(shakePx * pxToM, duration);
        }

        /// <summary>Süren daha güçlü bir sarsıntıyı (ör. skill kick) ezmeden piksel sarsıntı ekler.</summary>
        public void AddShakePxAtLeast(float shakePx, float decay)
        {
            float pxToM = _tuning != null ? _tuning.CameraShakePxToM : 0.01f;
            float amp = shakePx * pxToM;
            float remaining01 = _shakeDurationSec > 0f ? 1f - Mathf.Clamp01(_shakeElapsedSec / _shakeDurationSec) : 0f;
            if (_shakeAmplitude * remaining01 >= amp)
                return;
            AddShake(amp, 2f / Mathf.Max(0.5f, decay));
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
            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            ResolveYaw(dt);
            UpdateWindupPullback(dt);

            float framing = BossFramingWeight();
            float desiredDistance = ResolveCameraDistance(framing);
            _resolvedDistanceM = Mathf.SmoothDamp(
                _resolvedDistanceM,
                desiredDistance,
                ref _distanceVelocity,
                Mathf.Max(0.01f, _tuning.CameraLockOnDistanceSmoothSec),
                Mathf.Infinity,
                dt);

            Vector3 shoulder = _tuning.CameraShoulderOffset;
            shoulder.y += _windupPullback * _tuning.CameraWindupExtraHeightM;
            Vector3 localOffset = shoulder + Vector3.back * _resolvedDistanceM;
            Vector3 offset = Quaternion.Euler(OrbitPitchDeg, _resolvedYawDeg, 0f) * localOffset;
            Vector3 desired = _target.position + offset + lookAhead + _shakeOffset;

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _velocity,
                _tuning.FollowSmoothTimeSec,
                Mathf.Infinity,
                dt);

            Vector3 playerAim = _target.position + lookAhead * 0.35f
                + Vector3.up * _tuning.CameraLookHeightM;
            Vector3 lookTarget = playerAim;
            if (framing > 0f && _bossTarget != null)
            {
                Vector3 bossAim = _bossTarget.position + Vector3.up * _tuning.CameraBossAimHeightM;
                float blend = Mathf.Clamp01(_tuning.CameraBossFramingWeight) * framing;
                lookTarget = Vector3.Lerp(playerAim, bossAim, blend);
            }

            Quaternion look = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            float aimBlend = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, _tuning.CameraAimDampingSec));
            _aimRotation = Quaternion.Slerp(_aimRotation, look, aimBlend);

            AdvancePunch();
            transform.rotation = _aimRotation * Quaternion.Euler(0f, 0f, _rollDeg * _punchT);
            if (_cam != null)
            {
                _baseFov = Mathf.Clamp(_tuning.CameraFovDeg, 35f, 85f);
                _cam.fieldOfView = _baseFov * (1f - _fovKick * _punchT);
            }
        }

        float ResolveCameraDistance(float framingWeight)
        {
            float distance = _tuning.CameraDistanceM;
            if (framingWeight > 0f && _bossTarget != null)
            {
                Vector3 toBoss = _bossTarget.position - _target.position;
                toBoss.y = 0f;
                float extra = Mathf.Min(
                    toBoss.magnitude * _tuning.CameraLockOnDistancePerSepM,
                    _tuning.CameraLockOnMaxExtraDistanceM);
                distance = Mathf.Clamp(
                    distance + extra * framingWeight,
                    _tuning.CameraLockOnMinDistanceM,
                    _tuning.CameraLockOnMaxDistanceM);
            }

            float windupMul = Mathf.Lerp(1f, _tuning.CameraWindupDistanceMul, _windupPullback);
            return distance * windupMul;
        }

        void UpdateWindupPullback(float dt)
        {
            float target = 0f;
            if (_bossDirector != null && _bossDirector.WindupProgress01 > 0f
                && _bossDirector.CurrentAttackKind.HasValue
                && IsBigWindupTelegraph(_bossDirector.CurrentAttackKind.Value))
            {
                target = 1f;
            }

            _windupPullback = Mathf.SmoothDamp(
                _windupPullback,
                target,
                ref _windupVelocity,
                Mathf.Max(0.01f, _tuning.CameraWindupSmoothSec),
                Mathf.Infinity,
                dt);
        }

        void ResolveYaw(float dt)
        {
            float desired = OrbitYawDeg;
            if (_bossTarget != null)
            {
                Vector3 toBoss = _bossTarget.position - _target.position;
                toBoss.y = 0f;
                float range = Mathf.Max(0.01f, _tuning.CameraSoftLockRangeM);
                if (toBoss.sqrMagnitude <= range * range && toBoss.sqrMagnitude > 0.001f)
                {
                    float bossYaw = Mathf.Atan2(toBoss.x, toBoss.z) * Mathf.Rad2Deg;
                    desired = Mathf.LerpAngle(
                        OrbitYawDeg,
                        bossYaw,
                        Mathf.Clamp01(_tuning.CameraSoftLockStrength));
                }
            }

            _resolvedYawDeg = Mathf.SmoothDampAngle(
                _resolvedYawDeg,
                desired,
                ref _yawVelocity,
                Mathf.Max(0.01f, _tuning.FollowSmoothTimeSec),
                Mathf.Infinity,
                dt);
        }

        float BossFramingWeight()
        {
            if (_bossTarget == null)
                return 0f;
            Vector3 toBoss = _bossTarget.position - _target.position;
            toBoss.y = 0f;
            float range = Mathf.Max(0.01f, _tuning.CameraSoftLockRangeM);
            float distanceWeight = 1f - Mathf.SmoothStep(0.72f, 1f, toBoss.magnitude / range);
            return Mathf.Clamp01(distanceWeight);
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
