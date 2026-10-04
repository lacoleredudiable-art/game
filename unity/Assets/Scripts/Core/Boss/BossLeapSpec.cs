using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Grammar;
using System.Collections.Generic;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    public readonly struct BossLeapSpec
    {
        public BossLeapSpec(
            float minRangeM,
            float maxRangeM,
            float airSec,
            float landRadiusM,
            float wallMarginM)
        {
            MinRangeM = minRangeM;
            MaxRangeM = maxRangeM;
            AirSec = airSec;
            LandRadiusM = landRadiusM;
            WallMarginM = wallMarginM;
        }

        public float MinRangeM { get; }
        public float MaxRangeM { get; }
        public float AirSec { get; }
        public float LandRadiusM { get; }
        public float WallMarginM { get; }
    }
}
