using System;

using Dovus.Core.Shared;
namespace Dovus.Core.Equipment
{
    /// <summary>
    /// Küre elinin yanında durur. Uzun basış en çok 8 m'ye 0,4 sn'de taşır;
    /// çift dokunuş eline döndürür. Taşıma en çok 2 sn'de bir. Sayılar profil JSON'undan.
    /// </summary>
    public sealed class OrbAnchor
    {
        float _x;
        float _z;
        float _fromX;
        float _fromZ;
        float _toX;
        float _toZ;
        double _moveStartMs;
        double _moveEndMs;
        double _nextMoveMs;
        bool _moving;
        bool _atHand = true;

        public float X => _x;
        public float Z => _z;
        public bool AtHand => _atHand && !_moving;
        public bool IsMoving => _moving;

        public void SnapToHand(float handX, float handZ)
        {
            _x = handX;
            _z = handZ;
            _atHand = true;
            _moving = false;
        }

        public bool TryPlace(float handX, float handZ, float targetX, float targetZ, double nowMs, WeaponCombatProfile profile)
        {
            if (profile == null || nowMs < _nextMoveMs)
                return false;
            float max = profile.OrbPlaceM > 0f ? profile.OrbPlaceM : OrbAnchorDefaults.DefaultOrbPlaceM;
            float dx = targetX - handX;
            float dz = targetZ - handZ;
            float dist = (float)Math.Sqrt(dx * dx + dz * dz);
            if (dist > max && dist > 0.0001f)
            {
                float scale = max / dist;
                targetX = handX + dx * scale;
                targetZ = handZ + dz * scale;
            }
            _fromX = _atHand ? handX : _x;
            _fromZ = _atHand ? handZ : _z;
            _toX = targetX;
            _toZ = targetZ;
            _moveStartMs = nowMs;
            float sec = profile.OrbMoveSec > 0f ? profile.OrbMoveSec : OrbAnchorDefaults.DefaultOrbMoveSec;
            _moveEndMs = nowMs + sec * Units.SecToMs;
            _nextMoveMs = nowMs + (profile.OrbCooldownSec > 0f ? profile.OrbCooldownSec : 2f) * Units.SecToMs;
            _moving = true;
            _atHand = false;
            return true;
        }

        public bool TryRecall(float handX, float handZ, double nowMs, WeaponCombatProfile profile)
        {
            if (profile == null || _atHand || nowMs < _nextMoveMs)
                return false;
            _fromX = _x;
            _fromZ = _z;
            _toX = handX;
            _toZ = handZ;
            _moveStartMs = nowMs;
            float sec = profile.OrbMoveSec > 0f ? profile.OrbMoveSec : OrbAnchorDefaults.DefaultOrbMoveSec;
            _moveEndMs = nowMs + sec * Units.SecToMs;
            _nextMoveMs = nowMs + (profile.OrbCooldownSec > 0f ? profile.OrbCooldownSec : 2f) * Units.SecToMs;
            _moving = true;
            return true;
        }

        public void Tick(double nowMs, float handX, float handZ)
        {
            if (_atHand && !_moving)
            {
                _x = handX;
                _z = handZ;
                return;
            }
            if (!_moving)
                return;
            double span = _moveEndMs - _moveStartMs;
            float t = span <= 0.0 ? 1f : (float)Math.Min(1.0, (nowMs - _moveStartMs) / span);
            _x = _fromX + (_toX - _fromX) * t;
            _z = _fromZ + (_toZ - _fromZ) * t;
            if (t >= 1f)
            {
                _moving = false;
                float dx = _x - handX;
                float dz = _z - handZ;
                _atHand = dx * dx + dz * dz < OrbAnchorDefaults.AtHandDistSqrMax;
            }
        }
    }
}
