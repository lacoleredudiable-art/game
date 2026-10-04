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
    /// <summary>
    /// Küre HUD düğmesi. Çizim alanındaki basılı tutma ve çift dokunuş yoktur.
    /// Eldeyse seçili hedefe gider, dışarıdaysa geri çağrılır. Yolculuk süresi OrbAnchor'da.
    /// </summary>
    public static class OrbHudCommand
    {
        public static OrbGestureResult Tap(bool atHand) =>
            atHand ? OrbGestureResult.Place : OrbGestureResult.Recall;
    }
}
