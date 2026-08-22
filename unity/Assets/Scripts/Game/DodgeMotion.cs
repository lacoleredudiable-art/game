using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// DodgeState yer değiştirme oranını oyuncu transform'una uygular (§6).
    /// Yön: son hareket; o da yoksa bossun tersi.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class DodgeMotion : MonoBehaviour
    {
        GameClock _clock;
        DodgeState _dodge;
        DodgeTuning _tuning;
        MoveInput _input;
        Transform _boss;
        AfterimageTrail _afterimage;
        KinematicMotor _motor;

        Vector3 _lastMoveDir = Vector3.forward;
        Vector3 _startPos;
        Vector3 _dir;
        Vector3 _glideExtra;
        int _pressMs = int.MinValue;
        float _lastEmitRatio = -1f;

        public bool IsDisplacing { get; private set; }
        public float LastAppliedRatio { get; private set; }
        public Vector3 LastSlideStart { get; private set; }
        public bool IsBound => _dodge != null && _clock != null;

        public void Bind(
            GameClock clock,
            PentagonInput input,
            Transform boss,
            AfterimageTrail afterimage)
        {
            _clock = clock;
            _dodge = input.Dodge;
            _tuning = input.Combat.Dodge;
            _boss = boss;
            _afterimage = afterimage;
            _input = GetComponent<MoveInput>();
            _motor = GetComponent<KinematicMotor>();
        }

        void Start()
        {
            if (!IsBound)
                TryAutoBind();
        }

        void TryAutoBind()
        {
            var input = FindAnyObjectByType<PentagonInput>();
            var clock = FindAnyObjectByType<GameClock>();
            var boss = GameObject.Find("Boss");
            if (input == null || clock == null || boss == null)
                return;
            Bind(clock, input, boss.transform, GetComponent<AfterimageTrail>());
        }

        void Update()
        {
            RememberWalk();

            if (!IsBound)
                TryAutoBind();

            if (_dodge == null || _clock == null)
            {
                IsDisplacing = false;
                return;
            }

            int worldMs = (int)_clock.Director.WorldTimeMs;
            int? press = _dodge.PressTimeMs;
            if (press.HasValue && press.Value != _pressMs)
            {
                _pressMs = press.Value;
                BeginSlide();
            }

            if (!_dodge.IsActive(worldMs))
            {
                // Kare takılırsa (editör odağı, MCP) aktif pencere tek karede atlanabilir;
                // başlamış kaymayı 1.0'da bırak, yoksa dodge yerinde sayar.
                if (IsDisplacing && _tuning != null)
                {
                    Vector3 pos = ClampArena(_startPos + _dir * _tuning.DistanceM);
                    transform.position = pos;
                    LastAppliedRatio = 1f;
                }

                IsDisplacing = false;
                return;
            }

            float ratio = _dodge.GetDisplacementRatio(worldMs);
            Vector3 pos = _startPos + _dir * _tuning.DistanceM * ratio;

            float glide = _dodge.GetGlideVelocityRatio(worldMs);
            if (glide > 0f && _tuning.DurationMs > 0)
            {
                float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);
                float charSpeed = _tuning.DistanceM / (_tuning.DurationMs / 1000f);
                _glideExtra += _dir * charSpeed * glide * dtSec;
                pos += _glideExtra;
            }

            pos = ClampArena(pos);
            transform.position = pos;
            LastAppliedRatio = ratio;
            if (_dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_dir, Vector3.up);

            IsDisplacing = true;
            MaybeEmitAfterimage(ratio);
        }

        void BeginSlide()
        {
            _startPos = transform.position;
            LastSlideStart = _startPos;
            LastAppliedRatio = 0f;
            _glideExtra = Vector3.zero;
            _lastEmitRatio = -1f;
            _dir = ResolveDirection();
            _afterimage?.Clear();
            IsDisplacing = true;
        }

        Vector3 ResolveDirection()
        {
            if (_lastMoveDir.sqrMagnitude > 0.01f)
                return _lastMoveDir.normalized;

            if (_boss != null)
            {
                Vector3 away = transform.position - _boss.position;
                away.y = 0f;
                if (away.sqrMagnitude > 0.01f)
                    return away.normalized;
            }

            Vector3 fwd = transform.forward;
            fwd.y = 0f;
            return fwd.sqrMagnitude > 0.01f ? fwd.normalized : Vector3.forward;
        }

        void RememberWalk()
        {
            if (_input == null)
                return;

            Vector2 move = _input.MoveDirection;
            if (move.sqrMagnitude > 0.01f)
                _lastMoveDir = new Vector3(move.x, 0f, move.y);
        }

        void MaybeEmitAfterimage(float ratio)
        {
            if (_afterimage == null)
                return;

            int count = Mathf.Max(1, _afterimage.Count);
            float step = 1f / count;
            if (ratio + 0.001f < _lastEmitRatio + step && _lastEmitRatio >= 0f)
                return;

            _lastEmitRatio = ratio;
            Vector3 scale = _motor != null ? transform.localScale : Vector3.one;
            _afterimage.Emit(transform.position, transform.rotation, scale);
        }

        Vector3 ClampArena(Vector3 pos)
        {
            if (_motor == null)
                return pos;

            float limit = Mathf.Max(0f, _motor.Tuning.ArenaHalfSizeM - _motor.BodyRadiusM);
            pos.x = Mathf.Clamp(pos.x, -limit, limit);
            pos.z = Mathf.Clamp(pos.z, -limit, limit);
            return pos;
        }
    }
}
