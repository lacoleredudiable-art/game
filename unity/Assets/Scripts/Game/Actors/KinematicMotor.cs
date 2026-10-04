using Dovus.Core.Combat;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Rigidbody/CharacterController yok — transform, ölçeklenmiş dünya dt ile güncellenir.
    /// </summary>
    [RequireComponent(typeof(MoveInput))]
    public sealed class KinematicMotor : MonoBehaviour
    {
        [SerializeField] PrototypeTuning _tuning = new();
        [SerializeField] float _bodyRadiusM = 0.5f;

        GameClock _clock;
        MoveInput _input;
        DodgeMotion _dodgeMotion;
        PlayerVitals _vitals;
        ActorStatus _status;
        ActorVisual _visual;
        FollowCamera _follow;
        PlayerStateMachine _playerStates;
        System.Func<Transform> _combatFacingTarget;
        System.Func<bool> _combatFacingLocked;

        static readonly Collider[] ObstacleBuffer = new Collider[16];
        static readonly RaycastHit[] CastHits = new RaycastHit[8];

        public void BindCamera(FollowCamera follow) => _follow = follow;

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

        public PrototypeTuning Tuning
        {
            get
            {
                _tuning ??= new PrototypeTuning();
                return _tuning;
            }
            set => _tuning = value;
        }

        public float BodyRadiusM
        {
            get => _bodyRadiusM;
            set => _bodyRadiusM = value;
        }

        public Vector3 Velocity { get; private set; }
        public float LocoRefMps => _tuning != null ? _tuning.Player.WalkSpeedMps : 6.4f;
        public float LocoDampSec => _tuning != null ? _tuning.Player.AnimSpeedDampSec : 0.08f;
        public float LocoMaxPlayback => _tuning != null ? _tuning.Player.LocoMaxPlaybackMult : 1.5f;

        void Awake()
        {
            _tuning ??= new PrototypeTuning();
            _input = GetComponent<MoveInput>();
            _clock = FindAnyObjectByType<GameClock>();
        }

        void Update()
        {
            if (_dodgeMotion == null)
                _dodgeMotion = GetComponent<DodgeMotion>();
            if (_visual == null)
                _visual = GetComponent<ActorVisual>();
            if (_dodgeMotion != null && _dodgeMotion.IsDisplacing)
            {
                Velocity = Vector3.zero;
                _visual?.SetSpeed(0f);
                return;
            }

            var templateBody = GetComponent<MotionTemplateBody>();
            if (templateBody != null && templateBody.IsDisplacing)
            {
                // Yeri kalıp yazar. Bacak hızını burada sıfırlamak ayakları donduruyordu;
                // blend'i kalıbın kendi hızı besler.
                Velocity = Vector3.zero;
                return;
            }

            if (_vitals == null)
                _vitals = GetComponent<PlayerVitals>();
            if (_status == null)
                _status = GetComponent<ActorStatus>();

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

            if (_follow == null)
                _follow = FindAnyObjectByType<FollowCamera>();
            if (_follow != null && direction.sqrMagnitude > 0.0001f)
                direction = Quaternion.Euler(0f, _follow.MovementYawDeg, 0f) * direction;

            float speedMult = _status != null ? _status.EffectiveMoveSpeedMult : 1f;
            if (_playerStates != null && _playerStates.MoveLimited && _status != null)
                speedMult *= _status.Tuning.SlowSpeedMult;
            float dtSec = _clock != null ? (float)(_clock.WorldDeltaMs / 1000.0) : Time.deltaTime;

            float stickT = Mathf.InverseLerp(_tuning.Input.JoystickDeadZone, 1f, stick);
            float speedFrac = stick > 0.0001f ? Mathf.Lerp(_tuning.Player.MinStickSpeedFrac, 1f, stickT) : 0f;
            Vector3 dirFlat = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
            Vector3 target = dirFlat * (_tuning.Player.WalkSpeedMps * speedMult * speedFrac);

            Vector3 current = Velocity;
            float rate = target.sqrMagnitude >= current.sqrMagnitude ? _tuning.Player.MoveAccelMps2 : _tuning.Player.MoveDecelMps2;
            Velocity = Vector3.MoveTowards(current, target, rate * dtSec);

            _visual?.SetLocomotion(Velocity.magnitude, _tuning.Player.WalkSpeedMps, _tuning.Player.AnimSpeedDampSec,
                _tuning.Player.LocoMaxPlaybackMult);

            Vector3 from = transform.position;
            Vector3 next = SweepAndSlide(from, from + Velocity * dtSec);
            next = PushOutOfObstacles(next);
            next = ArenaClamp.XZ(next, _tuning.Arena.ArenaHalfSizeM, _bodyRadiusM);
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

        Vector3 SweepAndSlide(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            delta.y = 0f;
            float dist = delta.magnitude;
            if (dist < 0.0001f)
                return from;

            Vector3 dir = delta / dist;
            float radius = Mathf.Max(0.05f, _bodyRadiusM * 0.92f);
            Vector3 p1 = from + Vector3.up * (radius + 0.05f);
            Vector3 p2 = from + Vector3.up * 1.6f;
            int hits = Physics.CapsuleCastNonAlloc(
                p1, p2, radius, dir, CastHits, dist, ~0, QueryTriggerInteraction.Ignore);
            if (hits <= 0)
                return to;

            float best = dist;
            Vector3 bestNormal = Vector3.zero;
            for (int i = 0; i < hits; i++)
            {
                RaycastHit h = CastHits[i];
                if (h.collider == null)
                    continue;
                if (h.distance < best)
                {
                    best = h.distance;
                    bestNormal = h.normal;
                }
            }

            Vector3 stop = from + dir * Mathf.Max(0f, best - 0.02f);
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

        Vector3 PushOutOfObstacles(Vector3 pos)
        {
            Vector3 probe = pos + Vector3.up * 0.9f;
            // Tetik (dost vuruş kapsülü, skill alanı) duvar değildir. 11-8 dostu
            // yanına çağırınca bu itiş oyuncuya ikinci bir kayma yazıyordu.
            int count = Physics.OverlapSphereNonAlloc(
                probe, _bodyRadiusM, ObstacleBuffer, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = ObstacleBuffer[i];
                if (col == null)
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

                if (dist < _bodyRadiusM)
                    pos += away * (_bodyRadiusM - dist + 0.02f);

                probe = pos + Vector3.up * 0.9f;
            }

            return pos;
        }
    }
}
