using Dovus.Core.Actors;
using Dovus.Core.Grammar;
using Dovus.Core.Element;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Platform;
using Dovus.Game.Platform;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
#if UNITY_EDITOR || DOVUS_DEBUG
using Dovus.Game.DevTools;
#endif
using Dovus.Game.Hud;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Composition.Builders
{
    public sealed class HexagonInputBuilder
    {
        public void Build(WorldContext ctx)
        {
            GameTuning _tuning = ctx.Tuning;
            var combat = ctx.Combat;
            var player = ctx.Player.transform;
            var boss = ctx.BossReactorController;
            var vitals = ctx.PlayerVitalsHost;
            var follow = ctx.FollowCameraController;

            var root = new GameObject("Hexagon");
            root.transform.SetParent(ctx.SceneRoot, false);
            ctx.HexagonRoot = root;

            var mainCam = ctx.MainCamera;
            if (mainCam != null)
                mainCam.cullingMask &= ~(1 << CompositionConstants.HexagonInkLayer);

            var overlayGo = new GameObject("InkOverlayCam");
            overlayGo.transform.SetParent(root.transform, false);
            var overlay = overlayGo.AddComponent<HexagonOverlayCameraView>();
            overlay.Build(CompositionConstants.HexagonInkLayer);
            ctx.Overlay = overlay;
            ctx.OverlayCamera = overlay.Cam;
            AttachOverlayToMain(mainCam, overlay.Cam);

            ElementSystemDesign design = null;
            ElementSystemAssetCatalog assetCatalog = null;
            if (ElementSystemJsonLoader.TryLoad(out ElementSystemDesign loaded))
            {
                design = loaded;
                assetCatalog = ElementSystemAssetCatalog.CreateRuntime(design);
            }
            ctx.ElementDesign = design;
            ctx.AssetCatalog = assetCatalog;
            SkillMotor skills = design?.SkillMotor ?? SkillMotorLoader.Load();
            ctx.Skills = skills;
            var runeManager = new RuneManager(skills);
            if (!runeManager.TrySelectMainClass(
                    ctx.PrototypeMainClassId,
                    ctx.PrototypePassiveRuneIds,
                    out string buildError))
            {
                Debug.LogWarning($"[RuneManager] {buildError}; varsayılan build kullanıldı.");
            }
            ctx.RuneManager = runeManager;
            RuneLoadout loadout = runeManager.Current;
            ctx.RuneLoadout = loadout;
            DebugConfig.DevLog(
                $"[ElementSystem] mainClass={ctx.PrototypeMainClassId} build=[{string.Join(",", loadout.RuneIds)}] "
                + $"passives={loadout.PassiveCount}; SO={assetCatalog?.Runes.Count ?? 0}/"
                + $"{assetCatalog?.Weapons.Count ?? 0}/{assetCatalog?.Elements.Count ?? 0}");
            var view = root.AddComponent<HexagonView>();
            view.BindTheme(ctx.Assets.HudTheme);
            view.BindUiJuice(ctx.Runtime.UiJuice);
            view.Build(_tuning, overlay.Cam, skills, loadout);
            ctx.HexagonView = view;
            if (follow != null)
                view.BindLockOn(follow);

            var moveInput = player.GetComponent<MoveInputController>();
            if (moveInput != null)
            {
                var joystickGo = new GameObject("JoystickView");
                joystickGo.transform.SetParent(root.transform, false);
                var joystick = joystickGo.AddComponent<JoystickView>();
                joystick.Build(moveInput, _tuning, overlay.Cam);
            }

            var inkGo = new GameObject("InkTrail");
            inkGo.transform.SetParent(root.transform, false);
            inkGo.layer = CompositionConstants.HexagonInkLayer;
            var ink = inkGo.AddComponent<InkTrailView>();
            ink.Configure(_tuning, overlay, CompositionConstants.HexagonInkLayer, ctx.Runtime.Kenney);
            ctx.InkTrailView = ink;

            var syllable = root.AddComponent<SyllableFeedbackView>();
            syllable.Configure(_tuning, ctx.Runtime.Haptics);
            ctx.SyllableFeedbackView = syllable;
#if UNITY_EDITOR || DOVUS_DEBUG
            var debug = root.AddComponent<SentenceDebugHud>();
#else
            ISentenceDebugSink debug = null;
#endif
            ctx.SentenceDebugHud = debug;

            var input = root.AddComponent<HexagonInputController>();
            input.Tuning = _tuning;
            input.Combat = combat;
            input.Bind(ctx.Clock, ink, syllable, debug, skills, loadout);
            input.DotAccepted += view.NotifyPressed;
            input.DrawCaption += view.ShowDrawCaption;
            ctx.HexagonInputController = input;
            input.ConfigureDebugAndFeel(ctx.Runtime.DebugPanelInput, ctx.Runtime.Haptics);
            moveInput?.ConfigureDebugPanel(ctx.Runtime.DebugPanelInput);

            if (follow != null)
            {
                var orbit = root.AddComponent<CameraOrbitController>();
                ctx.CameraOrbitController = orbit;
                orbit.Bind(follow, player.GetComponent<MoveInputController>(), input, _tuning, view);
                var motor = player.GetComponent<KinematicMotorController>();
                motor?.BindCamera(follow);
            }
#if UNITY_EDITOR || DOVUS_DEBUG
            debug.Configure(input.Engine, view.CanvasRoot, skills, DebugConfig.Enabled && _tuning.Hud.ShowSentenceDebugHud);
            debug.BindVitals(vitals);
#endif
            input.BindVitals(vitals);

            var playerStatus = ctx.PlayerStatus;
            var bossStatus = ctx.BossStatus;
            if (playerStatus != null)
            {
                playerStatus.Bind(ctx.Clock, combat.Status, vitals, null, null);
                playerStatus.BindTeam(ctx.TeamAccess);
                input.BindStatus(playerStatus);
            }
            if (bossStatus != null)
            {
                bossStatus.Bind(ctx.Clock, combat.Status, null, ctx.BossVitals, boss);
                bossStatus.BindTeam(ctx.TeamAccess);
            }

            var readout = root.AddComponent<ReactionReadoutHud>();
            readout.Configure(combat.Feel, _tuning, view.CanvasRoot);
            ctx.ReactionReadoutHud = readout;
            input.BindResource(ctx.PlayerResourceHost, readout, skills);
            input.BindCooldown(ctx.PlayerCooldownHost, readout, skills);

            var targeting = ctx.Player.gameObject.AddComponent<PlayerTargetingController>();
            targeting.Bind(
                player,
                0,
                ctx.MainCamera,
                input,
                combat.Dodge.TapMaxMoveDp,
                view.CanvasRoot,
                follow,
                ctx.ActorViewRegistry,
                ActorDefaults.PlayerId);
            targeting.ConfigureDebugPanel(ctx.Runtime.DebugPanelInput);
            targeting.ConfigureTargetRegistry(ctx.Runtime.Targetables);
            ctx.PlayerTargetingController = targeting;
        }

        static void AttachOverlayToMain(Camera main, Camera overlay)
        {
            if (main == null || overlay == null)
                return;

            var mainData = main.GetComponent<UniversalAdditionalCameraData>();
            if (mainData == null)
                mainData = main.gameObject.AddComponent<UniversalAdditionalCameraData>();

            var overlayData = overlay.GetComponent<UniversalAdditionalCameraData>();
            if (overlayData == null)
                overlayData = overlay.gameObject.AddComponent<UniversalAdditionalCameraData>();

            overlayData.renderType = CameraRenderType.Overlay;
            if (!mainData.cameraStack.Contains(overlay))
                mainData.cameraStack.Add(overlay);
        }
    }
}
