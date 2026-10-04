using System;
using Dovus.Core.Status;
using Dovus.Core.Damage;

namespace Dovus.Core.Boss
{
    /// <summary>Element status / status_effect / status_duration — yalnız boss'a gidenler.</summary>
    public readonly struct ElementBossStatus
    {
        public ElementBossStatus(StatusKind kind, double durationMs, float magnitude)
        {
            Kind = kind;
            DurationMs = durationMs;
            Magnitude = magnitude;
        }

        public StatusKind Kind { get; }
        public double DurationMs { get; }
        public float Magnitude { get; }
    }
}
