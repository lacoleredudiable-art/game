using Dovus.App.Actors;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Tuning;
using Dovus.Game.Composition;
using Dovus.Game.Platform;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Actors
{
    /// <summary>
    /// Oyuncu canÄ± ve Ã¶lÃ¼m. Spec Â§11 hasar 22 verir, oyuncu tavanÄ± yok â€”
    /// bir Ã§akma = Ã¶lÃ¼m ki respawn dÃ¶ngÃ¼sÃ¼ denenebilsin.
    /// O4: dÃ¶nÃ¼ÅŸ sÃ¼resi dÃ¼nya saatiyle iÅŸler (duraklatmada durur; saat baÄŸlÄ± deÄŸilse gerÃ§ek saat);
    /// dÃ¼ÅŸÃ¼ÅŸte ve dÃ¶nÃ¼ÅŸte oyuncu durum panosu temizlenir (yanma/zehir/sersem/kÃ¶k taÅŸÄ±nmaz).
    /// </summary>
    public sealed class PlayerVitals : MonoBehaviour
    {
        readonly PlayerHealth _health = new PlayerHealth();
        BossTuning _boss;
        GameClock _clock;
        Vector3 _spawnPos;
        bool _captured;
        ActorVisual _visual;

        public PlayerHealth Health => _health;
        public int Hp => _health.Hp;
        public int MaxHp => _health.MaxHp;
        public bool IsDown => _health.IsDown;
        /// <summary>Play Sweep: can dÃ¼ÅŸer, Ã¶lÃ¼m ve doÄŸuÅŸ Ä±ÅŸÄ±nlamasÄ± olmaz.</summary>
        public bool SuppressDown
        {
            get => _health.SuppressDown;
            set => _health.SuppressDown = value;
        }
        public bool DevHpEnabled => _health.DevHpEnabled;
        public Vector3 SpawnPos => _spawnPos;

        /// <summary>DÃ¶nÃ¼ÅŸe kalan gerÃ§ek saniye (HUD okur); ayakta ise 0.</summary>
        public float RespawnInSec => _health.RespawnInSec(NowSec());

        /// <summary>O4: dÃ¶nÃ¼ÅŸ zamanlayÄ±cÄ±sÄ± iÃ§in dÃ¼nya saati.</summary>
        public void BindClock(GameClock clock) => _clock = clock;

        float NowSec() => _clock != null
            ? (float)(_clock.World.NowMs / 1000.0)
            : (float)(UnityUnscaledClock.Default.NowMs / 1000.0);

        void ClearStatusBoard() => GetComponent<ActorStatus>()?.Board.Clear();

        public void Bind(BossTuning boss, int maxHp, float startRatio = 1f)
        {
            _boss = boss;
            _health.Bind(maxHp, startRatio);
            CaptureSpawn();
        }

        /// <summary>
        /// Dev HP aÃ§Ä±kken havuz 1_000_000_000. KapalÄ±yken Bind'deki normal tavan.
        /// AÃ§Ä±lÄ±ÅŸta can da havuza Ã§ekilir; kapanÄ±nca normal tavana kÄ±rpÄ±lÄ±r.
        /// </summary>
        public void SetDevHp(bool enabled) => _health.SetDevHp(enabled);

        /// <summary>
        /// T10: panel slider'Ä± `PlayerMaxHp`'i canlÄ± deÄŸiÅŸtirebilsin diye â€” `Bind` tek seferlik
        /// (bir sonraki respawn'a kadar eski tavanda kalÄ±rdÄ±). GÃ¼ncel can, yeni tavana kÄ±rpÄ±lÄ±r
        /// (tavan dÃ¼ÅŸÃ¼rÃ¼lÃ¼rse anÄ±nda Ã¶lÃ¼m YOK â€” kÄ±rpma, hasar deÄŸil).
        /// </summary>
        public void SetMaxHp(int maxHp) => _health.SetMaxHp(maxHp);

        /// <summary>Play sweep vakasÄ±: yarÄ± can, diriliÅŸ zamanlayÄ±cÄ±sÄ± sÄ±fÄ±r (eski private alan yansÄ±masÄ±).</summary>
        public void ResetForSweepCase() => _health.ResetForSweepCase();

        public void CaptureSpawn()
        {
            _spawnPos = transform.position;
            _captured = true;
        }

        public bool ApplyDamage(int amount, bool dodgeable = true, bool shortShieldAlreadyApplied = false)
        {
            if (IsDown || amount <= 0)
                return false;
            if (PlayerDodgeRig.BlocksIncoming(this, dodgeable))
                return false;
            if (!shortShieldAlreadyApplied)
            {
                WeaponShortShieldHost host = GetComponent<WeaponShortShieldHost>();
                double now = host != null ? host.NowMs : 0;
                float pool = host != null && host.Shield.Active(now) ? host.Shield.Points : 0f;
                if (pool > 0f)
                {
                    DamageOutcome soaked = DamagePipeline.Resolve(new DamageQuery
                    {
                        SkillPower = amount,
                        Shield = pool,
                        CanCrit = false,
                        ScaleMagnitudes = false
                    });
                    if (soaked.ShieldAbsorbed > 0f)
                        host.Shield.Consume(soaked.ShieldAbsorbed, now);
                    amount = Mathf.CeilToInt(soaked.Amount);
                    if (amount <= 0)
                        return false;
                }
            }

            if (_visual == null)
                _visual = GetComponent<ActorVisual>();

            float wait = _boss != null ? _boss.RespawnMaxSec : 2f;
            HpLossResult loss = _health.ApplyHpLoss(amount, NowSec(), wait);
            if (loss.Kind == HpLossKind.Hit)
            {
                _visual?.Trigger(ActorVisual.TriggerHit);
                return false;
            }

            _visual?.Trigger(ActorVisual.TriggerDeath);
            ClearStatusBoard();
            return true;
        }

        /// <summary>Ä°yileÅŸtirme â€” tavanÄ± aÅŸmaz. GerÃ§ekten eklenen canÄ± dÃ¶ner.</summary>
        public int ApplyHeal(int amount) => _health.ApplyHeal(amount);

        void Update() => Tick();

        public void Tick()
        {
            if (!_health.ReviveDue(NowSec()))
                return;

            if (!_captured)
                CaptureSpawn();

            transform.position = _spawnPos;
            _health.Revive();
            ClearStatusBoard();
            if (_visual == null)
                _visual = GetComponent<ActorVisual>();
            _visual?.ResetToLocomotion();
        }
    }
}
