using Dovus.Core.Combat;

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
