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
            string weaponKey = _equippedWeapon != null ? _equippedWeapon.AnimationsKey : string.Empty;
            string archetype = WeaponArchetypeMap.ArchetypeFor(weaponKey);
            _combatFeel?.OnBossStruck(isCrit, allowHitstop, archetype);

            Vector3? hit = BossHitPoint();
            if (hit.HasValue && _boss != null)
            {
                Color tint = DamageTint() ?? Color.white;
                _sceneRuntime?.HitImpact?.Play(hit.Value, archetype, tint, isCrit, _boss.transform);
            }

            // Yol bağımsız: motion hit + kural_motoru_v4 ApplyDamage → aynı isabet VFX.
            if (_player != null)
            {
                Vector3 origin = hit ?? (_boss != null ? _boss.transform.position : _player.position);
                Vector3 dir = _boss != null
                    ? _boss.transform.position - _player.position
                    : _player.forward;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = _player.forward;
                RuleDrivenVfxSink.NotifyHit(_player, origin, dir.normalized);
            }

            _sfx?.Play(isCrit ? SfxLibrary.Crit : SfxLibrary.Hit);
        }
    }
}
