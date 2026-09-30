using Dovus.Core.Combat;
using Dovus.Core.Tuning;
using System;
using UnityEngine;

namespace Dovus.Game
{
    /// <summary>
    /// Oyuncu canı ve ölüm. Spec §11 hasar 22 verir, oyuncu tavanı yok —
    /// bir çakma = ölüm ki respawn döngüsü denenebilsin. Süre gerçek saat.
    /// </summary>
    public sealed class PlayerVitals : MonoBehaviour
    {
        BossTuning _boss;
        int _hp;
        float _respawnAtUnscaled = -1f;
        Vector3 _spawnPos;
        bool _captured;
        ActorVisual _visual;
        Func<bool> _reviveBlocked;

        public int Hp => _hp;
        public int MaxHp { get; private set; }
        public bool IsDown => _respawnAtUnscaled >= 0f;
        /// <summary>Play Sweep: can düşer, ölüm ve doğuş ışınlaması olmaz.</summary>
        public bool SuppressDown { get; set; }
        public bool DevHpEnabled { get; private set; }
        int _authoredMaxHp;
        public Vector3 SpawnPos => _spawnPos;

        /// <summary>Dönüşe kalan gerçek saniye (HUD okur); ayakta ise 0.</summary>
        public float RespawnInSec => IsDown ? Mathf.Max(0f, _respawnAtUnscaled - Time.unscaledTime) : 0f;

        /// <summary>
        /// Bağlama 11: reality revive_block — true iken respawn ertelenir.
        /// </summary>
        public void SetReviveBlockedGate(Func<bool> gate) => _reviveBlocked = gate;

        public void Bind(BossTuning boss, int maxHp, float startRatio = 1f)
        {
            _boss = boss;
            _authoredMaxHp = Mathf.Max(1, maxHp);
            DevHpEnabled = false;
            MaxHp = _authoredMaxHp;
            _hp = Mathf.Clamp(Mathf.RoundToInt(MaxHp * Mathf.Clamp01(startRatio)), 1, MaxHp);
            CaptureSpawn();
        }

        /// <summary>
        /// Dev HP açıkken havuz 1_000_000_000. Kapalıyken Bind'deki normal tavan.
        /// Açılışta can da havuza çekilir; kapanınca normal tavana kırpılır.
        /// </summary>
        public void SetDevHp(bool enabled)
        {
            if (_authoredMaxHp <= 0)
                _authoredMaxHp = MaxHp > 0 && MaxHp != DevPlayerHp.Pool ? MaxHp : Mathf.Max(1, MaxHp);
            DevHpEnabled = enabled;
            MaxHp = DevPlayerHp.Resolve(enabled, _authoredMaxHp);
            if (enabled)
            {
                _hp = MaxHp;
                _respawnAtUnscaled = -1f;
                return;
            }

            _hp = Mathf.Clamp(_hp, 1, MaxHp);
        }

        /// <summary>
        /// T10: panel slider'ı `PlayerMaxHp`'i canlı değiştirebilsin diye — `Bind` tek seferlik
        /// (bir sonraki respawn'a kadar eski tavanda kalırdı). Güncel can, yeni tavana kırpılır
        /// (tavan düşürülürse anında ölüm YOK — kırpma, hasar değil).
        /// </summary>
        public void SetMaxHp(int maxHp)
        {
            _authoredMaxHp = Mathf.Max(1, maxHp);
            if (DevHpEnabled)
                return;
            MaxHp = _authoredMaxHp;
            _hp = Mathf.Min(_hp, MaxHp);
        }

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

            _hp = Mathf.Max(0, _hp - amount);
            if (SuppressDown && _hp <= 0)
                _hp = 1;
            if (_hp > 0)
            {
                _visual?.Trigger(ActorVisual.TriggerHit);
                return false;
            }

            _visual?.Trigger(ActorVisual.TriggerDeath);
            float wait = _boss != null ? _boss.RespawnMaxSec : 2f;
            _respawnAtUnscaled = Time.unscaledTime + wait;
            return true;
        }

        /// <summary>İyileştirme — tavanı aşmaz. Gerçekten eklenen canı döner.</summary>
        public int ApplyHeal(int amount)
        {
            if (IsDown || amount <= 0 || _hp >= MaxHp)
                return 0;
            int before = _hp;
            _hp = Mathf.Min(MaxHp, _hp + amount);
            return _hp - before;
        }

        void Update() => Tick();

        public void Tick()
        {
            if (!IsDown || Time.unscaledTime < _respawnAtUnscaled)
                return;

            if (_reviveBlocked != null && _reviveBlocked())
            {
                // Kapı açık kalana kadar kısa aralıklarla yeniden dene (HUD 0'da takılı kalmasın).
                _respawnAtUnscaled = Time.unscaledTime + 0.25f;
                return;
            }

            if (!_captured)
                CaptureSpawn();

            transform.position = _spawnPos;
            _hp = MaxHp;
            _respawnAtUnscaled = -1f;
            if (_visual == null)
                _visual = GetComponent<ActorVisual>();
            _visual?.ResetToLocomotion();
        }
    }
}
