using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    public sealed class DebugToolsBuilder
    {
        public void Build(WorldContext ctx)
        {
            ArenaBuilder.EnsureEventSystem();
            if (DebugConfig.Enabled)
            {
                var v6Panel = ctx.HexagonRoot.AddComponent<GrammarDebugPanel>();
                v6Panel.Configure(
                    ctx.HexagonInput,
                    ctx.ManifestationDirector,
                    ctx.HexagonRoot.GetComponent<BuildSelectScreen>(),
                    ctx.HexagonView.CanvasRoot,
                    ctx.PlayerVitals);
                CreateTuningPanel(ctx.TuningConfig, ctx.PlayerVitals, ctx.FollowCamera);
                ctx.HexagonRoot.AddComponent<DebugPanelsController>();
            }
        }

        static void CreateTuningPanel(TuningConfig tuningConfig, PlayerVitals vitals, FollowCamera follow)
        {
            var panelGo = new GameObject("TuningPanel");
            var panel = panelGo.AddComponent<TuningPanel>();
            panel.Configure(tuningConfig, vitals, follow);
        }
    }
}
