using System;
using Dovus.Core.Tuning;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Boss saldırısı kare verisi ve etki hacmi — dovus-sistemi.md §11.
    /// Vuruş, aktif pencerenin başında tek karede çözülür.
    /// </summary>
    public sealed class BossAttack
    {
        readonly BossTuning _tuning;

        public BossAttack(BossTuning? tuning = null)
        {
            _tuning = tuning ?? new BossTuning();
        }

        public int WindupMs => _tuning.WindupMs;
        public int ActiveMs => _tuning.ActiveMs;
        public int RecoveryMs => _tuning.RecoveryMs;
        public float RadiusM => _tuning.RadiusM;
        public int Damage => _tuning.Damage;

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
