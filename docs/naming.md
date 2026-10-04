# İsimlendirme (PLAN 2B.12–2B.13)

## Tek dil (A24, PLAN 2B.13)

- **Kod tanımlayıcıları** (tip, metot, alan, yerel değişken, const): **İngilizce**.
- **Yorumlar** Türkçe olabilir.
- **Veri sözcükleri** (JSON anahtarları, fiil/sıfat kimlikleri, `bag_hatti`, `iki_kez` gibi): yalnızca string sabitlerinde; mümkünse konu başına `static class <Konu>Keys` ile toplanır (değerler değişmez). Bilinçli Türkçe serileştirilmiş alanlar `docs/glossary.md` ve `[FormerlySerializedAs]` ile korunur.

# İsimlendirme (PLAN 2B.12)

## MonoBehaviour sonekleri (Game sahne bileşeni)

| Sonek | Rol |
|-------|-----|
| `*Director` | Oyun akışı orkestratörü |
| `*View` | Görsel sunum |
| `*Hud` | UI katmanı |
| `*Host` | Sahneye bağlanan tek örnek kabuk (Core sistem taşıyıcısı) |
| `*Controller` | Girdi / kontrol |

## Saf sınıf sonekleri

| Katman | Sonek | Durum |
|--------|-------|-------|
| App / Core (durumsuz) | `*Rules`, `*Math` | App: `BossDirectorRules`, `BossApproachRules`, … |
| Game (durumlu, MonoBehaviour değil) | `*Runtime`, `*Service` | `WeaponPassiveRuntime`, `TemplateDeliveryRuntime`, … |
| Veri | `*Catalog`, `*Table` | `EquipmentCatalog`, `TeamModifierTable`, … |

## Game `MonoBehaviour` dağılımı (2026-10-04, PLAN 2B.12b)

Toplam **79** bileşen (iç içe yardımcılar dahil); **79 / 79** standart sonekle biter. `NamingConventionTests.Game_MonoBehaviours_UseStandardSuffix` kapısı
yeni bileşenlerde de bunu zorlar.

### 2B.12b yeniden adları (58)

Kural: `git mv` dosya + `.meta` (GUID aynı → sahne/prefab bağı korunur), `<Eski>Defaults` → `<Yeni>Defaults`. Bilinçli olarak DEĞİŞMEYENLER:
GameObject adları (`"AllyDummy"`, `"InkTrail"`, `"TuningPanel"`, `"CombatFeel"`, `"BuildSelectScreen"`, `"ReactionReadout"`, `"CastFlash"`,
`"ElementRadialMenu"`, `"TeamDebugMenu"`), log etiketleri (`[SkillExecutor]`, `[FollowCamera]` — PlaySweep ölçümü `[SkillExecutor]` önekini arar),
`[Header]` metinleri, JSON/CSV içerikleri ve tip adıyla başlayan diğer tipler (`HexagonInputSession`, `SkillExecutorRouter`, `ActorStatusTeamMoveSpeed`…).
`WorldContext` alanları ve `ICastSideEffectsHost` özellikleri tip adıyla birlikte yeniden adlandırıldı (ör. `ctx.PlayerVitalsHost`).

| Eski | Yeni |
|------|------|
| `ActorGrounding` | `ActorGroundingController` |
| `ActorPose` | `ActorPoseView` |
| `ActorStatus` | `ActorStatusHost` |
| `ActorVisual` | `ActorView` |
| `AllyDummy` | `AllyDummyController` |
| `DodgeMotion` | `DodgeMotionController` |
| `FootstepEmitter` | `FootstepView` |
| `KinematicMotor` | `KinematicMotorController` |
| `MotionTemplateBody` | `MotionTemplateBodyHost` |
| `PlayerCooldown` | `PlayerCooldownHost` |
| `PlayerDodgeRig` | `PlayerDodgeController` |
| `PlayerResource` | `PlayerResourceHost` |
| `Targetable` | `TargetableHost` |
| `PlayerTargeting` | `PlayerTargetingController` |
| `PlayerVitals` | `PlayerVitalsHost` |
| `TeamActor` | `TeamActorHost` |
| `AttackTelegraph` | `AttackTelegraphView` |
| `BossHitFlinch` | `BossHitFlinchView` |
| `BossReactor` | `BossReactorController` |
| `BossTelegraph` | `BossTelegraphView` |
| `BossVisual` | `BossView` |
| `HostileTargets` | `HostileTargetsHost` |
| `CameraOrbitInput` | `CameraOrbitController` |
| `FollowCamera` | `FollowCameraController` |
| `ElementRadialMenu` | `ElementRadialMenuHud` |
| `HexagonInput` | `HexagonInputController` |
| `HexagonOverlayCamera` | `HexagonOverlayCameraView` |
| `InkTrail` | `InkTrailView` |
| `MoveInput` | `MoveInputController` |
| `SyllableFeedback` | `SyllableFeedbackView` |
| `GameBootstrap` | `GameBootstrapHost` |
| `GameClock` | `GameClockHost` |
| `DodgePractice` | `DodgePracticeController` |
| `FeelPlayVerify` | `FeelPlayVerifyController` |
| `GrammarDebugPanel` | `GrammarDebugHud` |
| `TeamDebugMenu` | `TeamDebugHud` |
| `TuningPanel` | `TuningPanelHud` |
| `CombatFeel` | `CombatFeelDirector` |
| `VisualFreeze` | `VisualFreezeView` |
| `BuildSelectScreen` | `BuildSelectHud` |
| `ReactionReadout` | `ReactionReadoutHud` |
| `StatusIconStrip` | `StatusIconStripHud` |
| `ComposedSkillVfx` | `ComposedSkillVfxView` |
| `SkillExecutor` | `SkillExecutorController` |
| `AfterimageTrail` | `AfterimageTrailView` |
| `BillboardVfx` | `BillboardVfxView` |
| `CastFlash` | `CastFlashView` |
| `FxTween` | `FxTweenView` |
| `GroundScarField` | `GroundScarFieldView` |
| `HitFlash` | `HitFlashView` |
| `PresentationFx` | `PresentationFxView` |
| `WeaponGripProfile` | `WeaponGripView` |
| `WeaponHandProps` | `WeaponHandPropsView` |
| `WeaponPropIdleMotion` | `WeaponPropIdleView` |
| `HoldSurface` (iç içe, `ElementRadialMenuHud`) | `HoldSurfaceController` |
| `ScreenAnchoredCorner` (iç içe, `TuningPanelHud`) | `ScreenAnchoredCornerView` |
| `Runner` (iç içe, `UiJuice`) | `UiJuiceRunnerHost` |
| `FollowAnchor` (iç içe, `HitboxVfxRegistry`) | `FollowAnchorView` |

