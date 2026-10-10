using Dovus.Core.Grammar;
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

        /// <summary>Teslim isabeti (motion hit, HasarVer, impact anim event).</summary>
        public static void NotifyHit(Transform actor, Vector3 hitOrigin, Vector3 hitDir)
        {
            Resolve(actor)?.NotifyHit(hitOrigin, hitDir);
        }

        /// <summary>Dash/teslim bitişi (kenar freni).</summary>
        public static void NotifyMotionEnded(Transform actor, bool stoppedAtBodyEdge)
        {
            Resolve(actor)?.NotifyMotionEnded(stoppedAtBodyEdge);
        }
    }
}
