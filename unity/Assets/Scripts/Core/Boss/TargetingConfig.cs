using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>
    /// Hedefleme tasarım sayıları — <c>Resources/Bosses/karadul.json</c> "targeting" bloğu.
    /// Varsayılanlar 2026-10-01 kullanıcı kararı (test boss'u): oyuncu %70 / dost %30,
    /// dost boss vuruşunun yarısını alır, ölürse 8 sn sonra yarım canla kalkar, yem her zaman önce.
    /// </summary>
    public sealed class TargetingConfig
    {
        public float AllyWeight = BossDefaults.AllyTargetWeight;
        public float AllyDamageMult = 0.5f;
        public float AllyReviveSec = BossDefaults.AllyReviveSec;
        public float AllyReviveRatio = 0.5f;
        public bool DecoyPriority = true;

        /// <summary>karadul.json kökünden okur; blok ya da alan yoksa varsayılan kalır.</summary>
        public static TargetingConfig FromJson(JsonValue root)
        {
            var cfg = new TargetingConfig();
            if (root == null || !root.Has("targeting"))
                return cfg;
            JsonValue t = root["targeting"];
            cfg.AllyWeight = Clamp01(t["ally_weight"].AsFloat(cfg.AllyWeight));
            cfg.AllyDamageMult = Math.Max(0f, t["ally_damage_mult"].AsFloat(cfg.AllyDamageMult));
            cfg.AllyReviveSec = Math.Max(0f, t["ally_revive_sec"].AsFloat(cfg.AllyReviveSec));
            cfg.AllyReviveRatio = Clamp01(t["ally_revive_ratio"].AsFloat(cfg.AllyReviveRatio));
            cfg.DecoyPriority = t["decoy_priority"].AsBool(cfg.DecoyPriority);
            return cfg;
        }

        static float Clamp01(float v) => v < 0f ? 0f : v > 1f ? 1f : v;
    }
}
