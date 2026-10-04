using Dovus.Core.Grammar;
using Dovus.Core.Element;
using Dovus.Game.Actors;
using Dovus.Game.Cameras;
using Dovus.Game.Casting;
using Dovus.Game.Config;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
using Dovus.Game.Hud;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dovus.Game.Composition.Builders
{
    public sealed class HexagonInputBuilder
    {
        public void Build(WorldContext ctx)
        {
            PrototypeTuning _tuning = ctx.Tuning;
            var combat = ctx.Combat;
            var player = ctx.Player.transform;
            var boss = ctx.BossReactor;
            var vitals = ctx.PlayerVitals;
            var follow = ctx.FollowCamera;

            var root = new GameObject("Hexagon");
            root.transform.SetParent(ctx.SceneRoot, false);
            ctx.HexagonRoot = root;

            var mainCam = ctx.MainCamera;
            if (mainCam != null)
                mainCam.cullingMask &= ~(1 << CompositionConstants.HexagonInkLayer);

            var overlayGo = new GameObject("InkOverlayCam");
            overlayGo.transform.SetParent(root.transform, false);
            var overlay = overlayGo.AddComponent<HexagonOverlayCamera>();
            overlay.Build(CompositionConstants.HexagonInkLayer);
            ctx.Overlay = overlay;
            ctx.OverlayCamera = overlay.Cam;
            AttachOverlayToMain(mainCam, overlay.Cam);

            ElementSystemDesign design = null;
            ElementSystemAssetCatalog assetCatalog = null;
            if (ctx.Assets.TryGetElementDesign(out ElementSystemDesign loaded))
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
            view.Build(_tuning, overlay.Cam, skills, loadout);
            ctx.HexagonView = view;
            if (follow != null)
                view.BindLockOn(follow);

            var moveInput = player.GetComponent<MoveInput>();
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
            var ink = inkGo.AddComponent<InkTrail>();
            ink.Configure(_tuning, overlay, CompositionConstants.HexagonInkLayer);
            ctx.InkTrail = ink;

            var syllable = root.AddComponent<SyllableFeedback>();
            syllable.Configure(_tuning);
            ctx.SyllableFeedback = syllable;
            var debug = root.AddComponent<SentenceDebugHud>();
            ctx.SentenceDebugHud = debug;

            var input = root.AddComponent<HexagonInput>();
            input.Tuning = _tuning;
            input.Combat = combat;
            input.Bind(ctx.Clock, ink, syllable, debug, skills, loadout);
            input.DotAccepted += view.NotifyPressed;
            input.DrawCaption += view.ShowDrawCaption;
            ctx.HexagonInput = input;

            if (follow != null)
            {
                var orbit = root.AddComponent<CameraOrbitInput>();
                ctx.CameraOrbitInput = orbit;
                orbit.Bind(follow, player.GetComponent<MoveInput>(), input, _tuning, view);
                var motor = player.GetComponent<KinematicMotor>();
                motor?.BindCamera(follow);
            }
            debug.Configure(input.Engine, view.CanvasRoot, skills, DebugConfig.Enabled && _tuning.Hud.ShowSentenceDebugHud);
            debug.BindVitals(vitals);
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

            var readout = root.AddComponent<ReactionReadout>();
            readout.Configure(combat.Feel, _tuning, view.CanvasRoot);
            ctx.ReactionReadout = readout;
            input.BindResource(ctx.PlayerResource, readout, skills);
            input.BindCooldown(ctx.PlayerCooldown, readout, skills);

            var targeting = ctx.Player.gameObject.AddComponent<PlayerTargeting>();
            targeting.Bind(
                player,
                0,
                ctx.MainCamera,
                input,
                combat.Dodge.TapMaxMoveDp,
                view.CanvasRoot,
                follow);
            ctx.PlayerTargeting = targeting;
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
