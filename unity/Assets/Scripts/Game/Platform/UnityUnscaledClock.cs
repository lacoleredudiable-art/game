using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Platform
{
    public sealed class UnityUnscaledClock : IClock
    {
        public static readonly UnityUnscaledClock Default = new UnityUnscaledClock();

        public double NowMs => Time.unscaledTime * Units.SecToMs;

        public double DeltaMs => Time.unscaledDeltaTime * Units.SecToMs;

        public float DeltaSec => Time.unscaledDeltaTime;
    }
}
