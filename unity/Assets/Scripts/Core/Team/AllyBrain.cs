using System;
using System.Collections.Generic;

namespace Dovus.Core.Team
{
    /// <summary>
    /// Yapay zekâ dost beyni (KE-10.2, KE-10.3): öncelik sırası tehlike → takım görevi → saldırı → oyuncuyu izle.
    /// Dünya yalnız <see cref="Decide"/>'ın sonucunu uygular (NavMesh/animasyon Unity tarafında). Kusursuz değil:
    /// 0,3 s tepki gecikmesi, her 5 ön işaretten 1'inde geç kalma.
    /// </summary>
    public sealed class AllyBrain
    {
        readonly Dictionary<int, double> _seenAt = new Dictionary<int, double>();
        readonly Dictionary<int, bool> _late = new Dictionary<int, bool>();
        int _warned;

        public AllyDecision Decide(double nowSec, in AllyContext c)
        {
            var danger = ReactToDanger(nowSec, c);
            if (danger.HasValue) return danger.Value;

            if (c.AssignedZone.HasValue)
            {
                var z = c.AssignedZone.Value;
                // Bölgenin içindeyse dur (bekleme sayılır); değilse merkeze yürü.
                if (z.Contains(c.X, c.Z)) return AllyDecision.Idle;
                return AllyDecision.Move(z.X, z.Z);
            }

            var target = c.Role == AllyRole.Ranged ? c.HeadTarget : c.ClawTarget;
            if (target.HasValue) return Engage(c, target.Value);

            float dx = c.PlayerX - c.X, dz = c.PlayerZ - c.Z;
            if (dx * dx + dz * dz > TeamTaskDefaults.AllyFollowDistanceM * TeamTaskDefaults.AllyFollowDistanceM)
                return AllyDecision.Move(c.PlayerX, c.PlayerZ);
            return AllyDecision.Idle;
        }

        AllyDecision? ReactToDanger(double nowSec, in AllyContext c)
        {
            AllyDecision? best = null;
            double bestHit = double.MaxValue;
            for (int i = 0; i < c.Dangers.Count; i++)
            {
                var d = c.Dangers[i];
                if (!d.ArenaWide && !Inside(d, c)) continue;
                if (!_seenAt.ContainsKey(d.Id))
                {
                    _seenAt[d.Id] = nowSec;
                    _warned++;
                    // KE-10.3: her N. ön işarette geç kal.
                    _late[d.Id] = _warned % TeamTaskDefaults.AllyLateEveryNth == 0;
                }
                double delay = TeamTaskDefaults.AllyReactionDelaySec + (_late[d.Id] ? TeamTaskDefaults.AllyLateExtraDelaySec : 0.0);
                if (nowSec - _seenAt[d.Id] < delay) continue;
                if (d.HitInSec >= bestHit) continue;

                AllyDecision? act = d.ArenaWide ? ArenaWide(d, c) : Escape(d, c);
                if (act.HasValue) { best = act; bestHit = d.HitInSec; }
            }
            return best;
        }

        static bool Inside(AllyDanger d, in AllyContext c)
        {
            float dx = c.X - d.X, dz = c.Z - d.Z, r = d.Radius + c.Radius;
            return dx * dx + dz * dz <= r * r;
        }

        static AllyDecision Escape(AllyDanger d, in AllyContext c)
        {
            float dx = c.X - d.X, dz = c.Z - d.Z;
            float len = (float)Math.Sqrt(dx * dx + dz * dz);
            if (len < 1e-4f) { dx = 1f; dz = 0f; len = 1f; }
            float exitDist = d.Radius + c.Radius + TeamTaskDefaults.AllyExitMarginM;
            return AllyDecision.Move(d.X + dx / len * exitDist, d.Z + dz / len * exitDist);
        }

        static AllyDecision? ArenaWide(AllyDanger d, in AllyContext c)
        {
            // Taş gölgesi varsa en yakınına; yoksa vuruşa az kala dodge.
            TeamTaskZone? best = null;
            float bestD = float.MaxValue;
            for (int i = 0; i < c.Shelters.Count; i++)
            {
                float dist = c.Shelters[i].DistanceTo(c.X, c.Z);
                if (dist < bestD) { bestD = dist; best = c.Shelters[i]; }
            }
            if (best.HasValue)
                return best.Value.Contains(c.X, c.Z) ? AllyDecision.Idle : AllyDecision.Move(best.Value.X, best.Value.Z);
            if (d.HitInSec <= TeamTaskDefaults.AllyDodgeLeadSec)
                return AllyDecision.DodgeTo(c.PlayerX, c.PlayerZ);
            return null;
        }

        static AllyDecision Engage(in AllyContext c, AllyTarget t)
        {
            float dx = t.X - c.X, dz = t.Z - c.Z;
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            if (dist <= c.AttackRangeM) return AllyDecision.Strike(t);
            // Menzil kenarına kadar yaklaş (hedefin üstüne binme).
            float k = (dist - c.AttackRangeM * 0.9f) / dist;
            return AllyDecision.Move(c.X + dx * k, c.Z + dz * k);
        }
    }
}
