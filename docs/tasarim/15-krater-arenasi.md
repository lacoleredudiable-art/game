# 15 — Krater arenası

Sürüm 1.0 · 12 Ekim 2026 · Kullanıcı: Mustafa

Kül Ejderi'nin arenası. Sıfırdan tasarlanmıştır; bugünkü arenanın hiçbir parçası (yıkık köy duvarı, kaburgalar,
sütunlar, kozalar, ağ dokusu, yeşil zemin) kullanılmaz (`12` §12). Bu belge her nesnenin yerini, ölçüsünü,
çarpışmasını ve hangi asset'ten yapılacağını söyler. Boss'un kullandığı noktalar (`14` §12) burada aynen
geçer, değiştirilmez. Görünüm kuralları `12-sanat-incili.md`'dedir.

---

## 0. Nasıl okunur

- Kimlik `KA-x.y`; PR'da ve test adında yazılır (ör. `KA_4_1_Catlaklar_4mAra`).
- Kaynak ve doğrulama sütunları `10-olculer.md` §0'daki anlamda.
- **Koordinat:** arena merkezi (0, 0); X doğu, Z kuzey, Y yukarı; metre. **Azimut:** kuzeyden saat yönünde
  derece (0° = +Z kuzey, 90° = +X doğu, 180° güney, 270° batı).
- **Kod kuralı:** arena koddan kurulur (`GameBootstrapHost`); `.unity` ve `.prefab` dosyaları elle düzenlenmez
  (AGENTS.md kural 2). Yerleşim verisi tek bir JSON'da durur (KA-9.1); bu belgedeki sayılar oraya geçer.

---

## 1. Bölgeler

| Kimlik | Bölge | Yarıçap | Ne var | Oynanış |
|---|---|---|---|---|
| KA-1.1 | Oynanış alanı | 0–18 m | Düz zemin (Y = 0), çatlaklar, dikili taşlar, eski silahlar, ejderhanın başı ve pençeleri | Oyuncu, dost ve mermi yalnız burada (O-7.2) |
| KA-1.2 | Duvar | 18–22 m | Kraterin iç yamacı: kuzeyde ejderhanın gövdesi, başka yerlerde kaya | Çarpışma iç yüzü 18 m, yükseklik 4 m; görünmez duvar yok (iç yüz görünür kayadır) |
| KA-1.3 | Yamaç | 22–60 m | Kraterin dışa doğru yükselen yamacı, kenarı ve ejderhanın gövdesi ile kuyruğu | Görüntü; çarpışma yok |
| KA-1.4 | Uzak | 60–250 m | Dağ silüetleri, sisin içinde | Görüntü |
| KA-1.5 | Gökyüzü | 300 m kubbe | SI-4.8 | Görüntü |
| KA-1.6 | Zemin düzlemi | 0–300 m | Kameradan hiçbir açıda zeminin bittiği kenar görünmez (O-7.7) | — |

---

## 2. Zemin

| Kimlik | Madde | Değer | Asset | Doğrulama |
|---|---|---|---|---|
| KA-2.1 | Yakın zemin | 0–30 m, oynanış alanında tam düz (Y = 0 ± 1 cm); 18–30 m arasında duvarın altında görünmez | `DenemeSahnesi/Meshes/Ground_Near.obj` (13 920 yüz); malzeme SI-5.1: `dark_rock` dokusu + `burned_ground_01` ayrıntı dokusu | T (oynanış alanında yükseklik farkı ≤ 1 cm) |
| KA-2.2 | Uzak zemin | 30–300 m, yamaçların altında ve arasında | `Ground_Far.obj` (14 400 yüz) | K |
| KA-2.3 | Aşınma lekeleri | Zeminin %30–40'ı #3A3D42 (SI-2.3); yoğunluk merkezde ve taşların çevresinde fazla (yürünmüş yerler), duvar dibinde az | Leke maskesi dokusu (16 m ölçekli, SI-5.1) | K |
| KA-2.4 | Kül birikimi | Duvar dibinde (r 16–18 m) ve taşların kuzey yüzünün dibinde #5B5A57 kül bandı, 0,5–1,5 m genişlik | Aynı maske dokusunun ikinci kanalı | K |

---

## 3. Ejderhanın gövdesi (duvar ve yamaç)

`14` KE-2.2'deki yerleşim. Gövde sabit kaya kütlesidir; baş, boyun, pençeler ve kanatlar canlandırılır. Gövdenin
ölçüleri Tripo kaba modelinden (`ejder_govde` v0, 12 Ekim) ölçülmüştür.

