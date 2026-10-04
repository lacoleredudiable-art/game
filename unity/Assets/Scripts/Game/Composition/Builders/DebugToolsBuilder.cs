using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.DevTools;
using Dovus.Game.Diagnostics;
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
                var v6Panel = ctx.HexagonRoot.AddComponent<GrammarDebugHud>();
                v6Panel.Configure(
                    ctx.HexagonInputController,
                    ctx.ManifestationDirector,
                    ctx.HexagonRoot.GetComponent<BuildSelectHud>(),
                    ctx.HexagonView.CanvasRoot,
                    ctx.PlayerVitalsHost);
                CreateTuningPanel(ctx.TuningConfig, ctx.PlayerVitalsHost, ctx.FollowCameraController);
                ctx.HexagonRoot.AddComponent<DebugPanelsController>();
            }
        }

        static void CreateTuningPanel(TuningConfig tuningConfig, PlayerVitalsHost vitals, FollowCameraController follow)
        {
            var panelGo = new GameObject("TuningPanel");
            var panel = panelGo.AddComponent<TuningPanelHud>();
            panel.Configure(tuningConfig, vitals, follow);
        }
    }
}
