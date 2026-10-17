# 10 — Ölçüler ve yapım standartları

Sürüm 1.0 · 12 Ekim 2026 · Kullanıcı: Mustafa

Bu belge oyunun kural dışındaki bütün ölçülerini tutar: karakter, silah, animasyon, hareket, kamera, boss,
arena, sanat yönü, efekt, dövüş hissi, ses, ekran, teknik, çok oyunculu, asset hattı ve oyun akışı. Kurallar
(fiil, sıfat, 144 hücre, ritim, menzil) `01`–`05`'tedir; bu belge onları tekrar etmez, gerektiğinde bölüm
numarasıyla işaret eder.

## 0. Nasıl okunur

**Kimlik.** Her maddenin bir kimliği var (ör. `O-5.3`). PR açıklamasında bu kimlik yazılır.

**Kaynak sütunu.**
- `KOD`: bugün oyunda çalışan değer (dosya adı yanında). Belge bu değeri onaylar.
- `BELGE`: `01`–`09` belgelerinde zaten yazılı olan değer.
- `ÖNERİ`: Claude önerisi; telefonda denendikten sonra kullanıcı onaylar ya da değiştirir. Onaylanana kadar
  kod bu değeri kullanır; ajan kendi değerini uydurmaz.
- `KOD→ÖNERİ`: kodda bir değer var ama bu belge onu değiştiriyor (gerekçe satırda).

**Doğrulama sütunu.** Maddenin nasıl denetleneceği:
- `T` otomatik test (Core testi, saniyeler sürer; sayı ve kural denetimi).
- `G` Gözcü (oyun çalışırken ihlal olursa kırmızı log: "oyuncu zeminin altında", "kamera boss'un içinde").
- `K` görsel kontrol sayfası (contact sheet: belirli pozlarda otomatik ekran görüntüsü, gözle bakılır).
- `E` telefonda elle deneme (his, okunurluk).

**Tek kaynak kuralı.** Bir ölçü kodda tek bir yerde durur. Sahne dosyası (`Prototype.unity`), telefondaki
kayıtlı ayar dosyası (`tuning.vNN.json`) ya da başka bir JSON aynı ölçünün ikinci bir değerini taşıyamaz. 12
Ekim incelemesinde bulunan çift değerler §18'de listelidir; her biri tek değere bağlanacak.

**Birimler.** Uzunluk metre (m), süre saniye (s), açı derece (°), ekran ölçüsü dp (1 dp = 1/160 inç ≈ 0,16 mm).
Unity'de 1 birim = 1 m.

---

## 1. Karakter

### 1.1 Gövde ölçüleri (model ve Tripo/Blender için)

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-1.1 | Boy (ayak tabanından başın tepesine) | 1,78 m | KOD `PlayerSettings.PlayerVisualHeightM` | T (modelin sınır kutusu 1,74–1,82 m), K |
| O-1.2 | Oran | 7,4 kafa (kafa yüksekliği 0,24 m). Stilize ama gerçekçiye yakın; çocuksu (5–6 kafa) değil | ÖNERİ | K |
| O-1.3 | Göz hizası | 1,66 m | ÖNERİ | K |
| O-1.4 | Omuz hizası / omuz genişliği | 1,46 m / 0,46 m | ÖNERİ | K |
| O-1.5 | Kalça (bacak başlangıcı) hizası | 0,92 m | ÖNERİ | K |
| O-1.6 | Kol boyu (omuz ekleminden bilek eklemine) | 0,60 m | ÖNERİ | K |
| O-1.7 | El boyu (bilekten orta parmak ucuna) | 0,19 m | ÖNERİ | K |
| O-1.8 | Ayak boyu | 0,27 m (≈ 42–43 numara) | ÖNERİ | K |
| O-1.9 | Kol açıklığı (iki kol yana açık, parmak ucundan parmak ucuna) | 1,78 m (boya eşit) | ÖNERİ | K |
| O-1.10 | Pivot (modelin sıfır noktası) | İki ayağın arasında, zemin hizasında (Y = 0) | ÖNERİ | T (pivot Y ile modelin en alt noktası arası ≤ 1 cm) |
| O-1.11 | İskelet | Unity Humanoid; T-poz ya da A-poz (kollar 45°); kemik adları Mixamo standardı | KOD (k3 Humanoid) | T (Avatar geçerli, bütün zorunlu kemikler eşlenmiş) |

### 1.2 Çarpışma ve vurulma

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-1.12 | Çarpışma kapsülü yarıçapı | 0,35 m | BELGE (`06` §5.4), KOD `world_physics.body_radius_player_m` | T |
| O-1.13 | Çarpışma kapsülü yüksekliği / merkezi | 1,80 m / Y = 0,90 m | ÖNERİ | T |
| O-1.14 | Vurulma bölgesi (hurtbox) | Çarpışma kapsülüyle aynı; kafa, kol ayrı bölge yok (kritik vuruş yok) | ÖNERİ | T |
| O-1.15 | Görsel ile kapsül uyumu | Bekleme pozunda modelin gövdesi kapsülün dışına en çok 0,10 m taşar (silah hariç) | ÖNERİ | K |

### 1.3 Yürüme

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-1.16 | Tam hız (joystick sonuna kadar) | 6,4 m/s | KOD `WalkSpeedMps` | T |
| O-1.17 | En düşük hız (joystick ölü bölgeyi geçtiği an) | KALDIRILDI (15 Ekim [K]): ölü bölge üstü tam hız, analog yok | KOD `MinStickSpeedFrac` kullanılmıyor | K |
| O-1.18 | Hızlanma / yavaşlama | 52 m/s² (0 → tam hız 0,12 s) / 64 m/s² (tam hız → 0, 0,10 s) | KOD `MoveAccelMps2`, `MoveDecelMps2` | T |
| O-1.19 | Dönme hızı | 720 °/s (geri dönüş 0,25 s) | KOD `TurnRateDegPerSec` | T |
| O-1.20 | Kök hareketi (root motion) | **Kapalı.** Karakteri kod taşır; bütün klipler yerinde oynar (in-place). Klipteki kök yüksekliği kilitli | KOD (`lockRootHeightY`) | T (klipte kök XZ yer değiştirmesi ≤ 2 cm) |
| O-1.21 | Ayak kayması (foot sliding) kuralı | Koşu klibinin oynatma hızı = gerçek hız ÷ klibin bir döngüde kat ettiği mesafe × döngü süresi. Bir adımda ayak yerde en çok 5 cm kayar. Oynatma hızı 0,6–1,5× dışına çıkarsa ayrı yürüme klibi kullanılır | ÖNERİ (`LocoMaxPlaybackMult` 1,5 KOD) | K (yandan, yere yakın kamerayla 2 s kayıt) |
| O-1.22 | Adım sesi aralığı | 2,2 m'de bir (oyuncu), 2,4 m'de bir (boss) | KOD `FootstepStrideM` | E |
| O-1.23 | Zeminle ilişki | Ayak tabanı her karede zemin düzleminin 0–3 cm üstünde. Zeminin altına inmek yasak (dodge, takla, itilme, sendeleme dahil) | ÖNERİ | G (her kare: ayak Y < −0,05 m ise kırmızı) |

### 1.4 Okunurluk

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-1.24 | Ekrandaki boy | Varsayılan kamerada karakter ekran yüksekliğinin %18–25'i | ÖNERİ | K |
| O-1.25 | Silüet | 10 m uzaktaki dost karakter, silahı ve takım rengi ayırt edilebilir. Karakter rengi ortam renginden en az %30 parlaklık farkıyla ayrılır | ÖNERİ | K |
| O-1.26 | Takım rengi | Her oyuncunun bir halkası (ayak altında, 0,6 m çap, %40 opak) ve skill kenar rengi: 1 kor turuncu, 2 buz mavisi, 3 yeşim yeşili, 4 altın, 5 mor | ÖNERİ | K |

---

## 2. Silah

