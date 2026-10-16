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
            var sliceEquipment = ctx.SliceEquipment;
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
            var ruleVfx = ctx.Player.GetComponent<RuleDrivenVfxDirector>()
                ?? ctx.Player.AddComponent<RuleDrivenVfxDirector>();
            ruleVfx.Bind(runtime.FeelVfx, EjderVfxBatchHost.Create(ctx.SceneRoot, ctx.MainCamera));
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
            // kural_motoru_v4: denge çubuğu tavanı scale.boss_poise_bar (dünyaya özel CombatTuning örneği).
            combat.Boss.PoiseMax = ctx.Host.RuleEngineV4Scale.BossPoiseBar;
            BossAttackDamage? sliceBossDamage = RuleEngineV4BossDamage.Resolve(
                ctx.Host.RuleEngineV4Catalog.SliceBossAttacks, combat.Boss, combat.Status);
            RuleEngineV4BossDamage.ApplyPoisonBreathShape(ctx.Host.RuleEngineV4Catalog.SliceBossAttacks, combat.Boss);
            RuleEngineV4BossDamage.DisableLegacyPoisonCombo(combat.Status);
            bossDir.Bind(ctx.Clock, combat, tuning, boss, input, player, vitals, ctx.BossVitals, telegraph, feel, sliceBossDamage);
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
            float playerBodyM = RuleEngineV4PhysicsBodies.PlayerBodyRadiusM(ctx, CompositionConstants.PlayerRadiusM);
            float allyBodyM = RuleEngineV4PhysicsBodies.PlayerBodyRadiusM(
                ctx, CompositionConstants.PlayerRadiusM * SkillSystemBuilderDefaults.PlayerColliderRadiusScale);
            hostileTargets.Register(
                player,
                TargetKind.Player,
                playerBodyM,
                alive: () => vitals == null || !vitals.IsDown,
                stealthed: () => playerStatus != null && playerStatus.Board.IsStealthed);
            if (ctx.AllyDummyController != null)
            {
                ctx.AllyDummyController.ConfigureLife(targetingConfig);
                hostileTargets.Register(
                    ctx.AllyDummyController.transform,
                    TargetKind.Ally,
                    allyBodyM,
                    alive: () => !ctx.AllyDummyController.IsDown,
                    stealthed: () => ctx.AllyDummyController.Board != null && ctx.AllyDummyController.Board.IsStealthed,
                    damage: raw => RuleEngineV4WorldGuard.ApplyBossDamageToAlly(
                        CombatContext(ctx),
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
                    allyBodyM,
                    alive: () => !ctx.AllyDummy2Controller.IsDown,
                    stealthed: () => ctx.AllyDummy2Controller.Board != null && ctx.AllyDummy2Controller.Board.IsStealthed,
                    damage: raw => RuleEngineV4WorldGuard.ApplyBossDamageToAlly(
                        CombatContext(ctx),
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
            if (sliceEquipment != null && assetCatalog != null)
            {
                equipmentBonus = new EquipmentBonusResolver(sliceEquipment);
                equippedWeapon = assetCatalog.FindWeapon(4)?.ToEquipmentItem();
            }
            else
            {
                equipmentBonus = LoadSliceEquipment(out equippedWeapon);
            }
            ctx.EquipmentBonus = equipmentBonus;
            ctx.EquippedWeapon = equippedWeapon;
            if (equippedWeapon != null)
                DebugConfig.DevLog($"[Equipment] prototip silah={equippedWeapon.Name}; v6 fiil uyumu etkin.");
            var skillFactory = new SkillFactory(skills, equipmentBonus);
            ctx.SkillFactory = skillFactory;
            VerifyBindingPipeline(sliceEquipment, assetCatalog, runeManager, skillFactory, equippedWeapon);

            var manGo = new GameObject("Manifestation");
            manGo.transform.SetParent(ctx.SceneRoot, false);
            var director = manGo.AddComponent<RuleEngineV4SentenceHost>();
            var targeting = ctx.Player.GetComponent<PlayerTargetingController>();
            director.Bind(ctx.Clock, input, player, ctx.PlayerPose, boss, ctx.BossVitals, scars, tuning, damageHud, bossDir, playerStatus, bossStatus, debug, readout, follow, ctx.AllyDummyController, view, passiveHud, equippedWeapon, equipmentBonus, skills, skillFactory, null);
            director.BindCombatFeel(feel);
            director.BindTeam(ctx.TeamAccess);
            director.BindSfx(sfx);
            director.BindTargeting(targeting);
            director.BindHostileTargets(hostileTargets);
            director.BindSceneRuntime(runtime);
            director.BindLiveRegistries(runtime.Targetables);
            ctx.RuleEngineV4SentenceHost = director;
            ctx.RuleEngineV4Combat = RuleEngineV4ManifestationAdapter.Bind(director);
            director.BindCastPresentation(new CastPresentationListener());

            BossEncounterData.ApplyVolley(combat.Boss, tuning.Boss.ActiveBossResourcePath);
            var projectileHost = directorGo.AddComponent<HostileProjectileHost>();
            projectileHost.Bind(ctx.Clock, hostileTargets, player, playerStatus, vitals, ctx.Boss.transform, boss.BodyRadiusM);
            projectileHost.BindMainCamera(ctx.MainCamera, follow);
            bossDir.BindProjectiles(projectileHost);
            ctx.Boss.GetComponent<MotionTemplateBodyHost>()?.Bind(
                ctx.Clock, ArenaBoundary.RadiusM(combat.SkillMotion), boss.BodyRadiusM);
            ctx.Boss.GetComponent<MotionTemplateBodyHost>()?.BindFollowCamera(follow);
            ctx.Player.GetComponent<MotionTemplateBodyHost>()?.BindFollowCamera(follow);
            var webFields = directorGo.AddComponent<WebFieldView>();
            webFields.Bind(ctx.Clock, combat, bossDir, ctx.BossVitals, player, playerStatus);
            if (ctx.AllyDummyController != null)
                webFields.RegisterAlly(ctx.AllyDummyController);
            director.ConfigureWeaponCycle(sliceEquipment?.Items);
            string sliceJson = SliceCombatBootstrap.GrammarJson;
            if (!string.IsNullOrWhiteSpace(sliceJson))
            {
                SkillNumberCatalog numbers = SkillNumberCatalog.FromJson(sliceJson);
                SkillNumberTuningApplier.ApplyCcDurations(numbers, combat.Status);
                SkillNumberTuningApplier.ApplyBasicStrikeRange(numbers, combat.Manifestation);
                director.ConfigureSkillNumbers(numbers);
                resource.Bind(numbers.MaxMana, numbers.ManaRegenPerSec, numbers.ManaRegenDelaySec);
                cooldown.Bind(numbers.GlobalCooldownSec, numbers.MaxConcurrentCasts);
                director.ConfigureWeaponSwap(WeaponSwapRules.FromJson(sliceJson));
                director.ConfigureVerbExecution(VerbExecutionData.FromJson(sliceJson));
                MobilityCcData mobilityCc = MobilityCcData.FromJson(sliceJson);
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
            var elementMenu = root.AddComponent<ElementRadialMenuHud>();
            elementMenu.BindTheme(ctx.Assets.HudTheme);
            elementMenu.Configure(
                director, skills, playerStatus, tuning, view.CanvasRoot, elementTransitionMs);
            ctx.ElementMenu = elementMenu;
            ctx.Player.GetComponent<MoveInputController>()?.BindElementMenu(elementMenu);
            ctx.CameraOrbitController?.BindElementMenu(elementMenu);
            ctx.PlayerTargetingController?.BindElementMenu(elementMenu);
        }

        static RuleEngineV4CombatContext CombatContext(WorldContext ctx) =>
            ctx.RuleEngineV4Combat ?? RuleEngineV4ManifestationAdapter.Bind(ctx.RuleEngineV4SentenceHost);

        static void LogDesignWarning(string message) => Debug.LogWarning(message);

        static void VerifyBindingPipeline(
            EquipmentCatalog equipment,
            SliceRuntimeAssetCatalog assets,
            RuneManager runes,
            SkillFactory factory,
            EquipmentItem weapon)
        {
            if (equipment == null || assets == null || runes == null || factory == null || weapon == null)
            {
                Debug.LogWarning("[BindingReady] dilim preflight atlandı: bağımlılık eksik.");
                return;
            }

            int elementId = assets.Elements.Count > 0 ? assets.Elements[0].Id : 0;
            var buildSkills = runes.BuildSkills(factory, weapon, elementId);
            Skill smoke = factory.Create(1, 1, weapon, elementId);
            if (assets.Runes.Count != 6 || assets.Weapons.Count != 2
                || buildSkills.Count != SkillSystemBuilderDefaults.ExpectedBuildSkillCount
                || smoke.Resolution.Identity.Verb.Value != "1"
                || smoke.Resolution.Identity.Adjective.Value != "1")
            {
                throw new System.InvalidOperationException(
                    "dilim binding preflight 6/2 SO, 36 build skill ve 1-1 smoke bekler.");
            }

            DebugConfig.DevLog(
                $"[BindingReady] dilim gramer → SO 6/2 → "
                + $"buildSkills={buildSkills.Count} → smoke={smoke.DisplayName} → "
                + $"weapon={weapon.Name}");
        }

        static EquipmentBonusResolver LoadSliceEquipment(out EquipmentItem weapon)
        {
            weapon = null;
            if (!SliceCombatBootstrap.TryLoad(out _, out EquipmentCatalog catalog, out _))
                return new EquipmentBonusResolver();

            try
            {
                weapon = catalog.FindWeapon(4);
                return new EquipmentBonusResolver(catalog);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Equipment] dilim katalog okunamadı: {e.Message}");
                return new EquipmentBonusResolver();
            }
        }
    }
}
