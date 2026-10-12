# 11 — Onarım planı (kronik sorunlar)

Sürüm 1.0 · 12 Ekim 2026 · Kullanıcı: Mustafa

Bu plan tek tek hata kovalamak yerine hataları üreten dört kronik sebebi kapatır. Adımlar **sırayla**
yapılır; bir adım bitmeden (merge + ölçüt sağlandı) sonrakine geçilmez. Her adım bir ya da birkaç küçük PR'dır;
her PR tek iş yapar.

## Bitti ölçütü (bütün plan için)

Dilim APK'sında ve 60 saniyelik Gözcü duman testinde:
- **Gözcü satırı: 0** (12 Ekim başlangıç: 72)
- **`[V4-FALLBACK]` satırı: 0**
- Telefonda elle 5 dakikalık oyunda kullanıcının "hatalar" listesindeki maddelerin hiçbiri görülmez.

Her PR açıklamasında **önce/sonra Gözcü sayısı** yazılır. Sayı düşmeyen "düzeltme" PR'ı düzeltme sayılmaz.

## Kronik sebepler

| # | Sebep | Belirtisi |
|---|---|---|
| K1 | Tasarım kaynağı repoda değildi; ajan bilmediği davranışı ve ölçüyü uyduruyordu | 3-3'ün Levi ipine dönmesi, Hareket'te silah vuruşu, rastgele kamera/dodge sayıları |
| K2 | Aynı ölçü birden çok yerde farklı değerle duruyor (sahne, telefondaki ayar dosyası, kod varsayılanı, JSON) | Dodge duvardan geçiyor (duvar 18 m, sınır 50 m kare), kamera her yerde farklı, can 22 / 1000 |
| K3 | Sessiz geri dönüş: bir koşul tutmayınca kod uyarmadan başka bir şey oynatıyor | Kılıçta yumruk, yanlış klip, eski efekt |
| K4 | Eski motor dilim yolunda hâlâ çalışıyor; iki motor aynı anda karar veriyor | `MechanicField` efekti ekranı %87 kaplıyor, kamera `MechanicWall` içinde, eski dodge'a dönüş |

## Adımlar

### Adım 1 — K1: Tasarım kaynağı repoda
- `docs/tasarim/` (bu klasör) + AGENTS.md kuralları + Gözcü eşikleri `10-olculer`'e eşit.
- Ölçüt: merge edildi; AGENTS.md "önce hücreyi/ölçüyü oku" kuralını içeriyor.

### Adım 2 — K2: Her ölçünün tek değeri
`10-olculer.md` §18'deki çift değerler tek değere bağlanır. Ayrı PR'lar:
1. **Arena:** oynanış sınırı yalnız `kural-motoru-v4.json` `slice_arena.outer_wall_collision_radius_m` (18 m,
   daire). `ArenaHalfSizeM` sahne değeri 50 m'lik değer dilim yolunda kullanılmaz. Dodge,
   Hareket skilleri, itilme ve mermi aynı daire sınırını kullanır. (O-7.2, O-4.11)
2. **Kamera:** uzaklık, eğim, bakış yüksekliği, FOV tek kaynaktan (O-5.1–O-5.4); sahne dosyasındaki ayrı
   değerler kaldırılır.
3. **Can ve hasar ölçeği:** yalnız `scale` bloğu (oyuncu 1000). Boss JSON'undaki 120/22 ölçeği ve
   `PlayerSettings.PlayerMaxHp` dilim yolunda okunmaz. (O-6.9, O-13.1)
4. **Telefondaki ayar dosyası:** dilim APK'sı `tuning.vNN.json` okumaz; değerler yalnız kod/JSON'dan gelir.
   (O-17.8)
- Her PR'da: "bu değer şu tek yerde" diye test (`O_7_2_...` gibi adlı).
- Ölçüt: §18'deki Ç-1…Ç-9 maddelerinin hepsi kapandı; Gözcü'de `ayak_zemin_alti` ve gövde-duvar satırı 0.

