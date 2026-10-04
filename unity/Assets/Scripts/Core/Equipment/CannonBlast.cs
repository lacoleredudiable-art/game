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
    /// <summary>
    /// Top güllesinin itmesi ve geri tepmesi. Kenar kuralı dodge ile aynı:
    /// <see cref="DodgeEdge.KeepOutside"/> boss gövdesinin içine bırakmaz,
    /// daire arena gövdeyi dışarıda tutar. Hareket kalıbı konumun sahibi
    /// iken bu adım çağrılmaz; kuyruk kalıp bitince uygulanır.
    /// </summary>
    public static class CannonBlast
    {
        public static void Move(
            ref float x,
            ref float z,
            float dirX,
            float dirZ,
            float meters,
            float bossX,
            float bossZ,
            float minSeparation,
            float arenaRadius,
            float bodyRadius)
        {
            if (meters <= 0f)
                return;
            float mag = MathF.Sqrt(dirX * dirX + dirZ * dirZ);
            if (mag < 0.0001f)
                return;
            float inv = 1f / mag;
            dirX *= inv;
            dirZ *= inv;
            x += dirX * meters;
            z += dirZ * meters;
            if (minSeparation > 0f)
                DodgeEdge.KeepOutside(ref x, ref z, bossX, bossZ, minSeparation, dirX, dirZ);
            ClampArena(ref x, ref z, arenaRadius, bodyRadius);
        }

        public static void ClampArena(ref float x, ref float z, float arenaRadius, float bodyRadius)
        {
            float limit = MathF.Max(0f, arenaRadius - bodyRadius);
            float sqr = x * x + z * z;
            float limSqr = limit * limit;
            if (sqr <= limSqr || sqr <= 0.0001f)
                return;
            float scale = limit / MathF.Sqrt(sqr);
            x *= scale;
            z *= scale;
        }
    }

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