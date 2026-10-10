using Dovus.Core.RuleEngineV4;
using UnityEngine;

namespace Dovus.Game.Vfx
{
    /// <summary>
    /// Animation Event köprüsü: kliplerdeki Trail_On / Trail_Off / Impact / Ejder
    /// (ve camelCase eşleri) → <see cref="RuleDrivenVfxDirector"/>.
    /// Animator GO'suna eklenir; sabit kare numarası yok.
    /// </summary>
    public sealed class SkillAnimVfxEventHost : MonoBehaviour
    {
        RuleDrivenVfxDirector _director;
        RuleEngineV4SkillAnimEvent _mask = RuleEngineV4SkillAnimEvent.TrailOn
            | RuleEngineV4SkillAnimEvent.TrailOff
            | RuleEngineV4SkillAnimEvent.Impact
            | RuleEngineV4SkillAnimEvent.Ejder;

        public void Bind(RuleDrivenVfxDirector director) => _director = director;

        public void SetEventMask(RuleEngineV4SkillAnimEvent mask) =>
            _mask = mask;

        // Unity Animation Event fonksiyon adları (klip marker'ları).
        public void Trail_On()
        {
            if ((_mask & RuleEngineV4SkillAnimEvent.TrailOn) != 0)
                _director?.OnTrailOn();
        }

        public void Trail_Off()
        {
            if ((_mask & RuleEngineV4SkillAnimEvent.TrailOff) != 0)
                _director?.OnTrailOff();
        }

        public void TrailOn() => Trail_On();
        public void TrailOff() => Trail_Off();

        public void Impact()
        {
            if ((_mask & RuleEngineV4SkillAnimEvent.Impact) != 0)
                _director?.OnImpactAnimEvent();
        }

        public void impact() => Impact();

        public void Ejder()
        {
            if ((_mask & RuleEngineV4SkillAnimEvent.Ejder) != 0)
                _director?.OnEjderAnimEvent();
        }

        public void ejder() => Ejder();
    }
}
