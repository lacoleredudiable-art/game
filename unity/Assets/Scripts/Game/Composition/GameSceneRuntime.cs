using Dovus.Core.Tuning;
using Dovus.Game.Actors;
using Dovus.Game.Config;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Team;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Vfx;

namespace Dovus.Game.Composition
{
    /// <summary>Composition'da kurulan oyun oturumu servisleri (statik bind yerine).</summary>
    public sealed class GameSceneRuntime : ISkillSceneRuntime
    {
        public IDebugFlags DebugFlags { get; }
        public PlaceholderFactory Placeholders { get; }
        public HitboxVfxRegistry HitboxVfx { get; }
        public FeelVfxRuntime FeelVfx { get; }
        public FeelHapticsRuntime Haptics { get; }
        public HitImpactFxRuntime HitImpact { get; }
        public UiJuiceRuntime UiJuice { get; }
        public SceneLiveRegistry<TargetableHost> Targetables { get; } = new();
        public SceneLiveRegistry<AllyDummyController> AllyDummies { get; } = new();
        public SceneLiveRegistry<SummonExecutor> SummonExecutors { get; } = new();
        public IDebugPanelInputState DebugPanelInput { get; set; }
        public IDebugPanelsChromeSink DebugPanelsChrome { get; set; }

        public KenneyVfxTextures Kenney { get; }

        public GameSceneRuntime(
            IDebugFlags debugFlags,
            VfxLibrary vfxLibrary,
            GameTuning tuning,
            FeelTuning feelTuning)
        {
            DebugFlags = debugFlags;
            Kenney = new KenneyVfxTextures(tuning);
            Placeholders = new PlaceholderFactory(vfxLibrary);
            HitboxVfx = new HitboxVfxRegistry(vfxLibrary);
            FeelVfx = new FeelVfxRuntime(tuning, vfxLibrary);
            Haptics = new FeelHapticsRuntime(feelTuning);
            HitImpact = new HitImpactFxRuntime(feelTuning, Kenney);
            UiJuice = new UiJuiceRuntime();
        }
    }
}
