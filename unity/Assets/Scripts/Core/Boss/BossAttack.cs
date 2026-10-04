using System;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Boss
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
        int _volleyCount;
        float _landingX;
        float _landingZ;

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
        public int Damage => _kind switch
        {
            BossAttackKind.FireCone => _tuning.FireConeDamage,
            BossAttackKind.Volley => _tuning.VolleyDamage,
            BossAttackKind.WebField => 0,
            BossAttackKind.Pounce => _tuning.PounceDamage,
            _ => _tuning.Damage
        };

        /// <summary>Pounce: windup başında kilitlenen iniş X (dünya).</summary>
        public float LandingX => _landingX;
        /// <summary>Pounce: windup başında kilitlenen iniş Z (dünya).</summary>
        public float LandingZ => _landingZ;

        /// <summary>Volley: bu salvodaki mermi sayısı (öfkede count_enraged).</summary>
        public int VolleyCount => _kind == BossAttackKind.Volley ? _volleyCount : 0;
        /// <summary>Volley yelpazesinin TAM açısı (derece). Körlük çağıranda +%50 genişletir.</summary>
        public float VolleySpreadDeg => _tuning.VolleySpreadDeg;
        public float VolleySpeedMps => _tuning.VolleySpeedMps;
        public float VolleyRadiusM => _tuning.VolleyRadiusM;
        public float VolleyLifeSec => _tuning.VolleyLifeSec;

        /// <summary>Bir sonraki çakmanın ritmini ayarlar (Slam) — telegraf başlamadan çağrılır.</summary>
        public void ApplyVariant(SlamVariant variant)
        {
            _kind = BossAttackKind.Slam;
            _volleyCount = 0;
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
            _volleyCount = 0;
            _variant = SlamVariant.Yakin;
            _windupMs = _tuning.FireConeWindupMs;
            _radiusM = _tuning.FireConeRadiusM;
            _arcHalfAngleDeg = _tuning.FireConeArcHalfAngleDeg;
        }

        /// <summary>
        /// Zehir Tükürüğü (karadul.json "volley"): yelpaze mermi. Telegraf dar yay (yarı açı =
        /// yelpazenin yarısı), boyu VolleyTelegraphRangeM. Vuruş anında hacim testi yapılmaz.
        /// </summary>
        public void ApplyVolley(bool enraged)
        {
            _kind = BossAttackKind.Volley;
            _variant = SlamVariant.Yakin;
            _windupMs = _tuning.VolleyWindupMs;
            _radiusM = _tuning.VolleyTelegraphRangeM;
            _arcHalfAngleDeg = Math.Max(1f, _tuning.VolleySpreadDeg * 0.5f);
            _volleyCount = Math.Max(1, enraged ? _tuning.VolleyCountEnraged : _tuning.VolleyCount);
        }

        /// <summary>Ağ Örme: hedef konumunda disk telegrafı; anlık hasar yok.</summary>
        public void ApplyWebField()
        {
            _kind = BossAttackKind.WebField;
            _volleyCount = 0;
            _variant = SlamVariant.Yakin;
            _windupMs = _tuning.WebFieldWindupMs;
            _radiusM = _tuning.WebFieldRadiusM;
            _arcHalfAngleDeg = 180f;
        }

        /// <summary>Sıçrayış: iniş dairesi; hasar iniş anında. İniş noktası windup öncesi SetLanding ile verilir.</summary>
        public void ApplyPounce()
        {
            _kind = BossAttackKind.Pounce;
            _volleyCount = 0;
            _variant = SlamVariant.Yakin;
            _windupMs = _tuning.PounceWindupMs;
            _radiusM = _tuning.PounceLandRadiusM;
            _arcHalfAngleDeg = 180f;
        }

        /// <summary>Pounce windup başında kilitlenen iniş konumu (dünya XZ).</summary>
        public void SetLanding(float x, float z)
        {
            _landingX = x;
            _landingZ = z;
        }

        public int StrikeTimeMs(int telegraphStartMs) => telegraphStartMs + WindupMs;

        public int ActiveEndMs(int telegraphStartMs) => telegraphStartMs + WindupMs + ActiveMs;

        public int RecoveryEndMs(int telegraphStartMs) =>
            telegraphStartMs + WindupMs + ActiveMs + RecoveryMs;

        /// <summary>
        /// Oyuncu etki hacminde mi? Mesafe yarıçap içinde ve açı yayı içinde olmalı.
        /// Tam daire için arcHalfAngleDeg = 180 (veya daha büyük).
        /// Pounce'ta çağıran mesafeyi iniş noktasından verir (boss gövdesinden değil).
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