### 2.1 Fiziksel boyut ve tutuş

Silahın fiziksel boyu **görüntüdür**, oyun kuralı değildir. Menzil (`04`) değişmez: kılıç 1,05 m olsa da
menzili 6 m'dir. Silahlar mobil kamerada okunsun diye gerçek ölçüden biraz büyüktür.

| Kimlik | Silah | Boyut | Tutan el | Tutuş noktası | Kaynak |
|---|---|---|---|---|---|
| O-2.1 | Yumruk | Sargılı eldiven: el boyu 0,22 m, yumruk genişliği 0,12 m | İki el | — (elin kendisi) | ÖNERİ |
| O-2.2 | Yay | Boy 1,45 m; ok 0,75 m | Sol el yayı, sağ el kirişi | Yayın orta noktası (kabza) | ÖNERİ |
| O-2.3 | Kitap | 0,30 × 0,22 × 0,06 m (açıkken 0,44 m genişlik) | Sol el, açık tutar | Sırtın ortası | ÖNERİ |
| O-2.4 | Kılıç | Toplam 1,05 m: bıçak 0,80 m (genişlik 0,04 m), kabza + balçak 0,25 m. Kın yok (`06` K 21:05) | Sağ el | Kabzanın üst üçte biri (balçağa 4 cm) | ÖNERİ |
| O-2.5 | Küre | Çap 0,16 m | Sol avuç | Kürenin merkezi avucun ortasında | ÖNERİ |
| O-2.6 | Çekiç | Sap 1,00 m; baş 0,36 × 0,20 × 0,20 m | İki el: sağ el sap ucuna 0,10 m, sol el sağ elin 0,35 m üstü | Sap ucundan 0,10 m | ÖNERİ |
| O-2.7 | Top (el topu) | Namlu 0,75 m, ağız çapı 0,16 m | Sağ el kabzada, sol el namlu altında | Kabza | ÖNERİ |
| O-2.8 | Asa | Boy 1,75 m; uçta 0,15 m kristal | Sağ el | Alt uçtan 1,05 m (göğüs hizası) | ÖNERİ |
| O-2.9 | Tılsım | Mühür kâğıdı 0,08 × 0,20 m | Sağ el, işaret ve orta parmak arası | Kâğıdın alt üçte biri | ÖNERİ |
| O-2.10 | Kalkan | Yuvarlak, çap 0,80 m, kalınlık 0,05 m | Sağ kol (tek silah olduğu için) | Ön kolun ortası, kayış ve tutamak | KOD (`ShieldRight`) + ÖNERİ |

### 2.2 Tutuş standardı (bilek sorununun çözümü)

Bugün silahlar ele elle girilmiş açılarla bağlanıyor (`WeaponGripViewDefaults`: ör. kalkan 8° / 195° / 92°).
Bu yöntem her yeni model ya da animasyonda bozulur. Yerine şu standart gelir:

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-2.11 | Silah modelinin sıfır noktası | Tutuş noktası (§2.1 son sütun) | ÖNERİ | T |
| O-2.12 | Silah yerel eksenleri | +Y: uca doğru (bıçak ucu, asa kristali, namlu ağzı, sap başı). +Z: vuruş yüzü (keskin kenar, çekiç yüzü, namlu altı değil ağzı). +X: sağ el kuralıyla | ÖNERİ | T (modelin en uzak noktası +Y yönünde) |
| O-2.13 | El soketi | `RightHand` ve `LeftHand` kemiklerinin altında `Grip_R` / `Grip_L` boş nesnesi; avuç ortasında, parmaklar kapalıyken silah sapının geçeceği çizgide. Silah sokete sıfır konum ve sıfır açıyla bağlanır | ÖNERİ | T (bağlı silahın yerel konumu ve açısı sıfır) |
| O-2.14 | Bilek açısı sınırı | Bekleme ve yürümede bilek bükülmesi (ön kol eksenine göre) en çok 25°; saldırı pozlarında en çok 45° | ÖNERİ | K, G (her kare ölçülür, aşılırsa sarı log) |
| O-2.15 | Tek elli silahta sap açısı | Bekleme pozunda ön kol ile sap arası 70–110° | ÖNERİ | K |
| O-2.16 | İki elli silah | İkinci el IK ile sapa oturur (Two Bone IK; `com.unity.animation.rigging` paketi eklenir). Bugün IK yok | ÖNERİ | K |
| O-2.17 | Silah ele girmiyor / içinden geçiyor | Silah karakterin gövdesine en çok 3 cm gömülebilir (bekleme pozunda) | ÖNERİ | K |

### 2.3 Taşıma, değiştirme, iz

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-2.18 | Taşıma | Silah hep elde; sırtta/belde taşıma yok. Dilimde iki silah var (Kılıç, Yay); elde olmayan görünmez | ÖNERİ | K |
| O-2.19 | Silah değiştirme | 1,2 s; ilk 0,3 s eski silah söner (kor dağılır), son 0,3 s yeni silah belirir. Değiştirme sırasında yürünür, skill ve dodge yok | BELGE (v2) + ÖNERİ | T |
| O-2.20 | Kılıç izi (trail) | Bıçağın balçaktan 0,10 m yukarısından uca kadar; ömür 0,12 s; yalnız vuruş penceresinde açık | ÖNERİ (`07` §4) | K |

---

