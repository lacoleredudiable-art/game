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
            Vector2 move = _input.MoveDirection;
            Vector3 direction = new Vector3(move.x, 0f, move.y);
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            float dtSec = _clock != null ? (float)(_clock.WorldDeltaMs / 1000.0) : Time.deltaTime;
            Velocity = direction * _tuning.WalkSpeedMps;

            Vector3 next = transform.position + Velocity * dtSec;
            float limit = Mathf.Max(0f, _tuning.ArenaHalfSizeM - _bodyRadiusM);
            next.x = Mathf.Clamp(next.x, -limit, limit);
            next.z = Mathf.Clamp(next.z, -limit, limit);
            transform.position = next;

            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
