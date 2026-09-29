using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public readonly struct MotionTarget
    {
        public MotionTarget(bool has, float x, float z)
        {
            Has = has;
            X = x;
            Z = z;
        }

        public bool Has { get; }
        public float X { get; }
        public float Z { get; }
    }

    public readonly struct MotionStick
    {
        public MotionStick(bool held, float moveX, float moveZ)
        {
            Held = held;
            MoveX = moveX;
            MoveZ = moveZ;
        }

        public bool Held { get; }
        public float MoveX { get; }
        public float MoveZ { get; }
    }

    public readonly struct MotionHit
    {
        public MotionHit(
            string phase,
            string shape,
            string anchor,
            string payload,
            float lengthM,
            float radiusM,
            float share,
            float originX,
            float originY,
            float originZ,
            float dirX,
            float dirZ,
            float timeSec)
        {
            Phase = phase ?? string.Empty;
            Shape = shape ?? string.Empty;
            Anchor = anchor ?? string.Empty;
            Payload = payload ?? string.Empty;
            LengthM = lengthM;
            RadiusM = radiusM;
            Share = share;
            OriginX = originX;
            OriginY = originY;
            OriginZ = originZ;
            DirX = dirX;
            DirZ = dirZ;
            TimeSec = timeSec;
        }

        public string Phase { get; }
        public string Shape { get; }
        public string Anchor { get; }
        public string Payload { get; }
        public float LengthM { get; }
        public float RadiusM { get; }
        public float Share { get; }
        public float OriginX { get; }
        public float OriginY { get; }
        public float OriginZ { get; }
        public float DirX { get; }
        public float DirZ { get; }
        public float TimeSec { get; }
    }

    public readonly struct MotionTick
    {
        public MotionTick(
            float x, float y, float z,
            float faceX, float faceZ,
            bool finished,
            MotionHit[] hits)
        {
            X = x;
            Y = y;
            Z = z;
            FaceX = faceX;
            FaceZ = faceZ;
            Finished = finished;
            Hits = hits ?? Array.Empty<MotionHit>();
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public bool Finished { get; }
        public MotionHit[] Hits { get; }
    }

    /// <summary>
    /// Kalıbın fazlarını dünya konumuna çevirir. Kök yer değiştirme, süre, hedefe yapışma
    /// ve faz vuruşu burada. Unity yok; gövdeyi Game katmanı uygular.
    /// </summary>
    public sealed class MotionTemplateRunner
    {
        readonly List<MotionHit> _hits = new();
        MotionTemplate _template = null!;
        int _phase;
        float _time;
        float _held;
        float _elapsed;
        float _x, _y, _z;
        float _faceX, _faceZ;
        float _phaseX, _phaseY, _phaseZ;
        float _phaseFaceX, _phaseFaceZ;
        float _groundY;
        float _destX, _destZ;
        float _shot;
        float _walkX, _walkZ;
        bool _active;
        bool _finished;
        bool _hitSent;
        int _everySent;

        public bool Finished => _finished;
        public float Elapsed => _elapsed;
        public float X => _x;
        public float Y => _y;
        public float Z => _z;
        public float FaceX => _faceX;
        public float FaceZ => _faceZ;

        public void Begin(MotionTemplate template, float x, float y, float z, float faceX, float faceZ)
        {
            _template = template;
            _phase = 0;
            _time = 0f;
            _held = 0f;
            _elapsed = 0f;
            _x = x;
            _y = y;
            _z = z;
            _groundY = y;
            _walkX = 0f;
            _walkZ = 0f;
            _shot = 0f;
            _finished = template == null || template.Phases.Count == 0;
            _active = !_finished;
            Normalize(faceX, faceZ, out _faceX, out _faceZ);
            if (_active)
                EnterPhase();
        }

        public MotionTick Tick(float dt, in MotionTarget target, in MotionStick stick)
        {
            _hits.Clear();
            if (!_active || _finished || dt <= 0f || _template == null)
                return Capture();

            float left = dt;
            int guard = 0;
            while (left > 0.00001f && !_finished && guard++ < 12)
            {
                MotionPhase phase = _template.Phases[_phase];
                AimDest(phase, target);
                float dur = Math.Max(0.01f, phase.DurationSec);
                if (phase.Gate == "release")
                {
                    if (_time < dur)
                    {
                        float step = Math.Min(left, dur - _time);
                        Advance(phase, step, dur, target, stick);
                        left -= step;
                        if (_time < dur - 0.00001f)
                            break;
                    }

                    if (stick.Held && _held < phase.MaxHoldSec)
                    {
                        float holdStep = Math.Min(left, phase.MaxHoldSec - _held);
                        _held += holdStep;
                        _elapsed += holdStep;
                        left -= holdStep;
                        Pose(phase, 1f, 1f, target, stick, 0f);
                        if (stick.Held && _held < phase.MaxHoldSec - 0.0001f)
                            break;
                    }

                    NextPhase();
                    continue;
                }

                float room = dur - _time;
                if (room > 0.00001f)
                {
                    float step = Math.Min(left, room);
                    Advance(phase, step, dur, target, stick);
                    left -= step;
                }

                if (_time >= dur - 0.0001f)
                    NextPhase();
                else
                    break;
            }

            return Capture();
        }

        void Advance(MotionPhase phase, float step, float dur, in MotionTarget target, in MotionStick stick)
        {
            float prev = _time;
            _time += step;
            _elapsed += step;
            float u1 = Math.Clamp(_time / dur, 0f, 1f);
            float shaped = Curve(phase.Curve, u1);
            Pose(phase, u1, shaped, target, stick, step);
            Emit(phase, prev, _time, dur, target);
        }

        void Pose(
            MotionPhase phase,
            float uLinear,
            float u,
            in MotionTarget target,
            in MotionStick stick,
            float dt)
        {
            Axis(phase, target, out float fx, out float fz);
            float rx = fz;
            float rz = -fx;
            float side = phase.Side < 0f ? -1f : 1f;
            _shot = u * phase.ShotM;

            switch (phase.Motion)
            {
                case "lunge":
                case "dash":
                    _x = _phaseX + (_destX - _phaseX) * u;
                    _z = _phaseZ + (_destZ - _phaseZ) * u;
                    _y = _groundY;
                    break;
                case "retreat":
                    _x = _phaseX - fx * (u * phase.DistanceM);
                    _z = _phaseZ - fz * (u * phase.DistanceM);
                    _y = _groundY;
                    break;
                case "sidestep":
                    _x = _phaseX + rx * side * u * phase.DistanceM + fx * u * phase.ForwardM;
                    _z = _phaseZ + rz * side * u * phase.DistanceM + fz * u * phase.ForwardM;
                    _y = _groundY;
                    break;
                case "hop":
                    _x = _phaseX + rx * side * u * phase.DistanceM + fx * u * phase.ForwardM;
                    _z = _phaseZ + rz * side * u * phase.DistanceM + fz * u * phase.ForwardM;
                    _y = _groundY + 4f * phase.HeightM * uLinear * (1f - uLinear);
                    break;
                case "leap":
                    _x = _phaseX + (_destX - _phaseX) * u;
                    _z = _phaseZ + (_destZ - _phaseZ) * u;
                    _y = _groundY + 4f * phase.HeightM * uLinear * (1f - uLinear);
                    break;
                case "slam":
                    _x = _phaseX + (_destX - _phaseX) * u;
                    _z = _phaseZ + (_destZ - _phaseZ) * u;
                    _y = _groundY + phase.HeightM * (1f - uLinear);
                    break;
                case "pull":
                    _x = _phaseX + (_destX - _phaseX) * u;
                    _z = _phaseZ + (_destZ - _phaseZ) * u;
                    _y = _groundY;
                    break;
                case "spin":
                case "fan":
                    _x = _phaseX;
                    _z = _phaseZ;
                    _y = _groundY;
                    break;
                case "throw":
                    _x = _phaseX;
                    _z = _phaseZ;
                    _y = _groundY;
                    break;
                case "hover":
                    _x = _phaseX;
                    _z = _phaseZ;
                    _y = _groundY + phase.HeightM;
                    break;
                case "blink":
                    if (uLinear >= phase.SnapAt)
                    {
                        _x = _destX;
                        _z = _destZ;
                    }
                    else
                    {
                        _x = _phaseX;
                        _z = _phaseZ;
                    }
                    _y = _groundY;
                    break;
                case "channel":
                    _walkX += stick.MoveX * phase.WalkMps * dt;
                    _walkZ += stick.MoveZ * phase.WalkMps * dt;
                    _x = _phaseX + fx * u * phase.DriftM + _walkX;
                    _z = _phaseZ + fz * u * phase.DriftM + _walkZ;
                    _y = _groundY;
                    break;
                default:
                    if (phase.Motion != "hold")
                    {
                        DesignWarnings.Once(
                            "motion.kind." + phase.Motion,
                            "Bilinmeyen hareket: " + phase.Motion + ". Yerinde duruluyor.");
                    }
                    _x = _phaseX;
                    _z = _phaseZ;
                    _y = _groundY;
                    break;
            }

            ApplyFacing(phase, u, fx, fz, target);
        }

        void ApplyFacing(MotionPhase phase, float u, float fx, float fz, in MotionTarget target)
        {
            if (phase.Motion is "spin" or "fan")
            {
                float yaw = u * phase.YawDeg * (MathF.PI / 180f);
                float c = MathF.Cos(yaw);
                float s = MathF.Sin(yaw);
                _faceX = _phaseFaceX * c + _phaseFaceZ * s;
                _faceZ = -_phaseFaceX * s + _phaseFaceZ * c;
                Normalize(_faceX, _faceZ, out _faceX, out _faceZ);
                return;
            }

            if (phase.Facing == "target" && target.Has)
            {
                float dx = target.X - _x;
                float dz = target.Z - _z;
                if (dx * dx + dz * dz > 0.04f)
                {
                    Normalize(dx, dz, out _faceX, out _faceZ);
                    return;
                }
            }

            if (phase.Facing == "travel")
            {
                float dx = _x - _phaseX;
                float dz = _z - _phaseZ;
                if (dx * dx + dz * dz > 0.0004f)
                {
                    Normalize(dx, dz, out _faceX, out _faceZ);
                    return;
                }
            }

            if (phase.Facing == "hold")
            {
                _faceX = _phaseFaceX;
                _faceZ = _phaseFaceZ;
                return;
            }

            _faceX = fx;
            _faceZ = fz;
        }

        void Emit(MotionPhase phase, float prev, float now, float dur, in MotionTarget target)
        {
            MotionHitSpec hit = phase.Hit;
            if (hit == null)
                return;

            if (hit.EverySec > 0.01f)
            {
                float first = Math.Clamp(hit.At, 0f, 1f) * dur;
                for (int n = _everySent; n < 16; n++)
                {
                    float t = first + n * hit.EverySec;
                    if (t > dur + 0.0001f)
                        break;
                    if (prev < t && now >= t - 0.00001f)
                    {
                        _everySent = n + 1;
                        Push(phase, hit, target);
                    }
                }
                return;
            }

            if (_hitSent)
                return;
            float at = Math.Clamp(hit.At, 0f, 1f) * dur;
            if (prev < at && now >= at - 0.0001f)
            {
                _hitSent = true;
                Push(phase, hit, target);
            }
        }

        void Push(MotionPhase phase, MotionHitSpec hit, in MotionTarget target)
        {
            float ox = _x;
            float oz = _z;
            float dx = _faceX;
            float dz = _faceZ;
            float rx = dz;
            float rz = -dx;
            float side = phase.Side < 0f ? -1f : 1f;
            switch (hit.Anchor)
            {
                case "target":
                    if (target.Has)
                    {
                        ox = target.X;
                        oz = target.Z;
                    }
                    break;
                case "behind":
                    if (target.Has)
                    {
                        ox = target.X + dx * phase.BehindM;
                        oz = target.Z + dz * phase.BehindM;
                    }
                    else
                    {
                        ox = _x + dx * phase.BehindM;
                        oz = _z + dz * phase.BehindM;
                    }
                    break;
                case "side":
                    ox = _x + rx * side * Math.Max(phase.SideM, phase.DistanceM);
                    oz = _z + rz * side * Math.Max(phase.SideM, phase.DistanceM);
                    break;
                case "shot":
                    ox = _x + dx * Math.Max(_shot, 0.01f);
                    oz = _z + dz * Math.Max(_shot, 0.01f);
                    break;
                case "self":
                case "ring":
                    break;
                default:
                    ox = _x + dx * hit.LengthM * 0.35f;
                    oz = _z + dz * hit.LengthM * 0.35f;
                    break;
            }

            _hits.Add(new MotionHit(
                phase.Name,
                hit.Shape,
                hit.Anchor,
                hit.Payload,
                hit.LengthM,
                hit.RadiusM,
                hit.Share,
                ox,
                _y,
                oz,
                dx,
                dz,
                _elapsed));
        }

        void EnterPhase()
        {
            _time = 0f;
            _held = 0f;
            _hitSent = false;
            _everySent = 0;
            _walkX = 0f;
            _walkZ = 0f;
            _shot = 0f;
            _phaseX = _x;
            _phaseY = _y;
            _phaseZ = _z;
            _phaseFaceX = _faceX;
            _phaseFaceZ = _faceZ;
        }

        void NextPhase()
        {
            _phase++;
            if (_template == null || _phase >= _template.Phases.Count)
            {
                _finished = true;
                _active = false;
                return;
            }
            EnterPhase();
        }

        void AimDest(MotionPhase phase, in MotionTarget target)
        {
            Axis(phase, target, out float fx, out float fz);
            _destX = _phaseX + fx * phase.DistanceM;
            _destZ = _phaseZ + fz * phase.DistanceM;
            if (!target.Has)
                return;

            float dx = target.X - _phaseX;
            float dz = target.Z - _phaseZ;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 0.05f)
                return;
            if (phase.Homing != "track")
                return;
            float ux = dx / len;
            float uz = dz / len;
            switch (phase.Motion)
            {
                case "dash":
                    _destX = _phaseX + ux * (len + phase.OvershootM);
                    _destZ = _phaseZ + uz * (len + phase.OvershootM);
                    break;
                case "leap":
                case "pull":
                    float land = Math.Max(0.15f, len - phase.GapM);
                    _destX = _phaseX + ux * land;
                    _destZ = _phaseZ + uz * land;
                    break;
                case "lunge":
                    float reach = Math.Min(phase.DistanceM, Math.Max(0.2f, len - 0.45f));
                    _destX = _phaseX + ux * reach;
                    _destZ = _phaseZ + uz * reach;
                    break;
                case "blink":
                    _destX = target.X + ux * phase.BehindM;
                    _destZ = target.Z + uz * phase.BehindM;
                    break;
                case "slam":
                    _destX = _phaseX + ux * phase.DistanceM;
                    _destZ = _phaseZ + uz * phase.DistanceM;
                    break;
            }
        }

        void Axis(MotionPhase phase, in MotionTarget target, out float fx, out float fz)
        {
            if (phase.Homing == "track" && target.Has)
            {
                float dx = target.X - _phaseX;
                float dz = target.Z - _phaseZ;
                if (dx * dx + dz * dz > 0.04f)
                {
                    Normalize(dx, dz, out fx, out fz);
                    return;
                }
            }
            fx = _phaseFaceX;
            fz = _phaseFaceZ;
        }

        MotionTick Capture() =>
            new MotionTick(_x, _y, _z, _faceX, _faceZ, _finished, _hits.ToArray());

        static float Curve(float[] curve, float u)
        {
            if (curve == null || curve.Length == 0)
                return u;
            if (curve.Length == 1)
                return curve[0];
            float t = Math.Clamp(u, 0f, 1f) * (curve.Length - 1);
            int i = (int)t;
            if (i >= curve.Length - 1)
                return curve[curve.Length - 1];
            float f = t - i;
            return curve[i] + (curve[i + 1] - curve[i]) * f;
        }

        static void Normalize(float x, float z, out float ox, out float oz)
        {
            float len = MathF.Sqrt(x * x + z * z);
            if (len < 0.0001f)
            {
                ox = 0f;
                oz = 1f;
                return;
            }
            ox = x / len;
            oz = z / len;
        }
    }
}
