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
    public readonly struct BossFieldSpec
    {
        public BossFieldSpec(
            float radiusM,
            float lifeSec,
            int maxCount,
            string status,
            float refreshSec,
            float minCenterDistM)
        {
            RadiusM = radiusM;
            LifeSec = lifeSec;
            MaxCount = maxCount;
            Status = status ?? string.Empty;
            RefreshSec = refreshSec;
            MinCenterDistM = minCenterDistM;
        }

        public float RadiusM { get; }
        public float LifeSec { get; }
        public int MaxCount { get; }
        public string Status { get; }
        public float RefreshSec { get; }
        public float MinCenterDistM { get; }
    }
}
