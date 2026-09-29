using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public readonly struct MotionTarget
    {
        public MotionTarget(bool has, float x, float z, float radiusM = 0f)
        {
            Has = has;
            X = x;
            Z = z;
            RadiusM = Math.Max(0f, radiusM);
        }

        public bool Has { get; }
        public float X { get; }
        public float Z { get; }
        /// <summary>Hedef collider yarıçapı. 0 ise kenar payı yalnız saldıran gövdesinden gelir.</summary>
        public float RadiusM { get; }
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
            MotionHit[] hits,
            string anim = "",
            float animSpeed = 1f,
            float velX = 0f,
            float velZ = 0f,
            bool spin = false)
        {
            X = x;
            Y = y;
            Z = z;
            FaceX = faceX;
            FaceZ = faceZ;
            Finished = finished;
            Hits = hits ?? Array.Empty<MotionHit>();
            AnimKey = anim ?? string.Empty;
            AnimSpeed = animSpeed > 0.05f ? animSpeed : 1f;
            VelX = velX;
            VelZ = velZ;
            Spin = spin;
        }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float FaceX { get; }
        public float FaceZ { get; }
        public bool Finished { get; }
        public MotionHit[] Hits { get; }
        public string AnimKey { get; }
        public float AnimSpeed { get; }
        public float VelX { get; }
        public float VelZ { get; }
        /// <summary>Dönüş klibi ve gövde yaw'ı birlikte sürer.</summary>
        public bool Spin { get; }
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
        float _bodyRadius = 0.5f;
        float _stopGap = 0.15f;
        float _plantX, _plantZ;
        float _markX, _markZ;
        bool _plantSent;
        MotionTarget _lastTarget;
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
        /// <summary>Dönüş fazı varsa işaret, cast'in başladığı yerdir.</summary>
        public bool ReturnMarkPlaced { get; private set; }
        public float MarkX => _markX;
        public float MarkZ => _markZ;

        public void Begin(
            MotionTemplate template,
            float x, float y, float z,
            float faceX, float faceZ,
            float bodyRadiusM = 0.5f,
            float stopGapM = 0.15f)
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
            _bodyRadius = Math.Max(0f, bodyRadiusM);
            _stopGap = Math.Max(0f, stopGapM);
            _plantSent = false;
            _plantX = x;
            _plantZ = z;
            _markX = x;
            _markZ = z;
            ReturnMarkPlaced = false;
            if (template != null)
            {
                for (int i = 0; i < template.Phases.Count; i++)
                {
                    if (template.Phases[i].Motion == "return")
                    {
                        ReturnMarkPlaced = true;
                        break;
                    }
                }
            }
            _lastTarget = default;
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
                return Capture(0f, 0f);

            float x0 = _x;
            float z0 = _z;
            _lastTarget = target;
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

            float inv = 1f / dt;
            return Capture((_x - x0) * inv, (_z - z0) * inv);
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
                case "return":
                    _x = _phaseX + (_destX - _phaseX) * u;
                    _z = _phaseZ + (_destZ - _phaseZ) * u;
                    _y = _groundY;
                    KeepOutside(target, ref _x, ref _z);
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
                    if (phase.Land == "behind")
                    {
                        _x = _phaseX + (_destX - _phaseX) * u;
                        _z = _phaseZ + (_destZ - _phaseZ) * u;
                    }
                    else
                    {
                        _x = _phaseX + rx * side * u * phase.DistanceM + fx * u * phase.ForwardM;
                        _z = _phaseZ + rz * side * u * phase.DistanceM + fz * u * phase.ForwardM;
                    }
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
                    _y = _groundY + (phase.HeightM > 0.01f ? phase.HeightM : 0f);
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

            // Arkaya iniş ve içinden geçiş kasıtlı olarak gövdeyi keser; bitiş yine dışarıdadır.
            // Diğer fazlar her karede kenarın dışında tutulur.
            bool crossesBody = phase.Land == "behind" || phase.OvershootM > 0.01f;
            if (!crossesBody)
                KeepOutside(target, ref _x, ref _z);

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
                        ox = target.X;
                        oz = target.Z;
                    }
                    break;
                case "side":
                    ox = _x + rx * side * Math.Max(phase.SideM, phase.DistanceM);
                    oz = _z + rz * side * Math.Max(phase.SideM, phase.DistanceM);
                    break;
                case "target_side":
                    // Sekme vuruşu hedefin yanında durur; kaymış gövdenin bir yan mesafe daha dışına kaçmaz.
                    float lateral = 0.35f;
                    if (target.Has)
                    {
                        ox = target.X + rx * side * lateral;
                        oz = target.Z + rz * side * lateral;
                    }
                    else
                    {
                        ox = _x + rx * side * lateral;
                        oz = _z + rz * side * lateral;
                    }
                    break;
                case "plant":
                    ox = _plantX;
                    oz = _plantZ;
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
            if (_template != null && _phase >= 0 && _phase < _template.Phases.Count)
                RememberPlant(_template.Phases[_phase], _lastTarget);
            _phase++;
            if (_template == null || _phase >= _template.Phases.Count)
            {
                _finished = true;
                _active = false;
                return;
            }
            EnterPhase();
        }

        void RememberPlant(MotionPhase phase, in MotionTarget target)
        {
            if (!phase.Plant || _plantSent)
                return;
            float fx = _faceX;
            float fz = _faceZ;
            if (target.Has)
            {
                float dx = target.X - _x;
                float dz = target.Z - _z;
                float len = MathF.Sqrt(dx * dx + dz * dz);
                if (len > 0.05f)
                {
                    fx = dx / len;
                    fz = dz / len;
                }
                // Fitil, o andaki hedefin yakın kenarına çakılır; boss sonra yürürse yerinde kalır.
                _plantX = target.X - fx * target.RadiusM;
                _plantZ = target.Z - fz * target.RadiusM;
            }
            else
            {
                _plantX = _x;
                _plantZ = _z;
            }
            _plantSent = true;
            _hits.Add(new MotionHit(
                phase.Name,
                "sphere",
                "plant",
                "marker",
                0.25f,
                0.2f,
                0f,
                _plantX,
                _groundY,
                _plantZ,
                fx,
                fz,
                _elapsed));
        }

        void AimDest(MotionPhase phase, in MotionTarget target)
        {
            if (phase.Motion == "return")
            {
                _destX = _markX;
                _destZ = _markZ;
                KeepOutside(target, ref _destX, ref _destZ);
                return;
            }

            Axis(phase, target, out float fx, out float fz);
            _destX = _phaseX + fx * phase.DistanceM;
            _destZ = _phaseZ + fz * phase.DistanceM;
            if (!target.Has)
                return;

            float dx = target.X - _phaseX;
            float dz = target.Z - _phaseZ;
            float len = MathF.Sqrt(dx * dx + dz * dz);
            if (len < 0.001f)
                return;
            float ux = dx / len;
            float uz = dz / len;
            if (phase.Land == "behind")
            {
                float behind = Separation(target);
                _destX = target.X + ux * behind;
                _destZ = target.Z + uz * behind;
                return;
            }
            if (phase.Homing != "track")
                return;
            switch (phase.Motion)
            {
                case "dash":
                    if (phase.OvershootM > 0.01f)
                    {
                        float through = len + target.RadiusM + _bodyRadius + _stopGap + phase.OvershootM;
                        _destX = _phaseX + ux * through;
                        _destZ = _phaseZ + uz * through;
                    }
                    else
                        Approach(ux, uz, len, target, cap: 0f);
                    break;
                case "leap":
                case "pull":
                    Approach(ux, uz, len, target, cap: 0f);
                    break;
                case "lunge":
                    Approach(ux, uz, len, target, cap: phase.DistanceM);
                    break;
                case "blink":
                    // Arkaya iniş yukarıda ayrıldı. Düz ışınlanma hedefin yakın kenarında biter.
                    Approach(ux, uz, len, target, cap: 0f);
                    break;
                case "slam":
                    Approach(ux, uz, len, target, cap: phase.DistanceM);
                    break;
            }

            if (phase.OvershootM <= 0.01f)
                KeepOutside(target, ref _destX, ref _destZ);
        }

        /// <summary>Merkezler, saldıran kenarı + pay + hedef kenarı kadar ayrı durur. İçeri girilmez.</summary>
        void Approach(float ux, float uz, float len, in MotionTarget target, float cap)
        {
            float travel = Math.Max(0f, len - Separation(target));
            if (cap > 0.01f)
                travel = Math.Min(cap, travel);
            _destX = _phaseX + ux * travel;
            _destZ = _phaseZ + uz * travel;
        }

        float Separation(in MotionTarget target) =>
            _bodyRadius + target.RadiusM + _stopGap;

        /// <summary>Dönüş atılması hedefin içine inmez; işaret dışarıdaysa işaret kalır.</summary>
        void KeepOutside(in MotionTarget target, ref float x, ref float z)
        {
            if (!target.Has)
                return;
            float dx = x - target.X;
            float dz = z - target.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            float min = Separation(target);
            if (dist >= min)
                return;
            if (dist < 0.001f)
            {
                x = target.X - _faceX * min;
                z = target.Z - _faceZ * min;
                return;
            }
            float scale = min / dist;
            x = target.X + dx * scale;
            z = target.Z + dz * scale;
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

        MotionTick Capture(float velX, float velZ)
        {
            string anim = string.Empty;
            float speed = 1f;
            bool spin = false;
            if (_template != null && _template.Phases.Count > 0)
            {
                int index = _phase;
                if (index < 0 || index >= _template.Phases.Count)
                    index = _template.Phases.Count - 1;
                MotionPhase phase = _template.Phases[index];
                anim = string.IsNullOrEmpty(phase.Anim)
                    ? MotionAnimTable.FallbackKey(phase.Motion)
                    : phase.Anim;
                speed = phase.AnimSpeed;
                spin = phase.Motion is "spin" or "fan" || anim == "spin";
            }

            return new MotionTick(
                _x, _y, _z, _faceX, _faceZ, _finished, _hits.ToArray(),
                anim, speed, velX, velZ, spin);
        }

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
