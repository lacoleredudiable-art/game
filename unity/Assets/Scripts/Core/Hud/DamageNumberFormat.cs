using System;
using System.Globalization;

namespace Dovus.Core.Hud
{
    /// <summary>Yüzen hasar yazısı: 24300 → 24.3K, 1200000 → 1.2M.</summary>
    public static class DamageNumberFormat
    {
        public static string Format(float amount)
        {
            float v = Math.Abs(amount);
            if (v >= 1_000_000f)
                return Trim(v / 1_000_000f) + "M";
            if (v >= 1000f)
                return Trim(v / 1000f) + "K";
            return Math.Round(v).ToString("0", CultureInfo.InvariantCulture);
        }

        static string Trim(float scaled)
        {
            float tenths = (float)Math.Round(scaled * HudDefaults.DamageRoundScaleMult) / HudDefaults.DamageRoundScaleMult;
            if (Math.Abs(tenths - (float)Math.Round(tenths)) < 0.001f)
                return ((int)Math.Round(tenths)).ToString(CultureInfo.InvariantCulture);
            return tenths.ToString("0.0", CultureInfo.InvariantCulture);
        }
    }
}
