using System;
using Dovus.Core.Tuning;

namespace Dovus.Core.Time
{
    /// <summary>
    /// Dünya (ölçeklenmiş) ve gerçek (ölçeklenmemiş) saati birlikte yönetir.
    /// Yavaş çekim rampası ve hitstop — dovus-sistemi.md §7, teknoloji-kararlari.md §5.
    /// </summary>
    public sealed class TimeDirector
    {
        enum SlowmoPhase
        {
            None,
            RampDown,
            Hold,
            RampUp
        }

        readonly SlowmoTuning _defaults;

        double _realTimeMs;
        double _worldTimeMs;
        float _timeScale = 1f;

        SlowmoPhase _phase = SlowmoPhase.None;
        double _phaseElapsedMs;
        float _slowmoFactor;
        int _rampDownMs;
        int _holdMs;
        int _rampUpMs;

        double _hitstopRemainingMs;
        bool _slowmoPaused;
        SlowmoPhase _pausedPhase;
        double _pausedPhaseElapsedMs;
        float _pausedSlowmoFactor;
        int _pausedRampDownMs;
        int _pausedHoldMs;
        int _pausedRampUpMs;

        bool _pendingSlowmo;
        float _pendingFactor;
        int _pendingRampDownMs;
        int _pendingHoldMs;
        int _pendingRampUpMs;

        public TimeDirector(SlowmoTuning? tuning = null)
        {
            _defaults = tuning ?? new SlowmoTuning();
        }

        public double RealTimeMs => _realTimeMs;

        public double WorldTimeMs => _worldTimeMs;

        /// <summary>Anlık zaman ölçeği. Hitstop sırasında 0; yavaş çekimde [factor, 1].</summary>
        public float TimeScale => _timeScale;

        public bool IsHitstopActive => _hitstopRemainingMs > 0;

        public bool IsSlowmoActive => _phase != SlowmoPhase.None;

        /// <summary>
        /// Yavaş çekim tetikler. Aktif yavaş çekim varsa baştan başlar.
        /// Hitstop sırasında kuyruğa alınır; hitstop bitince uygulanır.
        /// </summary>
        public void TriggerSlowmo(
            float? factor = null,
            int? rampDownMs = null,
            int? holdMs = null,
            int? rampUpMs = null)
        {
            float f = factor ?? _defaults.Factor;
            int down = rampDownMs ?? _defaults.RampDownMs;
            int hold = holdMs ?? _defaults.HoldMs;
            int up = rampUpMs ?? _defaults.RampUpMs;

            if (_hitstopRemainingMs > 0)
            {
                QueueSlowmo(f, down, hold, up);
                return;
            }

            BeginSlowmo(f, down, hold, up);
        }

        /// <summary>
        /// Kısa süre ölçeği ~0'a çeker; yavaş çekimden önceliklidir.
        /// Üst üste gelirse süreler toplanır.
        /// </summary>
        public void TriggerHitstop(int durationMs)
        {
            if (durationMs <= 0)
                return;

            if (_hitstopRemainingMs <= 0 && _phase != SlowmoPhase.None && !_slowmoPaused)
                PauseSlowmo();

            _hitstopRemainingMs += durationMs;
            _timeScale = 0f;
        }

        /// <summary>Gerçek delta ile ilerler; ölçeklenmiş dünya deltasını döner.</summary>
        public double Tick(double realDtMs)
        {
            if (realDtMs < 0)
                realDtMs = 0;

            _realTimeMs += realDtMs;

            double scaledDt;
            if (_hitstopRemainingMs > 0)
            {
                _hitstopRemainingMs = Math.Max(0, _hitstopRemainingMs - realDtMs);
                _timeScale = 0f;
                scaledDt = 0;

                if (_hitstopRemainingMs <= 0)
                    OnHitstopEnded();
            }
            else
            {
                AdvanceSlowmo(realDtMs);
                scaledDt = realDtMs * _timeScale;
            }

            _worldTimeMs += scaledDt;
            return scaledDt;
        }

        void BeginSlowmo(float factor, int rampDownMs, int holdMs, int rampUpMs)
        {
            _phase = SlowmoPhase.RampDown;
            _phaseElapsedMs = 0;
            _slowmoFactor = ClampFactor(factor);
            _rampDownMs = Math.Max(0, rampDownMs);
            _holdMs = Math.Max(0, holdMs);
            _rampUpMs = Math.Max(0, rampUpMs);
            _slowmoPaused = false;
            _pendingSlowmo = false;
            _timeScale = 1f;
        }

