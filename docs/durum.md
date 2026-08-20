# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Bu dosyanın tek amacı, sıradaki
> ajanın repoyu taramadan nerede kaldığımızı anlaması. Kısa tut: ne bitti, ne üretildi,
> nerede sapma var.

**Son güncelleme:** 20 Ağustos 2026 · **Sıradaki görev:** T4

## Görev durumu

| Görev | Konu | Durum | PR |
|---|---|---|---|
| T0 | Unity 6 URP projesi, MCP, gitignore | bitti | — |
| T1 | Core assembly, ayar veri modeli, dotnet test kancası | bitti | master |
| T2 | Cümle gramer motoru | bitti | #2, master'a girdi |
| T3 | Dodge, boss frame verisi, derecelendirme | bitti | task/t3-dodge-boss-exchange |
| T4 | Zaman yönetmeni (yavaş çekim + hitstop) | bekliyor | — |
| T5 | Bootstrap sahne, kinematik hareket, sanal çubuk | bekliyor | — |
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
- `GradeTuning` — `MukemmelGapMaxMs` 110, `HarikaGapMaxMs` 200, `TemizGapMaxMs` 320
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

## Bilinen açıklar

- T1/T2/T3 `dotnet test` yeşil (`tools/CoreTests`, 30 test).
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
- **Unity bu kodu hiç derlemedi**: `Assets/Scripts` altında tek `.meta` yok, yani editör
  `Core`'u hiç import etmemiş. `SentenceEngine` nullable işaretleri kullanıyor
  (`SentenceTuning?`); Unity'nin varsayılan bağlamı kapalı olduğu için T5'te CS8632
  uyarıları beklenir (hata değil), `Assets/csc.rsp` ile susturulabilir.
