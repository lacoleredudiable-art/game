using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Platform
{
    public sealed class UnityFrameClock : IClock
    {
        public static readonly UnityFrameClock Default = new UnityFrameClock();

        public double NowMs => Time.time * Units.SecToMs;

        public double DeltaMs => Time.deltaTime * Units.SecToMs;

        public float DeltaSec => Time.deltaTime;
    }
}
