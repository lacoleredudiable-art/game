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
    /// DodgeState yer deÄŸiÅŸtirme oranÄ±nÄ± oyuncu transform'una uygular (Â§6).
    /// YÃ¶n: son hareket; o da yoksa bossun tersi.
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

        /// <summary>Kayma baÅŸladÄ±: baÅŸlangÄ±Ã§ konumu + yÃ¶n (toz/ses sunumu).</summary>
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
                // Kare takÄ±lÄ±rsa (editÃ¶r odaÄŸÄ±, MCP) aktif pencere tek karede atlanabilir;
                // baÅŸlamÄ±ÅŸ kaymayÄ± sonuna taÅŸÄ±. Glide kuyruÄŸu KORUNUR: eskiden burada
                // atÄ±lÄ±yordu ve her dodge'un son karesinde oyuncu ~1 m geriye zÄ±plÄ±yordu,
                // yani Â§6'nÄ±n "yaÄŸ gibi kayma"sÄ± gÃ¶rÃ¼nmÃ¼yordu (T8.1).
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
                float dtSec = (float)(_clock.WorldDeltaMs / 1000.0);
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
                _visual = GetComponent<ActorVisual>();
            _visual?.Trigger(ActorVisual.TriggerDodge);
            SlideStarted?.Invoke(_startPos, _dir);
        }

        Vector3 ResolveDirection()
        {
            Vector3 stick = Vector3.zero;
            if (_input != null)
            {
                Vector2 move = _input.MoveDirection;
                if (move.sqrMagnitude > 0.01f)
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
            // Kayma yataydÄ±r. Y'yi burada ezmek havadaki iptali eski yÃ¼ksekliÄŸe geri yazÄ±yordu.
            float y = transform.position.y;
            pos = KeepBossEdge(ClampArena(pos));
            pos.y = y;
            return pos;
        }

        Transform _reactorOwner;
        BossReactor _reactorCache;

        Vector3 KeepBossEdge(Vector3 pos)
        {
            if (_boss == null)
                return pos;
            float body = _motor != null ? _motor.BodyRadiusM : 0.5f;
            float bossR = 0.85f;
            // O11: yer deÄŸiÅŸtirme karesi baÅŸÄ±na GetComponent yerine boss baÅŸÄ±na bir kez.
            if (_reactorOwner != _boss || _reactorCache == null)
            {
                _reactorOwner = _boss;
                _reactorCache = _boss.GetComponent<BossReactor>();
            }
            BossReactor reactor = _reactorCache;
            if (reactor != null && reactor.BodyRadiusM > 0.01f)
                bossR = reactor.BodyRadiusM;
            float gap = _tuning != null ? _tuning.EdgeGapM : 0.15f;
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
        /// Â§6 "sÃ¶nen artÄ±k hÄ±z". Ana hareketin ortalama hÄ±zÄ± (3.8/0.26 = 14.6 m/s) buraya
        /// konulamaz: eÄŸri u=1'de hÄ±zÄ± sÄ±fÄ±ra indirdiÄŸi iÃ§in o deÄŸer ikinci bir atÄ±lÄ±m gibi
        /// okunuyordu. BÃ¼yÃ¼klÃ¼k artÄ±k veri (T8.1).
        /// </summary>
        float GlideSpeedMps() => _colors != null ? _colors.Player.DodgeGlideSpeedMps : 3.5f;

        Vector3 ClampArena(Vector3 pos)
        {
            if (_motor == null)
                return pos;

            return ArenaClamp.XZ(pos, _motor.Tuning.Arena.ArenaHalfSizeM, _motor.BodyRadiusM);
        }
    }
}
