# 12 — Kusur envanteri (dilim)

Sürüm 1.4 · 13 Ekim 2026 (2.5 sırasında güncellendi) · master `b1dbb3a5` (#171 sonrası) üzerinde kod taramasıyla hazırlandı.

Bu liste onarım planının (`11`) kalan işini **önceden** çıkarır. Ajan sıradaki maddeyi alır, yalnız onu yapar.
Çalışırken yeni bir kusur bulursa **düzeltmez**; PR açıklamasına "envantere eklenecek" diye yazar ve bu
listenin sonuna eklenir. Her madde bir PR'dır (aksi yazmadıkça).

Her maddede: **Belirti** (oyuncunun gördüğü) · **Yer** (dosya) · **Sebep** · **Düzeltme** · **Test** · **Ölçüt**.

Durum işaretleri: ⏳ verildi / devam · ☐ sırada · ✅ bitti.

---

## Biten (12 Ekim)
✅ #162 Gözcü · ✅ #163 tasarım kaynağı · ✅ #164 arena sınırı tek kaynak · ✅ #166 duvar halkası + sis ·
✅ #167 gövde kapsülü · ✅ #169 Hareket süpürmesi · ✅ #170 boss yarıçapı · ✅ #171 yürürken boss/dost engel · ✅ #172 kamera tek kaynak (Gözcü 83 → 70) · ✅ #173 can ve boss hasarı tek kaynak · ✅ #174 ayar dosyası (KAYDET, kaynak logu)

---

## A — Temel (Adım 2)

### A1 ✅ Kamera ölçüleri tek kaynak (2.2) — #172
- **Belirti:** kamera sahneye göre farklı uzaklıkta; kombo başında ekran gri/boş; dodge'da kamera "deliriyor".
- **Yer:** `Game/Cameras/FollowCameraController.cs` (satır 318–319), `Game/Actors/PlayerDodgeController.cs`
  (satır 186), `Game/Feel/SkillFeel.cs` (satır 109), `Core/Tuning/FeelTuning.cs` (satır 64–74),
  `Scenes/Prototype.unity` (CameraDistanceM 3.4, CameraFovDeg 56).
- **Sebep 1 — FOV'un 0'a düşmesi (bulundu):** `Punch(fovKick, rollDeg, shakePx, decay)` FOV'u
  `taban × (1 − fovKick × t)` diye hesaplıyor; `fovKick` oran olmalı (0,06–0,18 gibi).
  - `SkillFeel` skill başında `SkillKickStrike` = 3,2 / `SkillKickZone` = 2,4 / `SkillKickMotion` = 2,0 ...
    gönderiyor → FOV = taban × (1 − 3,2) = **negatif** → ekran boş/gri (Gözcü `kamera_fov_gecersiz` 13).
  - `PlayerDodgeController.PlayLocalFeel` `Punch(6, 1.5, 4, 10)` çağırıyor: parametreler başka bir imzaya göre
    yazılmış (genlik, frekans, süre, sönüm) → fovKick = 6 → kusursuz dodge'da FOV negatif.
- **Sebep 2:** kamera ölçüleri sahne, kod varsayılanı ve ayar dosyasında farklı.
- **Düzeltme:** `fovKick` birimi tek tanım: "FOV'un oransal daralması, 0–0,25". Skill kick değerleri bu
  birime çevrilir (ör. vuruş 0,08, alan 0,06, hareket 0,05); dodge çağrısı doğru imzaya göre yazılır;
  `Punch` girişte 0–0,25 aralığına sıkıştırır ve aralık dışı gelirse `[V4-FALLBACK]` yazar. Kamera ölçüleri
  tek kaynak (O-5.1–O-5.4); kilit mesafesi boss kenarından (O-5.8); kamera boss gövdesine girmez (O-5.15).
- **Test:** her skill türü ve kusursuz dodge sonrası FOV ≥ 10° (her kare); ölçüler tek kaynak; boss dibinde
  kamera boss kapsülü dışında.
- **Ölçüt:** Gözcü `kamera_fov_gecersiz` = 0.

### A2 ✅ Can ve hasar ölçeği (2.3) — #173
- **Belirti:** canlar ve hasarlar tutarsız (oyuncu 22 / 1000).
- **Yer:** `Game/Config/Sections/PlayerSettings.cs` (`PlayerMaxHp = 22`), `Resources/Bosses/aglarin-kralicesi.json`
  (`total_hp 120`, `player_hp 22`, saldırı hasarları), `kural-motoru-v4.json` → `scale`.
- **Düzeltme:** dilimde oyuncu ve boss canı yalnız `scale`'den; boss saldırı hasarları oyuncu canının yüzdesi
  olarak `10-olculer` O-6.14–O-6.18'e çevrilir.
- **Ek yer (#172 sonrası tarama):** `GameBootstrapHost.cs` 139–145 (üç ayrı can kaynağı: `scale`, boss JSON
  `BossCombatProfile`, `PlayerSettings`); `RuleEngineV4Catalog.cs` 56 (`RuleEngineV4CatalogDefaults.PlayerMaxHp`
  sessiz kod varsayılanı → `[V4-FALLBACK]`); `TuningPanelHud.Build.cs` 316 (panelde "Oyuncu can tavanı" 1–100
  kaydırıcısı dilimde canı 22–100'e ezebilir); `TuningPresets.cs` 52 (`PlayerMaxHp = 30`).
- **Test:** `O_13_1_OyuncuCani_1000`, `O_6_11_BossCani_15000`, boss saldırı hasarları O-6.14–O-6.18 ile aynı;
  dilimde panel/hazır ayar canı değiştirmez.

### A3 ✅ Ayar dosyası (2.4) — #174
- Plan `11` §2.4 tanımıyla: dosya kalır, sürümde sıfırlanır, açılışta değer kaynakları log'a yazılır.
- **Yer:** `Game/Config/TuningConfig.cs`, `GameTuning.Migrate.cs`.

### A4 ✅ Koruma kapısı (2.5) — #175
- GitHub'da Unity CI yok; koruma = yerel zorunlu O_ testleri + Gözcü tür bazlı cırcır + PR kapısı + dal koruması.

---

## B — Sessiz geri dönüşler (Adım 3)

### B1 ⏳ Kılıç düz vuruşunda yumruk
- **Belirti:** Kılıç'la merkeze dokununca yumruk görüntüsü.
- **Yer:** `Game/Skills/Hosts/MdSkillServicesHost.cs` satır 101–102,
  `Game/Skills/RuleEngineV4/RuleEngineV4SkillAnimBridge.cs` satır 55–81.
- **Sebep:** `PlayBasicStrike` 6 koşulun birinde `false` dönüyor ve çağıran sessizce
  `_visual.PulseBasicStrike()` (yumruk) oynatıyor. JSON'da Kılıç (4) + rün 1 → `Kilic_VUR` eşlemesi var; düşen
  koşul büyük ihtimalle `EquippedWeaponNumber` (silah kimliği ayrıştırması) ya da `PlaySkillClip`'in Animator'da
  `Kilic_VUR` durumunu bulamaması (`LastPlayApplied` false).
- **Düzeltme:** her `return false` öncesi `[V4-FALLBACK] düz vuruş: <koşul> <değerler>`; `PulseBasicStrike`
  yedeği dilimde kaldırılır. Log'dan düşen koşul bulunur, kökü düzeltilir.
- **Test:** gerçek dokunuşla Kılıç düz vuruşu → oynayan klip `Kilic_VUR`; Yay → `Yay_VUR`. `[V4-FALLBACK]` 0.

### B2 ☐ Silah değişince eski silahın yürüme klibi
- **Belirti:** Yay'a geçince karakter Kılıç gibi duruyor/yürüyor (Gözcü `uyumsuz_klip` 45).
- **Yer:** `Game/Actors/ActorView.cs` (satır ~204, override controller değişimi),
  `Game/Weapons/WeaponVisualRegistry.cs`, `Game/Editor/K3AssetSetup.cs`.
- **Sebep (aday):** silah değişiminde k3 Animator'ünün yürüme setini değiştiren kayıt yok ya da eksik;
  arketip override controller'ları repoda değil (`.overrideController` gitignored, yerelde üretiliyor).
- **Düzeltme:** silah değişimi k3'te yürüme/bekleme klibini `Loko_<Silah>_*`'a geçirir; kayıt yoksa
  `[V4-FALLBACK]`. Override'lar repoda ya da derlemede üretilir (yerel dosyaya bağımlılık kalkar).
- **Test:** Kılıç → Yay → Kılıç değişiminde her silahta 1 s içinde oynayan klip o silahın adını taşır.
- **Ölçüt:** Gözcü `uyumsuz_klip` = 0.

### B3 ☐ Kalan sessiz yedekler
- **Yer ve örnekler:** `ActorView.Casting.cs` satır 110 ("Animator state yok ... Yedek locomotion/saldırı
  klibi" uyarısı), `PlaceholderFactory` ("VFX asset missing ... using placeholder"), `CombatFallbacks.*`
  (`ArenaHalfSizeFallbackM` vb.).
- **Düzeltme:** dilim yolunda her biri `[V4-FALLBACK]` LogError; yedek görüntü oynatılmaz.
- **Ölçüt:** duman testi ve 5 dk telefonda `[V4-FALLBACK]` 0.

---

## C — Eski motoru dilimden çıkar (Adım 4)

### C1 ☐ Düz vuruşta ileri kayma
- **Belirti:** Yay'la düz vururken karakter yürümeden öne kayıyor.
- **Yer:** `Game/Skills/Flow/SentenceManifestationBridge.cs` satır ~93 (`TryBeginBasicStrikeStep`),
  `Game/Skills/Motion/MotionTemplateDriver.cs` satır 262 (`TryBeginBasicStrikeStep`, "basic_strike lunge").
- **Sebep:** düz vuruş eski motorun cümle yolundan geçiyor; eski motor her düz vuruşta silahtan bağımsız ileri
  atılış uyguluyor. Kural: düz vuruşta yerinde (ön sürede yalnız Yumruk ve Kitap yürür).
- **Düzeltme:** dilimde düz vuruş v4 yolundan (`RuleEngineV4` planlayıcı + yürütücü) yürür; atılış adımı dilimde
  çalışmaz.
- **Test:** Yay ile 5 düz vuruş: oyuncu konumu değişmez (≤ 2 cm). Kılıç aynı.

### C2 ☐ Eski motorun alan ve duvarları
- **Belirti:** ekranın %87'sini kaplayan efekt (`MechanicField`); kamera `MechanicWall` içinde.
- **Yer:** `Game/Skills/Mechanics/MechanicWorldRuntime.cs` (satır 84, 118, 217),
  `ManifestationDirector.zMechanicsServices.cs` satır 68. Dilim kontrolü yok.
- **Düzeltme:** `RuleEngineV4Feature.Enabled` iken `MechanicWorldRuntime` alan/duvar üretmez; üretmeye
  kalkarsa `[V4-FALLBACK] eski motor`.
- **Ölçüt:** Gözcü `efekt_ekran_kaplama@Mechanic*` ve `kamera_collider_icinde Mechanic*` = 0.

### C3 ☐ Eski motorun ezdiği değerler
- `Game/Skills/Launch/CastSideEffects.cs` satır 122 (arena alanını sahne değeriyle eziyor), eski motorun
  0,85 boss çarpışıcısı (#170 notu). Dilimde okunmaz.

### C4 ☐ Bağımlılık haritası
- Dilimin eriştiği eski motor dosyası sayısı ölçülür; C1–C3 sonrası yeniden ölçülür (her PR'da düşmeli).

---

## D — Kalan belirtiler (Adım 5)

### D1 ☐ Kamera davranışı (5.1)
- Boss'a 2 m'den yakınken yaw dondur, 2–5 m karışım (O-5.7); dönüş tavanı 120°/s (O-5.6); omuz değişimi
  ≥ 1,5 s (O-5.9); boss arada kalırsa %35 saydam (O-5.11); dost kapsülü ve can çubuğu görüşü kapatınca
  kamera kayar.
- **Yer:** `FollowCameraController.cs` (ResolveYaw), `.Collision.cs` satır 42–78, `.LockOn.cs`.
- **Ölçüt:** Gözcü `kamera_aci_hizi` = 0, `kamera_collider_icinde` = 0.

### D2 ☐ Dodge görünümü (5.2)
- İki faz (0,25 + 0,40 s, O-4.4a), kor parlaması (O-4.6a), iz yalnız süzülmede (O-4.10), takla ayağı zemin
  üstünde (Gözcü `ayak_zemin_alti`).
- **Yer:** `DodgeMotionController.cs`, `DodgeTuning.cs` (`GlideTailMs` 120 → 400), `Ortak_Takla` klip ayarı.
- **Ölçüt:** Gözcü `ayak_zemin_alti` = 0.

### D3 ☐ Skill: sayı hataları (S1)
- Ritim hasar çarpanı iki kez (planlayıcı + yürütücü `RuleEngineV4WorldCommandRunHost`), Yoğun şarjı iki kez
  bekleniyor, Yoğun Koruma 0,225 s (`KorumaYogunBlockSec × YogunDurationMult`) → 0,45 s.

### D4 ☐ Skill: Hareket ve Koruma (S2)
- `RuleEngineV4Planner`'da Koruma/Hareket'e eklenen silah teslimi kaldırılır; Hareket yolculuğu i-frame
  (`09` K 12 Ekim); Çekiç/Kalkan ön süresinde süper zırh.

### D5 ☐ Skill: Kontrol (S3)
- `KontrolUygula`, `TasmaBagla` yürütücüde karşılıksız → bütün 6-x hücreleri.

### D6 ☐ Skill: Arındırma (S4)
- `EtkiSok` yürütücüde karşılıksız → 9-1…9-6.

### D7 ☐ Skill: Güdümlü (S5)
- `Kilitlen` yürütücüde karşılıksız → x-6 hücreleri (8 s tavanı, K 22:53).

### D8 ☐ Skill: Sabit yapıları (S6)
- Diken, şifa kaynağı, duvar, tuzak, basamak taşı (3-4 basamak taşı bugün katı bir küp).

### D9 ☐ Yürürken minyon ve koza engel
- #171 yalnız boss ve dostu kapsadı; minyon (yarıçap 0,5) ve koza (0,6) eklenir.

### D10 ☐ Boss efektlerinin ekran kaplaması
- O-9.1 (%25). C2 sonrası kalan `efekt_ekran_kaplama` satırları; 2-6 (Yay) mavi şekil.

### D11 ☐ Ortam
- Işık açısı ve rengi (O-8.5), renk ayarları; sis yapıldı (#166).

### D12 ☐ Batchmode'da takılan 3 test
- `WaitForEndOfFrame` yerine batchmode'da çalışan bekleme; `[Ignore]` etiketleri kalkar.

### D13 ☐ Kılıç 3 kesiş zinciri
- `Kilic_VUR_b`, `Kilic_VUR_c` klipleri gelince (O-4.14).

---

## E — Ayar masası (Adım 6)
Plan `11` Adım 6.

---

## Envantere eklenenler (çalışırken bulunanlar)
_(Ajanlar buraya yazar; madde numarası, bulan PR, kısa açıklama.)_

| # | Bulan | Açıklama | Nereye bağlı |
|---|---|---|---|
| Y1 | #172 | `efekt_ekran_kaplama` 19 → 20 (+1), yeni kadrajdan sanılıyor, incelenmedi | D10 |
| Y2 | #172 | Gözcü'de "kamera boss gövdesinin içinde" kuralı yok (O-5.15 G) | D1 |
| Y3 | #172 | Oyuncu boss'un dibindeyken kamera boss'a dönerse "gövdeye girme" kuralı kamerayı oyuncunun göğsüne itebilir (O-5.10 / O-5.15) | D1 |
| Y4 | #172 | Paneldeki kamera kaydırıcıları (uzaklık, eğim, bakış yüksekliği, FOV) dilimde etkisiz; artık `slice_camera` okunuyor | A3 / E |
| Y5 | #172 | `slice_camera`'ya taşınmayanlar: omuz kayması, yumuşak kilit menzil/gücü, kadraj ağırlığı, boss bakış yüksekliği. `Prototype.unity`'de 4 kamera değeri duruyor (dilim okumuyor) | D1 |
| Y6 | #172 | Temizlik: `PlayerDodgeController._camera` kullanılmıyor; `FollowCameraController.Collision.cs` boş kaldı; `WallBase` test tanıları ayar değeri kullanıyor | C4 sonrası temizlik |
| Y7 | #172 | JSON'da boss boyu alanı yok (5 m `BossVisualHeightM`, O-6.10). Ejderha (çok parçalı boss) için boss parça listesi gerekecek | Kül Ejderi işi |
| Y8 | #172 | Telefonda elle kombo ve dodge görsel kontrolü yapılamadı (USB koptu) | Adım 2 sonu toplu telefon kontrolü |
| Y9 | #173 | Zehir/yanma tiki oyuncuda her karede `CeilToInt` ile en az 1 can düşürüyor: Zehir Nefesi toplamı 240 yerine 253 (#173 ölçtü). Düzeltme: küsurat biriktirilir. Kırmızı `[Ignore]`'lu test #173'te | D3 (sayı hataları) |
| Y10 | #173 | Panel "Hasar" kaydırıcısı dilimde etkisiz; "Oyuncu can tavanı" etiketi gerçek canı (1000) değil ayar değerini gösteriyor | A3 |
| Y11 | #173 | Zehir Nefesi konisi kodda 6 m / 40° yarı açı; O-6.17'de 8 m / 60° | D3 (sayı hataları) |
| Y12 | #173 | `BossCombatProfile` boss JSON'unu değil `element-sistemi.json`'u okuyor | B3 (sessiz yedekler) |
| Y13 | #173 | Gözcü duman testi boss'u donduruyor; boss saldırılarını kapsamıyor (ör. boss efekti kaplaması duman testinde görünmez) | D10 |
| Y14 | #169, #173 | Telefonda 3 kez `MeshCollider doesn't exist` (APK'da kod ayıklama MeshCollider'ı siliyor) | B3 |
| Y15 | #174 | Dilimde panel SIFIRLA boss denge çubuğunu 300'den 100'e çekiyor ve `[V4-FALLBACK]` yazıyor | B3 |
| Y16 | #174 | `TuningPanelHud.IsOpen` panel yok edilince sıfırlanmıyor (dokunma girişi kilitli kalabilir) | B3 |
| Y17 | #174 | Zehir değerleri dilimde koddan geliyor; `slice_boss_attacks` dışındaki zehir ayarlarının JSON karşılığı belgede net değil | D3 |
| Y18 | #174 | Panelde FOV kaydırıcısı yok; FOV yalnız logda görünüyor | E (ayar masası) |
| Y19 | #174 | Geliştirici APK'sında DevHp açık: oyuncunun gerçek can havuzu 1e9 (ölümsüz). Telefon testlerinde boss hasarı oyuncuyu öldürmez | B3 |
| Y20 | 2.5 | Tek oyun saati yok: dünya saati (GameClockHost), kamera, efekt ömürleri (~30 dosya) ve dokunuş süresi `Time.unscaledTime`/`realtimeSinceStartup` ile çalışıyor; duman testi deterministik değil. Hepsi tek enjekte edilebilir saate bağlanır; bitince Gözcü `efekt_ekran_kaplama` cırcırı da engelleyici olur | C4 sonrası (Adım 4) |
| Y21 | 2.5 | Ajan bilgisayarındaki Unity önbelleğinde 108 shadergraph'ın 102'si bozuk içe aktarılmıştı (`ShaderGraphMetadata`); yeniden içe aktarıldı (repo değişmedi). Daha önceki APK'larda shadergraph'lı malzemeler eksik/yedek shader'la çıkmış olabilir. Sonraki APK'da telefonda görsel kontrol | Adım 2 sonu telefon kontrolü / D11 |
