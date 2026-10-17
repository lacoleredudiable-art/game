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
using Dovus.Game.Platform;
using Dovus.Game.Config;
using Dovus.Game.Vfx;
using UnityEngine;
using Dovus.Core.Shared;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// DodgeState yer değiştirme oranını oyuncu transform'una uygular (§6).
    /// Yön: son hareket; o da yoksa bossun tersi.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public sealed partial class DodgeMotionController : MonoBehaviour
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
        HexagonInputController _inputCtl;

        Vector3 _startPos;
        Vector3 _dir;
        Vector3 _hitNormal;
        Vector3 _glideExtra;
        int _pressMs = int.MinValue;
        int _promotedMs = int.MinValue;
        float _rollRatioBase;
        float _lastEmitRatio = -1f;
        ActorView _visual;

        // O-4.9 kayma durumu. Kayma başlayınca dodge bitene dek serbest yola dönülmez.
        readonly DodgeSlide _slide = new DodgeSlide();
        bool _slideMode;
        int _simMs;
        float _pathM;
        BossTelegraphView _telegraphCache;
        Transform _telegraphOwner;

        public bool IsSliding => _slideMode && _slide.Active;
        public int SlideSide => _slide.Side;
        public float SlideBudgetM => _slide.BudgetM;

        public bool IsDisplacing { get; private set; }
        public float LastAppliedRatio { get; private set; }
        public Vector3 LastSlideStart { get; private set; }
        public bool IsBound => _dodge != null && _clock != null;

        /// <summary>O-4.9: engel ile gövde kenarı arası pay; v4 Hareket süpürmesi de bunu okur.</summary>
        public float EdgeGapM => _tuning != null ? _tuning.EdgeGapM : DodgeMotionControllerDefaults.FallbackEdgeGapM;

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
            _inputCtl = input;
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
            else if (_dodge.PromotedAtMs is int promoted && promoted != _promotedMs)
            {
                BeginRoll(promoted);
            }

            if (!_dodge.IsActive(worldMs))
            {
                // Kare takılırsa (editör odağı, MCP) aktif pencere tek karede atlanabilir;
                // başlamış kaymayı sonuna taşı. Glide kuyruğu KORUNUR: eskiden burada
                // atılıyordu ve her dodge'un son karesinde oyuncu ~1 m geriye zıplıyordu,
                // yani §6'nın "yağ gibi kayma"sı görünmüyordu (T8.1).
                if (IsDisplacing && _tuning != null && !_slideMode)
                {
                    float dist = _tuning.DistanceM * (_dodge != null ? _dodge.DistanceMultiplier : 1f);
                    transform.position = Place(_startPos + _dir * dist * (1f - _rollRatioBase) + _glideExtra);
                    LastAppliedRatio = 1f;
                }

                IsDisplacing = false;
                return;
            }

            float ratio = _dodge.GetDisplacementRatio(worldMs);
            float distanceM = _tuning.DistanceM * _dodge.DistanceMultiplier;
            Vector3 target = _startPos + _dir * distanceM * (ratio - _rollRatioBase);

            if (_slideMode)
            {
                StepSlide(worldMs, distanceM);
                LastAppliedRatio = ratio;
                // Kayma bütçesi bitip karakter durduysa, çubuk engelden uzağa bakınca konum sahipliği bırakılır:
                // yürüme hemen açılır (i-frame pencere sonuna dek sürer; eskiden 0,66 s donuyordu). Çubuk engele
                // bakarken sahiplik sürer: yürüme EdgeGapM payını yiyip yüzeye yapışmaz (O-4.9).
                IsDisplacing = _slide.Active || !StickLeavesObstacle();
                MaybeEmitAfterimage(ratio);
                return;
            }

            float glide = _dodge.GetGlideVelocityRatio(worldMs);
            if (glide > 0f)
            {
                float dtSec = (float)(_clock.WorldDeltaMs / Units.SecToMs);
                _glideExtra += _dir * GlideSpeedMps() * glide * dtSec;
            }

            Vector3 placed = Place(target + _glideExtra, out Vector3 hitNormal, out bool hit);
            transform.position = placed;
            if (hit)
                BeginSlideOnHit(worldMs, distanceM, ratio, placed, hitNormal);
            LastAppliedRatio = ratio;
            if (_dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(_dir, Vector3.up);

            IsDisplacing = !_slideMode || _slide.Active || !StickLeavesObstacle();
            MaybeEmitAfterimage(ratio);
        }

        void BeginSlide()
        {
            _startPos = transform.position;
            LastSlideStart = _startPos;
            LastAppliedRatio = 0f;
            _glideExtra = Vector3.zero;
            _lastEmitRatio = -1f;
            _rollRatioBase = 0f;
            _promotedMs = int.MinValue;
            ResetSlide();
            _dir = ResolveDirection(transform.forward);
            _afterimage?.Clear();
            IsDisplacing = true;
            if (_visual == null)
                _visual = GetComponent<ActorView>();
            _visual?.Trigger(ActorView.TriggerDodge);
            int nowMs = (int)_clock.Director.WorldTimeMs;
            float rollSec = Mathf.Max(0, _pressMs + _dodge.SlideEndOffsetMs - nowMs) / (float)Units.SecToMs;
            DodgeClipPlayback.ApplyDodge(_visual?.Animator, rollSec, _dodge.RecoveryMs / (float)Units.SecToMs);
            SlideStarted?.Invoke(_startPos, _dir);
        }

        /// <summary>
        /// Takla (O-4.5, 15-dodge-tasarimi §3.2): eski yol kesilir, yeni yol bulunduğu yerden başlar;
        /// yön ikinci basıştaki çubuk yönü (boşsa birinci dodge'un yönü). Toplam mesafe ×CombinedDistanceMult
        /// korunur (oran sürekli). Konumu yine bu denetleyici yazar: ikinci bir konum sahibi yok.
        /// </summary>
        void BeginRoll(int promotedMs)
        {
            _promotedMs = promotedMs;
            _startPos = transform.position;
            _glideExtra = Vector3.zero;
            _rollRatioBase = _dodge.RatioAtPromote;
            _lastEmitRatio = -1f;
            ResetSlide();
            _dir = ResolveDirection(_dir);
            _afterimage?.Clear();
            if (_visual == null)
                _visual = GetComponent<ActorView>();
            _visual?.Trigger(ActorView.TriggerDodge);
            int nowMs = (int)_clock.Director.WorldTimeMs;
            float rollSec = Mathf.Max(0, _pressMs + _dodge.SlideEndOffsetMs - nowMs) / (float)Units.SecToMs;
            DodgeClipPlayback.ApplyDodge(_visual?.Animator, rollSec, _dodge.RecoveryMs / (float)Units.SecToMs);
            SlideStarted?.Invoke(_startPos, _dir);
        }

        Vector3 ResolveDirection(Vector3 face)
        {
            Vector3 stick = StickWorld();
            DodgeDirection.Resolve(stick.x, stick.z, face.x, face.z, out float x, out float z);
            return new Vector3(x, 0f, z);
        }

        /// <summary>Çubuğun dünya düzlemindeki yönü (kamera yaw'ıyla); çubuk boşsa sıfır.</summary>
        Vector3 StickWorld()
        {
            if (_input == null)
                return Vector3.zero;
            Vector2 move = _input.MoveDirection;
            if (move.sqrMagnitude <= DodgeMotionControllerDefaults.PlanarMoveEpsilonSqr)
                return Vector3.zero;
            Vector3 stick = new Vector3(move.x, 0f, move.y);
            return _follow != null ? Quaternion.Euler(0f, _follow.MovementYawDeg, 0f) * stick : stick;
        }

        /// <summary>Engelde duran dodge'da çubuk engelden uzağa (ya da yüzeye teğet) bakıyor mu.</summary>
        bool StickLeavesObstacle()
        {
            if (_hitNormal.sqrMagnitude < DodgeMotionControllerDefaults.PlanarMoveEpsilonSqr)
                return false;
            Vector3 stick = StickWorld();
            return stick.sqrMagnitude > 0f && Vector3.Dot(stick, _hitNormal) >= 0f;
        }

        void ResetSlide()
        {
            _slide.Reset();
            _slideMode = false;
            _hitNormal = Vector3.zero;
        }

        Vector3 Place(Vector3 pos) => Place(pos, out _, out _);

        Vector3 Place(Vector3 pos, out Vector3 normal, out bool clipped)
        {
            // Kayma yataydır. Y'yi burada ezmek havadaki iptali eski yüksekliğe geri yazıyordu.
            float y = transform.position.y;
            normal = Vector3.zero;
            clipped = false;
            Vector3 arena = ClampArena(pos);
            Vector3 boss = KeepBossEdge(arena, _startPos);
            Vector3 result = boss;
            // Konum doğrudan yazılmaz: motorun süpürmesi engelde (sütun, duvar, koza, boss gövdesi)
            // keser; kesildiği yerde O-4.9 kaymayı başlatır.
            if (_motor != null)
            {
                result = _motor.SweepStop(transform.position, boss, out Vector3 motorNormal, out bool motorClipped);
                if (motorClipped)
                {
                    normal = motorNormal;
                    clipped = true;
                }
            }

            if (!clipped && PlanarDist2(boss, arena) > 1e-8f)
            {
                normal = BossNormal(boss);
                clipped = normal.sqrMagnitude > 0.0001f;
            }

            if (!clipped && PlanarDist2(arena, pos) > 1e-8f)
            {
                normal = ArenaNormal(arena);
                clipped = normal.sqrMagnitude > 0.0001f;
            }

            result.y = y;
            return result;
        }

        static float PlanarDist2(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        Vector3 BossNormal(Vector3 at)
        {
            if (_boss == null)
                return Vector3.zero;
            Vector3 n = at - _boss.position;
            n.y = 0f;
            return n.sqrMagnitude < 0.0001f ? Vector3.zero : n.normalized;
        }

        static Vector3 ArenaNormal(Vector3 at)
        {
            Vector3 n = new Vector3(-at.x, 0f, -at.z);
            return n.sqrMagnitude < 0.0001f ? Vector3.zero : n.normalized;
        }

        Transform _reactorOwner;
        BossReactorController _reactorCache;
        bool _bossRadiusFallbackLogged;

        Vector3 KeepBossEdge(Vector3 pos, Vector3 fromPos)
        {
            if (_boss == null)
                return pos;
            float body = _motor != null ? _motor.BodyRadiusM : 0.5f;
            float bossR = CombatFallbacks.BossBodyRadiusFallbackM;
            // O11: yer değiştirme karesi başına GetComponent yerine boss başına bir kez.
            if (_reactorOwner != _boss || _reactorCache == null)
            {
                _reactorOwner = _boss;
                _reactorCache = _boss.GetComponent<BossReactorController>();
            }
            BossReactorController reactor = _reactorCache;
            if (reactor != null && reactor.BodyRadiusM > DodgeMotionControllerDefaults.MinBodyRadiusM)
                bossR = reactor.BodyRadiusM;
            else if (!_bossRadiusFallbackLogged)
            {
                _bossRadiusFallbackLogged = true;
                Debug.LogError("[V4-FALLBACK] DodgeMotionController.KeepBossEdge: boss gövde yarıçapı okunamadı; "
                    + $"yedek {CombatFallbacks.BossBodyRadiusFallbackM} m kullanıldı (world_physics.body_radius_boss_m)");
            }
            float gap = EdgeGapM;
            float x = pos.x;
            float z = pos.z;
            DodgeEdge.StopBeforeCrossing(
                ref x, ref z,
                fromPos.x, fromPos.z,
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

            return ArenaClamp.XZ(pos, ArenaBoundary.RadiusM(_motor.Tuning), _motor.BodyRadiusM);
        }
    }
}
