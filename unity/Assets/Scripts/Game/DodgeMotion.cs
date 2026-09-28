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
        PrototypeTuning _colors;
        MoveInput _input;
        Transform _boss;
        AfterimageTrail _afterimage;
        KinematicMotor _motor;
        PlayerVitals _vitals;
        FollowCamera _follow;

        Vector3 _lastMoveDir = Vector3.forward;
        Vector3 _startPos;
        Vector3 _dir;
        Vector3 _glideExtra;
        int _pressMs = int.MinValue;
        float _lastEmitRatio = -1f;
        ActorVisual _visual;

        public bool IsDisplacing { get; private set; }
        public float LastAppliedRatio { get; private set; }
        public Vector3 LastSlideStart { get; private set; }
        public bool IsBound => _dodge != null && _clock != null;

        /// <summary>Kayma başladı: başlangıç konumu + yön (toz/ses sunumu).</summary>
        public event System.Action<Vector3, Vector3> SlideStarted;

        public void Bind(
            GameClock clock,
            HexagonInput input,
            Transform boss,
            AfterimageTrail afterimage,
            FollowCamera follow = null)
        {
            _clock = clock;
            _dodge = input.Dodge;
            _tuning = input.Combat.Dodge;
            _colors = input.Tuning;
            _boss = boss;
            _afterimage = afterimage;
            _follow = follow;
            _input = GetComponent<MoveInput>();
            _motor = GetComponent<KinematicMotor>();
            _vitals = GetComponent<PlayerVitals>();
            _visual = GetComponent<ActorVisual>();
        }

        void Start()
        {
            if (!IsBound)
                TryAutoBind();
        }

        void TryAutoBind()
        {
            var input = FindAnyObjectByType<HexagonInput>();
            var clock = FindAnyObjectByType<GameClock>();
            var boss = GameObject.Find("Boss");
            if (input == null || clock == null || boss == null)
                return;
            Bind(
                clock,
                input,
                boss.transform,
                GetComponent<AfterimageTrail>(),
                FindAnyObjectByType<FollowCamera>());
        }

        void Update()
        {
            RememberWalk();

            if (!IsBound)
                TryAutoBind();

            if (_dodge == null || _clock == null || (_vitals != null && _vitals.IsDown))
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
                // başlamış kaymayı sonuna taşı. Glide kuyruğu KORUNUR: eskiden burada
                // atılıyordu ve her dodge'un son karesinde oyuncu ~1 m geriye zıplıyordu,
                // yani §6'nın "yağ gibi kayma"sı görünmüyordu (T8.1).
                if (IsDisplacing && _tuning != null)
                {
                    transform.position = ClampArena(_startPos + _dir * _tuning.DistanceM + _glideExtra);
                    LastAppliedRatio = 1f;
                }

                IsDisplacing = false;
                return;
            }

            float ratio = _dodge.GetDisplacementRatio(worldMs);
            Vector3 target = _startPos + _dir * _tuning.DistanceM * ratio;

            float glide = _dodge.GetGlideVelocityRatio(worldMs);
            if (glide > 0f)
            {
                float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);
                _glideExtra += _dir * GlideSpeedMps() * glide * dtSec;
            }

            transform.position = ClampArena(target + _glideExtra);
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
            if (_visual == null)
                _visual = GetComponent<ActorVisual>();
            _visual?.Trigger(ActorVisual.TriggerDodge);
            SlideStarted?.Invoke(_startPos, _dir);
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
            {
                Vector3 direction = new Vector3(move.x, 0f, move.y);
                if (_follow != null)
                    direction = Quaternion.Euler(0f, _follow.MovementYawDeg, 0f) * direction;
                _lastMoveDir = direction;
            }
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
            _afterimage.Emit(transform.position, transform.rotation, transform.localScale);
        }

        /// <summary>
        /// §6 "sönen artık hız". Ana hareketin ortalama hızı (3.8/0.26 = 14.6 m/s) buraya
        /// konulamaz: eğri u=1'de hızı sıfıra indirdiği için o değer ikinci bir atılım gibi
        /// okunuyordu. Büyüklük artık veri (T8.1).
        /// </summary>
        float GlideSpeedMps() => _colors != null ? _colors.DodgeGlideSpeedMps : 3.5f;

        Vector3 ClampArena(Vector3 pos)
        {
            if (_motor == null)
                return pos;

            return ArenaClamp.XZ(pos, _motor.Tuning.ArenaHalfSizeM, _motor.BodyRadiusM);
        }
    }
}