| Kimlik | Parça | Konum | Ölçü | Not |
|---|---|---|---|---|
| KA-3.1 | Baş (faz 1) | Burun (0, 7,5), kafatası arkası (0, 16,5) | 9 m uzun, 7 m yüksek | KE-2.3 |
| KA-3.2 | Ön pençeler | Sol (−8, 13,7), sağ (8, 13,7); tırnak ucu Z 11,3 | 5,2 × 4,2 m taban; bacak dirsekte (Z 16,4, 4,5 m) geriye bükülür, omuz (±7,4, 18,9, 9,8) göğse girer | KE-2.4 |
| KA-3.3 | Boyun | Göğsün ortasından (0, 19), 10–12 m yükseklikten başın arkasına (0, 16,5) iner | Kalınlık 5 m | Göğsün kesik ön yüzünü kapatır |
| KA-3.4 | Göğüs ve omuzlar | Ön yüz Z ≈ 18 (duvar çizgisi), X −10…10 | Sırt Z = 20'de 14 m, Z = 25'te 17 m yükseklikte | Azimut 330°–30° arasında duvarın kendisi ejderhanın göğsüdür |
| KA-3.5 | Sırt ve kanatlar | Z 18–58; kanatlar Z 25–55 arasında X ±18'e yayılır, krater kenarının üstünde | En yüksek nokta 18 m (Z ≈ 35) | Kanatlar Kanat Savuruşu'nda açılır (KE-6.6) |
| KA-3.6 | Kuyruk | Kanatların altına kıvrılı, görünmez | — | Ayrı model yok |
| KA-3.7 | Kanat sektörleri | Azimut 300°–330° ve 30°–60° arasında duvarın görünen yüksekliği 6–7 m (kanadın ucu duvarın üstünden görünsün) | — | Kanat Savuruşu'nun hazırlığı okunur |

---

## 4. Çatlaklar

Küçük tehdit Çatlak Alevi'nin (KE-4.8) çıktığı yerler. Her biri 4 m uzunluğunda kor çatlak parçası. Yön,
merkezden geçen çizgiye diktir (çember boyunca); değer azimut gibi okunur (0° = kuzey-güney hattı).

| Kimlik | Çatlak | Merkez (X, Z) | Yön | Kimlik | Çatlak | Merkez (X, Z) | Yön |
|---|---|---|---|---|---|---|---|
| KA-4.1 | Ç1 | (−5, −1) | 169° | KA-4.9 | Ç9 | (−6, −12) | 117° |
| KA-4.2 | Ç2 | (5, −1) | 11° | KA-4.10 | Ç10 | (6, −12) | 63° |
| KA-4.3 | Ç3 | (−2, −4) | 117° | KA-4.11 | Ç11 | (−14, 7) | 27° |
| KA-4.4 | Ç4 | (3, −5) | 59° | KA-4.12 | Ç12 | (14, 7) | 153° |
| KA-4.5 | Ç5 | (−13, −3) | 167° | KA-4.13 | Ç13 | (−4, 3) | 37° |
| KA-4.6 | Ç6 | (13, −3) | 13° | KA-4.14 | Ç14 | (4, 3) | 143° |
| KA-4.7 | Ç7 | (−11, −9) | 141° | KA-4.15 | Ç15 | (0, 1) | 90° |
| KA-4.8 | Ç8 | (11, −9) | 39° | | | | |

| Kimlik | Kural | Değer | Doğrulama |
|---|---|---|---|
| KA-4.16 | Aralık | Çatlak merkezleri arası ≥ 4 m; taş merkezine ≥ 3 m; görev çemberi merkezine ≥ 3,5 m; pençe merkezine ≥ 3 m; oyuncu başlangıcına ≥ 3 m; başın önündeki alana (|X| < 6, Z > 4,5) girmez; r ≤ 16,5 m. Yukarıdaki 15 çatlak bu kurallara uyar (12 Ekim hesabı) | T |
| KA-4.17 | Görünüm | Bütün çatlaklar aynı görünür: sönük kor, şiddet 1,5, nabız 0,2 Hz (SI-5.1). Hangisinin tehlikeli olduğunu yalnız kırmızı ön işaret söyler (SI-9.2) | K |
| KA-4.18 | Süs çatlakları | Oynanış alanı dışında (r > 17 m) ve ejderhanın altında ek kor çatlak ağı; tehdit taşımaz. Toplam çatlak alanı zeminin %3–6'sı (SI-5.1) | K |
| KA-4.19 | Asset | Tehdit çatlağı: 4 m'lik şerit mesh, 3 çeşit (sağa kıvrık, sola kıvrık, düz), Blender'da yapılır, `LavaEmissive` shader'ı. Süs ağı: `DenemeSahnesi/Meshes/LavaCracks.obj` | — |