## 3. Animasyon

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-3.1 | Kare hızı | Klipler 30 fps üretilir, oyunda ara karelerle oynar | ÖNERİ | T |
| O-3.2 | Evreler | Her saldırı klibinde üç evre: hazırlık, vuruş (aktif), toparlanma. Hazırlık = silahın ön süresi, toparlanma = silahın toparlanması (`04` ritim tablosu). Ör. Kılıç VUR: 0,20 s hazırlık + vuruş karesi + 0,20 s toparlanma | BELGE (`04`) | T (klip olay karelerinin zamanı ritim tablosuyla ±1 kare) |
| O-3.3 | Olay işaretleri (event) | Her saldırı klibinde `Hit` (etkinin çıktığı kare), `FxStart`, `Step_L`/`Step_R` (adım). Yumruk'ta `Hit1–Hit3`. Etki zamanını klip değil kural motoru belirler; `Hit` karesi buna uyacak şekilde klip hızı ayarlanır | ÖNERİ | T |
| O-3.4 | Geçiş (blend) süresi | Oyuncu 0,06 s; boss 0,15 s; dodge'a geçiş 0,03 s (anında hissettirsin) | KOD `AnimCrossFadeSec`, `BossAnimCrossFadeSec` + ÖNERİ | T |
| O-3.5 | Kesme kuralları | Toparlanma her zaman dodge ile kesilir. Ön süre dodge ile kesilirse skill atılmaz; mana ve cooldown harcanır, **iade yok** ([K] Mustafa, 16 Ekim: "yanlış zamanda skill atmamalıydın"; `13` §9'daki "CD yanmaz" [Ö] reddedildi). Etki fazında da dodge skill'i hemen keser, etki geçtiyse cooldown yanar ([K] 16 Ekim; `13` §9 "etki kesilmez" [Ö] reddedildi). Hareket skilleri birbirini keser (`06` §1) | ÖNERİ + BELGE | T |
| O-3.6 | Üst gövde / alt gövde | Ön sürede yürünebilen silahlarda (Yumruk, Kitap) üst gövde saldırır, alt gövde yürür. Hız 0,15 m/s'nin altındaysa tam gövde klibi | KOD `UpperBodyCastMinSpeed` | K |
| O-3.7 | Bekleme (idle) | 1 temel + 2 çeşitleme (8–14 s arasında rastgele); silah başına ayrı duruş | ÖNERİ | K |
| O-3.8 | Darbe alma | Hafif darbe: 0,25 s, yalnız üst gövde, hareketi kesmez. Ağır darbe (boss saldırısı ≥ canın %20'si): 0,5 s, tam gövde, girdi kilitli değil (dodge keser) | ÖNERİ | K, E |
| O-3.9 | Yere düşme ve kalkma | Yalnız itme ≥ 3 m olduğunda; düşme 0,4 s + kalkma 0,6 s; kalkmanın ilk 0,3 s'si dodge ile kesilebilir | ÖNERİ | K |
| O-3.10 | Ölüm ve diriltilme | Ölüm 1,2 s (son pozda kalır); diriltme 1,0 s ayağa kalkış | ÖNERİ | K |
| O-3.11 | Durum makinesi | Geçiş tablosu `06` klip listesinden üretilir; her durumdan dodge'a geçiş vardır (ölüm hariç; skill'in hazırlık, etki ve toparlanma fazı dahil, [K] 16 Ekim; yalnız silah değiştirme ve dodge'un kendisi hariç, O-2.19); her durumdan ölüme geçiş vardır | ÖNERİ | T (tablo eksiksiz: her klibin giriş ve çıkışı tanımlı) |
| O-3.12 | Ayak IK | Arena zemini düz olduğu için dilimde yok. Eğimli zemin gelirse açılır | ÖNERİ | — |

---

## 4. Hareket ve kontrol

### 4.1 Joystick

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-4.1 | Joystick yarıçapı | 72 dp (≈ 11,5 mm) | KOD `JoystickMaxRadiusDp` | E |
| O-4.2 | Ölü bölge | 0,12 (yarıçapın %12'si); geçilince hız tam, yalnız yön okunur | KOD `JoystickDeadZone` | T |
| O-4.3 | Yer | Sol başparmak; parmağın ilk değdiği yerde belirir (yüzen joystick), yalnız ekranın solundan %35 genişlik ve alttan %60 yükseklikte (`JoystickZoneWidthFrac`/`JoystickZoneHeightFrac`); bölge dışı = kamera sürükleme + hedef dokunuşu | ÖNERİ | E (başlangıç tahmini; %55–60 telefonda denenir) |

### 4.2 Dodge

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-4.4 | Bir basış = zıplama | Joystick yönünde (joystick boşsa karakterin baktığı yön) 3,8 m, 0,25 s, yay şeklinde. Yükseklik **yalnız klipte** (tepe 0,6 m); çarpışma kapsülü zeminde kalır | KOD `DodgeTuning.DistanceM`, `DurationMs` + BELGE (`06` §5) | T, G |
| O-4.4a | İki faz | Dodge iki fazlıdır: **patlama** (ilk 0,25 s, mesafenin ~%75'i, kamera hafif hareket bulanıklığı) + **süzülme** (0,40 s, yavaşlayarak, kalan ~%25). Süzülme bitince doğrudan bekleme pozu; iniş sarsıntısı yok. Bugünkü süzülme kuyruğu 0,12 s (`GlideTailMs`) → 0,40 s. Referans: Dragon Nest 2:37,6 kare analizi (patlama 0,27 s, süzülme 0,40 s) | KOD→ÖNERİ (onaylı K 12 Ekim) | T, K |
| O-4.5 | Havadayken ikinci basış = takla | İlk basışın hasarsızlık penceresi açıkken (birleşme penceresi = `T_tek`, test değeri 0,5 s; `15` §3.2); toplam mesafe ×1,6 (6,1 m), zincir penceresi `T_zincir` (test değeri 1,3 s, birinci basıştan; `15` §3.3 Model A [K]); takla klibi havada döner, ayaklar zeminin altına inmez | KOD `DoubleTapWindowMs`, `CombinedDistanceMult`, `CombinedDurationMult` + BELGE | T, G, K |
| O-4.6 | Hasarsızlık penceresi | Tek basış `T_tek`: test değeri 0,5 s; zincir `T_zincir`: test değeri 1,3 s (`15` §3.4 [K] 14 Ekim; eski 0,26 / 0,52 s yerine geçti, kesin değer Aşama 8 ayar masasında) | KOD `IframeMs`, `CombinedIframeMs` | T |
| O-4.6a | Hasarsızlık görünür | I-frame penceresi boyunca karakterin kenarında kor turuncusu parlama (0,05 s'de açılır, pencere bitince 0,05 s'de söner); Hareket skillerinin yolculuğunda da aynı parlama. Referans: Dragon Nest dodge, 1. fazda altın parlama | ÖNERİ (onaylı K 12 Ekim) | K |
| O-4.7 | Hak ve dolum | 2 hak; her biri 6 s'de dolar. **Hak iadesi yoktur** (engele çarpıp yol alamayan dodge dahil; [K] Mustafa, 16 Ekim: "geri iade yok, yaptığının sonucuna katlanırsın") | KOD `MaxCharges`, `ChargeRechargeMs` | T |
| O-4.8 | ~~Kusursuz dodge~~ **[RED]** | Kaldırıldı: hak iadesi, ×1,3 vuruş, ağır çekim yok ([K] 13 Ekim, `13` §3). Eski satır: boss vuruşuna 0,15 s kala dodge → hak iadesi + ×1,3 + 0,2 s ağır çekim | ~~KOD `PerfectWindowMs` vb.~~ (kodda silinecek) | T, E |
| O-4.9 | Engeller | Dodge duvardan, sütundan, kozadan, boss gövdesinden ve canlı gövdeden (minyon, dost) geçemez ([K] Mustafa, 15 Ekim; minyon ve dost için gövde silindiri yürümeyle aynı: yarıçaplar toplamı, durur, kaymaz); çarpınca durmaz, yüzey boyunca kayar. Kayma bütçesi (yol uzunluğu) = R × max(t, `SlideMinRatio`); R = çarpma anında kalan yol, t = çubuk yönünün yüzeye teğet bileşeni (0-1). Taraf: t ≥ `EngineChoiceT` ise çubuğun teğet işaretinden (oyuncu seçer, tehlike alanı hesaba katılmaz); değilse motor `SlideScanPadM` kadar ileri tarayıp daha boş tarafı seçer; aktif alan telegraph'ları (slam, koni, atılış, zehir ağı alanı; mermi ve volley hariç; pencere her saldırının kendi windup'ı başlayınca) dolu sayılır; iki taraf da eşitse saat yönü. Kayma `SlideStepMs` sabit adımlarla yüzeyi takip eder (adım başına en çok `SlideMaxIter` iterasyon), kayma boyunca `EdgeGapM` korunur, taraf dodge boyunca kilitlidir. Bir adımda gerçek ilerleme beklenenin `SlideStallRatio` oranından azsa (köşe) kayma durur. i-frame `IframeMs` kadar sürer, hareketten bağımsızdır; hak her durumda yanar. "Tam 90°" ayrı kural değil, `EngineChoiceT` bandının içinde kalır | KOD `EdgeGapM` + K (tasarımcı kararı); `SlideMinRatio` 0,5, `EngineChoiceT` 0,17, `SlideScanPadM` 0,5 m, `SlideMaxIter` 4, `SlideStallRatio` 0,1 = (ölçüm) test başlangıç değeri, tahmin; `SlideStepMs` değeri yok (ölçüm); yeri `*Defaults` | G (her kare: oyuncu kapsülü bir engelin içinde ise kırmızı; Gözcü `oyuncu_govde_icinde` = merkez minyon/dost/boss silindirinde, `oyuncu_isinlanma` = dodge/kalıp sahibi yokken kare adımı > yürüme hızı × dt × 2); T (fps'ten bağımsız: sabit adımla aynı sonuç) |
| O-4.10 | After-image (iz görüntüsü) | Yalnız süzülme fazında, gerçek yol üzerinde, oyuncunun arkasında; 4 kopya, 0,15 s ömür, %55 opaklık. İz oyuncunun önüne düşmez | KOD `AfterimageAlpha` + ÖNERİ | K |
| O-4.11 | Arena sınırı | Dodge, takla, Hareket skilleri ve itilme arena duvarının (§7) içinde kalır; sınır daire şeklindedir, kare değil | ÖNERİ | G |

### 4.3 Girdi zamanlaması

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-4.12 | Girdi tamponu | Toparlanma sırasında verilen skill/dodge girdisi 0,15 s saklanır, toparlanma bitince çalışır | ÖNERİ | T |
| O-4.13 | Temel vuruş | Altıgenin merkezine kısa dokunuş (hareket ≤ 12 dp) | KOD `TapMaxMoveDp` | T |
| O-4.14 | Temel vuruş zinciri | 1,2 s içinde tekrar dokunulursa sonraki vuruş klibi. Kılıç: 3 kesiş (`Kilic_VUR`, `Kilic_VUR_b`, `Kilic_VUR_c`; her biri 0,40 s, vuruş karesi 0,20 s). Bugün yalnız `Kilic_VUR` var; diğer ikisi gelene kadar aynı klip tekrar oynar. Yay: `Yay_VUR` (tek atış) | KOD `BasicStrikeComboResetSec`, `klipler.csv` + ÖNERİ | T |
| O-4.15 | Hedef seçme | `09` [K] hedef seçme kuralı (0,2 s basılı tutma) | BELGE | T |

---

## 5. Kamera

12 Ekim videosundaki sorunlar: boss'a yakınken dönme, takla sırasında kameranın yer altına/havaya fırlaması,
boss saldırısının ekranı kaplaması. Aşağıdaki değerler bunları kapatır.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-5.1 | Uzaklık (karakterden kameraya) | 6,0 m | KOD→ÖNERİ (kod varsayılanı 5,85, sahne 3,4; 3,4 m boss'u kadraja sığdırmıyor) | T, E |
| O-5.2 | Bakış açısı (aşağı eğim) | 30° (yüksek omuz üstü; boss'un ön işaretleri yerde okunur) | KOD→ÖNERİ (kod 21°) | E |
| O-5.3 | Bakış noktası | Karakterin 1,2 m yüksekliği (göğüs) | KOD→ÖNERİ (kod 0,38 m) | T |
| O-5.4 | Görüş açısı (FOV, dikey) | 55° | KOD→ÖNERİ (kod 54, sahne 56; tek değer) | T |
| O-5.5 | Takip yumuşaklığı | 0,12 s | KOD `FollowSmoothTimeSec` | E |
| O-5.6 | Dönüş hız tavanı | 120 °/s (kamera bir karede bundan hızlı dönemez) | ÖNERİ | G (bir saniyede > 120° dönüş kırmızı) |
| O-5.7 | Boss'a yakınken | Oyuncu boss'un gövde kenarına 2 m'den yakınsa kamera yönü (yaw) donar; 2–5 m arasında yumuşak geçiş; 5 m'den uzakta normal takip: donarken biriken fark geri alınır, ama O-5.6 tavanının altında ve oyuncu kadrajdan çıkmadan (yavaşça döner) [K 2026-10-15]. Sebep: boss merkezine çok yakınken hedef yönü her karede ters dönüyor | ÖNERİ | G, E |
| O-5.8 | Hedef kilitliyken kadraj | Karakter ekranın alt üçte birinde, yatayda %40'ta; boss ekranın ortasında. Kilit mesafesi 5,2–8,8 m arasında oyuncu-boss mesafesine göre açılır | KOD `CameraLockOn*` + ÖNERİ | K |
| O-5.9 | Omuz değişimi | Kilitliyken kamera omzu (sağ/sol 1,1 m) en çok 1,5 s'de bir değişir | KOD `CameraLockOnShoulderSideM` + ÖNERİ | G |
| O-5.10 | Duvar arkası | Kamera ile karakter arasına duvar girerse 0,05 s'de öne gelir, engel kalkınca 0,35 s'de geri çekilir; en yakın 1,2 m. Sütun arada kalırsa kamera öne gelmez, mesafesini korur; sütun O-5.11 opaklığıyla (%35) saydamlaşır, kamera sütunun içine girmez [K 2026-10-15; önceki [Ö] "engelin önünde, karaktere yakın" kamerayı karakterin kafasına sokuyordu] | KOD `CameraCollision*`, `CameraSliceGuard` | G (`oyuncu_engel_arkasinda` her kare; sütun sayılmaz) |
| O-5.11 | Boss arada kalırsa | Boss gövdesi kamera ile karakter arasındaysa kamera öne gelmez; boss modeli %35 opaklığa iner | ÖNERİ | K |
| O-5.12 | Elle döndürme | Ekranın sağ yarısında boş bir yere sürükleme: 0,35 °/dp; eğim −8° ile 35° arası; ters eğim ayarı var | KOD `OrbitDegreesPerDp`, `CameraPitchMin/MaxDeg` | E |
| O-5.13 | Sarsıntı | Yalnız yerel oyuncu; genlik = 0,05 × silahın ritim çarpanı (kamera metresi), süre 0,1 s; boss büyük saldırısında 0,12 m, 0,25 s. Ayarlardan kapatılabilir | BELGE (`07` §4.1) + KOD `BossShakeAmpBaseM` | E |
| O-5.14 | Boss saldırı hazırlığında | Kamera 0,28 s'de ×1,42 geri çekilir (saldırı alanı kadraja girer), saldırı bitince döner | KOD `CameraWindup*` | E |
| O-5.15 | Yasaklar | Kamera zeminin altına inemez (Y ≥ 0,3 m), boss gövdesinin içine giremez, arena duvarının dışına çıkamaz | ÖNERİ | G (her kare kırmızı) |
| O-5.16 | Uzak kırpma | 320 m (uzak manzara görünsün) | KOD `CameraFarClipM` | — |
| O-5.18 | Yüksek baş | Boss'un başı 10 m'den yüksekteyse kilit bakış noktası baş yerine göğsün ortasıdır (Kül Ejderi faz 2, KE-6.3) | ÖNERİ | K |
| O-5.17 | Ölüm kamerası | Oyuncu ölünce kamera 1,5 s'de yükselir (eğim 50°, uzaklık 9 m), ölen oyuncuya bakar; diriltilince geri döner | ÖNERİ | E |

---

## 6. Boss

### 6.1 Her boss için doldurulacak şablon

Her yeni boss bu alanların hepsini doldurmadan koda girmez.

| Alan | Açıklama |
|---|---|
| Boy, gövde şekli | Yükseklik (m), ayak/bacak sayısı, yandan ve önden ölçüler |
| Çarpışma | Kapsül(ler): yarıçap, yükseklik; bacaklar ayrı mı |
| Vurulabilir bölge | Hangi bölgeler hasar alır (dilimde tek bölge) |
| Hareket | Yürüme hızı (m/s), dönme hızı (°/s), oyuncuya yaklaşma mesafesi |
| Her saldırı | Ad · ön işaret süresi (s) · ön işaret görünümü (yer işareti şekli, renk, ses) · aktif süre · alan şekli ve ölçüsü · hasar (oyuncu canının %'si) · denge · kesilebilir mi · sonrası açık kalma süresi (s) · hangi fazda |
| Hedef seçme | Kime saldırır (en yakın / en çok hasar veren / rastgele, ağırlıklar) |
| Fazlar | Can eşikleri, geçiş süresi, her fazda açılan saldırılar |
| Öfke (enrage) | Kaç saniye sonra, ne değişir |
| Sendeleme | Denge çubuğu dolunca süre ve görünüm |
| Ölüm | Süre, görünüm |

### 6.2 Genel boss kuralları

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-6.1 | Ön işaret en kısa süre | 0,6 s (mobilde tepki süresi + dodge çıkışı). Kesilebilir saldırılarda kesme penceresi bu süre içindedir | ÖNERİ | T |
| O-6.2 | Ön işaret görünümü | Yer işareti: kenar çizgisi baştan tam boy, içi saldırı anına doğru dolar. Renk: koyu kırmızı (#C21E2B) kenar, %35 opak dolgu; hiçbir oyuncu efekti bu rengi kullanamaz | ÖNERİ | K |
| O-6.3 | Ön işaret katmanı | Boss yer işareti bütün oyuncu efektlerinin üstünde çizilir (her zaman görünür) | ÖNERİ | K |
| O-6.4 | Saldırı sonrası açık kalma | Her saldırıdan sonra 0,8–1,5 s boss saldırmaz (oyuncunun vurma penceresi) | ÖNERİ | T |
| O-6.5 | Dönme hızı | Yürüyen boss'lar için 90 °/s (oyuncu dodge ile arkasına geçebilsin; Kül Ejderi yürümez, baş dönüşü KE-2.7) | KOD→ÖNERİ (kod 240 °/s; bu hızda arkaya geçmek mümkün değil) | E |
| O-6.6 | Kesilemez saldırı | Normal saldırılardan boss başına 1–2 tane; büyük duyurulu saldırılar her zaman kesilemez. Ön işaretinde altın kenar (#E8B04A, SI-2.16) | ÖNERİ | K |
| O-6.7 | Oyuncu sayısı ölçeği | Boss canı × (1 + 0,8 × (oyuncu sayısı − 1)) | ÖNERİ (`eksik-parametreler`) | T |
| O-6.8 | Öfke | Hedef dövüş süresinin 1,7 katında (180 s hedefse 300 s): saldırılar arası açık kalma yarıya iner | ÖNERİ | T |
| O-6.9 | Can ve hasar ölçeği | Boss canı ve saldırı hasarları `kural-motoru-v4.json` → `scale` ölçeğindedir (oyuncu 1000). Boss JSON'undaki eski 120/22 ölçeği kaldırılır | ÖNERİ (§18) | T |

### 6.3 Ağların Kraliçesi (dilim boss'u)

> **Kalkıyor (12 Ekim, karar A3).** Yerine Kül Ejderi geçer: `18-kul-ejderi.md`. Bu bölüm Kül Ejderi koda
> girene kadar bugünkü kodu anlatır; yeni iş bu değerlere göre yapılmaz.

Bugünkü değerler `Resources/Bosses/aglarin-kralicesi.json`'dan. Hasarlar v4 ölçeğine çevrildi (oyuncu canı
1000). Boss tasarım oturumunda yeniden bakılacak; o zamana kadar bunlar geçerli.

**Ölçüm tanımı (13 Ekim, K):** Koni/yelpaze açısı TAM açıdır (eksenden ± yarısı); boss saldırı mesafeleri boss
MERKEZİNDEN ölçülür.

| Kimlik | Madde | Değer | Kaynak |
|---|---|---|---|
| O-6.10 | Boy / gövde | 5,0 m yükseklik; dev örümcek, 8 bacak; gövde yarıçapı 2,5 m | KOD `BossVisualHeightM` + BELGE |
| O-6.11 | Can / denge | 15000 / 300 denge (oyuncu sayısıyla ölçeklenir) | KOD `scale.boss_max_hp`, `boss_poise_bar` |
| O-6.12 | Yürüme | 1,4 m/s (klip hızı) | KOD `BossWalkClipMps` |
| O-6.13 | Fazlar | 1. faz "Örücü" %100–50: Bacak Çakması, Ağ Tükürüğü, Ağ Örme. 2. faz "Avcı" %50–0: + Zehir Nefesi, Sıçrayış; Ağ Tükürüğü 5 mermi. Geçiş 2 s | KOD |
| O-6.14 | Bacak Çakması | Ön işaret 0,64 s (geç çeşidi 0,90 s); boss çevresinde daire, yarıçap 5,4 m (geniş çeşidi 8 m); hasar 220 (%22); kesilebilir | KOD (süre, alan) + ÖNERİ (hasar) |
| O-6.15 | Ağ Tükürüğü | Ön işaret 0,70 s; 3 mermi (2. fazda 5), 30° yelpaze, 7 m/s, mermi yarıçapı 0,35 m, ömür 3 s; mermi başına 60 hasar + 1,5 s yavaşlama | KOD + ÖNERİ (hasar) |
| O-6.16 | Ağ Örme | Ön işaret 0,90 s; 3 m yarıçaplı ağ alanı, 8 s yaşar, en çok 3 tane, merkezleri arası en az 4 m; içindeki oyuncu yavaşlar | KOD |
| O-6.17 | Zehir Nefesi | Ön işaret 0,80 s; koni 60°, 8 m; 180 hasar + zehir (3 s, saniyede 20) | KOD (süre) + ÖNERİ (koni, hasar) |
| O-6.18 | Sıçrayış | Ön işaret 1,00 s; 4–12 m uzaktaki oyuncuya sıçrar, havada 0,45 s, iniş dairesi 3 m, 140 hasar; duvara 2 m'den yakına inmez | KOD + ÖNERİ (hasar) |
| O-6.19 | Hedef seçme | Oyuncu %70, dost %30; dikkat çeken yem önceliklidir | KOD `targeting` |
| O-6.20 | Sendeleme | Denge dolunca 7 s; ardından 10 s tam kontrol yok | ÖNERİ (`eksik-parametreler`) |
| O-6.21 | Ölüm | 0,85 s çöküş; gövde Y'de %28'e basılır | KOD `BossDeathCollapseSec` |

---

## 7. Arena

> **Yeni arena: `19-krater-arenasi.md` (12 Ekim).** O-7.1 ve O-7.2 aynen geçerli. O-7.3'ün çarpışma kısmı
> (iç yüz 18 m, dış yüz 22 m, 4 m) geçerli, görünüm kısmı KA-6'ya geçer. O-7.4 → KA-5 (sütun ve kozalar
> kalkar), O-7.5 → KA-8.1, O-7.6 → KA-2, O-7.7 → KA-1 ve KA-6, O-7.8 → KA-6.6 ve KA-7, O-7.9 → kalkar
> (KA-11.2). Krater arenası koda girene kadar bu tablo bugünkü kodu anlatır.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-7.1 | Şekil | Daire | KOD (`slice_arena`) | — |
| O-7.2 | Oynanış sınırı (duvarın iç yüzü) | Yarıçap 18 m (görünen duvarın iç yüzüyle aynı; çarpışma duvarı da iç yüzü 18 m olacak şekilde kurulur, görünmez duvar yok). Oyuncu, boss, mermi ve itilen her şey bu dairenin içinde kalır | KOD `slice_arena.outer_wall_collision_radius_m` (§18: diğer iki değer bu değere bağlanır) | T, G |
| O-7.3 | Duvar | İç yüz 18 m, dış yüz 22 m; çarpışma yüksekliği 4 m; görsel yükseklik 6–9 m arasında düzensiz (yıkık köy duvarı ve kaburgalar) | KOD + ÖNERİ (görsel yükseklik) | K |
| O-7.4 | Engeller | 3 sütun (yarıçap 1 m, yükseklik 3 m): (−9, 4), (9, 4), (0, −10). 2 koza (yarıçap 0,6 m, 1,8 m, can 400): (−12, −6), (12, −6) | KOD `slice_arena` | T |
| O-7.5 | Başlangıç noktaları | Oyuncu (0, −14), boss (0, 6) ve boss oyuncuya bakar; çok oyunculuda oyuncular (0, −14) çevresinde 2 m arayla yay şeklinde | ÖNERİ | T |
| O-7.6 | Zemin | Düz (Y = 0); taş ve kül; ağ dokusu boss çevresinde yoğun. Zemin parıltısı yarıçapı 18 m | KOD `floor_glow` + ÖNERİ | K |
| O-7.7 | Duvarın ötesi ("sonsuz boşluk" sorunu) | Kameradan hiçbir açıda boş gökyüzü ya da zeminin bittiği kenar görünmez: 22–60 m arasında yıkık köy ve kaya halkası, 60–250 m arasında dağ siluetleri (düz kartlar), en dışta gökyüzü kubbesi. Zemin düzlemi 300 m yarıçapa uzanır, sis uzak kenarı gizler | ÖNERİ | K (8 yönde, kamera duvar dibinde, ekran görüntüsü) |
| O-7.8 | Ölçek referansı | Duvarlarda kapı açıklığı 2,2 m, basamak 0,18 m; köy evlerinin kapısı 2 m. Oyuncu bu nesnelerin yanında doğru boyda görünür | ÖNERİ | K |
| O-7.9 | Kaburgalar | 7 kaburga, 5 m arayla kuzey-güney ekseninde, ölçek 2,3; çarpışmaları yok (görüntü); 22 m dışında ve 2,5 m yükseklik altında oynanış alanına girmez | KOD `slice_arena_visuals.ribs` | K |

---

## 8. Sanat yönü

Ayrıntılı görsel dil (palet, ışık, sis, malzeme, silüet, ölçek, yapılmaz listesi) `16-sanat-incili.md`'dedir.
Aşağıdaki değerler orada aynen geçer.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-8.1 | Stil | Stilize gerçekçi: gerçek oranlar, abartısız; malzemeler sade (az ayrıntılı doku, net renk blokları). Ton: kasvetli karanlık fantezi, ejderha mitolojisi; kor ve kül | ÖNERİ | K |
| O-8.2 | Ortam paleti | Soğuk ve soluk: taş gri-mavi (#3A4048), kül (#5B5A57), gölge (#1C1F24), uzak sis (#4A5260) | ÖNERİ | K |
| O-8.3 | Vurgu rengi | Kor turuncu (#FF7A1A): rünler, ejderha motifleri, kalpteki kor. Ortamda yalnız ateş kaynaklarında | ÖNERİ | K |
| O-8.4 | Fiil ve element renkleri | `07` efekt motorundaki renkler geçerli; ortam paletiyle karışmaması için doygunlukları ortamdan yüksek tutulur | BELGE | K |
| O-8.5 | Ana ışık (güneş) | LED gibi beyaz, canlı: renk (1/1/1); fiili şiddet 1,6 (Gamma, tahmin); gölge şiddeti 0,75 (gölge siyah olmaz); açı euler (52, −30, 0). Eski "alacakaranlık / sıcak soluk" tanımı kaldırıldı ([K] Mustafa, 2026-10-15) | KOD `KeyLightIntensity` + `LookPresetsDefaults` (ölçüm: telefonda ayarlanır) | K |
| O-8.6 | Ortam ışığı | Gökyüzü (0,75/0,78/0,82), ufuk (0,62/0,64/0,67), zemin (0,42/0,43/0,45) (tahmin); ön ayar B ambiyanı koyulaştırmaz | KOD `ArenaSettings.Ambient*` | K |
| O-8.7 | Sis | Üstel sis, yoğunluk 0,012; renk #4A5260. Bugünkü 0,0032 neredeyse görünmüyor ("hava kasvetli değil" notu) | KOD→ÖNERİ | K |
| O-8.8 | Renk işleme | Gerçek kaynak `Look_B_Keskin.asset`: renk filtresi (1/1/1), kontrast +10, doygunluk −5, pozlama +0,3, beyaz dengesi 0, vinyet 0,18, bloom eşik 0,95 şiddet 0,55 (hepsi tahmin, telefonda ayarlanır). Rim yön ışığı (mavi) kaldırıldı (`RimLightIntensity` 0) | KOD (`Look_B_Keskin.asset` + `ArenaSettings`) | K |
| O-8.9 | Renk uzayı | Linear (bugün Gamma). Linear ışık ve sisi daha doğru gösterir; geçişte bütün renkler yeniden kontrol edilir, ayrı bir PR'dır | KOD→ÖNERİ | K |
| O-8.10 | Okunurluk sırası | 1) boss ön işareti, 2) dost skilleri ve dostun konumu, 3) kendi skillin, 4) boss gövdesi, 5) ortam. Bir üst sıradakini alttaki kapatamaz | ÖNERİ | K |

---

## 9. Efekt (VFX)

Efekt dili `07`'dedir. Burada yalnız sınırlar var.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-9.1 | Ekran kaplama sınırı | Tek bir efekt ekranın en çok %25'ini kaplar (boss saldırısı dahil); oyuncunun kendi efekti %15. Kaplama, efektin ekrandaki sınır kutusuyla ölçülür | ÖNERİ | K, G (aşılırsa sarı log) |
| O-9.2 | Karakter görünürlüğü | Hiçbir efekt kendi karakterini 0,3 s'den uzun tamamen örtmez | ÖNERİ | K |
| O-9.3 | Parçacık bütçesi | Ekranda toplam en çok 1500; tek efekt en çok 200 (çarpma flaşı 32) | KOD `EmitterMaxParticles` + ÖNERİ | G |
| O-9.4 | En kısa görünme süresi | Bir efekt en az 0,3 s görünür; ejderha silüeti ve kanat gibi "imza" anları en az 0,7 s | KOD (`entry_silhouette` 0,7) + ÖNERİ | T |
| O-9.5 | Uzak oyuncu efektleri | Kameraya 12 m'den uzak oyuncuların efektleri düşük ayrıntıda; düşük kalite ayarında yalnız yerel oyuncu tam | BELGE (`07`) | E |
| O-9.6 | Saydam katman (overdraw) | Ekranın herhangi bir noktasında en çok 4 saydam katman üst üste | ÖNERİ | E (GPU profil) |

---

## 10. Dövüş hissi

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-10.1 | Vuruş donması (hitstop) | Yakın silahta isabet anında vuran ve vurulan donar: süre = 0,05 × ritim çarpanı, 0,03–0,10 s arasında (Kılıç 0,04 s, Çekiç 0,10 s; Yumruk seri olduğu için her parçada alt sınır 0,03 s). Menzilli silahta yok. Oyun saati donmaz, yalnız iki modelin animasyonu | ÖNERİ | T, E |
| O-10.2 | Vurulma parlaması | Vurulan model 0,08 s beyaz, %60 | ÖNERİ | K |
| O-10.3 | Hasar sayısı | 28 dp (kritik/kusursuz 36 dp); 64 px yükselir, 0,35 s bekler, 0,45 s'de söner; ayarlardan kapatılabilir | KOD `DamageFloat*` | K |
| O-10.4 | Titreşim | Android 8 ve üstü. Kendi isabetin: 15 ms hafif. Hasar aldın: 50 ms orta. Boss büyük saldırısı sana değdi: 120 ms güçlü. ~~Kusursuz dodge: 2 × 20 ms~~ **[RED]** (kusursuz dodge kaldırıldı, `13` §3). Ayarlardan kapatılabilir | KOD (`FeelHaptics`, min SDK 26) + ÖNERİ | E |
| O-10.5 | ~~Kusursuz dodge hissi~~ **[RED]** | Kaldırıldı ([K] 13 Ekim, `13` §3). Eski satır: 0,2 s boyunca oyun hızı 0,5 | ~~KOD `PerfectFeelSec`~~ (kodda silinecek) | — |

---

## 11. Ses

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-11.1 | Öncelik sırası | 1) boss ön işaret sesi, 2) sana gelen hasar, 3) kendi vuruşun, 4) dost skilleri, 5) boss ve ortam, 6) müzik. Üst sıradaki çalarken alt sıradakiler 4 dB kısılır | ÖNERİ | E |
| O-11.2 | Aynı anda en çok ses | 24; aynı ses 0,05 s içinde tekrar çalmaz | ÖNERİ | T |
| O-11.3 | Boss ön işaret tonu | Perde 0,55 → 1,8 (hazırlık boyunca yükselir), ses 0,12 → 0,40 | KOD `TelegraphTone*` | E |
| O-11.4 | Her skill sesi | Fiil sesi (fiile özgü tını) + silah sesi (silaha özgü darbe) üst üste; sıfat sesi yok | ÖNERİ | E |
| O-11.5 | Rün çizim sesi | Her noktaya girişte kısa tını (fiil rününde alçak, sıfat rününde yüksek); kombo tanındığında harf sesi (`09` hedef seçme) | BELGE + ÖNERİ | E |
| O-11.6 | Müzik | Arena girişi sakin; boss uyanınca dövüş müziği; 2. fazda katman eklenir; boss ölünce 3 s sessizlik | ÖNERİ | E |
| O-11.7 | Ses seviyesi | Toplam karışım −16 LUFS (mobil standart) | ÖNERİ | E |

