using System;
using System.Collections.Generic;
using Dovus.Core.Layers;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// docs/element-sistemi.json manipulation_layers.zone_layer — kalıcı alan yaşam döngüsü.
    /// StateBridgeBoard gibi dünyada yaşayan obje listesi: soft-cap + süre bitince düşme.
    /// Görsel yok (Game katmanı); ManifestationDirector'a bağlanmaz (Faz 6).
    /// </summary>
    public sealed class ZoneDirector : IZoneDirector
    {
        readonly List<ZoneInstance> _zones = new();
        int _nextId = 1;

        public ZoneDirector(int maxActiveZones)
        {
            // Soft-cap en az 1 — StateBridgeBoard MaxMarks deseniyle aynı.
            MaxActiveZones = Math.Max(1, maxActiveZones);
        }

        public IReadOnlyList<ZoneInstance> ActiveZones => _zones;
        public int MaxActiveZones { get; }

        public bool TrySpawn(
            string element, string movement, float durationSec,
            float x, float y, float z, float radiusM,
            out ZoneInstance spawned)
        {
            spawned = default;
            if (durationSec <= 0f || radiusM <= 0f)
                return false;

            movement = NormalizeMovement(movement);

            while (_zones.Count >= MaxActiveZones)
                _zones.RemoveAt(0);

            spawned = new ZoneInstance(
                _nextId++,
                element ?? string.Empty,
                x, y, z,
                radiusM,
                durationSec,
                movement);
            _zones.Add(spawned);
            return true;
        }

        public void Tick(float dtSec)
        {
            if (dtSec <= 0f)
                return;

            for (int i = _zones.Count - 1; i >= 0; i--)
            {
                ZoneInstance zone = _zones[i];
                float rem = zone.RemainingSec - dtSec;
                if (rem <= 0f)
                {
                    _zones.RemoveAt(i);
                    continue;
                }

                _zones[i] = WithRemaining(zone, rem);
            }
        }

        public bool Remove(int id)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                if (_zones[i].Id != id)
                    continue;
                _zones.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool MoveZone(int id, float x, float y, float z)
        {
            int i = IndexOf(id);
            if (i < 0)
                return false;

            ZoneInstance zone = _zones[i];
            // Yalnızca oyuncu yönlendirmeli alanlar MoveZone ile kayar.
            if (!string.Equals(zone.Movement, ZoneMovement.PlayerDirected, StringComparison.Ordinal))
                return false;

            _zones[i] = WithPosition(zone, x, y, z);
            return true;
        }

        public bool SetFollowTarget(int id, float x, float y, float z)
        {
            int i = IndexOf(id);
            if (i < 0)
                return false;

            ZoneInstance zone = _zones[i];
            if (!string.Equals(zone.Movement, ZoneMovement.FollowTarget, StringComparison.Ordinal))
                return false;

            _zones[i] = WithPosition(zone, x, y, z);
            return true;
        }

        int IndexOf(int id)
        {
            for (int i = 0; i < _zones.Count; i++)
            {
                if (_zones[i].Id == id)
                    return i;
            }
            return -1;
        }

        /// <summary>Boş/bilinmeyen movement → static (lav_halkasi JSON'da movement yok).</summary>
        static string NormalizeMovement(string movement)
        {
            if (string.IsNullOrEmpty(movement))
                return ZoneMovement.Static;
            if (string.Equals(movement, ZoneMovement.PlayerDirected, StringComparison.Ordinal) ||
                string.Equals(movement, ZoneMovement.FollowTarget, StringComparison.Ordinal) ||
                string.Equals(movement, ZoneMovement.Static, StringComparison.Ordinal))
                return movement;
            return ZoneMovement.Static;
        }

        static ZoneInstance WithRemaining(in ZoneInstance z, float remainingSec) =>
            new(z.Id, z.Element, z.X, z.Y, z.Z, z.RadiusM, remainingSec, z.Movement);

        static ZoneInstance WithPosition(in ZoneInstance z, float x, float y, float zPos) =>
            new(z.Id, z.Element, x, y, zPos, z.RadiusM, z.RemainingSec, z.Movement);
    }
}
