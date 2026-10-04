using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;

namespace Dovus.App.Mechanics
{
    /// <summary>MechanicWorldRuntime.RedirectMechanicDamage — hasar paylaşımı / yönlendirme.</summary>
    public static class MechanicDamageRedirectRules
    {
        public static float ApplyShare(float remaining, float ratio, out float redirected)
        {
            return BossStatusMath.SplitShare(remaining, ratio, out redirected);
        }
    }
}
