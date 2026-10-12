# 16 — Asset listesi ve iş kartları

Sürüm 1.0 · 12 Ekim 2026 · Kullanıcı: Mustafa

Kül Ejderi ve krater arenası için üretilecek asset'ler. Her biri için ölçü, üçgen ve doku bütçesi, kaynak ve
kabul kontrolü. Sıra `13-tasarim-hatti.md` D bölümündedir. İlk ve en uzun iş ejderhadır (§1); onarım planı
sürerken yapılabilir, kod beklemez.

Kimlik `AL-x.y`. Kabul kontrol sayfası O-16.8 yoludur: kullanıcı onaylamadan asset oyuna girmez.

---

## 1. Kül Ejderi — iş kartı (Tripo → Blender)

### 1.1 Parçalar

Ejderhanın tamamı tek modelde üretilmez: 45 m'lik bir modelde baş ayrıntısı kaybolur, ve gövdenin büyük kısmı
hiç kıpırdamaz (`14` KE-2.2). Dört parça üretilir, Blender'da birleştirilir, iki dosya olarak çıkar.

| Kimlik | Parça | Tripo'da ayrı üretim | Çıkış dosyası | Hareket |
|---|---|---|---|---|
| AL-1.1 | Bütün ejderha (kaba) | Evet, bir kez — **✔ 12 Ekim** (`ejder_kaba.glb`, Tripo 3 numaralı çeşit) | — (gövde ve kanat kaynağı) | — |
| AL-1.2 | Baş + boyun | Evet, en çok özen burada | `ejder_on.fbx` (tek iskelet: baş, boyun, göğüs, iki ön bacak) | Canlı |
| AL-1.3 | Ön bacak + pençe | Evet, bir tane; Blender'da aynalanır | `ejder_on.fbx` içinde | Canlı |
| AL-1.4 | Göğüs ve omuzlar | AL-1.1'den kesilir | `ejder_on.fbx` içinde | Canlı (faz 2'de kalkar) |
| AL-1.5 | Gövde ve kanatlar | AL-1.1'den kesilir — **v0 ✔ 12 Ekim** (baş ve bacaklar kesildi, 10 000 üçgen, arena konumunda) | `ejder_govde.fbx` | Gövde sabit, kanatlar canlı (Kanat Savuruşu) |
| AL-1.6 | Kuyruk | Yok (kanatların altında kıvrılı; Kanat Savuruşu kararı 12 Ekim) | — | — |

### 1.2 Tripo istemleri

Tripo İngilizce istemde daha iyi sonuç verir. Her istem için **en az 4 çeşit** üret, ekran görüntülerini bana
gönder; seçimi §1.3'e göre birlikte yapalım. Kullanıcının elindeki referans görsel (sisli kayada taş ejderha
başı) yalnız stil karşılaştırması için kullanılır; başkasına ait bir çizimse doğrudan görselden 3B'ye
verilmez.

**AL-1.7 — Bütün ejderha (kaba, oran için):**
```
Ancient colossal stone dragon lying asleep, body curled to one side, long heavy head resting flat on the
ground with chin down, two massive front legs planted forward, wings folded tight and fused into the back
like weathered rock ridges, scales like broken grey-blue basalt cliff plates, ash settled on top surfaces,
thin ember-orange glowing cracks between some plates, noble and very old, calm, angular blocky shapes,
realistic proportions, matte stone, no bright colors, no thin spikes
```

**AL-1.8 — Baş ve boyun (asıl model):**
```
Side three-quarter view of the head and long neck of an ancient stone dragon, the neck extends far back
behind the head and is as long as the head, chin resting on the ground, long wedge-shaped skull, heavy brow
ridge, two thick backward-swept horns like weathered rock, mouth closed, calm half-closed eyes with a faint
ember-orange glow, large angular grey-blue basalt plates with fine cracks, ash on top surfaces, noble,
ancient, serene expression, not angry, symmetric, matte stone, isolated single object on plain light grey
background, no ground, no scenery, no body, no legs
```
İlk deneme (12 Ekim) yalnız önden görünüş ve zeminli arka planla çıktı; 3B çevirmede derinlik tahmin edilemez.
Önden görünüşte stil olarak 3 numaralı çeşit (köşeli kaya plakaları, gövdeyle uyumlu) seçildi; yukarıdaki istem
o stili yandan ve uzun boyunla ister. Boyun, gövdenin kesik ön yüzünü (Z ≈ 18) kapatacak kadar uzun olmalı (KA-3.3).

