namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Silah değiştirme kalkanı: kısa süreli puan.
    /// Dodge yuttuktan sonra hasar borusunun son azaltma/kalkan aşamasına girer.
    /// </summary>
    public sealed class WeaponShortShield
    {
        float _points;
        double _expiresMs;

        public float Points => _points;

        public bool Active(double nowMs) => _points > EquipmentDefaults.MinDistM && nowMs < _expiresMs;

        public void Grant(float points, double nowMs, float durationSec)
        {
            _points = points > 0f ? points : 0f;
            _expiresMs = nowMs + (durationSec > 0f ? durationSec : 0f) * EquipmentDefaults.SecToMs;
            if (_points <= 0f)
                _expiresMs = nowMs;
        }

        public float Absorb(float incoming, double nowMs)
        {
            if (incoming <= 0f || !Active(nowMs))
                return incoming;
            float taken = Consume(incoming, nowMs);
            return incoming - taken;
        }

        /// <summary>
        /// Boru aşama (f) kalkanı zaten hasardan düştü.
        /// Burada yalnız havuz puanı iner; hasar ikinci kez kesilmez.
        /// </summary>
        public float Consume(float points, double nowMs)
        {
            if (points <= 0f || !Active(nowMs))
                return 0f;
            float taken = points < _points ? points : _points;
            _points -= taken;
            if (_points <= EquipmentDefaults.MinDistM)
                _points = 0f;
            return taken;
        }
    }
}
