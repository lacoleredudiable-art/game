using System;
using Dovus.Core.Data;

using Dovus.Core.Shared;
namespace Dovus.Core.Equipment
{
    public enum WeaponSwapResult
    {
        Started,
        Disabled,
        NoSecondWeapon,
        OnCooldown,
        AlreadySwapping,
        StateBlocked
    }
}
