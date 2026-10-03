# MAP.md — konu → dosya → giriş noktası

**Nasıl kullanılır:** Görevde bir konu adı geçiyorsa bu tabloda bul; yalnız listelenen dosyayı aç. Giriş noktası, okumaya başlayacağın tip ve metot. Kök `AGENTS.md` sert kurallar; iş sırası `docs/PLAN.md`; hedef yapı `docs/ARCHITECTURE-PLAN.md`. Doğrulama: `tools/verify.ps1`.

**Büyük dosya uyarısı:** `ManifestationDirector` (~7.400 satır, çok sayıda partial), `HexagonInput`, `BossDirector`, `PrototypeBootstrap`, `PrototypeTuning` — tam dosya okuma; önce bu haritadaki giriş noktasına git, `Select-String` ile daralt.

| Konu | Dosya(lar) | Giriş noktası |
|------|------------|---------------|
| Skill atma — girdi | `Game/HexagonInput.cs`, `Game/MoveInput.cs`, `Game/JoystickView.cs` | `HexagonInput.Update` |
| Skill atma — cümle | `Core/Grammar/SentenceEngine.cs`, `Core/Grammar/SentenceState.cs` | `SentenceEngine.OnDotTouched`, `SentenceEngine.Commit`, `SentenceEngine.Tick` |
| Skill atma — çözüm | `Core/Grammar/SkillMotor.cs`, `Game/SkillMotorLoader.cs`, `Game/ElementSystemJsonLoader.cs` | `SkillMotor.Resolve`, `SkillMotor.ResolveWords`, `SkillMotorLoader.Load` |
| Skill atma — yürütme | `Game/ManifestationDirector.cs`, `Game/ManifestationDirector.VerbExecution.cs`, `Core/Execution/SkillExecutorRouter.cs`, `Game/SkillExecution/ISkillExecutor.cs` | `ManifestationDirector.OnSentenceCompleted`, `SkillExecutorRouter.Route` |
| Skill atma — görsel/VFX | `Game/ManifestationDirector.CastPresentation.cs`, `Game/ManifestationDirector.MotionTemplate.cs`, `Game/FeelVfx.cs`, `Game/PresentationFx.cs` | `ManifestationDirector.Update` |
| Rün / cümle grameri | `Core/Grammar/Rune.cs`, `Core/Grammar/RuneLoadout.cs`, `Core/Grammar/RuneManager.cs`, `Core/Grammar/HexagonLayout.cs` | `RuneManager.TrySelect`, `SentenceEngine` |
| Skill verisi (JSON) | `docs/element-sistemi.json`, `unity/Assets/Resources/ElementSystem/element-sistemi.json` | `SkillMotor.FromJson` (Core), `ElementSystemJsonLoader.TryLoad` (Game) |
| Hareket kalıbı — Core | `Core/Motion/MotionTemplateRunner.cs`, `Core/Motion/MotionTemplateCatalog.cs`, `Core/Motion/PositionOwnership.cs` | `MotionTemplateRunner.Tick`, `PositionOwnership.Prepare` |
| Hareket kalıbı — Game | `Game/MotionTemplateBody.cs`, `docs/motion-templates.json` (üretim: `tools/build-motion-templates.py`) | `MotionTemplateBody.TickMotion` |
| Hasar / kritik | `Core/Combat/DamagePipeline.cs`, `Core/Combat/CritSystem.cs`, `Core/Combat/ClosingDamageMath.cs` | `DamagePipeline.Resolve` |
| Status | `Core/Status/StatusBoard.cs`, `Core/Status/StatusApplicator.cs`, `Game/ActorStatus.cs` | `StatusBoard.Tick`, `StatusApplicator.Apply` |
| Dodge — Core | `Core/Combat/DodgeState.cs`, `Core/Combat/DodgeChargeBank.cs`, `Core/Combat/PerfectDodge.cs`, `Core/Combat/DodgeCancel.cs` | `DodgeState` (durum geçişleri) |
| Dodge — Game | `Game/DodgeMotion.cs`, `Game/PlayerDodgeRig.cs`, `Game/DodgeChargeHud.cs` | `DodgeMotion.Update`, `PlayerDodgeRig.Update` |
| Boss — Core | `Core/Combat/BossVitals.cs`, `Core/Combat/BossAttack.cs`, `Core/Combat/BossPoise.cs`, `Core/Combat/ExchangeResolver.cs`, `Core/Combat/BossAttackKindPicker.cs` | `ExchangeResolver.Resolve`, `BossVitals` |
| Boss — Game | `Game/BossDirector.cs`, `Game/BossReactor.cs`, `Game/BossVisual.cs`, `Game/BossTelegraph.cs`, `Game/HostileTargets.cs` | `BossDirector.Update` |
| Silah / ekipman / pasif | `Core/Equipment/EquipmentCatalog.cs`, `Core/Equipment/WeaponSwap.cs`, `Core/Combat/SlotPassiveDirector.cs`, `Game/WeaponSO.cs` | `WeaponSwap`, `SlotPassiveDirector` |
| Silah görselleri / el prop | `Game/WeaponVisualRegistry.cs`, `Game/WeaponHandProps.cs`, `Game/WeaponArchetypeMap.cs`, `Game/ActorVisual.cs` | `WeaponVisualRegistry` (SO), `WeaponHandProps.Awake` |
| Portal / sınır / takım | `Core/Portal/PortalSystem.cs`, `Core/Border/BorderMode.cs`, `Core/Team/TeamComboSystem.cs`, `Game/PortalBorderTeamHost.cs` | `PortalSystem.Cast`, `PortalSystem.Tick`, `PortalBorderTeamHost.Update` |
| Oyuncu can / mana / diriliş | `Game/PlayerVitals.cs`, `Game/PlayerResource.cs`, `Core/Combat/ResourceTracker.cs` | `PlayerVitals.Tick`, `PlayerResource.Update` |
| HUD | `Game/VitalsHud.cs`, `Game/CombatOverlayHud.cs`, `Game/SkillPreviewHud.cs`, `Game/PassiveHud.cs`, `Game/DamageNumberHud.cs` | ilgili `Update` / `Tick*` |
| Kamera | `Game/FollowCamera.cs`, `Game/CameraOrbitInput.cs`, `Game/CameraAmbienceColliders.cs` | `FollowCamera.Update`, `FollowCamera.ResolveYaw` |
| VFX / SFX | `Game/VfxLibrary.cs`, `Game/SfxDirector.cs`, `Game/SfxLibrary.cs`, `Game/HitImpactFx.cs`, `Game/ComposedSkillVfx.cs` | `SfxDirector.Awake`, `VfxLibrary` |
| Tuning | `Game/PrototypeTuning.cs`, `Game/TuningConfig.cs`, `Core/Tuning/*.cs` | `PrototypeTuning.EnsureRuntimeDefaults`, `TuningSchema` |
| Sahne kurulumu | `Game/PrototypeBootstrap.cs`, `Game/PlaceholderFactory.cs`, `Game/CircularArena.cs` | `PrototypeBootstrap.Awake` |
| Debug / dev | `Game/DebugConfig.cs`, `Game/DebugPanelsController.cs`, `Game/V611DebugPanel.cs`, `Game/SentenceDebugHud.cs`, `Game/TuningPanel.cs` | `DebugPanelsController`, `V611DebugPanel.Update` |
| Editor menüleri | `Game/Editor/PlaySweep.cs`, `Game/Editor/FeelCapture*.cs`, `Game/Editor/*Bind.cs`, `Game/Editor/AndroidBuilder.cs` | `[MenuItem("Dovus/...")]` |
| Testler — Core | `tools/CoreTests/*Tests.cs` | `dotnet test tools/CoreTests` |
| Testler — Integration | `tools/IntegrationTests/*Tests.cs` | `dotnet test tools/IntegrationTests` |
| Git dışı asset yedeği (Synty/Mixamo/VFX) | `docs/asset-yedegi.md`, `tools/IntegrationTests/known-missing-asset-guids.txt` | yedek zip: PC `C:\Users\lacol\_backup\` |
| Araç — gramer | `tools/AtomSim/Program.cs` | `dotnet run --project tools/AtomSim` |
| Araç — başsız tarama | `tools/SweepV2/Host/Program.cs`, `tools/SweepV2/Shim/*` | `SweepV2.Program.Main` |
| Araç — Game derleme | `tools/GameCompile/check.py`, `tools/GameCompile/GameCompile.csproj` | `python tools/GameCompile/check.py` |
| CI | `.github/workflows/sweep-v2.yml` | workflow `sweep` job |
| Zaman / saat | `Core/Time/TimeDirector.cs`, `Game/GameClock.cs` | `TimeDirector.Tick`, `GameClock.Update` |
| Mekanik gramer | `Core/Mechanic/MechanicGrammar.cs`, `Core/Mechanic/MechanicRules.cs`, `Game/ManifestationDirector.MechanicGrammar.cs` | `MechanicGrammar.Compose` |
| Canlı efekt / plan | `Core/Manifestation/LivingEffect.cs`, `Core/Manifestation/SkillWorldPlanner.cs` | `LivingEffect.Tick`, `SkillWorldPlanner.Plan` |
| Hedefleme | `Core/Combat/TargetPicker.cs`, `Core/Combat/TargetingRules.cs`, `Game/PlayerTargeting.cs` | `PlayerTargeting.Update` |
| Boss mermileri | `Core/Combat/HostileProjectiles.cs`, `Game/HostileProjectileHost.cs` | `HostileProjectileHost.Update` |
| Animasyon köprüsü | `Core/Presentation/AnimationDatabase.cs`, `Game/AnimationBridge.cs` | `AnimationDatabase.FromJson` |
| Hexagon UI | `Game/HexagonView.cs` (partial'lar), `Game/HexagonLayoutScreen.cs`, `Game/ElementRadialMenu.cs` | `HexagonView` partial'ları |
| Build seçim ekranı | `Game/BuildSelectScreen.cs` | `BuildSelectScreen` |
| Doğrulama tek komut | `tools/verify.ps1` | `powershell -File tools/verify.ps1` |

*Not:* `SkillMotionMotor` / eski hareket sürücüleri ölü; skill sırasında konum yalnız `MotionTemplateRunner` + `MotionTemplateBody` (kök `AGENTS.md` §3).
