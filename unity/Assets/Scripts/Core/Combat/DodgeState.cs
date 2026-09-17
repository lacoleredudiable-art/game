using System;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Dodge zaman çizelgesi ve yer değiştirme oranı — dovus-sistemi.md §6.
    /// Konum değil, toplam mesafeye göre oran döner (Unity'siz).
    /// </summary>
    public sealed class DodgeState
    {
        readonly DodgeTuning _tuning;
        int _pressTimeMs = -1;

        public DodgeState(DodgeTuning? tuning = null)
        {
            _tuning = tuning ?? new DodgeTuning();
        }

        /// <summary>
        /// Ulti/pasif dash_cooldown_mult çarpımı (0 = serbest dodge). IsOnCooldown bunu kullanır.
        /// </summary>
        public float CooldownMult { get; set; } = 1f;

        public int? PressTimeMs => _pressTimeMs >= 0 ? _pressTimeMs : null;

        public void Begin(int pressTimeMs)
        {
            _pressTimeMs = pressTimeMs;
        }

        public void Reset()
        {
            _pressTimeMs = -1;
        }

        public bool IsActive(int worldTimeMs)
        {
            if (_pressTimeMs < 0)
                return false;

            int elapsed = worldTimeMs - _pressTimeMs;
            int totalMs = _tuning.StartupMs + _tuning.DurationMs + _tuning.GlideTailMs;
            return elapsed >= 0 && elapsed < totalMs;
        }

        public bool IsOnCooldown(int worldTimeMs)
        {
            if (_pressTimeMs < 0)
                return false;

            float mult = CooldownMult;
            if (mult < 0f)
                mult = 0f;
            int cd = (int)Math.Round(_tuning.CooldownMs * mult);
            return worldTimeMs < _pressTimeMs + cd;
        }

        /// <summary>
        /// Dokunulmazlık penceresi: [basma + iframeStart, basma + iframeStart + iframe).
        /// </summary>
        public bool IsInvulnerable(int worldTimeMs)
        {
            if (_pressTimeMs < 0)
                return false;

            int start = _pressTimeMs + _tuning.IframeStartMs;
            int end = start + _tuning.IframeMs;
            return worldTimeMs >= start && worldTimeMs < end;
        }

        public int IframeStartMs(int pressTimeMs) => pressTimeMs + _tuning.IframeStartMs;

        public int IframeEndMs(int pressTimeMs) => pressTimeMs + _tuning.IframeStartMs + _tuning.IframeMs;

        /// <summary>Hareket eğrisi s(u) = 1 - (1-u)^curveExp.</summary>
        public float EvaluateCurve(float u)
        {
            u = Math.Clamp(u, 0f, 1f);
            return 1f - MathF.Pow(1f - u, _tuning.CurveExp);
        }

        /// <summary>
        /// Anlık yer değiştirme oranı: startup'ta 0, süre boyunca eğri, süre sonunda 1.0.
        /// </summary>
        public float GetDisplacementRatio(int worldTimeMs)
        {
            if (_pressTimeMs < 0)
                return 0f;

            int elapsed = worldTimeMs - _pressTimeMs;
            if (elapsed <= _tuning.StartupMs)
                return 0f;

            int moveElapsed = elapsed - _tuning.StartupMs;
            if (moveElapsed <= _tuning.DurationMs)
            {
                float u = moveElapsed / (float)_tuning.DurationMs;
                return EvaluateCurve(u);
            }

            return 1f;
        }

        /// <summary>
        /// Glide tail boyunca sönen artık hız oranı (0..1). Ana süre bitince 1, tail sonunda 0.
        /// </summary>
        public float GetGlideVelocityRatio(int worldTimeMs)
        {
            if (_pressTimeMs < 0)
                return 0f;

            int elapsed = worldTimeMs - _pressTimeMs;
            int moveEnd = _tuning.StartupMs + _tuning.DurationMs;
            if (elapsed <= moveEnd)
                return 0f;

            int glideElapsed = elapsed - moveEnd;
            if (glideElapsed >= _tuning.GlideTailMs)
                return 0f;

            float t = 1f - glideElapsed / (float)_tuning.GlideTailMs;
            return t * t;
        }
    }
}