---

## 5. Dikili taşlar

| Kimlik | Taş | Merkez | Yükseklik | Ön yüz yönü | Durum |
|---|---|---|---|---|---|
| KA-5.1 | Batı taşı | (−9, 4) | 8 m | Geniş yüz ejderhanın başına (0, 14) bakar | Üst ucu çapraz kırık |
| KA-5.2 | Doğu taşı | (9, 4) | 7 m | Başa bakar | Üst ucu kırık, bir köşesi kopmuş, kopan parça dibinde |
| KA-5.3 | Güney taşı | (0, −10) | 6 m | Başa bakar (kuzey) | En çok kırılan; kısa olduğu için başlangıç kamerasında ejderhayı kapatmaz |

| Kimlik | Madde | Değer | Asset | Doğrulama |
|---|---|---|---|---|
| KA-5.4 | Taban ve çarpışma | Taban 2 × 1,5 m (geniş yüz 2 m); çarpışma kutusu taban ölçüsünde, 6 m yükseklik (KE-12.1) | `Ortam/runtasi/runtasi.fbx` ölçeklenir; `runtasi_oyma_uv` maskesi oyma harfleri taşır | T |
| KA-5.5 | Oymalar | Her taşın iki geniş yüzünde Ejderha Dili harfleri; sönük kor 0,3, görevde 3,0 (SI-5.6) | `runtasi_oyma_uv` + `07` §1.1 harf atlası | K |
| KA-5.6 | Görev çemberi | Batı ve doğu taşında güney yüzün 2 m önünde: (−9, 1,25), (9, 1,25); güney taşında kuzey yüzün 2 m önünde: (0, −7,25). r 2 m, yalnız görevde görünür (beyaz kenar) | Zemin decal'i | T |
| KA-5.7 | Gölge şeritleri (Kül Yağmuru) | Başın (0, 14) taşa bakış çizgisi boyunca, taşın arka yüzünden 1 m sonra başlayıp 6 m uzanan 3 m genişliğinde şerit. Batı taşı: (−10,2, 2,7) → (−14,2, −1,8); doğu taşı: (10,2, 2,7) → (14,2, −1,8); güney taşı: (0, −11,8) → (0, −17,8). Yalnız görevde görünür | Zemin decal'i | T |
| KA-5.8 | Kor Dalgası gölgesi | Kor Dalgası'nda (KE-5.7) korunan alan: taşın arkasında, başa göre taşın gölgesi (3 m genişlik, taştan 8 m'ye kadar). Görünmez; hesap çarpışma ışınıyla | — | T |

---

## 6. Duvar ve yamaç

| Kimlik | Madde | Değer | Asset | Doğrulama |
|---|---|---|---|---|
| KA-6.1 | Çarpışma | Tek parça halka: iç yüz 18 m, 4 m yükseklik, kesintisiz; boss sektörü dahil (O-7.2, O-7.3) | Kodla üretilen halka çarpışması (teknik hattın `fix/arena-siniri-tek-kaynak` işi) | T, G |
| KA-6.2 | Kaya duvarı (azimut 30°–330°) | 18 kaya yüzü parçası, iç yüzleri r 18 m'de, yüksekliği 6–9 m arasında düzensiz, merkeze bakar; komşu parçalar en az 1 m bindirir (arada boşluk yok) | `PolyHaven/rock_face_01`, `rock_face_02` (4000'er üçgen), dönüş ve ölçek çeşitlemesi | K (SI-13.1 ortam turu) |
| KA-6.3 | Duvar dibi kayaları | 10 kaya bloğu r 17,8–19 m'de, yarısı duvara gömülü; oynanış alanına en çok 0,2 m taşar, çarpışmaları duvarın içinde kalır | `PolyHaven/boulder_01` (3000 üçgen) | T (r < 17,8 m'de kaya yok) |
| KA-6.4 | Ejderha duvarı (azimut 330°–30°) | Duvarın görünen yüzü ejderhanın göğsüdür (KA-3.4) | `13` D4 (ejderha gövdesi) | K |
| KA-6.5 | Yamaç | 22–60 m arasında dışa doğru yükselen kaya; kenar çizgisi r 45–60 m'de 25–40 m yükseklikte. 8 yamaç parçası | `PolyHaven/mountainside`, `namaqualand_cliff_02` | K |
| KA-6.6 | Güney basamakları | Azimut 175°–185° arasında, duvarın üstünden (r 22 m) yamaca (r 40 m) çıkan eski oyma basamaklar: basamak 0,18 m yüksek, 0,35 m derin, 2,2 m geniş, yer yer kırık. Savaşçıların krater'e indiği yol; çarpışma yok | Blender'da basit modüler basamak (yeni, küçük iş) | K |
| KA-6.7 | Uzak dağlar | 60–250 m'de 3 katman silüet; en yakını 60–90 m, en uzağı 200–250 m | `DenemeSahnesi/Meshes/Skyline.obj` + `FogSilhouette` shader | K |
| KA-6.8 | Kenar sisi | Duvarın tepesinde ve yamaç diplerinde yumuşak sis halkası | `DenemeSahnesi/Meshes/EdgeMist.obj` + `EdgeMist` shader | K |