**AL-1.9 — Ön bacak ve pençe:**
```
Front leg and claw of a colossal stone dragon, planted firmly on the ground, four thick talons like broken
dark rock, forearm armored with large rock plates, weathered grey-blue basalt, ash on top surfaces,
angular blocky shapes, matte stone, realistic, no thin spikes
```

**AL-1.10 — Kuyruk:** gerekmiyor (AL-1.6).

Tripo ayarları: en yüksek kalite modeli ve PBR dokuyu aç. Tripo'nun otomatik iskeleti insan ve dört ayaklı
gövde içindir; bu parçalara uymaz, **iskelet Blender'da kurulur**. Tripo'da düşük üçgenli (low poly /
retopology) çıktı seçeneği varsa AL-1.12'deki hedefle kullan, yoksa Blender'da indirilir.

### 1.3 Çeşit seçme ölçütleri

| Kimlik | Ölçüt | Geçer |
|---|---|---|
| AL-1.11 | Silüet | Düz siyah, 64 px yükseklikte: baş, boynuz ve pençe tanınır (SI-6.2). İnce dikenler ve saç gibi uzantılar yok |
| AL-1.12 | Biçim | Büyük, köşeli kaya plakaları (SI-6.1); yuvarlak, şişkin, yumuşak et görüntüsü yok |
| AL-1.13 | Yüz | Gözler derin ve dar, ağız kapalı; sevimli değil, öfkeli de değil: sakin ve yaşlı (SI-1.2) |
| AL-1.14 | Hata | Erimiş gibi bozuk yer yok (özellikle göz, boynuz kökü, pençe parmakları); simetrik |
| AL-1.15 | Renk | Önemli değil; doku Blender'da yeniden boyanır (AL-1.20) |

### 1.4 Blender kabul ölçüleri

Model bunları sağlamadan Unity'ye girmez. FBX'i bana gönderirsen ölçüleri ben de ölçerim.

