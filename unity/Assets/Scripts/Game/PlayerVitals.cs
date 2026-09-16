using Dovus.Core.Tuning;
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

        public int Hp => _hp;
        public int MaxHp { get; private set; }
        public bool IsDown => _respawnAtUnscaled >= 0f;
        public Vector3 SpawnPos => _spawnPos;

        /// <summary>Dönüşe kalan gerçek saniye (HUD okur); ayakta ise 0.</summary>
        public float RespawnInSec => IsDown ? Mathf.Max(0f, _respawnAtUnscaled - Time.unscaledTime) : 0f;

        public void Bind(BossTuning boss, int maxHp, float startRatio = 1f)
        {
            _boss = boss;
            MaxHp = Mathf.Max(1, maxHp);
            _hp = Mathf.Clamp(Mathf.RoundToInt(MaxHp * Mathf.Clamp01(startRatio)), 1, MaxHp);
            CaptureSpawn();
        }

        /// <summary>
        /// T10: panel slider'ı `PlayerMaxHp`'i canlı değiştirebilsin diye — `Bind` tek seferlik
        /// (bir sonraki respawn'a kadar eski tavanda kalırdı). Güncel can, yeni tavana kırpılır
        /// (tavan düşürülürse anında ölüm YOK — kırpma, hasar değil).
        /// </summary>
        public void SetMaxHp(int maxHp)
        {
            MaxHp = Mathf.Max(1, maxHp);
            _hp = Mathf.Min(_hp, MaxHp);
        }

        public void CaptureSpawn()
        {
            _spawnPos = transform.position;
            _captured = true;
        }

        public bool ApplyDamage(int amount)
        {
            if (IsDown || amount <= 0)
                return false;

            if (_visual == null)
                _visual = GetComponent<ActorVisual>();

            _hp = Mathf.Max(0, _hp - amount);
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
