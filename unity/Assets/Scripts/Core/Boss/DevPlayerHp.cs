using System;
using Dovus.Core.Status;
using Dovus.Core.Damage;

namespace Dovus.Core.Boss
{
    /// <summary>Prototip Dev HP havuzu. Test paneli aç/kapa; varsayılan açık.</summary>
    public static class DevPlayerHp
    {
        public const int Pool = 1_000_000_000;

        public static int Resolve(bool enabled, int normalMaxHp) =>
            enabled ? Pool : Math.Max(1, normalMaxHp);
    }
}
