using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
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
        /// <summary>Kor izi skill'e aittir: düz vuruş klibi aynı olsa da bu olaylar dinlenmez.</summary>
        public const RuleEngineV4SkillAnimEvent SkillOnlyEvents =
            RuleEngineV4SkillAnimEvent.TrailOn | RuleEngineV4SkillAnimEvent.TrailOff;

        RuleDrivenVfxDirector _director;
        RuleEngineV4SkillAnimEvent _mask = RuleEngineV4SkillAnimEvent.TrailOn
            | RuleEngineV4SkillAnimEvent.TrailOff
            | RuleEngineV4SkillAnimEvent.Impact
            | RuleEngineV4SkillAnimEvent.Ejder;
        readonly SkillClipHold _basicStrike = new();
        RuleEngineV4SkillAnimEvent _basicStrikeMask;
        Animator _animator;

        public void Bind(RuleDrivenVfxDirector director) => _director = director;

        /// <summary>Skill çağrısı: olay maskesi klip kuralından; düz vuruş penceresi kapanır.</summary>
        public void SetEventMask(RuleEngineV4SkillAnimEvent mask)
        {
            _mask = mask;
            _basicStrike.Release();
        }

        /// <summary>
        /// Düz vuruş klibi state'i oynarken yalnız bu maske geçer, Trail_On / Trail_Off asla.
        /// State'ten çıkılınca skill maskesi geri gelir.
        /// </summary>
        public void BeginBasicStrike(string stateName, RuleEngineV4SkillAnimEvent events)
        {
            _basicStrike.Hold(stateName);
            _basicStrikeMask = events & ~SkillOnlyEvents;
        }

        /// <summary>Düz vuruş penceresinde gelip yutulan Trail_On sayısı (test kanıtı).</summary>
        public int SuppressedTrailEvents { get; private set; }

        bool Allows(RuleEngineV4SkillAnimEvent ev)
        {
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_basicStrike.IsHeld(_animator))
                return (_basicStrikeMask & ev) != 0;
            return (_mask & ev) != 0;
        }

        // Unity Animation Event fonksiyon adları (klip marker'ları).
        public void Trail_On()
        {
            if (Allows(RuleEngineV4SkillAnimEvent.TrailOn))
                _director?.OnTrailOn();
            else if (_basicStrike.IsHeld(_animator))
                SuppressedTrailEvents++;
        }

        public void Trail_Off()
        {
            if (Allows(RuleEngineV4SkillAnimEvent.TrailOff))
                _director?.OnTrailOff();
        }

        public void TrailOn() => Trail_On();
        public void TrailOff() => Trail_Off();

        /// <summary>Klipten gelen Impact olayı sayısı (maske dışı olanlar dahil).</summary>
        public int ImpactEvents { get; private set; }

        public void Impact()
        {
            ImpactEvents++;
            if (Allows(RuleEngineV4SkillAnimEvent.Impact))
                _director?.OnImpactAnimEvent();
        }

        public void impact() => Impact();

        public void Ejder()
        {
            if (Allows(RuleEngineV4SkillAnimEvent.Ejder))
                _director?.OnEjderAnimEvent();
        }

        public void ejder() => Ejder();
    }
}
