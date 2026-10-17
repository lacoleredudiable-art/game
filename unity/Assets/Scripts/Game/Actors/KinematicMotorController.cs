using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Core.RuleEngineV4;
using UnityEngine;
using Dovus.Core.Shared;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Rigidbody/CharacterController yok — transform, ölçeklenmiş dünya dt ile güncellenir.
    /// </summary>
    [RequireComponent(typeof(MoveInputController))]
    public sealed class KinematicMotorController : MonoBehaviour
    {
        [SerializeField] GameTuning _tuning = new();
        [SerializeField] float _bodyRadiusM = 0.5f;
        [SerializeField] float _rootAboveSoleM;

        GameClockHost _clock;
        MoveInputController _input;
        DodgeMotionController _dodgeMotion;
        PlayerVitalsHost _vitals;
        ActorStatusHost _status;
        ActorView _visual;
        FollowCameraController _follow;
        MotionTemplateBodyHost _templateBody;
        PlayerStateMachine _playerStates;
        System.Func<Transform> _combatFacingTarget;
        System.Func<bool> _combatFacingLocked;

        static readonly Collider[] ObstacleBuffer = new Collider[16];
        static readonly RaycastHit[] CastHits = new RaycastHit[8];
        readonly RuleEngineV4WalkBodyBlock _bodyBlock = new();

        public void BindCamera(FollowCameraController follow) => _follow = follow;

        public void BindClock(GameClockHost clock) => _clock = clock;

        public void BindPlayerStates(PlayerStateMachine states) => _playerStates = states;

        /// <summary>
        /// Hedef tutulurken veya saldırı sürerken hareket yönü gövdeyi döndürmez; karakter hedefe
        /// bakıp strafe/backpedal eder. Delegeler oyuncu örneğine özeldir (co-op static state yok).
        /// </summary>
        public void BindCombatFacing(
            System.Func<Transform> target,
            System.Func<bool> facingLocked)
        {
            _combatFacingTarget = target;
            _combatFacingLocked = facingLocked;
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

        public float BodyRadiusM
        {
            get => _bodyRadiusM;
            set => _bodyRadiusM = value;
        }

        /// <summary>Kökün ayak tabanından yüksekliği. Kök kapsül merkezindeyse yarı yükseklik; sondalar ayaktan ölçülür.</summary>
        public float RootAboveSoleM
        {
            get => _rootAboveSoleM;
            set => _rootAboveSoleM = value;
        }

        public float CapsuleRadiusM => Mathf.Max(KinematicMotorControllerDefaults.CapsuleRadiusFloorM, _bodyRadiusM);

        /// <summary>
        /// Gövde kapsülü (O-1.12, O-1.13): alt ucu ayak tabanında, üst ucu ayaktan <see cref="KinematicMotorControllerDefaults.BodyHeightM"/>;
        /// <paramref name="bottom"/> ve <paramref name="top"/> uç kürelerin merkezleri.
        /// </summary>
        public void BodyCapsule(Vector3 root, out Vector3 bottom, out Vector3 top, out float radius)
        {
            radius = CapsuleRadiusM;
            float sole = root.y - _rootAboveSoleM;
            float height = Mathf.Max(2f * radius, KinematicMotorControllerDefaults.BodyHeightM);
            bottom = new Vector3(root.x, sole + radius, root.z);
            top = new Vector3(root.x, sole + height - radius, root.z);
        }

        Vector3 BodyMid(Vector3 root) =>
            new(root.x, root.y - _rootAboveSoleM + KinematicMotorControllerDefaults.BodyHeightM * 0.5f, root.z);

        public Vector3 Velocity { get; private set; }
        public float LocoRefMps => _tuning != null ? _tuning.Player.WalkSpeedMps : KinematicMotorControllerDefaults.FallbackWalkSpeedMps;
        public float LocoDampSec => _tuning != null ? _tuning.Player.AnimSpeedDampSec : KinematicMotorControllerDefaults.FallbackAnimSpeedDampSec;
        public float LocoMaxPlayback => _tuning != null ? _tuning.Player.LocoMaxPlaybackMult : KinematicMotorControllerDefaults.FallbackLocoMaxPlaybackMult;

        void Awake()
        {
            _tuning ??= new GameTuning();
            _input = GetComponent<MoveInputController>();
        }

        // Tembel önbellek: DodgeMotionController, MotionTemplateBodyHost, ActorStatusHost vb. bu bileşenden SONRA eklenir
        // (Awake'te henüz yok); null iken her karede bir kez daha aranır (eski Update davranışı).
        void ResolveSiblings()
        {
            if (_dodgeMotion == null)
                _dodgeMotion = GetComponent<DodgeMotionController>();
            if (_visual == null)
                _visual = GetComponent<ActorView>();
            if (_templateBody == null)
                _templateBody = GetComponent<MotionTemplateBodyHost>();
            if (_vitals == null)
                _vitals = GetComponent<PlayerVitalsHost>();
            if (_status == null)
                _status = GetComponent<ActorStatusHost>();
        }

        void Update()
        {
            ResolveSiblings();
            if (_dodgeMotion != null && _dodgeMotion.IsDisplacing)
            {
                Velocity = Vector3.zero;
                _visual?.SetSpeed(0f);
                return;
            }

            if (_templateBody != null && _templateBody.IsDisplacing)
            {
                // Yeri kalıp yazar. Bacak hızını burada sıfırlamak ayakları donduruyordu;
                // blend'i kalıbın kendi hızı besler.
                Velocity = Vector3.zero;
                return;
            }

            if (_vitals != null && _vitals.IsDown)
            {
                Velocity = Vector3.zero;
                _visual?.SetSpeed(0f);
                return;
            }

            if (_status != null && _status.EffectiveBlocksMovement)
            {
                Velocity = Vector3.zero;
                _visual?.SetSpeed(0f);
                return;
            }

            if (_playerStates != null && _playerStates.BlocksMove)
            {
                Velocity = Vector3.zero;
                _visual?.SetSpeed(0f);
                return;
            }

            Vector2 move = _input.MoveDirection;
            Vector3 direction = new Vector3(move.x, 0f, move.y);
            float stick = Mathf.Clamp01(direction.magnitude);
            if (stick > 0.0001f)
                direction /= Mathf.Max(1f, direction.magnitude);

            if (_follow != null && direction.sqrMagnitude > 0.0001f)
                direction = Quaternion.Euler(0f, _follow.MovementYawDeg, 0f) * direction;

            float speedMult = _status != null ? _status.EffectiveMoveSpeedMult : 1f;
            if (_playerStates != null && _playerStates.MoveLimited && _status != null)
                speedMult *= _status.Tuning.SlowSpeedMult;
            float dtSec = GameClockFallback.DeltaSec(_clock, "clock.KinematicMotorController");

            // Analog yok (15 Ekim [K]): çubuk ölü bölgeyi geçtiyse tam hız (O-1.17).
            float speedFrac = stick > 0.0001f ? 1f : 0f;
            Vector3 dirFlat = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
            Vector3 target = dirFlat * (_tuning.Player.WalkSpeedMps * speedMult * speedFrac);

            Vector3 current = Velocity;
            float rate = target.sqrMagnitude >= current.sqrMagnitude ? _tuning.Player.MoveAccelMps2 : _tuning.Player.MoveDecelMps2;
            Velocity = Vector3.MoveTowards(current, target, rate * dtSec);

            _visual?.SetLocomotion(Velocity.magnitude, _tuning.Player.WalkSpeedMps, _tuning.Player.AnimSpeedDampSec,
                _tuning.Player.LocoMaxPlaybackMult);

            Vector3 from = transform.position;
            Vector3 next = SweepAndSlide(from, from + Velocity * dtSec);
            if (_tuning.RuleEngineV4.SliceScene)
            {
                BodyCapsule(next, out Vector3 bottom, out Vector3 top, out float radius);
                next = _bodyBlock.Resolve(from, next, bottom, top, radius, transform);
            }
            next = PushOutOfObstacles(next);
            next = ArenaClamp.XZ(next, ArenaBoundary.RadiusM(_tuning), _bodyRadiusM);
            transform.position = next;

            Transform combatTarget = _combatFacingTarget?.Invoke();
            bool lockFacing = combatTarget != null || (_combatFacingLocked?.Invoke() ?? false);
            Vector3 faceDirection = dirFlat;
            if (combatTarget != null)
            {
                faceDirection = combatTarget.position - transform.position;
                faceDirection.y = 0f;
                if (faceDirection.sqrMagnitude > 0.0001f)
                    faceDirection.Normalize();
            }

            if (!lockFacing && dirFlat.sqrMagnitude > 0.0001f)
                faceDirection = dirFlat;
            else if (lockFacing && combatTarget == null)
                faceDirection = Vector3.zero; // vuruşta hedef yoksa çubuk gövdeyi çevirmez

            if (faceDirection.sqrMagnitude > 0.0001f && dtSec > 0f)
            {
                Quaternion want = Quaternion.LookRotation(faceDirection, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, want, _tuning.Player.TurnRateDegPerSec * dtSec);
            }
        }

        /// <summary>
        /// Dodge yolu (kayma + toparlanma): yürüyüşle aynı kapsül süpürmesi, ama ilk engelde durur, kaymaz.
        /// Engel = katı collider, sabit (Anchored) gövdeli tetik (koza) ya da canlı gövde (minyon, dost). Y korunur.
        /// </summary>
        public Vector3 SweepStop(Vector3 from, Vector3 to) => SweepStop(from, to, out _, out _);

        /// <summary>
        /// O-4.9 için SweepStop: kesildiyse (clipped) engelin dışa bakan yatay normalini de verir.
        /// Arena sınırı da engeldir: normal içeri bakar.
        /// </summary>
        public Vector3 SweepStop(Vector3 from, Vector3 to, out Vector3 normal, out bool clipped)
        {
            normal = Vector3.zero;
            clipped = false;
            Vector3 delta = to - from;
            delta.y = 0f;
            float dist = delta.magnitude;
            Vector3 next = from;
            if (dist >= 0.0001f)
            {
                Vector3 dir = delta / dist;
                float go = FirstBlockDistance(from, dir, dist, out Vector3 hitNormal);
                next = from + dir * go;
                if (go < dist)
                {
                    clipped = true;
                    normal = hitNormal;
                }
                // Canlı gövde (minyon, dost) yürümeyle aynı silindir kuralı; tetik olduğu için kapsül süpürmesi görmez (O-4.9).
                BodyCapsule(next, out Vector3 bottom, out Vector3 top, out float radius);
                next = _bodyBlock.Resolve(from, next, bottom, top, radius, transform, dodgeStop: true);
                if (_bodyBlock.LastStopNormal.sqrMagnitude > 0.0001f)
                {
                    clipped = true;
                    normal = _bodyBlock.LastStopNormal;
                }
            }

            Vector3 pushed = PushOutOfObstacles(next, anchoredTriggersBlock: true);
            if ((pushed - next).sqrMagnitude > 1e-8f && normal.sqrMagnitude < 0.0001f)
            {
                normal = pushed - next;
                normal.y = 0f;
                clipped = true;
            }

            next = pushed;
            Vector3 arena = ArenaClamp.XZ(next, ArenaBoundary.RadiusM(Tuning), _bodyRadiusM);
            if ((arena.x - next.x) * (arena.x - next.x) + (arena.z - next.z) * (arena.z - next.z) > 1e-8f)
            {
                clipped = true;
                normal = new Vector3(arena.x - next.x, 0f, arena.z - next.z);
                if (normal.sqrMagnitude < 1e-8f)
                    normal = -new Vector3(arena.x, 0f, arena.z);
            }

            next = arena;
            next.y = from.y;
            if (normal.sqrMagnitude > 0.0001f)
            {
                normal.y = 0f;
                normal.Normalize();
            }

            return next;
        }

        /// <summary>
        /// O-4.9: konumun yakınındaki engel yüzeyi (reachM içinde). Normal engelden dışa bakar;
        /// corrected = gövde kenarı engelden gapM uzakta olacak konum (kayma boyunca EdgeGapM korunur).
        /// </summary>
        public bool ProbeContact(Vector3 pos, float gapM, float reachM, out Vector3 normal, out Vector3 corrected)
        {
            normal = Vector3.zero;
            corrected = pos;
            Vector3 probe = BodyMid(pos);
            BodyCapsule(pos, out Vector3 p1, out Vector3 p2, out float radius);
            float reach = radius + gapM + reachM;
            int count = Physics.OverlapCapsuleNonAlloc(
                p1, p2, reach, ObstacleBuffer, ~0, QueryTriggerInteraction.Collide);
            float best = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                Collider col = ObstacleBuffer[i];
                if (col == null || !BlocksDodge(col))
                    continue;
                Vector3 closest = col.ClosestPoint(probe);
                Vector3 away = probe - closest;
                away.y = 0f;
                float d = away.magnitude;
                if (d < 0.0001f || d >= best)
                    continue;
                best = d;
                normal = away / d;
                corrected = pos + normal * (radius + gapM - d);
            }

            if (best == float.MaxValue)
                return false;
            corrected.y = pos.y;
            return true;
        }

        /// <summary>O-4.9: gövde bu noktada bir dodge engeline değer mi (taraf seçiminde "dolu" sayılır).</summary>
        public bool PointBlocked(Vector3 pos)
        {
            BodyCapsule(pos, out Vector3 p1, out Vector3 p2, out float radius);
            int count = Physics.OverlapCapsuleNonAlloc(
                p1, p2, radius, ObstacleBuffer, ~0, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                if (BlocksDodge(ObstacleBuffer[i]))
                    return true;
            }

            return false;
        }

        float FirstBlockDistance(Vector3 from, Vector3 dir, float dist, out Vector3 normal)
        {
            normal = Vector3.zero;
            BodyCapsule(from, out Vector3 p1, out Vector3 p2, out float radius);
            int hits = Physics.CapsuleCastNonAlloc(
                p1, p2, radius, dir, CastHits, dist, ~0, QueryTriggerInteraction.Collide);
            float best = dist;
            bool blocked = false;
            for (int i = 0; i < hits; i++)
            {
                RaycastHit h = CastHits[i];
                if (!BlocksDodge(h.collider))
                    continue;
                if (h.distance <= 0f)
                {
                    // Başlangıçta temas: engelden uzaklaşan dodge serbest; içine doğru olan durur.
                    Vector3 probe = BodyMid(from);
                    Vector3 toward = h.collider.ClosestPoint(probe) - probe;
                    toward.y = 0f;
                    if (Vector3.Dot(toward, dir) <= 0f)
                        continue;
                    normal = -toward.normalized;
                }
                else
                {
                    normal = h.normal;
                }

                if (h.distance < best)
                {
                    best = h.distance;
                    blocked = true;
                }
            }

            normal.y = 0f;
            if (normal.sqrMagnitude > 0.0001f)
                normal.Normalize();
            return blocked ? Mathf.Max(0f, best - KinematicMotorControllerDefaults.MoveStopInsetM) : dist;
        }

        bool BlocksDodge(Collider col)
        {
            if (col == null || col.transform.IsChildOf(transform))
                return false;
            if (!col.isTrigger)
                return true;
            var body = col.GetComponentInParent<RuleEngineV4PhysicsBodyHost>();
            return body != null && body.WeightTier == RuleEngineV4WeightTier.Anchored;
        }

        Vector3 SweepAndSlide(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < 0.0001f)
                return from;

            Vector3 dir = delta / dist;
            BodyCapsule(from, out Vector3 p1, out Vector3 p2, out float radius);
            int hits = Physics.CapsuleCastNonAlloc(
                p1, p2, radius, dir, CastHits, dist, ~0, QueryTriggerInteraction.Ignore);
            if (hits <= 0)
                return to;

            float best = dist;
            Vector3 bestNormal = Vector3.zero;
            for (int i = 0; i < hits; i++)
            {
                RaycastHit h = CastHits[i];
                if (h.collider == null || h.collider.transform.IsChildOf(transform))
                    continue;
                if (h.distance < best)
                {
                    best = h.distance;
                    bestNormal = h.normal;
                }
            }

            Vector3 stop = from + dir * Mathf.Max(0f, best - KinematicMotorControllerDefaults.MoveStopInsetM);
            bestNormal.y = 0f;
            if (bestNormal.sqrMagnitude < 0.0001f)
                return stop;

            bestNormal.Normalize();
            float remain = dist - best;
            if (remain <= 0.001f)
                return stop;

            Vector3 slide = Vector3.ProjectOnPlane(dir * remain, bestNormal);
            return stop + slide;
        }

        Vector3 PushOutOfObstacles(Vector3 pos, bool anchoredTriggersBlock = false)
        {
            Vector3 probe = BodyMid(pos);
            BodyCapsule(pos, out Vector3 p1, out Vector3 p2, out float radius);
            // Tetik (dost vuruş kapsülü, skill alanı) duvar değildir. 11-8 dostu
            // yanına çağırınca bu itiş oyuncuya ikinci bir kayma yazıyordu.
            // Dodge'da yalnız sabit gövdeli tetik (koza) duvar sayılır.
            int count = Physics.OverlapCapsuleNonAlloc(
                p1, p2, radius, ObstacleBuffer, ~0,
                anchoredTriggersBlock ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = ObstacleBuffer[i];
                if (col == null || col.transform.IsChildOf(transform))
                    continue;
                if (anchoredTriggersBlock && !BlocksDodge(col))
                    continue;

                Vector3 closest = col.ClosestPoint(probe);
                Vector3 away = probe - closest;
                away.y = 0f;
                float dist = away.magnitude;
                if (dist < 0.0001f)
                {
                    Vector3 fromCenter = probe - col.bounds.center;
                    fromCenter.y = 0f;
                    if (fromCenter.sqrMagnitude < 0.0001f)
                        fromCenter = -new Vector3(pos.x, 0f, pos.z);
                    if (fromCenter.sqrMagnitude < 0.0001f)
                        fromCenter = Vector3.forward;
                    away = fromCenter.normalized;
                    dist = 0f;
                }
                else
                {
                    away /= dist;
                }

                if (dist < radius)
                    pos += away * (radius - dist + KinematicMotorControllerDefaults.SeparationInsetM);

                probe = BodyMid(pos);
            }

            return pos;
        }
    }
}
