using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Platform
{
    public sealed class UnityFrameClock : IClock
    {
        public static readonly UnityFrameClock Default = new UnityFrameClock();

        public double NowMs => Time.time * PlatformTimeDefaults.SecToMs;

        public double DeltaMs => Time.deltaTime * PlatformTimeDefaults.SecToMs;

        public float DeltaSec => Time.deltaTime;
    }
}
