using System;

namespace Dovus.App.Boss
{
    public static class VolleyPattern
    {
        public static float EffectiveSpreadDeg(float baseSpreadDeg, bool blind) =>
            blind ? baseSpreadDeg * BossDefaults.BlindSpreadMult : baseSpreadDeg;

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
