using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>Görev düzeni: az kişiyle taş sayısını indirme ve yapay zekâ dostlara bölge dağıtımı.</summary>
    public static class TeamTaskPlanner
    {
        /// <summary>
        /// KE-7.11: yaşayan sayısı taş sayısından azsa taş sayısı kişi sayısına iner, en yakın taşlardan başlayarak.
        /// Yalnız aynı anda durma gereken görevde (Kor Mührü) uygulanır; öbürlerinde liste aynen döner.
        /// </summary>
        public static TeamTaskSpec FitToParticipants(TeamTaskSpec spec, IReadOnlyList<TeamTaskMember> members)
        {
            if (spec == null) throw new ArgumentNullException(nameof(spec));
            if (spec.Kind != TeamTaskKind.HeartSeal) return spec;
            int alive = 0;
            for (int i = 0; i < members.Count; i++) if (members[i].Alive) alive++;
            if (alive <= 0 || alive >= spec.Zones.Count) return spec;

            var order = new List<TeamTaskZone>(spec.Zones);
            order.Sort((a, b) => NearestMemberDist(a, members).CompareTo(NearestMemberDist(b, members)));
            return new TeamTaskSpec(spec.Kind, spec.DurationSec, spec.HoldSec, order.GetRange(0, alive), spec.Announcement);
        }

        /// <summary>
        /// KE-10.2: dost en yakın boş bölgeye gider. Her dost sırayla, henüz kimsenin almadığı en yakın bölgeyi seçer.
        /// Uyku Mührü: yanan taş boş sayılmaz. Kor Mührü: oyuncuya da bir taş ayrılır (en yakını), dostlar kalanları alır.
        /// Kül Yağmuru: şerit paylaşılır, herkes en yakın şeride gider.
        /// Dönüş: dost id → bölge id. Bölge bulunamayan dost sözlükte yoktur.
        /// </summary>
        public static Dictionary<int, int> AssignAllies(
            TeamTask task, IReadOnlyList<TeamTaskMember> allies, TeamTaskMember? player)
        {
            var result = new Dictionary<int, int>();
            if (task == null || task.Status != TeamTaskStatus.Running) return result;
            var zones = task.Spec.Zones;
            bool exclusive = task.Spec.Kind != TeamTaskKind.AshRain;
            var taken = new HashSet<int>();

            if (task.Spec.Kind == TeamTaskKind.HeartSeal && player.HasValue && player.Value.Alive)
            {
                int pz = Nearest(zones, player.Value.X, player.Value.Z, taken, task, false);
                if (pz >= 0) taken.Add(pz);
            }

            for (int a = 0; a < allies.Count; a++)
            {
                var ally = allies[a];
                if (!ally.Alive) continue;
                int zi = Nearest(zones, ally.X, ally.Z, exclusive ? taken : null, task, task.Spec.Kind == TeamTaskKind.SleepSeal);
                if (zi < 0) continue;
                if (exclusive) taken.Add(zi);
                result[ally.Id] = zones[zi].Id;
            }
            return result;
        }

        static int Nearest(IReadOnlyList<TeamTaskZone> zones, float x, float z, HashSet<int>? taken, TeamTask task, bool skipLit)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < zones.Count; i++)
            {
                if (taken != null && taken.Contains(i)) continue;
                if (skipLit && task.IsLit(i)) continue;
                float d = zones[i].DistanceTo(x, z);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        static float NearestMemberDist(TeamTaskZone z, IReadOnlyList<TeamTaskMember> members)
        {
            float best = float.MaxValue;
            for (int i = 0; i < members.Count; i++)
                if (members[i].Alive) best = Math.Min(best, z.DistanceTo(members[i].X, members[i].Z));
            return best;
        }
    }
}
