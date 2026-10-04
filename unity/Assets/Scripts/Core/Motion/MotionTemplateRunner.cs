using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Kalıbın fazlarını dünya konumuna çevirir. Kök yer değiştirme, süre, hedefe yapışma
    /// ve faz vuruşu burada. Unity yok; gövdeyi Game katmanı uygular.
    /// </summary>
    public sealed partial class MotionTemplateRunner
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
        float _stopGap = MotionTemplateRunnerDefaults.StopGapM;
        float _plantX, _plantZ;
        float _markX, _markZ;
        bool _plantSent;
        MotionTarget _lastTarget;
        bool _active;
        bool _finished;
        bool _hitSent;
        bool _snapped;
        bool _hasTracked;
        MotionTarget _tracked;
        /// <summary>Kaydırılmamış hedef: nişan yumuşar, gövde ayrımı gerçek yeri kullanır.</summary>
        MotionTarget _body;
        bool _yieldApproach;
        float _yieldX, _yieldZ;
        float _blendU0 = -1f;
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
            float stopGapM = MotionTemplateRunnerDefaults.StopGapM)
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
            _hasTracked = false;
            _tracked = default;
            _body = default;
            _yieldApproach = false;
            _yieldX = x;
            _yieldZ = z;
            _blendU0 = -1f;
            _finished = template == null || template.Phases.Count == 0;
            _active = !_finished;
            Normalize(faceX, faceZ, out _faceX, out _faceZ);
            if (_active)
                EnterPhase();
        }

        /// <summary>
        /// Gövdeyi kalıp dışında bir şey taşıdıysa (itme, çekme, ışınlanma) kalan fazlar
        /// yeni yerden sürer; sonraki vuruşlar oyuncunun o anki konumunda açılır.
        /// İşaret (dönüş) ve dikilen nokta dünyada kalır.
        /// </summary>
        public void Rebase(float x, float z)
        {
            if (!_active || _finished)
                return;
            float dx = x - _x;
            float dz = z - _z;
            if (dx * dx + dz * dz < MotionTemplateRunnerDefaults.SegmentLen2EpsilonSqr)
                return;
            _x = x;
            _z = z;
            _phaseX += dx;
            _phaseZ += dz;
            _yieldX += dx;
            _yieldZ += dz;
            if (_template.Phases[_phase].Motion != "return")
            {
                _destX += dx;
                _destZ += dz;
            }
        }

        public MotionTick Tick(float dt, in MotionTarget target, in MotionStick stick)
        {
            _hits.Clear();
            if (!_active || _finished || dt <= 0f || _template == null)
                return Capture(0f, 0f);

            float x0 = _x;
            float z0 = _z;
            _snapped = false;
            // Hedef bir karede ışınlanırsa dash varışı da sıçrar (3-2'de 7,79 m).
            // Nişan noktası en çok 40 m/s kayar; ışınlanma fazı bunu kullanmaz.
            MotionTarget aim = SlewTarget(target, dt);
            _lastTarget = aim;
            _body = target;
            float left = dt;
            int guard = 0;
            while (left > 0.00001f && !_finished && guard++ < 12)
            {
                MotionPhase phase = _template.Phases[_phase];
                float durAhead = Math.Max(MotionTemplateRunnerDefaults.MinDistM, phase.DurationSec);
                if (_yieldApproach && !aim.HoldApproach)
                {
                    // Çekme bitince faz eğrisi eski başlangıca zıplamasın.
                    _yieldApproach = false;
                    _blendU0 = Math.Clamp(_time / durAhead, 0f, MotionTemplateRunnerDefaults.OutgoingMultCap);
                    _phaseX = _x;
                    _phaseZ = _z;
                }
                AimDest(phase, aim);
                float dur = Math.Max(MotionTemplateRunnerDefaults.MinDistM, phase.DurationSec);
                if (phase.Gate == "release")
                {
                    if (_time < dur)
                    {
                        float step = Math.Min(left, dur - _time);
                        Advance(phase, step, dur, aim, stick);
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
                        Pose(phase, 1f, 1f, aim, stick, 0f);
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
                    Advance(phase, step, dur, aim, stick);
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

        /// <summary>Nişan hedefini kare hızıyla takip eder. 8 m'lik bir ışınlanma oyuncuyu sürüklemez.</summary>
        MotionTarget SlewTarget(in MotionTarget target, float dt)
        {
            if (!target.Has || _snapped)
            {
                _hasTracked = false;
                return target;
            }
            if (!_hasTracked)
            {
                _tracked = target;
                _hasTracked = true;
                return target;
            }
            float dx = target.X - _tracked.X;
            float dz = target.Z - _tracked.Z;
            float dist = MathF.Sqrt(dx * dx + dz * dz);
            float cap = MotionTemplateRunnerDefaults.MaxDeltaPerTickMult * MathF.Max(dt, 0.001f);
            if (dist <= cap)
            {
                _tracked = target;
                return target;
            }
            float scale = cap / dist;
            _tracked = target.At(_tracked.X + dx * scale, _tracked.Z + dz * scale);
            return _tracked;
        }
    }
}