---

## 12. Ekran ve arayüz

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-12.1 | Yön | Yatay (iki yatay yön arasında döner, dik yok) | KOD→ÖNERİ (bugün otomatik döndürme) | T |
| O-12.2 | Güvenli alan | Bütün arayüz `Screen.safeArea` içinde (çentik ve kamera deliği) | ÖNERİ | K (çentikli telefon görüntüsü) |
| O-12.3 | Ölçekleme | Arayüz kısa kenar 600 dp'ye göre ölçeklenir | KOD `HudFitShortSideDp` | K |
| O-12.4 | Çizim altıgeni | Merkez ekranın %78 yatay, %30 dikey noktasında (sağ alt); yarıçap 112 dp (≈ 18 mm); nokta değme yarıçapı 34 dp (≈ 5,4 mm), merkez 38 dp | KOD `Hexagon*`, `DotHitRadiusDp`, `CenterHitRadiusDp` | E |
| O-12.5 | Mürekkep | Çizgi kalınlığı 3,5 dp; parmak kalkınca 0,4 s'de söner | KOD `InkWidthDp`, `InkLingerSec` | K |
| O-12.6 | Butonlar | Dodge yarıçapı 40 dp, silah değiştirme 36 dp, hedef kilidi 36 dp; altıgenle aralarında en az 40 dp boşluk | KOD | E |
| O-12.7 | Sol el | Ayardan bütün düzen aynalanır | KOD `MirrorForLeftHand` | K |
| O-12.8 | Can ve mana | Sol üst: can çubuğu 224 × 17 dp, altında mana; dost canları altında küçük (160 × 10 dp) | KOD `VitalsBar*` + ÖNERİ (dost) | K |
| O-12.9 | Boss çubuğu | Üst orta: 460 × 24 dp; altında denge çubuğu (8 dp); faz eşikleri çentikle gösterilir | KOD `VitalsBossBar*` + ÖNERİ | K |
| O-12.10 | Durum simgeleri | 28 dp, 6 dp aralık; can çubuğunun altında | KOD `StatusIcon*` | K |
| O-12.11 | Cooldown gösterimi | Altıgenin çevresinde ince halka dolar; mana yetmezse altıgen %50 soluk | ÖNERİ | K |
| O-12.12 | Skill önizleme kartı | Atılan komboda 324 × 72 dp kart (kombo adı, fiil-sıfat), 0,9 s | KOD `SkillPreview*` | K |
| O-12.13 | Yazı | En küçük yazı 14 dp; ana yazı tipi tek aile; Türkçe karakterler (ğ, ş, ı, İ) hepsi var | ÖNERİ | K |
| O-12.14 | Renk körlüğü | Hiçbir bilgi yalnız renkle verilmez: fiil rengi + sembol, boss ön işareti renk + kenar deseni | ÖNERİ | K |
| O-12.15 | Arayüzün kapladığı alan | Dövüş sırasında arayüz ekranın en çok %20'sini kaplar; ekranın orta %50'lik bölgesinde kalıcı arayüz yok | ÖNERİ | K |

