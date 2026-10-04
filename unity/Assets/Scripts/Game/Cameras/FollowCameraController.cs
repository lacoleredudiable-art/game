using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Config;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using System.Collections.Generic;
using UnityEngine;

namespace Dovus.Game.Cameras
{
    /// <summary>
    /// Yumuşak takip + hafif önden bakış. T8 sarsıntı/yumruk için AddShake API'si.
    /// </summary>
    public sealed partial class FollowCameraController : MonoBehaviour
    {
        [SerializeField] Transform _target;
        [SerializeField] Transform _bossTarget;
        [SerializeField] GameTuning _tuning = new();

        KinematicMotorController _targetMotor;
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
        float _punchDecay = FollowCameraControllerDefaults.PunchDecay;
        float _resolvedYawDeg;
        float _yawVelocity;
        float _resolvedDistanceM;
        float _distanceVelocity;
        float _windupPullback;
        float _windupVelocity;
        bool _windupHooked;
        Quaternion _aimRotation;
        Transform[] _ignoreRoots;
        int _collisionLayerMask;
        bool _collisionFilterLogged;
        float _smoothedAlongDistM;
        float _alongDistVelocity;
        float _collisionPulledInM;
        float _lockOnShoulderSign = 1f;
        float _lastLockOnOverlapPct = 1f;
        static readonly RaycastHit[] CollisionHits = new RaycastHit[8];

        /// <summary>Son karede pivot→istenen mesafe ekseninde scenery çekişi (m).</summary>
        public float CollisionPulledInM => _collisionPulledInM;

        /// <summary>Lock-on: oyuncu ekran dikdörtgeninin boss örtüsüne göre görünür oranı (0–1).</summary>
        public float LockOnPlayerVisibleRatio => _lastLockOnOverlapPct;

        /// <summary>Görsel hitstop sırasında kamera takibini dondur (simülasyon saati değil).</summary>
        public float VisualHoldUntilUnscaled { get; set; }

        /// <summary>
        /// 16 Eylül: "kamera sabit, döndüremiyorum" bug raporu. Oyuncu etrafında yatay dönüş —
        /// CombatFeelDirector/§8 tuning'e dokunmadan; 0 iken davranış birebir eskisiyle aynı (T8/T8.1
        /// tuning'i bozmuyoruz). <see cref="CameraOrbitController"/> tarafından sürülür.
        /// </summary>
        public float OrbitYawDeg { get; set; }
        /// <summary>Omuz ofsetinin yatay eksende eğimi (+ = kamera yükselir, aşağı bakar). 0 = eski kadraj.</summary>
        public float OrbitPitchDeg { get; set; }
        /// <summary>Kamera soft-lock'u uygulandıktan sonraki yaw; kamera-göreli hareket bunu kullanır.</summary>
        public float MovementYawDeg => _resolvedYawDeg;

        /// <summary>Yumuşatılmış geri çekilme mesafesi (doğrulama / tuning paneli).</summary>
        public float ResolvedDistanceM => _resolvedDistanceM;

        /// <summary>0–1 windup geri çekilme karışımı.</summary>
        public float WindupPullback01 => _windupPullback;

