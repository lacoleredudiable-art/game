using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public sealed partial class MotionTemplateRunner
    {
        void EnterPhase()
        {
            _time = 0f;
            _held = 0f;
            _hitSent = false;
            _everySent = 0;
            _blendU0 = -1f;
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
                if (len > MotionTemplateRunnerDefaults.MinRadiusM)
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
                MotionTemplateRunnerDefaults.PlantHitRadiusM,
                MotionTemplateRunnerDefaults.PlantHitLengthM,
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
                // Arkaya iniş, cast'in başladığı tarafa göredir. Ara sıçrama boss'u
                // geçse de varış ön yüze dönmez. Silah çarpanı bu yönü değiştirmez.
                float ox = _markX;
                float oz = _markZ;
                float bx = target.X - ox;
                float bz = target.Z - oz;
                float blen = MathF.Sqrt(bx * bx + bz * bz);
                float bux = ux;
                float buz = uz;
                if (blen > MotionTemplateRunnerDefaults.MinRadiusM)
                {
                    bux = bx / blen;
                    buz = bz / blen;
                }
                float behind = Separation(target);
                _destX = target.X + bux * behind;
                _destZ = target.Z + buz * behind;
                return;
            }
            if (phase.Motion == "pull")
            {
                // Dost kancası hedefin yakın kenarında biter. distance_m burayı kısaltmaz:
                // 40 m ile 2 m aynı temasta durur (6-1 sinir noktası da bu kovalamacayı kullanır).
                Approach(ux, uz, len, target, cap: 0f);
                ClampPullDest(target);
                return;
            }
            if (phase.Homing != "track")
            {
                // overshoot_m yoksa düz hamle gövdeyi kesmez. Kenar itmesi merkezi geçince
                // oyuncuyu tek karede karşı yüze atıyordu (3-7, uzatılmış dash).
                if (phase.Motion is "dash" or "lunge" && phase.OvershootM <= MotionTemplateRunnerDefaults.MinDistM)
                    StopAtBodyEdge(target);
                return;
            }
            switch (phase.Motion)
            {
                case "dash":
                    if (phase.OvershootM > MotionTemplateRunnerDefaults.MinDistM)
                    {
                        float through = len + target.RadiusM + _bodyRadius + _stopGap + phase.OvershootM;
                        _destX = _phaseX + ux * through;
                        _destZ = _phaseZ + uz * through;
                    }
                    else
                        Approach(ux, uz, len, target, cap: phase.DistanceM);
                    break;
                case "leap":
                    Approach(ux, uz, len, target, cap: 0f);
                    break;
                case "lunge":
                    Approach(ux, uz, len, target, cap: phase.DistanceM);
                    break;
                case "blink":
                    // Menzil gövdeye yetiyorsa yakın kenarda durmak yerine öte kenardan çık.
                    // Ara kare yok: ışınlanma tek anda iner, merkezde kare bırakmaz.
                    if (phase.DistanceM + MotionTemplateRunnerDefaults.MinRadiusM >= len)
                    {
                        float exit = Separation(target);
                        _destX = target.X + ux * exit;
                        _destZ = target.Z + uz * exit;
                    }
                    else
                        Approach(ux, uz, len, target, cap: 0f);
                    break;
                case "slam":
                    Approach(ux, uz, len, target, cap: phase.DistanceM);
                    break;
            }

            if (phase.OvershootM <= MotionTemplateRunnerDefaults.MinDistM)
                KeepOutside(target, ref _destX, ref _destZ);
        }

        /// <summary>Merkezler, saldıran kenarı + pay + hedef kenarı kadar ayrı durur. İçeri girilmez.</summary>
        void Approach(float ux, float uz, float len, in MotionTarget target, float cap)
        {
            float travel = Math.Max(0f, len - Separation(target));
            if (cap > MotionTemplateRunnerDefaults.MinDistM)
                travel = Math.Min(cap, travel);
            _destX = _phaseX + ux * travel;
            _destZ = _phaseZ + uz * travel;
        }

        /// <summary>
        /// Arkaya iniş düz çizgiyle merkezden geçer. Yol, hedefin çevresinde kenar payının
        /// dışında bir yay çizer; bitiş yine öte kenardadır.
        /// </summary>
        void ArcAround(float u, float side, in MotionTarget target)
        {
            if (!target.Has)
            {
                _x = _phaseX + (_destX - _phaseX) * u;
                _z = _phaseZ + (_destZ - _phaseZ) * u;
                return;
            }

            float sx = _phaseX - target.X;
            float sz = _phaseZ - target.Z;
            float ex = _destX - target.X;
            float ez = _destZ - target.Z;
            float r0 = MathF.Sqrt(sx * sx + sz * sz);
            float r1 = MathF.Sqrt(ex * ex + ez * ez);
            float minR = Separation(target);
            if (r0 < MotionTemplateRunnerDefaults.MinRadiusM)
                r0 = minR;
            if (r1 < MotionTemplateRunnerDefaults.MinRadiusM)
                r1 = minR;
            float a0 = MathF.Atan2(sz, sx);
            float a1 = MathF.Atan2(ez, ex);
            float delta = a1 - a0;
            while (delta > MathF.PI)
                delta -= 2f * MathF.PI;
            while (delta < -MathF.PI)
                delta += 2f * MathF.PI;
            if (MathF.Abs(MathF.Abs(delta) - MathF.PI) < MotionTemplateRunnerDefaults.LateralFrac)
                delta = (side < 0f ? -1f : 1f) * MathF.PI;
            float angle = a0 + delta * u;
            float radius = MathF.Max(minR, r0 + (r1 - r0) * u);
            _x = target.X + MathF.Cos(angle) * radius;
            _z = target.Z + MathF.Sin(angle) * radius;
        }

        void PullTravel(float u, in MotionTarget target)
        {
            if (target.HasObstacle && SegmentTooClose(target))
                ArcObstacle(u, target);
            else
            {
                _x = _phaseX + (_destX - _phaseX) * u;
                _z = _phaseZ + (_destZ - _phaseZ) * u;
            }
        }

        void ClampPullDest(in MotionTarget target)
        {
            if (!target.HasObstacle)
                return;
            float clear = _bodyRadius + target.ObstacleRadiusM + _stopGap;
            float dx = _destX - target.ObstacleX;
            float dz = _destZ - target.ObstacleZ;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            if (dist >= clear)
                return;
            float px = _phaseX - target.ObstacleX;
            float pz = _phaseZ - target.ObstacleZ;
            float plen = MathF.Sqrt(px * px + pz * pz);
            if (plen < 0.001f)
            {
                px = -_faceX;
                pz = -_faceZ;
                plen = MathF.Max(0.001f, MathF.Sqrt(px * px + pz * pz));
            }
            _destX = target.ObstacleX + px / plen * clear;
            _destZ = target.ObstacleZ + pz / plen * clear;
        }

        bool SegmentTooClose(in MotionTarget target)
        {
            float clear = _bodyRadius + target.ObstacleRadiusM + _stopGap;
            return PointSegment(
                target.ObstacleX, target.ObstacleZ,
                _phaseX, _phaseZ, _destX, _destZ) < clear - MotionTemplateRunnerDefaults.ClearDistEpsilonM;
        }

        void ArcObstacle(float u, in MotionTarget target)
        {
            float clear = _bodyRadius + target.ObstacleRadiusM + _stopGap;
            float sx = _phaseX - target.ObstacleX;
            float sz = _phaseZ - target.ObstacleZ;
            float ex = _destX - target.ObstacleX;
            float ez = _destZ - target.ObstacleZ;
            float r0 = MathF.Sqrt(sx * sx + sz * sz);
            float r1 = MathF.Sqrt(ex * ex + ez * ez);
            if (r0 < MotionTemplateRunnerDefaults.MinRadiusM)
                r0 = clear;
            if (r1 < MotionTemplateRunnerDefaults.MinRadiusM)
                r1 = clear;
            float a0 = MathF.Atan2(sz, sx);
            float a1 = MathF.Atan2(ez, ex);
            float delta = a1 - a0;
            while (delta > MathF.PI)
                delta -= 2f * MathF.PI;
            while (delta < -MathF.PI)
                delta += 2f * MathF.PI;
            if (MathF.Abs(MathF.Abs(delta) - MathF.PI) < MotionTemplateRunnerDefaults.LateralFrac)
                delta = MathF.PI;
            float angle = a0 + delta * u;
            float radius = MathF.Max(clear, r0 + (r1 - r0) * u);
            _x = target.ObstacleX + MathF.Cos(angle) * radius;
            _z = target.ObstacleZ + MathF.Sin(angle) * radius;
        }

        static float PointSegment(float px, float pz, float ax, float az, float bx, float bz)
        {
            float abx = bx - ax;
            float abz = bz - az;
            float len2 = abx * abx + abz * abz;
            if (len2 < 0.0001f)
                return MathF.Sqrt((px - ax) * (px - ax) + (pz - az) * (pz - az));
            float t = Math.Clamp(((px - ax) * abx + (pz - az) * abz) / len2, 0f, 1f);
            float cx = ax + abx * t - px;
            float cz = az + abz * t - pz;
            return MathF.Sqrt(cx * cx + cz * cz);
        }

        float Separation(in MotionTarget target) =>
            _bodyRadius + target.RadiusM + _stopGap;

        /// <summary>Faz başından varışa düz yol ayrım çemberine giriyorsa varış giriş noktasıdır.</summary>
        void StopAtBodyEdge(in MotionTarget target)
        {
            float r = Separation(target);
            float sx = _phaseX - target.X;
            float sz = _phaseZ - target.Z;
            float c = sx * sx + sz * sz - r * r;
            if (c <= 0f)
                return;
            float vx = _destX - _phaseX;
            float vz = _destZ - _phaseZ;
            float a = vx * vx + vz * vz;
            if (a < 1e-6f)
                return;
            float b = sx * vx + sz * vz;
            float disc = b * b - a * c;
            if (b >= 0f || disc < 0f)
                return;
            float t = (-b - MathF.Sqrt(disc)) / a;
            if (t >= 1f)
                return;
            t = Math.Max(0f, t);
            _destX = _phaseX + vx * t;
            _destZ = _phaseZ + vz * t;
        }

        /// <summary>Atış, tutma ve dönüş oyuncunun ayaklarını yerinden kaldırmaz.</summary>
        static bool FeetPlanted(MotionPhase phase) =>
            phase.Motion is "hold" or "throw" or "spin" or "fan" or "hover";

        /// <summary>Dönüş atılması hedefin içine inmez; işaret dışarıdaysa işaret kalır.</summary>
        void KeepOutside(in MotionTarget aim, ref float x, ref float z)
        {
            if (!aim.Has)
                return;
            MotionTarget target = _body.Has ? _body : aim;
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
                if (dx * dx + dz * dz > MotionTemplateRunnerDefaults.StepDistSqrMin)
                {
                    Normalize(dx, dz, out fx, out fz);
                    return;
                }
            }
            fx = _phaseFaceX;
            fz = _phaseFaceZ;
        }
    }
}