---

## 13. Sayılar ve ekonomi

Tek kaynak: `kural-motoru-v4.json` → `scale` ve `globals`. Kural metinleri `01`–`05` ve `09`.

| Kimlik | Madde | Değer | Kaynak |
|---|---|---|---|
| O-13.1 | Oyuncu canı | 1000 | KOD `scale` |
| O-13.2 | Taban hasar / şifa / denge | 100 / 120 / 20 | KOD `scale` |
| O-13.3 | Mana | Havuz 100, dolum 10/s, skill bedeli 25 | KOD `scale`, BELGE (`09`) |
| O-13.4 | Cooldown | 4 s; boşa giden skill cooldown'un yarısını harcar | KOD `scale` |
| O-13.5 | Ritim çarpanı | Hasar silahın ritim çarpanıyla **bir kez** çarpılır | BELGE (`09`) |

---

## 14. Çok oyunculu

Dilimde çok oyunculu yok ve kodda ağ paketi yok. Kurulduğunda bu değerler geçerli.

| Kimlik | Madde | Değer | Kaynak |
|---|---|---|---|
| O-14.1 | Oyuncu sayısı | 1–5 | KOD (boss JSON `coop_required` 5) |
| O-14.2 | Otorite | Oyunu kuran cihaz (host) kural motorunu çalıştırır ve sonuçları yollar; diğerleri girdi yollar | ÖNERİ |
| O-14.3 | Güncelleme sıklığı | Saniyede 20 durum paketi | ÖNERİ |
| O-14.4 | Gecikme toleransı | 150 ms'ye kadar oynanabilir. Kendi hareketin ve dodge'un anında (yerel tahmin); skill sonucu host'tan gelir | ÖNERİ |
| O-14.5 | Bağlantı kopması | 30 s içinde dönülürse kaldığı yerden; dönmezse karakteri çıkar, boss canı yeniden ölçeklenmez | ÖNERİ |
| O-14.6 | Diriltme | Dost yanında 3 s durarak diriltir; diriltilen %50 canla kalkar | ÖNERİ |
| O-14.7 | Bütün oyuncular ölürse | Dövüş biter; 3 s sonra sonuç ekranı | ÖNERİ |