        /// <summary>
        /// ff-4: gerçek lock-on durumu. Kapalı: oyuncu merkezli takip, alt üçte bir, boss
        /// menzildeyken yumuşak yaw/çerçeve (<see cref="BossFramingWeight"/>). Açık: bakış
        /// hedefi oyuncu–boss orta noktası (boss baş yüksekliği dahil), mesafe ayrıma göre
        /// büyür (lock-on min/max'e kenetli). Panel satırı, Tab tuşu (bkz. CameraOrbitController) ve
        /// yakalama API'si bunu değiştirir.
        /// </summary>
        public bool LockOnActive { get; set; }

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
                _targetMotor = _target != null ? _target.GetComponent<KinematicMotorController>() : null;
            }
        }

        public GameTuning Tuning
        {
            get
            {
                _tuning ??= new GameTuning();
                return _tuning;
            }
            set => _tuning = value;
        }

        public Camera ViewCamera => _cam;

        void Awake()
        {
            _tuning ??= new GameTuning();
            _cam = GetComponent<Camera>();
            if (_cam != null)
            {
                _baseFov = _cam.fieldOfView;
                _aimRotation = transform.rotation;
            }
            _resolvedYawDeg = OrbitYawDeg;
            _resolvedDistanceM = _tuning.Camera.CameraDistanceM;
            if (_targetMotor == null && _target != null)
                _targetMotor = _target.GetComponent<KinematicMotorController>();
        }

        void OnDisable() => UnhookBossDirector();

        /// <summary>Karakter/VFX kökleri spherecast'ten çıkar; layer mask bind'de kurulur.</summary>
        public void BindCollisionFiltering(Transform playerRoot, Transform bossRoot, Transform allyRoot = null)
        {
            var roots = new List<Transform>(3);
            if (playerRoot != null)
                roots.Add(playerRoot);
            if (bossRoot != null)
                roots.Add(bossRoot);
            if (allyRoot != null)
                roots.Add(allyRoot);
            _ignoreRoots = roots.ToArray();

            int blocker = LayerMask.NameToLayer("CameraBlocker");
            _collisionLayerMask = blocker >= 0
                ? LayerMask.GetMask("Default", "CameraBlocker")
                : LayerMask.GetMask("Default");
            if (!_collisionFilterLogged)
            {
                DebugConfig.DevLog(
                    $"[FollowCamera] collision mask layers={(blocker >= 0 ? "Default+CameraBlocker" : "Default only")} "
                    + $"ignoreRoots={_ignoreRoots.Length} (player/boss/ally, triggers skipped)");
                _collisionFilterLogged = true;
            }
        }

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
                || _bossDirector.AttackRadiusM >= _tuning.Camera.CameraWindupMinRadiusM;
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
            float pxToM = _tuning != null ? _tuning.Hud.CameraShakePxToM : FollowCameraControllerDefaults.FallbackShakePxToM;
            AddShake(shakePx * pxToM, duration);
        }

        /// <summary>Süren daha güçlü bir sarsıntıyı (ör. skill kick) ezmeden piksel sarsıntı ekler.</summary>
        public void AddShakePxAtLeast(float shakePx, float decay)
        {
            float pxToM = _tuning != null ? _tuning.Hud.CameraShakePxToM : FollowCameraControllerDefaults.FallbackShakePxToM;
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

            float now = Time.unscaledTime;
            if (now < VisualHoldUntilUnscaled)
                return;

            Vector3 targetVelocity = _targetMotor != null ? _targetMotor.Velocity : Vector3.zero;

            Vector3 flatVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            Vector3 lookAhead = flatVelocity.sqrMagnitude > 0.0001f
                ? flatVelocity.normalized * _tuning.Camera.LookAheadM
                : Vector3.zero;
            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            ResolveYaw(dt);
            UpdateWindupPullback(dt);

            float framing = BossFramingWeight();
            float desiredDistance = ResolveCameraDistance();
            _resolvedDistanceM = Mathf.SmoothDamp(
                _resolvedDistanceM,
                desiredDistance,
                ref _distanceVelocity,
                Mathf.Max(FollowCameraControllerDefaults.MinPositiveSmoothSec, _tuning.Camera.CameraLockOnDistanceSmoothSec),
                Mathf.Infinity,
                dt);

            Vector3 playerAim = _target.position + lookAhead * FollowCameraControllerDefaults.LookAheadBlend
                + Vector3.up * _tuning.Camera.CameraLookHeightM;

            Vector3 shoulder = _tuning.Camera.CameraShoulderOffset;
            shoulder.y += _windupPullback * _tuning.Camera.CameraWindupExtraHeightM;
            if (LockOnActive)
            {
                _lockOnShoulderSign = ResolveLockOnShoulderSign(playerAim, dt);
                shoulder.x += _lockOnShoulderSign * _tuning.Camera.CameraLockOnShoulderSideM;
            }

            Vector3 localOffset = shoulder + Vector3.back * _resolvedDistanceM;
            Vector3 offset = Quaternion.Euler(OrbitPitchDeg, _resolvedYawDeg, 0f) * localOffset;
            Vector3 desired = _target.position + offset + lookAhead + _shakeOffset;
            desired = ApplyCameraCollision(playerAim, desired, dt);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _velocity,
                _tuning.Camera.FollowSmoothTimeSec,
                Mathf.Infinity,
                dt);

            Vector3 lookTarget = playerAim;
            if (_bossTarget != null)
            {
                Vector3 bossAim = _bossTarget.position + Vector3.up * _tuning.Camera.CameraBossAimHeightM;
                if (LockOnActive)
                {
                    lookTarget = Vector3.Lerp(playerAim, bossAim, _tuning.Camera.CameraLockOnLookBlendToBoss);
                    UpdateLockOnScreenOverlap();
                }
                else if (framing > 0f)
                {
                    float blend = Mathf.Clamp01(_tuning.Camera.CameraBossFramingWeight) * framing;
                    lookTarget = Vector3.Lerp(playerAim, bossAim, blend);
                }
            }

            Quaternion look = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
            float aimBlend = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, _tuning.Camera.CameraAimDampingSec));
            _aimRotation = Quaternion.Slerp(_aimRotation, look, aimBlend);

            AdvancePunch();
            transform.rotation = _aimRotation * Quaternion.Euler(0f, 0f, _rollDeg * _punchT);
            if (_cam != null)
            {
                _baseFov = Mathf.Clamp(_tuning.Camera.CameraFovDeg, FollowCameraControllerDefaults.FovClampMinDeg, FollowCameraControllerDefaults.FovClampMaxDeg);
                _cam.fieldOfView = _baseFov * (1f - _fovKick * _punchT);
            }
        }

        float ResolveCameraDistance()
        {
            float distance = _tuning.Camera.CameraDistanceM;
            // Mesafe büyümesi SADECE gerçek lock-on'da (ff-4) — menzil yakınlığıyla değil,
            // yoksa varsayılan == lock-on olur (eski bug).
            if (LockOnActive && _bossTarget != null)
            {
                Vector3 toBoss = _bossTarget.position - _target.position;
                toBoss.y = 0f;
                float extra = Mathf.Min(
                    toBoss.magnitude * _tuning.Camera.CameraLockOnDistancePerSepM,
                    _tuning.Camera.CameraLockOnMaxExtraDistanceM);
                distance = Mathf.Clamp(
                    distance + extra,
                    _tuning.Camera.CameraLockOnMinDistanceM,
                    _tuning.Camera.CameraLockOnMaxDistanceM);
            }

            float windupMul = Mathf.Lerp(1f, _tuning.Camera.CameraWindupDistanceMul, _windupPullback);
            return distance * windupMul;
        }


        bool IsBlockingCollider(Collider col)
        {
            if (col == null || col.isTrigger)
                return false;
            if (_ignoreRoots != null)
            {
                Transform t = col.transform;
                for (int r = 0; r < _ignoreRoots.Length; r++)
                {
                    Transform root = _ignoreRoots[r];
                    if (root != null && t.IsChildOf(root))
                        return false;
                }
            }

            return true;
        }

        float ResolveLockOnShoulderSign(Vector3 playerAim, float dt)
        {
            if (_cam == null || _bossTarget == null)
                return _lockOnShoulderSign;

            float scorePos = ScoreShoulderSide(+1f, playerAim);
            float scoreNeg = ScoreShoulderSide(-1f, playerAim);
            float pick = scorePos >= scoreNeg ? 1f : -1f;
            if (Mathf.Abs(pick - _lockOnShoulderSign) > FollowCameraControllerDefaults.ShoulderSignEpsilon)
            {
                float lead = pick > 0f ? scorePos - scoreNeg : scoreNeg - scorePos;
                if (lead < _tuning.Camera.CameraLockOnShoulderFlipHysteresis)
                    pick = _lockOnShoulderSign;
            }

            return pick;
        }

        float ScoreShoulderSide(float sign, Vector3 playerAim)
        {
            Vector3 shoulder = _tuning.Camera.CameraShoulderOffset;
            shoulder.x += sign * _tuning.Camera.CameraLockOnShoulderSideM;
            Vector3 localOffset = shoulder + Vector3.back * _resolvedDistanceM;
            Vector3 offset = Quaternion.Euler(OrbitPitchDeg, _resolvedYawDeg, 0f) * localOffset;
            Vector3 camPos = _target.position + offset;
            Vector3 lookTarget = Vector3.Lerp(playerAim, _bossTarget.position + Vector3.up * _tuning.Camera.CameraBossAimHeightM,
                _tuning.Camera.CameraLockOnLookBlendToBoss);
            Quaternion rot = Quaternion.LookRotation(lookTarget - camPos, Vector3.up);
            Vector3 playerVp = WorldToViewport(rot, camPos, _target.position + Vector3.up * FollowCameraControllerDefaults.PlayerViewportHeightM);
            // Oyuncu sol üçte birde (+), boss üst yarıda — skor.
            float playerSide = playerVp.x < FollowCameraControllerDefaults.PlayerSideViewportLeft ? 1f : playerVp.x > FollowCameraControllerDefaults.PlayerSideViewportRight ? FollowCameraControllerDefaults.PlayerSideWeightEdge : FollowCameraControllerDefaults.PlayerSideWeightCenter;
            float playerLow = playerVp.y < FollowCameraControllerDefaults.PlayerLowViewportThreshold ? 1f : 0.5f;
            return playerSide * playerLow;
        }

        static Vector3 WorldToViewport(Quaternion camRot, Vector3 camPos, Vector3 world)
        {
            Vector3 local = Quaternion.Inverse(camRot) * (world - camPos);
            if (local.z <= FollowCameraControllerDefaults.MinLocalDepthM)
                return new Vector3(0.5f, 0.5f, -1f);
            float fov = 60f;
            float tan = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float vx = 0.5f + 0.5f * (local.x / (local.z * tan));
            float vy = 0.5f + 0.5f * (local.y / (local.z * tan));
            return new Vector3(vx, vy, local.z);
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
            float angle = _shakeElapsedSec * FollowCameraControllerDefaults.ShakeAngleRateHz;
            _shakeOffset = new Vector3(
                Mathf.Sin(angle) * _shakeAmplitude * falloff,
                Mathf.Cos(angle * FollowCameraControllerDefaults.ShakeCosHarmonicMult) * _shakeAmplitude * FollowCameraControllerDefaults.ShakeAmplitudeFalloffMult * falloff,
                0f);

            if (_shakeElapsedSec >= _shakeDurationSec)
            {
                _shakeDurationSec = 0f;
                _shakeOffset = Vector3.zero;
            }
        }
    }
}
