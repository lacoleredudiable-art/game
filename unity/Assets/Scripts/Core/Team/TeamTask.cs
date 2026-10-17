using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>
    /// Süreli ortak takım görevi (saf mantık; zaman saniye olarak parametre). Dünya her kare <see cref="Tick"/> çağırır,
    /// <see cref="Status"/> bitince cezayı <see cref="TeamTaskPenalty"/> verir.
    /// </summary>
    public sealed class TeamTask
    {
        readonly List<int> _exposed = new List<int>();
        bool[] _lit = Array.Empty<bool>();
        double[] _hold = Array.Empty<double>();
        double _syncHold;
        double _startSec;
        double _lastSec;

        public TeamTaskSpec Spec { get; private set; } = new TeamTaskSpec(TeamTaskKind.SleepSeal, 0.0, 0.0, null, null);
        public TeamTaskStatus Status { get; private set; } = TeamTaskStatus.Idle;

        /// <summary>Sayaç payı: yanan taş / (Kül Yağmuru'nda) güvende olan sayısı. "0/3" yazısı.</summary>
        public int Progress { get; private set; }

        /// <summary>Sayaç paydası: taş sayısı / (Kül Yağmuru'nda) yaşayan sayısı.</summary>
        public int Total { get; private set; }

        public double TimeLeftSec { get; private set; }

        /// <summary>Kül Yağmuru başarısızlığında şerit dışında kalanlar (KE-6.13).</summary>
        public IReadOnlyList<int> ExposedMemberIds => _exposed;

        public bool IsLit(int zoneIndex) => zoneIndex >= 0 && zoneIndex < _lit.Length && _lit[zoneIndex];

        /// <summary>Taş başına kesintisiz bekleme (sn) — HUD halkası için.</summary>
        public double HoldProgress(int zoneIndex) =>
            zoneIndex >= 0 && zoneIndex < _hold.Length ? _hold[zoneIndex] : 0.0;

        /// <summary>Kor Mührü'nde ortak bekleme (sn).</summary>
        public double SyncHoldSec => _syncHold;

        public void Start(TeamTaskSpec spec, double nowSec)
        {
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            int n = spec.Zones.Count;
            _lit = new bool[n];
            _hold = new double[n];
            _syncHold = 0.0;
            _startSec = nowSec;
            _lastSec = nowSec;
            _exposed.Clear();
            Progress = 0;
            Total = n;
            TimeLeftSec = spec.DurationSec;
            Status = n == 0 ? TeamTaskStatus.Succeeded : TeamTaskStatus.Running;
        }

        public void Cancel()
        {
            if (Status == TeamTaskStatus.Running)
                Status = TeamTaskStatus.Idle;
        }

        public TeamTaskStatus Tick(double nowSec, IReadOnlyList<TeamTaskMember> members)
        {
            if (Status != TeamTaskStatus.Running)
                return Status;

            double dt = Math.Max(0.0, nowSec - _lastSec);
            _lastSec = nowSec;
            double elapsed = nowSec - _startSec;
            TimeLeftSec = Math.Max(0.0, Spec.DurationSec - elapsed);

            switch (Spec.Kind)
            {
                case TeamTaskKind.SleepSeal: TickSleepSeal(dt, members); break;
                case TeamTaskKind.HeartSeal: TickHeartSeal(dt, members); break;
                case TeamTaskKind.AshRain: TickAshRain(elapsed, members); break;
            }
            return Status;
        }

        void TickSleepSeal(double dt, IReadOnlyList<TeamTaskMember> members)
        {
            var zones = Spec.Zones;
            int lit = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                if (!_lit[i])
                {
                    if (AnyAliveInside(zones[i], members))
                    {
                        _hold[i] += dt;
                        if (_hold[i] >= Spec.HoldSec)
                            _lit[i] = true;
                    }
                    else
                    {
                        _hold[i] = 0.0; // kesintisiz: çıkan sıfırlar
                    }
                }
                if (_lit[i]) lit++;
            }
            Progress = lit;
            if (lit == zones.Count) Status = TeamTaskStatus.Succeeded;
            else if (TimeLeftSec <= 0.0) Status = TeamTaskStatus.Failed;
        }

        void TickHeartSeal(double dt, IReadOnlyList<TeamTaskMember> members)
        {
            var zones = Spec.Zones;
            // Aynı kişi iki taşı birden tutamaz: her taşa ayrı yaşayan kişi gerekir.
            var used = new HashSet<int>();
            int occupied = 0;
            for (int i = 0; i < zones.Count; i++)
            {
                int who = FirstAliveInside(zones[i], members, used);
                if (who >= 0) { used.Add(who); occupied++; }
            }
            Progress = occupied;
            if (occupied == zones.Count)
            {
                _syncHold += dt;
                for (int i = 0; i < _hold.Length; i++) _hold[i] = _syncHold;
                if (_syncHold >= Spec.HoldSec)
                {
                    for (int i = 0; i < _lit.Length; i++) _lit[i] = true;
                    Progress = zones.Count;
                    Status = TeamTaskStatus.Succeeded;
                    return;
                }
            }
            else
            {
                _syncHold = 0.0; // biri çıkarsa hepsi sıfırlanır
                for (int i = 0; i < _hold.Length; i++) _hold[i] = 0.0;
            }
            if (TimeLeftSec <= 0.0) Status = TeamTaskStatus.Failed;
        }

        void TickAshRain(double elapsed, IReadOnlyList<TeamTaskMember> members)
        {
            int alive = 0, ok = 0;
            _exposed.Clear();
            for (int m = 0; m < members.Count; m++)
            {
                var mem = members[m];
                if (!mem.Alive) continue;
                alive++;
                bool safe = false;
                for (int z = 0; z < Spec.Zones.Count && !safe; z++)
                    safe = Spec.Zones[z].Contains(mem.X, mem.Z);
                if (safe) ok++; else _exposed.Add(mem.Id);
            }
            Progress = ok;
            Total = alive;
            if (elapsed >= Spec.DurationSec)
                Status = _exposed.Count == 0 ? TeamTaskStatus.Succeeded : TeamTaskStatus.Failed;
        }

        static bool AnyAliveInside(TeamTaskZone z, IReadOnlyList<TeamTaskMember> members) =>
            FirstAliveInside(z, members, null) >= 0;

        static int FirstAliveInside(TeamTaskZone z, IReadOnlyList<TeamTaskMember> members, HashSet<int>? skip)
        {
            for (int i = 0; i < members.Count; i++)
            {
                var m = members[i];
                if (!m.Alive || (skip != null && skip.Contains(m.Id))) continue;
                if (z.Contains(m.X, m.Z)) return m.Id;
            }
            return -1;
        }
    }
}
