using System;
using Dovus.Core;
using Dovus.Core.Grammar;
using Dovus.Core.Shared;

namespace Dovus.Core.Status
{
    /// <summary>
    /// Kart metni (skill.effect) ile uygulanan durum türünü aynı hizada tutar.
    /// Sayı uydurmaz: süre ve güç JSON alanından, yoksa tek yedekten gelir.
    /// </summary>
    public static class CardEffectRules
    {
        public static bool Names(string effect, string token) =>
            ContainsToken(effect, token);

        public static bool NamesSlow(string effect) =>
            !string.IsNullOrEmpty(effect)
            && effect.IndexOf("yavaş", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool NamesBlind(string effect) =>
            !string.IsNullOrEmpty(effect)
            && effect.IndexOf("kör", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool NamesAccuracy(string effect) =>
            !string.IsNullOrEmpty(effect)
            && effect.IndexOf("isabet", StringComparison.OrdinalIgnoreCase) >= 0;

        public static bool NamesEnemySlow(string effect)
        {
            if (string.IsNullOrEmpty(effect))
                return false;
            bool enemy = effect.IndexOf("düşman", StringComparison.OrdinalIgnoreCase) >= 0
                || effect.IndexOf("dusman", StringComparison.OrdinalIgnoreCase) >= 0;
            bool slow = NamesSlow(effect)
                || effect.IndexOf("hızı", StringComparison.OrdinalIgnoreCase) >= 0
                || effect.IndexOf("hizi", StringComparison.OrdinalIgnoreCase) >= 0;
            return enemy && slow;
        }

        public static bool WantsSelfHaste(string effect)
        {
            if (string.IsNullOrEmpty(effect) || NamesEnemySlow(effect))
                return false;
            return effect.IndexOf("haste", StringComparison.OrdinalIgnoreCase) >= 0
                || effect.IndexOf("hız buff", StringComparison.OrdinalIgnoreCase) >= 0
                || effect.IndexOf("hiz buff", StringComparison.OrdinalIgnoreCase) >= 0
                || effect.IndexOf("hareket/cast", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>İyileştirme, kalkan, buff ve dosta giden fiiller.</summary>
        public static bool PrefersAlly(string targetMode, string action)
        {
            if (action is "heal" or "shield" or "buff" or "regen")
                return true;
            return string.Equals(targetMode, "self_or_ally", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Dost hedefi skill silah menzilini kullanmaz.</summary>
        public static float ResolveRange(bool allyTargeted, float allyRangeM, float weaponRangeM) =>
            allyTargeted && allyRangeM > 0f ? allyRangeM : weaponRangeM;

        /// <summary>
        /// Dosta giden becerideki zararlı ek, JSON düşman diyorsa boss'a gider
        /// (vuran / düşman yavaşlat). Aksi halde boss'a inmez.
        /// </summary>
        public static bool HarmfulHitsEnemy(string targetMode, string action, string effect)
        {
            if (!PrefersAlly(targetMode, action))
                return true;
            if (string.IsNullOrEmpty(effect))
                return false;
            return effect.IndexOf("vuran", StringComparison.OrdinalIgnoreCase) >= 0
                || NamesEnemySlow(effect);
        }

        /// <summary>Kart "stun" diyorsa motorun varsayılan kökü stun olur. Tersi de geçerli.</summary>
        public static string CcKind(string effect, string engineKind)
        {
            bool stun = Names(effect, "stun");
            bool root = Names(effect, "root");
            if (stun && !root)
                return "stun";
            if (root && !stun)
                return "root";
            return engineKind ?? string.Empty;
        }

        /// <summary>Kart yalnız yavaşlatma diyorsa isabet cezası kör değil yavaşlatmadır.</summary>
        public static bool AccuracyIsSlow(string effect) =>
            NamesSlow(effect) && !NamesBlind(effect) && !NamesAccuracy(effect);

        public static StatusKind MovementLockKind(string effect, bool daze)
        {
            string kind = CcKind(effect, daze ? "stun" : "root");
            return string.Equals(kind, "stun", StringComparison.OrdinalIgnoreCase)
                ? StatusKind.Stun
                : StatusKind.Root;
        }

        /// <summary>Kartın tek başına adlandırdığı CC. Birden fazla veya hiç yoksa None.</summary>
        public static StatusKind ExclusiveKind(string effect)
        {
            bool stun = Names(effect, "stun");
            bool root = Names(effect, "root");
            bool haste = WantsSelfHaste(effect);
            bool slow = NamesSlow(effect) || NamesEnemySlow(effect);
            bool blind = NamesBlind(effect);
            if (stun && !root && !haste)
                return StatusKind.Stun;
            if (root && !stun && !haste)
                return StatusKind.Root;
            if (haste && !slow && !stun && !root)
                return StatusKind.Haste;
            if (slow && !blind && !haste && !stun && !root && !NamesAccuracy(effect))
                return StatusKind.Slow;
            if (blind && !slow && !stun && !root)
                return StatusKind.Blind;
            return StatusKind.None;
        }

        public static float HasteMagnitude(string effect, float selfHaste, float enemySlow, float damageBuff)
        {
            if (selfHaste > 1f)
                return selfHaste;
            if (selfHaste > 0f)
                return 1f + selfHaste;
            if (enemySlow > 0f && enemySlow < 1f)
                return 1f + (1f - enemySlow);
            if (damageBuff > 0f)
                return 1f + damageBuff;
            DesignWarnings.Once(
                "self_haste",
                "element-sistemi.json kendine hız sayısı yok; yedek +%50 kullanıldı.");
            return 1f + StatusDefaults.SelfHasteBonus;
        }

        public static bool SharesHasteWithAlly(string effect) =>
            !string.IsNullOrEmpty(effect)
            && effect.IndexOf("ikisi", StringComparison.OrdinalIgnoreCase) >= 0;

        static bool ContainsToken(string text, string token)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token))
                return false;
            int i = 0;
            while ((i = text.IndexOf(token, i, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                bool left = i == 0 || !char.IsLetter(text[i - 1]);
                int end = i + token.Length;
                bool right = end >= text.Length || !char.IsLetter(text[end]);
                if (left && right)
                    return true;
                i = end;
            }
            return false;
        }
    }
}
