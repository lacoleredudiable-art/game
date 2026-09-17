using Dovus.Core.Combat;
using UnityEngine;

namespace Dovus.Game
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
        SkillMotionDriver _skillMotion;
        PlayerVitals _vitals;
        ActorStatus _status;
        ActorVisual _visual;
        FollowCamera _follow;
        PlayerStateMachine _playerStates;

        // 16 Eylül: "duvarların içine giriliyor" bug raporu — WallColliderFit dungeon
        // parçalarına BoxCollider ekliyor, burada onlara karşı itme (push-out) uygulanır.
        static readonly Collider[] ObstacleBuffer = new Collider[8];

        public void BindCamera(FollowCamera follow) => _follow = follow;

        public void BindPlayerStates(PlayerStateMachine states) => _playerStates = states;

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
                _visual?.SetSpeed(0f);
                return;
            }

            if (_skillMotion == null)
                _skillMotion = GetComponent<SkillMotionDriver>();
            if (_skillMotion != null && _skillMotion.IsDisplacing)
            {
                _visual?.SetSpeed(0f);
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
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            // Kamera-göreli hareket — orbit yaw ile stick "ileri"si kamera-ileri olur.
            if (_follow == null)
                _follow = FindAnyObjectByType<FollowCamera>();
            if (_follow != null && direction.sqrMagnitude > 0.0001f)
                direction = Quaternion.Euler(0f, _follow.OrbitYawDeg, 0f) * direction;

            float speedMult = _status != null ? _status.EffectiveMoveSpeedMult : 1f;
            // recovering.can_move=limited — StatusTuning.SlowSpeedMult (sayı uydurma yok).
            if (_playerStates != null && _playerStates.MoveLimited && _status != null)
                speedMult *= _status.Tuning.SlowSpeedMult;
            float dtSec = _clock != null ? (float)(_clock.WorldDeltaMs / 1000.0) : Time.deltaTime;
            float walk = _tuning.WalkSpeedMps * speedMult;
            Velocity = direction * walk;
            _visual?.SetSpeed(walk > 0.01f ? Mathf.Clamp01(direction.magnitude) : 0f);

            Vector3 next = transform.position + Velocity * dtSec;
            next = PushOutOfObstacles(next);
            float limit = Mathf.Max(0f, _tuning.ArenaHalfSizeM - _bodyRadiusM);
            next.x = Mathf.Clamp(next.x, -limit, limit);
            next.z = Mathf.Clamp(next.z, -limit, limit);
            transform.position = next;

            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        /// <summary>
        /// Rigidbody/CharacterController yok (dosya başlığı) — WallColliderFit'in eklediği
        /// BoxCollider'lara karşı basit küre-itme. Yalnızca yatay düzlemde (Y'ye dokunmaz).
        /// </summary>
        Vector3 PushOutOfObstacles(Vector3 pos)
        {
            int count = Physics.OverlapSphereNonAlloc(pos + Vector3.up * 0.9f, _bodyRadiusM, ObstacleBuffer);
            for (int i = 0; i < count; i++)
            {
                Collider col = ObstacleBuffer[i];
                if (col == null)
                    continue;

                Vector3 probe = pos + Vector3.up * 0.9f;
                Vector3 closest = col.ClosestPoint(probe);
                Vector3 away = probe - closest;
                away.y = 0f;
                float dist = away.magnitude;
                if (dist < _bodyRadiusM && dist > 0.0001f)
                    pos += away.normalized * (_bodyRadiusM - dist);
            }

            return pos;
        }
    }
}
