namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Silah değiştirme kalkanı: kısa süreli puan. Gelen hasar kapısında,
    /// dodge yuttuktan sonra tüketilir. Hasar formülünün içinde değildir;
    /// hasar borusu bunu kendi azaltma adımına sonra taşıyabilir.
    /// </summary>
    public sealed class WeaponShortShield
    {
        float _points;
        double _expiresMs;

        public float Points => _points;

        public bool Active(double nowMs) => _points > 0.01f && nowMs < _expiresMs;

        public void Grant(float points, double nowMs, float durationSec)
        {
            _points = points > 0f ? points : 0f;
            _expiresMs = nowMs + (durationSec > 0f ? durationSec : 0f) * 1000.0;
            if (_points <= 0f)
                _expiresMs = nowMs;
        }

        public float Absorb(float incoming, double nowMs)
        {
            if (incoming <= 0f || !Active(nowMs))
                return incoming;
            float taken = incoming < _points ? incoming : _points;
            _points -= taken;
            if (_points <= 0.01f)
                _points = 0f;
            return incoming - taken;
        }
    }

    /// <summary>Dodge kapısı önce. Kalkan yalnız dodge yutmadıysa puan yer.</summary>
    public static class WeaponShortShieldGate
    {
        public static float Apply(bool dodgeBlocks, WeaponShortShield shield, float incoming, double nowMs)
        {
            if (dodgeBlocks)
                return 0f;
            if (incoming <= 0f || shield == null)
                return incoming;
            return shield.Absorb(incoming, nowMs);
        }
    }
}
