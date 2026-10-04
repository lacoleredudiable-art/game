using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Game.Boss;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Config;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// DodgeState yer değiştirme oranını oyuncu transform'una uygular (§6).
    /// Yön: son hareket; o da yoksa bossun tersi.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed class DodgeMotionController : MonoBehaviour
    {
        GameClockHost _clock;
        DodgeState _dodge;
        DodgeTuning _tuning;
        GameTuning _colors;
        MoveInputController _input;
        Transform _boss;
        AfterimageTrailView _afterimage;
        KinematicMotorController _motor;
        PlayerVitalsHost _vitals;
        FollowCameraController _follow;

        Vector3 _startPos;
        Vector3 _dir;
        Vector3 _glideExtra;
        int _pressMs = int.MinValue;
        float _lastEmitRatio = -1f;
        ActorView _visual;

        public bool IsDisplacing { get; private set; }
        public float LastAppliedRatio { get; private set; }
        public Vector3 LastSlideStart { get; private set; }
        public bool IsBound => _dodge != null && _clock != null;

        /// <summary>Kayma başladı: başlangıç konumu + yön (toz/ses sunumu).</summary>
        public event System.Action<Vector3, Vector3> SlideStarted;

        public void Bind(
            GameClockHost clock,
            HexagonInputController input,
            Transform boss,
            AfterimageTrailView afterimage,
            FollowCameraController follow = null)
        {
            _clock = clock;
            _dodge = input.Dodge;
            _tuning = input.Combat.Dodge;
            _colors = input.Tuning;
            _boss = boss;
            _afterimage = afterimage;
            _follow = follow;
            _input = GetComponent<MoveInputController>();
            _motor = GetComponent<KinematicMotorController>();
            _vitals = GetComponent<PlayerVitalsHost>();
            _visual = GetComponent<ActorView>();
        }

        void Update()
        {
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
                    float dist = _tuning.DistanceM * (_dodge != null ? _dodge.DistanceMultiplier : 1f);
                    transform.position = Place(_startPos + _dir * dist + _glideExtra);
                    LastAppliedRatio = 1f;
                }

                IsDisplacing = false;
                return;
            }

            float ratio = _dodge.GetDisplacementRatio(worldMs);
            float distanceM = _tuning.DistanceM * _dodge.DistanceMultiplier;
            Vector3 target = _startPos + _dir * distanceM * ratio;

            float glide = _dodge.GetGlideVelocityRatio(worldMs);
            if (glide > 0f)
            {
                float dtSec = (float)(_clock.WorldDeltaMs / ActorsTimeDefaults.SecToMs);
                _glideExtra += _dir * GlideSpeedMps() * glide * dtSec;
            }

            transform.position = Place(target + _glideExtra);
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
                _visual = GetComponent<ActorView>();
            _visual?.Trigger(ActorView.TriggerDodge);
            SlideStarted?.Invoke(_startPos, _dir);
        }

        Vector3 ResolveDirection()
        {
            Vector3 stick = Vector3.zero;
            if (_input != null)
            {
                Vector2 move = _input.MoveDirection;
                if (move.sqrMagnitude > DodgeMotionControllerDefaults.PlanarMoveEpsilonSqr)
                {
                    stick = new Vector3(move.x, 0f, move.y);
                    if (_follow != null)
                        stick = Quaternion.Euler(0f, _follow.MovementYawDeg, 0f) * stick;
                }
            }

            Vector3 face = transform.forward;
            DodgeDirection.Resolve(stick.x, stick.z, face.x, face.z, out float x, out float z);
            return new Vector3(x, 0f, z);
        }

        Vector3 Place(Vector3 pos)
        {
            // Kayma yataydır. Y'yi burada ezmek havadaki iptali eski yüksekliğe geri yazıyordu.
            float y = transform.position.y;
            pos = KeepBossEdge(ClampArena(pos));
            pos.y = y;
            return pos;
        }

        Transform _reactorOwner;
        BossReactorController _reactorCache;

        Vector3 KeepBossEdge(Vector3 pos)
        {
            if (_boss == null)
                return pos;
            float body = _motor != null ? _motor.BodyRadiusM : 0.5f;
            float bossR = DodgeMotionControllerDefaults.FallbackBossRadiusM;
            // O11: yer değiştirme karesi başına GetComponent yerine boss başına bir kez.
            if (_reactorOwner != _boss || _reactorCache == null)
            {
                _reactorOwner = _boss;
                _reactorCache = _boss.GetComponent<BossReactorController>();
            }
            BossReactorController reactor = _reactorCache;
            if (reactor != null && reactor.BodyRadiusM > DodgeMotionControllerDefaults.MinBodyRadiusM)
                bossR = reactor.BodyRadiusM;
            float gap = _tuning != null ? _tuning.EdgeGapM : DodgeMotionControllerDefaults.FallbackEdgeGapM;
            float x = pos.x;
            float z = pos.z;
            DodgeEdge.StopBeforeCrossing(
                ref x, ref z,
                _startPos.x, _startPos.z,
                _boss.position.x, _boss.position.z,
                body + bossR + gap);
            pos.x = x;
            pos.z = z;
            return pos;
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
        float GlideSpeedMps() => _colors != null ? _colors.Player.DodgeGlideSpeedMps : DodgeMotionControllerDefaults.FallbackGlideSpeedMps;

        Vector3 ClampArena(Vector3 pos)
        {
            if (_motor == null)
                return pos;

            return ArenaClamp.XZ(pos, _motor.Tuning.Arena.ArenaHalfSizeM, _motor.BodyRadiusM);
        }
    }
}
