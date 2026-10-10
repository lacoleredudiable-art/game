using Dovus.Game.Audio;
using Dovus.Game.Feel;
using Dovus.Game.Vfx;
using Dovus.Game.Weapons;
using UnityEngine;

namespace Dovus.Game.Skills
{
    /// <summary>İsabet hissi köprüsü: hasar yolları buradan CombatFeelDirector'e (hitstop/parlama) ve havuzlu isabet VFX'e haber verir.</summary>
    public sealed partial class ManifestationDirector
    {
        CombatFeelDirector _combatFeel;

        public void BindCombatFeel(CombatFeelDirector feel) => _combatFeel = feel;

        internal void NotifyBossStruck(bool isCrit, bool allowHitstop)
        {
            string archetype = WeaponArchetypeMap.ArchetypeFor(
                _equippedWeapon != null ? _equippedWeapon.AnimationsKey : string.Empty);
            _combatFeel?.OnBossStruck(isCrit, allowHitstop, archetype);
            Vector3? hit = BossHitPoint();
            if (hit.HasValue && _boss != null)
                _sceneRuntime?.HitImpact?.Play(
                    hit.Value, archetype, DamageTint() ?? Color.white, isCrit, _boss.transform);
            RuleDrivenVfxSink.NotifyBossStrike(_player, _boss != null ? _boss.transform : null, hit);
            _sfx?.Play(isCrit ? SfxLibrary.Crit : SfxLibrary.Hit);
        }
    }
}
