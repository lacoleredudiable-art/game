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

    public static class VolleyPattern
    {
        public static float EffectiveSpreadDeg(float baseSpreadDeg, bool blind) =>
            blind ? baseSpreadDeg * BossDefaults.Lit15f : baseSpreadDeg;

        public static VolleyLayout ComputeLayout(int volleyCount, float spreadDeg)
        {
            int count = volleyCount < 1 ? 1 : volleyCount;
            float start = count > 1 ? -spreadDeg * 0.5f : 0f;
            float step = count > 1 ? spreadDeg / (count - 1) : 0f;
            return new VolleyLayout(spreadDeg, count, start, step);
        }

        public static float YawDegAt(VolleyLayout layout, int index) =>
            layout.StartDeg + layout.StepDeg * index;
    }
}
