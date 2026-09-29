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

        public bool IsDisplacing => _playing;

        public void Bind(GameClock clock, float arenaHalfM, float bodyRadiusM)
        {
            _clock = clock;
            _arena = arenaHalfM > 1f ? arenaHalfM : 50f;
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
            _runner.Begin(template, p.x, p.y, p.z, f.x, f.z, bodyRadiusM, stopGapM);
            _target = target;
            _held = held;
            _onHit = onHit;
            _playing = !_runner.Finished;
            _tickedThisFrame = false;
        }

        public void Stop() => _playing = false;

        void Update()
        {
            TickMotion();
            _tickedThisFrame = true;
        }

        void LateUpdate()
        {
            // Play, Update'ten sonra geldiyse (build ekranı kapanınca ilk cast) bu kare de işlensin.
            if (!_tickedThisFrame)
                TickMotion();
            _tickedThisFrame = false;
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
                return;
            }
            if (_vitals != null && _vitals.IsDown)
            {
                _playing = false;
                return;
            }

            float dt = _clock != null ? (float)(_clock.WorldDeltaMs / 1000.0) : Time.deltaTime;
            if (dt <= 0f)
                return;

            MotionTarget target = _target != null ? _target() : default;
            bool held = _held != null && _held();
            Vector3 stick = WorldStick();
            MotionTick tick = _runner.Tick(dt, target, new MotionStick(held, stick.x, stick.z));
            Vector3 pos = ArenaClamp.XZ(new Vector3(tick.X, tick.Y, tick.Z), _arena, _body);
            pos.y = tick.Y;
            transform.position = pos;
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
            {
                _playing = false;
                if (_visual == null)
                    _visual = GetComponent<ActorVisual>();
                _visual?.EndMotionAnim();
            }
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
