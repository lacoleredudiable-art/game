using Dovus.Core.Boss;
using Dovus.Core.Shared;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Status;

namespace Dovus.Core.Equipment
{
    /// <summary>Büyü Kitabı değiştirme bonusu: o vuruş mana yemez.</summary>
    public static class WeaponManaWaiver
    {
        public static void Charge(ResourceTracker tracker, float cost, bool freeCast)
        {
            if (freeCast || tracker == null || cost <= 0f)
                return;
            tracker.Consume(cost);
        }
    }
}
