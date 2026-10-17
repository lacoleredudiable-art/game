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
                ctx.RuleEngineV4SentenceHost,
                ctx.HexagonRoot.GetComponent<BuildSelectHud>(),
                ctx.HexagonView.CanvasRoot,
                ctx.PlayerVitalsHost,
                ctx.Runtime.DebugFlags);
            CreateTuningPanel(ctx.TuningConfig, ctx.PlayerVitalsHost, ctx.FollowCameraController);
            ctx.HexagonRoot.AddComponent<Dovus.Game.DevTools.DebugPanelsController>();
            var teamDebug = ctx.HexagonRoot.AddComponent<Dovus.Game.DevTools.TeamDebugHud>();
            teamDebug.Configure(ctx.TeamAccess, ctx.Runtime.DebugPanelsChrome);
            CreateGozcu(ctx);
            // Dodge denemesi: BossDirector HudBuilder'dan sonra kurulur, bu yüzden burada bağlanır.
            var practice = ctx.HexagonRoot.AddComponent<Dovus.Game.DevTools.DodgePracticeController>();
            practice.Bind(ctx.Player.transform, ctx.Boss.transform, ctx.BossDirector, ctx.Clock);
        }

        static void CreateGozcu(WorldContext ctx)
        {
            TextAsset json = Resources.Load<TextAsset>(RuleEngineV4CatalogAccess.JsonResource);
            if (json == null)
                Debug.LogError("[V4-FALLBACK] Gözcü: kural-motoru-v4.json yok, varsayılan eşikler.");
            RuleEngineV4Gozcu rules = RuleEngineV4Gozcu.FromJson(json != null ? json.text : "{}");
            var go = new GameObject(nameof(Dovus.Game.DevTools.GozcuHost));
            go.transform.SetParent(ctx.SceneRoot, false);
            var gozcu = go.AddComponent<Dovus.Game.DevTools.GozcuHost>();
            gozcu.Configure(
                rules,
                ctx.Player != null ? ctx.Player.transform : null,
                ctx.MainCamera,
                () =>
                {
                    if (ctx.RuleEngineV4Combat != null)
                        return ctx.RuleEngineV4Combat.EquippedWeaponNumber();
                    if (ctx.EquippedWeapon == null)
                        return 0;
                    string id = ctx.EquippedWeapon.Id ?? string.Empty;
                    int colon = id.LastIndexOf(':');
                    return int.TryParse(colon >= 0 ? id.Substring(colon + 1) : id, out int w) ? w : 0;
                });
            CameraBossBody body = ctx.FollowCameraController != null ? ctx.FollowCameraController.BossBody : null;
            if (body != null)
                gozcu.WatchBossBody(body.Root, body.RadiusM, body.TopAboveRootM);
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
