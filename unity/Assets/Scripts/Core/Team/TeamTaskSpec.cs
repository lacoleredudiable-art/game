using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Görev tanımı. Sayılar <see cref="TeamTaskDefaults"/>'tan.</summary>
    public sealed class TeamTaskSpec
    {
        public TeamTaskSpec(TeamTaskKind kind, double durationSec, double holdSec, IReadOnlyList<TeamTaskZone> zones, string announcement)
        {
            Kind = kind;
            DurationSec = durationSec;
            HoldSec = holdSec;
            Zones = zones ?? Array.Empty<TeamTaskZone>();
            Announcement = announcement ?? string.Empty;
        }

        public TeamTaskKind Kind { get; }
        public double DurationSec { get; }
        public double HoldSec { get; }
        public IReadOnlyList<TeamTaskZone> Zones { get; }
        public string Announcement { get; }

        public static TeamTaskSpec SleepSeal(IReadOnlyList<TeamTaskZone> stones) => new TeamTaskSpec(
            TeamTaskKind.SleepSeal, TeamTaskDefaults.SleepSealDurationSec, TeamTaskDefaults.SleepSealHoldSec, stones,
            "Mühürler sönüyor — 15 saniyede üç taşı yeniden yakın");

        public static TeamTaskSpec HeartSeal(IReadOnlyList<TeamTaskZone> stones) => new TeamTaskSpec(
            TeamTaskKind.HeartSeal, TeamTaskDefaults.HeartSealDurationSec, TeamTaskDefaults.HeartSealHoldSec, stones,
            "Kalp mühürleniyor — 10 saniyede üç taşta aynı anda durun");

        public static TeamTaskSpec AshRain(IReadOnlyList<TeamTaskZone> shadowStrips) => new TeamTaskSpec(
            TeamTaskKind.AshRain, TeamTaskDefaults.AshRainDurationSec, 0.0, shadowStrips,
            "Kül yağıyor — 8 saniyede bir taşın arkasına geçin");
    }
}
