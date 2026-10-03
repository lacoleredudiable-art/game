# Mimari hedef planı (ARCHITECTURE-PLAN)

Kaynak: 2026-10-04 mimari analizi (master `4b8eb94`). İş sırası `docs/PLAN.md` Aşama 2'dedir.
Bu belge **hedefi** anlatır; bugünkü kodun nerede olduğu için `docs/MAP.md`'ye bak.

## Değişmez ilke
- Aşama 2 refaktörleri **davranış değiştirmez**: `dotnet test tools/CoreTests` sayısı ve
  `SweepV2 --all --gate` 1440 sonucu birebir aynı kalır. Aynı kalamıyorsa iş durur, rapor edilir.
- Unity dosyası taşınırken `.meta` ile birlikte taşınır (`git mv` ikisini birden) → GUID korunur.
- `.unity` / `.prefab` elle düzenlenmez (kök `AGENTS.md`).

## Kararlaştırılan hedef
- Katmanlar: Domain / Application / Infrastructure / Presentation / Composition / DevTools / Editor
- Namespace Dovus.<Katman>.<Konu> = klasör
- Application: CastPipeline, BossBrain, CombatWorld/Simulation; komut alır, olay yayar; IClock/IRng
- Presentation olayları dinler; ManifestationDirector olay→görsel eşleyiciye küçülür
- Sonek kuralı: domain soneksiz, …Rules, …Catalog, …Pipeline/…Service, …View/…Hud/…Presenter; sürüm adları kalkar


## Hedef klasör ve katman yapısı
**İlke:**
- Önce **context**, sonra **katman**.
- Domain ve Application saf C# (`noEngineReferences: true`), Unity'ye bağlı olanlar ayrı asmdef'lerde.
- Bağımlılık yönü: Presentation/Infrastructure → Application → Domain.

```
unity/Assets/Scripts/
├─ Domain/                          Dovus.Domain.asmdef        (saf C#, referans: yok)
│  ├─ Shared/        SkillId, WeaponId, RuneId, ElementId, ActorId (value type), GameTime, ICombatRng, Vec2
│  ├─ Casting/       Rune, RuneLoadout, Sentence, Syllable, Closing   (bugünkü Grammar: rün dili)
│  ├─ Skills/        Skill (aggregate), Verb, Adjective, SkillMechanic, Hitbox, SkillCatalog(arayüz)
│  ├─ Elements/      Element (enum/tip), ElementPaint, ElementReaction      ← bugün yok
│  ├─ Combat/        Damage, Crit, Armor, Poise, Status, StatusBoard        (yalnız hasar/status)
│  ├─ Movement/      MotionTemplate, PositionOwnership, Dodge*, Displacement
│  ├─ Equipment/     Weapon, WeaponPassive (enum), WeaponSwap
│  ├─ Boss/          Boss (aggregate), BossAttack, BossPhase, BossBrain, WebField
│  ├─ Actors/        Player (aggregate: Hp, Resource, Cooldown, Status), Ally   ← PlayerVitals/ActorStatus'tan
│  └─ Team/          TeamCombo, Portal, Border, ModifierSet (oyuncu başına) ← PortalBorderTeamHooks'tan
├─ Application/                     Dovus.Application.asmdef   (saf C#, ref: Domain)
│  ├─ Casting/       CastPipeline (MD:1316–1332'nin yeni evi), CastCommand, CastResult/Events
│  ├─ Boss/          BossEncounterService
│  └─ Simulation/    CombatSimulation.Tick(dt, commands) → events (co-op'ta host'ta koşar)
├─ Infrastructure/
│  ├─ Data/          Dovus.Infrastructure.Data.asmdef (saf C#): MiniJson, ElementSystemMapper
│  │                 (JSON → Skill/Weapon/Element tipleri; JsonValue BURADA biter), MotionTemplateMapper
│  └─ Unity/         Dovus.Infrastructure.Unity.asmdef: ResourcesLoader, TuningConfig, PlayerPrefs,
│                    UnityRng/UnityClock adaptörleri, (ileride) Photon/
├─ Presentation/                    Dovus.Presentation.asmdef  (Unity; ref: Application, Domain)
│  ├─ Casting/       HexagonInput (yalnız girdi→CastCommand), HexagonView, InkTrail, SyllableFeedback
│  ├─ Skills/        SkillPresenter (MD'nin kalan görsel kısmı), LivingEffectView, Vfx/
│  ├─ Boss/          BossView, BossTelegraph, BossVisual, BossReactor
│  ├─ Actors/        ActorVisual, ActorPose, KinematicMotor, WeaponHandProps, WeaponGripProfile
│  ├─ Camera/        FollowCamera, CameraOrbitInput
│  ├─ Hud/           VitalsHud, DamageNumberHud, StatusIconStrip, CombatOverlayHud, HudTheme
│  ├─ Environment/   CircularArena, LavaDecor, SceneAtmosphere, CombatAmbience*
│  ├─ Audio/         SfxDirector, SfxLibrary
│  └─ Config/        InputTuning, CameraTuning, HudTuning, ArenaTuning… (PrototypeTuning'in parçaları)
├─ Composition/                     Dovus.Composition.asmdef: GameBootstrap (eski PrototypeBootstrap)
├─ DevTools/                        Dovus.DevTools.asmdef (define: DOVUS_DEBUG): TuningPanel, V611→DebugPanel,
│                                   TeamDebugMenu, FrameTimeHud, DodgePractice, FeelPlayVerify
└─ Editor/                          Dovus.Editor.asmdef: Build/, AssetBinding/ (…Bind), Sweep/ (PlaySweep)
                                    — tek seferlik *Capture*/*Cw* script'leri arşiv dalına
```

