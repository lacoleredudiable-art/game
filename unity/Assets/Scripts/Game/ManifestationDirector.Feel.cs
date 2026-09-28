using UnityEngine;

namespace Dovus.Game
{
    /// <summary>İsabet hissi köprüsü: hasar yolları buradan CombatFeel'e (hitstop/parlama) ve kıvılcım + sese haber verir.</summary>
    public sealed partial class ManifestationDirector
    {
        CombatFeel _combatFeel;

        void NotifyBossStruck(bool isCrit, bool allowHitstop)
        {
            if (_combatFeel == null)
                _combatFeel = FindAnyObjectByType<CombatFeel>();
            _combatFeel?.OnBossStruck(isCrit, allowHitstop);

            Vector3? hit = BossHitPoint();
            if (hit.HasValue && _boss != null)
                FeelVfx.HitSpark(hit.Value - Vector3.up * _boss.BodyRadiusM, DamageTint() ?? Color.white, isCrit);
            SfxDirector.Play(isCrit ? SfxLibrary.Crit : SfxLibrary.Hit);
        }
    }
}
