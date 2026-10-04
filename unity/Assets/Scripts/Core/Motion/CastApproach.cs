using System;
using System.Collections.Generic;

namespace Dovus.Core.Motion
{
    /// <summary>
    /// Menzil kapısı sessizce reddetmez. Eksik mesafe kalıbın başına bir kapanış
    /// hamlesi olarak eklenir; ışınlanma yok. Hız 8 m/s, en kısa 0,12 sn.
    /// </summary>
    public static class CastApproach
    {
        public const float SpeedMps = 8f;
        public const float MinSec = 0.12f;
        public const string PhaseName = "kapan";

        /// <summary>
        /// Merkez mesafesinden, vuruş kenarına ve kalıbın kendi kapanışına kalan yol.
        /// Sıfırsa oyuncu zaten menzildedir.
        /// </summary>
        public static float Meters(
            float centerDistM,
            float playerRadiusM,
            float targetRadiusM,
            float edgeReachM,
            float alreadyClosingM)
        {
            float allow = Math.Max(0f, edgeReachM)
                + Math.Max(0f, playerRadiusM)
                + Math.Max(0f, targetRadiusM)
                + Math.Max(0f, alreadyClosingM);
            return Math.Max(0f, centerDistM - allow);
        }

        public static MotionTemplate Prepend(MotionTemplate template, float meters)
        {
            if (template == null || meters <= MotionDefaults.Min05f)
                return template;
            float sec = Math.Max(MinSec, meters / SpeedMps);
            var phase = new MotionPhase(
                PhaseName, "lunge", sec, "target", "track", string.Empty, 0f,
                meters, 0f, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f,
                0f, 0f, null, null);
            var phases = new List<MotionPhase>(template.Phases.Count + 1) { phase };
            for (int i = 0; i < template.Phases.Count; i++)
                phases.Add(template.Phases[i]);
            return template.WithPhases(phases);
        }
    }
}
