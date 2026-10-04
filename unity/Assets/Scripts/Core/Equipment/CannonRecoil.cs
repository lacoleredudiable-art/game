using System;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;

namespace Dovus.Core.Equipment
{
    /// <summary>Kalıp bitene kadar bekleyen geri tepme. Kalıp sürerken konum yazılmaz.</summary>
    public sealed class CannonRecoil
    {
        float _dirX;
        float _dirZ;
        float _meters;
        float _bossX;
        float _bossZ;
        float _minSeparation;
        float _arenaRadius;
        float _bodyRadius;

        public bool Pending { get; private set; }

        public void Queue(
            float dirX,
            float dirZ,
            float meters,
            float bossX,
            float bossZ,
            float minSeparation,
            float arenaRadius,
            float bodyRadius)
        {
            _dirX = dirX;
            _dirZ = dirZ;
            _meters = meters;
            _bossX = bossX;
            _bossZ = bossZ;
            _minSeparation = minSeparation;
            _arenaRadius = arenaRadius;
            _bodyRadius = bodyRadius;
            Pending = meters > 0f;
        }

        public bool TryApply(bool templateOwnsPosition, ref float x, ref float z)
        {
            if (!Pending || templateOwnsPosition)
                return false;
            CannonBlast.Move(
                ref x, ref z, _dirX, _dirZ, _meters,
                _bossX, _bossZ, _minSeparation, _arenaRadius, _bodyRadius);
            Pending = false;
            return true;
        }
    }
}
