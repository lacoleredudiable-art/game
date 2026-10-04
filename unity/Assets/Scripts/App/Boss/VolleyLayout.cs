using System;

namespace Dovus.App.Boss
{
    public readonly struct VolleyLayout
    {
        public VolleyLayout(float spreadDeg, int count, float startDeg, float stepDeg)
        {
            SpreadDeg = spreadDeg;
            Count = count;
            StartDeg = startDeg;
            StepDeg = stepDeg;
        }

        public float SpreadDeg { get; }
        public int Count { get; }
        public float StartDeg { get; }
        public float StepDeg { get; }
    }
}
