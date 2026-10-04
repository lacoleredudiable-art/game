# MAP.md — konu → dosya → giriş noktası

**Nasıl kullanılır:** Görevde bir konu adı geçiyorsa bu tabloda bul; yalnız listelenen dosyayı aç. Giriş noktası, okumaya başlayacağın tip ve metot. Kök `AGENTS.md` sert kurallar; iş sırası `docs/PLAN.md`; hedef yapı `docs/ARCHITECTURE-PLAN.md`; Core üst klasör sırası / döngü kapısı `docs/core-layers.md`. Doğrulama: `tools/verify.ps1`.

**Büyük dosya uyarısı:** `ManifestationDirector` (partial toplamı ~3.9k satır, servislere taşınıyor), `HexagonInput`, `BossDirector`, `PrototypeTuning` — tam dosya okuma; önce bu haritadaki giriş noktasına git, `Select-String` ile daralt. `PrototypeBootstrap` ince kök; kurulum `Game/Composition/Builders/*`.

| Konu | Dosya(lar) | Giriş noktası |
|------|------------|---------------|
| Skill atma — girdi | `Game/Casting/HexagonInput.cs` (orkestratör), `Game/Casting/Input/` (`PointerRouter`, `StrokeCaster`, `CastGate`, `DodgeTrigger`, `CastFeedback`), `Game/Casting/MoveInput.cs`, `Game/Casting/JoystickView.cs` | `HexagonInput.Update` |
| Skill atma — cümle | `Core/Grammar/SentenceEngine.cs`, `Core/Grammar/SentenceState.cs` | `SentenceEngine.OnDotTouched`, `SentenceEngine.Commit`, `SentenceEngine.Tick` |
| Skill atma — çözüm | `Core/Grammar/SkillMotor.cs`, `Game/Data/SkillMotorLoader.cs`, `Game/Data/ElementSystemJsonLoader.cs` | `SkillMotor.Resolve`, `SkillMotor.ResolveWords`, `SkillMotorLoader.Load` |
| Skill atma — yürütme | `App/Casting/CastPipeline.cs`, `Game/Skills/ManifestationDirector.cs`, `Game/Skills/ManifestationDirector.VerbExecution.cs`, `Core/Casting/SkillExecutorRouter.cs`, `Game/Skills/Execution/ISkillExecutor.cs` | `CastPipeline.RunSkill`, `CastPipeline.RunBasic`, `ManifestationDirector.OnSentenceCompleted`, `SkillExecutorRouter.Route` |
| Skill atma — görsel/VFX | `Game/Skills/ManifestationDirector.CastPresentation.cs`, `Game/Skills/Motion/MotionTemplateDriver.cs`, `Game/Skills/Motion/MotionHitResolver.cs`, `Game/Vfx/FeelVfx.cs`, `Game/Vfx/PresentationFx.cs` | `MotionTemplateDriver.TryBeginMotionTemplate`, `ManifestationDirector.Update` |
| Silah pasifleri / küre / top | `Game/Skills/Weapons/WeaponPassiveRuntime.cs`, `Game/Skills/Weapons/OrbController.cs`, `Game/Skills/Weapons/CannonBlast.cs`, `Game/Skills/ManifestationDirector.zWeaponServices.cs` | `WeaponPassiveRuntime.HitMods`, `OrbController.TryPlace`, `CannonBlast.TryCannonBlast` |
| Kalıp teslim kuyruğu | `Game/Skills/Motion/TemplateDeliveryRuntime.cs`, `Game/Skills/ManifestationDirector.zMotionServices.cs` | `TemplateDeliveryRuntime.ArmTemplateDelivery` |
| Mermi silme | `Game/Skills/Projectiles/ProjectileEraser.cs`, `Game/Skills/ManifestationDirector.Projectiles.cs` | `ProjectileEraser.TickProjectileErase`, `ManifestationDirector.BindProjectiles` |
| Rün / cümle grameri | `Core/Element/Rune.cs`, `Core/Element/RuneLoadout.cs`, `Core/Grammar/RuneManager.cs`, `Core/Input/HexagonLayout.cs`, `Core/Input/JumpKind.cs` | `RuneManager.TrySelect`, `SentenceEngine` |
| Skill verisi (JSON) | `docs/element-sistemi.json`, `unity/Assets/Resources/ElementSystem/element-sistemi.json`, `Core/Data/ElementSystemDocument.cs` | `ElementSystemDocument.Parse`, `SkillMotor.FromDocument`, `ElementSystemJsonLoader.TryLoad` |
| JSON mapper'lar | `Core/Boss/BossEncounterMapper.cs`, `Core/Data/VfxBindingMapper.cs`, `Core/Data/ElementSystemHeader.cs`, `Core/Data/ElementSystemDocument.cs`, `Core/Grammar/SkillEngineModifiers.cs` | `BossEncounterMapper.TryParseHud`, `VfxBindingMapper.TryParse`, `ElementSystemHeader.TryParse`, `ElementSystemDocument.Parse` |
| Hareket kalıbı — Core | `Core/Motion/MotionTemplateRunner.cs`, `Core/Motion/MotionTemplateCatalog.cs`, `Core/Motion/PositionOwnership.cs` | `MotionTemplateRunner.Tick`, `PositionOwnership.Prepare` |
| Hareket kalıbı — Game | `Game/Actors/MotionTemplateBody.cs`, `docs/motion-templates.json` (üretim: `tools/build-motion-templates.py`) | `MotionTemplateBody.TickMotion` |
| Hasar / kritik | `Core/Damage/DamagePipeline.cs`, `Core/Damage/CritSystem.cs`, `Core/Damage/ClosingDamageMath.cs` | `DamagePipeline.Resolve` |
| Status | `Core/Status/StatusBoard.cs`, `Core/Status/StatusApplicator.cs`, `Game/Actors/ActorStatus.cs` | `StatusBoard.Tick`, `StatusApplicator.Apply` |
| Dodge — Core | `Core/Dodge/DodgeState.cs`, `Core/Dodge/DodgeChargeBank.cs`, `Core/Dodge/PerfectDodge.cs`, `Core/Dodge/DodgeCancel.cs` | `DodgeState` (durum geçişleri) |
| Dodge — Game | `Game/Actors/DodgeMotion.cs`, `Game/Actors/PlayerDodgeRig.cs`, `Game/Hud/DodgeChargeHud.cs` | `DodgeMotion.Update`, `PlayerDodgeRig.Update` |
| Boss — Core | `Core/Boss/BossVitals.cs`, `Core/Boss/BossAttack.cs`, `Core/Boss/BossPoise.cs`, `Core/Dodge/ExchangeResolver.cs`, `Core/Boss/BossAttackKindPicker.cs` | `ExchangeResolver.Resolve`, `BossVitals` |
| Boss — App | `App/Boss/BossAttackSelector.cs`, `App/Boss/BossBrain.cs`, `App/Boss/BossStrikeResolver.cs`, `App/Boss/BossMechanicStatusMap.cs`, `App/Boss/VolleyPattern.cs`, `App/Boss/BossApproachRules.cs`, `App/Boss/BossPoiseState.cs`, `App/Boss/BossDirectorRules.cs` | `BossAttackSelector.TrySelect`, `BossBrain.Tick`, `BossStrikeResolver.PlayerVolumeHits`, `VolleyPattern.ComputeLayout` |
| Boss — Game | `Game/Boss/BossDirector.cs` (+ `BossDirector.BrainPort.cs`, `BossDirector.Combat.cs`, `BossDirector.Approach.cs`), `Game/Boss/BossStrikeApplier.cs`, `Game/Boss/BossReactor.cs`, `Game/Boss/BossVisual.cs`, `Game/Boss/BossTelegraph.cs`, `Game/Boss/HostileTargets.cs` | `BossDirector.Update`, `BossStrikeApplier` (nested) |
| Silah / ekipman / pasif | `Core/Equipment/EquipmentCatalog.cs`, `Core/Equipment/WeaponPassiveKind.cs`, `Core/Equipment/WeaponSwap.cs`, `Core/Passives/SlotPassiveDirector.cs`, `Game/Weapons/WeaponSO.cs` | `WeaponSwap`, `SlotPassiveDirector`, `WeaponPassiveKind` |
| Silah görselleri / el prop | `Game/Weapons/WeaponVisualRegistry.cs`, `Game/Weapons/WeaponHandProps.cs`, `Game/Weapons/WeaponArchetypeMap.cs`, `Game/Actors/ActorVisual.cs` | `WeaponVisualRegistry` (SO), `WeaponHandProps.Awake` |
| Portal / sınır / takım | `Core/Portal/PortalSystem.cs`, `Core/Border/BorderMode.cs`, `Core/Team/TeamComboSystem.cs`, `App/Team/TeamModifierTable.cs`, `Game/Team/PortalBorderTeamHost.cs` | `PortalSystem.Cast`, `PortalSystem.Tick`, `PortalBorderTeamHost.Update`; portal/takım skill kimliği → `engine.portal_op` / `engine.team_op` |
| Oyuncu can / mana / diriliş | `App/Actors/PlayerHealth.cs`, `Game/Actors/PlayerVitals.cs`, `Game/Actors/PlayerResource.cs`, `Core/Casting/ResourceTracker.cs` | `PlayerVitals.Tick`, `PlayerResource.Update` |
| Kombo soğuma (CD) | `Core/Casting/CooldownTracker.cs`, `Core/Casting/ComboCooldownKey.cs`, `Game/Actors/PlayerCooldown.cs`, `Game/Casting/HexagonInput.cs`, `Game/Skills/ManifestationDirector.cs` | `ComboCooldownKey.For`, `CooldownTracker.TryStart`, `HexagonInput.TryAllowComboCooldownForNextDot`, `ManifestationDirector.ApplyCooldown` |
| HUD | `Game/Hud/VitalsHud.cs`, `Game/Hud/CombatOverlayHud.cs`, `Game/Hud/SkillPreviewHud.cs`, `Game/Hud/PassiveHud.cs`, `Game/Hud/DamageNumberHud.cs` | ilgili `Update` / `Tick*` |
| Kamera | `Game/Cameras/FollowCamera.cs`, `Game/Cameras/CameraOrbitInput.cs`, `Game/Cameras/CameraAmbienceColliders.cs` | `FollowCamera.Update`, `FollowCamera.ResolveYaw` |
| VFX / SFX | `Game/Vfx/VfxLibrary.cs`, `Game/Audio/SfxDirector.cs`, `Game/Audio/SfxLibrary.cs`, `Game/Vfx/HitImpactFx.cs`, `Game/ComposedSkillVfx.cs` | `SfxDirector.Awake`, `VfxLibrary` |
| Tuning | `Game/Config/PrototypeTuning.cs`, `PrototypeTuning.Arena.cs`, `PrototypeTuning.Player.cs`, `PrototypeTuning.Input.cs`, `PrototypeTuning.Camera.cs`, `PrototypeTuning.Visuals.cs`, `PrototypeTuning.Boss.cs`, `PrototypeTuning.Hud.cs`, `Game/Config/TuningConfig.cs`, `Core/Tuning/*.cs` | `PrototypeTuning.EnsureRuntimeDefaults`, `TuningSchema` |
| Sahne kurulumu | `Game/Composition/PrototypeBootstrap.cs`, `Game/Composition/Builders/` (`WorldContext`, `ArenaBuilder`, `ActorsBuilder`, `CameraBuilder`, `HexagonInputBuilder`, `HudBuilder`, `SkillSystemBuilder`, `DebugToolsBuilder`, `VisualAttach`), `Game/Composition/PlaceholderFactory.cs`, `Game/Arena/CircularArena.cs` | `PrototypeBootstrap.Awake` → `BuildWorld` kurucu sırası |
| Debug / dev | `Game/DevTools/DebugConfig.cs`, `Game/DevTools/DebugPanelsController.cs`, `Game/DevTools/V611DebugPanel.cs`, `Game/DevTools/SentenceDebugHud.cs`, `Game/DevTools/TuningPanel.cs` | `DebugPanelsController`, `V611DebugPanel.Update` |
| Editor menüleri | `Game/Editor/PlaySweep.cs`, `Game/Editor/*Bind.cs`, `Game/Editor/AndroidBuilder.cs` | `[MenuItem("Dovus/...")]` |
| Testler — Core | `tools/CoreTests/*Tests.cs` | `dotnet test tools/CoreTests` |
| Testler — Integration | `tools/IntegrationTests/*Tests.cs` | `dotnet test tools/IntegrationTests` |
| Git dışı asset yedeği (Synty/Mixamo/VFX) | `docs/asset-yedegi.md`, `tools/IntegrationTests/known-missing-asset-guids.txt` | yedek zip: PC `C:\Users\lacol\_backup\` |
| Araç — gramer | `tools/AtomSim/Program.cs` | `dotnet run --project tools/AtomSim` |
| Araç — başsız tarama | `tools/SweepV2/Host/Program.cs`, `tools/SweepV2/Shim/*` | `SweepV2.Program.Main` |
| Araç — Game derleme | `tools/GameCompile/check.py`, `tools/GameCompile/GameCompile.csproj` | `python tools/GameCompile/check.py` |
| CI | `.github/workflows/sweep-v2.yml` | workflow `sweep` job |
| Zaman / saat | `Core/Time/TimeDirector.cs`, `App/Time/TimeDirectorClock.cs`, `Game/Composition/GameClock.cs` | `TimeDirector.Tick`, `GameClock.Update`, `GameClock.World` |
| App (Dovus.App) | `App/Time/TimeDirectorClock.cs`, `App/Time/ManualClock.cs`, `App/AGENTS.md` | `TimeDirectorClock.Advance` |
| Platform adaptörleri | `Game/Platform/UnityFrameClock.cs`, `Game/Platform/UnityUnscaledClock.cs`, `Game/Platform/UnityRng.cs` | `UnityFrameClock.Default`, `UnityRng.Default` |
| IClock / IRng (Core) | `Core/Shared/IClock.cs`, `Core/Shared/IRng.cs`, `Core/Shared/CombatRng.cs` | `IClock.DeltaSec`, `CombatRng.Seeded` |
| Mekanik gramer | `Core/Mechanic/MechanicGrammar.cs`, `Core/Mechanic/MechanicRules.cs`, `Game/Skills/ManifestationDirector.MechanicGrammar.cs` | `MechanicGrammar.Compose` |
| Canlı efekt / plan | `Core/Manifestation/LivingEffect.cs`, `Core/Manifestation/SkillWorldPlanner.cs` | `LivingEffect.Tick`, `SkillWorldPlanner.Plan` |
| Hedefleme | `Core/Boss/TargetPicker.cs`, `Core/Casting/TargetingRules.cs`, `Game/Actors/PlayerTargeting.cs` | `PlayerTargeting.Update` |
| Boss mermileri | `Core/Boss/HostileProjectiles.cs`, `Game/Boss/HostileProjectileHost.cs` | `HostileProjectileHost.Update` |
| Animasyon köprüsü | `Core/Presentation/AnimationDatabase.cs`, `Game/Actors/AnimationBridge.cs` | `AnimationDatabase.FromJson` |
| Hexagon UI | `Game/Casting/HexagonView.cs` (partial'lar), `Game/Casting/HexagonLayoutScreen.cs`, `Game/Casting/ElementRadialMenu.cs` | `HexagonView` partial'ları |
| Build seçim ekranı | `Game/Hud/BuildSelectScreen.cs` | `BuildSelectScreen` |
| Doğrulama tek komut | `tools/verify.ps1` | `powershell -File tools/verify.ps1` |

*Not:* `SkillMotionMotor` yalnız saf plan üretir (tür, i-frame, hedef); `SkillMotionDriver` ölü. Skill sırasında konum yalnız `MotionTemplateRunner` + `MotionTemplateBody` (kök `AGENTS.md` §3).