**Namespace kuralı:** `Dovus.<Katman>.<Context>`, örneğin `Dovus.Domain.Skills` veya `Dovus.Presentation.Boss`. Klasör ile birebir aynı olur.

**İsim kuralı önerisi:**

| Rol | Sonek |
|---|---|
| Domain nesnesi | sonek yok: `Skill`, `Boss`, `Weapon` |
| Saf kural | `…Rules` (tek sonek; `Math`/`Picker`/`Resolver`/`Policy` buna toplanır) |
| Referans veri | `…Catalog` (tek sonek; `Data`/`Database`/`Library`/`Registry` buna toplanır) |
| Use case | `…Service` / `…Pipeline` |
| Unity bileşeni | `…View` (görsel), `…Hud` (UI), `…Input` (girdi), `…Presenter` (domain → görsel köprüsü) |

Sürüm ve sayı adları (`V611`, `Weapons10`, `Cw3`) tip adlarından kalkar; sürüm bilgisi git'te kalır.

**Taşıma riski ve sırası:**
- Unity dosyalarını `.meta` dosyalarıyla birlikte (`git mv` ikisini birden) taşımak GUID'i korur. Sahne zaten koddan kurulduğu için sahne referansı kırılma riski düşük.
- ScriptableObject asset'leri (`VfxLibrary.asset`, `WeaponVisualRegistry.asset`, `ElementSO`…) script GUID'ine bağlı olduğu için `.meta` korunmalı (tahmin: başka risk yok).
- Önerilen sıra:
  1. Game'i alt klasörlere ayırıp namespace'leri düzeltmek (KÜÇÜK; davranış değişmez).
  2. `Shared` ID value type'ları ile `Element` ve `Skill` tiplerini eklemek, JsonValue'yu mapper'a hapsetmek (ORTA).
  3. Application/CastPipeline'ı Core'a taşımak (BÜYÜK; ana raporda R1).

## Integration testler (Aşama 0.2 ve sonrası)
1. JSON → domain eşleme (144 skill)
2. Asset referans testi (VfxLibrary, WeaponVisualRegistry, proplar)
3. Kayıt/ayar round-trip + sürüm ezme
4. Saat/rastgele adaptör testleri
5. PlayMode sahne smoke (gece/merge öncesi)
6. İleride Photon komut/olay serileştirme
- 1–3 her PR'da CI; 5 sadece gece; az ve hızlı

