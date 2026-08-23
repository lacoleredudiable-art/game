using System;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Boss canı — saf C# (PlayerVitals'in Unity'li deseninin Core karşılığı).
    /// Can 0 → IsDown; yeniden doğuş <see cref="Revive"/> ile dışarıdan tetiklenir.
    /// Zaman parametre olarak geçer; bu sınıf zamanı tutmaz. Spec §11.
    /// </summary>
    public sealed class BossVitals
    {
        float _hp;
        float _maxHp;
        bool _isDown;

        public BossVitals(float maxHp = 120f)
        {
            SetMaxHp(maxHp);
            _hp = _maxHp;
        }

        public float Hp => _hp;
        public float MaxHp => _maxHp;
        public bool IsDown => _isDown;

        public void SetMaxHp(float maxHp)
        {
            _maxHp = Math.Max(1f, maxHp);
            if (!_isDown)
                _hp = Math.Min(_hp, _maxHp);
        }

        /// <summary>
        /// Hasar uygular. Can 0'a düştüyse true (ölüm anı). Down iken veya amount≤0 ise false.
        /// </summary>
        public bool ApplyDamage(float amount)
        {
            if (_isDown || amount <= 0f)
                return false;

            _hp = Math.Max(0f, _hp - amount);
            if (_hp > 0f)
                return false;

            _isDown = true;
            return true;
        }

        /// <summary>Tam canla ayağa kalkar (§11: noktalama, bitiş değil).</summary>
        public void Revive()
        {
            _hp = _maxHp;
            _isDown = false;
        }
    }
}
