using Dovus.Core;
using Dovus.Core.Boss;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Casting;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Shared;
using Dovus.Core.Status;
using Dovus.Game.Assets;
using Dovus.Game.Skills.Presentation;
using Dovus.Game.Skills.RuleEngineV4;
using Dovus.Game.Actors;
using Dovus.Game.Platform;
using Dovus.Game.Platform;
using Dovus.Game.Audio;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Data;
using Dovus.Game.Diagnostics;
using Dovus.Game.Feel;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Vfx;
using UnityEngine;

namespace Dovus.Game.Composition.Builders
{
    public sealed class SkillSystemBuilder
    {
        public void Build(WorldContext ctx)
        {
            var tuning = ctx.Tuning;
            var combat = ctx.Combat;
            var player = ctx.Player.transform;
            var boss = ctx.BossReactorController;
            var vitals = ctx.PlayerVitalsHost;
            var telegraph = ctx.BossTelegraphView;
            var follow = ctx.FollowCameraController;
            var input = ctx.HexagonInputController;
            var view = ctx.HexagonView;
            var debug = ctx.SentenceDebugHud;
            var readout = ctx.ReactionReadoutHud;
            var damageHud = ctx.DamageNumberHud;
            var passiveHud = ctx.PassiveHud;
            var skills = ctx.Skills;
            var runeManager = ctx.RuneManager;
            var design = ctx.ElementDesign;
            var assetCatalog = ctx.AssetCatalog;
            var playerStatus = ctx.PlayerStatus;
            var bossStatus = ctx.BossStatus;
            var resource = ctx.PlayerResourceHost;
            var cooldown = ctx.PlayerCooldownHost;
            var root = ctx.HexagonRoot;

            var runtime = ctx.Runtime;
            var debugFlags = runtime.DebugFlags;

            if (ctx.AllyDummyController != null)
            {
                ctx.AllyDummyController.BindTeam(ctx.TeamAccess);
                ctx.AllyDummyController.BindLiveRegistry(runtime.AllyDummies);
            }
            if (ctx.AllyDummy2Controller != null)
            {
                ctx.AllyDummy2Controller.BindTeam(ctx.TeamAccess);
                ctx.AllyDummy2Controller.BindLiveRegistry(runtime.AllyDummies);
            }

            var feelGo = new GameObject("CombatFeel");
            feelGo.transform.SetParent(ctx.SceneRoot, false);
            var feel = feelGo.AddComponent<CombatFeelDirector>();
            var visualFreeze = feelGo.AddComponent<VisualFreezeView>();
            feel.Bind(ctx.Clock, follow, combat, tuning, ctx.OverlayCamera, debug, readout);
            feel.BindActors(ctx.Player.GetComponent<HitFlashView>(), ctx.Boss.GetComponent<HitFlashView>());
            var playerVisual = ctx.Player.GetComponent<ActorView>();
            var bossVisualComp = ctx.Boss.GetComponent<BossView>();
            Animator playerAnim = playerVisual != null ? playerVisual.Animator : null;
            Animator bossAnim = bossVisualComp != null ? bossVisualComp.Animator : null;
            visualFreeze.Bind(follow, playerAnim);
            feel.BindPresentation(visualFreeze, ctx.Afterimage, player);
            feel.BindHaptics(runtime.Haptics);
            var bossFlinch = ctx.Boss.gameObject.GetComponent<BossHitFlinchView>() ?? ctx.Boss.gameObject.AddComponent<BossHitFlinchView>();
            bossFlinch.Bind(combat.Feel, bossAnim);
            ctx.Player.GetComponent<PlayerDodgeController>()?.Bind(ctx.Clock, input, follow, readout, feel);
            var overlayHud = feelGo.AddComponent<CombatOverlayHud>();
            overlayHud.BindTheme(ctx.Assets.HudTheme);
            overlayHud.BindUiJuice(runtime.UiJuice);
            overlayHud.Configure(
                vitals, ctx.BossVitals, player, ctx.Boss.transform, ctx.OverlayCamera, ctx.MainCamera, follow);
            ctx.CombatFeelDirector = feel;

            var directorGo = ctx.Boss.gameObject;
            var bossDir = directorGo.AddComponent<BossDirector>();
            bossDir.Bind(ctx.Clock, combat, tuning, boss, input, player, vitals, ctx.BossVitals, telegraph, feel);
            bossDir.BindTeam(ctx.TeamAccess);
            if (bossStatus != null)
                bossDir.BindStatus(bossStatus);
            if (playerStatus != null)
                bossDir.BindPlayerStatus(playerStatus);
            bossDir.BindVisual(ctx.Boss.GetComponent<BossView>());
            follow?.BindBossDirector(bossDir);
            ctx.BossDirector = bossDir;

            var hostileTargets = directorGo.AddComponent<HostileTargetsHost>();
            TargetingConfig targetingConfig = BossEncounterData.LoadTargeting(tuning.Boss.ActiveBossResourcePath);
            hostileTargets.Configure(targetingConfig);
            hostileTargets.Register(
                player,
                TargetKind.Player,
                CompositionConstants.PlayerRadiusM,
                alive: () => vitals == null || !vitals.IsDown,
                stealthed: () => playerStatus != null && playerStatus.Board.IsStealthed);
            if (ctx.AllyDummyController != null)
            {
                ctx.AllyDummyController.ConfigureLife(targetingConfig);
                hostileTargets.Register(
                    ctx.AllyDummyController.transform,
                    TargetKind.Ally,
                    CompositionConstants.PlayerRadiusM * SkillSystemBuilderDefaults.PlayerColliderRadiusScale,
                    alive: () => !ctx.AllyDummyController.IsDown,
                    stealthed: () => ctx.AllyDummyController.Board != null && ctx.AllyDummyController.Board.IsStealthed,
                    damage: raw => RuleEngineV4WorldGuard.ApplyBossDamageToAlly(
                        ctx.ManifestationDirector,
                        ctx.AllyDummyController.transform,
                        raw,
                        ctx.AllyDummyController.ApplyBossDamage));
            }
            if (ctx.AllyDummy2Controller != null)
            {
                ctx.AllyDummy2Controller.ConfigureLife(targetingConfig);
                hostileTargets.Register(
                    ctx.AllyDummy2Controller.transform,
                    TargetKind.Ally,
                    CompositionConstants.PlayerRadiusM * SkillSystemBuilderDefaults.PlayerColliderRadiusScale,
                    alive: () => !ctx.AllyDummy2Controller.IsDown,
                    stealthed: () => ctx.AllyDummy2Controller.Board != null && ctx.AllyDummy2Controller.Board.IsStealthed,
                    damage: raw => RuleEngineV4WorldGuard.ApplyBossDamageToAlly(
                        ctx.ManifestationDirector,
                        ctx.AllyDummy2Controller.transform,
                        raw,
                        ctx.AllyDummy2Controller.ApplyBossDamage));
            }
            bossDir.BindTargets(hostileTargets);
            ctx.VitalsHud.BindBoss(bossDir);

            // AddComponent sırası master ile aynı: SfxDirector burada eklenir.
            var sfx = feelGo.AddComponent<SfxDirector>();
            sfx.Bind(ctx.Assets.Sfx);
            ctx.Sfx = sfx;
            ctx.Player.GetComponent<PlayerDodgeController>()?.BindSfx(sfx);
            var presentationFx = feelGo.AddComponent<PresentationFxView>();
            presentationFx.Bind(bossDir, ctx.DodgeMotionController, feel, input, sfx, follow, combat);
            presentationFx.BindFeelVfx(runtime.FeelVfx);
#if UNITY_EDITOR || DOVUS_DEBUG
            var feelVerify = feelGo.AddComponent<Dovus.Game.DevTools.FeelPlayVerifyController>();
            feelVerify.Bind(follow, player);
#endif
            var playerSteps = ctx.Player.gameObject.AddComponent<FootstepView>();
            playerSteps.StrideM = tuning.Player.FootstepStrideM;
            playerSteps.Bind(sfx);
            playerSteps.BindFeelVfx(runtime.FeelVfx);
            var bossSteps = ctx.Boss.gameObject.AddComponent<FootstepView>();
            bossSteps.StrideM = tuning.Player.BossFootstepStrideM;
            bossSteps.IsBoss = true;
            bossSteps.Bind(sfx);
            bossSteps.BindFeelVfx(runtime.FeelVfx);

            var scarsGo = new GameObject("GroundScars");
            scarsGo.transform.SetParent(ctx.SceneRoot, false);
            var scars = scarsGo.AddComponent<GroundScarFieldView>();
            scars.Configure(tuning);
            ctx.GroundScars = scars;

            EquipmentBonusResolver equipmentBonus;
            EquipmentItem equippedWeapon;
            if (design != null && assetCatalog != null)
            {
                equipmentBonus = new EquipmentBonusResolver(design.Equipment);
                equippedWeapon = assetCatalog.FindWeapon(4)?.ToEquipmentItem();
            }
            else
            {
                equipmentBonus = LoadPrototypeEquipment(out equippedWeapon);
            }
            ctx.EquipmentBonus = equipmentBonus;
            ctx.EquippedWeapon = equippedWeapon;
            if (equippedWeapon != null)
                DebugConfig.DevLog($"[Equipment] prototip silah={equippedWeapon.Name}; v6 fiil uyumu etkin.");
            var skillFactory = new SkillFactory(skills, equipmentBonus);
            ctx.SkillFactory = skillFactory;
            VerifyBindingPipeline(design, assetCatalog, runeManager, skillFactory, equippedWeapon);

            var manGo = new GameObject("Manifestation");
            manGo.transform.SetParent(ctx.SceneRoot, false);
            var director = manGo.AddComponent<ManifestationDirector>();
            var targeting = ctx.Player.GetComponent<PlayerTargetingController>();
            director.Bind(ctx.Clock, input, player, ctx.PlayerPose, boss, ctx.BossVitals, scars, tuning, damageHud, bossDir, playerStatus, bossStatus, debug, readout, follow, ctx.AllyDummyController, view, passiveHud, equippedWeapon, equipmentBonus, skills, skillFactory, design?.Animations);
            director.BindCombatFeel(feel);
            director.BindTeam(ctx.TeamAccess);
            director.BindSfx(sfx);
            director.BindTargeting(targeting);
            director.BindHostileTargets(hostileTargets);
            director.BindSceneRuntime(runtime);
            director.BindLiveRegistries(runtime.Targetables, runtime.SummonExecutors);
            ctx.ManifestationDirector = director;
            TextAsset motionTemplates = AssetLoader.Load<TextAsset>("ElementSystem/motion-templates", null);
            if (motionTemplates != null && !string.IsNullOrWhiteSpace(motionTemplates.text))
                director.ConfigureMotionTemplates(MotionTemplateCatalog.FromJson(motionTemplates.text));
            director.BindCastPresentation(new CastPresentationListener());

            BossEncounterData.ApplyVolley(combat.Boss, tuning.Boss.ActiveBossResourcePath);
            var projectileHost = directorGo.AddComponent<HostileProjectileHost>();
            projectileHost.Bind(ctx.Clock, hostileTargets, player, playerStatus, vitals, ctx.Boss.transform, boss.BodyRadiusM);
            projectileHost.BindMainCamera(ctx.MainCamera, follow);
            bossDir.BindProjectiles(projectileHost);
            director.BindProjectiles(projectileHost);

            ctx.Boss.GetComponent<MotionTemplateBodyHost>()?.Bind(
                ctx.Clock, combat.SkillMotion.ArenaHalfSizeM, CompositionConstants.BossRadiusM);
            ctx.Boss.GetComponent<MotionTemplateBodyHost>()?.BindFollowCamera(follow);
            ctx.Player.GetComponent<MotionTemplateBodyHost>()?.BindFollowCamera(follow);
            var webFields = directorGo.AddComponent<WebFieldView>();
            webFields.Bind(ctx.Clock, combat, bossDir, ctx.BossVitals, player, playerStatus);
            if (ctx.AllyDummyController != null)
                webFields.RegisterAlly(ctx.AllyDummyController);
            director.ConfigureWeaponCycle(design?.Equipment.Items);
            if (design != null)
            {
                DesignWarnings.Warned -= LogDesignWarning;
                DesignWarnings.Warned += LogDesignWarning;
                SkillNumberCatalog numbers = SkillNumberCatalog.FromDocument(design.Document);
                SkillNumberTuningApplier.ApplyCcDurations(numbers, combat.Status);
                SkillNumberTuningApplier.ApplyBasicStrikeRange(numbers, combat.Manifestation);
                director.ConfigureSkillNumbers(numbers);
                resource.Bind(numbers.MaxMana, numbers.ManaRegenPerSec, numbers.ManaRegenDelaySec);
                cooldown.Bind(numbers.GlobalCooldownSec, numbers.MaxConcurrentCasts);
                director.ConfigureWeaponSwap(WeaponSwapRules.FromDocument(design.Document));
                director.ConfigureVerbExecution(VerbExecutionData.FromDocument(design.Document));
                MobilityCcData mobilityCc = MobilityCcData.FromDocument(design.Document);
                director.ConfigureMobilityCc(mobilityCc);
            }
            view.BindWeaponSwap(director, ctx.Clock);
            input.WeaponSwapRequested += () => director.TryRequestWeaponSwap();
            input.OrbCommandRequested += () => director.ToggleOrb();
            input.SwapHoldCommandSec = () => director.SwapButtonHoldSec;

            var preview = root.AddComponent<SkillPreviewHud>();
            preview.BindTheme(ctx.Assets.HudTheme);
            preview.BindUiJuice(runtime.UiJuice);
            preview.Configure(input.Engine, skills, skillFactory, director, tuning, view.CanvasRoot);

            var buildSelect = root.AddComponent<BuildSelectHud>();
            buildSelect.Configure(skills, runeManager, input, view, ctx.Clock, director, !tuning.Hud.SkipBuildSelectOnStart);

            int elementTransitionMs = SkillSystemBuilderDefaults.ElementTransitionMs;
            if (design != null
                && ElementSystemHeader.TryParse(design.Document, elementTransitionMs, out ElementSystemHeader elementHdr))
                elementTransitionMs = elementHdr.SelectionTransitionMs;
            var elementMenu = root.AddComponent<ElementRadialMenuHud>();
            elementMenu.BindTheme(ctx.Assets.HudTheme);
            elementMenu.Configure(
                director, skills, playerStatus, tuning, view.CanvasRoot, elementTransitionMs);
            ctx.ElementMenu = elementMenu;
            ctx.Player.GetComponent<MoveInputController>()?.BindElementMenu(elementMenu);
            ctx.CameraOrbitController?.BindElementMenu(elementMenu);
            ctx.PlayerTargetingController?.BindElementMenu(elementMenu);
        }

