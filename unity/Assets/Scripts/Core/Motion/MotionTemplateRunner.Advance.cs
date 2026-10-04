using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public sealed partial class MotionTemplateRunner
    {
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
            if (EmiciApproach.Freezes(phase, target.HoldApproach))
            {
                if (!_yieldApproach)
                {
                    _yieldApproach = true;
                    _yieldX = _x;
                    _yieldZ = _z;
                }
                _x = _yieldX;
                _z = _yieldZ;
                // Çekme donunca da kök zemin yüksekliğinde kalır. Mutlak 0 kapsülü gömer.
                _y = _groundY;
                _shot = u * phase.ShotM;
                ApplyFacing(phase, u, fx, fz, target);
                return;
            }

            if (_blendU0 >= 0f)
            {
                float span = 1f - _blendU0;
                u = span < MotionTemplateRunnerDefaults.MinRadiusM ? 0f : Math.Clamp((uLinear - _blendU0) / span, 0f, 1f);
            }

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
                    KeepOutside(target, ref _x, ref _z);
                    break;
                case "sidestep":
                    _x = _phaseX + rx * side * u * phase.DistanceM + fx * u * phase.ForwardM;
                    _z = _phaseZ + rz * side * u * phase.DistanceM + fz * u * phase.ForwardM;
                    _y = _groundY;
                    break;
                case "hop":
                    if (phase.Land == "behind")
                        ArcAround(u, side, target);
                    else
                    {
                        _x = _phaseX + rx * side * u * phase.DistanceM + fx * u * phase.ForwardM;
                        _z = _phaseZ + rz * side * u * phase.DistanceM + fz * u * phase.ForwardM;
                    }
                    _y = _groundY + MotionTemplateRunnerDefaults.JumpHeightFourMult * phase.HeightM * uLinear * (1f - uLinear);
                    break;
                case "leap":
                    if (phase.Land == "behind")
                        ArcAround(u, side, target);
                    else
                    {
                        _x = _phaseX + (_destX - _phaseX) * u;
                        _z = _phaseZ + (_destZ - _phaseZ) * u;
                    }
                    _y = _groundY + MotionTemplateRunnerDefaults.JumpHeightFourMult * phase.HeightM * uLinear * (1f - uLinear);
                    break;
                case "slam":
                    _x = _phaseX + (_destX - _phaseX) * u;
                    _z = _phaseZ + (_destZ - _phaseZ) * u;
                    _y = _groundY + phase.HeightM * (1f - uLinear);
                    break;
                case "pull":
                    PullTravel(u, target);
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
                        _snapped = true;
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
                    _y = _groundY + (phase.HeightM > MotionTemplateRunnerDefaults.MinDistM ? phase.HeightM : 0f);
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

            // Yükseklik yalnız havadaki fazın eğrisinden gelir. Çekme dahil yer fazları zemin kökünde.
            _y = _groundY + phase.HeightAboveGround(uLinear);

            // Arkaya iniş ve içinden geçiş kasıtlı olarak gövdeyi keser; bitiş yine dışarıdadır.
            // Atış / tutma ayakları yerinden kaldırmaz. Kenar itmesi boss merkeze gelince
            // oyuncuyu karşı yüze ışınlıyordu (5-2, 10-2, 11-2, 12-2).
            bool crossesBody = phase.Land == "behind" || phase.OvershootM > MotionTemplateRunnerDefaults.MinDistM;
            if (!crossesBody && !FeetPlanted(phase))
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
                if (dx * dx + dz * dz > MotionTemplateRunnerDefaults.StepDistSqrMin)
                {
                    Normalize(dx, dz, out _faceX, out _faceZ);
                    return;
                }
            }

            if (phase.Facing == "travel")
            {
                float dx = _x - _phaseX;
                float dz = _z - _phaseZ;
                if (dx * dx + dz * dz > MotionTemplateRunnerDefaults.FacingDistSqrMin)
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

            if (hit.EverySec > MotionTemplateRunnerDefaults.MinDistM)
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
            // Yelpaze/dönüş gövdeyi çevirir; vuruş fazın başındaki nişanda kalır.
            if (phase.Motion is "fan" or "spin")
            {
                dx = _phaseFaceX;
                dz = _phaseFaceZ;
            }
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
                    float lateral = MotionTemplateRunnerDefaults.LateralFrac;
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
                    float shot = Math.Max(_shot, MotionTemplateRunnerDefaults.MinDistM);
                    ox = _x + dx * shot;
                    oz = _z + dz * shot;
                    if (target.Has)
                    {
                        float along = (target.X - _x) * dx + (target.Z - _z) * dz;
                        if (along >= 0f && along <= shot)
                        {
                            ox = _x + dx * along;
                            oz = _z + dz * along;
                        }
                    }
                    break;
                case "self":
                case "ring":
                    break;
                default:
                    ox = _x + dx * hit.LengthM * MotionTemplateRunnerDefaults.LateralFrac;
                    oz = _z + dz * hit.LengthM * MotionTemplateRunnerDefaults.LateralFrac;
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
    }
}