## Tespit edilen sorunlar (özet, kanıtlar analiz raporunda)
### God class / god config
1. ManifestationDirector: ~7.400 satır, 276 metot, 49 sınıfa bağlı, skill akışı içinde (MD.cs:1316–1332)
2. HexagonInput (1.168): girdi + skill motoru + UI
3. BossDirector (1.154): boss AI Unity tarafında
4. PrototypeBootstrap (922): 77 sınıfa bağlı
5. PrototypeTuning: 243 ayar, 39 dosya bağımlı
6. element-sistemi.json: 231 KB, veri + lore + changelog, 10–12 kez parse
### Katman / izolasyon
7. Game katmansız: 132 dosya tek klasör/namespace; 120 sınıftan 54'ü döngüsel
8. Core içi döngüler: Combat↔Status, Equipment↔Grammar
9. Skill akışı, boss AI, oyuncu canı Unity'de (Core'da değil)
10. Mana Time.deltaTime (PlayerResource.cs:43), diriliş unscaledTime (PlayerVitals.cs:41)
11. Tohumsuz Random (DamagePipeline.cs:25), Random.value (PortalBorderTeamHost.cs:61)
12. Game testleri 6.477 satırlık sahte Unity katmanına (SweepV2) bağımlı
### Anti-pattern
13. Global statik çarpanlar (PortalBorderTeamHooks: 9 dosya / 40 erişim) + 7 singleton
14. Skill ID switch'leri: PortalSystem 20, TeamComboSystem 18; 88 string ID, 234 string case; pasifler string
15. 59 FindAnyObjectByType/Camera.main; KinematicMotor.cs:86 her karede GetComponent
16. Build git'te olmayan dosyalara bağlı: VfxLibrary 28 eksik referans, WeaponVisualRegistry 6/6 anim seti yok
17. Kural/kod çelişkisi (SkillMotionMotor "ölü" deniyor ama çağrılıyor); 4.479 satır capture script; 85 merge edilmiş branch; ~2.900 sabit sayı
18. Hit-stop tüm dünyayı donduruyor (co-op engeli)
### İsimlendirme / klasör
19. Core'da konu + teknik tür karışık (Tuning/Presentation/Execution); Boss ve Element'in klasörü yok; Combat çöplük (47 dosya)
20. Namespace≠klasör (Game tek namespace, SkillExecution, Editor'de 2 namespace)
21. Sonek karmaşası (Catalog/Data/Profile/Loader/Library/Registry/Database; Director/System/Host/Hooks/...); SkillMotor yanıltıcı
22. Çok konulu/geçici adlar: PortalBorderTeam*, Prototype*, TeamActor/AllyDummy/IAllyPlayer
23. Sürüm/sayı adları: Weapons10, V611DebugPanel, FeelCaptureCw3, SweepV2; yorumlarda T8/T10/K1 kodları
24. Türkçe/İngilizce karışık tanımlayıcılar ve veri sözcükleri
25. Dosya adı≠tip (10 dosya), çok tipli dosyalar (PortalSystem 13), yanlış yerde dosyalar (MiniJson, HexagonLayout, TouchButtonGesture, DamageNumberFormat)
### DDD
26. Rune enum'unda eski "element = rün" dili
27. Tek Skill kavramı yok, 6–8 paralel model
28. Kavramlar string (SkillId, element, sıfat "2"); ham JSON Game'e sızıyor (104 erişim)
29. Entity/aggregate yok: oyuncu Core'da yok, hedefler Transform
30. SkillResolution 41 alan (24 string)
31. Anemik model, 85 statik kural sınıfı; skill yan etkileri event değil doğrudan çağrı
32. Repository arayüzü yok, katalog = parser + factory + depo
