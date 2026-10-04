using Dovus.Core;
using Dovus.Core.Combat;
using Dovus.Core.Data;
using Dovus.Core.Equipment;
using Dovus.Core.Execution;
using Dovus.Core.Grammar;
using Dovus.Game.Actors;
using Dovus.Game.Audio;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Data;
using Dovus.Game.DevTools;
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
            var boss = ctx.BossReactor;
            var vitals = ctx.PlayerVitals;
            var telegraph = ctx.BossTelegraph;
            var follow = ctx.FollowCamera;
            var input = ctx.HexagonInput;
            var view = ctx.HexagonView;
            var debug = ctx.SentenceDebugHud;
            var readout = ctx.ReactionReadout;
            var damageHud = ctx.DamageNumberHud;
            var passiveHud = ctx.PassiveHud;
            var skills = ctx.Skills;
            var runeManager = ctx.RuneManager;
            var design = ctx.ElementDesign;
            var assetCatalog = ctx.AssetCatalog;
            var playerStatus = ctx.PlayerStatus;
            var bossStatus = ctx.BossStatus;
            var resource = ctx.PlayerResource;
            var cooldown = ctx.PlayerCooldown;
            var root = ctx.HexagonRoot;

            var feelGo = new GameObject("CombatFeel");
            feelGo.transform.SetParent(ctx.SceneRoot, false);
            var feel = feelGo.AddComponent<CombatFeel>();
            var visualFreeze = feelGo.AddComponent<VisualFreeze>();
            feel.Bind(ctx.Clock, follow, combat, tuning, ctx.OverlayCamera, debug, readout);
            feel.BindActors(ctx.Player.GetComponent<HitFlash>(), ctx.Boss.GetComponent<HitFlash>());
            var playerVisual = ctx.Player.GetComponent<ActorVisual>();
            var bossVisualComp = ctx.Boss.GetComponent<BossVisual>();
            Animator playerAnim = playerVisual != null ? playerVisual.Animator : null;
            Animator bossAnim = bossVisualComp != null ? bossVisualComp.Animator : null;
            visualFreeze.Bind(follow, playerAnim);
            feel.BindPresentation(visualFreeze, ctx.Afterimage, player);
            HitImpactFx.Configure(combat.Feel);
            FeelHaptics.Configure(combat.Feel);
            var bossFlinch = ctx.Boss.gameObject.GetComponent<BossHitFlinch>() ?? ctx.Boss.gameObject.AddComponent<BossHitFlinch>();
            bossFlinch.Bind(combat.Feel, bossAnim);
            ctx.Player.GetComponent<PlayerDodgeRig>()?.Bind(ctx.Clock, input, follow, readout, feel);
            var overlayHud = feelGo.AddComponent<CombatOverlayHud>();
            overlayHud.Configure(vitals, ctx.BossVitals, player, ctx.Boss.transform, ctx.OverlayCamera);
            ctx.CombatFeel = feel;

            var directorGo = ctx.Boss.gameObject;
            var bossDir = directorGo.AddComponent<BossDirector>();
            bossDir.Bind(ctx.Clock, combat, tuning, boss, input, player, vitals, ctx.BossVitals, telegraph, feel);
            if (bossStatus != null)
                bossDir.BindStatus(bossStatus);
            if (playerStatus != null)
                bossDir.BindPlayerStatus(playerStatus);
            bossDir.BindVisual(ctx.Boss.GetComponent<BossVisual>());
            follow?.BindBossDirector(bossDir);
            ctx.BossDirector = bossDir;

            var hostileTargets = directorGo.AddComponent<HostileTargets>();
            TargetingConfig targetingConfig = BossEncounterData.LoadTargeting(tuning.Boss.ActiveBossResourcePath);
            hostileTargets.Configure(targetingConfig);
            hostileTargets.Register(
                player,
                TargetKind.Player,
                CompositionConstants.PlayerRadiusM,
                alive: () => vitals == null || !vitals.IsDown,
                stealthed: () => playerStatus != null && playerStatus.Board.IsStealthed);
            if (ctx.AllyDummy != null)
            {
                ctx.AllyDummy.ConfigureLife(targetingConfig);
                hostileTargets.Register(
                    ctx.AllyDummy.transform,
                    TargetKind.Ally,
                    CompositionConstants.PlayerRadiusM * 0.95f,
                    alive: () => !ctx.AllyDummy.IsDown,
                    stealthed: () => ctx.AllyDummy.Board != null && ctx.AllyDummy.Board.IsStealthed,
                    damage: raw => ctx.AllyDummy.ApplyBossDamage(raw));
            }
            bossDir.BindTargets(hostileTargets);
            ctx.VitalsHud.BindBoss(bossDir);

            feelGo.AddComponent<SfxDirector>();
            feelGo.AddComponent<PresentationFx>().Bind(bossDir, ctx.DodgeMotion, feel, input, follow, combat);
            var feelVerify = feelGo.AddComponent<FeelPlayVerify>();
            feelVerify.Bind(follow, player);
            var playerSteps = ctx.Player.gameObject.AddComponent<FootstepEmitter>();
            playerSteps.StrideM = tuning.Player.FootstepStrideM;
            var bossSteps = ctx.Boss.gameObject.AddComponent<FootstepEmitter>();
            bossSteps.StrideM = tuning.Player.BossFootstepStrideM;
            bossSteps.IsBoss = true;

            var scarsGo = new GameObject("GroundScars");
            scarsGo.transform.SetParent(ctx.SceneRoot, false);
            var scars = scarsGo.AddComponent<GroundScarField>();
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
            var targeting = ctx.Player.GetComponent<PlayerTargeting>();
            director.Bind(ctx.Clock, input, player, ctx.PlayerPose, boss, ctx.BossVitals, scars, tuning, damageHud, bossDir, playerStatus, bossStatus, debug, readout, follow, ctx.AllyDummy, view, passiveHud, equippedWeapon, equipmentBonus, skills, skillFactory, design?.Animations);
            director.BindTargeting(targeting);
            director.BindHostileTargets(hostileTargets);
            ctx.ManifestationDirector = director;

            BossEncounterData.ApplyVolley(combat.Boss, tuning.Boss.ActiveBossResourcePath);
            var projectileHost = directorGo.AddComponent<HostileProjectileHost>();
            projectileHost.Bind(ctx.Clock, hostileTargets, player, playerStatus, vitals, ctx.Boss.transform, boss.BodyRadiusM);
            bossDir.BindProjectiles(projectileHost);
            director.BindProjectiles(projectileHost);

            ctx.Boss.GetComponent<MotionTemplateBody>()?.Bind(
                ctx.Clock, combat.SkillMotion.ArenaHalfSizeM, CompositionConstants.BossRadiusM);
            var webFields = directorGo.AddComponent<WebFieldView>();
            webFields.Bind(ctx.Clock, combat, bossDir, ctx.BossVitals, player, playerStatus);
            if (ctx.AllyDummy != null)
                webFields.RegisterAlly(ctx.AllyDummy);
            director.ConfigureWeaponCycle(design?.Equipment.Items);
            if (design != null)
            {
                DesignWarnings.Warned -= LogDesignWarning;
                DesignWarnings.Warned += LogDesignWarning;
                SkillNumberCatalog numbers = SkillNumberCatalog.FromDocument(design.Document);
                numbers.ApplyCcDurations(combat.Status);
                numbers.ApplyBasicStrikeRange(combat.Manifestation);
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
            preview.Configure(input.Engine, skills, skillFactory, director, tuning, view.CanvasRoot);

            var buildSelect = root.AddComponent<BuildSelectScreen>();
            buildSelect.Configure(skills, runeManager, input, view, ctx.Clock, director, !tuning.Hud.SkipBuildSelectOnStart);

            int elementTransitionMs = 300;
            if (design != null
                && ElementSystemHeader.TryParse(design.Document, elementTransitionMs, out ElementSystemHeader elementHdr))
                elementTransitionMs = elementHdr.SelectionTransitionMs;
            var elementMenu = root.AddComponent<ElementRadialMenu>();
            elementMenu.Configure(
                director, skills, playerStatus, tuning, view.CanvasRoot, elementTransitionMs);
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
                || assets.Elements.Count != 6 || buildSkills.Count != 36
                || smoke.Id != "1-1")
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
