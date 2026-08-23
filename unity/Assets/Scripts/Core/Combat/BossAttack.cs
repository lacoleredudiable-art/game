using System;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Boss saldırısı kare verisi ve etki hacmi — dovus-sistemi.md §11.
    /// Vuruş, aktif pencerenin başında tek karede çözülür.
    /// Windup/radius aktif çakma varyantından gelir; Active/Recovery paylaşılır.
    /// </summary>
    public sealed class BossAttack
    {
        readonly BossTuning _tuning;
        int _windupMs;
        float _radiusM;
        SlamVariant _variant;

        public BossAttack(BossTuning? tuning = null)
        {
            _tuning = tuning ?? new BossTuning();
            ApplyVariant(SlamVariant.Yakin);
        }

        public SlamVariant Variant => _variant;
        public int WindupMs => _windupMs;
        public int ActiveMs => _tuning.ActiveMs;
        public int RecoveryMs => _tuning.RecoveryMs;
        public float RadiusM => _radiusM;
        public int Damage => _tuning.Damage;

        /// <summary>Bir sonraki çakmanın ritmini ayarlar — telegraf başlamadan çağrılır.</summary>
        public void ApplyVariant(SlamVariant variant)
        {
            _variant = variant;
            _windupMs = _tuning.WindupMsFor(variant);
            _radiusM = _tuning.RadiusMFor(variant);
        }

        public int StrikeTimeMs(int telegraphStartMs) => telegraphStartMs + WindupMs;

        public int ActiveEndMs(int telegraphStartMs) => telegraphStartMs + WindupMs + ActiveMs;

        public int RecoveryEndMs(int telegraphStartMs) =>
            telegraphStartMs + WindupMs + ActiveMs + RecoveryMs;

        /// <summary>
        /// Oyuncu etki hacminde mi? Mesafe yarıçap içinde ve açı yayı içinde olmalı.
        /// Tam daire için arcHalfAngleDeg = 180 (veya daha büyük).
        /// </summary>
        public bool IsInEffectVolume(float distanceM, float angleFromForwardDeg, float arcHalfAngleDeg = 180f)
        {
            if (distanceM > RadiusM)
                return false;

            if (arcHalfAngleDeg >= 180f)
                return true;

            return MathF.Abs(NormalizeAngle(angleFromForwardDeg)) <= arcHalfAngleDeg;
        }

        static float NormalizeAngle(float degrees)
        {
            float a = degrees % 360f;
            if (a > 180f)
                a -= 360f;
            else if (a < -180f)
                a += 360f;
            return a;
        }
    }
}
