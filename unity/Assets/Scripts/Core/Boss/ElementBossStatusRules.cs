using System;
using Dovus.Core.Status;
using Dovus.Core.Damage;

namespace Dovus.Core.Boss
{
    public static class ElementBossStatusRules
    {
        /// <summary>
        /// Ateş burn ve Karanlık weaken boss'a iner. Şifa/hız/kalkan/arınma kendinedir.
        /// Sayılar elements[].status_effect ve status_duration metninden.
        /// </summary>
        public static bool TryForBoss(
            string status,
            string statusEffect,
            float durationSec,
            out ElementBossStatus apply)
        {
            apply = default;
            if (string.IsNullOrEmpty(status) || durationSec <= 0f)
                return false;

            if (string.Equals(status, "burn", StringComparison.OrdinalIgnoreCase))
            {
                apply = new ElementBossStatus(
                    StatusKind.Burn,
                    durationSec * BossStatusMathDefaults.SecToMs,
                    FirstNumber(statusEffect, BossStatusMathDefaults.DefaultBurnDps));
                return true;
            }

            if (string.Equals(status, "weaken", StringComparison.OrdinalIgnoreCase))
            {
                float percent = FirstNumber(statusEffect, BossStatusMathDefaults.DefaultWeakenPercent);
                if (percent < 0f)
                    percent = -percent;
                if (percent > 100f)
                    percent = 100f;
                apply = new ElementBossStatus(
                    StatusKind.Weaken,
                    durationSec * BossStatusMathDefaults.SecToMs,
                    1f - percent / 100f);
                return true;
            }

            return false;
        }

        static float FirstNumber(string text, float fallback)
        {
            if (string.IsNullOrEmpty(text))
                return fallback;
            int start = -1;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c >= '0' && c <= '9')
                {
                    start = i;
                    break;
                }
            }
            if (start < 0)
                return fallback;
            int end = start;
            while (end < text.Length && text[end] >= '0' && text[end] <= '9')
                end++;
            if (int.TryParse(text.Substring(start, end - start), out int value) && value > 0)
                return value;
            return fallback;
        }
    }
}
