using Dovus.Game.Actors;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Vfx;

namespace Dovus.Game.Skills
{
    public sealed partial class RuleEngineV4SentenceHost
    {
        internal ISkillSceneRuntime _sceneRuntime;
        internal SceneLiveRegistry<TargetableHost> _liveTargetables;

        public void BindSceneRuntime(ISkillSceneRuntime runtime) => _sceneRuntime = runtime;

        public void BindLiveRegistries(SceneLiveRegistry<TargetableHost> targetables) =>
            _liveTargetables = targetables;

        internal PlaceholderFactory Placeholders => _sceneRuntime?.Placeholders;

        internal HitboxVfxRegistry HitboxVfx => _sceneRuntime?.HitboxVfx;
    }
}
