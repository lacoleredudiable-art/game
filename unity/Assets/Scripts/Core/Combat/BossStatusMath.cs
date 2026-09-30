using System;
using Dovus.Core.Status;

namespace Dovus.Core.Combat
{
    /// <summary>Prototip Dev HP havuzu. Test paneli aç/kapa; varsayılan açık.</summary>
    public static class DevPlayerHp
    {
        public const int Pool = 1_000_000_000;

        public static int Resolve(bool enabled, int normalMaxHp) =>
            enabled ? Pool : Math.Max(1, normalMaxHp);
    }

    /// <summary>
    /// Boss'un durumlara verdiği cevap. Süreler tahtada durur; burası o anki büyüklüğü okur.
    /// </summary>
    public static class BossStatusMath
    {
        /// <summary>Kör büyüklüğü ıskalama şansı (0–1). accuracy_debuff 0.3 → %30.</summary>
        public static float BlindMissChance(StatusBoard board)
        {
            if (board == null)
                return 0f;
            return board.BlindMissChance;
        }

        /// <summary>roll01 [0,1). Şansın altı ıskalar.</summary>
        public static bool Misses(StatusBoard board, float roll01)
        {
            float chance = BlindMissChance(board);
            if (chance <= 0f)
                return false;
            if (roll01 < 0f)
                roll01 = 0f;
            if (roll01 > 1f)
                roll01 = 1f;
            return roll01 < chance;
        }

        public static float OutgoingDamage(float raw, StatusBoard board)
        {
            if (raw <= 0f)
                return 0f;
            float mult = board != null ? board.OutgoingDamageMult : 1f;
            if (mult < 0f)
                mult = 0f;
            return raw * mult;
        }

        /// <summary>
        /// hasar_buff düşmana eksi miktar: giden hasar 1 − |miktar|.
        /// 0.2 güç çalma → çarpan 0.8 (Weaken).
        /// </summary>
        public static float WeakenOutgoingMult(double amount)
        {
            float cut = (float)Math.Abs(amount);
            if (cut < 0f)
                cut = 0f;
            if (cut > 1f)
                cut = 1f;
            return 1f - cut;
        }

        public static bool TryEnemyDamageDebuff(
            double amount,
            double durationSec,
            out float outgoingMult,
            out double durationMs)
        {
            outgoingMult = WeakenOutgoingMult(amount);
            durationMs = durationSec * 1000.0;
            return durationMs > 0d && outgoingMult < 0.999f;
        }

        /// <summary>Tam daire / yer saldırısı. Dar koni nişan ister.</summary>
        public static bool IsGroundAoe(float arcHalfAngleDeg) => arcHalfAngleDeg >= 180f;

        /// <summary>
        /// Gizlilik hedefi düşürür: nişanlı saldırı değmez. Hacimdeki yer/AoE değer.
        /// </summary>
        public static bool VolumeHits(bool targetStealthed, bool inVolume, float arcHalfAngleDeg)
        {
            if (!inVolume)
                return false;
            if (!targetStealthed)
                return true;
            return IsGroundAoe(arcHalfAngleDeg);
        }

        /// <summary>Gizlilik hasar yutmaz. Yalnız Stasis (i-frame) yutar.</summary>
        public static bool DamageInvulnerable(bool stasis) => stasis;

        public static double BlindDurationMs(double baseMs, double lifetimeAddSec) =>
            Math.Max(0d, baseMs) + Math.Max(0d, lifetimeAddSec) * 1000.0;

        /// <summary>accuracy_debuff büyüklüğünü kör ıskalama şansına kırpar.</summary>
        public static float BlindChanceFromAccuracy(float accuracy)
        {
            if (accuracy <= 0f)
                return 0f;
            if (accuracy >= 1f)
                return 1f;
            return accuracy;
        }

        /// <summary>
        /// Kalkanın emdiği (ölçeksiz havuz) + cana geçen hasar. Yansıma ikisini de görür;
        /// kalkan vuruşu yutunca yansıma susmasın.
        /// </summary>
        public static float ReflectBase(float hpDamage, float shieldAbsorbedUnscaled, bool scaled)
        {
            float soaked = shieldAbsorbedUnscaled > 0f
                ? (scaled ? CombatScale.Magnitude(shieldAbsorbedUnscaled) : shieldAbsorbedUnscaled)
                : 0f;
            float through = hpDamage > 0f ? hpDamage : 0f;
            return through + soaked;
        }

        /// <summary>Can bağı / yönlendirme: oran kadar ayrılır, kalan oyuncuda kalır.</summary>
        public static float SplitShare(float incoming, float ratio, out float redirected)
        {
            if (incoming <= 0f || ratio <= 0f)
            {
                redirected = 0f;
                return incoming > 0f ? incoming : 0f;
            }

            float clamped = ratio > 1f ? 1f : ratio;
            redirected = incoming * clamped;
            return incoming - redirected;
        }
    }

    /// <summary>Element status / status_effect / status_duration — yalnız boss'a gidenler.</summary>
    public readonly struct ElementBossStatus
    {
        public ElementBossStatus(StatusKind kind, double durationMs, float magnitude)
        {
            Kind = kind;
            DurationMs = durationMs;
            Magnitude = magnitude;
        }

        public StatusKind Kind { get; }
        public double DurationMs { get; }
        public float Magnitude { get; }
    }

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
                    durationSec * 1000.0,
                    FirstNumber(statusEffect, 3f));
                return true;
            }

            if (string.Equals(status, "weaken", StringComparison.OrdinalIgnoreCase))
            {
                float percent = FirstNumber(statusEffect, 15f);
                if (percent < 0f)
                    percent = -percent;
                if (percent > 100f)
                    percent = 100f;
                apply = new ElementBossStatus(
                    StatusKind.Weaken,
                    durationSec * 1000.0,
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
