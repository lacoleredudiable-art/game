# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Tek amacı: sıradaki ajanın repoyu
> taramadan nerede kaldığımızı anlaması. **Kısa tut.** Görev günlüğü tutulmaz — ne bitti,
> ne çalışıyor, ne açık. Uzun anlatım gerekiyorsa yeri commit mesajı ya da PR açıklaması.

**Son güncelleme:** 29 Ağustos 2026
**Faz:** alfa prototip çalışıyor · **dövüş tasarımı yeniden açıldı** (rünler ve mekanikler)
**Sıradaki:** yeni tasarım `docs/dovus-sistemi.md` §4'e yazılacak; görevler ondan doğar

---

## 1. Elimizde ne var

Telefonda çalışan, ayarlanabilir bir dövüş prototipi. Tek sahne, tek boss, primitive görsel
(kapsül + LineRenderer + quad), sanat varlığı yok.

Oynanış hâli: sol yarıda sanal çubukla yürüyorsun, sağ yarıda beşgen 5 noktanın üzerinde
sürükleyerek cümle kuruyorsun (ilk nokta fiil, sonrakiler sıfat), etki çizerken dünyada
yaşıyor ve elinin altında şekil değiştiriyor. Beşgenin ortası düz vuruş / erken kapanış,
dışındaki disk dodge. Boss YERE ÇAKMA'yı üç ritimde yapıyor, telegrafı okunuyor; tam
zamanında sıyırırsan yavaş çekim açılıyor ve normalde sığmayan uzunlukta cümle kurabiliyorsun.
Kapanış bossun canından düşüyor, boss ölünce yavaş çekim + çökme + tam canla yeniden doğuş.
Ayar paneli oyunun içinde (71 satır: grup başlıkları + slider'lar), değerleri JSON'a kaydediyor.

Prototipin cevapladığı sorular: "sırtın ürperiyor mu", "yavaş çekim ödül mü", "yazıyor muyum
yönetiyor muyum", "canlanıyor mu" → dördü de telefonda **evet**. Açık kalan tek soru iki
oyuncunun aynı bossu farklı cümlelerle geçmesi; ikinci bir oyuncu gerekiyor, kodla kapanmıyor.

## 2. Kod haritası

Mimari sözleşme `docs/teknoloji-kararlari.md` §3'te: `Core` saf C#, `Game` Unity kabuğu.

| Yer | Ne yapıyor |
|---|---|
| `Core/Tuning` | Bütün ayar verisi: `CombatTuning` (+ `.Dodge/.Sentence/.Slowmo/.Boss/.Grade/.Feel/.Manifestation`). Hepsi `[Serializable]`, `CopyFrom`/`ResetToDefaults` taşır (panel canlı yazsın diye iç nesne **kimliği korunur**) |
| `Core/Grammar` | `SentenceEngine` — nokta dokunuşu → kelime, iptal penceresi, dwell, kapanış, `Recovering` girdi kilidi. `PentagonLayout.ClassifyJump`, `Rune` + hece |
| `Core/Combat` | `DodgeState` (i-frame + eğri), `BossAttack` (frame verisi), `SlamVariant`(+`Picker`), `ExchangeResolver` (derece/sebep), `BossVitals` |
| `Core/Manifestation` | `LivingEffect` (yaşayan etki: seyahat/morph/kapanış), `SilhouetteBuilder` (kelime → silüet eksenleri), `EffectSilhouette` |
| `Core/Time` | `TimeDirector` — dünya saati + gerçek saat, yavaş çekim rampası, hitstop |
| `Game` girdi | `PentagonInput` (çizim + merkez + dodge diski, parmak yuvaları), `MoveInput` (dinamik çubuk), `PentagonLayoutScreen` (dp→px, aynalama) |
| `Game` dünya | `PrototypeBootstrap` (sahneyi koddan kurar), `KinematicMotor`, `FollowCamera`, `BossDirector`, `BossTelegraph`, `BossReactor`, `DodgeMotion`, `PlayerVitals` |
| `Game` his | `CombatFeel` (hitstop/slowmo/kamera yumruğu/vinyet), `ActorPose`, `AfterimageTrail`, `InkTrail`, `SyllableFeedback`, `GroundScarField`, `LivingEffectView` |
| `Game` HUD | `ReactionReadout`, `VitalsHud`, `RecoveryLockHud`, `DamageNumberHud` (varsayılan kapalı), `SentenceDebugHud`, `FrameTimeHud` |
| `Game` ayar | `TuningConfig` (persistentDataPath/`tuning.json`), `TuningPanel`, `TuningPresets`, `PrototypeTuning` |
| `Game/Editor` | `PrototypeSceneCreator` (Dovus → Create Prototype Scene), `AndroidBuilder` (IL2CPP/ARM64/minSDK 24) |
| `tools/CoreTests` | `dotnet test` — **92 test yeşil**. Core kaynaklarını joker ile link'ler, Unity gerekmez |
| `tools/skill-preview` | Unity'siz tasarım aracı: `runes.json`'da rün tarif et, `python3 preview.py 5-1-2-4` gramer + rejim geçişlerini anlatır |

Editör/build/telefon tuzakları: `docs/unity-notlari.md`.

## 3. Doğrulanmış / doğrulanmamış

**Doğrulanmış:** `dotnet test` 92 yeşil. Telefonda (Xiaomi/HyperOS, development build)
boşta arena **16,6 ms · 60 fps**; iki parmak aynı anda sorunsuz; panik dodge ve disk yayı;
derecelendirme eşikleri (90/160/220 ms) elde ayırt ediliyor; `tuning.json` editörde
durdur/başlat ile kalıcı.

**Doğrulanmamış:**

- **Kare bütçesinin sekiz satırından yalnızca biri dolu** (boşta arena). Yürüyüş, telegraf,
  4 noktalı cümle, yavaş çekim, 30 kapanış sonrası, panel açık, açılış süresi ölçülmedi.
  Sanat girmeden doldurulmalı.
- `tuning.json` kalıcılığı **gerçek APK kapat/aç** ile denenmedi.
- İki oyuncu turu hiç yapılmadı.
- `InputSystemUIInputModule` (panel) + `EnhancedTouch` (beşgen) aynı anda gerçek parmakla
  denenmedi; MCP'de `onClick.Invoke()` ile dolaylı test edildi.

## 4. Bilinen açıklar

Tasarım kararı gerektirenler `docs/dovus-sistemi.md` §4'e taşındı. Buradakiler teknik.

- **`SentenceEngine.PublishState` her `Tick`/`OnDotTouched`/`OnDwell`'de yeni
  `SentenceWord[]` allocate ediyor** (`SnapshotWords`). Building fazında her karede çalışır;
  sıcak yolda GC baskısı. Havuzlanmış dizi ya da yalnızca değişince güncelleme çözüm.
  Telefonda ölçülmedi.
- **`History` sınırsız büyüyor.** Uzun dövüşte sınırlanmalı.
- **`ExchangeResolver.IsInvulnerableAtStrike`, `DodgeState`'in i-frame matematiğini ikinci kez
  yazıyor.** Ayarlar değişirse ikisi sessizce ayrışır.
- **Vurulmada `GapMs`/`ReactionMs` 0 dönüyor.** Vurulma ekranında tepki süresi gösterilecekse
  doldurulmalı.
- **TEMİZ ve SIYIRDI bantları kaçan dodge'da erişilemez.** Dodge 4.05 m taşıyor, boss ~1.70
  m'de duruyor; 5.4 m'lik hacimde `gap` 197 ms'yi geçen dodge hacmin dışına çıkıyor, yani
  `SlowmoMinGrade = TEMİZ` ayarı fiilen HARİKA'ya kayıyor. GENİŞ varyantı (8.0 m) bandı
  açıyor; `BossApproachStopPadM` ile de ayarlanabilir.
- **Tepki yazısının bandı yüksek yoğunluklu telefonda beşgenin üst rününe değebilir**
  (geometriden türetildi, ölçülmedi). Band y 0.56–0.80, 400 dpi'da üst rün y≈0.63.
- **`ReactionReadout` beşgenin canvas'ında** (sort 50), `CombatFeel`'in katmanı 200; vurulma
  vinyeti yazının üstüne biniyor. Renk kuralını ihlal etmiyor (telegraf hâlâ en üstte) ama
  yazının okunaklılığı kontrol edilmeli.
- **Arena kare (kenar 24 m) ama ayarın adı `ArenaHalfSizeM`.** Yuvarlak arena istenirse ad ve
  kırpma birlikte değişmeli.
- **Mobilde ikinci kamera (Overlay stack) fazladan bir render geçişi.** Kare bütçesinde bakılacak.
- **`5-1-1` gibi tekrar sıçraması** motorda `JumpKind.Repeat` olarak doğru sınıflanıyor ama
  cümle bağlamında testi yok.
- **"Sıyırma" kelimesi iki işi yapıyor:** hem başarılı dodge'un genel adı hem en düşük
  derecenin adı. Ekranda karışıklık yaratıyor.
- **Hece sesleri sinüs tıkırtısı.** Yapı var, müzikal kalite yok.
- `PlayerSettings.runInBackground` kapalı; editörde odak kaybında play mode duruyor, MCP ile
  ölçüm alırken yanıltır.

## 5. Yeniden tasarım turu (29 Ağustos)

Sahibi rünleri ve mekanikleri yeniden tasarlıyor. **Prototip kodu duruyor** — silinen bir şey
yok, değişiklikler onun üstüne gelecek.

Bu turda yapılan tek şey **sadeleştirme**:

- Görev-görev tutulan 1700 satırlık günlük arşive alındı → bu dosya kısa bir durum notu oldu.
- `docs/dovus-sistemi.md` **sıfırlandı**: v1 spesifikasyonu arşivde; yeni belge pazarlıksız
  kısıtları, kodda çalışan mekaniklerin tarifini, bütün sayıları ve **boş** bir tasarım
  bölümünü tutuyor. Rün seti, cümle ekonomisi, boss repertuarı ve run yapısı orada açık.
- Editör/build/telefon tuzakları `docs/unity-notlari.md`'ye çıkarıldı (görev günlüğünün
  içinde kaybolmuşlardı).
- Kapanmış görev listesi ve his kontrol listesi arşive alındı; `docs/gorev-listesi.md` yalnızca
  çalışma düzenini tutuyor, görev tablosu boş.

`docs/arsiv/` **bağlayıcı değildir** ve okunmaz — orada yazan bir karar, buradaki ya da
`dovus-sistemi.md`'deki bir kararla çelişirse geçerli olan yeni belgedir.
