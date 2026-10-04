using System;

namespace Dovus.Core.Boss
{
    /// <summary>Mermi yok olma / değişme sebebi (log, tarama sayaçları, ileride RPC).</summary>
    public enum ProjectileEventKind : byte
    {
        Spawned,
        Expired,
        HitFriendly,
        HitHostile,
        Erased,
        Absorbed,
        LinkErased,
        Reflected,
        Shrouded,
        Cleared
    }
}
