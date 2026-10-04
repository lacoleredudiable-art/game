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
        bool _combined;
        int _promotedAtMs = -1;
        float _ratioAtPromote;

        public DodgeState(DodgeTuning? tuning = null)
        {
            _tuning = tuning ?? new DodgeTuning();
        }

        /// <summary>
        /// Ulti/pasif dash_cooldown_mult çarpımı. Hak dolum süresine (DodgeCharges.RechargeMult) aktarılır.
        /// </summary>
        public float CooldownMult { get; set; } = 1f;

        public int? PressTimeMs => _pressTimeMs >= 0 ? _pressTimeMs : null;

        public bool IsCombined => _combined;

        public void Begin(int pressTimeMs)
        {
            _pressTimeMs = pressTimeMs;
            _combined = false;
            _promotedAtMs = -1;
            _ratioAtPromote = 0f;
        }

        public void PromoteToCombined(int worldTimeMs)
        {
            if (_pressTimeMs < 0 || _combined)
                return;

            // Review fix: ratio is relative to the multiplied distance after promotion, so scale it
            // down by the distance multiplier to keep the displacement continuous (no jump).
            _ratioAtPromote = GetDisplacementRatio(worldTimeMs) / CombinedDistanceScale;
            _promotedAtMs = worldTimeMs;
            _combined = true;
        }

        public void Reset()
        {
            _pressTimeMs = -1;
            _combined = false;
            _promotedAtMs = -1;
            _ratioAtPromote = 0f;
        }

        public float DistanceMultiplier => _combined ? _tuning.CombinedDistanceMult : 1f;

        float CombinedDistanceScale => Math.Max(0.001f, _tuning.CombinedDistanceMult);

        public int MoveDurationMs =>
            _combined
                ? Math.Max(1, (int)Math.Round(_tuning.DurationMs * _tuning.CombinedDurationMult))
                : _tuning.DurationMs;

        public bool IsActive(int worldTimeMs)
        {
            if (_pressTimeMs < 0)
                return false;

            int elapsed = worldTimeMs - _pressTimeMs;
            int totalMs = _tuning.StartupMs + MoveDurationMs + _tuning.GlideTailMs;
            return elapsed >= 0 && elapsed < totalMs;
        }

        /// <summary>
        /// Dokunulmazlık penceresi: [basma + iframeStart, basma + iframeStart + iframe).
        /// Birleşik dodge: iframeStart'tan itibaren CombinedIframeMs.
        /// </summary>
        public bool IsInvulnerable(int worldTimeMs)
        {
            if (_pressTimeMs < 0)
                return false;

            int start = _pressTimeMs + _tuning.IframeStartMs;
            int end = _combined
                ? _pressTimeMs + _tuning.IframeStartMs + _tuning.CombinedIframeMs
                : start + _tuning.IframeMs;
            return worldTimeMs >= start && worldTimeMs < end;
        }

        public int IframeStartMs(int pressTimeMs) => pressTimeMs + _tuning.IframeStartMs;

        public int IframeEndMs(int pressTimeMs) =>
            _combined
                ? pressTimeMs + _tuning.IframeStartMs + _tuning.CombinedIframeMs
                : pressTimeMs + _tuning.IframeStartMs + _tuning.IframeMs;

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
            int durationMs = MoveDurationMs;

            if (_combined && _promotedAtMs >= 0)
            {
                int promoteElapsed = _promotedAtMs - _pressTimeMs - _tuning.StartupMs;
                if (promoteElapsed < 0)
                    promoteElapsed = 0;

                if (moveElapsed <= promoteElapsed)
                {
                    int preDur = _tuning.DurationMs;
                    if (preDur <= 0)
                        return 1f;
                    float uPre = moveElapsed / (float)preDur;
                    return EvaluateCurve(uPre) / CombinedDistanceScale;
                }

                int remainDur = durationMs - promoteElapsed;
                if (remainDur <= 0)
                    return 1f;

                int afterPromote = moveElapsed - promoteElapsed;
                float u = afterPromote / (float)remainDur;
                return _ratioAtPromote + (1f - _ratioAtPromote) * EvaluateCurve(u);
            }

            if (moveElapsed <= durationMs)
            {
                float u = moveElapsed / (float)durationMs;
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
            int moveEnd = _tuning.StartupMs + MoveDurationMs;
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
