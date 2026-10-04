# MAP.md — konu → dosya → giriş noktası

**Nasıl kullanılır:** Görevde bir konu adı geçiyorsa bu tabloda bul; yalnız listelenen dosyayı aç. Giriş noktası, okumaya başlayacağın tip ve metot. Kök `AGENTS.md` sert kurallar; iş sırası `docs/PLAN.md`; mimari `docs/ARCHITECTURE.md`; oyun özeti `docs/OYUN.md`. Doğrulama: `tools/verify.ps1`.

**Büyük dosya uyarısı:** `ManifestationDirector` (partial toplamı `ClassSizeRatchetTests` ≤3411; `Game/Skills/State/*`, `Game/Skills/Hosts/*`), `HexagonInputController`, `BossDirector`, `GameTuning` — tam dosya okuma; önce giriş noktası, `Select-String` ile daralt. Kurulum `Game/Composition/Builders/*`.

| Konu | Dosya(lar) | Giriş noktası |
|------|------------|---------------|
| Skill atma — girdi | `Game/Casting/HexagonInputController.cs` (orkestratör), `Game/Casting/Input/` (`PointerRouter`, `StrokeCaster`, `CastGate`, `DodgeTrigger`, `CastFeedback`), `Game/Casting/MoveInputController.cs`, `Game/Casting/JoystickView.cs` | `HexagonInputController.Update` |
| Skill atma — cümle | `Core/Grammar/SentenceEngine.cs`, `Core/Grammar/SentenceState.cs` | `SentenceEngine.OnDotTouched`, `SentenceEngine.Commit`, `SentenceEngine.Tick` |
| Skill atma — çözüm | `Core/Grammar/SkillMotor.cs`, `Game/Data/SkillMotorLoader.cs`, `Game/Data/ElementSystemJsonLoader.cs` | `SkillMotor.Resolve`, `SkillMotor.ResolveWords`, `SkillMotorLoader.Load` |
| Skill atma — yürütme | `App/Casting/CastPipeline.cs`, `Game/Skills/ManifestationDirector.cs`, `Game/Skills/ManifestationDirector.VerbExecution.cs`, `Core/Casting/SkillExecutorRouter.cs`, `Game/Skills/Execution/ISkillExecutor.cs` | `CastPipeline.RunSkill`, `CastPipeline.RunBasic`, `ManifestationDirector.OnSentenceCompleted`, `SkillExecutorRouter.Route` |
| Skill atma — görsel/VFX | `Game/Skills/ManifestationDirector.CastPresentation.cs`, `Game/Skills/Motion/MotionTemplateDriver.cs`, `Game/Skills/Motion/MotionHitResolver.cs`, `Game/Vfx/FeelVfx.cs`, `Game/Vfx/PresentationFxView.cs` | `MotionTemplateDriver.TryBeginMotionTemplate`, `ManifestationDirector.Update` |
| Silah pasifleri / küre / top | `Game/Skills/Weapons/WeaponPassiveRuntime.cs`, `Game/Skills/Weapons/OrbController.cs`, `Game/Skills/Weapons/CannonBlast.cs`, `Game/Skills/ManifestationDirector.zWeaponServices.cs` | `WeaponPassiveRuntime.HitMods`, `OrbController.TryPlace`, `CannonBlast.TryCannonBlast` |
| Kalıp teslim kuyruğu | `Game/Skills/Motion/TemplateDeliveryRuntime.cs`, `Game/Skills/ManifestationDirector.zMotionServices.cs` | `TemplateDeliveryRuntime.ArmTemplateDelivery` |
| Mermi silme | `Game/Skills/Projectiles/ProjectileEraser.cs`, `Game/Skills/ManifestationDirector.Projectiles.cs` | `ProjectileEraser.TickProjectileErase`, `ManifestationDirector.BindProjectiles` |
| Rün / cümle grameri | `Core/Element/Rune.cs`, `Core/Element/RuneLoadout.cs`, `Core/Grammar/RuneManager.cs`, `Core/Input/HexagonLayout.cs`, `Core/Input/JumpKind.cs` | `RuneManager.TrySelect`, `SentenceEngine` |
| Skill verisi (JSON) | `docs/element-sistemi.json`, `unity/Assets/Resources/ElementSystem/element-sistemi.json`, `Core/Data/ElementSystemDocument.cs` | `ElementSystemDocument.Parse`, `SkillMotor.FromDocument`, `ElementSystemJsonLoader.TryLoad` |
| JSON mapper'lar | `Core/Boss/BossEncounterMapper.cs`, `Core/Data/VfxBindingMapper.cs`, `Core/Data/ElementSystemHeader.cs`, `Core/Data/ElementSystemDocument.cs`, `Core/Grammar/SkillEngineModifiers.cs` | `BossEncounterMapper.TryParseHud`, `VfxBindingMapper.TryParse`, `ElementSystemHeader.TryParse`, `ElementSystemDocument.Parse` |
| Hareket kalıbı — Core | `Core/Motion/MotionTemplateRunner.cs`, `Core/Motion/MotionTemplateCatalog.cs`, `Core/Motion/PositionOwnership.cs` | `MotionTemplateRunner.Tick`, `PositionOwnership.Prepare` |
| Hareket kalıbı — Game | `Game/Actors/MotionTemplateBodyHost.cs`, `docs/motion-templates.json` (üretim: `tools/build-motion-templates.py`) | `MotionTemplateBodyHost.TickMotion` |
| Hasar / kritik | `Core/Damage/DamagePipeline.cs`, `Core/Damage/CritSystem.cs`, `Core/Damage/ClosingDamageMath.cs` | `DamagePipeline.Resolve` |
| Status | `Core/Status/StatusBoard.cs`, `Core/Status/StatusApplicator.cs`, `Game/Actors/ActorStatusHost.cs` | `StatusBoard.Tick`, `StatusApplicator.Apply` |
| Dodge — Core | `Core/Dodge/DodgeState.cs`, `Core/Dodge/DodgeChargeBank.cs`, `Core/Dodge/PerfectDodgeRule.cs`, `Core/Dodge/DodgeCancelRules.cs`, `Core/Dodge/SkillCastLease.cs` | `DodgeState` (durum geçişleri) |
| Dodge — Game | `Game/Actors/DodgeMotionController.cs`, `Game/Actors/PlayerDodgeController.cs`, `Game/Hud/DodgeChargeHud.cs` | `DodgeMotionController.Update`, `PlayerDodgeController.Update` |
| Boss — Core | `Core/Boss/BossVitals.cs`, `Core/Boss/BossAttack.cs`, `Core/Boss/BossPoise.cs`, `Core/Dodge/ExchangeResolver.cs`, `Core/Boss/BossAttackKindPicker.cs` | `ExchangeResolver.Resolve`, `BossVitals` |
| Boss — App | `App/Boss/BossAttackSelector.cs`, `App/Boss/BossBrain.cs`, `App/Boss/BossStrikeResolver.cs`, `App/Boss/BossMechanicStatusMap.cs`, `App/Boss/VolleyPattern.cs`, `App/Boss/BossApproachRules.cs`, `App/Boss/BossPoiseState.cs`, `App/Boss/BossDirectorRules.cs` | `BossAttackSelector.TrySelect`, `BossBrain.Tick`, `BossStrikeResolver.PlayerVolumeHits`, `VolleyPattern.ComputeLayout` |
| Boss — Game | `Game/Boss/BossDirector.cs` (+ partial'lar), `Game/Boss/BossDirector.StrikeApplier.cs` (nested strike), `Game/Boss/BossReactorController.cs`, `Game/Boss/BossView.cs`, `Game/Boss/BossTelegraphView.cs`, `Game/Boss/HostileTargetsHost.cs`, `Game/Boss/HostileProjectileHost.cs` | `BossDirector.Update`, nested `StrikeApplier` |
| Silah / ekipman / pasif | `Core/Equipment/EquipmentCatalog.cs`, `Core/Equipment/WeaponPassiveKind.cs`, `Core/Equipment/WeaponSwapRules.cs`, `Core/Equipment/WeaponSwapState.cs`, `Core/Passives/SlotPassiveDirector.cs`, `Game/Weapons/WeaponSO.cs` | `WeaponSwapState`, `SlotPassiveDirector`, `WeaponPassiveKind` |
| Silah görselleri / el prop | `Game/Weapons/WeaponVisualRegistry.cs`, `Game/Weapons/WeaponHandPropsView.cs`, `Game/Weapons/WeaponArchetypeMap.cs`, `Game/Actors/ActorView.cs` | `WeaponVisualRegistry` (SO), `WeaponHandPropsView.Awake` |
| Portal / sınır / takım | `Core/Portal/PortalSystem.cs`, `Core/Border/BorderMode.cs`, `Core/Team/TeamComboSystem.cs`, `App/Team/TeamModifierTable.cs`, `Game/Team/TeamModifierHub.cs`, `Game/Team/TeamComboHost.cs`, `Game/Team/TeamComboAccess.cs` | `PortalSystem.Cast`, `PortalSystem.Tick`, `TeamComboHost.Update`; çarpanlar `TeamComboAccess.Hub` → `TeamModifierHub`; portal/takım skill kimliği → `PortalSystem.IsPortalSkill` / `TeamComboSystem.IsTeamSkill` (kurulu `portal_op` / `team_op`) |
| Oyuncu can / mana / diriliş | `App/Actors/PlayerHealth.cs`, `Game/Actors/PlayerVitalsHost.cs`, `Game/Actors/PlayerResourceHost.cs`, `Core/Shared/ResourceTracker.cs` | `PlayerVitalsHost.Tick`, `PlayerResourceHost.Update` |
| Aktör DDD (2B.15a) | `Core/Actors/Actor.cs`, `App/Actors/ActorRegistry.cs`, `Game/Actors/ActorViewRegistry.cs`, `Game/Composition/Builders/ActorsBuilder.cs` | `ActorsBuilder.Build` (kayıt), `PlayerTargetingController.SelectedActorId` |
| Kombo soğuma (CD) | `Core/Casting/CooldownTracker.cs`, `Core/Casting/ComboCooldownKey.cs`, `Game/Actors/PlayerCooldownHost.cs`, `Game/Casting/HexagonInputController.cs`, `Game/Skills/ManifestationDirector.cs` | `ComboCooldownKey.For`, `CooldownTracker.TryStart`, `HexagonInputController.TryAllowComboCooldownForNextDot`, `ManifestationDirector.ApplyCooldown` |
| HUD | `Game/Hud/VitalsHud.cs`, `Game/Hud/CombatOverlayHud.cs`, `Game/Hud/SkillPreviewHud.cs`, `Game/Hud/PassiveHud.cs`, `Game/Hud/DamageNumberHud.cs` | ilgili `Update` / `Tick*` |
| Kamera | `Game/Cameras/FollowCameraController.cs`, `Game/Cameras/CameraOrbitController.cs`, `Game/Cameras/CameraAmbienceColliders.cs` | `FollowCameraController.Update`, `FollowCameraController.ResolveYaw` |
| VFX / SFX | `Game/Vfx/VfxLibrary.cs`, `Game/Audio/SfxDirector.cs`, `Game/Audio/SfxLibrary.cs`, `Game/Vfx/HitImpactFx.cs`, `Game/Skills/Execution/ComposedSkillVfxView.cs` | `SfxDirector.Awake`, `VfxLibrary` |
| Tuning | `Game/Config/GameTuning.cs`, `GameTuning.Sections.cs`, `GameTuning.Migrate.cs`, `Game/Config/Sections/*Settings.cs`, `Game/Config/TuningConfig.cs`, `Core/Tuning/*.cs` | `GameTuning.EnsureRuntimeDefaults`, `TuningSchema` |
| Sahne kurulumu | `Game/Composition/GameBootstrapHost.cs` (`Dovus.Game.Composition.asmdef`), `Game/Composition/AssetCatalog.cs`, `Game/Assets/AssetLoader.cs`, `Game/Composition/Builders/` (`WorldContext`, `ArenaBuilder`, `ActorsBuilder`, `CameraBuilder`, `HexagonInputBuilder`, `HudBuilder`, `SkillSystemBuilder`, `DebugToolsBuilder`, `VisualAttach`), `Game/Vfx/PlaceholderFactory.cs`, `Game/Arena/CircularArena.cs` | `GameBootstrapHost.Awake` → `BuildWorld`: `ctx.Assets = AssetCatalog.Standalone`; `VfxLibraryStandalone.Instance`; runtime `Resources.Load` / `Shader.Find` yalnız `AssetLoader`; eksik yol raporu `docs/asset-references.md` (`AssetReferenceTests`); `CameraBuilder` → `ctx.MainCamera` / `ctx.FollowCameraController`; saat ve kamera `Bind*` ile runtime bileşenlere (Find/Camera.main yok; `FindInjectionTests`) |
| Debug / dev | `Game/Diagnostics/DebugConfig.cs`, `Game/DevTools/` (`Dovus.Game.DevTools.asmdef`, `UNITY_EDITOR \|\| DOVUS_DEBUG`), `DebugPanelsController`, `GrammarDebugHud`, `SentenceDebugHud`, `TuningPanelHud` | `DebugPanelsController`, `GrammarDebugHud.Update`; runtime girdi `Diagnostics/DebugPanelInput` |
| Editor menüleri | `Game/Editor/Sweep/PlaySweep*.cs`, `Game/Editor/*Bind.cs`, `Game/Editor/AndroidBuilder.cs` | `[MenuItem("Dovus/...")]` |
| Play Sweep (saf) | `App/Sweep/SweepComboCatalog.cs`, `App/Sweep/SweepCsvFormat.cs` | `SweepComboCatalog.OrderedCombos` |
| Testler — Core | `tools/CoreTests/*Tests.cs`, `tools/CoreTests/AppSimulationTests.cs` (App, shim yok) | `dotnet test tools/CoreTests` |
| Testler — Integration | `tools/IntegrationTests/*Tests.cs` | `dotnet test tools/IntegrationTests` |
| Git dışı asset yedeği (Synty/Mixamo/VFX) | `docs/asset-yedegi.md`, `tools/IntegrationTests/known-missing-asset-guids.txt` | yedek zip: PC `C:\Users\lacol\_backup\` |
| Araç — gramer | `tools/AtomSim/Program.cs` | `dotnet run --project tools/AtomSim` |
| Araç — başsız tarama | `tools/SweepV2/Host/Program.cs`, `tools/SweepV2/Shim/*` (Unity shim; davranış doğrulaması) | `SweepV2.Program.Main` |
| Araç — Game derleme | `tools/GameCompile/check.py`, `tools/GameCompile/GameCompile.csproj` | `python tools/GameCompile/check.py` |
| CI | `.github/workflows/sweep-v2.yml` | workflow `sweep` job |
| Zaman / saat | `Core/Time/TimeDirector.cs`, `App/Time/TimeDirectorClock.cs`, `Game/Platform/GameClockHost.cs` | `TimeDirector.Tick`, `GameClockHost.Update`, `GameClockHost.World` |
| App (Dovus.App) | `App/Time/TimeDirectorClock.cs`, `App/Time/ManualClock.cs`, `App/AGENTS.md` | `TimeDirectorClock.Advance` |
| Platform adaptörleri | `Game/Platform/UnityFrameClock.cs`, `Game/Platform/UnityUnscaledClock.cs`, `Game/Platform/UnityRng.cs`, `Game/Platform/GameClockHost.cs`, `Game/Platform/AssetCatalog.cs`, `Game/Platform/PlaceholderFactory.cs` | `UnityFrameClock.Default`, `GameClockHost.World`, `AssetCatalog.Standalone` |
| Debug kapısı / HUD arayüzleri | `Game/Diagnostics/DebugConfig.cs`, `Game/Diagnostics/ISentenceDebugSink.cs` | `DebugConfig.Enabled`, `ISentenceDebugSink.NoteSkillBang` |
| IClock / IRng (Core) | `Core/Shared/IClock.cs`, `Core/Shared/IRng.cs`, `Core/Shared/CombatRng.cs` | `IClock.DeltaSec`, `CombatRng.Seeded` |
| Mekanik gramer | `Core/Mechanic/MechanicGrammar.cs`, `Core/Mechanic/MechanicRules.cs`, `Game/Skills/ManifestationDirector.MechanicGrammar.cs` | `MechanicGrammar.Compose` |
| Canlı efekt / plan | `Core/Manifestation/LivingEffect.cs`, `Core/Manifestation/SkillWorldPlanner.cs` | `LivingEffect.Tick`, `SkillWorldPlanner.Plan` |
| Hedefleme | `Core/Boss/TargetPicker.cs`, `Core/Casting/TargetingRules.cs`, `Game/Actors/PlayerTargetingController.cs` | `PlayerTargetingController.Update` |
| Boss mermileri | `Core/Boss/HostileProjectiles.cs`, `Game/Boss/HostileProjectileHost.cs` | `HostileProjectileHost.Update` |
| Animasyon köprüsü | `Core/Presentation/AnimationDatabase.cs`, `Game/Actors/AnimationBridge.cs` | `AnimationDatabase.FromJson` |
| Hexagon UI | `Game/Casting/HexagonView.cs` (partial'lar), `Game/Casting/HexagonLayoutScreen.cs`, `Game/Casting/ElementRadialMenuHud.cs` | `HexagonView` partial'ları |
| Build seçim ekranı | `Game/Hud/BuildSelectHud.cs` | `BuildSelectHud` |
| Doğrulama tek komut | `tools/verify.ps1` | `powershell -File tools/verify.ps1` |

*Not:* `SkillMotionMotor` yalnız saf plan üretir (tür, i-frame, hedef); `SkillMotionDriver` ölü. Skill sırasında konum yalnız `MotionTemplateRunner` + `MotionTemplateBodyHost` (kök `AGENTS.md` §3).
