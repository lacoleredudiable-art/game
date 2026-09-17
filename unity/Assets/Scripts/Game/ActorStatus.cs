using Dovus.Core.Combat;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>StatusBoard taşıyıcısı — oyuncu veya boss.</summary>
    public sealed class ActorStatus : MonoBehaviour
    {
        public StatusBoard Board { get; } = new StatusBoard();

        /// <summary>
        /// 16 Eylül: ulti (active_modes) çarpanları StatusBoard'a KARIŞTIRILMADI — StatusKind
        /// enum'u element-sistemi.json "mechanics" id'leriyle birebir (durum etkileşim tablosu
        /// bunlara göre kurulu); ulti tamamen ayrı bir eksen. Yalnızca oyuncunun ActorStatus'una
        /// bağlanır (bkz. ManifestationDirector.Bind), boss'unki hep null kalır.
        /// </summary>
        public ActiveModeDirector ModeDirector { get; set; }

        /// <summary>KinematicMotor bunu okur — StatusBoard × aktif ulti modu.</summary>
        public float EffectiveMoveSpeedMult => Board.MoveSpeedMult * (ModeDirector?.MoveSpeedMult ?? 1f);

        public bool EffectiveBlocksMovement => Board.BlocksMovement || (ModeDirector?.BlocksMovement ?? false);

        StatusTuning _tuning = new();
        GameClock _clock;
        BossVitals _bossVitals;
        PlayerVitals _playerVitals;
        BossReactor _reactor;
        bool _stealthVisual;
        Renderer[] _renderers;

        public void Bind(
            GameClock clock,
            StatusTuning tuning,
            PlayerVitals playerVitals = null,
            BossVitals bossVitals = null,
            BossReactor reactor = null)
        {
            _clock = clock;
            _tuning = tuning ?? new StatusTuning();
            _playerVitals = playerVitals;
            _bossVitals = bossVitals;
            _reactor = reactor;
            _renderers = GetComponentsInChildren<Renderer>(true);
        }

        public StatusTuning Tuning
        {
            get => _tuning;
            set => _tuning = value ?? new StatusTuning();
        }

        void Update()
        {
            if (_clock == null)
                return;

            float payload = Board.Tick(_clock.WorldDeltaMs, _tuning);
            SyncStealthVisual();
            if (Mathf.Abs(payload) < 0.001f)
                return;

            if (payload > 0f)
                ApplyDamage(payload);
            else
                ApplyHeal(-payload);
        }

        /// <summary>Gizlilik: camgöbeği yarı saydam (oyuncu efekt rengi kuralı).</summary>
        void SyncStealthVisual()
        {
            bool stealth = Board.IsStealthed;
            if (stealth == _stealthVisual)
                return;
            _stealthVisual = stealth;
            if (_renderers == null || _renderers.Length == 0)
                _renderers = GetComponentsInChildren<Renderer>(true);

            float a = stealth ? 0.35f : 1f;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer r = _renderers[i];
                if (r == null) continue;
                Material mat = r.material;
                if (mat == null)
                    continue;
                bool hasBase = mat.HasProperty("_BaseColor");
                bool hasColor = mat.HasProperty("_Color");
                if (!hasBase && !hasColor)
                    continue;
                if (hasBase)
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = a;
                    mat.SetColor("_BaseColor", c);
                }
                else
                {
                    Color c = mat.color;
                    c.a = a;
                    mat.color = c;
                }
            }
        }

        public void ApplyDamage(float raw)
        {
            if (raw <= 0f) return;
            float modeMult = ModeDirector?.DamageTakenMult ?? 1f;
            float afterShield = Board.AbsorbDamage(raw * Board.IncomingDamageMult * modeMult);
            if (afterShield <= 0f) return;

            if (_bossVitals != null)
                _bossVitals.ApplyDamage(afterShield);
            else if (_playerVitals != null)
                _playerVitals.ApplyDamage(Mathf.CeilToInt(afterShield));
        }

        public void ApplyHeal(float amount)
        {
            if (amount <= 0f)
                return;
            // 16 Eylül: "Kavurucu Yara" — yanık hedefte pasif regen tick'i de azalır.
            if (_playerVitals != null)
            {
                int healed = _playerVitals.ApplyHeal(Mathf.CeilToInt(amount * Board.HealEffectivenessMult));
                if (healed > 0)
                    ModeDirector?.NotifyHealed(); // "healer iyileştirirse biter" (Kan Çılgınlığı)
            }
        }

        public void ApplyKnockbackFrom(Vector3 fromWorld)
        {
            if (_reactor == null)
                return;
            _reactor.React(
                fromWorld,
                _tuning.KnockbackMeters,
                _tuning.KnockbackLiftM,
                _tuning.KnockbackShakeSec,
                _clock != null ? _clock.Director.WorldTimeMs : 0);
        }
    }
}
