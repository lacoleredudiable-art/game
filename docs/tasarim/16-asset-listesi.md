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
hiç kıpırdamaz (`14` KE-2.2). Beş parça üretilir, Blender'da birleştirilir, üç dosya olarak çıkar.

| Kimlik | Parça | Tripo'da ayrı üretim | Çıkış dosyası | Hareket |
|---|---|---|---|---|
| AL-1.1 | Bütün ejderha (kaba) | Evet, bir kez | — (yalnız oran ve gövde için kaynak) | — |
| AL-1.2 | Baş + boyun | Evet, en çok özen burada | `ejder_on.fbx` (tek iskelet: baş, boyun, göğüs, iki ön bacak) | Canlı |
| AL-1.3 | Ön bacak + pençe | Evet, bir tane; Blender'da aynalanır | `ejder_on.fbx` içinde | Canlı |
| AL-1.4 | Göğüs ve omuzlar | AL-1.1'den kesilir | `ejder_on.fbx` içinde | Canlı (faz 2'de kalkar) |
| AL-1.5 | Gövde (sırt, kalça) | AL-1.1'den kesilir | `ejder_govde.fbx` | Sabit |
| AL-1.6 | Kuyruk | Evet | `ejder_kuyruk.fbx` (zincir iskelet) | Canlı (faz 2 savuruşu) |

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
Head and neck of an ancient stone dragon, chin resting on the ground, long wedge-shaped skull, heavy brow
ridge, two thick backward-swept horns like weathered rock, mouth closed with a few stone teeth visible at
the edge, deep-set narrow eyes with vertical slit pupils, overlapping large grey-blue basalt plates with
fine cracks, ash on upper surfaces, noble, calm, ancient, angular blocky forms, symmetric, matte rock,
realistic, no thin spikes, no bright colors
```

**AL-1.9 — Ön bacak ve pençe:**
```
Front leg and claw of a colossal stone dragon, planted firmly on the ground, four thick talons like broken
dark rock, forearm armored with large rock plates, weathered grey-blue basalt, ash on top surfaces,
angular blocky shapes, matte stone, realistic, no thin spikes
```

**AL-1.10 — Kuyruk:**
```
Long thick tail of a stone dragon lying on the ground, slowly tapering, covered in overlapping rock plates,
a ridge of short broken stone spines along the top, grey-blue basalt, ash on top surfaces, matte stone,
realistic
```

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
| AL-1.17 | Ölçü (pençe) | Taban 4 m uzun, 3 m geniş; ayak bileği 4 m yükseklikte, omuz 8 m | KE-2.4, KA-3.2 |
| AL-1.18 | Ölçü (bütün) | Burundan kuyruk ucuna ~45 m, gövde arenanın kuzeydoğu kenarı boyunca kıvrık (KA-3.3–3.6'daki noktalar) | KE-2.2 |
| AL-1.19 | Üçgen | `ejder_on` ≤ 26 000 (baş + boyun 14 000, göğüs 4 000, her bacak 4 000) · `ejder_kuyruk` ≤ 4 000 · `ejder_govde` ≤ 10 000 → toplam ≤ 40 000 | O-15.6 |
| AL-1.20 | Doku | Her dosya tek doku seti; `ejder_on` 2048, gövde ve kuyruk 1024 + tekrarlayan kaya ayrıntı dokusu. Albedo SI-5.3 ailesine çekilir: taban #3A4048 (±5 L\*), boynuz ve pençe #5B5A57, yukarı bakan yüzlerde kül. **Emission maskesi** ayrı kanal: gözler, pul araları, göğüs çatlakları (kalp), boyun çatlakları. Doku ASTC | SI-5.3–5.5, O-15.7 |
| AL-1.21 | Kesimler | Alt çene ayrı parça (en çok 10° açılır, SI-8.6); göz kapakları şekil anahtarı (blend shape) ya da kemik (göz kırpma, SI-8.2); göğüs plakaları ayrı parçalar (faz 3'te açılır, KE-7.1) ve altında kalp (kor çekirdek) | SI-8, KE-7 |
| AL-1.22 | Yön ve pivot | Baş +Z'ye bakar (arenada kod 180° döndürür). `ejder_on` sıfır noktası iki ön pençenin ortasında, zeminde; `ejder_kuyruk` sıfır noktası kuyruk kökünde; `ejder_govde` sıfır noktası arena merkezine göre konumlanmış (KA-3) | O-16.1 |
| AL-1.23 | İskelet (`ejder_on`) | Kök (göğüs) → Boyun_1–4 → Bas → Cene; Goz_Kapak_L/R; her bacak için Omuz → Kol_Ust → Kol_Alt → Bilek → Parmak_1–4. Adlar Türkçe karakter içermez | O-16.6 |
| AL-1.24 | İskelet (`ejder_kuyruk`) | Kuyruk_01–08 zinciri | KE-6.6 |
| AL-1.25 | Dışa aktarım | FBX, ölçek 1,0, "Apply Transform" açık, Y yukarı / Z ileri, adlar `ejder_on`, `ejder_govde`, `ejder_kuyruk` | O-16.1, O-16.6 |
| AL-1.26 | Kontrol sayfası | Ön, yan, arka, oyun kamerası açısı (arena başlangıcından, (0, −14)) × 3 poz (yatıyor, faz 2 doğrulmuş, faz 3 göğüs yerde) = 12 görüntü | O-16.8 |

### 1.5 Animasyonlar (model onaylandıktan sonra)

| Kimlik | Klip | Süre | Kaynak |
|---|---|---|---|
| AL-1.27 | Nefes (bekleme) | 6 s döngü | SI-8.1 |
| AL-1.28 | Göz kırpma | 0,4 s | SI-8.2 |
| AL-1.29 | Uyanış | 4 s | KE-5.3 |
| AL-1.30 | Pençe Çakması (sol, sağ) | ön işaret 1,0 s + vuruş 0,2 s + açık kalma 1,2 s | KE-5.4 |
| AL-1.31 | Baş Savuruşu (iki yön) | 1,2 + 0,3 + 1,0 s | KE-5.5 |
| AL-1.32 | Kor Soluğu (yatarken, doğrulmuş, geniş) | Ön işaret + etkin süre + açık kalma, her biri kendi satırındaki gibi | KE-5.6, KE-6.5, KE-7.4 |
| AL-1.33 | Kor Dalgası / Göğün Çöküşü / Kraterin Kalbi (büyük saldırı başları) | 3 s duyuru + vuruş | KE-5.7, KE-6.7, KE-7.5 |
| AL-1.34 | Doğruluş (faz 1 → 2) | 4 s | KE-6.1 |
| AL-1.35 | Faz 2 bekleme, pençe çakması (yüksekten) | — | KE-6.4 |
| AL-1.36 | Kuyruk Savuruşu (`ejder_kuyruk`) | 2,0 + 0,4 + 1,5 s | KE-6.6 |
| AL-1.37 | Göğüs yere (faz 2 → 3), göğüs plakaları açılır / kapanır | 4 s; açılma 0,6 s | KE-7.1, KE-7.8 |
| AL-1.38 | Pençe Süpürmesi (sol, sağ) | 1,6 + 0,4 + 1,5 s | KE-7.2 |
| AL-1.39 | Sendeleme (yatarken, doğrulmuşken çöküş) | 7 s | KE-8.1 |
| AL-1.40 | Ölüm | 3 s | KE-7.12 |

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
