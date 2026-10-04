using System;
using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Status
{
    /// <summary>Tempo yavaşlatmasının süresi ve gücü. JSON yoksa tek yedek + bir kez uyarı.</summary>
    public static class TempoSyncRules
    {
        public static void Read(double durationSec, float strength, out double durationMs, out float slowStrength)
        {
            bool missing = false;
            if (durationSec > 0)
                durationMs = durationSec * StatusDefaults.SecToMs;
            else
            {
                durationMs = StatusDefaults.TempoSyncFallbackMs;
                missing = true;
            }

            if (strength > 0f && strength < 1f)
                slowStrength = strength;
            else
            {
                slowStrength = StatusDefaults.TempoSyncFallbackStrength;
                missing = true;
            }

            if (missing)
            {
                DesignWarnings.Once(
                    "tempo_sync",
                    "element-sistemi.json tempo süresi veya gücü yok; yedek 1 sn ve 0.7 kullanıldı.");
            }
        }
    }
}
