using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Platform
{
    public sealed class UnityUnscaledClock : IClock
    {
        public static readonly UnityUnscaledClock Default = new UnityUnscaledClock();

        public double NowMs => Time.unscaledTime * 1000.0;

        public double DeltaMs => Time.unscaledDeltaTime * 1000.0;

        public float DeltaSec => Time.unscaledDeltaTime;
    }
}