**Başka dallar için:** yukarıdaki eski adları kullanan kod birleştirmede yeniden adlandırılmalı (dosya yolları da değişti).

## 2B.12 tip yeniden adları

| Eski | Yeni |
|------|------|
| `PortalBorderTeamHost` | `TeamComboHost` |
| `PortalBorderTeamAccess` | `TeamComboAccess` |
| `PortalBorderTeamDefaults` | `TeamComboDefaults` |
| `PrototypeBootstrap` | `GameBootstrap` (2B.12b: `GameBootstrapHost`) |
| `PrototypeTuning` | `GameTuning` (düz `[Serializable]` alan: Unity alan adıyla bağlar → `MovedFrom` gerekmez; sahne/`tuning.json` alan adları aynı) |
| `V611DebugPanel` | `GrammarDebugPanel` (2B.12b: `GrammarDebugHud`) |

`Weapons10` tip adı Scripts altında yok (2B.2f). `SweepV2` araç adı; kod tipi değil.

Bilinçli olarak DEĞİŞMEYENLER: sahne dosyası `Prototype.unity` (ve içindeki `m_EditorClassIdentifier` satırı — Unity GUID ile bağlar, bir sonraki kayıtta kendisi günceller), serileştirilmiş alanlar `_prototypeMainClassId`/`_prototypePassiveRuneIds`, `Resources/Bosses/*.json` içindeki belge amaçlı `"PrototypeTuning.PlayerMaxHp"` `maps_to` anahtarları (kod okumuyor). Görünür GameObject adları değişti: `TeamComboHost` (eski host adı), `GrammarSimpleControls` (eski `V611SimpleControls`); ada göre arayan kod yok.

**Başka dallar için:** eski tip adlarını (`PrototypeTuning`, `PrototypeBootstrap`, `PortalBorderTeam*`, `V611DebugPanel`) kullanan kod birleştirmede yukarıdaki tabloya göre yeniden adlandırılmalı.

## Yorum kodu sözlüğü

Eski PLAN / PR kısaltmaları; yeni yorumlarda tercih: kısa Türkçe cümle. Anlam belirsizse bu tabloya bak.

| Kod | Anlam | Belge |
|-----|-------|-------|
| O1 | Dodge: süre eşiği vs sürükleme eşiği | `Core/Tuning/DodgeTuning.cs` |
| O2 | Tek mükemmel dodge penceresi (PERFECT) | `DodgeTuning`, `ExchangeResolver` |
| O3 | Portal geri vuruş hasar çift sayım düzeltmesi | `TeamComboHost` |
| O4 | Diriliş zamanlayıcısı dünya saati | `PlayerVitalsHost` |
| O5 | Eski `tuning.json` yedekleme | `TuningConfig` |
| O6 | %50 canla başlama debug bayrağı | `DebugConfig` |
| O7 | Tek kalıcı savaş RNG (kritik/sapma) | `CritSystem`, `ManifestationDirector.Damage` |
| O8 | Yay zırh delme bonusu tüketimi | `ManifestationDirector.Damage` |
| O10 | Kanallı skill sırasında silah swap kilidi | `ManifestationDirector.WeaponSwap` |
| O11 | GetComponent / Find tekrarını önbellekleme | çeşitli Game dosyaları |
| F1 | Stasis → yavaşlatma (oyuncu donması kaldırıldı) | `ManifestationDirector.VerbExecution` |
| T1 | İptal penceresi nokta sayısına bağlı | `SentenceEngine`, `ActorPoseView` |
| T4 | Kalıcı zemin izi / gölgesiz telefon | `GroundScarFieldView`, `CombatAmbienceEnvironment` |
| T8.1 | Tuning sürüm yaması (bilinçli 0 korunur) | `GameTuning` |
| T9–T14 | Tuning/HUD/telemetri sürüm yamaları | `GameTuning.Migrate`, `HudSettings` |
| T10 | Canlı ayar paneli + JSON round-trip | `TuningConfig`, `TuningPanelHud`, Core `*Tuning` |
| T11 | Kare süresi HUD / ground scar cap | `FrameTimeHud`, `GameTuning` |
| T12 | Kapanış hasarı HUD aracı (varsayılan kapalı) | `HudSettings`, `ManifestationDirector` |
| A21–A23 | PLAN 2B.12 isimlendirme maddeleri | `docs/PLAN.md` |
| 2B.x | Mimari alt iş kodu | `docs/PLAN.md` |