---

## 7. Eski silahlar (ölçek referansı)

SI-7.3. Yere eğik saplanmış, paslı, kırık kılıç ve mızraklar. Çarpışmasız. 16 adet.

| Kimlik | Grup | Konumlar |
|---|---|---|
| KA-7.1 | Duvar dibi (8) | Azimut 140°, 165°, 195°, 220°, 245°, 265°, 290°, 315°; r 16,5–17,5 m |
| KA-7.2 | Taş çevresi (6) | Her taşın 1,5–2,5 m yakınında 2 tane; görev çemberinin ve gölge şeridinin içine düşenler serbest (hikâye: savaşçılar burada siper almış) |
| KA-7.3 | Açık alan (2) | (−8, −5), (8, −6) |

| Kimlik | Madde | Değer | Asset | Doğrulama |
|---|---|---|---|---|
| KA-7.4 | Ölçü ve duruş | Boy 1,1–1,3 m (görünen kısım 0,8–1,0 m), dikeyden 10–25° eğik, 0,3–0,5 m gömülü, yönleri rastgele | `k3/Silahlar/Kilic.fbx`, Quaternius `Warrior_Sword`, `Rogue_Dagger`; paslı malzeme SI-5.7 | K |
| KA-7.5 | Çatlakla ilişki | Hiçbiri bir tehdit çatlağının şeridine (1,5 × 4 m) girmez | — | T |
| KA-7.6 | Ayrıntı seviyesi | 10 m'den uzakta LOD1 (üçgenlerin %50'si) | — | T |

---

## 8. Başlangıç, ışık, sis, ses ve gezinme

| Kimlik | Madde | Değer | Kaynak |
|---|---|---|---|
| KA-8.1 | Oyuncu başlangıcı | (0, −14), kuzeye bakar. Çok oyunculuda r 14 m çemberi üstünde 2 m arayla: (0, −14), (±1,99, −13,86), (±3,95, −13,43) | KE-12.4, O-7.5 |
| KA-8.2 | Uyanma çizgisi | Z = −2 | KE-12.5 |
| KA-8.3 | Güneş | SI-3.1: azimut 315°, yükseklik 20° | SI-3.1 |
| KA-8.4 | Işık sondaları | Oynanış alanında 4 m ızgara, 3 yükseklikte (0,5 m, 3 m, 8 m); duvar halkası üstünde 30°'de bir. Kor dolgu ışığı bunlara pişirilir (SI-3.4) | ÖNERİ |
| KA-8.5 | Yansıma sondası | Merkezde tek, 40 m kutu, pişirilmiş | ÖNERİ |
| KA-8.6 | Ek ışıklar | Faz 3 kalp ışığı (0, 3, 13); ejderha ateşi sırasında ağız ucunda. Başka gerçek zamanlı ışık yok (SI-3.5) | SI-3.5 |
| KA-8.7 | Yer sisi kartları | 6 kart, azimut 30°, 90°, 150°, 210°, 270°, 330°'de, r 20 m merkezli, 0,3 m/s sürüklenir; arena merkezine doğru opaklık SI-4.3'e göre azalır | SI-4.3 |
| KA-8.8 | Ses kaynakları (yer) | Rüzgâr (2D, sürekli); kraterin uğultusu (3D, (0, 0, 24), göğsün altında); ejderha nefesi (3D, burun delikleri); çatlak cızırtısı (3D, yalnız alev sırasında, çatlakta). Ses tasarımı sonra (`13` D7) | ÖNERİ |
| KA-8.9 | Gezinme ağı (yapay zekâ dostları) | Oynanış alanında, ajan yarıçapı 0,35 m (r ≤ 17,65 m); taşlar, pençeler ve faz 2 bacakları ağı oyar (carve). Faz geçişinde ağ yeniden hesaplanmaz, engeller güncellenir | KE-10.2 |

---

## 9. Veri ve bütçe

