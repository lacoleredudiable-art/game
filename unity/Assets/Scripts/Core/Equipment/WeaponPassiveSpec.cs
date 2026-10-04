using System;

using Dovus.Core.Shared;
namespace Dovus.Core.Equipment
{
    /// <summary>Silah pasifinin JSON sayıları. Davranış <see cref="WeaponPassiveRules"/> içindedir.</summary>
    public sealed class WeaponPassiveSpec
    {
        public static readonly WeaponPassiveSpec None = new(string.Empty, 1f, 0f, 0f, 0f, 0f, 0f, 0f, 1f, 1f, 0, 0f, 0f, 0f, WeaponCombatProfileDefaults.DefaultBaseCrit);

        public WeaponPassiveSpec(
            string id,
            float damageMult,
            float arcDeg,
            float stunSec,
            float icdSec,
            float windowSec,
            float moveWindowSec,
            float stillSec,
            float durationMult,
            float powerMult,
            int everyNth,
            float chainGapSec,
            float angleDeg,
            float critChance,
            float baseCrit)
        {
            Id = id ?? string.Empty;
            Kind = WeaponPassiveKinds.Parse(Id);
            DamageMult = damageMult > 0f ? damageMult : 1f;
            ArcDeg = arcDeg;
            StunSec = stunSec;
            IcdSec = icdSec;
            WindowSec = windowSec;
            MoveWindowSec = moveWindowSec;
            StillSec = stillSec;
            DurationMult = durationMult > 0f ? durationMult : 1f;
            PowerMult = powerMult > 0f ? powerMult : 1f;
            EveryNth = everyNth;
            ChainGapSec = chainGapSec;
            AngleDeg = angleDeg;
            CritChance = critChance;
            BaseCrit = baseCrit > 0f ? baseCrit : WeaponCombatProfileDefaults.DefaultBaseCrit;
        }

        public string Id { get; }
        public WeaponPassiveKind Kind { get; }
        public float DamageMult { get; }
        public float ArcDeg { get; }
        public float StunSec { get; }
        public float IcdSec { get; }
        public float WindowSec { get; }
        public float MoveWindowSec { get; }
        public float StillSec { get; }
        public float DurationMult { get; }
        public float PowerMult { get; }
        public int EveryNth { get; }
        public float ChainGapSec { get; }
        public float AngleDeg { get; }
        public float CritChance { get; }
        public float BaseCrit { get; }

        public static WeaponPassiveSpec FromJson(JsonValue row)
        {
            if (row.IsNull || row.Kind != JsonKind.Object)
                return None;
            return new WeaponPassiveSpec(
                row["id"].AsString(),
                row["damage_mult"].AsFloat(1f),
                row["arc_deg"].AsFloat(0f),
                row["stun_sec"].AsFloat(0f),
                row["icd_sec"].AsFloat(0f),
                row["window_sec"].AsFloat(0f),
                row["move_window_sec"].AsFloat(0f),
                row["still_sec"].AsFloat(0f),
                row["duration_mult"].AsFloat(1f),
                row["power_mult"].AsFloat(1f),
                row["every_nth"].AsInt(0),
                row["chain_gap_sec"].AsFloat(0f),
                row["angle_deg"].AsFloat(0f),
                row["crit_chance"].AsFloat(0f),
                row["base_crit"].AsFloat(WeaponCombatProfileDefaults.DefaultBaseCrit));
        }
    }
}
