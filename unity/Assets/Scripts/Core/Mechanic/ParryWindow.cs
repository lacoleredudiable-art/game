using System;
using System.Collections.Generic;
using System.Linq;

namespace Dovus.Core.Mechanic
{
    /// <summary>C1 — savuşturma: pencere içindeki ilk vuruş tamamen yutulur ve oranla geri döner.</summary>
    public sealed class ParryWindow
    {
        double _untilMs;
        float _ratio;
        bool _used = true;

        public void Arm(double untilMs, float ratio)
        {
            _untilMs = untilMs;
            _ratio = Math.Max(0f, Math.Min(1f, ratio));
            _used = _ratio <= 0f;
        }

        public bool Active(double nowMs) => !_used && nowMs < _untilMs;

        public bool TryConsume(double nowMs, float incoming, out float reflected)
        {
            reflected = 0f;
            if (!Active(nowMs) || incoming <= 0f)
                return false;
            _used = true;
            reflected = incoming * _ratio;
            return true;
        }

        public void Clear() => _used = true;
    }
}
