using System;
using System.Collections.Generic;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    public readonly struct StateMark
    {
        public StateMark(int id, string type, float x, float z, double expiresAtMs)
        {
            Id = id;
            Type = type ?? string.Empty;
            X = x;
            Z = z;
            ExpiresAtMs = expiresAtMs;
        }

        public int Id { get; }
        public string Type { get; }
        public float X { get; }
        public float Z { get; }
        public double ExpiresAtMs { get; }
    }

    public readonly struct StateBridgeLink
    {
        public StateBridgeLink(int id, string type, int markA, int markB, float ax, float az, float bx, float bz, double expiresAtMs)
        {
            Id = id;
            Type = type ?? string.Empty;
            MarkA = markA;
            MarkB = markB;
            AX = ax; AZ = az;
            BX = bx; BZ = bz;
            ExpiresAtMs = expiresAtMs;
        }

        public int Id { get; }
        public string Type { get; }
        public int MarkA { get; }
        public int MarkB { get; }
        public float AX { get; }
        public float AZ { get; }
        public float BX { get; }
        public float BZ { get; }
        public double ExpiresAtMs { get; }
    }

    /// <summary>
    /// Durum birleşim: aynı tip iki işaret → köprü (portal). Elle portal skill_id yok.
    /// </summary>
    public sealed class StateBridgeBoard
    {
        readonly List<StateMark> _marks = new();
        readonly List<StateBridgeLink> _bridges = new();
        int _nextMarkId = 1;
        int _nextBridgeId = 1;
        double _traverseReadyAtMs;

        public IReadOnlyList<StateMark> Marks => _marks;
        public IReadOnlyList<StateBridgeLink> Bridges => _bridges;

        public StateMark PlaceMark(string type, float x, float z, double worldMs, SkillMotionTuning tuning)
        {
            if (tuning == null)
                tuning = new SkillMotionTuning();
            type ??= SkillMotionMotor.MarkTypeBeacon;

            // Aynı tipe soft-cap: en eskiyi düşür.
            while (_marks.Count >= Math.Max(1, tuning.MaxMarks))
                _marks.RemoveAt(0);

            var mark = new StateMark(
                _nextMarkId++,
                type,
                x, z,
                worldMs + tuning.MarkLifetimeSec * 1000.0);
            _marks.Add(mark);
            TryFormBridges(worldMs, tuning);
            return mark;
        }

        public void Tick(double worldMs, SkillMotionTuning tuning)
        {
            Expire(worldMs);
            if (tuning != null)
                TryFormBridges(worldMs, tuning);
        }

        /// <summary>
        /// Portal girişinde diğer uca ışınlan. true → dest doldu.
        /// </summary>
        public bool TryTraverse(float playerX, float playerZ, double worldMs, SkillMotionTuning tuning, out float destX, out float destZ)
        {
            destX = playerX;
            destZ = playerZ;
            if (tuning == null || worldMs < _traverseReadyAtMs)
                return false;

            float r = tuning.PortalEnterRadiusM;
            float r2 = r * r;

            for (int i = 0; i < _bridges.Count; i++)
            {
                StateBridgeLink b = _bridges[i];
                if (worldMs > b.ExpiresAtMs)
                    continue;

                float da = Dist2(playerX, playerZ, b.AX, b.AZ);
                float db = Dist2(playerX, playerZ, b.BX, b.BZ);
                if (da <= r2)
                {
                    destX = b.BX;
                    destZ = b.BZ;
                    _traverseReadyAtMs = worldMs + tuning.PortalTraverseCooldownSec * 1000.0;
                    return true;
                }
                if (db <= r2)
                {
                    destX = b.AX;
                    destZ = b.AZ;
                    _traverseReadyAtMs = worldMs + tuning.PortalTraverseCooldownSec * 1000.0;
                    return true;
                }
            }

            return false;
        }

        void TryFormBridges(double worldMs, SkillMotionTuning tuning)
        {
            Expire(worldMs);
            float maxDist = tuning.BridgeMaxDistanceM;
            float maxDist2 = maxDist * maxDist;

            for (int i = 0; i < _marks.Count; i++)
            {
                for (int j = i + 1; j < _marks.Count; j++)
                {
                    StateMark a = _marks[i];
                    StateMark b = _marks[j];
                    if (!string.Equals(a.Type, b.Type, StringComparison.Ordinal))
                        continue;
                    if (Dist2(a.X, a.Z, b.X, b.Z) > maxDist2)
                        continue;
                    if (BridgeExists(a.Id, b.Id))
                        continue;
                    if (_bridges.Count >= Math.Max(1, tuning.MaxBridges))
                        _bridges.RemoveAt(0);

                    _bridges.Add(new StateBridgeLink(
                        _nextBridgeId++,
                        a.Type,
                        a.Id, b.Id,
                        a.X, a.Z, b.X, b.Z,
                        worldMs + tuning.BridgeDurationSec * 1000.0));
                }
            }
        }

        bool BridgeExists(int markA, int markB)
        {
            for (int i = 0; i < _bridges.Count; i++)
            {
                StateBridgeLink b = _bridges[i];
                if ((b.MarkA == markA && b.MarkB == markB) || (b.MarkA == markB && b.MarkB == markA))
                    return true;
            }
            return false;
        }

        void Expire(double worldMs)
        {
            for (int i = _marks.Count - 1; i >= 0; i--)
            {
                if (worldMs > _marks[i].ExpiresAtMs)
                    _marks.RemoveAt(i);
            }

            for (int i = _bridges.Count - 1; i >= 0; i--)
            {
                StateBridgeLink b = _bridges[i];
                if (worldMs > b.ExpiresAtMs || !MarkAlive(b.MarkA) || !MarkAlive(b.MarkB))
                    _bridges.RemoveAt(i);
            }
        }

        bool MarkAlive(int id)
        {
            for (int i = 0; i < _marks.Count; i++)
                if (_marks[i].Id == id) return true;
            return false;
        }

        static float Dist2(float ax, float az, float bx, float bz)
        {
            float dx = ax - bx;
            float dz = az - bz;
            return dx * dx + dz * dz;
        }
    }
}
