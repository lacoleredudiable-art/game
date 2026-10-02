using UnityEngine;

namespace Dovus.Game
{
    /// <summary>İsabet hissi köprüsü: hasar yolları buradan CombatFeel'e (hitstop/parlama) ve havuzlu isabet VFX'e haber verir.</summary>
    public sealed partial class ManifestationDirector
    {
        CombatFeel _combatFeel;

        void NotifyBossStruck(bool isCrit, bool allowHitstop)
        {
            if (_combatFeel == null)
                _combatFeel = FindAnyObjectByType<CombatFeel>();

            string weaponKey = _equippedWeapon != null ? _equippedWeapon.AnimationsKey : string.Empty;
            string archetype = WeaponArchetypeMap.ArchetypeFor(weaponKey);
            _combatFeel?.OnBossStruck(isCrit, allowHitstop, archetype);

            Vector3? hit = BossHitPoint();
            if (hit.HasValue && _boss != null)
            {
                Color tint = DamageTint() ?? Color.white;
                HitImpactFx.Play(hit.Value, archetype, tint, isCrit, _boss.transform);
            }

            SfxDirector.Play(isCrit ? SfxLibrary.Crit : SfxLibrary.Hit);
        }
    }
}
