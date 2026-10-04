using System;

namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Süreli alan. Süre uzayınca dilim küçülmez; aynı payla yeni vuruş eklenir.
    /// </summary>
    public static class SustainedField
    {
        public static int TickCount(float durationSec, float tickSec)
        {
            if (durationSec <= 0f)
                return 1;
            float tick = tickSec > WeaponPassiveDefaults.MinTickSec ? tickSec : WeaponPassiveDefaults.MinTickSec;
            return Math.Max(1, (int)Math.Ceiling(durationSec / tick - 1e-4f));
        }

        public static float PerTickShare(float baseDurationSec, float tickSec) =>
            1f / TickCount(baseDurationSec, tickSec);
    }
}