        void QueueSlowmo(float factor, int rampDownMs, int holdMs, int rampUpMs)
        {
            _pendingSlowmo = true;
            _pendingFactor = ClampFactor(factor);
            _pendingRampDownMs = Math.Max(0, rampDownMs);
            _pendingHoldMs = Math.Max(0, holdMs);
            _pendingRampUpMs = Math.Max(0, rampUpMs);
        }

        void PauseSlowmo()
        {
            _slowmoPaused = true;
            _pausedPhase = _phase;
            _pausedPhaseElapsedMs = _phaseElapsedMs;
            _pausedSlowmoFactor = _slowmoFactor;
            _pausedRampDownMs = _rampDownMs;
            _pausedHoldMs = _holdMs;
            _pausedRampUpMs = _rampUpMs;
        }

        void ResumeSlowmo()
        {
            _slowmoPaused = false;
            _phase = _pausedPhase;
            _phaseElapsedMs = _pausedPhaseElapsedMs;
            _slowmoFactor = _pausedSlowmoFactor;
            _rampDownMs = _pausedRampDownMs;
            _holdMs = _pausedHoldMs;
            _rampUpMs = _pausedRampUpMs;
            _timeScale = EvaluateSlowmoScale();
        }

        void OnHitstopEnded()
        {
            if (_slowmoPaused)
                ResumeSlowmo();
            else if (_pendingSlowmo)
            {
                _pendingSlowmo = false;
                BeginSlowmo(_pendingFactor, _pendingRampDownMs, _pendingHoldMs, _pendingRampUpMs);
            }
            else if (_phase == SlowmoPhase.None)
                _timeScale = 1f;
            else
                _timeScale = EvaluateSlowmoScale();
        }

        void AdvanceSlowmo(double realDtMs)
        {
            if (_phase == SlowmoPhase.None)
            {
                _timeScale = 1f;
                return;
            }

            _phaseElapsedMs += realDtMs;

            while (_phase != SlowmoPhase.None)
            {
                int phaseDurationMs = PhaseDurationMs(_phase);
                if (phaseDurationMs <= 0 || _phaseElapsedMs < phaseDurationMs)
                    break;

                _phaseElapsedMs -= phaseDurationMs;
                _phase = NextPhase(_phase);
                if (_phase == SlowmoPhase.None)
                {
                    _timeScale = 1f;
                    return;
                }
            }

            _timeScale = EvaluateSlowmoScale();
        }

        SlowmoPhase NextPhase(SlowmoPhase phase) =>
            phase switch
            {
                SlowmoPhase.RampDown => SlowmoPhase.Hold,
                SlowmoPhase.Hold => SlowmoPhase.RampUp,
                SlowmoPhase.RampUp => SlowmoPhase.None,
                _ => SlowmoPhase.None
            };

        int PhaseDurationMs(SlowmoPhase phase) =>
            phase switch
            {
                SlowmoPhase.RampDown => _rampDownMs,
                SlowmoPhase.Hold => _holdMs,
                SlowmoPhase.RampUp => _rampUpMs,
                _ => 0
            };

        float EvaluateSlowmoScale()
        {
            return _phase switch
            {
                SlowmoPhase.RampDown => Lerp(1f, _slowmoFactor, SmoothStep(PhaseProgress(_rampDownMs))),
                SlowmoPhase.Hold => _slowmoFactor,
                SlowmoPhase.RampUp => Lerp(_slowmoFactor, 1f, SmoothStep(PhaseProgress(_rampUpMs))),
                _ => 1f
            };
        }

        float PhaseProgress(int durationMs)
        {
            if (durationMs <= 0)
                return 1f;

            return (float)Math.Clamp(_phaseElapsedMs / durationMs, 0, 1);
        }

        static float ClampFactor(float factor) => Math.Clamp(factor, 0f, 1f);

        static float Lerp(float a, float b, float t) => a + (b - a) * t;

        /// <summary>Yumuşak geçiş — rampa iniş/çıkış ease'i.</summary>
        static float SmoothStep(float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            return t * t * (3f - 2f * t);
        }
    }
}