        static void LogDesignWarning(string message) => Debug.LogWarning(message);

        static void VerifyBindingPipeline(
            ElementSystemDesign design,
            ElementSystemAssetCatalog assets,
            RuneManager runes,
            SkillFactory factory,
            EquipmentItem weapon)
        {
            if (design == null || assets == null || runes == null || factory == null || weapon == null)
            {
                Debug.LogWarning("[BindingReady] v6.1.1 preflight atlandı: bağımlılık eksik.");
                return;
            }

            int elementId = assets.Elements.Count > 0 ? assets.Elements[0].Id : 0;
            var buildSkills = runes.BuildSkills(factory, weapon, elementId);
            Skill smoke = factory.Create(1, 1, weapon, elementId);
            if (assets.Runes.Count != 12 || assets.Weapons.Count != 10
                || assets.Elements.Count != 6 || buildSkills.Count != SkillSystemBuilderDefaults.ExpectedBuildSkillCount
                || smoke.Resolution.Identity.Verb.Value != "1"
                || smoke.Resolution.Identity.Adjective.Value != "1")
            {
                throw new System.InvalidOperationException(
                    "v6 binding preflight 12/10/6 SO, 36 build skill ve 1-1 smoke bekler.");
            }

            DebugConfig.DevLog(
                $"[BindingReady] JSON {design.Version} → SO 12/10/6 → "
                + $"buildSkills={buildSkills.Count} → smoke={smoke.DisplayName} → "
                + $"weapon={weapon.Name} → element={assets.Elements[0].DisplayName}");
        }

        static EquipmentBonusResolver LoadPrototypeEquipment(out EquipmentItem weapon)
        {
            weapon = null;
            if (!ElementSystemJsonLoader.TryLoad(out ElementSystemDesign design))
                return new EquipmentBonusResolver();

            try
            {
                EquipmentCatalog catalog = design.Equipment;
                weapon = catalog.FindWeapon(4);
                return new EquipmentBonusResolver(catalog);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Equipment] katalog okunamadı: {e.Message}");
                return new EquipmentBonusResolver();
            }
        }
    }
}
