using System;

namespace Dovus.Core.Shared
{
    /// <summary>
    /// Mana havuzu — docs/element-sistemi.json global_rules.resource_system.
    /// Consume sonrası regen_delay_after_cast_sec kadar yenilenme durur, sonra regen_per_sec.
    /// Oyuna bağlı değil (PlayerVitalsHost / ManifestationDirector ayrı karar).
    /// </summary>
    public sealed class ResourceTracker
    {
        readonly float _maxMana;
        readonly float _regenPerSec;
        readonly float _regenDelayAfterCastSec;

        float _mana;
        float _regenDelayLeftSec;

        /// <summary>Varsayılanlar JSON resource_system: 100 / 8 / 1.5.</summary>
        public ResourceTracker(
            float maxMana = 100f,
            float regenPerSec = SharedDefaults.RegenPerSec,
            float regenDelayAfterCastSec = SharedDefaults.RegenDelayAfterCastSec)
        {
            _maxMana = Math.Max(0f, maxMana);
            _regenPerSec = Math.Max(0f, regenPerSec);
            _regenDelayAfterCastSec = Math.Max(0f, regenDelayAfterCastSec);
            _mana = _maxMana;
        }

        public float Mana => _mana;
        public float MaxMana => _maxMana;
        public float RegenPerSec => _regenPerSec;
        public float RegenDelayAfterCastSec => _regenDelayAfterCastSec;
        public float RegenDelayLeftSec => _regenDelayLeftSec;

        public bool CanAfford(float cost) => cost <= 0f || _mana >= cost;

        /// <summary>
        /// Cast'i engellemeden maliyeti uygular (Bağlama 2). Yetmezse 0'a kilitler;
        /// cost≤0 ise no-op. Yenilenme gecikmesi cost pozitifken başlar.
        /// </summary>
        public void Consume(float cost)
        {
            if (cost <= 0f)
                return;

            _mana = Math.Max(0f, _mana - cost);
            _regenDelayLeftSec = _regenDelayAfterCastSec;
        }

        /// <summary>Gecikme bitene kadar yenilemez; kalan dt ile mana tavanına kadar dolar.</summary>
        /// <summary>Play Sweep vaka sıfırlaması: tam mana (eski reflection yazımıyla aynı: yalnız _mana; regen gecikmesine dokunmaz).</summary>
        public void RefillToMax()
        {
            _mana = _maxMana;
        }

        public void Tick(float dtSec)
        {
            if (dtSec <= 0f || _mana >= _maxMana)
            {
                if (dtSec > 0f && _regenDelayLeftSec > 0f)
                    _regenDelayLeftSec = Math.Max(0f, _regenDelayLeftSec - dtSec);
                return;
            }

            if (_regenDelayLeftSec > 0f)
            {
                if (dtSec <= _regenDelayLeftSec)
                {
                    _regenDelayLeftSec -= dtSec;
                    return;
                }

                dtSec -= _regenDelayLeftSec;
                _regenDelayLeftSec = 0f;
            }

            _mana = Math.Min(_maxMana, _mana + _regenPerSec * dtSec);
        }
    }
}