### Adım 3 — K3: Sessiz geri dönüş yasağı
1. Dilim yolundaki her geri dönüş noktası (`PlayBasicStrike`'ın 5 koşulu, klip bulunamadı, efekt bulunamadı,
   kural bulunamadı) `[V4-FALLBACK] <ne> <neden>` diye `LogError` yazar ve sessizce başka bir şey oynatmaz.
2. Gözcü `[V4-FALLBACK]` satırlarını sayar (eşik 0).
3. İlk koşuda çıkan her `[V4-FALLBACK]` satırı için kök sebep bulunur ve düzeltilir; her biri ayrı PR.
   Beklenen ilk kazanç: Kılıç düz vuruşu `Kilic_VUR` oynar (yumruk biter), silah değişince doğru yürüme klibi.
- Ölçüt: duman testinde `[V4-FALLBACK]` 0; Gözcü `uyumsuz_klip` 0.

### Adım 4 — K4: Eski motoru dilim yolundan çıkar
1. `DOVUS_V4_SLICE` açıkken eski motorun karar ve görüntü yolları (`ManifestationDirector`, `MechanicField`,
   `MechanicWall`, `MotionTemplateRunner`, eski dodge) **çalışmaz**. Çalışmaya kalkarsa `[V4-FALLBACK] eski
   motor: <ne>` yazar.
2. Bağımlılık haritası (v4'ün eriştiği dosyalar) her PR'dan sonra yeniden çıkarılır; dilimin eriştiği eski
   motor dosyası sayısı her PR'da düşer. Silme değil **taşıma**: v4'ün gerçekten ihtiyaç duyduğu parça v4
   tarafına taşınır, kalan eski motor dondurulmuş kalır.
- Ölçüt: dilimde eski motor kaynaklı Gözcü satırı (`efekt_ekran_kaplama@Mechanic*`,
  `kamera_collider_icinde Mechanic*`) 0; eski motor kaynaklı `[V4-FALLBACK]` 0.

### Adım 5 — Kalan belirtiler (kronik sebepler kapandıktan sonra)
Sırayla, her biri ayrı PR, her birinde önce/sonra Gözcü sayısı:
1. Kamera: kombo başında FOV'un 0'a düşmesi (`kamera_fov_gecersiz`), dönüş tavanı 120°/s, boss yakınında
   yaw dondurma, dosta/duvara gömülme (O-5.6–O-5.15).
2. Dodge görünümü: tek basış zıplama, havada takla, ayak zemin üstünde, iz yalnız arkada (O-4.4–O-4.10).
3. Yürütücüde eksik komutlar: `KontrolUygula`, `TasmaBagla`, `EtkiSok`, `Kilitlen` (Kontrol, Arındırma,
   Güdümlü kilit) — `09` "Bilinen sapmalar".
4. Sayı hataları: ritim çarpanı bir kez, Yoğun şarjı bir kez, Yoğun Koruma 0,45 s — `09`.
5. Hareket ve Koruma hücrelerinden silah teslimini kaldır; Sabit yapıları (diken, kaynak, duvar, tuzak) — `09`.
6. Boss efektlerinin ekran kaplaması (O-9.1), ortam: sis, duvar ötesi, ışık (O-7.7, O-8.5–O-8.7).
7. `WaitForEndOfFrame` yüzünden atlanan 3 test: batchmode'da çalışan bekleme yöntemine geçir.

## Çalışma kuralları (bütün adımlar)
- Bir PR tek iş. Aynı dalda iki ajan çalışmaz.
- Ajan test çalıştırırken Unity editörü kapalı.
- Yalnız ilgili testler (`-testFilter`); 20 dakikayı geçen test durdurulur.
- Kırmızı test önce master'da denenir; orada da kırmızıysa eski hatadır, düzeltilmez, yazılır.
- Belgede olmayan davranış ya da ölçü uydurulmaz; sorulur.