---

## 15. Teknik ve performans

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-15.1 | Motor | Unity 6, URP, Input System | KOD | — |
| O-15.2 | Platform | Android, en düşük sürüm API 25 (Android 7.1), ARM64, IL2CPP | KOD (ProjectSettings) | T |
| O-15.3 | Hedef cihaz | Kullanıcının test telefonu, ayrıca bir alt segment (2021 orta seviye, 4 GB bellek) | ÖNERİ | E |
| O-15.4 | Kare hızı | Hedef 60 fps; düşük kalite ayarında 30 fps sabit. Dövüşte kare süresinin %95'i 20 ms altında (60 fps modunda) | KOD `TargetFrameRateHz` 60 + ÖNERİ | E (kare süresi göstergesi `ShowFrameTimeHud`) |
| O-15.5 | Çizim çağrısı (draw call) | Ekranda en çok 150 | ÖNERİ | E |
| O-15.6 | Üçgen bütçesi | Karakter 15 000, silah 2 000, boss 40 000, ekranda toplam 300 000 | ÖNERİ | T (model içe aktarılınca sayılır) |
| O-15.7 | Doku | Karakter 2048 px (tek doku seti), silah 1024, boss 2048, ortam parçaları 1024; ASTC sıkıştırma | ÖNERİ | T |
| O-15.8 | Bellek | Oyun en çok 1,5 GB | ÖNERİ | E |
| O-15.9 | Isınma | 20 dakika kesintisiz dövüşte kare hızı %20'den fazla düşmez | ÖNERİ | E |
| O-15.10 | APK boyutu | En çok 150 MB | ÖNERİ | T |
| O-15.11 | Açılış | Uygulama açılışından arenaya en çok 10 s | ÖNERİ | E |
| O-15.12 | Ekran kapanması | Dövüşte ekran kapanmaz | KOD (`SleepTimeout.NeverSleep`) | — |

