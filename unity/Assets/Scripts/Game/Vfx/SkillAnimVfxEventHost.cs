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

        public void Bind(RuleDrivenVfxDirector director) => _director = director;

        // Unity Animation Event fonksiyon adları (klip marker'ları).
        public void Trail_On() => _director?.OnTrailOn();
        public void Trail_Off() => _director?.OnTrailOff();
        public void TrailOn() => _director?.OnTrailOn();
        public void TrailOff() => _director?.OnTrailOff();

        public void Impact() => _director?.OnImpactAnimEvent();
        public void impact() => _director?.OnImpactAnimEvent();

        public void Ejder() => _director?.OnEjderAnimEvent();
        public void ejder() => _director?.OnEjderAnimEvent();
    }
}
