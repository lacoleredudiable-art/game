using System;
using System.Collections.Generic;
using Dovus.Core.Grammar;

using Dovus.Core.Shared;
namespace Dovus.Core.Boss
{
    /// <summary>Dost kuklanın boss hasarı ve yeniden kalkışı (saf; zaman parametre, dünya saati).</summary>
    public static class AllyLifeRules
    {
        /// <summary>Boss ham hasarı × ally_damage_mult (DamagePipeline'a girmeden önce).</summary>
        public static float AllyRawDamage(float raw, TargetingConfig cfg) =>
            raw <= 0f ? 0f : raw * Math.Max(0f, (cfg ?? new TargetingConfig()).AllyDamageMult);

        /// <summary>Düştüğü andan ally_revive_sec geçti mi. downAtMs &lt; 0: düşmedi.</summary>
        public static bool ReviveDue(double downAtMs, double nowMs, TargetingConfig cfg)
        {
            if (downAtMs < 0)
                return false;
            double sec = Math.Max(0f, (cfg ?? new TargetingConfig()).AllyReviveSec);
            return nowMs - downAtMs >= sec * Units.SecToMs;
        }

        /// <summary>Kalkış canı: max × ally_revive_ratio, en az 1.</summary>
        public static int ReviveHp(int maxHp, TargetingConfig cfg)
        {
            int max = Math.Max(1, maxHp);
            float ratio = (cfg ?? new TargetingConfig()).AllyReviveRatio;
            int hp = (int)Math.Round(max * ratio, MidpointRounding.AwayFromZero);
            return Math.Max(1, Math.Min(max, hp));
        }
    }
}
