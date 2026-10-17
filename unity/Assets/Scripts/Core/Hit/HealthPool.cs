using System;

namespace Dovus.Core.Hit
{
    /// <summary>
    /// Genel can havuzu (müttefik, minyon, kukla). Boss <c>BossVitals</c>, oyuncu <c>PlayerHealth</c> kendi
    /// havuzunu korur; sözleşme tarafına adaptörle bağlanır. Zaman tutmaz.
    /// </summary>
    public sealed class HealthPool
    {
        float _hp;
        float _maxHp;

        public HealthPool(float maxHp)
        {
            _maxHp = Math.Max(1f, maxHp);
            _hp = _maxHp;
        }

        public float Hp => _hp;
        public float MaxHp => _maxHp;
        public bool IsDead => _hp <= 0f;
        public float Ratio => _hp / _maxHp;

        /// <summary>Her gerçek can kaybında (önceki, sonraki).</summary>
        public event Action<float, float>? Damaged;

        /// <summary>Her gerçek iyileşmede (önceki, sonraki).</summary>
        public event Action<float, float>? Healed;

        /// <summary>Can 0'a indiği an, ölüm başına bir kez.</summary>
        public event Action? Died;

        public event Action? Revived;

        /// <summary>Gerçekte düşen miktarı döner. Ölüyse ya da amount≤0 ise 0.</summary>
        public float ApplyDamage(float amount)
        {
            if (IsDead || amount <= 0f)
                return 0f;

            float before = _hp;
            _hp = Math.Max(0f, _hp - amount);
            Damaged?.Invoke(before, _hp);
            if (_hp <= 0f)
                Died?.Invoke();
            return before - _hp;
        }

        /// <summary>Tavanı aşmaz, ölüyü diriltmez. Gerçekte eklenen miktarı döner.</summary>
        public float ApplyHeal(float amount)
        {
            if (IsDead || amount <= 0f || _hp >= _maxHp)
                return 0f;

            float before = _hp;
            _hp = Math.Min(_maxHp, _hp + amount);
            Healed?.Invoke(before, _hp);
            return _hp - before;
        }

        /// <summary>Tavan değişince güncel can kırpılır; kırpma hasar sayılmaz, ölüm yapmaz.</summary>
        public void SetMaxHp(float maxHp)
        {
            _maxHp = Math.Max(1f, maxHp);
            if (!IsDead)
                _hp = Math.Min(_hp, _maxHp);
        }

        public void Revive()
        {
            _hp = _maxHp;
            Revived?.Invoke();
        }
    }
}
