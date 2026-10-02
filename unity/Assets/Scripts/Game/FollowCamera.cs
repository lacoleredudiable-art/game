using System.Collections.Generic;
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
        /// CombatFeel/§8 tuning'e dokunmadan; 0 iken davranış birebir eskisiyle aynı (T8/T8.1
        /// tuning'i bozmuyoruz). <see cref="CameraOrbitInput"/> tarafından sürülür.
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
        /// büyür (lock-on min/max'e kenetli). Panel satırı, Tab tuşu (bkz. CameraOrbitInput) ve
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

            float now = Time.unscaledTime;
            if (now < VisualHoldUntilUnscaled)
                return;

            Vector3 targetVelocity = _targetMotor != null ? _targetMotor.Velocity : Vector3.zero;

            Vector3 flatVelocity = new Vector3(targetVelocity.x, 0f, targetVelocity.z);
            Vector3 lookAhead = flatVelocity.sqrMagnitude > 0.0001f
                ? flatVelocity.normalized * _tuning.LookAheadM
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
                Mathf.Max(0.01f, _tuning.CameraLockOnDistanceSmoothSec),
                Mathf.Infinity,
                dt);

            Vector3 playerAim = _target.position + lookAhead * 0.35f
                + Vector3.up * _tuning.CameraLookHeightM;

            Vector3 shoulder = _tuning.CameraShoulderOffset;
            shoulder.y += _windupPullback * _tuning.CameraWindupExtraHeightM;
            if (LockOnActive)
            {
                _lockOnShoulderSign = ResolveLockOnShoulderSign(playerAim, dt);
                shoulder.x += _lockOnShoulderSign * _tuning.CameraLockOnShoulderSideM;
            }

            Vector3 localOffset = shoulder + Vector3.back * _resolvedDistanceM;
            Vector3 offset = Quaternion.Euler(OrbitPitchDeg, _resolvedYawDeg, 0f) * localOffset;
            Vector3 desired = _target.position + offset + lookAhead + _shakeOffset;
            desired = ApplyCameraCollision(playerAim, desired, dt);

            transform.position = Vector3.SmoothDamp(
                transform.position,
                desired,
                ref _velocity,
                _tuning.FollowSmoothTimeSec,
                Mathf.Infinity,
                dt);

            Vector3 lookTarget = playerAim;
            if (_bossTarget != null)
            {
                Vector3 bossAim = _bossTarget.position + Vector3.up * _tuning.CameraBossAimHeightM;
                if (LockOnActive)
                {
                    lookTarget = Vector3.Lerp(playerAim, bossAim, _tuning.CameraLockOnLookBlendToBoss);
                    UpdateLockOnScreenOverlap();
                }
                else if (framing > 0f)
                {
                    float blend = Mathf.Clamp01(_tuning.CameraBossFramingWeight) * framing;
                    lookTarget = Vector3.Lerp(playerAim, bossAim, blend);
                }
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

        float ResolveCameraDistance()
        {
            float distance = _tuning.CameraDistanceM;
            // Mesafe büyümesi SADECE gerçek lock-on'da (ff-4) — menzil yakınlığıyla değil,
            // yoksa varsayılan == lock-on olur (eski bug).
            if (LockOnActive && _bossTarget != null)
            {
                Vector3 toBoss = _bossTarget.position - _target.position;
                toBoss.y = 0f;
                float extra = Mathf.Min(
                    toBoss.magnitude * _tuning.CameraLockOnDistancePerSepM,
                    _tuning.CameraLockOnMaxExtraDistanceM);
                distance = Mathf.Clamp(
                    distance + extra,
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
                if (LockOnActive && toBoss.sqrMagnitude > 0.001f)
                {
                    // Lock-on: menzilden bağımsız tam yaw kenetleme (konum da boss'a döner).
                    desired = Mathf.Atan2(toBoss.x, toBoss.z) * Mathf.Rad2Deg;
                }
                else
                {
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

        Vector3 ApplyCameraCollision(Vector3 pivot, Vector3 desiredWorld, float dt)
        {
            Vector3 delta = desiredWorld - pivot;
            float targetAlong = delta.magnitude;
            if (targetAlong < 0.02f)
            {
                _collisionPulledInM = 0f;
                return desiredWorld;
            }

            Vector3 dir = delta / targetAlong;
            float blockedAlong = targetAlong;
            float radius = Mathf.Max(0.05f, _tuning.CameraCollisionSphereRadiusM);
            int hitCount = Physics.SphereCastNonAlloc(
                pivot,
                radius,
                dir,
                CollisionHits,
                targetAlong,
                _collisionLayerMask,
                QueryTriggerInteraction.Ignore);
            float best = targetAlong;
            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit h = CollisionHits[i];
                if (!IsBlockingCollider(h.collider))
                    continue;
                float along = h.distance - _tuning.CameraCollisionMarginM;
                if (along < best)
                    best = along;
            }

            blockedAlong = Mathf.Max(_tuning.CameraCollisionMinDistanceM, best);
            bool pullingIn = blockedAlong < _smoothedAlongDistM - 0.001f;
            float smooth = pullingIn
                ? _tuning.CameraCollisionPullInSmoothSec
                : _tuning.CameraCollisionPullOutSmoothSec;
            if (_smoothedAlongDistM <= 0.01f)
                _smoothedAlongDistM = targetAlong;
            _smoothedAlongDistM = Mathf.SmoothDamp(
                _smoothedAlongDistM,
                Mathf.Min(targetAlong, blockedAlong),
                ref _alongDistVelocity,
                Mathf.Max(0.001f, smooth),
                Mathf.Infinity,
                dt);
            _collisionPulledInM = Mathf.Max(0f, targetAlong - _smoothedAlongDistM);
            return pivot + dir * _smoothedAlongDistM;
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
            if (_cam == null)
                _cam = GetComponent<Camera>();
            if (_cam == null || _bossTarget == null)
                return _lockOnShoulderSign;

            float scorePos = ScoreShoulderSide(+1f, playerAim);
            float scoreNeg = ScoreShoulderSide(-1f, playerAim);
            float pick = scorePos >= scoreNeg ? 1f : -1f;
            if (Mathf.Abs(pick - _lockOnShoulderSign) > 0.01f)
            {
                float lead = pick > 0f ? scorePos - scoreNeg : scoreNeg - scorePos;
                if (lead < _tuning.CameraLockOnShoulderFlipHysteresis)
                    pick = _lockOnShoulderSign;
            }

            return pick;
        }

        float ScoreShoulderSide(float sign, Vector3 playerAim)
        {
            Vector3 shoulder = _tuning.CameraShoulderOffset;
            shoulder.x += sign * _tuning.CameraLockOnShoulderSideM;
            Vector3 localOffset = shoulder + Vector3.back * _resolvedDistanceM;
            Vector3 offset = Quaternion.Euler(OrbitPitchDeg, _resolvedYawDeg, 0f) * localOffset;
            Vector3 camPos = _target.position + offset;
            Vector3 lookTarget = Vector3.Lerp(playerAim, _bossTarget.position + Vector3.up * _tuning.CameraBossAimHeightM,
                _tuning.CameraLockOnLookBlendToBoss);
            Quaternion rot = Quaternion.LookRotation(lookTarget - camPos, Vector3.up);
            Vector3 playerVp = WorldToViewport(rot, camPos, _target.position + Vector3.up * 0.9f);
            // Oyuncu sol üçte birde (+), boss üst yarıda — skor.
            float playerSide = playerVp.x < 0.42f ? 1f : playerVp.x > 0.58f ? 0.35f : 0.7f;
            float playerLow = playerVp.y < 0.55f ? 1f : 0.5f;
            return playerSide * playerLow;
        }

        static Vector3 WorldToViewport(Quaternion camRot, Vector3 camPos, Vector3 world)
        {
            Vector3 local = Quaternion.Inverse(camRot) * (world - camPos);
            if (local.z <= 0.05f)
                return new Vector3(0.5f, 0.5f, -1f);
            float fov = 60f;
            float tan = Mathf.Tan(fov * 0.5f * Mathf.Deg2Rad);
            float vx = 0.5f + 0.5f * (local.x / (local.z * tan));
            float vy = 0.5f + 0.5f * (local.y / (local.z * tan));
            return new Vector3(vx, vy, local.z);
        }

        void UpdateLockOnScreenOverlap()
        {
            if (_cam == null || _bossTarget == null)
                return;
            Rect player = ProjectActorRect(_target, 0.9f, 0.45f);
            Rect boss = ProjectActorRect(_bossTarget, _tuning.CameraBossAimHeightM, 1.2f);
            float playerArea = player.width * player.height;
            if (playerArea < 1e-5f)
            {
                _lastLockOnOverlapPct = 1f;
                return;
            }

            float overlap = RectIntersectionArea(player, boss);
            _lastLockOnOverlapPct = Mathf.Clamp01(1f - overlap / playerArea);
        }

        Rect ProjectActorRect(Transform actor, float centerUpM, float halfHeightM)
        {
            Vector3 c = actor.position + Vector3.up * centerUpM;
            Vector3 top = c + Vector3.up * halfHeightM;
            Vector3 bottom = c - Vector3.up * halfHeightM;
            Vector3 left = c - transform.right * 0.35f;
            Vector3 right = c + transform.right * 0.35f;
            Vector3[] pts =
            {
                _cam.WorldToViewportPoint(top),
                _cam.WorldToViewportPoint(bottom),
                _cam.WorldToViewportPoint(left),
                _cam.WorldToViewportPoint(right)
            };
            float minX = 1f, maxX = 0f, minY = 1f, maxY = 0f;
            for (int i = 0; i < pts.Length; i++)
            {
                if (pts[i].z <= 0f)
                    continue;
                minX = Mathf.Min(minX, pts[i].x);
                maxX = Mathf.Max(maxX, pts[i].x);
                minY = Mathf.Min(minY, pts[i].y);
                maxY = Mathf.Max(maxY, pts[i].y);
            }

            if (maxX < minX)
                return Rect.zero;
            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }

        static float RectIntersectionArea(Rect a, Rect b)
        {
            float xMin = Mathf.Max(a.xMin, b.xMin);
            float xMax = Mathf.Min(a.xMax, b.xMax);
            float yMin = Mathf.Max(a.yMin, b.yMin);
            float yMax = Mathf.Min(a.yMax, b.yMax);
            if (xMax <= xMin || yMax <= yMin)
                return 0f;
            return (xMax - xMin) * (yMax - yMin);
        }

        /// <summary>Play doğrulama: mevcut yerleşimde overlap yüzdesini logla (sunum).</summary>
        public void LogLockOnOverlap(float separationLabelM)
        {
            LockOnActive = true;
            UpdateLockOnScreenOverlap();
            float occludedPct = (1f - _lastLockOnOverlapPct) * 100f;
            DebugConfig.DevLog(
                $"[FollowCamera] lock-on overlap sep={separationLabelM:F1}m playerVisible={_lastLockOnOverlapPct * 100f:F1}% "
                + $"bossOccludesPlayer={occludedPct:F1}%");
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
