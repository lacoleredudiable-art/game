using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Platform
{
    public sealed class UnityFrameClock : IClock
    {
        public static readonly UnityFrameClock Default = new UnityFrameClock();

        public double NowMs => Time.time * 1000.0;

        public double DeltaMs => Time.deltaTime * 1000.0;

        public float DeltaSec => Time.deltaTime;
    }
}
