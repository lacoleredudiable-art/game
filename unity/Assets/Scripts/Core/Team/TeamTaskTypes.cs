using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Takım görevi türü (docs 18-kul-ejderi.md §5.3, §6.3, §7.3).</summary>
    public enum TeamTaskKind
    {
        /// <summary>Uyku Mührü: her taşta 2 s kesintisiz dur; 3 taş 15 s içinde yanmalı (sırayla olabilir).</summary>
        SleepSeal,
        /// <summary>Kor Mührü: 3 taşta AYNI ANDA, birlikte 3 s kesintisiz dur; süre 10 s.</summary>
        HeartSeal,
        /// <summary>Kül Yağmuru: süre dolunca bütün yaşayanlar bir gölge şeridinde olmalı.</summary>
        AshRain,
    }

    public enum TeamTaskStatus { Idle, Running, Succeeded, Failed }

    public enum TeamDifficulty { Training, Normal, Hard, Hell }

    public enum TeamZoneShape { Circle, Box }

    /// <summary>Görev bölgesi (XZ düzlemi). Daire: taş çemberi. Kutu: gölge şeridi (HalfX/HalfZ, YawRad).</summary>
    public readonly struct TeamTaskZone
    {
        public TeamTaskZone(int id, float x, float z, float radius)
        {
            Id = id; X = x; Z = z; Radius = radius; Shape = TeamZoneShape.Circle;
            HalfX = 0f; HalfZ = 0f; YawRad = 0f;
        }

        public TeamTaskZone(int id, float x, float z, float halfX, float halfZ, float yawRad)
        {
            Id = id; X = x; Z = z; Radius = 0f; Shape = TeamZoneShape.Box;
            HalfX = halfX; HalfZ = halfZ; YawRad = yawRad;
        }

        public int Id { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public TeamZoneShape Shape { get; }
        public float HalfX { get; }
        public float HalfZ { get; }
        public float YawRad { get; }

        public bool Contains(float px, float pz)
        {
            float dx = px - X, dz = pz - Z;
            if (Shape == TeamZoneShape.Circle)
                return dx * dx + dz * dz <= Radius * Radius;
            // Kutuyu yerel eksene çevir (yaw: +Z'den saat yönü karşıtı değil, düz döndürme).
            float c = (float)Math.Cos(YawRad), s = (float)Math.Sin(YawRad);
            float lx = dx * c + dz * s;
            float lz = -dx * s + dz * c;
            return Math.Abs(lx) <= HalfX && Math.Abs(lz) <= HalfZ;
        }

        public float DistanceTo(float px, float pz)
        {
            float dx = px - X, dz = pz - Z;
            return (float)Math.Sqrt(dx * dx + dz * dz);
        }
    }

    /// <summary>Görevin her tick'te gördüğü kişi (oyuncu ya da dost; ağ oyuncusu da aynı kapı).</summary>
    public readonly struct TeamTaskMember
    {
        public TeamTaskMember(int id, float x, float z, bool alive)
        {
            Id = id; X = x; Z = z; Alive = alive;
        }

        public int Id { get; }
        public float X { get; }
        public float Z { get; }
        public bool Alive { get; }
    }

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
