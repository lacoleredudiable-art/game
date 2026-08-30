# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Tek amacı: sıradaki ajanın repoyu
> taramadan nerede kaldığımızı anlaması. **Kısa tut.** Görev günlüğü tutulmaz — ne bitti,
> ne çalışıyor, ne açık. Uzun anlatım gerekiyorsa yeri commit mesajı ya da PR açıklaması.

**Son güncelleme:** 30 Ağustos 2026
**Faz:** alfa prototip çalışıyor · **dövüş tasarımı yeniden açıldı** (rünler ve mekanikler)
**Sıradaki:** yeni tasarım `docs/dovus-sistemi.md` §4'e yazılacak; görevler ondan doğar.
**30 Ağustos kararı:** yavaş çekim mekaniği kaldırıldı (co-op senkron kaygısı, bkz. §6).

---

## 1. Elimizde ne var

Telefonda çalışan, ayarlanabilir bir dövüş prototipi. Tek sahne, tek boss, primitive görsel
(kapsül + LineRenderer + quad), sanat varlığı yok.

Oynanış hâli: sol yarıda sanal çubukla yürüyorsun, sağ yarıda beşgen 5 noktanın üzerinde
sürükleyerek cümle kuruyorsun (ilk nokta fiil, sonrakiler sıfat), etki çizerken dünyada
yaşıyor ve elinin altında şekil değiştiriyor. Beşgenin ortası düz vuruş / erken kapanış,
dışındaki disk dodge. Boss YERE ÇAKMA'yı üç ritimde yapıyor, telegrafı okunuyor; tam
zamanında sıyırırsan hitstop + kamera yumruğu tetikleniyor. Kapanış bossun canından düşüyor,
boss ölünce çökme pozu + tam canla yeniden doğuş (**30 Ağustos:** ikisi de eskiden yavaş
çekimle sarılıydı, bkz. §6).
Ayar paneli oyunun içinde (71 satır: grup başlıkları + slider'lar), değerleri JSON'a kaydediyor.

Prototipin cevapladığı sorular: "sırtın ürperiyor mu", "yazıyor muyum yönetiyor muyum",
"canlanıyor mu" → üçü de telefonda **evet** (bu turda `dotnet test` ile doğrulandı, telefonda
tekrar ölçülmedi). "Yavaş çekim ödül mü" sorusu artık geçersiz — mekanik kaldırıldı. Açık kalan
soru iki oyuncunun aynı bossu farklı cümlelerle geçmesi; ikinci bir oyuncu gerekiyor, kodla
kapanmıyor.

## 2. Kod haritası

Mimari sözleşme `docs/teknoloji-kararlari.md` §3'te: `Core` saf C#, `Game` Unity kabuğu.

| Yer | Ne yapıyor |
|---|---|
| `Core/Tuning` | Bütün ayar verisi: `CombatTuning` (+ `.Dodge/.Sentence/.Boss/.Grade/.Feel/.Manifestation`). Hepsi `[Serializable]`, `CopyFrom`/`ResetToDefaults` taşır (panel canlı yazsın diye iç nesne **kimliği korunur**) |
| `Core/Grammar` | `SentenceEngine` — nokta dokunuşu → kelime, iptal penceresi, dwell, kapanış, `Recovering` girdi kilidi. `PentagonLayout.ClassifyJump`, `Rune` + hece |
| `Core/Combat` | `DodgeState` (i-frame + eğri), `BossAttack` (frame verisi), `SlamVariant`(+`Picker`), `ExchangeResolver` (derece/sebep), `BossVitals` |
| `Core/Manifestation` | `LivingEffect` (yaşayan etki: seyahat/morph/kapanış), `SilhouetteBuilder` (kelime → silüet eksenleri), `EffectSilhouette` |
| `Core/Time` | `TimeDirector` — dünya saati + gerçek saat, hitstop (yavaş çekim 30 Ağustos'ta kaldırıldı) |
| `Game` girdi | `PentagonInput` (çizim + merkez + dodge diski, parmak yuvaları), `MoveInput` (dinamik çubuk), `PentagonLayoutScreen` (dp→px, aynalama) |
| `Game` dünya | `PrototypeBootstrap` (sahneyi koddan kurar), `KinematicMotor`, `FollowCamera`, `BossDirector`, `BossTelegraph`, `BossReactor`, `DodgeMotion`, `PlayerVitals` |
| `Game` his | `CombatFeel` (hitstop/kamera yumruğu/vinyet), `ActorPose`, `AfterimageTrail`, `InkTrail`, `SyllableFeedback`, `GroundScarField`, `LivingEffectView` |
| `Game` HUD | `ReactionReadout`, `VitalsHud`, `RecoveryLockHud`, `DamageNumberHud` (varsayılan kapalı), `SentenceDebugHud`, `FrameTimeHud` |
| `Game` ayar | `TuningConfig` (persistentDataPath/`tuning.json`), `TuningPanel`, `TuningPresets`, `PrototypeTuning` |
| `Game/Editor` | `PrototypeSceneCreator` (Dovus → Create Prototype Scene), `AndroidBuilder` (IL2CPP/ARM64/minSDK 24) |
| `tools/CoreTests` | `dotnet test` — **78 test yeşil**. Core kaynaklarını joker ile link'ler, Unity gerekmez |
| `tools/skill-preview` | Unity'siz tasarım aracı: `runes.json`'da rün tarif et, `python3 preview.py 5-1-2-4` gramer + rejim geçişlerini anlatır |

Editör/build/telefon tuzakları: `docs/unity-notlari.md`.

## 3. Doğrulanmış / doğrulanmamış

**Doğrulanmış:** `dotnet test` 78 yeşil (30 Ağustos'ta yavaş çekim kaldırılınca 92'den düştü,
bkz. §6). Telefonda (Xiaomi/HyperOS, development build) — **bu ölçüm yavaş çekim kaldırılmadan
önceydi, tekrar edilmedi** — boşta arena 16,6 ms · 60 fps; iki parmak aynı anda sorunsuz;
panik dodge ve disk yayı; derecelendirme eşikleri (90/160/220 ms) elde ayırt ediliyor;
`tuning.json` editörde durdur/başlat ile kalıcı.

**Doğrulanmamış:**

- **Kare bütçesinin sekiz satırından yalnızca biri dolu** (boşta arena). Yürüyüş, telegraf,
  4 noktalı cümle, 30 kapanış sonrası, panel açık, açılış süresi ölçülmedi. Sanat girmeden
  doldurulmalı.
- **Yavaş çekim kaldırıldıktan sonra telefonda hiç oynanmadı.** Unity editörü/MCP bu turda
  yok; `dotnet test` yeşil ama his (hitstop tek başına yeterli mi, boss ölüm pozu artık
  gerçek zamanda ~0.85 sn — önceden yavaş çekimle uzuyordu) elde doğrulanmadı.
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
  m'de duruyor; 5.4 m'lik hacimde `gap` 197 ms'yi geçen dodge hacmin dışına çıkıyor. GENİŞ
  varyantı (8.0 m) bandı açıyor; `BossApproachStopPadM` ile de ayarlanabilir. (Eski hâliyle
  bu madde `SlowmoMinGrade` eşiğinin kaymasından bahsediyordu — o ayar 30 Ağustos'ta
  mekanikle birlikte kalktı, ama derece bandının kendisi hâlâ aynı sorunu taşıyor.)
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
- **`docs/dovus-sistemi.md`, `docs/gorev-listesi.md`, `docs/unity-notlari.md` içinde satır
  içi kalmış satır-numarası artıkları var** (ör. "önemli\n    80|önemli" gibi metnin ortasına
  gömülü "NN|" parçaları — muhtemelen bir önceki ajanın araç çıktısını kopyala-yapıştırla
  dosyaya yazmasından kaldı). Okumayı bozuyor ama anlamı değiştirmiyor. Ayrı bir görevde
  toplu temizlenmeli (regex: satır başında boşluk + `[0-9]+|` + kelime).

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

## 6. Yeniden tasarım turu (30 Ağustos) — yavaş çekim kaldırıldı

Sahibinin kararı: **yavaş çekim mekaniği tamamen kaldırıldı.** Gerekçe: co-op'ta paylaşılan
dünya saatini tek oyuncunun dodge'una göre yavaşlatmak senkron sorunu doğuruyor — herkesin
zamanı bir kişinin başarısına göre bükülmesi hem ağ hem adalet açısından pahalı. Hitstop kalıyor
(kısa, yerel, senkron sorunu taşımıyor).

**Kod tarafında yapılan:** `SlowmoTuning` silindi, `TimeDirector` yalnızca hitstop taşıyacak
şekilde sadeleştirildi, `CombatFeel`/`ManifestationDirector`/`TuningPanel`/`TuningPresets`
içindeki tüm slowmo çağrıları temizlendi. Boss ölüm akışı artık yavaş çekim bitişini beklemek
yerine doğrudan çökme pozunun süresini (`BossDeathCollapseSec`, dünya saati) bekliyor. Testler
güncellendi: `SlowmoSentenceFitTests` fixture'ı silindi, `TimeDirectorTests` hitstop-only
davranışa yeniden yazıldı — **`dotnet test` 78 yeşil.**

**Açık kalan sonuç — sahibine soru:** yavaş çekim, gerçek zamanda ulaşılamayan 4. sıfat
noktasının **tek** kapısıydı (dovus-sistemi.md §2: "dördüncü nokta ... onu ancak bedavaysa
çizersin"). Silinen `SlowmoSentenceFitTests`'in ölçtüğü şey: gerçek zamanda en hızlı
tempoda bile 3 kelime tavan (bkz. eski `Realtime_IsTheBaseline` test case'leri). Yavaş çekim
gidince **4 noktalı cümle artık pratikte hiç kurulamıyor** — kod hâlâ izin veriyor ama
pencereler (420/360/300 ms) hiçbir gerçek-zaman temposunda dördüncüye yetmiyor. Üç seçenek:
(a) böyle kalsın, 4 nokta zaten "teorik tavan" olsun, cümle ekonomisi fiilen 3 noktaya
düşsün; (b) pencereler gevşetilip 4. nokta gerçek zamanda da erişilebilir yapılsın (§3
sayıları değişir); (c) 4. noktaya başka bir kapı (ör. belirli bir rün kombinasyonu) tasarlanır.
Bu bir sayı/tasarım kararı, ajan uydurmuyor — `dovus-sistemi.md` §4.2'ye işlendi.

**Henüz konuşulmayan/belgelenmeyen açık:** sahibi "Kalahan" adlı bir boss'tan bahsetti,
"daha önce konuşulmuştu" dedi — repoda ve arşivde hiç iz yok. §4.3 (boss repertuarı) bu
detay gelmeden doldurulamaz.

**Telefonda doğrulanmadı** — bu tur yalnızca Core/`dotnet test` ile kapandı, Unity editörü
bu ortamda yok. Bir sonraki Unity dokunuşunda his tekrar ölçülmeli (özellikle boss ölüm
pozunun artık gerçek zamanda ~0.85 sn sürmesi — eskiden yavaş çekimle çok daha uzun
hissediyordu).