---

## 16. Asset hattı (Tripo → Blender → Unity)

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| O-16.1 | Birim ve eksen | 1 birim = 1 m; Unity'de Y yukarı, Z ileri. Blender'dan FBX dışa aktarımı: ölçek 1,0, "Apply Transform" açık, karakter +Z'ye (ileri) bakar | ÖNERİ | T (içe aktarılan modelin ölçeği 1, dönüşü sıfır) |
| O-16.2 | Tripo çıktısı | Ham model kabul edilmez; Blender'da: üçgen sayısı §15'e indirilir, ölçü §1/§2'ye getirilir, pivot §1.10/§2.11'e taşınır | ÖNERİ | T |
| O-16.3 | Rig | Karakter: Mixamo uyumlu Humanoid iskelet; `Grip_R`/`Grip_L` soketleri eklenir (§2.2) | ÖNERİ | T |
| O-16.4 | Silah modeli | Sıfır noktası tutuş noktasında, eksenleri §2.2'ye göre; tek malzeme; LOD gerekmez | ÖNERİ | T |
| O-16.5 | Animasyon | Yerinde (in-place), 30 fps, olay kareleri §3.3; Humanoid'e yeniden hedeflendikten sonra kontrol sayfası | ÖNERİ | K |
| O-16.6 | İsimlendirme | Model: `<karakter>_<parça>` (ör. `k3_govde`); silah: `silah_<ad>` (ör. `silah_kilic`); klip: `<Silah>_<Eylem>` (ör. `Kilic_VUR`); hepsi Türkçe karakter içermez | KOD (`Kilic_VUR`) + ÖNERİ | T |
| O-16.7 | LOD | Karakter ve boss: 2 seviye (LOD1 üçgenlerin %50'si, ekran yüksekliği %45'in altında); ortam: kaburgalardaki gibi | KOD (`kaburga_LOD1`) + ÖNERİ | T |
| O-16.8 | Kabul kontrol sayfası | Her yeni karakter/silah için 6 poz (bekleme, yürüme, VUR hazırlık, VUR vuruş, dodge tepe noktası, darbe alma) × 4 açı (ön, yan, arka, oyun kamerası) = 24 ekran görüntüsü tek sayfada. Kullanıcı onaylamadan oyuna girmez | ÖNERİ | K |

