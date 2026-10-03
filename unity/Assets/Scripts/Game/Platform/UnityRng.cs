using Dovus.Core.Shared;
using UnityEngine;

namespace Dovus.Game.Platform
{
    public sealed class UnityRng : IRng
    {
        public static readonly UnityRng Default = new UnityRng();

        public double NextDouble() => Random.value;
    }
}
