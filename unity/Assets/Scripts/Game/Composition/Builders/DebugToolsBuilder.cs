using Dovus.Core.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Diagnostics;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.RuleEngineV4;
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
            CreateGozcu(ctx);
        }

        static void CreateGozcu(WorldContext ctx)
        {
            TextAsset json = Resources.Load<TextAsset>(RuleEngineV4CatalogAccess.JsonResource);
            if (json == null)
                Debug.LogError("[V4-FALLBACK] Gözcü: kural-motoru-v4.json yok, varsayılan eşikler.");
            RuleEngineV4Gozcu rules = RuleEngineV4Gozcu.FromJson(json != null ? json.text : "{}");
            var go = new GameObject(nameof(Dovus.Game.DevTools.GozcuHost));
            go.transform.SetParent(ctx.SceneRoot, false);
            go.AddComponent<Dovus.Game.DevTools.GozcuHost>().Configure(
                rules,
                ctx.Player != null ? ctx.Player.transform : null,
                ctx.MainCamera,
                ctx.ManifestationDirector);
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
