using System;
using System.Collections.Generic;

namespace Dovus.Core.Combat
{
    /// <summary>
    /// Fiil soğuması + global soğuma + eşzamanlı cast limiti —
    /// docs/element-sistemi.json global_rules.cooldown_rules.
    /// verb id → soğuma bitiş zamanı (worldMs). Oyuna bağlı değil.
    /// </summary>
    public sealed class CooldownTracker
    {
        readonly float _globalCooldownSec;
        readonly int _maxConcurrentCasts;
        readonly Dictionary<string, double> _verbReadyAtMs = new(StringComparer.Ordinal);

        double _globalReadyAtMs;
        int _activeCasts;

        /// <summary>Varsayılanlar JSON cooldown_rules: 0.3s GCD, max 1 eşzamanlı cast.</summary>
        public CooldownTracker(float globalCooldownSec = 0.3f, int maxConcurrentCasts = 1)
        {
            _globalCooldownSec = Math.Max(0f, globalCooldownSec);
            _maxConcurrentCasts = Math.Max(1, maxConcurrentCasts);
        }

        public float GlobalCooldownSec => _globalCooldownSec;
        public int MaxConcurrentCasts => _maxConcurrentCasts;
        public int ActiveCasts => _activeCasts;

        public float GlobalRemainingSec(double worldMs) =>
            Math.Max(0f, (float)((_globalReadyAtMs - worldMs) / 1000.0));

        public float VerbRemainingSec(string verbId, double worldMs)
        {
            if (string.IsNullOrEmpty(verbId) || !_verbReadyAtMs.TryGetValue(verbId, out double readyAt))
                return 0f;
            return Math.Max(0f, (float)((readyAt - worldMs) / 1000.0));
        }

        public bool CanStart(string verbId, double worldMs)
        {
            if (string.IsNullOrEmpty(verbId))
                return false;
            if (_activeCasts >= _maxConcurrentCasts)
                return false;
            if (worldMs < _globalReadyAtMs)
                return false;
            if (_verbReadyAtMs.TryGetValue(verbId, out double readyAt) && worldMs < readyAt)
                return false;
            return true;
        }

        /// <summary>
        /// Cast başlatır: GCD + fiil soğuması yazar, eşzamanlı sayacı artırır.
        /// Reddedilirse false (durum değişmez).
        /// </summary>
        public bool TryStart(string verbId, float verbCooldownSec, double worldMs)
        {
            if (!CanStart(verbId, worldMs))
                return false;

            _activeCasts++;
            // float*1000 (ör. 0.3f) kayan nokta sapması üretmesin diye ms yuvarlanır
            _globalReadyAtMs = worldMs + Math.Round(_globalCooldownSec * 1000.0);
            _verbReadyAtMs[verbId] = worldMs + Math.Round(Math.Max(0f, verbCooldownSec) * 1000.0);
            return true;
        }

        /// <summary>Cast bittiğinde eşzamanlı yuvayı boşaltır (max_concurrent_casts).</summary>
        public void CompleteCast()
        {
            if (_activeCasts > 0)
                _activeCasts--;
        }
    }
}
