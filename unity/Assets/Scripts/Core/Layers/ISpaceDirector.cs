using System.Collections.Generic;

namespace Dovus.Core.Layers
{
    /// <summary>manipulation_layers.space_layer.effects[].type.</summary>
    public enum SpaceEffectKind : byte
    {
        ShortBlink = 0,
        PhaseBlink = 1,
        InvisibleLink = 2,
        Tear = 3,
        StealthShift = 4
    }

    /// <summary>
    /// Dünyada yaşayan space_layer örneği (link / tear). Blink anlıktır — bu struct'ta tutulmaz.
    /// </summary>
    public struct SpaceEffect
    {
        /// <summary>Çalışma zamanı kimliği (Remove / cross bayrağı).</summary>
        public int RuntimeId;
        /// <summary>JSON effect id (karabasan_hat / hiclik_yarik).</summary>
        public string Id;
        public SpaceEffectKind Kind;
        public float X, Y, Z;
        public float DurationSec;
        /// <summary>tear — JSON damage_on_cross (30).</summary>
        public float DamageOnCross;
        /// <summary>link — hedefe tick hasarı (görev: 3).</summary>
        public float DrainPerTick;
        /// <summary>link — oyuncuya tick heal (görev: 1.5).</summary>
        public float HealPerTick;
        /// <summary>link — kopma mesafesi (görev: 12).</summary>
        public float MaxRangeM;
        public float RemainingSec;
        /// <summary>link sahibi (oyuncu).</summary>
        public string OwnerId;
        /// <summary>link hedefi (boss / düşman).</summary>
        public string TargetId;
    }

    /// <summary>Link tick çıktısı — Game hasar/heal uygular.</summary>
    public readonly struct SpaceLinkTick
    {
        public SpaceLinkTick(int runtimeId, string ownerId, string targetId, float drain, float heal)
        {
            RuntimeId = runtimeId;
            OwnerId = ownerId ?? string.Empty;
            TargetId = targetId ?? string.Empty;
            Drain = drain;
            Heal = heal;
        }

        public int RuntimeId { get; }
        public string OwnerId { get; }
        public string TargetId { get; }
        public float Drain { get; }
        public float Heal { get; }
    }

    /// <summary>
    /// space_layer yaşam döngüsü. Görsel/collider Game'de; Core süre, mesafe, tick, cross.
    /// </summary>
    public interface ISpaceDirector
    {
        IReadOnlyList<SpaceEffect> ActiveEffects { get; }
        int MaxActiveLinks { get; }

        bool TrySpawnLink(
            string effectId, float durationSec,
            float ownerX, float ownerY, float ownerZ,
            float targetX, float targetY, float targetZ,
            string ownerId, string targetId,
            float drainPerTick, float healPerTick, float maxRangeM,
            out SpaceEffect spawned);

        bool TrySpawnTear(
            string effectId, float durationSec,
            float x, float y, float z,
            float damageOnCross,
            out SpaceEffect spawned);

        /// <summary>
        /// Konumları günceller, mesafe/süre düşer, link tick üretir.
        /// owner/target dünya konumu çağıran verir (Unity'siz).
        /// </summary>
        void Tick(
            float dtSec,
            float ownerX, float ownerY, float ownerZ,
            float targetX, float targetY, float targetZ,
            List<SpaceLinkTick> linkTicksOut);

        bool Remove(int runtimeId);

        /// <summary>Owner hasar alınca tüm InvisibleLink'lerini kopar.</summary>
        int BreakLinksOwnedBy(string ownerId);

        /// <summary>
        /// Tear ilk geçiş → damageOnCross; aynı actorId tekrar → 0.
        /// effect yoksa veya tear değilse 0.
        /// </summary>
        float TryCrossTear(int runtimeId, string actorId);
    }

    public static class SpaceEffectKindUtil
    {
        public static bool TryParse(string type, out SpaceEffectKind kind)
        {
            kind = type switch
            {
                "short_blink" => SpaceEffectKind.ShortBlink,
                "phase_blink" => SpaceEffectKind.PhaseBlink,
                "invisible_link" => SpaceEffectKind.InvisibleLink,
                "tear" => SpaceEffectKind.Tear,
                "stealth_shift" => SpaceEffectKind.StealthShift,
                _ => default
            };
            return type is "short_blink" or "phase_blink" or "invisible_link" or "tear" or "stealth_shift";
        }
    }
}
