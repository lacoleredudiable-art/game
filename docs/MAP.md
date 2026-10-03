# MAP.md — konu → dosya → giriş noktası

**Nasıl kullanılır:** Görevde bir konu adı geçiyorsa bu tabloda bul; yalnız listelenen dosyayı aç. Giriş noktası, okumaya başlayacağın tip ve metot. Kök `AGENTS.md` sert kurallar; iş sırası `docs/PLAN.md`; hedef yapı `docs/ARCHITECTURE-PLAN.md`. Doğrulama: `tools/verify.ps1`.

**Büyük dosya uyarısı:** `ManifestationDirector` (~7.400 satır, çok sayıda partial), `HexagonInput`, `BossDirector`, `PrototypeBootstrap`, `PrototypeTuning` — tam dosya okuma; önce bu haritadaki giriş noktasına git, `Select-String` ile daralt.

| Konu | Dosya(lar) | Giriş noktası |
|------|------------|---------------|
| Skill atma — girdi | `Game/Casting/HexagonInput.cs`, `Game/Casting/MoveInput.cs`, `Game/Casting/JoystickView.cs` | `HexagonInput.Update` |
| Skill atma — cümle | `Core/Grammar/SentenceEngine.cs`, `Core/Grammar/SentenceState.cs` | `SentenceEngine.OnDotTouched`, `SentenceEngine.Commit`, `SentenceEngine.Tick` |
| Skill atma — çözüm | `Core/Grammar/SkillMotor.cs`, `Game/Data/SkillMotorLoader.cs`, `Game/Data/ElementSystemJsonLoader.cs` | `SkillMotor.Resolve`, `SkillMotor.ResolveWords`, `SkillMotorLoader.Load` |
| Skill atma — yürütme | `App/Casting/CastPipeline.cs`, `Game/Skills/ManifestationDirector.cs`, `Game/Skills/ManifestationDirector.VerbExecution.cs`, `Core/Execution/SkillExecutorRouter.cs`, `Game/Skills/Execution/ISkillExecutor.cs` | `CastPipeline.RunSkill`, `CastPipeline.RunBasic`, `ManifestationDirector.OnSentenceCompleted`, `SkillExecutorRouter.Route` |
| Skill atma — görsel/VFX | `Game/Skills/ManifestationDirector.CastPresentation.cs`, `Game/Skills/ManifestationDirector.MotionTemplate.cs`, `Game/Vfx/FeelVfx.cs`, `Game/Vfx/PresentationFx.cs` | `ManifestationDirector.Update` |
| Rün / cümle grameri | `Core/Grammar/Rune.cs`, `Core/Grammar/RuneLoadout.cs`, `Core/Grammar/RuneManager.cs`, `Core/Grammar/HexagonLayout.cs` | `RuneManager.TrySelect`, `SentenceEngine` |
| Skill verisi (JSON) | `docs/element-sistemi.json`, `unity/Assets/Resources/ElementSystem/element-sistemi.json` | `SkillMotor.FromJson` (Core), `ElementSystemJsonLoader.TryLoad` (Game) |
| JSON mapper'lar | `Core/Data/BossEncounterMapper.cs`, `Core/Data/VfxBindingMapper.cs`, `Core/Data/ElementSystemHeader.cs` | `BossEncounterMapper.TryParseHud`, `VfxBindingMapper.TryParse`, `ElementSystemHeader.TryParse` |
| Hareket kalıbı — Core | `Core/Motion/MotionTemplateRunner.cs`, `Core/Motion/MotionTemplateCatalog.cs`, `Core/Motion/PositionOwnership.cs` | `MotionTemplateRunner.Tick`, `PositionOwnership.Prepare` |
| Hareket kalıbı — Game | `Game/Actors/MotionTemplateBody.cs`, `docs/motion-templates.json` (üretim: `tools/build-motion-templates.py`) | `MotionTemplateBody.TickMotion` |
| Hasar / kritik | `Core/Combat/DamagePipeline.cs`, `Core/Combat/CritSystem.cs`, `Core/Combat/ClosingDamageMath.cs` | `DamagePipeline.Resolve` |
| Status | `Core/Status/StatusBoard.cs`, `Core/Status/StatusApplicator.cs`, `Game/Actors/ActorStatus.cs` | `StatusBoard.Tick`, `StatusApplicator.Apply` |
| Dodge — Core | `Core/Combat/DodgeState.cs`, `Core/Combat/DodgeChargeBank.cs`, `Core/Combat/PerfectDodge.cs`, `Core/Combat/DodgeCancel.cs` | `DodgeState` (durum geçişleri) |
| Dodge — Game | `Game/Actors/DodgeMotion.cs`, `Game/Actors/PlayerDodgeRig.cs`, `Game/Hud/DodgeChargeHud.cs` | `DodgeMotion.Update`, `PlayerDodgeRig.Update` |
| Boss — Core | `Core/Combat/BossVitals.cs`, `Core/Combat/BossAttack.cs`, `Core/Combat/BossPoise.cs`, `Core/Combat/ExchangeResolver.cs`, `Core/Combat/BossAttackKindPicker.cs` | `ExchangeResolver.Resolve`, `BossVitals` |
| Boss — Game | `Game/Boss/BossDirector.cs`, `Game/Boss/BossReactor.cs`, `Game/Boss/BossVisual.cs`, `Game/Boss/BossTelegraph.cs`, `Game/Boss/HostileTargets.cs` | `BossDirector.Update` |
| Silah / ekipman / pasif | `Core/Equipment/EquipmentCatalog.cs`, `Core/Equipment/WeaponSwap.cs`, `Core/Combat/SlotPassiveDirector.cs`, `Game/Weapons/WeaponSO.cs` | `WeaponSwap`, `SlotPassiveDirector` |
| Silah görselleri / el prop | `Game/Weapons/WeaponVisualRegistry.cs`, `Game/Weapons/WeaponHandProps.cs`, `Game/Weapons/WeaponArchetypeMap.cs`, `Game/Actors/ActorVisual.cs` | `WeaponVisualRegistry` (SO), `WeaponHandProps.Awake` |
| Portal / sınır / takım | `Core/Portal/PortalSystem.cs`, `Core/Border/BorderMode.cs`, `Core/Team/TeamComboSystem.cs`, `Game/Team/PortalBorderTeamHost.cs` | `PortalSystem.Cast`, `PortalSystem.Tick`, `PortalBorderTeamHost.Update` |
| Oyuncu can / mana / diriliş | `Game/Actors/PlayerVitals.cs`, `Game/Actors/PlayerResource.cs`, `Core/Combat/ResourceTracker.cs` | `PlayerVitals.Tick`, `PlayerResource.Update` |
| HUD | `Game/Hud/VitalsHud.cs`, `Game/Hud/CombatOverlayHud.cs`, `Game/Hud/SkillPreviewHud.cs`, `Game/Hud/PassiveHud.cs`, `Game/Hud/DamageNumberHud.cs` | ilgili `Update` / `Tick*` |
| Kamera | `Game/Cameras/FollowCamera.cs`, `Game/Cameras/CameraOrbitInput.cs`, `Game/Cameras/CameraAmbienceColliders.cs` | `FollowCamera.Update`, `FollowCamera.ResolveYaw` |
| VFX / SFX | `Game/Vfx/VfxLibrary.cs`, `Game/Audio/SfxDirector.cs`, `Game/Audio/SfxLibrary.cs`, `Game/Vfx/HitImpactFx.cs`, `Game/ComposedSkillVfx.cs` | `SfxDirector.Awake`, `VfxLibrary` |
| Tuning | `Game/Config/PrototypeTuning.cs`, `Game/Config/TuningConfig.cs`, `Core/Tuning/*.cs` | `PrototypeTuning.EnsureRuntimeDefaults`, `TuningSchema` |
| Sahne kurulumu | `Game/Composition/PrototypeBootstrap.cs`, `Game/Composition/PlaceholderFactory.cs`, `Game/Arena/CircularArena.cs` | `PrototypeBootstrap.Awake` |
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
| Hedefleme | `Core/Combat/TargetPicker.cs`, `Core/Combat/TargetingRules.cs`, `Game/Actors/PlayerTargeting.cs` | `PlayerTargeting.Update` |
| Boss mermileri | `Core/Combat/HostileProjectiles.cs`, `Game/Boss/HostileProjectileHost.cs` | `HostileProjectileHost.Update` |
| Animasyon köprüsü | `Core/Presentation/AnimationDatabase.cs`, `Game/Actors/AnimationBridge.cs` | `AnimationDatabase.FromJson` |
| Hexagon UI | `Game/Casting/HexagonView.cs` (partial'lar), `Game/Casting/HexagonLayoutScreen.cs`, `Game/Casting/ElementRadialMenu.cs` | `HexagonView` partial'ları |
| Build seçim ekranı | `Game/Hud/BuildSelectScreen.cs` | `BuildSelectScreen` |
| Doğrulama tek komut | `tools/verify.ps1` | `powershell -File tools/verify.ps1` |

*Not:* `SkillMotionMotor` / eski hareket sürücüleri ölü; skill sırasında konum yalnız `MotionTemplateRunner` + `MotionTemplateBody` (kök `AGENTS.md` §3).
