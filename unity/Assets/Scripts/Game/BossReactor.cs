using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Boss fiziksel tepkisi — geri tepme, sarsılma, kaldırma. Can/hasar T8.
    /// </summary>
    public sealed class BossReactor : MonoBehaviour
    {
        [SerializeField] PrototypeTuning _tuning = new();

        // Bootstrap'ta wiring yok (T7.2 bu dosyaya dokunamıyor) — varsayılan Bootstrap'ın
        // boss yarıçapıyla (0.85 m) eşleşiyor. T10/T8 gerçek örneği bağlarsa burası da güncellenmeli.
        [SerializeField] float _bodyRadiusM = 0.85f;

        Vector3 _home;

        // Geri tepme _home'a KALICI işlenir (aşağıda React). Bu alan sadece görsel yumuşatma:
        // home aniden kaydığında gövde ışınlanmasın diye kalan farkı taşır ve zamanla
        // (BossRecoilEaseDecayPerSec) sıfıra söner — asla eski _home'a geri dönmez.
        Vector3 _visualOffset;

        float _shakeUntil;
        float _shakeAmp;
        float _liftVel;
        bool _captured;

        // §11 ölüm: kısa çökme pozu (squash). Yavaş çekim TimeDirector'da; burada yalnızca silüet.
        Vector3 _baseScale = Vector3.one;
        bool _collapsed;
        float _collapseUntilWorldMs;

        public PrototypeTuning Tuning
        {
            get
            {
                _tuning ??= new PrototypeTuning();
                return _tuning;
            }
            set => _tuning = value;
        }

        public float BodyRadiusM
        {
            get => _bodyRadiusM;
            set => _bodyRadiusM = value;
        }

        public bool IsCollapsed => _collapsed;

        /// <summary>
        /// Kalıcı konum. Dışarıdan yazılabilir ki T8'in yaklaşma hareketi bunun üstüne binsin
        /// (pozisyon sahipliği çakışmasın) — arena sınırına kırpılır.
        /// </summary>
        public Vector3 Home
        {
            get => _home;
            set
            {
                _home = ClampToArena(value);
                _captured = true;
            }
        }

        public void CaptureHome()
        {
            _home = ClampToArena(transform.position);
            _captured = true;
            _baseScale = transform.localScale;
            if (_baseScale.sqrMagnitude < 1e-6f)
                _baseScale = Vector3.one;
        }

        public void React(
            Vector3 fromWorld,
            float knockbackM,
            float liftM,
            float shakeSec,
            double worldTimeMs)
        {
            if (_collapsed)
                return;

            if (!_captured)
                CaptureHome();

            Vector3 flat = _home - fromWorld;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-4f)
                flat = -transform.forward;
            flat.Normalize();

            Vector3 oldHome = _home;
            Vector3 newHome = ClampToArena(_home + flat * knockbackM);
            _home = newHome;
            // Home kalıcı kaydı; anında ışınlanmasın diye farkı görsel ofsete taşı, o zamanla
            // sıfıra sönsün (§8: "düşman tepki vermeli" — itildiği yerde kalmalı, geri kaymamalı).
            _visualOffset += oldHome - newHome;

            _liftVel = Mathf.Max(_liftVel, liftM * Tuning.BossLiftVelocityPerM);
            _shakeAmp = Mathf.Max(_shakeAmp, Tuning.BossShakeAmpBaseM + knockbackM * Tuning.BossShakeAmpPerKnockbackM);
            _shakeUntil = (float)worldTimeMs + shakeSec * 1000f;
        }

        /// <summary>Kabuk kapanışı: yerinde sabitle (kısa kilit).</summary>
        public void Pin(float durationSec, double worldTimeMs)
        {
            if (_collapsed)
                return;

            _visualOffset = Vector3.zero;
            _liftVel = 0f;
            _shakeAmp = Tuning.BossPinShakeAmpM;
            _shakeUntil = (float)worldTimeMs + durationSec * 1000f;
        }

        /// <summary>
        /// §11 ölüm pozu: yerinde çöker (squash). Süre dünya saati — yavaş çekimle birlikte uzar.
        /// </summary>
        public void BeginCollapse(float durationSec, double worldTimeMs)
        {
            if (!_captured)
                CaptureHome();

            _collapsed = true;
            _liftVel = 0f;
            _shakeAmp = 0f;
            _shakeUntil = 0f;
            _visualOffset = Vector3.zero;
            _collapseUntilWorldMs = (float)(worldTimeMs + Mathf.Max(0.05f, durationSec) * 1000.0);
            ApplyCollapseScale(1f);
        }

        public void EndCollapse()
        {
            _collapsed = false;
            _collapseUntilWorldMs = 0f;
            transform.localScale = _baseScale;
            Vector3 p = _home;
            p.y = _home.y;
            transform.position = p;
        }

        public void Tick(float dtSec, double worldTimeMs)
        {
            if (!_captured)
                CaptureHome();

            float now = (float)worldTimeMs;

            if (_collapsed)
            {
                float remain = Mathf.Max(0f, _collapseUntilWorldMs - now);
                float total = Mathf.Max(0.05f, Tuning.BossDeathCollapseSec) * 1000f;
                float u = 1f - Mathf.Clamp01(remain / total);
                ApplyCollapseScale(u);
                Vector3 flat = _home;
                flat.y = _home.y;
                transform.position = flat;
                return;
            }

            Vector3 shake = Vector3.zero;
            if (now < _shakeUntil)
            {
                float t = (_shakeUntil - now) / 1000f;
                shake = new Vector3(
                    (Mathf.PerlinNoise(now * 0.05f, 0.1f) - 0.5f) * 2f,
                    0f,
                    (Mathf.PerlinNoise(0.3f, now * 0.05f) - 0.5f) * 2f) * (_shakeAmp * t);
            }
            else
            {
                _shakeAmp = 0f;
            }

            // Kaldırma + yerçekimi
            float y = transform.position.y;
            y += _liftVel * dtSec;
            _liftVel -= Tuning.BossGravityMps2 * dtSec;
            if (y <= _home.y)
            {
                y = _home.y;
                _liftVel = 0f;
            }

            _visualOffset = Vector3.Lerp(
                _visualOffset,
                Vector3.zero,
                1f - Mathf.Exp(-Tuning.BossRecoilEaseDecayPerSec * dtSec));
            Vector3 p = _home + _visualOffset + shake;
            p.y = y;
            transform.position = p;
        }

        void ApplyCollapseScale(float progress01)
        {
            float squash = Mathf.Lerp(1f, Tuning.BossDeathSquashY, Mathf.Clamp01(progress01));
            float spread = Mathf.Lerp(1f, Tuning.BossDeathSpreadXz, Mathf.Clamp01(progress01));
            transform.localScale = new Vector3(
                _baseScale.x * spread,
                _baseScale.y * squash,
                _baseScale.z * spread);
        }

        // T5 dersi: arena dışına sonsuza kayan gövde. KinematicMotor'daki aynı desen.
        Vector3 ClampToArena(Vector3 pos)
        {
            float limit = Mathf.Max(0f, Tuning.ArenaHalfSizeM - _bodyRadiusM);
            pos.x = Mathf.Clamp(pos.x, -limit, limit);
            pos.z = Mathf.Clamp(pos.z, -limit, limit);
            return pos;
        }
    }
}
