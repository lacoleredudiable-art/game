using System;
using Dovus.Core.Status;
using Dovus.Core.Damage;
using Dovus.Core.Shared;

namespace Dovus.Core.Boss
{
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
            durationMs = durationSec * Units.SecToMs;
            return durationMs > 0d && outgoingMult < BossStatusMathDefaults.OutgoingMultCap;
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
            Math.Max(0d, baseMs) + Math.Max(0d, lifetimeAddSec) * Units.SecToMs;

        /// <summary>accuracy_debuff büyüklüğünü kör ıskalama şansına kırpar.</summary>
        public static float BlindChanceFromAccuracy(float accuracy) =>
            StatusMath.BlindChanceFromAccuracy(accuracy);

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
}