---

## 17. Oyun akışı ve ayarlar

| Kimlik | Madde | Değer | Kaynak |
|---|---|---|---|
| O-17.1 | Dilim akışı | Açılış → silah seçimi (iki silah) → arena (boss uyuyor) → oyuncu 6 m'ye yaklaşınca ya da vurunca boss uyanır (2 s uyanış) → dövüş → sonuç ekranı | ÖNERİ |
| O-17.2 | Ölüm (tek oyuncu); ölüm ve sonuç ekranında boss canı büyük yazılır ([K] 16 Ekim, `13` §5 [Ö] onaylandı) | 2 s ölüm → "Tekrar" ve "Silah değiştir" düğmeleri; tekrar 3 s içinde arenada | ÖNERİ |
| O-17.3 | Kazanma | Boss ölümünden 2 s sonra sonuç: süre, atılan skill sayısı, en çok hasar veren kombo, deneme sayısı | ÖNERİ |
| O-17.4 | Hedef | Dövüş süresi ~180 s; ilk zaferin ~8 denemede gelmesi | KOD (boss JSON `target_clear_time_sec`, `target_attempts`) |
| O-17.5 | Eğitim | İlk açılışta 3 adım: 1) merkeze dokun (temel vuruş), 2) iki nokta çiz (ilk kombo), 3) çift rün. Her adım bir kukla üzerinde; geçilebilir | ÖNERİ |
| O-17.6 | Duraklatma | Tek oyuncuda oyun durur; çok oyunculuda durmaz, yalnız menü açılır | ÖNERİ |
| O-17.7 | Ayarlar | Sol el düzeni, kamera hassasiyeti ve ters eğim, sarsıntı aç/kapa, titreşim aç/kapa, hasar sayıları aç/kapa, ses (müzik/efekt ayrı), kalite (düşük 30 fps / yüksek 60 fps) | KOD (kısmen) + ÖNERİ |
| O-17.8 | Kayıt | Ayarlar ve son seçilen silahlar cihazda saklanır. Ayar dosyası sürüm değişince eski değerleri **taşımaz**, sıfırlar (§18) | ÖNERİ |

---

## 18. 12 Ekim'de bulunan çift değerler (tek değere bağlanacak)

| # | Ölçü | Yer 1 | Yer 2 | Yer 3 | Bu belgenin değeri |
|---|---|---|---|---|---|
| Ç-1 | Arena sınırı | `kural-motoru-v4.json` duvar çarpışması **18 m** (daire) | `ArenaSettings.ArenaHalfSizeM` **25 m** | `Prototype.unity` sahne değeri **50 m**; dodge ve Hareket skilleri bunu kullanır (`ArenaClamp.XZ`, daire) | 18 m daire (O-7.2). Dodge'un duvardan geçmesinin bir sebebi: duvar 18 m'de, hareket sınırı 50 m'lik karede |
| Ç-2 | Kamera uzaklığı | Kod varsayılanı 5,85 m | Sahne 3,4 m | — | 6,0 m (O-5.1) |
| Ç-3 | Kamera FOV | Kod 54° | Sahne 56° | — | 55° (O-5.4) |
| Ç-4 | Oyuncu canı | `scale.player_max_hp` 1000 | `PlayerSettings.PlayerMaxHp` 22, boss JSON 22 | — | 1000 (O-13.1) |
| Ç-5 | Boss canı | `scale.boss_max_hp` 15000 | Boss JSON `total_hp` 120 | `eksik-parametreler` önerisi 12000 | 15000 (O-6.11) |
| Ç-6 | Kamera bakış yüksekliği | `CameraLookHeightM` 0,38 | — | — | 1,2 m (O-5.3) |
| Ç-7 | Boss dönme hızı | `BossTurnRateDegPerSec` 240 | — | — | 90 °/s (O-6.5) |
| Ç-8 | Telefondaki ayar dosyası | `tuning.vNN.json` cihazda saklanıyor; kod varsayılanı değişse de eski kayıt telefonda kalabilir | — | — | Sürüm değişince sıfırla (O-17.8). Ayar dosyası kalır; dosyadaki bir değer yalnız panelden bilerek "Kaydet"e basılarak yazılmışsa geçerlidir (dosya panelden kaydedilen alanları işaretler, işaretsiz alanlar kod/JSON değerinde kalır; otomatik kayıt kullanıcı değeri sayılmaz). Açılışta tek log bloğu her ayarı `ad = değer (kaynak: JSON / ayar dosyası / kod)` diye yazar. Dilimde JSON'dan gelen ayarların panel kaydırıcıları gri ve "JSON" etiketli, değiştirilemez (plan §2.4, 12 Ekim) |
| Ç-9 | Yay'ın diğer Hareket kombolarında geri sıçrama | `kural-motoru-v4.json` Yay `_not_hareket`: "diğer Hareket kombolarında kısa geri sıçrama" | `09` [K]: 3-3 geri sıçrama değildir; Hareket kombolarında silah teslimi yok | — | `09` geçerli; JSON notu ve `backstep_m` 3-3 ile çelişiyorsa kaldırılır |

---

## 19. Bu belgeden çıkan denetimler (özet)

| Tür | Yaklaşık sayı | Örnek |
|---|---|---|
| Otomatik test (`T`) | ~70 | "Kılıç VUR klibinin `Hit` karesi 0,20 s ± 1 kare", "arena sınırı her yerde 18 m", "silah modelinin sıfır noktası tutuş noktasında" |
| Gözcü kuralı (`G`) | ~15 | "oyuncu zeminin altında", "oyuncu duvarın/boss'un içinde", "kamera 1 s'de 120°'den fazla döndü", "kamera zeminin altında", "efekt ekranın %25'inden fazlası" |
| Görsel kontrol (`K`) | ~45 | Karakter ve silah kontrol sayfası, ortamın 8 yönden görüntüsü, ön işaret okunurluğu |
| Telefonda deneme (`E`) | ~30 | Joystick hissi, titreşim, ses dengesi, ısınma |

Bu sayıların her biri tek bir maddeye bağlıdır. Ajan test yazarken madde kimliğini testin adına koyar
(ör. `O_7_2_ArenaSiniri_18m`); böylece hangi testin hangi tasarım kararını koruduğu görülür.
