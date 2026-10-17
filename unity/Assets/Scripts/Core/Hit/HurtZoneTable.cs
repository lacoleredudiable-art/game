using System.Collections.Generic;

namespace Dovus.Core.Hit
{
    /// <summary>Bir aktörün bölge listesi. Gövde bölgesi her zaman vardır.</summary>
    public sealed class HurtZoneTable
    {
        readonly Dictionary<string, HurtZone> _zones = new Dictionary<string, HurtZone>();

        public HurtZoneTable()
        {
            _zones[HitDefaults.BodyZoneId] = new HurtZone(HitDefaults.BodyZoneId, HitDefaults.NeutralZoneFactor);
        }

        public int Count => _zones.Count;

        public HurtZoneTable Add(string id, float damageFactor)
        {
            _zones[id] = new HurtZone(id, damageFactor);
            return this;
        }

        /// <summary>Boş ya da null kimlik gövde bölgesidir. Tanımsız bölge false döner (sessiz gövdeye düşülmez).</summary>
        public bool TryGet(string? id, out HurtZone zone) =>
            _zones.TryGetValue(string.IsNullOrEmpty(id) ? HitDefaults.BodyZoneId : id!, out zone);
    }
}