| Kimlik | Madde | Değer | Kaynak |
|---|---|---|---|
| AL-1.16 | Ölçü (baş) | Çene yerdeyken 7 m yükseklik, burun ucundan kafatası arkasına 9 m | KE-2.3 |
| AL-1.17 | Ölçü (pençe) | Taban 5,2 × 4,2 m, bilek 1,6 m, dirsek 4,5 m, omuz 9,8 m yükseklikte (Tripo bacağı ×10, üst kol 2 kat uzatılıp göğse yatırıldı) | KE-2.4, KA-3.2 |
| AL-1.18 | Ölçü (bütün) | Burun Z 7,5; gövde Z 18–58, X ±18, 18 m yükseklik (KA-3) | KE-2.2 |
| AL-1.19 | Üçgen | `ejder_on` ≤ 26 000 (baş + boyun 14 000, göğüs 4 000, her bacak 4 000) · `ejder_govde` (kanatlar dahil) ≤ 10 000 → toplam ≤ 36 000 | O-15.6 |
| AL-1.20 | Doku | Her dosya tek doku seti; `ejder_on` 2048, gövde 1024 + tekrarlayan kaya ayrıntı dokusu. Albedo SI-5.3 ailesine çekilir: taban #3A4048 (±5 L\*), boynuz ve pençe #5B5A57, yukarı bakan yüzlerde kül. **Emission maskesi** ayrı kanal: gözler, pul araları, göğüs çatlakları (kalp), boyun çatlakları. Doku ASTC | SI-5.3–5.5, O-15.7 |
| AL-1.21 | Kesimler | Alt çene ayrı parça (en çok 10° açılır, SI-8.6); göz kapakları şekil anahtarı (blend shape) ya da kemik (göz kırpma, SI-8.2); göğüs plakaları ayrı parçalar (faz 3'te açılır, KE-7.1) ve altında kalp (kor çekirdek) | SI-8, KE-7 |
| AL-1.22 | Yön ve pivot | Bütün dosyalar arena koordinatlarında, sıfır noktası arena merkezi (0, 0, 0); Unity'de konum ve dönüş sıfır, baş güneye (−Z) bakar. Blender'da arena doğusu −X, kuzeyi −Y'dir (Unity sol el, Blender sağ el); bu kurala uymayan dosya aynalanmış çıkar | O-16.1 |
| AL-1.23 | İskelet (`ejder_on`) | Gogus → Boyun_1–5 → Bas (→ Cene, v1); Gogus → Omuz_L/R → Kol_Ust → Kol_Alt → Pence (parmak kemikleri yok); Goz_Kapak şekil anahtarı (v1). L = batı, R = doğu. Adlar Türkçe karakter içermez | O-16.6 |
| AL-1.24 | İskelet (`ejder_govde`) | Kok (sabit) → Gogus; Kok → Kanat_L_01–05, Kanat_R_01–05 | KE-6.6, KE-6.1 |
| AL-1.25 | Dışa aktarım | FBX, ölçek 1,0, "Apply Transform" açık, Y yukarı / Z ileri (Blender: ileri −Z, yukarı Y), adlar `ejder_on`, `ejder_govde` | O-16.1, O-16.6 |
| AL-1.26 | Kontrol sayfası | Ön, yan, arka, oyun kamerası açısı (arena başlangıcından, (0, −14)) × 3 poz (yatıyor, faz 2 doğrulmuş, faz 3 göğüs yerde) = 12 görüntü | O-16.8 |

### 1.5 Animasyon: poz kütüphanesi + kodla zamanlama (karar 12 Ekim)

Saldırılar klip olarak yapılmaz. Blender'da **donmuş pozlar** yapılır; Unity'de kod, pozlar arasında boss JSON'undaki
sürelerle geçiş yapar. Böylece ön işaret ile hareket aynı sayıdan okunur ve dengelemede süre değişince animasyon
yeniden yapılmaz. Yalnız 4 sinema anı klip olarak yapılır. Oyuncu karakterlerinin klip düzeni değişmez.

**Dışa aktarım:** her poz, `ejder_on` ve `ejder_govde` FBX'lerinde tek karelik bir klip olarak durur, adı
`Poz_<Ad>`. Dinlenme pozu = `Poz_Yatis`. Bir pozun dokunmadığı kemik dinlenme değerinde kalır.

#### 1.5.1 Pozlar

| Kimlik | Poz | Kemikler | Ne zaman |
|---|---|---|---|
| AL-1.27 | `Poz_Yatis` | Hepsi | Faz 1 bekleme (dinlenme pozu) |
| AL-1.28 | `Poz_Nefes_Cekme` | Boyun_1–5, Gogus | Kor Soluğu hazırlığı: boyun 3–5° kalkar, göğüs şişer |
| AL-1.29 | `Poz_Bas_Sola_Cekik` / `Poz_Bas_Saga_Cekik` | Boyun_3–5, Bas | Baş Savuruşu hazırlığı (savuruşun tersine çekilir, 35°, baş 2 m kalkık) |
| AL-1.30 | `Poz_Bas_Kalkik` | Boyun_2–5, Bas | Kor Dalgası hazırlığı (baş 6 m yükselir) |
| AL-1.31 | `Poz_Bas_Yere_Vurmus` | Boyun_4–5, Bas | Kor Dalgası vuruşu (çene yere çarpar) |
| AL-1.32 | `Poz_Bas_Goge` | Boyun_1–5, Bas | Göğün Çöküşü duyurusu (baş göğe) |
| AL-1.33 | `Poz_Pence_L_Havada` / `_R_` | Bacak zinciri | Pençe Çakması hazırlığı |
| AL-1.34 | `Poz_Pence_L_Yerde` / `_R_` | Bacak zinciri | Pençe Çakması vuruşu ve açık kalma |
| AL-1.35 | `Poz_Pence_L_Acik` / `_R_` | Bacak zinciri | Pençe Süpürmesi hazırlığı (yana açık) |
| AL-1.36 | `Poz_Pence_L_Supurme_Sonu` / `_R_` | Bacak zinciri | Pençe Süpürmesi sonu |
| AL-1.37 | `Poz_Dogrulmus` | Gogus, bacaklar, boyun, baş | Faz 2 bekleme (baş 15 m, bacaklar dik) |
| AL-1.38 | `Poz_Dogrulmus_Pence_L_Havada` / `_R_` | Bacak zinciri | Faz 2 Pençe Çakması |
| AL-1.39 | `Poz_Dogrulmus_Nefes` | Boyun, baş | Tepeden Kor Soluğu hazırlığı |
| AL-1.40 | `Poz_Gogus_Yerde` | Gogus, bacaklar, boyun, baş | Faz 3 bekleme (baş 10 m) |
| AL-1.41 | `Poz_Plaka_Acik` | Göğüs plakaları | Kor Mührü başarısı (kalp açık) |
| AL-1.42 | `Poz_Kanat_L_Acik` / `_R_` | Kanat_L/R_01–05 | Kanat Savuruşu hazırlığı |
| AL-1.43 | `Poz_Kanat_L_Supurme_Sonu` / `_R_` | Kanat zinciri | Kanat Savuruşu sonu |
| AL-1.44 | `Poz_Sendeleme` | Hepsi | Sendeleme (baş yerde yan yatık) |
| AL-1.45 | `Poz_Agiz_Acik` | Cene | Ayrı katman: soluk, kükreme, replik (çene en çok 10°, SI-8.6) |

#### 1.5.2 Geçiş kuralları (kod)

Her saldırı boss JSON'unda bir **poz dizisi** olarak yazılır: `[{poz, süre, eğri}]`. Süreler `14`'teki saldırı
satırından gelir; kod ayrı süre uydurmaz.

| Kimlik | Evre | Süre | Eğri | Örnek: Pençe Çakması (KE-5.4) |
|---|---|---|---|---|
| AL-1.46 | Hazırlık | Ön işaret süresinin tamamı | Yavaş başlar, yavaş biter (ease-in-out) | `Yatis` → `Pence_L_Havada`, 1,0 s |
| AL-1.47 | Vuruş | Etkin süre | Hızlanarak (ease-in) | → `Pence_L_Yerde`, 0,2 s |
| AL-1.48 | Açık kalma | Açık kalma süresi | Poz tutulur | `Pence_L_Yerde`, 1,2 s |
| AL-1.49 | Toparlanma | 0,8 s | Yavaşlayarak (ease-out) | → `Yatis` |

Kesilen saldırıda (KE §0) o anki poz 0,3 s'de `Poz_Sendeleme`'nin %40'ına karışır, 1,5 s tutulur, sonra bekleme pozuna döner.

#### 1.5.3 Sürekli katmanlar (kod, pozların üstüne eklenir)

| Kimlik | Katman | Değer | Kaynak |
|---|---|---|---|
| AL-1.50 | Nefes | Gogus ve Boyun_1: 6 s'lik sinüs, ölçek ±%2, dönüş ±1° | SI-8.1 |
| AL-1.51 | Göz kırpma | 8–12 s'de bir, 0,4 s; göz kapağı şekil anahtarı | SI-8.2 |
| AL-1.52 | Baş takibi | Bas + Boyun_5, hedef oyuncuya en çok 30 °/s, ±25° sınır; replik ve sendelemede kapalı | SI-8.3 |
| AL-1.53 | Kıpırtı | Bütün kemiklerde ±0,5° yavaş gürültü (0,2 Hz) | SI-8.5 |

#### 1.5.4 Sinema klipleri (4)

| Kimlik | Klip | Süre | Kaynak |
|---|---|---|---|
| AL-1.54 | `Klip_Uyanis` | 4 s | KE-5.3 |
| AL-1.55 | `Klip_Dogrulus` | 4 s | KE-6.1 |
| AL-1.56 | `Klip_Gogus_Yere` | 4 s | KE-7.1 |
| AL-1.57 | `Klip_Olum` | 3 s | KE-7.12 |

İlk sürümleri Claude Blender'da anahtar karelerle yapar; cilası sonra.

**Gövde iskeleti eki:** faz 2 doğrulmasında göğsün kalkabilmesi için `ejder_govde` iskeletine `Gogus` kemiği
eklenir (göğüs ön yüzünden sırtın ortasına; arka gövde Kok'ta sabit). AL-1.24 buna göre okunur.


---

## 2. Arena asset'leri

| Kimlik | Asset | Kaynak | Durum | Bütçe | Kabul |
|---|---|---|---|---|---|
| AL-2.1 | Yakın ve uzak zemin | `DenemeSahnesi/Meshes/Ground_Near`, `Ground_Far` + `dark_rock`, `burned_ground_01` | Elde | 28 bin üçgen | SI-13.1, SI-13.3 |
| AL-2.2 | Tehdit çatlağı (3 çeşit, 4 m) | Blender, yeni | Yapılacak | 200 üçgen / parça | KA-4 |
| AL-2.3 | Süs çatlak ağı | `LavaCracks.obj` | Elde | 2,6 bin | KA-4.18 |
| AL-2.4 | Duvar kayaları | `PolyHaven/rock_face_01`, `rock_face_02` | Elde | 4 bin / parça, 14 parça, instancing | KA-6.2 |
| AL-2.5 | Duvar dibi kayaları | `PolyHaven/boulder_01` | Elde | 3 bin, 10 parça | KA-6.3 |
| AL-2.6 | Yamaç | `PolyHaven/mountainside`, `namaqualand_cliff_02` | Elde | 5–6 bin, 8 parça | KA-6.5 |
| AL-2.7 | Dikili taş (3 çeşit kırık) | `Ortam/runtasi` ölçeklenir, üst ucu Blender'da kırılır | Uyarlanacak | 2 bin / taş | KA-5 |
| AL-2.8 | Güney basamakları | Blender, modüler | Yapılacak | 1 bin | KA-6.6 |
| AL-2.9 | Eski silahlar (16) | `Kilic.fbx`, Quaternius `Warrior_Sword`, `Rogue_Dagger`; paslı malzeme | Uyarlanacak | 1,5 bin, LOD1 %50 | KA-7 |
| AL-2.10 | Uzak dağ silüetleri | `Skyline.obj` + `FogSilhouette` | Elde | 0,8 bin | KA-6.7 |
| AL-2.11 | Kenar sisi | `EdgeMist.obj` + `EdgeMist` | Elde | 0,2 bin | KA-6.8 |
| AL-2.12 | Yer sisi kartları | Shader + 6 kart | Yapılacak | — | KA-8.7 |

## 3. Efektler

| Kimlik | Efekt | Kaynak | Sınır |
|---|---|---|---|
| AL-3.1 | Havada kül, kor kıvılcımı | Parçacık | SI-4.4, SI-4.5, SI-4.7 |
| AL-3.2 | Kül fırtınası | Sis + parçacık | SI-4.6 |
| AL-3.3 | Darbe tozu | Parçacık | SI-9.4 |
| AL-3.4 | Boss ateşi (koni, şerit) | Mesh + shader + duman | SI-9.1 |
| AL-3.5 | Kor Dalgası ve Kor Halkası | Halka mesh + shader | KE-5.7, KE-7.3 |
| AL-3.6 | Pul kırılması | Kopan pul mesh'i (`ejder_on`'dan) + kıvılcım | SI-9.7 |
| AL-3.7 | Ortam tehditleri (çatlak alevi, düşen kül parçası, titreyen zemin) | Mesh + parçacık + ön işaret | SI-9.3 |
| AL-3.8 | Ön işaretler (daire, yay, koni, şerit, halka) | Zemin decal'i, O-6.2 | O-6.2, O-6.3 |
