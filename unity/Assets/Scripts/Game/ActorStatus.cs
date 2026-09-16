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
            if (Mathf.Abs(payload) < 0.001f)
                return;

            if (payload > 0f)
                ApplyDamage(payload);
            else
                ApplyHeal(-payload);
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
