using System;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Skill hareketinin dokunulmazlığı (SkillMotionPlan.IframeMs: 3-7 dash 400 ms, ışınlanma /
    /// Zenitsu 220 ms, element blink 100–300 ms). Stasis DEĞİL: oyuncu donmaz, hareket sürer;
    /// yalnız pencere içindeki vuruş yutulur. Pencere uzar, kısalmaz. Bitiş hariç: [açılış, bitiş).
    /// </summary>
    public sealed class SkillIframeWindow
    {
        double _startMs = -1;
        double _untilMs = -1;

        public double UntilMs => _untilMs;

        public void Open(double nowMs, int durationMs)
        {
            if (durationMs <= 0)
                return;
            double end = nowMs + durationMs;
            if (!IsActive(nowMs))
                _startMs = nowMs;
            if (end > _untilMs)
                _untilMs = end;
        }

        public bool IsActive(double nowMs) => _untilMs > 0 && nowMs >= _startMs && nowMs < _untilMs;

        /// <summary>Pencere içindeyse 0, değilse ham hasar.</summary>
        public float Filter(double nowMs, float raw) => IsActive(nowMs) ? 0f : Math.Max(0f, raw);

        public void Clear()
        {
            _startMs = -1;
            _untilMs = -1;
        }
    }
}
