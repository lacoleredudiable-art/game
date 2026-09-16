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
        BossAttackKind _kind = BossAttackKind.Slam;
        float _arcHalfAngleDeg = 180f;

        public BossAttack(BossTuning? tuning = null)
        {
            _tuning = tuning ?? new BossTuning();
            ApplyVariant(SlamVariant.Yakin);
        }

        public SlamVariant Variant => _variant;
        public BossAttackKind Kind => _kind;
        public int WindupMs => _windupMs;
        public int ActiveMs => _tuning.ActiveMs;
        public int RecoveryMs => _tuning.RecoveryMs;
        public float RadiusM => _radiusM;
        /// <summary>Tam daire (Slam) = 180; FireCone daha dar bir yay.</summary>
        public float ArcHalfAngleDeg => _arcHalfAngleDeg;
        public int Damage => _kind == BossAttackKind.FireCone ? _tuning.FireConeDamage : _tuning.Damage;

        /// <summary>Bir sonraki çakmanın ritmini ayarlar (Slam) — telegraf başlamadan çağrılır.</summary>
        public void ApplyVariant(SlamVariant variant)
        {
            _kind = BossAttackKind.Slam;
            _variant = variant;
            _windupMs = _tuning.WindupMsFor(variant);
            _radiusM = _tuning.RadiusMFor(variant);
            _arcHalfAngleDeg = 180f;
        }

        /// <summary>
        /// 16 Eylül — ikinci saldırı türü: Cehennem Nefesi (karadul.json "fire_cone").
        /// Dar bir koni; tam daire değil.
        /// </summary>
        public void ApplyFireCone()
        {
            _kind = BossAttackKind.FireCone;
            _variant = SlamVariant.Yakin;
            _windupMs = _tuning.FireConeWindupMs;
            _radiusM = _tuning.FireConeRadiusM;
            _arcHalfAngleDeg = _tuning.FireConeArcHalfAngleDeg;
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
