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

        public int Hp => _hp;
        public int MaxHp { get; private set; }
        public bool IsDown => _respawnAtUnscaled >= 0f;
        public Vector3 SpawnPos => _spawnPos;

        /// <summary>Dönüşe kalan gerçek saniye (HUD okur); ayakta ise 0.</summary>
        public float RespawnInSec => IsDown ? Mathf.Max(0f, _respawnAtUnscaled - Time.unscaledTime) : 0f;

        public void Bind(BossTuning boss, int maxHp)
        {
            _boss = boss;
            MaxHp = Mathf.Max(1, maxHp);
            _hp = MaxHp;
            CaptureSpawn();
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

            _hp = Mathf.Max(0, _hp - amount);
            if (_hp > 0)
                return false;

            float wait = _boss != null ? _boss.RespawnMaxSec : 2f;
            _respawnAtUnscaled = Time.unscaledTime + wait;
            return true;
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
        }
    }
}
