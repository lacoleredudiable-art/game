using System;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Casting;
using Dovus.Core.Status;
using Dovus.Core.Tuning;

namespace Dovus.Core.Dodge
{
    /// <summary>
    /// Dodge hakları. Dolu haklar hemen harcanır; eksik hak teker teker dolar
    /// (HUD'da bir pip dolarken diğeri dolu ya da boş kalır).
    /// </summary>
    public sealed class DodgeChargeBank
    {
        readonly DodgeTuning _tuning;
        int _ready;
        int _accruedMs;
        int _lastMs = int.MinValue;

        public DodgeChargeBank(DodgeTuning? tuning = null, int? startReady = null)
        {
            _tuning = tuning ?? new DodgeTuning();
            int max = Max;
            _ready = startReady.HasValue ? Math.Clamp(startReady.Value, 0, max) : max;
        }

        /// <summary>Ulti/pasif dash_cooldown_mult. 0 = haklar anında dolar.</summary>
        public float RechargeMult { get; set; } = 1f;

        public int Max => Math.Max(1, _tuning.MaxCharges);

        public int Ready => _ready;

        /// <summary>Sıradaki boş hakın doluluk oranı. Depo doluysa 1.</summary>
        public float Fill01
        {
            get
            {
                if (_ready >= Max)
                    return 1f;
                int need = NeedMs();
                if (need <= 0)
                    return 1f;
                return Math.Clamp(_accruedMs / (float)need, 0f, 1f);
            }
        }

        public void Tick(int worldMs)
        {
            int max = Max;
            if (_ready > max)
                _ready = max;

            if (_lastMs == int.MinValue)
            {
                _lastMs = worldMs;
                if (_ready >= max)
                    _accruedMs = 0;
                return;
            }

            int dt = worldMs - _lastMs;
            _lastMs = worldMs;
            if (dt < 0)
                dt = 0;

            int need = NeedMs();
            if (need <= 0)
            {
                _ready = max;
                _accruedMs = 0;
                return;
            }

            if (_ready >= max || dt == 0)
            {
                if (_ready >= max)
                    _accruedMs = 0;
                return;
            }

            _accruedMs += dt;
            while (_accruedMs >= need && _ready < max)
            {
                _accruedMs -= need;
                _ready++;
            }

            if (_ready >= max)
                _accruedMs = 0;
        }

        public bool TrySpend(int worldMs)
        {
            Tick(worldMs);
            if (_ready <= 0)
                return false;
            _ready--;
            return true;
        }

        /// <summary>Tam hak (1) ya da kesir (0.5 = dolmakta olan hakka yarım süre).</summary>
        public void Refund(float charges)
        {
            if (charges <= 0f)
                return;

            int max = Max;
            int whole = (int)MathF.Floor(charges);
            float frac = charges - whole;
            if (whole > 0)
                _ready = Math.Min(max, _ready + whole);

            if (frac > 0.0001f && _ready < max)
            {
                int need = NeedMs();
                if (need <= 0)
                {
                    _ready = max;
                    _accruedMs = 0;
                    return;
                }

                _accruedMs += (int)MathF.Round(frac * need);
                while (_accruedMs >= need && _ready < max)
                {
                    _accruedMs -= need;
                    _ready++;
                }
            }

            if (_ready >= max)
                _accruedMs = 0;
        }

        int NeedMs()
        {
            float mult = RechargeMult;
            if (mult < 0f)
                mult = 0f;
            if (mult == 0f)
                return 0;
            return Math.Max(1, (int)Math.Round(_tuning.ChargeRechargeMs * mult));
        }
    }
}
