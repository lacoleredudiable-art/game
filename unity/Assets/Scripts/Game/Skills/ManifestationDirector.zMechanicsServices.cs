using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Mechanic;
using Dovus.Core.Status;
using Dovus.Core.Tuning;
using Dovus.Game.Data;
using Dovus.Game.Skills.Hosts;
using Dovus.Game.Skills.Mechanics;
using System.Linq;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        MdMechanicsHost _mechanicsHost;
        MechanicWorldRuntime _mechanicWorld;
        JsonEffectRuntime _jsonEffects;
        VolumePayloadApplier _volumePayload;
        MechanicPortals _mechanicPortals;

        internal MechanicGrammar MechanicEngine =>
            ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design) ? design.Mechanics : null;

        internal SkillResolution SkillFromPlan(MechanicPlan plan)
        {
            if (_skills == null || plan == null || plan.Verb <= 0 || plan.Adjective <= 0)
                return SkillResolution.Empty;
            return _skills.Resolve(new[] { plan.Verb, plan.Adjective });
        }

        internal bool HasSelfReflect(MechanicPlan plan) =>
            plan != null && !JsonEffectRules.IsWorldMirror(plan)
            && plan.Effects.Any(e => e.Stat == "yansit" && e.Target == "kendin");

        internal int JsonCleanseCount(in SkillResolution skill) =>
            JsonEffectRules.CleanseCount(
                !skill.IsEmpty && !skill.Engine.IsNull ? skill.Engine.CleanseCount(0) : 0,
                MechanicPlanFor(skill));

        internal float ShieldAbsorbFor(in SkillResolution skill)
        {
            float absorb = !skill.IsEmpty && !skill.Engine.IsNull
                ? skill.Engine.ShieldAbsorb(0f)
                : 0f;
            StatusTuning tuning = _combat != null ? _combat.Status : new StatusTuning();
            return absorb > 0f ? absorb : tuning.ShieldAbsorb;
        }

        internal int MechanicWorldLeftoverCount()
        {
            if (_mechanicWorld == null)
                return 0;
            return _mechanicWorld.LeftoverCount() + _mechanicPortals.LeftoverCount();
        }

        internal void ClearMechanicWorldSweep()
        {
            EnsureMechanicsServices();
            _mechanicWorld.ClearSweepState();
            _mechanicPortals.ClearSweepState();
        }

        void EnsureMechanicsServices()
        {
            if (_mechanicsHost != null)
                return;
            _mechanicsHost = new MdMechanicsHost(this);
            _mechanicWorld = new MechanicWorldRuntime(_mechanicsHost);
            _jsonEffects = new JsonEffectRuntime(_mechanicsHost, _mechanicWorld);
            _volumePayload = new VolumePayloadApplier(_mechanicsHost, _jsonEffects);
            _mechanicWorld.Wire(_volumePayload, _jsonEffects);
            _mechanicPortals = new MechanicPortals(_mechanicsHost);
        }
    }
}
