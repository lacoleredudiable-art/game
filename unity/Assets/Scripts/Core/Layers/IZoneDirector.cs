using System.Collections.Generic;

namespace Dovus.Core.Layers
{
    /// <summary>
    /// manipulation_layers.zone_layer — dünyada yaşayan kalıcı alan örneği.
    /// Id örnek kimliği (StateBridgeBoard.StateMark gibi); Element/Movement JSON'dan gelir.
    /// </summary>
    public readonly struct ZoneInstance
    {
        public ZoneInstance(
            int id, string element, float x, float y, float z,
            float radiusM, float remainingSec, string movement)
        {
            Id = id;
            Element = element ?? string.Empty;
            X = x;
            Y = y;
            Z = z;
            RadiusM = radiusM;
            RemainingSec = remainingSec;
            Movement = movement ?? string.Empty;
        }

        public int Id { get; }
        public string Element { get; }
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float RadiusM { get; }
        public float RemainingSec { get; }
        /// <summary>"static" | "player_directed" | "follow_target" (JSON movement).</summary>
        public string Movement { get; }
    }

    /// <summary>
    /// Zone katmanı arayüzü. Görsel spawn Game katmanında; Core yalnızca yaşam süresi + konum.
    /// </summary>
    public interface IZoneDirector
    {
        IReadOnlyList<ZoneInstance> ActiveZones { get; }
        int MaxActiveZones { get; }

        /// <summary>
        /// Yeni zone ekler. Limit doluysa en eskisi düşer (StateBridgeBoard soft-cap deseni).
        /// durationSec ≤ 0 veya radiusM ≤ 0 ise false.
        /// </summary>
        bool TrySpawn(
            string element, string movement, float durationSec,
            float x, float y, float z, float radiusM,
            out ZoneInstance spawned);

        /// <summary>Kalan süreyi düşürür; RemainingSec ≤ 0 olanları siler.</summary>
        void Tick(float dtSec);

        bool Remove(int id);

        /// <summary>
        /// Konum güncelleme — movement tipine göre:
        /// player_directed → mutlak konum set edilir;
        /// follow_target / static → false (değişmez).
        /// </summary>
        bool MoveZone(int id, float x, float y, float z);

        /// <summary>
        /// follow_target zone'u hedefe çeker. Diğer movement tiplerinde false.
        /// </summary>
        bool SetFollowTarget(int id, float x, float y, float z);
    }

    /// <summary>JSON movement string sabitleri — kombo tablosu değil, tip etiketleri.</summary>
    public static class ZoneMovement
    {
        public const string Static = "static";
        public const string PlayerDirected = "player_directed";
        public const string FollowTarget = "follow_target";
    }
}
