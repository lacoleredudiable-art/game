using UnityEngine;

namespace Dovus.Game
{
    /// <summary>İsabet hissi köprüsü: hasar yolları buradan CombatFeel'e (hitstop/parlama) haber verir.</summary>
    public sealed partial class ManifestationDirector
    {
        CombatFeel _combatFeel;

        void NotifyBossStruck(bool isCrit, bool allowHitstop)
        {
            if (_combatFeel == null)
                _combatFeel = FindAnyObjectByType<CombatFeel>();
            _combatFeel?.OnBossStruck(isCrit, allowHitstop);
        }
    }
}
