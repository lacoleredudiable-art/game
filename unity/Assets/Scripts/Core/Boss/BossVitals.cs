using System;

namespace Dovus.Core.Boss
{
    /// <summary>
    /// Boss canı — saf C# (PlayerVitalsHost'in Unity'li deseninin Core karşılığı).
    /// Can 0 → IsDown; yeniden doğuş <see cref="Revive"/> ile dışarıdan tetiklenir.
    /// Zaman parametre olarak geçer; bu sınıf zamanı tutmaz. Spec §11.
    /// K1: her hasar yolu (kapanış, DoT, yansıma, minyon, emme, yönlendirme, takım) buradan geçer;
    /// ölüm ve can değişimi TEK yerden <see cref="Died"/> / <see cref="HpChanged"/> olaylarıyla duyurulur.
    /// Ölüm/diriliş akışı ve ileride boss bölüm geçişleri (%65/%30) bu kancaya abone olur.
    /// </summary>
    public sealed class BossVitals
    {
        float _hp;
        float _maxHp;
        bool _isDown;

        /// <summary>Can 0'a indiği an, hangi hasar yolundan gelirse gelsin, ölüm başına bir kez.</summary>
        public event Action Died;

        /// <summary>Her gerçek can kaybında (önceki, sonraki). Bölüm eşikleri <see cref="CrossedBelow"/> ile okur.</summary>
        public event Action<float, float> HpChanged;

        /// <summary><see cref="Revive"/> sonrası (ölüm zamanlayıcısı dışarıdan dirilişi de görsün).</summary>
        public event Action Revived;

        public BossVitals(float maxHp = BossDefaults.DefaultMaxHp)
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

            float before = _hp;
            _hp = Math.Max(0f, _hp - amount);
            bool killed = _hp <= 0f;
            if (killed)
                _isDown = true;
            if (_hp < before)
                HpChanged?.Invoke(before, _hp);
            if (!killed)
                return false;

            Died?.Invoke();
            return true;
        }

        /// <summary>Can oranı bu vuruşta eşiğin altına indi mi (before &gt; eşik ≥ after). Bölüm geçişleri için.</summary>
        public static bool CrossedBelow(float hpBefore, float hpAfter, float maxHp, float ratio)
        {
            if (maxHp <= 0f)
                return false;
            float threshold = maxHp * ratio;
            return hpBefore > threshold && hpAfter <= threshold;
        }

        /// <summary>
        /// Debug: canı <paramref name="ratio"/> × tavana indirir. Hasar yolundan geçer (ApplyDamage),
        /// böylece <see cref="HpChanged"/> ve HUD normal kayıpla aynı görür. Asla öldürmez (en az 1 can
        /// kalır); zaten bu orandan aşağıdaysa, ya da boss yere düşmüşse hiçbir şey yapmaz. Değişim olduysa true.
        /// </summary>
        public bool SkipToHpRatio(float ratio)
        {
            if (_isDown)
                return false;
            float target = Math.Max(1f, _maxHp * Math.Min(1f, Math.Max(0f, ratio)));
            if (_hp <= target)
                return false;
            ApplyDamage(_hp - target);
            return true;
        }

        /// <summary>Tam canla ayağa kalkar (§11: noktalama, bitiş değil).</summary>
        public void Revive()
        {
            _hp = _maxHp;
            _isDown = false;
            Revived?.Invoke();
        }
    }
}
