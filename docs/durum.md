# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Bu dosyanın tek amacı, sıradaki
> ajanın repoyu taramadan nerede kaldığımızı anlaması. Kısa tut: ne bitti, ne üretildi,
> nerede sapma var.

**Son güncelleme:** 22 Ağustos 2026 · **Sıradaki görev:** T10 (oyun içi ayar paneli)

> **T9 + T9.1 `master`'a girdi.** T10 normal şekilde `master`'dan dallanır. T10'un bilmesi
> gereken hazırlık notları için aşağıdaki "T9.1" bölümünün sonuna bak.

## Görev durumu

| Görev | Konu | Durum | PR |
|---|---|---|---|
| T0 | Unity 6 URP projesi, MCP, gitignore | bitti | — |
| T1 | Core assembly, ayar veri modeli, dotnet test kancası | bitti | master |
| T2 | Cümle gramer motoru | bitti | #2, master'a girdi |
| T3 | Dodge, boss frame verisi, derecelendirme | bitti | task/t3-dodge-boss-exchange |
| T4 | Zaman yönetmeni (yavaş çekim + hitstop) | bitti | task/t4-time-director |
| T5 | Bootstrap sahne, kinematik hareket, sanal çubuk | bitti | master |
| T6 | Beşgen girdi yüzeyi, mürekkep izi | bitti | task/t6-pentagon-input → master |
| T6.1 | T6 denetim düzeltmeleri | bitti | aynı dal |
| T7 | Tezahür katmanı (üç rün) | bitti | task/t7-manifestation |
| T7.1 | T7 denetim düzeltmeleri (bağlantı/mantık) | bitti | task/t7.1-tezahur-bagi |
| T7.2 | T7 denetim düzeltmeleri (görünüm katmanı) | bitti | task/t7.2-tezahur-gorunum |
| T7.3 | Bootstrap wiring (paylaşılan tuning, collider yok) | bitti | task/t7.3-bootstrap-wiring |
| T7.4 | Yerleşik mesh adı regresyonu (ölçek) | bitti | aynı dal → master |
| T6.2 | Düz vuruş, dodge düğmesi, toparlanma kilidi | bitti | task/t6.2-duz-vurus |
| T8 | Boss telegrafı, sıyırma, yavaş çekim, kamera | bitti | task/t8-boss-telegraph |
| T8.1 | T8 denetim düzeltmeleri | bitti | task/t8.1-denetim-duzeltmeleri |
| T8.2 | Yavaş çekim süresi (§7 ödülü gerçek oldu) | bitti | task/t8.2-yavas-cekim-suresi |
| T9 | HUD, parlak tepki yazısı | bitti | [#4](https://github.com/lacoleredudiable-art/game/pull/4) → master |
| T9.1 | T9 denetim düzeltmeleri (bant taşması, overdraw, dp) | bitti | aynı dal → master |
| T10 | Oyun içi ayar paneli | bekliyor | — |
| T11 | Android build, his turu | bekliyor | — |

Durum değerleri: `bekliyor` · `sürüyor` · `bitti` · `bloke`

## T6.2 kararı (22 Ağustos — uygulandı, bkz. "T6.2 — Düz vuruş" bölümü)

Sahibiyle karara bağlandı; **yeniden tartışılmayacak**, görev metni
`docs/gorev-listesi.md`'de (T8'den önce çalıştırılır):

1. **Düz vuruş yoktu, eklendi.** Beşgenin merkezi artık düz vuruş; cümle kurulurken aynı
   tıklama **erken kapanış** (öder, iptal etmez). Altıncı rün değil — merkez kelime değil
   düğme; ekonomisi §5'in 1 nokta satırından gelir.
2. **Dodge merkezden çıktı**, beşgenin dışında ekrana sabit düğmeye taşındı. Sol çubuğun
   yanına konmadı: çubuk dinamik olduğu için sabit yeri olmaz. Cümle sürerken hâlâ Abort.
