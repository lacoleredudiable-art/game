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
    public readonly struct BossOnHitStatus
    {
        public BossOnHitStatus(string id, float durationSec)
        {
            Id = id ?? string.Empty;
            DurationSec = durationSec;
        }

        public string Id { get; }
        public float DurationSec { get; }
        public bool IsValid => !string.IsNullOrEmpty(Id);
    }

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

    public sealed class BossAttackEntry
    {
        public string Id = string.Empty;
        public BossAttackKind Kind;
        public IReadOnlyList<string> Mechanics = System.Array.Empty<string>();
        public BossOnHitStatus OnHitStatus;
        public BossFieldSpec? Field;
        public BossLeapSpec? Leap;
    }

    public sealed class BossHudSnapshot
    {
        public string Name { get; set; } = "BOSS";
        public string Subtitle { get; set; } = string.Empty;
        public List<(int Phase, string Name, float UpperFrac)> Phases { get; } = new();
        public Dictionary<string, string> AttackNamesById { get; } = new();
        public Dictionary<BossAttackKind, string> AttackNamesByKind { get; } = new();
    }

    /// <summary>Parse edilmiş boss encounter JSON kökü; yalnız mapper üretir.</summary>
    public sealed class BossEncounterDocument
    {
        internal BossEncounterDocument(JsonValue root) => Root = root;

        internal JsonValue Root { get; }

        public bool IsValid => Root != null && !Root.IsNull;
    }
}
