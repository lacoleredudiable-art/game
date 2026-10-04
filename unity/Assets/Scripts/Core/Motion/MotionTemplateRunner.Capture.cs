using System;
using System.Collections.Generic;
using Dovus.Core;

namespace Dovus.Core.Motion
{
    public sealed partial class MotionTemplateRunner
    {
        MotionTick Capture(float velX, float velZ)
        {
            string anim = string.Empty;
            float speed = 1f;
            bool spin = false;
            bool airborne = false;
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
                airborne = phase.Airborne && !_finished;
            }
            return new MotionTick(
                _x, _y, _z, _faceX, _faceZ, _finished, _hits.ToArray(),
                anim, speed, velX, velZ, spin, airborne);
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
