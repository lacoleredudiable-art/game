using System;

using Dovus.Core.Shared;
namespace Dovus.Core.Equipment
{
    public sealed class WeaponSwapBonusSpec
    {
        public static readonly WeaponSwapBonusSpec None = new(string.Empty, 2f, 1f, 0f, 0f, 0f, 0f, 1f, false, false, false);

        public WeaponSwapBonusSpec(
            string id,
            float windowSec,
            float damageMult,
            float arcDeg,
            float poiseMult,
            float shield,
            float shieldSec,
            float areaMult,
            bool ignoreArmor,
            bool cleanse,
            bool freeMana)
        {
            Id = id ?? string.Empty;
            WindowSec = windowSec > 0f ? windowSec : 2f;
            DamageMult = damageMult > 0f ? damageMult : 1f;
            ArcDeg = arcDeg;
            PoiseMult = poiseMult > 0f ? poiseMult : 1f;
            Shield = shield;
            ShieldSec = shieldSec > 0f ? shieldSec : 0f;
            AreaMult = areaMult > 0f ? areaMult : 1f;
            IgnoreArmor = ignoreArmor;
            Cleanse = cleanse;
            FreeMana = freeMana;
        }

        public string Id { get; }
        public float WindowSec { get; }
        public float DamageMult { get; }
        public float ArcDeg { get; }
        public float PoiseMult { get; }
        public float Shield { get; }
        public float ShieldSec { get; }
        public float AreaMult { get; }
        public bool IgnoreArmor { get; }
        public bool Cleanse { get; }
        public bool FreeMana { get; }
        public bool CountsAsBackstab => string.Equals(Id, "sirt", StringComparison.Ordinal);
        public bool CountsAsStill => string.Equals(Id, "sabit", StringComparison.Ordinal);
        public bool SpawnsAtLastHit => string.Equals(Id, "son_vurus", StringComparison.Ordinal);

        public static WeaponSwapBonusSpec FromJson(JsonValue row)
        {
            if (row.IsNull || row.Kind != JsonKind.Object)
                return None;
            return new WeaponSwapBonusSpec(
                row["id"].AsString(),
                row["window_sec"].AsFloat(2f),
                row["damage_mult"].AsFloat(1f),
                row["arc_deg"].AsFloat(0f),
                row["poise_mult"].AsFloat(1f),
                row["shield"].AsFloat(0f),
                row["shield_sec"].AsFloat(0f),
                row["area_mult"].AsFloat(1f),
                row["ignore_armor"].AsBool(false),
                row["cleanse"].AsBool(false),
                row["free_mana"].AsBool(false));
        }
    }
}
