using System;
using Dovus.Core.Motion;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Hareket kalıbını oyuncunun köküne uygular. Çubuk bu sırada gövdeyi çevirmez;
    /// bakış kalıbın kendi kuralındadır (çoğu vuruş hedefe kilitli).
    /// </summary>
    [DefaultExecutionOrder(12)]
    public sealed class MotionTemplateBody : MonoBehaviour
    {
        readonly MotionTemplateRunner _runner = new();
        GameClock _clock;
        DodgeMotion _dodge;
        PlayerVitals _vitals;
        MoveInput _move;
        FollowCamera _camera;
        Func<MotionTarget> _target;
        Func<bool> _held;
        Action<MotionHit> _onHit;
        MotionAnimTable _anims = MotionAnimTable.BuiltIn;
        ActorVisual _visual;
        KinematicMotor _motor;
        string _weaponKey = string.Empty;
        int _verbId;
        float _arena = 50f;
        float _body = 0.5f;
        bool _playing;
        bool _tickedThisFrame;
        bool _stopAfterSample;
        bool _hasPlayClock;
        double _playStartWorldMs;

        public bool IsDisplacing => _playing;
        public float PlayedSec => _runner.Elapsed;
        public float PlayLengthSec { get; private set; }
        public string SkillId { get; private set; } = string.Empty;

        public void Bind(GameClock clock, float arenaHalfM, float bodyRadiusM)
        {
            _clock = clock;
            _arena = arenaHalfM > 1f ? arenaHalfM : 50f;
            // Oynayan kalıbın gövde yarıçapı silah değişiminde yeniden yazılmaz.
            if (_playing)
                return;
            _body = bodyRadiusM > 0f ? bodyRadiusM : 0.5f;
        }

        public void SetAnimContext(MotionAnimTable anims, string weaponKey, int verbId)
        {
            _anims = anims ?? MotionAnimTable.BuiltIn;
            _weaponKey = weaponKey ?? string.Empty;
            _verbId = verbId;
        }

        public void Play(
            MotionTemplate template,
            Func<MotionTarget> target,
            Func<bool> held,
            Action<MotionHit> onHit,
            float bodyRadiusM = 0.5f,
            float stopGapM = 0.15f)
        {
            if (template == null)
                return;
            Vector3 p = transform.position;
            Vector3 f = transform.forward;
            f.y = 0f;
            if (f.sqrMagnitude < 0.0001f)
                f = Vector3.forward;
            if (bodyRadiusM > 0f)
                _body = bodyRadiusM;
            _runner.Begin(template, p.x, p.y, p.z, f.x, f.z, bodyRadiusM, stopGapM);
            _target = target;
            _held = held;
            _onHit = onHit;
            PlayLengthSec = SumLength(template);
            _playing = !_runner.Finished;
            _stopAfterSample = false;
            _hasPlayClock = _clock != null;
            _playStartWorldMs = _hasPlayClock ? _clock.Director.WorldTimeMs : 0;
            _tickedThisFrame = false;
        }

        public void NoteSkill(string skillId) => SkillId = skillId ?? string.Empty;

        public void Stop() => _playing = false;

        static float SumLength(MotionTemplate template)
        {
            if (template == null)
                return 0f;
            float sum = 0f;
            for (int i = 0; i < template.Phases.Count; i++)
                sum += template.Phases[i].DurationSec;
            return sum;
        }

        void Update()
        {
            // Bitiş bayrağı bir sonraki kareye kalır: bu karenin ölçümü (tarama PostLateUpdate)
            // son adımı hâlâ kalıbın yer değiştirmesi olarak görsün. Aynı karede kapatmak
            // 1,41 m'lik adımı ikinci sistem diye yazıyordu.
            if (_stopAfterSample)
            {
                _stopAfterSample = false;
                _playing = false;
                if (_visual == null)
                    _visual = GetComponent<ActorVisual>();
                _visual?.EndMotionAnim();
            }
            TickMotion();
            _tickedThisFrame = true;
        }

        void LateUpdate()
        {
            // Play, Update'ten sonra geldiyse (build ekranı kapanınca ilk cast) bu kare de işlensin.
            if (!_tickedThisFrame)
                TickMotion();
            else if (_playing)
                StampRunner();
            _tickedThisFrame = false;
        }

        /// <summary>
        /// Klip kök hareketi kalıbın üstüne ikinci bir kayma yazmasın. Konum kalıbın koşucusudur.
        /// </summary>
        void StampRunner()
        {
            Vector3 pos = ArenaClamp.XZ(new Vector3(_runner.X, _runner.Y, _runner.Z), _arena, _body);
            pos.y = _runner.Y;
            transform.position = pos;
        }

        void TickMotion()
        {
            if (!_playing)
                return;
            if (_dodge == null)
                _dodge = GetComponent<DodgeMotion>();
            if (_vitals == null)
                _vitals = GetComponent<PlayerVitals>();
            if (_dodge != null && _dodge.IsDisplacing)
            {
                _playing = false;
                _stopAfterSample = false;
                return;
            }
            if (_vitals != null && _vitals.IsDown)
            {
                _playing = false;
                _stopAfterSample = false;
                return;
            }

            if (_runner.Elapsed > PlayLengthSec + 0.05f)
            {
                _stopAfterSample = true;
                return;
            }

            float dt = _clock != null ? (float)(_clock.WorldDeltaMs / 1000.0) : Time.deltaTime;
            if (_hasPlayClock && _clock != null)
            {
                // Tarama saati ile koşucu ayrışırsa kalıp yazar süresinden uzun görünür
                // (Yumruk 2-9: 0,20 sn'lik hamle 0,51 sn oynadı).
                float world = (float)((_clock.Director.WorldTimeMs - _playStartWorldMs) / 1000.0);
                float behind = world - _runner.Elapsed;
                if (behind > dt)
                    dt = behind;
            }
            if (dt <= 0f)
                return;

            MotionTarget target = _target != null ? _target() : default;
            bool held = _held != null && _held();
            Vector3 stick = WorldStick();
            MotionTick tick = _runner.Tick(dt, target, new MotionStick(held, stick.x, stick.z));
            StampRunner();
            // Dönüş hem klibi (AnimKey spin) hem gövde yaw'ını sürer. Yalnız transform
            // döndürmek bacakları dondurup tüm gövdeyi çeviriyordu.
            if (tick.FaceX * tick.FaceX + tick.FaceZ * tick.FaceZ > 0.0001f)
                transform.rotation = Quaternion.LookRotation(new Vector3(tick.FaceX, 0f, tick.FaceZ), Vector3.up);
            DriveLegs(tick);

            if (tick.Hits != null)
            {
                for (int i = 0; i < tick.Hits.Length; i++)
                    _onHit?.Invoke(tick.Hits[i]);
            }

            if (tick.Finished)
                _stopAfterSample = true;
        }

        void DriveLegs(in MotionTick tick)
        {
            if (_visual == null)
                _visual = GetComponent<ActorVisual>();
            if (_visual == null)
                return;
            if (_motor == null)
                _motor = GetComponent<KinematicMotor>();
            float refMps = _motor != null ? _motor.LocoRefMps : 6.4f;
            var blend = LocoBlend.FromVelocity(tick.VelX, tick.VelZ, tick.FaceX, tick.FaceZ, refMps);
            // Kalıp hızı kısa fazda sönümün gerisinde kalmasın; ayak gövdeyle aynı karede eşleşsin.
            float damp = 0f;
            float maxPlayback = LocoBlend.TemplatePlaybackCap;
            _visual.DriveMotion(
                blend, tick.AnimKey, tick.AnimSpeed, tick.Spin,
                _anims, _weaponKey, _verbId, refMps, damp, maxPlayback);
        }

        Vector3 WorldStick()
        {
            if (_move == null)
                _move = GetComponent<MoveInput>();
            if (_move == null)
                return Vector3.zero;
            Vector2 m = _move.MoveDirection;
            var stick = new Vector3(m.x, 0f, m.y);
            if (stick.sqrMagnitude < 0.0001f)
                return Vector3.zero;
            if (stick.sqrMagnitude > 1f)
                stick.Normalize();
            if (_camera == null)
                _camera = FindAnyObjectByType<FollowCamera>();
            if (_camera != null)
                stick = Quaternion.Euler(0f, _camera.MovementYawDeg, 0f) * stick;
            return stick;
        }
    }
}
