# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Bu dosyanın tek amacı, sıradaki
> ajanın repoyu taramadan nerede kaldığımızı anlaması. Kısa tut: ne bitti, ne üretildi,
> nerede sapma var.

**Son güncelleme:** 21 Ağustos 2026 · **Sıradaki görev:** T6

## Görev durumu

| Görev | Konu | Durum | PR |
|---|---|---|---|
| T0 | Unity 6 URP projesi, MCP, gitignore | bitti | — |
| T1 | Core assembly, ayar veri modeli, dotnet test kancası | bitti | master |
| T2 | Cümle gramer motoru | bitti | #2, master'a girdi |
| T3 | Dodge, boss frame verisi, derecelendirme | bitti | task/t3-dodge-boss-exchange |
| T4 | Zaman yönetmeni (yavaş çekim + hitstop) | bitti | task/t4-time-director |
| T5 | Bootstrap sahne, kinematik hareket, sanal çubuk | bitti | master |
| T6 | Beşgen girdi yüzeyi, mürekkep izi | bekliyor | — |
| T7 | Tezahür katmanı (üç rün) | bekliyor | — |
| T8 | Boss telegrafı, sıyırma, yavaş çekim, kamera | bekliyor | — |
| T9 | HUD, parlak tepki yazısı | bekliyor | — |
| T10 | Oyun içi ayar paneli | bekliyor | — |
| T11 | Android build, his turu | bekliyor | — |

Durum değerleri: `bekliyor` · `sürüyor` · `bitti` · `bloke`

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
  + §10 renkleri. **Tek örnek**: Bootstrap kendi `_tuning`'ini `MoveInput`/`KinematicMotor`/`FollowCamera`'ya
  referansla verir, kopyalamaz — T10 canlı ayarı bunu bekliyor
- Editor: **Dovus → Create Prototype Scene** (`PrototypeSceneCreator`) — `Assets/Scenes/Prototype.unity` + build settings

**Unity:** Play mode doğrulandı — arena, oyuncu/boss kapsülü, takip kamerası, WASD hareketi, çok parmak.
Sahne: `Assets/Scenes/Prototype.unity` (tek Bootstrap objesi). URP renderer düzeltildi.

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

## Bilinen açıklar

- T1/T2/T3/T4 `dotnet test` yeşil (`tools/CoreTests`, 46 test).
- **T5 çok parmak girdi katmanında doğrulandı, donanımda değil.** Play mode'da sanal
  `Touchscreen`'e iki eşzamanlı dokunuş enjekte edildi: sağ yarıdaki parmak basılıyken sol
  çubuk kuruluyor, sağ parmak gezinirken/kalkarken sol yön bozulmuyor, ölçülen hız tam
  4.50 m/s. Gerçek dokunmatik sürücüsü (palm rejection, parmak indeksi geri dönüşümü)
  yalnızca telefonda görülür → T11. Not: Device Simulator tek işaretçi ürettiği için bu
  kriteri zaten doğrulayamaz.
- Bu doğrulama tek seferlik bir prob script'iyle yapıldı, repoda test olarak durmuyor;
  T6 sağ yarıya çizimi eklerken aynı senaryo elle tekrarlanmalı.
- Yeni eşikler (90/160/220) masa başı kararıdır, telefonda sınanmadı — T11'in his turunda
  ilk ayarlanacak sayılar bunlar.
- "Sıyırma" kelimesi §6'da hem başarılı dodge'un genel adı hem de en düşük derecenin adı;
  T9 ekrana yazarken bu çakışma karışıklık yaratabilir.
- 4. sıfat için uzatma penceresi belgede yok; 4. noktada cümle hemen kapanış üretir
  (taşan dokunuş da aynı sonucu verir).
- **`OnDotTouched`/`OnDwell` `worldTimeMs` parametresini kullanmıyor**; pencere yalnızca
  `Tick(dtMs)` ile eriyor, yani dokunuş zamanı kare hassasiyetine yuvarlanıyor (60 fps'te
  ~16 ms, 300 ms'lik pencerede %5). T8'in "yavaş çekimde 4 nokta sığıyor" kriteri bu
  pencerelerin hassasiyetine dayanıyor; T6'yı yazan ajan ya parametreyi son tarih
  hesabında kullanmalı ya da imzadan kaldırmalı.
- `5-1-1` gibi **tekrar sıçraması** (§4 örneği) motorda `JumpKind.Repeat` olarak doğru
  sınıflanıyor ama cümle bağlamında testi yok.
- `History` sınırsız büyüyor; uzun dövüşte sınırlanmalı.
- **Unity Game katmanı derlendi**; play mode doğrulandı. `IsExternalInit` shim ve `csc.rsp` Unity'de sorunsuz.
- Arena kare (kenar 24 m) ama ayarın adı `ArenaHalfSizeM`; boss dövüşü yuvarlak arena isterse
  (T8) ad ve kırpma birlikte değişmeli.
- `PlayerSettings.runInBackground` kapalı; telefonda dert değil ama editörde odak kaybında
  play mode duruyor, MCP ile ölçüm alırken yanıltabilir.
- Vurulma sonucunda `GapMs`/`ReactionMs` doldurulmuyor (0 dönüyor). T9 vurulma ekranında
  tepki süresini göstermek isterse burayı doldurmak gerekir.
- `ExchangeResolver.IsInvulnerableAtStrike`, `DodgeState`'teki i-frame matematiğini
  ikinci kez yazıyor; ayarlar değişirse ikisi ayrışabilir.