3. **Toparlanma artık girdi kilidi** (eskiden sadece pozdu). Düz vuruş, yeni fiil ve dodge
   kilidi keser — kısa cümle + araya düz vuruş dokumasının ödülü budur. Ödül **kesilen
   süredir**, çarpan değil (§12'ye satır eklendi).

Spec güncellendi: `docs/dovus-sistemi.md` §1, §2 (girdi tablosu + iki gerekçe maddesi),
§5 ("Düz vuruş ve erken kapanış" + "Toparlanma girdi kilididir"), §12 (yeni tuzak satırı).

**Model düzeni** (`docs/gorev-listesi.md` "Hangi görev hangi modelle"): Core/gramere dokunan
T6.2 ve T8 Opus; yüzey görevleri T9/T10 Sonnet; mekanik işler Composer; denetim turları yazan
modelden farklı bir modelle (Grok) ve denetçi kod yazmaz.

## Üretilen API yüzeyi

Sonraki ajanın bilmesi gereken tipler ve imzalar. Görev bitince buraya **kısa** ekleme yap —
dosya listesi değil, çağrılacak şeyin adı ve ne yaptığı.

### T1 — `Dovus.Core.Tuning` (namespace)

- `CombatTuning` — hepsini toplar: `.Dodge`, `.Sentence`, `.Slowmo`, `.Boss`, `.Grade`, `.Feel`.
  Parametresiz kurulunca varsayılanlar spec'ten gelir.
- `DodgeTuning` — `StartupMs`, `IframeStartMs`, `IframeMs`, `DistanceM`, `DurationMs`,
  `CurveExp`, `GlideTailMs`, `CooldownMs`, `TapMaxMs`, `TapMaxMoveDp`
- `SentenceTuning` — `MaxSentenceDots`, `CancelWindowMs[]` (dizi: 420/360/300),
  `DwellMs`, `DwellMaxStacks`, `Steps[]`.
  Yardımcılar: `StepForDots(int)` ve `CancelWindowForDots(int)` (yuva dolduysa 0 döner).
- `SentenceStep` — `DurationSec`, `TotalEffect`, `RecoverySec` + türetilen `EffectPerSecond`.
- `SlowmoTuning` — `Factor`, `RampDownMs`, `HoldMs`, `RampUpMs`, `AudioLowpassHz`,
  `SlowmoMinGrade`, `SlowmoBonusDots`
- `BossTuning` — `WindupMs`, `ActiveMs`, `RecoveryMs`, `RadiusM`, `Damage`, `IdleMinMs`,
  `IdleMaxMs`, `ApproachSpeedMps`, `RespawnMaxSec`
- `GradeTuning` — `MukemmelGapMaxMs` 90, `HarikaGapMaxMs` 160, `TemizGapMaxMs` 220.
  Değişmez: son eşik `DodgeTuning.IframeMs`'den küçük kalmalı (yoksa SIYIRDI üretilemez)
- `FeelTuning` — hitstop/impact frame/sessizlik/kamera/afterimage + tepki yazısı ayarları
- `DodgeGrade` (enum) — `Mukemmel`, `Harika`, `Temiz`, `Siyirdi`

**Test:** `cd tools/CoreTests && dotnet test`. `.csproj` Core'un tamamını joker ile link'ler.

### T2 — `Dovus.Core.Grammar` (namespace)

- `Rune` (1–5) + `RuneInfo.Syllable` / `TryFromDot`
- `PentagonLayout.ClassifyJump(from, to)` → `JumpKind` (Short / Long / Repeat); komşu = ±1, uzak = ±2
- `SentenceEngine(SentenceTuning?)` — `OnDotTouched(dot, worldTimeMs)`, `OnDwell(worldTimeMs)`,
  `Tick(dtMs)`, `Abort()`; `State` (`SentenceState`), `History`, `SentenceCompleted`
- Max 4 noktada kapanış üretilir; fazla dokunuş yeni fiil başlatır. Abort → `Closing == null`
- Pencere/ödül: `SentenceTuning.CancelWindowForDots` / `StepForDots` üzerinden (T1 API)
- `OnDwell` iptal penceresini **dondurur** (yığın başına `DwellMs` iade). Bekleme eşiğini
  motor ölçmez — parmağın 220 ms'yi doldurduğunu girdi katmanı (T6) bildirir.

### T3 — `Dovus.Core.Combat` (namespace)

- `DodgeState(DodgeTuning?)` — `Begin(pressTimeMs)`, `IsInvulnerable(worldTimeMs)`,
  `EvaluateCurve(u)` → s(u)=1-(1-u)^curveExp, `GetDisplacementRatio(worldTimeMs)`,
  `GetGlideVelocityRatio(worldTimeMs)`, `IsOnCooldown`, `IframeStartMs`/`IframeEndMs`
- `BossAttack(BossTuning?)` — `WindupMs`/`ActiveMs`/`RecoveryMs`, `StrikeTimeMs(telegraphStart)`,
  `IsInEffectVolume(distanceM, angleFromForwardDeg, arcHalfAngleDeg=180)`
- `ExchangeResolver(CombatTuning?)` — `Resolve(ExchangeInput)` → `ExchangeResult`
  (Outcome Dodged/Hit/Safe, Grade, GapMs, ReactionMs, Reason + `HitReasonText`)
- `GradeFromGap(gapMs)` — eşikler T1 `GradeTuning`'den (110/200/320 ms, üst sınır dahil)
- i-frame yarı-açık aralık: `[press+iframeStart, press+iframeStart+iframe)`

**Test:** `CombatExchangeTests` — 18 test; toplam `dotnet test` 30 yeşil.

### T4 — `Dovus.Core.Time` (namespace)

- `TimeDirector(SlowmoTuning?)` — `RealTimeMs`, `WorldTimeMs`, `TimeScale`, `IsHitstopActive`,
  `IsSlowmoActive`
- `Tick(realDtMs)` → ölçeklenmiş dünya deltası; gerçek saat her zaman `realDt` ile ilerler
- `TriggerSlowmo(factor?, rampDown?, hold?, rampUp?)` — varsayılanlar `SlowmoTuning`'den (§7).
  Aktif yavaş çekim varsa baştan başlar; hitstop sırasında kuyruğa alınır
- `TriggerHitstop(ms)` — ölçek ~0; yavaş çekimi duraklatır ve bitince kaldığı yerden sürdürür.
  Üst üste gelirse süreler toplanır
- Rampa geçişleri `SmoothStep` ease; iniş 55 / tut 190 / çıkış 420 ms

**Test:** `TimeDirectorTests` — 8 test; toplam `dotnet test` 38 yeşil.

### T5 — `Dovus.Game` (namespace, Unity kabuğu)

- `PrototypeBootstrap` — Awake'te arena + oyuncu/boss kapsülü + güneş + kamera kurar; sahne kökünde tek script.
  Kapsül ölçüleri sabit: oyuncu r=0.5 h=2.0, boss r=0.85 h=2.6
- `GameClock` — `TimeDirector.Tick(unscaledDelta)`; `WorldDeltaMs` / `RealDeltaMs` okunur.
  `[DefaultExecutionOrder(-1000)]`: saati okuyan her davranıştan önce ilerler
- `KinematicMotor` — Rigidbody yok; `MoveInput` yönü × `Tuning.WalkSpeedMps` × ölçeklenmiş dt.
  Konum arena kenarına kırpılır (`ArenaHalfSizeM − BodyRadiusM`)
- `MoveInput` — sol yarı dinamik sanal çubuk (`EnhancedTouch`); masaüstü WASD yedeği; sağ yarı dokunuşlara dokunmaz
- `FollowCamera` — yumuşak takip + hafif look-ahead; `AddShake(amplitudeM, durationSec)` T8 için
- `PrototypeTuning` — yürüme hızı 4.5 m/s, arena yarım kenarı 12 m, çubuk 72 dp (spec'te yok, varsayılan)
  + §10 renkleri + T6 beşgen alanları. **Tek örnek**: Bootstrap kendi `_tuning`'ini paylaşır
- Editor: **Dovus → Create Prototype Scene** (`PrototypeSceneCreator`) — `Assets/Scenes/Prototype.unity` + build settings

**Unity:** Play mode doğrulandı — arena, oyuncu/boss kapsülü, takip kamerası, WASD hareketi, çok parmak.
Sahne: `Assets/Scenes/Prototype.unity` (tek Bootstrap objesi). URP renderer düzeltildi.

### T6 / T6.1 — `Dovus.Game` beşgen girdi

- `PentagonInput` — çizim yarısı `IsRightHalf` (ayna ile); `SentenceEngine` + `DodgeState`.
  Merkez tap: `DodgeTuning.TapMaxMs`/`TapMaxMoveDp`; aşımı çizim. Dodge → `Abort` + `Begin`
  (cooldown'daysa hiçbiri yok, HUD yazmaz). `Canceled` dokunuş tap sayılmaz.
  Dwell: **dünya zamanı** (`WorldDeltaMs`) ile `DwellMs` birikir → `OnDwell`.
  Kapalı nokta (`PrototypeTuning.IsDotOpen`) motora/ses/mürekkep/titreşime gitmez.
  Masaüstü: fare + Space. `Engine` / `Dodge` / `Combat` public.
- `MoveInput` — çubuk yarısı `!IsRightHalf` (T6.1: aynada çift sahiplik kapandı)
- `PentagonLayoutScreen` — ekran-piksel merkez/nokta/hit; `IsRightHalf`
- `PentagonView` — Overlay canvas; kapalı nokta soluk (alpha × 0.28)
- `InkTrail` + `PentagonOverlayCamera` — LineRenderer mürekkep, URP overlay stack
- `SyllableFeedback` — hece adı `RuneInfo.Syllable`; frekanslar yerelde.
  Titreşim: Android `VibrationEffect.createOneShot` (`DotVibrationMs`, varsayılan 30)
- `SentenceDebugHud` — fiil + sıfat debug metni
- `PrototypeTuning` — `OpenDot1..5` (§4: 1/2/5 açık), `DotVibrationMs`

**Unity:** derleme OK, konsol temiz. `dotnet test` 46 yeşil.

### T7 — Tezahür (`Dovus.Core.Manifestation` + `Dovus.Game`)

- `ManifestationTuning` — `CombatTuning.Manifestation`; hız/yarıçap/silüet adımları (§8'de yok,
  varsayılan; T11 his). T7.1'de Core/Game'e gömülü his sayıları (sönme/bang/pierce/koridor/
  sıfat sabitleri) da buraya taşındı — bkz. T7.1 bölümü.
- `EffectSilhouette` — Focus / Pierce / Spread / Lift (sayı çarpanı değil).
- `SilhouetteBuilder.FromWords` — fiil tohumu + sıfat eksenleri; dwell yığını büyütür.
- `LivingEffect` — yaşayan etki: seyahat, silüete morph, Abort / ArmClosing / FireClosingBang.
  T7.1: menzilini bitiren etki artık cümle Building/AwaitingClosing sürerken sönmez (bekler);
  `SetWords` hâlâ Dead/Fading dışında her fazda kabul eder.
- `ManifestationDirector` — `SentenceEngine` → spawn/mutate; recovery + `PostHitSilenceMs` sonra
  kapanış; `ForceSync` / `ActiveLogic` (prob). T7.1: spawn kararı artık `_buildingView` kimliğine
  bağlı (nokta sayısı polling'i değil); kapanışta tam kelime listesi `SetWords` ile iletilir.
- `LivingEffectView` — LineRenderer halka→yay→hat + az küre (sürü) + iğne kapsülü.
- `ActorPose` — rün squash/stretch + toparlanma nefesi (T1/T5).
- `BossReactor` — knockback / lift / pin (hasar yok).
- `GroundScarField` — kalıcı çatlak/iğne/sürü/asit izi (§10 yeşil yalnızca asit).
- `PrimitiveMesh.Get(PrimitiveType)` — yerleşik mesh'in tek kaynağı (T7.4). Yeni bir küp/küre/
  kapsül gerekirse `GameObject.CreatePrimitive` **değil** bunu çağır: collider doğmaz ve ölçü
  `CreatePrimitive`'le aynı kalır.
- Kapalı rün geri bildirimi **yeniden yazılmadı** (T6.1).

**Test:** `SilhouetteBuilderTests` 6; toplam `dotnet test` 52 yeşil (T7.1 sonrası 57, bkz. altta).
**Unity play:** `5→5-1` focus toplanıyor (aynı LivingEffect); `5-1-2` spread artıyor;
  Abort → kapanış yok; çözülünce AwaitingClosing → bang + scar.

### T7.1 — T7 denetim düzeltmeleri (bağlantı katmanı)

Denetimde bulunan 5 bağlantı/mantık hatası düzeltildi (Core/Grammar ve görünüm dosyalarına
dokunulmadı):

1. **4. sıfat silüete hiç işlemiyordu.** `SentenceEngine` 4. noktada cümleyi dokunuş anında
   çözüyor, `SyncFromSentence` ise `Building` dışında hiç çalışmadığı için son sıfat asla
   `LivingEffect.SetWords`'e ulaşmıyordu. Şimdi `OnSentenceCompleted`, kapanış kurulmadan önce
   `sentence.Words` (tam liste) ile `SetWords` çağırıyor — `LivingEffect.SetWords` `AwaitingClosing`
   fazında da kabul ettiği için sıra önemli değil.
2. **Menzilini bitiren etki cümle kapanmadan sessizce sönüyordu.** `LivingEffect.Tick`, `Traveling`
   fazında menzil dolunca `BeginFade()` çağırıyordu; `ArmClosing` ise `Fading` fazında hiçbir şey
   yapmadan dönüyordu — bang/iz/boss tepkisi hiç gelmiyordu (İĞNE 0.75 sn'de menzili bitirir, 4
   noktalı cümle §5'e göre en az 1.2 sn+ sürer). Artık `Traveling`/`AwaitingClosing` sürerken menzil
   dolması etkiyi öldürmüyor, ucunda bekliyor/sürüyor; sönme yalnızca `Abort`'ta ve `Banging`
   sonrasında olur. Güvenlik payı: `ManifestationTuning.MaxHoldPastRangeSec` (bkz. sapmalar).
3. **Aynı karede iki nokta kaydedilirse cümle hiç doğmuyordu.** Spawn kararı `count==1` pollingine
   bağlıydı; EnhancedTouch bir karede birden fazla `onFingerMove` verince sayı 0→2 sıçrayıp spawn'ı
   hiç tetiklemeyebiliyordu (T6.1'deki `Canceled` hatasıyla aynı sınıf; fare ile üretilemez, telefonda
   üretilir). `ManifestationDirector` artık `_buildingView` adlı kimlik referansı tutuyor: `Building`
   fazında elde yaşayan (Dead/Fading olmayan) bir etki yoksa spawn eder, varsa `SetWords` çağırır.
   `_buildingView`, her `OnSentenceCompleted`'da (Abort ya da Resolved) `null`'a dönüyor.
4. **`view.Scarred` iki işi birden yapıyordu.** Seyahat çatlağı (odaklı SARSINTI) damgalanınca
   `StampScar` `IsViewScarred` yüzünden erken dönüyor, kapanışın kendi izini hiç bırakmıyordu
   (`5-1` gibi cümlelerde). İki bayrak ayrıldı: seyahat izi hâlâ `view.Scarred` (görünüm dosyasına
   dokunulmadı); kapanış izi artık Director'ın kendi `HashSet<LivingEffect> _closingStamped`'inde.
5. **Konsol 3× CS8632 veriyordu.** `ManifestationDirector.cs`'deki `LivingEffect?` / `LivingEffectView?`
   dönüş tipleri (Game assembly'sinde nullable context yok) kaldırıldı, null kontrolüyle yazıldı.
   Ölü `_lastVerb` alanı ve kullanılmayan `PendingClosing.Fired` de silindi.
6. Core'a gömülü his sayıları `ManifestationTuning`'e taşındı (davranış **aynı** kaldı, yeri
   değişti): `LivingEffect` sönme (`FadeDurationSec`=0.35), bang (`BangDurationSec`=0.45), pierce hız
   katkısı (`PierceSpeedBonus`=0.25), hat yarı genişliği (`WaveCorridorHalfWidthWideM`/`NarrowM`=2.2/0.35);
   `SilhouetteBuilder` odak eşiği (`SuruFocusReduceThreshold`=0.35) ve sıfat sabitleri
   (`SuruFocusReduceAmount`=0.05, `KabukSpreadMultiplier`=0.55, `KabukFocusAdd`=0.12,
   `ZehirSpreadAdd`=0.2).

**Test:** `ManifestationWiringTests` 5 yeni test (4. sıfat hedefe işliyor, `SetWords`
`AwaitingClosing`'te kabul ediliyor, menzil bitince ölmüyor + `ArmClosing` yutulmuyor, `Abort`
menzil sonrasında da kapanışsız, güvenlik payı gerçekten çalışıyor). Toplam `dotnet test` **57
yeşil** (52 + 5).

**Unity play mode (MCP prob, gerçek `SentenceEngine`/`ManifestationDirector` üzerinden):**
- 4 noktalı cümle, her nokta penceresinin sonuna yakın (~%90 dolu pencerede) çizildi → cümle 4
  kelimeyle çözüldü (`lastHistoryDots=4`), kapanış patladı, **2 yeni scar** ve **boss knockback**
  (~0.10 m yer değiştirme) geldi. Düzeltmeden önce bu senaryo (kısıtlı zamanlama) etkiyi
  `Fading`'e düşürüp `ArmClosing`'i yutardı.
- Bir karede senkron iki nokta (`OnDotTouched(5,..); OnDotTouched(1,..)` — Director Update'i araya
  girmeden) → bir kare sonra `ActiveCount=1`, `Verb=Sarsinti` doğru spawn edildi; cümle zaman
  aşımıyla çözüldü, **2 yeni scar** (seyahat çatlağı + kapanış İĞNE izi) — ikisi de geldi
  (düzeltmeden önce ikinci scar hiç gelmezdi).
- Cümle ortasında (3 kelime kurulu) `engine.Abort()` (merkez tap-dodge'ın yaptığı çağrı) →
  `Phase=Aborted`, `LastClosing=null`, **0 yeni scar**, boss hareketi yok (~0). T7'de geçen
  kriter bozulmadı.
- Konsol: play mode boyunca `errorCount=0`, `warningCount=0` (Unity AI Toolkit'in proje koduyla
  ilgisiz, tek seferlik ağ uyarısı hariç — derlemeden önce de vardı, kodumuzla ilgisi yok).

### T7.2 — T7 denetim düzeltmeleri (görünüm katmanı)

Denetimde bulunan 6 görünüm hatası düzeltildi (`Assets/Scripts/Core`'a ve
`ManifestationDirector.cs`'e dokunulmadı):

1. **Sönme hiç görünmüyordu.** `LivingEffectView`/`GroundScarField` materyali
   `Universal Render Pipeline/Unlit` ile kuruyordu — bu shader varsayılan OPAK, `1 - FadeT` ve
   izlerin 0.85 alfası ekrana hiç yansımıyordu. İkisi de artık repodaki doğru desenle aynı:
   `InkTrail.EnsureMaterial`'daki gibi önce `Sprites/Default` denenir (gerçek alfa harman);
   yalnızca o bulunamazsa URP Unlit'e düşülür ve o zaman `_Surface`/`_Blend`/blend modu/render
   queue elle saydama çevrilir. Play mode'da doğrulandı: `Abort()` sonrası materyal alfası
   0.35 sn boyunca 1→0.8→0.6→0.4→0.2 şeklinde kademeli düştü (tek karede kaybolmuyor).
2. **Kapanış patlamasında çizgi genişliği birikiyordu.** `PulseBang` her karede
   `widthMultiplier *= pulse` yapıyordu; `DrawWave` genişliği yalnızca hat/SARSINTI dalında
   yeniden yazıyordu, halka/yay dalında önceki karenin (zaten çarpılmış) değeri kalıyordu —
   birikim 0.45 sn'de genişliği çöktürüyor ya da şişiriyordu. `DrawWave` şimdi HER dalda taban
   genişliği (`_lineBaseWidth`) yeniden hesaplıyor; `PulseBang` üstüne `*=` değil `taban * pulse`
   atıyor. Play mode'da 40 karelik bang boyunca genişlik `[0.048, 0.392]` aralığında kaldı
   (taban ~0.1–0.22, pulse ±80%) — kaybolma ya da patlama yok.
3. **Boss geri tepmesi eski yerine geri kayıyordu.** `BossReactor.Tick` `_home + _offset + shake`
   yazıyordu ve `_offset` üstel olarak sıfıra sönüyordu — itilme görsel bir titremeden ibaretti,
   kalıcı değildi. `React` artık `_home`'u KALICI kaydırıyor (arena kenarına kırpılı); eski
   `_offset`'in yerini `_visualOffset` aldı — bu sadece home aniden değiştiğinde ışınlanmayı
   önleyen bir yumuşatma, asla eski `_home`'a geri dönmüyor. `Home` artık genel (get/set)
   property: T8'in yaklaşma hareketi üstüne binebilir. Play mode'da doğrulandı: bir SARSINTI
   kapanışı sonrası boss `z=5.00 → z=6.08`'e itildi, birkaç yüz kare sonra da tam `6.08`'de
   kaldı (pozisyon == home). Arena kenarına 30 kez art arda büyük itiş uygulanınca konum
   `ArenaHalfSizeM - BodyRadiusM = 11.15`'te tam kırpıldı, aşmadı.
4. **Yerdeki iz sınırsız birikiyordu.** `GroundScarField.Stamp` her damga için yeni
   `GameObject` + `Quad` yaratıyordu, tavan yoktu. Artık `PrototypeTuning.GroundScarCapCount`
   (varsayılan 60 — spec'te sayı yok, T11 kare bütçesi için icat edildi) kadar nesne havuzlanıyor;
   tavan dolunca en eski iz round-robin sırayla yeniden yapılandırılıyor (transform/materyal/ölçek
   güncellenir), yok edip yeniden yaratılmıyor. İz süreye bağlı silinmiyor (§8/T4 — kalıcı kalır).
   İzole testte doğrulandı: tavan 5 iken 12 damga çağrısı sonunda tam 5 nesne kaldı.
5. **Gömülü his sayıları veri oldu (AGENTS kural 3).** `LivingEffectView` çizgi genişlikleri
   (0.18/0.35/0.12/0.22/0.1), silüet eşikleri (0.2/0.35/0.45/0.75) ve küre/iğne ölçekleri;
   `ActorPose` poz süresi (180 ms) ve rün başına squash/stretch vektörleri; `BossReactor`
   yerçekimi (22), geri tepme yumuşatma sönmesi (3.2) ve sarsıntı genlik katsayıları (0.12/0.05,
   + Pin'in 0.04'ü) hepsi `PrototypeTuning`'e taşındı. Değerler AYNI kaldı, yalnızca yeri değişti.
   `ActorPose`/`BossReactor` kendi `PrototypeTuning` alanlarını taşıyor (`[SerializeField]` +
   `Tuning` property, `KinematicMotor` ile aynı desen); T7.3'te Bootstrap paylaşılan
   `_tuning` örneğini ve `BossRadiusM`'yi bağladı.
6. **`GameObject.CreatePrimitive`'in ürettiği collider bir kare yaşıyordu.**
   `LivingEffectView` (küre/iğne) ve `GroundScarField` (quad) artık `MeshFilter`/`MeshRenderer`'ı
   doğrudan kurup `Resources.GetBuiltinResource<Mesh>("Sphere.fbx"/"Capsule.fbx"/"Quad.fbx")`
   ile aynı yerleşik mesh'i atıyor — `CreatePrimitive` hiç çağrılmıyor, collider hiç oluşmuyor
   (`Destroy` edilen bir şey de yok). İzole testte doğrulandı: oluşan hiçbir alt nesnede
   `Collider` bulunmuyor.

**Test:** Core'a dokunulmadığı için `dotnet test` hâlâ **57 yeşil**, değişmedi.

**Unity play mode (MCP prob, gerçek sahne + `BossReactor`/`GroundScarField`/`LivingEffectView`
izole örnekleri + gerçek `SentenceEngine` üzerinden tek bir SARSINTI kapanışı):**

- Sönme: `Abort()` sonrası materyal alfası 5 karede 0.35 sn'lik `FadeDurationSec` boyunca
  düzgün kademeli düştü (yukarıda 1. madde).
- Kapanış patlaması: 40 kare boyunca genişlik `[0.048, 0.392]` bandında sabit kaldı, birikim yok.
- Gerçek sahne + gerçek `SentenceEngine.OnDotTouched(5, ...)` ile tek nokta (SARSINTI) cümlesi:
  pencere kendiliğinden kapandı (`Phase=Resolved`), boss `(0,1.3,5.00)`'dan `(0,1.3,6.08)`'e
  itildi ve **orada kaldı** (`transform.position == Home`), tam 1 yeni scar (`GroundScars`
  child count 1), `ManifestationDirector.ActiveCount=0` (etki kapanıp öldü). Konsol:
  `errorCount=0`; tek uyarı T7.1'de de not edilen, koddan bağımsız AI Toolkit ağ uyarısı.
- Boss knockback + arena kırpma + tavan/recycle + collider yokluğu izole component testleriyle
  de (Play/Edit modunda doğrudan `BossReactor`/`GroundScarField` örnekleri üstünden) ayrıca
  doğrulandı (yukarıdaki maddelerde sayılar var).

### T7.3 — Bootstrap wiring

T7.2'nin dokunamadığı `PrototypeBootstrap.cs` bağlantıları kapandı:

1. **`ActorPose`/`BossReactor` paylaşılan `PrototypeTuning`.** `BuildWorld`'de `pose.Tuning =
   _tuning` → `CaptureBase()` ve `reactor.Tuning = _tuning` → `CaptureHome()` sırası korundu
   (`CaptureHome` arena yarıçapını Tuning'den okur).
2. **`BossReactor.BodyRadiusM` Bootstrap sabitiyle bağlandı** (`reactor.BodyRadiusM = BossRadiusM`,
   `KinematicMotor` deseni).
3. **Arena/oyuncu/boss artık `CreateMeshObject` ile kuruluyor**; `CreatePrimitive` + collider
   `Destroy` kaldırıldı — sahne kökünde hiç collider yok. (Mesh adları yanlış seçilmişti, T7.4
   düzeltti.)

**Test:** `dotnet test` **57 yeşil**. Unity play: collider sayısı 0; `_tuning.ArenaHalfSizeM=6`
   ile boss kırpma sınırı küçülüyor (sonra varsayılan geri alındı).

### T7.4 — Yerleşik mesh adı regresyonu (ölçek)

T7.2/T7.3 `CreatePrimitive`'i kaldırırken mesh'leri `Resources.GetBuiltinResource<Mesh>` ile
adla istedi, ama **öneksiz adlar başka bir mesh setine gidiyor**: `Plane.fbx`/`Capsule.fbx`/
`Sphere.fbx` Maya kaynaklı eski varlıklar. Ölçüldü (aynı editör oturumu):

| İstenen | Öneksiz ad | Ölçüsü | `CreatePrimitive` mesh'i | Ölçüsü |
|---|---|---|---|---|
| Zemin | `Plane.fbx` | 1×0×1 | `New-Plane.fbx` | 10×0×10 |
| Kapsül | `Capsule.fbx` | 2×4×2 | `New-Capsule.fbx` | 1×2×1 |
| Küre | `Sphere.fbx` | 2×2×2 | `New-Sphere.fbx` | 1×1×1 |
| Quad (iz) | `Quad.fbx` | 1×1×0 | `Quad.fbx` | aynı — tek doğru olan buydu |

Sonuç sahnede: arena 24 m yerine **2.4 m**, oyuncu/boss iki kat büyük ve yarısı **yerin
altında** (`altY = −1.00` / `−1.30`), sürü küreleri ve iğne iki kat kalın. Ölçek formüllerinin
hiçbiri yanlış değildi — hepsi `CreatePrimitive` ölçülerini varsayıyordu.

Düzeltme: **`PrimitiveMesh.Get(PrimitiveType)`** (yeni, `Assets/Scripts/Game/PrimitiveMesh.cs`).
Adı tek yerde tutar, tipe göre cache'ler, ad bulunamazsa uyarı basıp mesh'i geçici bir
primitive'den alır (aynı karede `DestroyImmediate` — collider dünyaya karışmaz) ki bir sonraki
Unity sürümünde ad değişirse sahne sessizce görünmez olmasın. `PrototypeBootstrap`,
`LivingEffectView` ve `GroundScarField` artık string yerine bu yardımcıyı çağırıyor;
`CreatePrimitive`'i sahne kurulumunda kimse çağırmıyor.

**Test:** `dotnet test` **57 yeşil** (Core'a dokunulmadı).
**Unity play mode (MCP prob, gerçek sahne):**
- Altı primitive tipinin hepsinde `PrimitiveMesh.Get(t)` ile `CreatePrimitive(t)` mesh'i
  **referans olarak aynı** (`ReferenceEquals=True`).
- Arena `24.00 × 24.00` m (`ArenaHalfSizeM=12` ile tutarlı), üst yüzey y=0.
- Oyuncu `1.00 × 2.00` m, boss `1.70 × 2.60` m; **ikisinin de altı tam y=0** (gömülme yok).
- `COLLIDER sayısı = 0` (kurulumda, cümle sırasında ve kapanışta).
- Gerçek `SentenceEngine` ile SÜRÜ cümlesi: küreler `Sphere` mesh (1 m) × 0.35 ölçek = 0.35 m.
  İĞNE cümlesi: `Capsule` mesh (1×2) × (0.20, 0.53, 0.20) = 0.20 m kalınlık, 1.05 m boy.
- Konsol `errorCount=0`; tek uyarı T7.1/T7.2'de de not edilen, koddan bağımsız AI Toolkit ağ
  uyarısı. Oyun kamerasından alınan karede arena tam boy, aktörler zemine oturuyor.

### T6.2 — Düz vuruş, dodge düğmesi, toparlanma kilidi

Girdi düzeni §1/§2'ye göre değişti: **merkez artık dodge değil, düz vuruş / erken kapanış**;
dodge beşgenin dışında ekrana sabit ayrı bir disk. §5'teki toparlanma artık gerçek bir girdi
kilidi ve kesilebilir.

**Core (`Dovus.Core.Grammar`):**

- `SentencePhase.Recovering` (yeni) — kapanış ödendi, girdi kilitli. **Yalnızca `State.Phase`'in
  anlık değeri**; `CompletedSentence.Phase` kapanışta `Resolved` kalır (geçmiş/ödül okunuyor).
- `SentenceEngine.Commit()` — erken kapanış. Yalnızca `Building` + en az bir kelime varken
  çalışır, gövdesi `ResolveWithClosing()`. `Idle`/`Recovering`'de sessizce hiçbir şey yapmaz.
- Kapanış üreten **her** yol (`Commit`, dördüncü nokta, pencere zaman aşımı) `Recovering`'e girer;
  kalan süre `SentenceTuning.StepForDots(n).RecoverySec` (§5 tablosu: 0.18/0.26/0.38/0.55 sn) ve
  `Tick(dtMs)` ile **dünya zamanında** erir, bitince `Idle`.
- Kesme: `Recovering`'de `OnDotTouched` kilidi sıfırlar ve yeni cümleyi başlatır. `Abort()`
  `Building`'de yatırımı batırır (eskisi gibi), `Recovering`'de yalnızca kilidi keser — `History`
  ve `LastClosing` yerinde kalır, iptal kaydı **eklenmez**.
- `SentenceState.RemainingRecoveryMs` + `IsRecovering` (T9 gösterecek).

**Girdi (`PentagonInput`):**

- Merkez kısa dokunma: `Building` ise `Commit()`; `Idle`/`Recovering` ise düz vuruş —
  `OnDotTouched(Tuning.BasicStrikeDot)` + `Commit()` **aynı karede** (+ hece sesi).
- Dodge iki yerden gelir: (a) beşgenin dışındaki disk, (b) masaüstü `Space`. Disk için **ayrı
  bir parmak yuvası** var: çizim parmağı ekranda dururken ikinci parmak diske basabilir (panik
  dodge). İkinci parmağın eşiği aşan sürüklemesi dodge'u iptal eder (çizim yuvası dolu olduğu
  için çizime dönüşmez); **birinci** parmakla diskten sürükleme normal çizimdir.
- Hit sırası `BeginPointer`'da: dodge diski → merkez → nokta. `HitDot` hem merkezi hem diski
  dışlar.
- `FingerMode.DodgePending` eklendi; tap eşikleri (`TapMaxMs`/`TapMaxMoveDp`) merkez ve disk için
  aynı.

**Yerleşim/görünüm:**

- `PentagonLayoutScreen.DodgeButtonPx` / `DodgeButtonRadiusPx` — ofset beşgen merkezinden dp
  cinsinden, `MirrorForLeftHand` X'i çevirir. Konum **çizim yarısının içine** kırpılıyor: hem
  ekrandan taşmıyor hem de sanal çubuğun yarısına sızmıyor (T6.1 kuralı: bir yarı, bir sahip).
- `PentagonView` diski çiziyor (etiketsiz disk). Merkez §10 camgöbeğine, disk §10 moruna çekildi.
- `PrototypeTuning`: `DodgeButtonOffsetXDp/YDp`, `DodgeButtonRadiusDp`,
  `DodgeButtonScreenMarginDp`, `DodgeButtonColor`, `BasicStrikeDot`; `PentagonCenterXNorm`
  0.78 → 0.72 (disk sağ kenardan taşmasın).
- `SentenceDebugHud`: `NoteDodge(bool)` / `NoteCommit()` / `NoteBasicStrike()`; `Recovering`'de
  kalan kilit ms olarak yazılıyor.

**Tezahür bağı (`ManifestationDirector`):**

- Düz vuruş `OnDotTouched` + `Commit`'i aynı karede çağırdığı için Director `Building` fazını hiç
  görmez ve spawn yutulurdu (T7.1'deki "aynı karede iki nokta" hatasının aynı sınıfı). Artık
  `OnSentenceCompleted`, elde yaşayan bir etki yoksa **kapanış için etkiyi orada doğuruyor** —
  ödenmiş kapanış §8/T2 gereği dünyada yaşamak zorunda.
- `FindFallbackView()` **silindi**: "son canlı etkiyi bul" davranışı, hızlı düz vuruşlarda yeni
  kapanışı önceki cümlenin hâlâ patlayan etkisine bağlıyordu (yanlış hedefe ödeme). Yerine
  yukarıdaki spawn geldi.
- `ActorPose.EndRecovery()` (yeni) — kilit kesilince nefes nefese poz da kesilir; Director
  `State.Phase != Recovering` olduğunda çağırıyor. **Kapanış patlaması kesilmez**, kendi
  zamanlamasıyla (`TickPendingClosings`) gelir (§5).

**Test:** `SentenceRecoveryTests` 9 yeni test; `dotnet test` **66 yeşil** (57 + 9). Mevcut iki
test güncellendi (kapanıştan sonra `State.Phase` artık `Resolved` değil `Recovering`).

**Unity play mode (MCP prob, sanal `Touchscreen` ile gerçek girdi katmanı üzerinden):**

- Merkeze tap (cümle yok) → 1 noktalık ödeme (etki 1.0), kilit **180 ms** (§5), aynı karede
  `ManifestationDirector.ActiveCount` 0 → 1 (spawn yutulmuyor); ~0.3 sn sonra kapanış patladı:
  **1 yeni scar**, etki öldü, kilit kendiliğinden eridi (`Idle`). Boss 7 m uzakta olduğu için
  kapanış menzili dışında kaldı (knockback yok) — beklenen.
- Kilit sürerken 5-1 çizmek → `Building`, kilit 0 (kesildi), pencere 360 ms.
- `Building`'de merkeze tap → **2 nokta ödemesi (2.4)**, Abort değil; ardından 2 noktanın kilidi
  (260 ms) başladı.
- Kilit sürerken diske tap → kilit 0 / `Idle`, `History` büyümedi, `LastClosing` (2.4) durdu,
  dodge gerçekten başladı (cooldown açıldı).
- Kilit sürerken merkeze tap → yeni düz vuruş + kilit yeniden 180 ms (kesme becerisi).
- Merkezden ve diskten sürükleme → çizim; ikisinde de vuruş/dodge tetiklenmedi.
- Çizim parmağı basılıyken ikinci parmakla diske tap → cümle `Aborted`/ödenmedi, dodge başladı,
  sol çubuk yönü (0,0) — yarı sahipliği bozulmadı.
- Sol yarıda çubuk + sağ yarıda çizim aynı anda: çubuk (0.93, 0.31), cümle 2 kelime.
- Yerleşim (979×495 game view): disk merkezden 145 px (beşgen yarıçapı 90 px → dışında), sağ
  kenara 171 px, alta 41 px boşluk. Merkez `#5FF0FF`, disk `#B98CFF`; hiçbir oyuncu ögesinde
  kırmızı-turuncu yok. Ekran görüntüsünde kapalı rünler (3/4) hâlâ soluk.
- Konsol: `errorCount=0`; tek uyarı T7.1/T7.2'de de not edilen, koddan bağımsız AI Toolkit ağ
  uyarısı.

> **Sonraki ajana (Unity editörü açıkken ölçüm alacak):** açık sahnenin bellekteki hâli, assembly
> reload'dan sonra **eski alan değerlerini korur** — `PrototypeTuning`'de bir varsayılanı
> değiştirdiysen play mode hâlâ eski değeri gösterir. **Yeni eklenen int/float alanlar da
> 0 gelebilir** (C# initializer deserialize'da uygulanmaz) — T8 `EnsureT8Defaults()` bunu
> `PlayerMaxHp` / pencere ipucu / telegraf renkleri için yamalar. Ölçümden önce sahneyi
> diskten yeniden aç (`EditorSceneManager.OpenScene(path, OpenSceneMode.Single)`), sonra play.

## T8 — Boss telegrafı, sıyırma, yavaş çekim, kamera

Sahiplenilen açıklar ve boss döngüsü.

**Core (`Dovus.Core.Grammar`):**

- `SentenceEngine.OnDotTouched` / `OnDwell` artık `worldTimeMs` ile `CatchUp` yapıyor;
  pencere Tick'i beklemeden erir (~16 ms kare yuvarlaması kapandı). `Tick(dt)` aynı saate
  hizalanır, çift sayım yok.
- `SentenceState.ArmedWindowMs` — pencere ipucu oranı için (T9 da okuyabilir).

**Dövüş döngüsü (`Dovus.Game`):**

- `BossDirector` — idle'da `BossReactor.Home`'a 2.2 m/s yaklaşır (transform'a yazılmaz).
  YERE ÇAKMA: windup 640 / active 90 / recovery 720 / radius 5.4. Vuruş aktifin başında
  tek karede `ExchangeResolver`. Bu telegraftan önceki dodge press'i yok sayılır
  (eski basış "erken bastın" üretmesin).
- `BossTelegraph` — yer diski §10 `#FF4D24`/`#FF9A3C`, hazırlık squash/stretch, yükselen
  sinüs ton (220 Hz, perde 0.55→1.8).
- `DodgeMotion` — `GetDisplacementRatio` + glide artık hızı; yön son hareket, yoksa
  bossun tersi. `KinematicMotor` kayma sırasında durur.
- `AfterimageTrail` — FeelTuning.AfterimageCount/LifeMs, oyuncu rengi, ölçeklenmemiş ömür.
- `CombatFeel` — sıyırmada hitstop + `TriggerSlowmo` (`grade <= Temiz`) + kamera yumruğu
  (FOV/roll/sarsıntı, unscaled sönme) + 33 ms impact frame. Vurulmada kırmızı vinyet +
  `HitstopPlayerHitMs`. Yavaş çekimde listener lowpass 700 Hz. Ekran katmanı Overlay
  kamerada `sortingOrder=200`.
- `PlayerVitals` — tavan `PlayerMaxHp` (22, spec'te yok); ölümde ≤2 sn gerçek saatte
  spawn'a dönüş. Ölünce boss `Home` doğuşa çekilir (spawn üstünde zincir ölüm olmasın).
- `FollowCamera.Punch(fovKick, rollDeg, shakePx, decay)`
- `GameClock.Bind(SlowmoTuning)` — paylaşılan `CombatTuning.Slowmo`

**Katmanlama / pencere ipucu:**

- `PentagonView` artık `ScreenSpaceOverlay` değil, Overlay kamera üzerinde
  `ScreenSpaceCamera` (sort 50). Telegraf dünyada + Feel tehdit flaşı üstte — tek
  Overlay-kamera mekanizması (§10).
- `LivingEffectView.SetWindowCue`: kalan pencere × `Travel/MaxRange` nabız
  (camgöbeği/mor, kırmızı yok).

**Test:** `SentenceWindowPrecisionTests` 4 + `SlowmoSentenceFitTests` 2;
`dotnet test` **72 yeşil** (66 + 6). 350 ms gerçek boşlukla normalde 3 nokta,
0.22× yavaş çekimde 4 nokta.

**Unity play mode (MCP):**

- Derleme temiz, collider 0, canvas `ScreenSpaceCamera` sort 50.
- Boss yaklaşır, telegraf/çakma çözülür: vurulma `geç kaldın` / erken dodge
  `erken bastın` debug HUD'a yazılıyor.
- Ölüm → 2 sn → HP 22, boss eve dönüyor.
- `EnsureT8Defaults` olmasa serileşmiş `PlayerMaxHp=0` hemen öldürüyordu.
- Konsol: `errorCount=0`; tek uyarı kod dışı AI Toolkit ağ uyarısı.

**Play'de tam doğrulanamadı (T11 / denetim):** telegrafı okuyup tam zamanında
dodge → yavaş çekim + yer değiştirme + afterimage. MCP ile kare-içi zamanlama
kaçtı; Core yavaş çekim 4-nokta testi yeşil. Dodge kayması `DodgeMotion.IsBound`
üzerinden bir kez daha bakılmalı.

> **Denetim notu:** yukarıdaki play mode gözlemleri son commit'ten (8e8de05) ÖNCEsine ait.
> O commit'teki `DodgeMotion` "kare takılırsa" yaması derlemeyi kırdı — bkz. T8 denetimi 1.

## T8 denetimi (22 Ağustos) — T8.1'de kapatıldı

> **Bu bölüm artık tarih.** 22 maddenin hepsinin akıbeti için aşağıdaki
> "T8.1 — denetim düzeltmeleri" bölümüne bak. Maddelerin gerekçeleri burada duruyor
> çünkü T8.1'in neden öyle çözdüğünü açıklayan tek yer bu.

`dotnet test` **72 yeşil**, ama Unity Game assembly'si **derlenmiyor**. Aşağıdaki maddeler
T8.1'in görev listesi. Dal `master`'a **birleştirilmedi**, PR açık: 1. madde bloke ediyor.

### Bloke

1. **Dal Unity'de derlenmiyor — `DodgeMotion.cs:92` CS0136.** `Update()` içinde `pos` hem
   `if (!_dodge.IsActive(...))` bloğunda hem de dış kapsamda tanımlı; C# bunu yasaklıyor.
   Hata commit 8e8de05'in içinde, çalışma ağacı temiz. Konsol: *"All compiler errors have to
   be fixed before you can enter playmode!"* — yani T8 kabul kriterlerinin **hiçbiri** bu
   commit'le play mode'da doğrulanamaz.
   **Süreç açığı:** `dotnet test` bunu asla yakalayamaz, çünkü `tools/CoreTests` yalnızca
   `Assets/Scripts/Core`'u link'liyor. "Yeşil test = birleştir" kuralı Unity katmanı için
   yetersiz; T8.1 dalı kapatmadan önce editörde derlemeyi gözle görmeli.

### Mekanik hatalar

2. **Glide kuyruğu her dodge'un sonunda geri emiliyor (§6 "yağ gibi kayma" ölü).**
   `_dodge.IsActive` 500 ms'de kapanıyor; o karede fallback `pos = _startPos + _dir * 3.8`
   yazıp `_glideExtra`'yı (≈**1.07 m**) atıyor. Yani her dodge, kayma bittiği anda ~1 m
   **geriye zıplıyor**. Fallback kare atlamaları için eklenmişti ama her dodge'un normal son
   karesinde de çalışıyor.
3. **Glide artık hızı yoktan doğuyor.** Eğri `s(u)=1-(1-u)^3.2` türevi u=1'de **0** — ana
   hareket sıfır hızla bitiyor. Glide sonra `charSpeed = 3.8/0.26 = 14.6 m/s`'i sıfırdan
   enjekte ediyor: 280. ms'de 14.6 m/s'lik hız süreksizliği, "yumuşak sönüş" değil ikinci bir
   sıçrama. `charSpeed` uydurma; §6 artık hızın büyüklüğünü vermiyor.
4. **Telegraf diski bossun ölçeğini miras alıyor — yarıçapı yanlış gösteriyor.** `SlamDisc`
   boss transform'unun çocuğu; boss `localScale = (1.7, 1.3, 1.7)`. Diskin local ölçeği
   `(radius*2, radius*2, 1)` olduğu için dünyadaki çapı **×1.7** oluyor: 5.4 m yarıçap
   ekranda ~9.2 m (windup squash'ıyla ~7.5 m'ye iniyor, yani boyu hazırlık pozuna da bağlı).
   §10 "telegraf en okunabilir katman" derken oyuncuya gerçek etki hacminden %40–70 büyük bir
   alan gösteriyoruz. Ayrıca mesh **Quad** — daire değil **kare**; 5.4 m'lik daireyi hiç
   temsil etmiyor. Disk bossun çocuğu olmaktan çıkarılmalı (ya da ölçek telafi edilmeli).
5. **Çakma anında boss yukarı gerilmiş kalıyor.** `Phase.Active` (90 ms) boyunca
   `SetProgress` hiç çağrılmıyor; `SlamFlash` ölçeği geri almıyor. Boss tam vururken
   `(0.82, 1.28, 0.82)`'de, yani hâlâ "hazırlanıyor" pozunda; aşağı squash yok. Recovery
   fade'i de `SetProgress(fade*0.35, ...)` ile ölçeği yeniden geriyor.
6. **Pencere ipucu görünmez — §8/T2 "bedava kazanç" hâlâ kapalı.**
   `LivingEffectView.SyncVisual` nabzı `alpha = Clamp01(alpha * pulse)` ile uyguluyor ama
   `pulse ≥ 1` ve taban alpha 0.95 → sonuç her zaman `[0.99, 1.00]` aralığında kırpılıyor.
   Nabız fiziksel olarak ölçülemez. Aşağı yönlü modülasyon ya da genişlik/ölçek sürmek
   gerekiyor; `_windowRemaining01` doğru akıyor, sorun yalnızca uygulama.
7. **`BossDirector` doğru sebebi yanlışa çeviriyor (§6 "her ölüm açıklanabilir").**
   `press < _telegraphStartMs` ise press `null`'a çekiliyor → `ExchangeResolver` "geç kaldın"
   diyor. Oysa resolver aynı girdiyle **"erken bastın"** üretecekti ve doğrusu o. Doğru eşik
   press değil **i-frame sonu**: `press + IframeStart + Iframe < telegraphStart` ise basış
   gerçekten bu saldırıya ait değil (o zaman "geç kaldın"), aksi halde "erken bastın".
   Kök neden: `DodgeState.Reset()` hiç çağrılmıyor, basış sonsuza kadar yaşıyor.
8. **`Safe` sonucu tamamen sessiz + geometri ödül bandını kesiyor.**
   `CombatFeel.OnExchange` yalnızca `Dodged`/`Hit`'i işliyor; `Safe`'te HUD'a satır yok,
   hitstop yok, kamera yok. Bu, ölçülebilir bir bandı yutuyor: boss `Approach` durma mesafesi
   `0.85 + 0.5 + 0.35 = 1.70 m`, etki yarıçapı 5.4 m. Oyuncu dururken dodge yönü
   "bossun tersi" olduğu için, strike anındaki mesafe `1.70 + 3.8×ratio(gap)`:

   | `gap` | yer değiştirme | mesafe | sonuç |
   |---|---|---|---|
   | 90 ms (MÜKEMMEL) | 2.42 m | 4.12 m | hacim içi → derece gelir |
   | 160 ms (HARİKA) | 3.48 m | 5.18 m | hacim içi (kıl payı) |
   | ~197 ms | 3.70 m | 5.40 m | **sınır** |
   | 220 ms (TEMİZ) | 3.77 m | 5.47 m | hacim dışı → `Safe`, hiçbir şey yok |
   | >260 ms | 3.8 m + | >5.5 m | `Safe` — "erken bastın" hiç yazılmıyor |

   Yani **TEMİZ ve SIYIRDI bantları** ve **"erken bastın" sebebi** kaçan dodge'da erişilemez;
   `slowmoMinGrade = TEMİZ` fiilen HARİKA'ya kayıyor. §6'nın "vuruş anında hacmin içindeyse"
   kuralına uygun, o yüzden **spec ihlali değil** — ama kararı veren sayı `+0.35f` durma payı,
   kodda gömülü ve belgede yok. Yana doğru dodge'da (son hareket yönü) hepsi erişilebilir.
   T8.1 en azından `Safe`'e HUD satırı vermeli; durma mesafesi veri olmalı.
9. **Yavaş çekim 4-nokta testi gerçek ayarı sınamıyor.**
   `SlowmoSentenceFitTests` `holdMs: 10_000, rampDown/Up: 0` ile tetikliyor — spec §7 `hold`
   **190 ms**, rampalar 55/420. Gerçek rampalarla 350 ms'lik gerçek boşluklar dünya zamanında
   ~79 / ~197 / **~349 ms** yiyor; üçüncü pencere 300 ms olduğu için **4. nokta sığmıyor**.
   Gerçekten çalışan bant ~300 ms/vuruş; 290 ms'de ise yavaş çekim **olmadan da** 4 nokta
   sığıyor. Kabul kriteri "normalde 2–3, yavaş çekimde 4" gerçek sayılarla çok dar bir
   pencerede doğru. Test spec'in `SlowmoTuning` varsayılanlarıyla kurulmalı.
10. **Ölü oyuncu oynamaya devam ediyor.** `PlayerVitals` hiçbir girdiyi kapatmıyor: 2 sn
    boyunca yürünebiliyor, cümle çizilebiliyor, dodge atılabiliyor; sonra `transform.position`
    spawn'a geri çekiliyor (çizilen cümle havada kalıyor).
11. **Ölümde boss ışınlanıyor.** `HandlePlayerDown` `_reactor.Home = _originHome` diye
    doğrudan atıyor; T7.2'nin `_visualOffset` yumuşatması yalnızca `React` yolunda var, setter'da
    yok. Ayrıca birikmiş kalıcı knockback (T7.2'nin kasıtlı davranışı) her ölümde sıfırlanıyor.

### Mobil bütçe

12. **Üç tam ekran `Image` her karede çiziliyor.** `CombatFeel` `Impact`/`Vignette`/`ThreatFlash`
    katmanlarını `Color.clear` yaparak "kapatıyor" — ama alfa 0 bir `Graphic` yine de geometri
    üretip harmanlanıyor: sürekli **3× tam ekran overdraw**. T7 görev metnindeki YASAKLAR
    "büyük yarı saydam katman (mobil overdraw)" tam olarak bunu diyor. Kullanılmayan katman
    `enabled = false` olmalı. Ayrıca "vinyet" gradyan değil **düz kırmızı dolgu**: 0.85 sn
    boyunca dünyayı (ve boss telegrafını) basıyor, recovery 720 ms olduğu için bir sonraki
    windup hâlâ kırmızı perdenin altında başlıyor — §10'un "telegraf en üstte" kuralıyla çelişir.
13. **`AfterimageTrail.SetAlpha` her karede her hayalet için yeni `MaterialPropertyBlock`.**
    7 hayalet × 60 fps ≈ 420 tahsis/sn. `SnapshotWords` açığıyla aynı sınıf; blok bir kez
    yaratılıp yeniden kullanılmalı.

### AGENTS kural 3 (his sayısı koda gömülmez)

14. **§10 renkleri iki yerden geliyor.** `CombatFeel.ShowThreat` `#FF9A3C`/`#FF4D24`'ü elle
    yazıyor, oysa aynı değerler bu görevde `PrototypeTuning.TelegraphWarm/Hot` olarak eklendi.
    `CombatFeel` `PrototypeTuning` almıyor — T6.1'de kapatılan "tek kaynak" hatasının aynısı.
15. **Gömülü his sayıları:** `shakePx * 0.01` (px→m), `_baseCutoff = 22000`, boss durma payı
    `+0.35`, telegraf squash/stretch `0.28`/`0.18`, ton perdesi `0.55→1.8` ve ses `0.12+0.28`,
    hayalet alfası `0.55`, vinyet `0.85`/`0.45`, impact sönmesi `0.04`, tehdit nabzı
    `0.15/0.55/0.15/4/10/0.35`. T7.2'nin yaptığı taşımanın aynısı gerekiyor.
16. **`EnsureT8Defaults()` tasarımcı niyetini sessizce eziyor.** `WindowCuePulseHz = 0`
    (nabzı kapatmak) **dört** alanı geri alıyor; koyu bir telegraf rengi de geri alınıyor.
    T10'un canlı ayar paneliyle doğrudan çelişir. Kalıcı çözüm: sahnedeki serileşmiş
    `PrototypeTuning` bir kez yeniden yazılıp bu metod **silinmeli**.

### Küçük / temizlik

17. `CombatFeel.SentenceEngineBridge` — `SentenceDebugHud`'ı saran, tek satırlık, adı yanlış
    (SentenceEngine ile ilgisi yok) bir sarmalayıcı. Alan doğrudan HUD olmalı.
18. `BossDirector.ResolveStrike` vurulmada `_engine?.Abort()`'u iki kez çağırıyor.
19. `DodgeMotion.MaybeEmitAfterimage`: `_motor != null ? transform.localScale : Vector3.one` —
    `_motor` kontrolünün okunan değerle ilgisi yok.
20. `SentenceWindowPrecisionTests.Tick_AfterCatchUp_DoesNotDoubleCount` yorumu yanlış:
    "aksi halde 320 kalırdı" diyor ama assert edilen değer de 320; çift sayımda 220 kalırdı.
21. `SentenceDebugHud` ölümde "ölüm — dönüş …" yazıp kalan süreyi hiç basmıyor.
22. **`SentenceDebugHud`'ın Text'i `PentagonView.SetLayerRecursively`'den SONRA yaratılıyor**,
    dolayısıyla layer 0'da kalıyor; Overlay kamerasının cullingMask'i yalnızca layer 5.
    `ScreenSpaceOverlay`'de layer önemsizdi, `ScreenSpaceCamera`'da olabilir — HUD'ın ekranda
    gerçekten görünüp görünmediği **gözle doğrulanmalı** (derleme kırık olduğu için bakılamadı).
    Genel kırılganlık: Build'den sonra canvas'a eklenen her çocuk sessizce yanlış layer'a düşer.

**Denetimde bakılamayanlar (derleme kırık):** katmanlama değişikliğinin ekran görüntüsü
(beşgen noktaları + telegraf + HUD aynı karede), dodge kaymasının 3.8 m'si, yavaş çekimin
gözle hali, afterimage. 4. ve 12. maddeler transform/render matematiğinden türetildi, ekranda
teyit edilmedi.

## T8.1 — denetim düzeltmeleri (22 Ağustos)

Derleme açıldı, oyun modu çalışıyor, `dotnet test` **73 yeşil**. 22 maddenin 21'i kapandı;
9. madde bir **karara** dönüştü (aşağıda).

**Kapanan maddeler ve nasıl:**

| # | Ne yapıldı |
|---|---|
| 1 | `DodgeMotion.Update` içindeki ikinci `pos` → `target`; CS0136 gitti, editör derliyor |
| 2 | Fallback artık `_glideExtra`'yı koruyor: ölçülen yer değiştirme **4.054 m** (3.8 + 0.254) |
| 3 | Glide hızı veri: `PrototypeTuning.DodgeGlideSpeedMps = 3.5` (14.6 m/s'lik süreksizlik yok) |
| 4 | Disk boss'un çocuğu değil, sahne kökünde; mesh **Cylinder** (gerçek daire). Ölçüldü: dünya ölçeği `(10.80, 0.02, 10.80)` → yarıçap **tam 5.40 m**, iki eksende eşit |
| 5 | `SlamFlash` → `Slam()`: çakmada aşağı squash. `Recover(t)` toparlanmada pozu tabana getirir, yeniden germez. Ölçüldü: windup `(1.39, 1.66)` → slam `(1.89, 1.01)` → hide `(1.70, 1.30)` |
| 6 | `LivingEffectView` nabzı **aşağı** modüle ediyor; `Clamp01` kırpması yok, ipucu görünür |
| 7 | Eşik basma anı değil **i-frame sonu**: `_dodge.IframeEndMs(press) < telegraphStart`. "Erken bastın" artık "geç kaldın"a dönüşmüyor |
| 8 | `Safe` HUD'a **"MENZİL DIŞI (derece yok)"** yazıyor; durma payı veri (`BossApproachStopPadM`) |
| 10 | `PlayerVitals.IsDown` girdiyi kapatıyor: `KinematicMotor` durur, `PentagonInput` parmak/klavye almaz ve açık pointer'ı iptal eder |
| 11 | Ölümde boss yalnızca **oyuncunun doğuş noktasının** etki yarıçapı içindeyse eve çekiliyor; aksi halde birikmiş knockback korunuyor, ışınlanma yok |
| 12 | Üç tam ekran katman kullanılmadığında `enabled = false` (oyun modunda ölçüldü: **açık katman 0**). Vinyet ve tehdit artık düz dolgu değil, kenardan içeri sönen maske — ekran ortası ve telegraf açık kalıyor (§10) |
| 13 | `MaterialPropertyBlock` tek örnek, tembel kurulum. **Not:** statik alan başlatıcısı olamıyor; Unity `MonoBehaviour` kurucusundan `MaterialPropertyBlock` yaratmayı yasaklıyor (ilk denemede 50 hata verdi) |
| 14 | `CombatFeel` artık `PrototypeTuning` alıyor; `TelegraphWarm/Hot` tek kaynak |
| 15 | 16 his sayısı `PrototypeTuning`'e taşındı (px→m, taban cutoff, durma payı, squash/stretch, ton perdesi/sesi, hayalet alfası, vinyet/impact/tehdit) |
| 16 | `EnsureT8Defaults` → `EnsureRuntimeDefaults`: `TuningVersion` sürüm damgasıyla kapılı. Sahne bir kez yeniden kaydedilince blok hiç girmez, tasarımcının bilinçli `0`'ı ezilmez |
| 17 | `SentenceEngineBridge` silindi, alan doğrudan `SentenceDebugHud` |
| 18 | Çift `_engine.Abort()` tek çağrıya indi |
| 19 | Anlamsız `_motor != null` üçlemesi kaldırıldı |
| 20 | Yanlış test yorumu düzeltildi |
| 21 | HUD ölümde kalan süreyi basıyor: `PlayerVitals.RespawnInSec` |
| 22 | `SentenceDebugHud` layer'ı ebeveyn canvas'tan alıyor. Oyun modunda ölçüldü: `FeelCanvas` **UI**, `SentenceDebug` **UI** |

**Oyun modunda doğrulandı** (T8'de derleme kırık olduğu için hiçbiri doğrulanamamıştı):
sahne kuruluyor, `PlayerVitals` 22/22, `BossDirector`/`DodgeMotion`/`CombatFeel` ayakta,
telegraf diski dairesel ve doğru yarıçapta, dodge 4.054 m taşıyor, tam ekran katmanlar boşta
kapalı, HUD doğru layer'da. Ekran görüntüsü: turuncu-kırmızı disk boss'un altında, oyuncu
camgöbeği — §10 renk ayrımı yerinde.

### T8.1'de çıkan karar → T8.2'de kapandı

9. madde ("yavaş çekimde 4 nokta" kriteri tutmuyor) `SlowmoTuning` süresi ölçülerek
büyütülerek çözüldü. Ayrıntı aşağıda, "T8.2" bölümünde.

## T8.2 — Yavaş çekim süresi (22 Ağustos)

**Yapılan:** `SlowmoTuning.HoldMs` 190 → **900**, `RampUpMs` 420 → **600**.
`docs/dovus-sistemi.md` §7 güncellendi (sayı + gerekçe + ölçüm tablosu). `dotnet test` **79 yeşil**.

**Neden gerekti.** Eski profille yavaş çekim **hiçbir** dokunuş temposunda tek bir cümlede
tutulan kelime sayısını değiştirmiyordu; §7'nin ödülü gramerde karşılıksızdı (alçak geçiren
filtre + görsel vardı, mekanik yoktu). Sebep faktör değil **profilin yönü**: iptal pencereleri
cümle büyüdükçe daralıyor (420 → 360 → 300) ama yavaş çekim zamanla zayıflıyor, yani en dar
pencere yavaş çekim bittikten sonraya düşüyordu. Kazanç ihtiyaç olmayan yere (ilk boşluk,
420 ms pencere) gidiyordu. 350 ms temposunda eski dünya maliyeti `120/350/350` — üçüncü boşluk
**hiç** indirim almıyordu.

Bu yüzden yalnızca rampayı uzatmak çözmüyor: `RampUpMs` 420 → 855 denendi, üçüncü boşluk
347 ms'de kaldı (pencere 300), çünkü `SmoothStep` en yavaş kısmını başta harcıyor. Derin
kısmın (hold) uzaması gerekiyordu.

**Ölçülen sonuçlar** (tek cümlede tutulan kelime sayısı, dört dokunuş):

| dokunuş aralığı | yavaş çekim yok | 190/420 (eski) | **900/600** |
|---|---|---|---|
| 350 ms | 3 | 3 | 4 |
| 400 ms | 2 | 2 | **4** |
| 450 ms | 1 | 1 | 3 |

Değer §7'nin kendi kurundan ("bir mükemmel dodge ≈ iki ekstra nokta") geriye çözüldü: 400 ve
450 ms'de fark **tam +2**. Yukarıdan da sınırlı — `HoldMs 1200` her tempoda 4 veriyor, yani
ödül otomatikleşip beceri bandı siliniyor. `SlowmoBonusDots` **0 kaldı**: §7 "tavanı
yükseltmez" diyor ve bonus nokta `SentenceEngine`'e "yavaş çekimde pencereler farklı" diye
bir özel durum eklemek olurdu (yavaş çekim cümle ortasında bitince bonus ne olacak? gibi
kenar durumlar). Motor yavaş çekimin varlığını hâlâ bilmiyor, yalnızca dünya zamanını görüyor.

**Testler artık ödülün kendisini koruyor** (`SlowmoSentenceFitTests`): temel çizgi, tavana
ulaşma, +2 kuru, doygunlaşmama ve "tavan yükselmiyor". Hepsi `SlowmoTuning`'i okur, sayıyı
tekrar etmez. `CombatTuningDefaultsTests` spec aynası olarak 900/600'e güncellendi — bu test
değişikliği yakaladığı için spec ile kodun ayrışması mümkün değil.

**Uyarı (T11 his turu).** Toplam yavaş çekim 665 ms → **1555 ms** gerçek zamana çıktı. O
sürede boss yalnızca ~430 ms dünya zamanı ilerliyor, yani ~1.1 sn gerçek zaman kaybediyor.
İfade penceresinin üstüne ciddi bir **savunma** avantajı da geliyor; iyi bir dodge "kaçış"
olarak sömürülüyorsa ilk kısılacak sayı budur.

### T9 — HUD (`Dovus.Game`)

- `ReactionReadout` — §6 gösterimi: kenarda (varsayılan sağ, `PrototypeTuning.ReadoutAnchorRight`
  ile ayarlanabilir) büyük/parlak tepki yazısı. `CombatFeel.OnExchange`'den `NoteExchange(ExchangeResult)`
  ile beslenir (yeni `CombatFeel.Bind` parametresi, `debug` ile aynı yerden). Dodged'de
  `"0.45 sn  MÜKEMMEL"` + §6 tablosundaki mesaj (`"tepki süren mükemmel"` vb.); Hit'te sebep yazısı
  (`"erken bastın"`/`"geç kaldın"`) nötr `PentagonDotColor` ile (§10: kırmızı-turuncu yasak). Seri
  sayacı (üst üste Dodged, Hit'te sıfırlanır) ve oturumun en iyi tepkisi de aynı bileşende. Animasyon
  tamamen `Time.unscaledTime` — punto/glow/bekleme/sönme `FeelTuning.Readout*`'tan (T1'de spec'ten
  kondu, burada ilk kez gerçekten kullanıldı); giriş vuruşu (scale punch) `ReadoutPunchInSec`.
  "Katmanlı glow" bir radial-gradient `Image` + `Outline` bileşeninin üst üste binmesiyle taklit
  edilir (TMPro yok, proje `UnityEngine.UI.Text` kullanıyor — T5'in paket listesi TMPro'yu
  saymıştı ama hiçbir görev kurmadı, mevcut deseni bozmadım).
- `VitalsHud` — §6/§11 "boss ve oyuncu can göstergesi, sade". Oyuncu barı `PlayerVitals.Hp/MaxHp`'ı
  gerçek zamanlı okur. **Boss barı kozmetiktir** (bkz. sapmalar) — Core/Game'de boss hasarı yok.
- `PrototypeTuning`: `ReadoutAnchorRight`, `ReadoutPunchInSec`, `VitalsBarWidthDp/HeightDp/SpacingDp`,
  `BossVitalsColor`. `TuningVersion` 2 → 3 (yeni alanlar için tek seferlik yama, T8.1'deki desenin
  aynısı).

**Test:** Core'a dokunulmadı, `dotnet test` hâlâ **79 yeşil**. Unity: `AssetDatabase.Refresh()`
sonrası `EditorUtility.scriptCompilationFailed=False`; sahne diskten yeniden açılıp temiz play
mode'da konsol `errorCount=0`. `ReactionReadout`/`VitalsHud` hiyerarşisi (`Main`/`Sub`/`Tally`/`Glow`,
`BossFill`/`PlayerFill`) doğru kuruluyor.

**Unity play mode (MCP prob, gerçek `CombatFeel.OnExchange` + elle tetiklenen `ExchangeResult`'lar
üzerinden):**
- Gerçek boss döngüsü çalışırken (oyuncu dodge atmadı) bir "geç kaldın" `Hit` olayı **kendiliğinden**
  `ReactionReadout`'a ulaştı — `CombatFeel → ReactionReadout` bağının canlı oyunda çalıştığının kanıtı
  (manuel tetikleme değil).
- Elle: MÜKEMMEL (reaction 0.45) → metin `"0,45 sn  MÜKEMMEL"` + `"tepki süren mükemmel"`; ~2 sn
  sonra (hold 900 + fade 500 ms'nin üstünde) `main/sub/glow` alfası tam 0'a döndü ve kuyruk metni
  `"en iyi tepki: 0,45 sn"`a geçti — sönme matematiği doğru.
- Boss'u `enabled=false` yapıp arka arkaya 3 Dodged (MÜKEMMEL/TEMİZ/HARİKA, reaction 0.50/0.20/0.35)
  tetikledim: bir kare sonra kuyruk `"seri ×3 · en iyi 0,20 sn"` — seri sayıyor, en iyi değer doğru
  minimum. Ardından bir `Hit` tetikleyince seri sıfırlandı (kuyruk yalnızca `"en iyi tepki: 0,20 sn"`
  kaldı) ve metin `"erken bastın"`a döndü.
- `VitalsHud`: oyuncu gerçek `PlayerVitals.ApplyDamage`/respawn döngüsüyle test edildi (boss'un
  gerçek saldırısı oyuncuyu vurup 22/22 → 0 → 2 sn sonra 22/22'ye döndürdü), bar `fillAmount`
  her seferinde `Hp/MaxHp` ile eşleşti. Boss barı beklenen gibi sabit `1`.
- **T9'da doğrulanamayan tek şey** hold penceresinin ORTASINDA bir ekran görüntüsüydü (kabul
  kriteri 1: "yazı okunaklı, parlak ve zamanında görünüyor"). İki oturum denendi, MCP round-trip'i
  pencereyi hep kaçırdı; `Unity_Camera_Capture` de URP kamera-stack'ini kompoze edemediği için
  (Base'de UI yok, Overlay tek başına beyaz) işe yaramadı. **T9.1'de kapandı** — aşağıya bak.

### T9.1 — T9 denetim düzeltmeleri (22 Ağustos)

T9'un kodu okundu, oyun modunda ölçüldü ve **gerçek ekran görüntüsüyle** doğrulandı. Üç hata
düzeltildi, doğrulanamayan kabul kriteri kapatıldı. `dotnet test` **79 yeşil** (Core'a dokunulmadı).

**Ekran görüntüsü yöntemi (sonraki ajan için):** `Unity_Camera_Capture` yerine oyun modunda
`ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/x.png"))` çağrılıyor — bu Game view'ı
**kompoze** yakalar, yani URP Overlay kamera stack'i ve UI dahil. Pencereyi kaçırmamak için
`ReactionReadout.Configure`'a geçici bir `FeelTuning { ReadoutHoldMs = 60000 }` verildi (yalnızca
prob; commit'e girmedi). Dosya `unity/Temp/` altına yazılır, git yok sayar.

1. **Yazı kendi bandını taşıp dünyayı kapatıyordu (kabul kriteri 3).** `ReadoutSizePx = 96` sabit
   punto + `HorizontalWrapMode.Overflow` demek: "0,45 sn MÜKEMMEL" 1027×495 game view'da **944 px**
   yer kaplıyordu, band ise 411 px. Yazı ekranın soluna kadar uzanıp arenayı ve bossu örtüyordu
   (ilk ekran görüntüsünde net). Punto spec'ten geldiği için (§8 başlangıç sayıları) sayı
   değişmedi; artık **tavan** olarak okunuyor: `FitTexts` yazının `preferredWidth`'ini bandın
   genişliğine oranlayıp puntoyu düşürüyor. Ölçüldü: ana satır 96 → **41 px**, `preferredWidth`
   402 ≤ band 411; alt satır 31, kuyruk 23 — üçü de bandın içinde.
   Unity'nin kendi `resizeTextForBestFit`'i **denendi ve çalışmadı**: kurulum karesinde bandın
   genişliği daha 0 olduğu için puntoyu 14'te dondurdu (ekranda okunmayacak kadar küçük).
2. **Boşta duran yazı overdraw üretiyordu** — T8.1 denetiminin 12. maddesinin aynısı ("alfa 0 bir
   `Graphic` yine de geometri üretip harmanlanır"). `HideAll` renkleri `Color.clear` yapıyordu ama
   bileşenler açık kalıyordu: ekranın **%40×%24'ünü kaplayan glow `Image`'i** + üç `Text` her karede
   çiziliyordu. Artık `enabled = false`; oyun modunda ölçüldü: yazı yokken dördü de kapalı.
3. **`VitalsHud`'ın `...Dp` alanları ham piksel olarak kullanılıyordu.** Canvas `ConstantPixelSize`,
   yani projedeki her dp ölçüsü `PentagonLayoutScreen.DpToPixels`'ten geçer (beşgen, dodge diski,
   çubuk). Barlar bu yoldan geçmediği için yüksek yoğunluklu telefonda **~2.5 kat küçük** çıkacaktı.
   Düzeltildi (ölçüldü: dpi 144'te 220 dp → 198 px). Koda gömülü 18 px'lik üst boşluk da veri oldu:
   `PrototypeTuning.VitalsMarginDp`.

**T10 hazırlığı (aynı düzeltmede yapıldı).** T9'un alanları kurulum anında bir kez okunuyordu, yani
T10'un canlı slider'ı ekranda hiçbir şeyi değiştirmezdi ("her slider anında canlı etki eder,
yeniden başlatma gerektirmez"). `ReactionReadout.ApplyTuningLayout` ve `VitalsHud.ApplyTuningLayout`
artık her karede **uygulanan değeri karşılaştırıp** değiştiyse yerleşimi yeniden yazıyor:
punto, glow şiddeti, yazının hangi kenarda duracağı, bar ölçüleri/renkleri. Değişmeyen karenin
maliyeti birkaç float karşılaştırması. Aynı desen T10'da yeni alanlar için tekrarlanabilir.

T10'un ayar nesnelerini araması gerekmesin diye, **çalışma anında hangi örnek nerede**:

| Ayar | Örnek nerede doğuyor | Çalışma anında nereden tutulur |
|---|---|---|
| `PrototypeTuning` | `PrototypeBootstrap._tuning` (`[SerializeField]`, sahnede serileşmiş) | `PentagonInput.Tuning` (public alan); `KinematicMotor`/`ActorPose`/`BossReactor`/`FollowCamera` aynı **referansı** paylaşır (T5 denetimi: kopya yok) |
| `CombatTuning` (`.Dodge`/`.Sentence`/`.Slowmo`/`.Boss`/`.Grade`/`.Feel`/`.Manifestation`) | `PrototypeBootstrap.BuildWorld` içinde `new CombatTuning()` | `PentagonInput.Combat` (public alan). Core POCO'su, `ScriptableObject` **yok** — T10'un 1. maddesi onu kuracak |

İkisi de referansla paylaşıldığı için bir slider alanı yazınca ilgili sistem bir sonraki karede
görür; istisna, değeri **kurulumda** okuyan yerlerdir (bar/yazı yerleşimi buydu, T9.1 kapattı;
`GameClock.Bind`, `PlayerVitals.Bind` gibi tek seferlik bağlamalar hâlâ öyle). Ayrıca T8.1'in 16.
maddesi duruyor: `EnsureRuntimeDefaults` sahnedeki serileşmiş kopyayı `TuningVersion` damgasıyla
bir kez yamalıyor. Sahne şu an **v0** (alanlar YAML'de hiç yok), yani her açılışta varsayılanlara
çekiliyor. T10 kalıcılığı JSON'a yazacağı için bu yama sırasını bilmek zorunda: **önce**
`EnsureRuntimeDefaults`, **sonra** JSON.

**Oyun modunda ölçülenler (MCP, gerçek sahne):**

- Derleme temiz (`scriptCompilationFailed=False`), konsol `errorCount=0`; tek uyarı T7.1'den beri
  not edilen, koddan bağımsız AI Toolkit ağ uyarısı.
- **Kabul kriteri 1 kapandı:** MÜKEMMEL dodge'un ekran görüntüsü alındı — camgöbeği "0,45 sn
  MÜKEMMEL", altında "tepki süren mükemmel", kuyrukta "en iyi tepki: 0,45 sn", arkasında glow
  halesi. Yazı okunaklı ve parlak.
- **Kabul kriteri 3 kapandı:** aynı karede boss, arena ve beşgen açıkta; yazı yalnızca sağ bandı
  kaplıyor.
- **Kabul kriteri 2 ölçüldü:** `Time.timeScale = 0.1` iken tetiklenen yazı **7.1 sn gerçek zaman**
  sonra tamamen kapanmıştı. Ölçekli saatte olsaydı 1.4 sn'lik hold+fade 14 sn sürerdi — animasyon
  gerçekten `Time.unscaledTime`'da.
- `Hit` yolu canlı boss saldırısıyla da doğrulandı: "geç kaldın" nötr renkte, bandın içinde.
- Seri sayacı iki tetiklemede `seri ×2 · en iyi 0,45 sn` yazdı.

## Spec'ten sapmalar

Belgedeki bir kural/sayı uygulanamadıysa buraya yaz: hangisi, neden, yerine ne kondu.
Sessiz sapma en pahalı hata türü.

### T1 denetiminde düzeltilenler

- **`GradeTuning.ReactionDisplaySec = 0.45` kaldırıldı.** Tepki süresi bir ayar değil,
  çalışma anında ölçülen sonuç (`basma anı − telegraf başlangıcı`). Sabit olarak kalsaydı
  T3 ve T9 onu değişmez sanabilirdi. Spec §8'e bunu söyleyen bir not eklendi.
- **`SentenceTuning` düzleştirilmiş `Dot1..Dot4` alanları diziye çevrildi**, ve
  `EffectPerSecond` saklanmak yerine türetildi — saklanan hâli, süre ayarlandığında yalan
  söylüyordu.
- **`FeelTuning` boştu**, çünkü spec §8'de sayı yoktu (ajan haklı olarak uydurmadı).
  Eksik bizdeydi: §8'e "Başlangıç sayıları" tablosu eklendi ve sınıf dolduruldu.
- **`.csproj` dosya dosya link'liyordu**, jokere çevrildi.

### T2 denetiminde düzeltilenler

- **Bekleme artık iptal penceresini dondurur.** Önce donmuyordu: bekleme 220 ms, fiil
  penceresi 420 ms olduğu için ikinci yığın gelmeden cümle çözülüyordu — yani
  `dwellMaxStacks = 2` oyunda ulaşılamaz bir sayıydı. Kriter 6'nın testi bunu göremiyordu
  çünkü `OnDwell`'i hiç `Tick` çağırmadan üst üste çağırıyordu; test artık dünya zamanını
  gerçekten akıtıyor. Kuralın gerekçesi spec §3'e (ve §5'e tek satır) yazıldı.

### T3/T4 denetiminde düzeltilenler

- **Vuruştan sonra basılan dodge "erken bastın" diyordu.** i-frame penceresi vuruşun
  sonrasında açıldığı için kod bunu erken basma sanıyordu; oyuncuya tam ters sebep
  gösteriliyordu (§6: "her ölüm açıklanabilir olmalı"). Artık vuruş i-frame'den önce
  kalıyorsa sebep `GecKaldin`. Test: `PressAfterStrike_HitsWithGecKaldin`.
- **Hitstop sırasında gelen yavaş çekim yutuluyordu.** Duraklatılmış eski yavaş çekim
  yeni tetiği eziyordu; üstelik kuyruktaki tetik temizlenmediği için çok sonra gelen
  alakasız bir hitstop'ın bitiminde hayalet yavaş çekim başlatabiliyordu. Yeni tetik artık
  eskisini geçersiz kılıyor ve kuyruk her hitstop sonunda tüketiliyor. Testler:
  `SlowmoDuringHitstop_WhileEarlierSlowmoPaused_StartsFresh`,
  `QueuedSlowmo_DoesNotLeakIntoALaterHitstop`.
- **Derecelendirme eşikleri i-frame penceresine sığdırıldı: 110/200/320 → 90/160/220.**
  Eski eşiklerle `DodgeGrade.Siyirdi` hiç üretilemiyordu; başarılı bir dodge'da `gap`
  tanımı gereği pencereden (260 ms) küçük, TEMİZ eşiği ise 320'ydi. Yan etkisi §7'deki
  `slowmoMinGrade = TEMİZ`'in işlevsiz kalmasıydı — her başarılı dodge yavaş çekim
  veriyordu; artık 221–259 bandı ödül vermiyor. Kural spec §6'ya yazıldı ve
  `GradeThresholds_FitInsideIframeWindow` ile teste bağlandı; ayrıca
  `Resolve_ProducesEveryGrade_WithinIframeWindow` dört derecenin de `Resolve` üzerinden
  gerçekten doğduğunu doğruluyor.
- **`ExchangeInput`/`ExchangeResult` Unity'de derlenmezdi.** `init` accessor'lar Unity 6'nın
  .NET Standard 2.1 BCL'inde CS0518 veriyor (resmî olarak "desteklenmeyen özellik").
  `Core/Compat/IsExternalInit.cs` shim'i eklendi; net8.0 testlerinde `#if` ile devre dışı.
  Ayrıca `Core/csc.rsp` (`-nullable:enable`) kondu ki nullable işaretleri uyarıya değil
  gerçek denetime dönüşsün. **İkisi de Unity açılana kadar doğrulanmadı.**

### T5 denetiminde düzeltilenler

- **Boss gövdesi bossun telegraf rengindeydi** (`#FF6120`). §10 kırmızı-turuncuyu yalnızca
  *tehdide* ayırıyor; gövde sürekli o renkte kalırsa T8'in telegrafı yandığında kontrast
  kalmaz — kural tam da bunu korumak için "pazarlıksız". Gövde nötr koyuya alındı, oyuncu
  §10'daki camgöbeğinin kendisine (`#5FF0FF`) çekildi. Renkler artık `PrototypeTuning`'de.
- **Her şey `Universal Render Pipeline/Unlit` ile çiziliyordu.** Kurulan güneş, yumuşak
  gölgeler ve `RenderSettings.sun` tamamen ölü koddu; kapsüller düz leke olarak görünüyordu,
  yani derinlik okunmuyordu. `Lit`e alındı.
- **`GameClock` ile onu okuyan davranışların Update sırası tanımsızdı.** `KinematicMotor`
  önce koşarsa bir önceki karenin `WorldDeltaMs`'ini kullanıyordu (ilk karede 0).
  `[DefaultExecutionOrder(-1000)]` ile sıra sabitlendi — T8 yavaş çekimde bunu kare kare
  ölçecek.
- **`PrototypeTuning`'in üç kopyası vardı**, `CopyTuning` ile elle senkronlanıyordu.
  T10 slider'ı Bootstrap'in kopyasını değiştirince hareket/kamera sessizce eski değerde
  kalırdı. Artık tek örnek referansla paylaşılıyor.
- **`ArenaHalfSizeM` ölü ayardı**: yalnızca zeminin ölçeğini belirliyordu, oyuncu zeminin
  dışına sonsuza yürüyebiliyordu. T8'de boss 2.2 m/s ile 4.5 m/s'lik oyuncuyu asla
  yakalayamayacağı için dövüş kaçılarak bozulabilirdi. Konum artık arena kenarına kırpılıyor.
- **`MainCamera` etiketi `TagManager.asset`'e eklenmişti.** Bu Unity'nin yerleşik etiketi;
  her `AssetDatabase.Refresh` "already registered" satırı basıyordu, yani "konsol temiz"
  kriteri gürültülüydü. Etiket listesi geri boşaltıldı.
- **Kapsüllerin collider'ları duruyordu.** Teknoloji kararları §4 fiziği tamamen dışarıda
  bırakıyor (vuruş tespiti matematik); bırakılan collider ileride yanlışlıkla fiziğe
  dayanmayı davet ediyordu, siliniyor.
- Boss kapsülü 0.1 m yere gömülüydü (y=1.2, yarı yükseklik 1.3); konum yükseklikten türetildi.

### T6 sapmaları / varsayılanlar

- **Beşgen yarıçapı / konum / hit yarıçapı / mürekkep ömrü spec'te yok.**
  `PrototypeTuning`: `PentagonRadiusDp=100`, merkez norm (0.78, 0.40), `DotHitRadiusDp=30`,
  `CenterHitRadiusDp=24`, `InkLingerSec=0.40`. Telefonda T11'de ayarlanacak.
- **`DotVibrationMs = 30`** — spec'te sayı yok. `Handheld.Vibrate` ~500 ms idi; 4 noktalı
  cümlede ayrık onay (§2) bozuluyordu. Android `createOneShot`; diğer platform sessiz.
- **`OnDotTouched`/`OnDwell` hâlâ `worldTimeMs` yutmuyor** (Core değişmedi — T6/T6.1 yasak).
  Girdi katmanı doğru değeri iletiyor; motor `Tick` ile eritiyor. Açık kalır.

## T6 denetimi + T6.1 kapanış

T6 play mode (sanal Touchscreen): `5-1-2`, merkez tap/sürükleme, dwell yığınları, sol+sağ
çok parmak, mürekkep — geçti. Konsol temiz; `dotnet test` 46.

T6.1 düzeltmeleri uygulandı (aynı dal):

1. `MoveInput` → `IsRightHalf` (ayna çift sahipliği kapandı)
2. Dwell → `WorldDeltaMs` (§3 pencere donması ile birim uyumu)
3. `OpenDot1..5` — kapalı (3/4) motora/ses/mürekkep yok; view soluk
4. `Canceled` dokunuş → tap-dodge yok
5. Android kısa titreşim `DotVibrationMs=30`
6. Hece adı `RuneInfo.Syllable`
7. `OnDisable` `_fingerId` temizliği; `_engine` null guard; cooldown'da Abort/HUD yok

**T6.1 doğrulama (play mode, sanal Touchscreen — 8 senaryonun 8'i geçti):**

- `5-1-2` → `SÜRÜ/4.4`, bozulmadı
- Kapalı 3 ve 4 cümleye kelime eklemiyor; parmak 3→4→5 gezinince fiil 5 oluyor
- Aynalama açıkken sol yarıda çizim çubuğu sürmüyor (`yön=(0,0)`); sağ yarı çubuğu sürüyor
- Merkez tap dodge açıyor, merkezden sürükleme açmıyor
- `Canceled` merkez dokunuşu dodge tetiklemiyor
- Sol çubuk + sağ çizim eşzamanlı
- **Dwell gerçekten dünya zamanında:** 0.2× yavaş çekimde ilk yığın ~1100 ms *gerçek* sürede
  düştü (= 220 ms dünya zamanı), pencere 218 → 398 ile tam `DwellMs` kadar iade aldı: nötr.
  Düzeltmeden önce aynı sürede 2 yığın düşer ve pencere bedavaya sıfırlanırdı.

Konsol temiz, `dotnet test` 46 yeşil. Titreşim/hece kulakla ve ekran görüntüsü T11'e kalıyor.

> **MCP ile ölçüm alacak ajana:** enjekte edilen dokunuşlar işlemiyorsa sebep büyük ihtimalle
> `runInBackground` kapalı olması — editör odağı kaybedince play loop duruyor ve `Time.frameCount`
> sabit kalıyor. Prob eklemeden **önce** ayrı bir komutla `Application.runInBackground = true`
> yapıp iki ölçümde kare sayısının arttığını doğrula; probun kendi içinde ayarlaman yetmez
> (o satır çalışmak için zaten bir kareye ihtiyaç duyar).

## T7 sapmaları / varsayılanlar

- **Tezahür hızları/yarıçapları spec'te yok.** `ManifestationTuning`: Wave 9 m/s / 9 m,
  Needle 16 / 12, Swarm 6.5 / 7, FocusPerIgne 0.78, MorphLerp 3.5/s, ScarScale 1.4.
  Telefonda T11.
- **KABUK/ZEHİR fiil olarak spawn edilmez** (noktalar kapalı); sıfat/kapanış türü olarak
  silüet ve scar yolları yine var (gramer beş rün tanır).
- **Seyahat teması tek seferlik hafif sarsıntı**; asıl ödeme kapanış bang'inde. Can/hasar T8.

## T7.1 sapmaları / varsayılanlar

- **`ManifestationTuning.MaxHoldPastRangeSec = 6f` (uydurma, spec'te yok).** Menzilini bitiren
  etkinin cümle kapanana kadar beklemesi gerekiyor (§5/T2), ama bu bekleme motor tarafından asla
  gerçekten sınanmıyor — `SentenceEngine` her cümleyi er ya da geç kapatır (Abort ya da pencere
  zaman aşımı). Bu alan sadece güvenlik payı: cümle hiç kapanmazsa (beklenmeyen bir durum/hata)
  etki sonsuza kadar dünyada asılı kalmasın diye. 6 sn, en uzun gerçekçi cümle+toparlanma+sessizlik
  süresinden (~2 sn) kasıtlı olarak kat kat büyük seçildi ki normal oyunda hiç tetiklenmesin.
- **§8/T2'nin "bedava kazancı" kurulmadı: "iptal penceresini dalganın nerede olduğuna bakarak
  bilirsin".** Şu an iptal penceresi hâlâ görünmez — `LivingEffectView` etkinin silüetini/mesafesini
  çizer ama kalan pencere süresiyle görsel olarak bağlanmış değil (`SentenceDebugHud` metinle
  gösteriyor, dünyada değil). Spec'te bunu somutlaştıran bir sayı/mekanik yok, uydurulmadı. Bu,
  T7.1'in görev tanımı dışındaydı (görünüm dosyalarına dokunma yasağı) ve   T7.2 de almadı —
  **T8 aldı:** `LivingEffectView.SetWindowCue` + `Travel/MaxRange` nabzı.

## T7.2 sapmaları / varsayılanlar

- **`PrototypeTuning.GroundScarCapCount = 60` (uydurma, spec'te sayı yok).** T11 kare bütçesi
  için icat edildi — 30 kapanışta gerçekçi en kötü senaryo (SARSINTI odaklı çatlak kapanışta
  3 alt damga + seyahatte 1 = 4/kapanış) ~120 iz üretebilir; tavan bunun yarısından azında
  görsel yoğunluğu sınırlıyor. Telefonda T11'de ayarlanacak.
- **`LivingEffectView`'daki iğne (needle) ölçek sabitleri (0.35/0.14 kalınlık, 0.7/0.5 uzunluk)
  ve küre yanı offset sabitleri (0.35/1.4/0.4) da veri oldu**, görev metninde tek tek sayılmasa
  da "küre ölçekleri" kategorisiyle aynı karakterde — needle ölçek sabitleri taşındı, yanı-offset
  sabitleri (renk lerp oranı, y-yüksekliği ofseti, nefes/breath sabitleri gibi küçük çizim
  detaylarıyla birlikte) kasıtlı olarak taşınmadı; kapsamı büyütmemek için görevde açıkça
  sayılanlarla ve doğrudan yan yana duran ölçek çiftleriyle sınırlı tutuldu.
- **"30 kapanış üst üste" kabul kriteri gerçek 30 kez canlı oyunda değil, eşdeğer bir izole
  testle doğrulandı:** `GroundScarField.Stamp` tavan=5 ile 12 kez çağrıldı, sonuçta tam 5 nesne
  kaldı ve hiçbirinde collider yoktu (yukarıdaki T7.2 bölümü madde 4). Gerçek sahnede tek bir
  canlı kapanış da aynı `Stamp` yolunu kullandığını doğruladı (`GroundScars` child count 1).
  Unity MCP'nin `Update()` private metodunu reflection'sız tetikleyememesi yüzünden 30 gerçek
  kapanışı gerçek zamanda (~30 sn bekleme) art arda çalıştırmadım — mekanik izole testte kanıtlı,
  ama tam senaryo (Hierarchy'den elle sayım) doğrulanmadı. **Bir sonraki ajan/insan telefonda
  veya editörde 30 kapanış yapıp Hierarchy'de `GroundScars` altındaki nesne sayısını gözle
  saymalı.**

## T6.2 sapmaları / varsayılanlar

- **Dodge diskinin yeri/boyu spec'te yok (uydurma).** `PrototypeTuning`:
  `DodgeButtonOffsetXDp = 80`, `DodgeButtonOffsetYDp = -140` (beşgen merkezinden sağa-aşağı),
  `DodgeButtonRadiusDp = 34`, `DodgeButtonScreenMarginDp = 8`. Ölçüt: disk beşgenin hit
  alanlarının dışında kalsın (merkezden uzaklık 161 dp > yarıçap 100 + disk 34) ve başparmağın
  doğal yayına düşsün. **Telefonda T11'de ayarlanacak** — §13'ün 1. sorusu doğrudan buna bağlı.
- **`PentagonCenterXNorm` 0.78 → 0.72.** Görev metni "düğme ekranın dışına taşmasın, merkezi
  gerektiği kadar içeri al" diyordu. Ek olarak `DodgeButtonPx` konumu **çizim yarısının içine**
  kırpıyor: kırpma olmasa dar bir ekranda disk sanal çubuğun yarısına düşüp T6.1'de kapatılan
  çift sahiplik hatasını geri getirebilirdi.
- **`BasicStrikeDot = 5` (SARSINTI).** §5 "hangi fiille vurduğu veridir (prototipte 5/SARSINTI)"
  diyor, sayı orada. Açık/kapalı rün bayrağına **bakılmıyor**: merkez bir kelime değil düğme, o
  yüzden `IsDotOpen` kapısından geçmiyor. `BasicStrikeDot` kapalı bir rüne çevrilirse beşgende
  soluk görünen bir rünle vurulur — bilinçli, ama T10 ayar panelinde tuzak olabilir.
- **Düz vuruşta hece sesi çalıyor, mürekkep izi çizilmiyor.** §2 "her kaydedilen nokta kısa
  titreşim + hece sesi verir" dediği için ses var (motorda gerçekten bir kelime kaydediliyor);
  mürekkep iki nokta arası bir iz olduğu için tek noktalı vuruşta karşılığı yok.
- **Erken kapanışın (`Commit`) kendi sesi/geri bildirimi yok**, yalnızca debug HUD notu. Spec bir
  ses/his tanımlamıyor, uydurulmadı — T9'un HUD'ı ya da T11'in his turu almalı.

## T8 sapmaları / varsayılanlar

- **`PlayerMaxHp = 22`** (spec'te oyuncu tavanı yok, hasar 22). Bir çakma = ölüm;
  respawn döngüsü böyle denenebiliyor. T11.
- **Pencere ipucu sayıları spec'te yok:** `WindowCueUrgentRatio=0.30`,
  `WindowCuePulseHz=2`, `WindowCueUrgentHz=8`, `WindowCuePulseAmp=0.45`.
  Kalan süre `RemainingWindowMs/ArmedWindowMs`; dalga yeri `Travel/MaxRange`.
- **Telegraf tonu 220 Hz**, perde 0.55→1.8 (spec "yükselen ses", sayı yok).
- **`shakePx * 0.01` → metre** (6 px = 6 cm). Dönüşüm spec'te yok.
- **`PrototypeTuning.EnsureT8Defaults()`** — T8.1'de `EnsureRuntimeDefaults` + `TuningVersion`
  damgasına döndü. Sahne bir kez yeniden kaydedilince blok bir daha girmez.

## T8.1 sapmaları / varsayılanlar

Hepsi `PrototypeTuning`'de, hiçbiri kodda gömülü değil (AGENTS kural 3).

- **`DodgeGlideSpeedMps = 3.5`** — §6 "sönen artık hız"ın büyüklüğünü vermiyor. T8'in
  kullandığı 14.6 m/s (ana hareketin ortalama hızı) ikinci bir atılım gibi okunuyordu; yürüme
  hızı mertebesi seçildi. 220 ms'lik kuyrukta ≈ **0.25 m** ek mesafe (ölçüldü: 4.054 m toplam).
- **`BossApproachStopPadM = 0.35`** — T8'de kodda gömülüydü. Bu sayı hangi derecelerin
  erişilebilir olduğunu belirliyor (denetim 8. maddesindeki tablo); veri olması T11 his
  turunda ödül bandını ayarlanabilir kılıyor.
- **Telegraf pozu:** `TelegraphStretch = 0.28`, `TelegraphSquash = 0.18`,
  `TelegraphSlamSquash = 0.22`. §11 pozu tarif ediyor, oran vermiyor.
- **Ton:** perde `0.55 → 1.8`, ses `0.12 → 0.40`. Spec "yükselen ses" diyor, sayı yok.
- **His katmanı:** `AfterimageAlpha = 0.55`, `ImpactFadeSec = 0.04`, `VignetteHoldSec = 0.85`,
  `VignetteFadeSec = 0.45`, `VignetteAlpha = 0.55`, `ThreatAlphaMax = 0.35`,
  `ThreatPulseHz 4 → 14`, `CameraShakePxToM = 0.01`, `AudioBaseCutoffHz = 22000`.
  Hepsi T8'de koda gömülüydü, değerler korundu.
- **Vinyet/tehdit maskesi 64×64 üretilmiş doku**, alfa yarıçapın 0.40'ından kenara
  `SmoothStep`. Spec "kırmızı vinyet" diyor, profil vermiyor. Düz dolgu yerine bu seçildi
  çünkü §10 telegrafın en okunabilir katman kalmasını istiyor.
- **Telegraf diski `PrimitiveType.Cylinder`**, ölçek `(çap, 0.02, çap)`, y = 0.03.
  Unity silindiri ~20 kenarlı, yani ekranda çokgen bir daire — prototip için kabul.

## T9 sapmaları / varsayılanlar

- **Boss can göstergesi kozmetiktir, gerçek bir hasar mekaniği YOK.** T9'un kabul kriteri "boss ve
  oyuncu can göstergesi, sade" diyor ama Core/Game'de hiçbir yerde boss HP/hasar tutulmuyor — T7'den
  beri "boss fiziksel tepki verir (geri tepme/sarsılma/kabuk), hasar yok" diye kayıtlı ve bu görevin
  YASAKLAR'ı yeni oyun mekaniği eklemeyi (dolayısıyla boss-HP sistemini) kapatıyor. `VitalsHud` bossu
  her zaman dolu, nötr renkli (`BossVitalsColor`, ne oyuncu ne tehdit paletinden) bir bar olarak
  gösteriyor — sadece simetri için var. Gerçek boss HP'si ayrı bir **karar** gerektirir (boss nasıl
  "ölür"? bu prototipte hiç tanımlı değil) — T10/T11'den önce sahibiyle konuşulmalı.
- **`PrototypeTuning.ReadoutPunchInSec = 0.12f`** (uydurma). Spec "giriş vuruşu (scale punch)"
  diyor ama süre vermiyor; `FeelTuning.ReadoutPunchScale` (büyüklük) zaten spec'ten kondu, süre yok.
  Küçük bir animasyon inceliği olduğu için mertebe seçildi (kamera yumruğunun sönme süresiyle
  aynı büyüklük mertebesi — T8.1'in `ShakeDecay`'i ~0.33 sn'ye karşılık geliyor).
- **`ReadoutAnchorRight = true`** — §6 "sağ kenarda" diyor, varsayılan onu yansıtıyor; "hangi kenarda
  duracağı ayarlanabilir" kabul kriteri için alan eklendi, sol tarafı T10/T11'de denenebilir.
- **Katmanlı glow gerçek bir bloom/post-process değil.** Proje TMPro kurmadı (T5 paket listesinde
  vardı ama hiçbir görev kullanmadı, mevcut kod hep `UnityEngine.UI.Text`); glow bir radial-gradient
  `Image` + `Outline` bileşeninin üst üste binmesiyle taklit edildi. Gerçek bloom istenirse ayrı bir
  görsellik görevi (Faz 4) gerekir.
- **Vurulma sebebi yazısının rengi §10'a göre seçildi ama spec'te renk belirtilmiyor.** Nötr
  `PrototypeTuning.PentagonDotColor` kullanıldı (yeni alan eklenmedi, var olanı yeniden kullandım) —
  hem oyuncu (camgöbeği/mor) hem tehdit (kırmızı-turuncu) paletinden kasıtlı olarak ayrı.

### T9.1 sapmaları / varsayılanlar

- **`FeelTuning.ReadoutSizePx` artık bir tavan, sabit punto değil.** Spec §8'in sayısı (96)
  değişmedi; yazı bandına sığmıyorsa oranla küçülüyor. Gerekçe yukarıda (T9.1 madde 1): sabit
  puntoyla yazı §10'un "telegraf en okunabilir katman" kuralını çiğneyip dünyayı örtüyordu.
  Ekran büyüdükçe punto tavana kadar geri çıkar, yani telefonda küçülme olmayabilir.
- **`PrototypeTuning.VitalsMarginDp = 18`** (uydurma, spec'te yok) — T9'da `VitalsHud`'a gömülü
  18 px'lik üst boşluktu, değer aynı kaldı, yalnızca dp'ye ve veriye taşındı (AGENTS kural 3).

## Bilinen açıklar

- T1/T2/T3/T4/T7/T7.1/T6.2/T8/T8.1/T8.2 `dotnet test` yeşil (`tools/CoreTests`, **79** test).
- **Boss can göstergesi hâlâ kozmetik — karar sahibinde.** `VitalsHud`'ın boss barı her zaman dolu;
  Core/Game'de boss HP/hasar yok ve "boss nasıl ölür" prototipte hiç tanımlı değil. T9'un
  YASAKLAR'ı yeni mekanik eklemeyi kapattığı için T9.1'de de dokunulmadı. Gerçek bir boss-HP
  sistemi istenirse **ayrı bir görev** olmalı (T11'den önce sahibiyle konuşulacak).
- **Tepki yazısının bandı yüksek yoğunluklu telefonda beşgenin üst rününe değebilir.** Band
  y 0.56–0.80; beşgen merkezi y 0.40 ve yarıçapı 100 dp, yani 400 dpi'lık bir ekranda üst rün
  y≈0.63'e çıkıyor. Ölçüldü değil, geometriden türetildi — T11'de telefonda gözle bakılmalı;
  gerekirse bandın alt sınırı ya da beşgen merkezi ayarlanır (ikisi de veri).
- **`ReactionReadout` `CombatFeel`'in canvas'ında değil, beşgenin canvas'ında** (sort 50; Feel
  katmanları 200). Vurulmada kırmızı vinyet kenardan içeri sönen bir maske olduğu için sağ kenardaki
  yazının üstüne biniyor. §10 ihlali değil (telegraf hâlâ en üstte) ama "geç kaldın" yazısının
  okunaklılığı telefonda kontrol edilmeli.
- **Toparlanma kilidi hiçbir girdiyi engellemiyor**, çünkü §5'e göre kilidi kesen üç şey (düz
  vuruş, yeni fiil, dodge) oyuncunun elindeki eylemlerin **hepsi**. Yani kilit şu an "kalan süre"
  okunabilir bir sayı + kesme becerisinin ölçüsü; mekanik olarak yalnızca `Commit`/`OnDwell`'i
  yutuyor. **T9 bunu almadı**: kalan kilit hâlâ yalnızca `SentenceDebugHud`'ın debug metninde
  ("kilit: X ms"), kalıcı HUD'da değil. Oyuncu kestiği süreyi göremediği sürece §5'in beceri
  ekseni görünmez kalıyor — T10'un paneli ya da T11'in his turu almalı.
- **Düz vuruşun kendi tezahürü yok:** `BasicStrikeDot` fiilinin normal cümle görselini kullanıyor
  (SARSINTI halka dalgası). Tek noktalık vuruşun ayrı bir silüeti/animasyonu olup olmayacağı
  spec'te yok; T11'de "vuruş mu, cümle mi" hissi karışırsa buraya bakılmalı.
- **`SentenceEngine.PublishState`, her `Tick`/`OnDotTouched`/`OnDwell`'de `SnapshotWords()` ile
  yeni bir `SentenceWord[]` allocate ediyor** (T2 kaynaklı, T7.1 denetiminde görüldü). Cümle
  kurulurken bu her karede çalışıyor (Building fazında). Sıcak yolda GC baskısı yaratabilir;
  Core/Grammar'a T7.1'de dokunma yasağı olduğu için düzeltilmedi — küçük havuzlanmış bir dizi ya
  da `Words` alanını yalnızca değiştiğinde güncellemek çözüm olabilir. T8/T9 ya da ayrı bir
  performans görevi almalı.
- **T5/T6/T6.2/T7 çok parmak / tezahür play mode'da (enjekte dokunuşla) doğrulandı, donanımda
  değil.** Gerçek dokunmatik → T11. Özellikle panik dodge (çizim parmağı + ikinci parmak diske)
  ve diskin başparmakla erişilebilirliği telefonda sınanmadı.
- Yeni eşikler (90/160/220) masa başı kararıdır, telefonda sınanmadı — T11'in his turunda
  ilk ayarlanacak sayılar bunlar.
- "Sıyırma" kelimesi §6'da hem başarılı dodge'un genel adı hem de en düşük derecenin adı;
  T9 ekrana yazarken bu çakışma karışıklık yaratabilir.
- 4. sıfat için uzatma penceresi belgede yok; 4. noktada cümle hemen kapanış üretir
  (taşan dokunuş da aynı sonucu verir).
- **`OnDotTouched`/`OnDwell` dünya saatini CatchUp ile yiyor** (T8). Eski 16 ms
  yuvarlama kapandı.
- `5-1-1` gibi **tekrar sıçraması** (§4 örneği) motorda `JumpKind.Repeat` olarak doğru
  sınıflanıyor ama cümle bağlamında testi yok.
- `History` sınırsız büyüyor; uzun dövüşte sınırlanmalı.
- **Unity Game katmanı T8.1'de derleniyor** (CS0136 kapandı). `IsExternalInit` shim ve
  `csc.rsp` Unity'de sorunsuz. Play mode T8.1'de ölçüldü.
- Arena kare (kenar 24 m) ama ayarın adı `ArenaHalfSizeM`; boss dövüşü yuvarlak arena isterse
  (T8) ad ve kırpma birlikte değişmeli.
- `PlayerSettings.runInBackground` kapalı; telefonda dert değil ama editörde odak kaybında
  play mode duruyor, MCP ile ölçüm alırken yanıltabilir.
- Vurulma sonucunda `GapMs`/`ReactionMs` doldurulmuyor (0 dönüyor). T9 vurulma ekranında
  tepki süresini göstermek isterse burayı doldurmak gerekir.
- `ExchangeResolver.IsInvulnerableAtStrike`, `DodgeState`'teki i-frame matematiğini
  ikinci kez yazıyor; ayarlar değişirse ikisi ayrışabilir.
- **Dodge yer değiştirmesi T8.1'de ölçüldü: 4.054 m** (3.8 + 0.25 kayma kuyruğu).
  Space/disk ile gözle de doğrulandı (oyun modu). Afterimage kuyruğu hâlâ gözle
  "kaç hayalet, ne kadar soluk" olarak T11 his turuna kalıyor.
- **`ExchangeOutcome.Safe` artık HUD'a "MENZİL DIŞI (derece yok)" yazıyor.** TEMİZ/SIYIRDI
  bantlarının kaçan dodge'da erişilebilirliği `BossApproachStopPadM` ile ayarlanır; şu an
  0.35 m durma payında o bantlar hâlâ hacim dışı. §7 ödülünün ne kadar erişilebilir olduğu
  telefonda ölçülmeli (T11).
- **Yavaş çekim ödülü T8.2'de gerçek oldu** (hold 900, rampUp 600; ölçüm tablosu yukarıda).
  Kelime sayısı artık tempoya göre +2'ye kadar çıkıyor, yani T9 HUD'unda "kaç nokta hakkı
  kazandın" diye gösterilecek bir şey **var**. Yeni risk: yavaş çekim 1555 ms sürüyor ve
  savunma avantajı da veriyor — T11'de ölçülmeli.
- Hece sesleri sinüs tıkırtısı; §9 yapısı var, müzikal kalite T11 his turuna.
- **Katmanlama Overlay kamera + Screen Space Camera** (T8). Pentagon sort 50, Feel/tehdit
  200. Dünya telegraf diski ana kamerada. Overlay canvas kalktı.
- Mobilde ikinci kamera fazladan bir render geçişi; T11 kare bütçesinde bakılacak.
