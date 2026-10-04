using Dovus.Game.Actors;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Vfx;

namespace Dovus.Game.Skills
{
    public sealed partial class ManifestationDirector
    {
        ISkillSceneRuntime _sceneRuntime;
        SceneLiveRegistry<TargetableHost> _liveTargetables;
        SceneLiveRegistry<SummonExecutor> _liveSummons;

        public void BindSceneRuntime(ISkillSceneRuntime runtime) => _sceneRuntime = runtime;

        public void BindLiveRegistries(
            SceneLiveRegistry<TargetableHost> targetables,
            SceneLiveRegistry<SummonExecutor> summonExecutors)
        {
            _liveTargetables = targetables;
            _liveSummons = summonExecutors;
        }

        internal PlaceholderFactory Placeholders => _sceneRuntime?.Placeholders;

        internal HitboxVfxRegistry HitboxVfx => _sceneRuntime?.HitboxVfx;
    }
}
