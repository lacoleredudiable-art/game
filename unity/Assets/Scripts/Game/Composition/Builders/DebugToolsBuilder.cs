using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Team;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    public sealed class DebugToolsBuilder
    {
        public void Build(WorldContext ctx)
        {
            ArenaBuilder.EnsureEventSystem();
#if UNITY_EDITOR || DOVUS_DEBUG
            if (DebugConfig.Enabled)
            {
                BuildDebugTools(ctx);
            }
#endif
        }

#if UNITY_EDITOR || DOVUS_DEBUG
        static void BuildDebugTools(WorldContext ctx)
        {
            var v6Panel = ctx.HexagonRoot.AddComponent<Dovus.Game.DevTools.GrammarDebugHud>();
            v6Panel.Configure(
                ctx.HexagonInputController,
                ctx.ManifestationDirector,
                ctx.HexagonRoot.GetComponent<BuildSelectHud>(),
                ctx.HexagonView.CanvasRoot,
                ctx.PlayerVitalsHost,
                ctx.Runtime.DebugFlags);
            CreateTuningPanel(ctx.TuningConfig, ctx.PlayerVitalsHost, ctx.FollowCameraController);
            ctx.HexagonRoot.AddComponent<Dovus.Game.DevTools.DebugPanelsController>();
            var teamDebug = ctx.HexagonRoot.AddComponent<Dovus.Game.DevTools.TeamDebugHud>();
            teamDebug.Configure(ctx.TeamAccess, ctx.Runtime.DebugPanelsChrome);
        }

        static void CreateTuningPanel(TuningConfig tuningConfig, PlayerVitalsHost vitals, FollowCameraController follow)
        {
            var panelGo = new GameObject("TuningPanel");
            var panel = panelGo.AddComponent<Dovus.Game.DevTools.TuningPanelHud>();
            panel.Configure(tuningConfig, vitals, follow);
        }
#endif
    }
}