| Kimlik | Madde | Değer |
|---|---|---|
| KA-9.1 | Yerleşim verisi | `Resources/Arenas/krater.json`: bölge yarıçapları, çatlak listesi (merkez, yön, çeşit), taşlar (merkez, yükseklik, kırık çeşidi), görev çemberleri, gölge şeritleri, eski silahlar, duvar ve yamaç parçaları (asset, konum, dönüş, ölçek), sis kartları, ışık sondası ızgarası, başlangıç noktaları. Eski `slice_arena` ve `slice_arena_visuals` blokları bu dosyanın yerine geçtiğinde kalkar |
| KA-9.2 | Üçgen bütçesi (ekranda, en kötü an) | Zemin ~28 bin · görünen duvar ~40 bin · yamaç ~20 bin · uzak ~2 bin · taşlar ~6 bin · eski silahlar ~12 bin (LOD ile) · ejderha 40 bin · 5 oyuncu ~85 bin → ~233 bin (sınır 300 bin, O-15.6) |
| KA-9.3 | Çizim çağrısı | Duvar ve yamaç parçaları GPU instancing ile; eski silahlar tek malzeme. Hedef ≤ 150 (O-15.5) |

---

## 10. Gri kutu (`13` C1)

Model yapılmadan önce telefonda denenecek ilk sürüm. Her şey gerçek ölçüde, düz renkli basit şekiller.

| Kimlik | Parça | Gri kutu şekli |
|---|---|---|
| KA-10.1 | Zemin | r 30 m düz disk, #26282C |
| KA-10.2 | Duvar | r 18–22 m halka, 4 m yüksek (çarpışma) + görüntü için 7 m yüksek gri silindir halkası; kuzey sektörü (330°–30°) boş, orada gövde durur |
| KA-10.3 | Ejderha | Baş: 9 × 7 × 6 m kutu (burun (0, 7,5)); pençeler 5 × 4 × 4 m kutu, (±8, 13,7); gövde: `ejder_govde` v0 doğrudan kullanılır (pivot arena merkezinde); gözler iki turuncu küre |
| KA-10.4 | Taşlar | 2 × 1,5 m taban, 8 / 7 / 6 m yükseklikte kutular |
| KA-10.5 | Çatlaklar | 15 turuncu şerit (4 × 0,4 m) |
| KA-10.6 | Eski silahlar | 16 ince çubuk (1,1 m) |
| KA-10.7 | Sis ve ışık | SI-3 ve SI-4 değerleri baştan açık (sis ölçek hissinin yarısıdır) |

**Gri kutuda ölçülecekler (`13` C2):**

| Kimlik | Ölçüm | Hedef |
|---|---|---|
| KA-10.8 | Kilitli kamerada başın ekran payı | %25–60 (SI-7.2) |
| KA-10.9 | Taşlar arası koşu | Batı ↔ doğu taşı 18 m ≈ 2,8 s; yan taş ↔ güney taşı 16,6 m ≈ 2,6 s (6,4 m/s); Uyku Mührü (15 s) ve Kor Mührü (10 s) rahat ama boş değil |
| KA-10.10 | Kül Yağmuru | Arenanın en uzak noktasından en yakın gölge şeridine ≤ 8 s'nin yarısı (≤ 4 s) |
| KA-10.11 | Kor Dalgası | Halka içinden dodge ile geçiş telefonda yapılabiliyor mu (0,19 s geçiş, 0,26 s hasarsızlık) |
| KA-10.12 | Okunurluk | SI-13 sayfaları (ortam turu, gri tonlama, kor sayımı) |

---

## 11. Kaldırılanlar

Karar A3 ve `12` §12 gereği. Silme işi teknik hatta, kendi PR'ında yapılır; diğer ajanların dallarına
dokunulmaz.

| Kimlik | Kalkan | Yer |
|---|---|---|
| KA-11.1 | Ağların Kraliçesi (örümcek) | `Art/Bosses/Spider/`, `Resources/Bosses/aglarin-kralicesi.json`, ilgili kod |
| KA-11.2 | Kaburgalar | `Art/Ortam/kaburga/`, `slice_arena_visuals.ribs`; `art/ortam-yerlesim` dalındaki kaburga işi bu tasarımla geçersiz |
| KA-11.3 | Kozalar | `Art/Ortam/koza/`, `slice_arena` koza girdileri |
| KA-11.4 | Köy duvarı ve sütunlar | `Art/Ortam/duvar/`, `Art/Ortam/sutun/` (sütunların yerini dikili taşlar alır) |
| KA-11.5 | `10-olculer.md` O-7.3–O-7.9 | Bu belgeye bağlanır (`13` B4) |
