using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Rigidbody/CharacterController yok — transform, ölçeklenmiş dünya dt ile güncellenir.
    /// </summary>
    [RequireComponent(typeof(MoveInput))]
    public sealed class KinematicMotor : MonoBehaviour
    {
        [SerializeField] float _speedMps = 4.5f;

        GameClock _clock;
        MoveInput _input;

        public float SpeedMps
        {
            get => _speedMps;
            set => _speedMps = value;
        }

        public Vector3 Velocity { get; private set; }

        void Awake()
        {
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
            Velocity = direction * _speedMps;
            transform.position += Velocity * dtSec;

            if (direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
}
