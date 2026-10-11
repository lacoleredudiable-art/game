using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Yol bağımsız VFX girişleri: eski MotionTemplateDriver ve (birleşince) kural_motoru_v4
    /// aynı API'yi çağırır. Bayrak/path bilmez — yalnız çözülmüş skill + teslim olayları.
    /// </summary>
    public static class RuleDrivenVfxSink
    {
        public static RuleDrivenVfxDirector Resolve(Transform actor)
        {
            if (actor == null)
                return null;
            return actor.GetComponent<RuleDrivenVfxDirector>()
                ?? actor.GetComponentInChildren<RuleDrivenVfxDirector>();
        }

        /// <summary>Cast shout / skill çözümü sonrası — uyanış + rün; iz/silüet event bekler.</summary>
        public static void BeginSkill(Transform actor, in SkillResolution skill, string weaponKey)
        {
            Resolve(actor)?.BeginSkill(skill, weaponKey);
        }

        public static void BeginSkill(Transform actor, in SkillResolution skill, EquipmentItem weapon)
        {
            BeginSkill(actor, skill, WeaponKey(weapon));
        }

        /// <summary>v4 basılı tutma: yalnız harf flaşı, 2. rün konduğu an.</summary>
        public static void FlashRunes(Transform actor, in SkillResolution skill, EquipmentItem weapon)
        {
            Resolve(actor)?.FlashRunes(skill, WeaponKey(weapon));
        }

        public static void ClearRunePreflash(Transform actor)
        {
            Resolve(actor)?.ClearRunePreflash();
        }

        /// <summary>"Atılmadı" geri bildirimi: yalnız mana yetmedi / bekleme sürüyor (ıska ve boşa gitmede çağrılmaz).</summary>
        public static void NotCast(Transform actor, int verbRuneId, int adjectiveRuneId, RuleEngineV4CastDenial denial)
        {
            if (!RuleEngineV4NotCastFx.Applies(denial))
                return;
            Resolve(actor)?.NotCast(verbRuneId, adjectiveRuneId, denial);
        }

        static string WeaponKey(EquipmentItem weapon) =>
            weapon != null
                ? (string.IsNullOrEmpty(weapon.AnimationsKey) ? weapon.Id : weapon.AnimationsKey)
                : string.Empty;

        /// <summary>Teslim isabeti (motion hit, HasarVer, impact anim event).</summary>
        public static void NotifyHit(Transform actor, Vector3 hitOrigin, Vector3 hitDir)
        {
            Resolve(actor)?.NotifyHit(hitOrigin, hitDir);
        }

        /// <summary>NotifyBossStruck köprüsü — path bağımsız isabet VFX.</summary>
        public static void NotifyBossStrike(Transform player, Transform boss, Vector3? hitPoint)
        {
            if (player == null)
                return;
            Vector3 origin = hitPoint ?? (boss != null ? boss.position : player.position);
            Vector3 dir = boss != null ? boss.position - player.position : player.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                dir = player.forward;
            NotifyHit(player, origin, dir.normalized);
        }

        /// <summary>Dash/teslim bitişi (kenar freni).</summary>
        public static void NotifyMotionEnded(Transform actor, bool stoppedAtBodyEdge)
        {
            Resolve(actor)?.NotifyMotionEnded(stoppedAtBodyEdge);
        }
    }
}
