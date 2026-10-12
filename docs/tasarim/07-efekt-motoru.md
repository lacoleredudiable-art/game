# Bölüm 7: Efekt motoru

*Kaynak: `karakterler/v4/efekt-motoru.md`*

## v4 · Efekt motoru: kurala bağlı VFX (fiil rengi, sıfat şekli, silah teslimi)

Hazırlayan: Animatör (A'nın görevi), 9 Ekim 2026. Güncelleme 20:50: K 20:39–20:40 fiziksel / büyü fiili ayrımı (§0, §3.3, §6, §6.1, §7, §9). Karar: `v3-kararlar.md` 19:48. "(2) kurala bağlı efekt motoru (VFX): fiil rengi/niyeti, sıfat şekli … efekt kurala göre biner."
Kaynak:
- `kural-kitabi-v4.md`: §1.7 tasma, §1.13 element, §1.14 zaman, §1.16 mermi/alan, §3 sıfatlar, §6 hücreler
- `kural-ekleri-dongu.md`, `sim-kural-ekleri.md` K8
- `dunya-fizigi.md`
- `silahlar.md`
- `karakter-boru-hatti.md` §5.2 (kor shader), §8.1 (dissolve)
- `animasyon-kurallari.md` (eş belge)

`arsiv/` kullanılmadı. Oyun reposuna dokunulmadı.
Güncelleme 9 Ekim 20:39: K 20:29–20:37 kararları eklendi (silah tarzı, Hareket görünümleri, Kılıç akan kor izi, §3.1–3.2).
Güncelleme 22:20: **K 22:15** (grup kontrolü): Ortak 5 s yedeği ve Tetikli süre sonu yok, boşa gider (§2, §5.5 sönüş önerisi); vuruş ağırlığı hasar çarpanına bağlanabilir (§4.1 öneri); Koruma her alan tikinde parlama (§5.5); kenar freni kıvılcımı; Güdümlü kilit bitişi; kapsül yarıçapları, kenardan menzil, Yörüngeli = taşıyan + 2 m (§2, §5.4); Yansıma tavanı (§5.5).
Güncelleme 21:10: **K 21:05** işlendi. Teslim sınıfı kesin (§3 tablo, S-E1 kapandı); Kılıç VUR yerinde iaido, Zenitsu yalnız ATIL (§3.1, §3.2, §3.4); Kalkan VUR kenarla indirme, omuz hücumu ve kükreyen baş ATIL'da (§3, §3.4); süper zırh vb. silah kanunu kalıntısı yok (AS-03); Güçlendirme = yürek koru, Arındırma = pul dökme (§1); palet 3 rol ailesi kalır; kın yok.
Güncelleme 9 Ekim 21:05: **K 20:58 eşleşme denetimi onayı:** büyüde VFX jesti yapan elden, Top'ta her zaman namludan (§3.3, §6, §6.1).
Güncelleme 9 Ekim 20:55: **K 20:42 ejderha lore katmanı** (§0, §1 motif sütunu, §1.1 rün → Ejderha Dili harfi, §3.4 ejderha silüeti anları, §4 skill uyanışı, §7, §9, §10 S-V7/S-V8, §11 S-E9…S-E14). Kaynak ayrıca `kural-kitabi-v4` §8.4 (GameDesign lore önerileri 1, 3, 4 + Kontrol zinciri). Hepsi **yalnız görünüm**, mekanik değişmez.

> Onaylı kurallar test edilene kadar kilitli. Yeni fikirler §10 **Sonra** listesinde. Kilitli kararla çakışanlar §11'de açık soru; her birinde ne yeniden yazılır, kaç gün. Gün tahminleri **[tahmin]**.

---

### 0. Kısa özet
- **Efekt = 5 kanalın üst üste binmesi, her kanalın tek sahibi var:**
  1. **Fiil → renk ailesi + motif** (niyet)
  2. **Sıfat → şekil** (halka, hat, sekme, yörünge, zincir, kilit izi, yapı, işaret…)
  3. **Silah → taşıyıcı** (ok, harf, gülle, ışın, mühür, kesik, çatlak, kor fışkırması…)
  4. **Oyuncu → sahiplik rengi** (yalnız işaret, zincir, tasma ucu ve yük halkası)
  5. **Element → küçük ikincil kıvılcım** (yalnız kenar, çekirdeğe dokunmaz)
- Hücre başına efekt yok. `VfxPlan = Coz(SkillSpec)`, animasyondaki gibi istisnasız (§6).
- **Primitifler çoğunlukla shader'lı mesh**, parçacık değil: halka = quad + SDF halka shader'ı, hat = uzatılmış quad, tasma = şerit mesh. Parçacık yalnız kıvılcım, toz ve kırıntıda.
- **Particle System (Shuriken) + Shader Graph** önerilir. **VFX Graph** mobilde yalnız compute shader + SSBO destekli cihazlarda çalışır, **OpenGL ES'i desteklemez** ve URP'de hâlâ yalnız bazı platformları kapsar. Geniş Android hedefi için ana yol olamaz (§8, kaynaklı).
- Kor shader'ı (`_Intensity`) karakter ve silahta **kuralın sayacıdır**: şarj dolumu, Fedakarlık bedeli, Güçlendirme süresi, silah değişimi.
- **Kılıç akan kor izi** (K 20:37): mesh trail önerilir, Trail Renderer prototip ve LOD için (§3.1). Hareket görünümleri silahtan (§3.2).
- **Büyü fiilleri (K 20:40):** 8 büyü fiilinin animasyonu basit ve silahtan bağımsız (`animasyon-kurallari` §2.6). Fiiller arasındaki ve silahlar arasındaki farkı **efekt taşır**: fiil motifi (§1) + silah taşıyıcısı (§3) + elde silahın kor yankısı (§3.3).
- **Ejderha lore katmanı (K 20:42), yalnız görünüm:** (1) çizilen rün 0,2 s'de Ejderha Dili harfine oturur (§1.1); (2) her fiil bir ejderha bedeni motifi taşır (§1); (3) skill sırasında silahın çatlakları korla uyanır (§4); (4) fiziksel imza hareketlerinde kısa kor ejderha silüeti anı: tek mesh kartı + dissolve, 0,2–0,45 s, uzak oyuncuda kapalı (§3.4).
- İş ≈ **27 gün** [tahmin] (§9; mesh trail +1, büyü soketi ve silah yankısı +0,25, ejderha katmanı ≈ +4).

---

### 1. Kanal 1: Fiil → renk ailesi ve motif

**Kural:**
- Renk ailesi **rolden** gelir (3 aile).
- Fiil, ailenin içinde **ton ve motif** seçer.
- On iki ayrı renk telefonda ve 5 oyunculu kaosta okunmaz. Üç aile + motif okunur.

Dünya sisli ve gri (`karakter-boru-hatti` okunurluk notu); efekt rengi bu yüzden taşıyıcı. **Öneri:** fiil paletinde mavi yok. Su elementinin rengi mevcut kodda `ElementWater` #47B8FF, mana barı da mavi; fiil rengi element kanalıyla karışmasın (S-E7). Koruma bu yüzden kemik/altın tonundadır.
Değerler lineer HDR. Bloom eşiği 0,95 olduğu için çekirdek parlaklığı ×2–4.

| Rol | Aile | Fiil | Çekirdek renk (lineer RGB) | Yoğunluk | **Ejderha motifi** (K 20:42; şekilden bağımsız işaret) |
|---|---|---|---|---|---|
| Saldırı | **Kor kırmızı-turuncu** | Zarar | (1,0, 0,28, 0,06) | 3,0 | **pençe:** üç paralel pençe kesiği; kıvılcım patlaması pençe izi biçiminde, hedefte 3 çizgilik kesik |
| | | Zayıflatma | (0,55, 0,16, 0,10) koyulaşan | 1,5 | **is:** kurum ve is bulutu hedefe çöker, renk solar (hedefte desatürasyon) |
| | | Çağırma | (1,0, 0,42, 0,10) | 2,5 | **kemik:** yardımcı kor dolan bir kemik kafesten doğar (kordan beden + kemik çatlak maskesi) |
| | | Hareket | (1,0, 0,55, 0,20) | 1,5 | **kanat:** iz (trail) iki yana açılıp kapanan kanat çırpışı biçiminde + ayak altında kor kıvılcım |
| Tank | **Kemik beyazı-altın** | Koruma | (1,0, 0,85, 0,55) | 2,0 | **pul:** ejder pulu kubbe parçaları (oyuncunun etrafında, yerinde) |
| | | Yansıma | (1,0, 0,95, 0,80) | 2,5 | **pul parlaması:** tek büyük pulda ayna parıltısı, kırılıp dönen kıvılcım |
| | | Kontrol | (0,9, 0,6, 0,25) | 2,0 | **kuyruk zinciri:** tasma ejder kuyruğu omurlarından zincir (§5.3) + hedefte kor halka |
| | | Kuvvet | (1,0, 0,75, 0,40) | 2,0 | **kuyruk yayı:** şok dalgası kalın, uca doğru incelen kuyruk savuruşu yayı + toz |
| Destek | **Sıcak yeşil-altın (amber)** | Şifa | (0,55, 1,0, 0,35) | 2,0 | **nefes buğusu:** dostun üstünde yükselen sıcak buğu (yumuşak sis kartı) + ışık damlaları |
| | | Güçlendirme | (1,0, 0,80, 0,25) | 2,5 | **yürek koru nabzı** (K 21:05, kesin): dostun göğsünde kor bir kalp atışı nabzı yayılır, silahı tutuşur (§4) |
| | | Arındırma | (0,95, 1,0, 0,70) | 2,5 | **pul dökme / deri değiştirme** (K 21:05, kesin): kara, kirli pullar dostun üstünden soyulup savrulur, altından temiz kor pul görünür |
| | | Zaman | (0,75, 0,9, 0,55) solgun | 1,5 | **halkalı pul:** yaş halkalı pul dönerek geri akar + **hayalet iz** (hedefin son 3 s'lik yolu soluk siluetle geri akar) |

**Taraf işareti (okunurluk):**
- Düşmana giden efekt **dış kenarı keskin**.
- Dosta giden efekt **yumuşak kenarlı ve yükselen**.
- Sana işleyen efekt **gövdeye yapışık**.

Bu kenar kuralı renkten bağımsızdır. Renk körlüğünde de çalışır.

**Motif kuralı (K 20:42):** motif fiilin **bir ejderha bedeni parçasıdır** ve renkten önce okunur. Şekil (§2) motifi taşır, motifi değiştirmez: Yayılan-Zarar halkasının kenarı pençe kesikleridir, Çizgisel-Kuvvet hattı kuyruk yayıdır. Motif dokuları tek atlas satırında (A2, fiil başına 1 hücre, 12 hücre). Paletle çatışma → S-E11.

#### 1.1 Rün çizimi → Ejderha Dili harfi (K 20:42)
- Oyuncunun parmakla çizdiği çizgi **0,2 s içinde** fiilin Ejderha Dili harfine oturur (çizgi noktaları harfin noktalarına lerp). Ardından sıfat harfi yanında oturur.
- **Flaş çizim sırasıyla:** önce fiil, sonra sıfat (`AK-VORN` = Yoğun Zarar; kitap §1.1, K 9 Ekim). GameDesign örneğindeki `VORN-AK` sırası kullanılmaz (kitap AS-16).
- İki harf yan yana 0,3 s parlar (kor `_CoreColor`, bloom) ve söner. Teslim anına kadar küçük harf karakterin üstünde kalabilir [öneri].
- **Uygulama:** UI katmanında (ekran uzayı) tek emissive harf atlası: 24 harf (12 fiil + 12 sıfat), 512², ASTC 6×6 ≈ 120 KB [hesap]. Çizgi zaten UI'da çiziliyor; ek parçacık yok. Kod ≈ 0,5 gün, harf glifleri tasarım işi (S-E12).
- Mekanik değişmez: tanıma (hangi rün) bugünkü gibi çizgiden; harf yalnız sonucu gösterir.

---

### 2. Kanal 2: Sıfat → şekil (primitif)
Her sıfat tek bir şekil primitifi verir. Primitif, fiilin rengi ve motifiyle boyanır. Ölçüler kural kitabından gelir, VFX bunları **asla kendisi seçmez**: `VfxPlan` spec'ten okur.

| # | Sıfat | Primitif | Görüntü kuralı | Ölçü (kitaptan) |
|---|---|---|---|---|
| 1 | Yoğun | **Çöküş noktası** | Şarjda kor içe çöker (ters parçacık + `_Intensity` 1→2,5), teslimde tek büyük parlama. Şarjsız fiillerde yalnız tek parlama. Koruma/Yansıma'da 0,45 s'lik dar pencere halkası | şarj 0,35 s, pencere 0,45 s, ×1,35 |
| 2 | Odaklı | **Hedef kilidi** | Seçilen hedefte tek, ince nişan halkası (0,3 s), teslim düz hat | — |
| 3 | Sıçrayan | **Sekme yayı** | Hedeften hedefe kor yay (bezier şerit), her sekmede küçülen parlama | 3 hedef, 6 m, ×0,5 |
| 4 | Sabit | **Yapı** | Duvar ya da direk mesh'i dissolve ile yerden yükselir. Gövdede fiil motifi, tabanında **3 m iş alanı halkası** (zemin decal'i). Nabızda halka bir kez parlar. Yıkılınca kor çatlaklarla dağılır | duvar 4×2×0,5 m, direk 1 m taban, 6 s |
| 5 | Yayılan | **Genişleyen halka** | Merkezden min(4 m, silah menzili)'ne 0,25 s'de açılan zemin halkası; değdiği hedefte bir kez parlama | min(4 m, silah menzili), en çok 5 hedef (K 21:10) |
| 6 | Güdümlü | **Kilit izi** | Hedefte döner nişan + senden/mermiden hedefe giden ince **takip çizgisi** (kilit sürdükçe). Kilit hedef ölünce/kaybolunca biter; bütün kilitlerde en geç 8 s'de biter (K 22:54): çizgi 0,15 s'de geri çekilip söner, nişan kapanır. İlk 3 s çizgi dolu, sonra kesikli (bozulabilir). Yakın silahta senin arkanda hız izi | 3 s kilit, 0,3 s vuruş arası |
| 7 | Tetikli | **Kapan işareti** | Hedefin üstünde kapanan bir sayaç halkası (5 s). Tetikte halka kapanır ve fiil motifi patlar. Süre tetiksiz dolarsa skill boşa gider: patlama yok, yalnız isteğe bağlı sönüş (§5.5, K 22:15) | en çok 5 s |
| 8 | Fedakarlık | **Bedel akışı** | Kullanıcının göğsünden kor "kan" sızar (karakter `_Intensity` flaşı + damlalar), sonra teslime akar | %30 güncel can |
| 9 | İşaretli | **İşaret + zincir + yankı** | İşaret: oyuncu renginde rün (zemin/canlı). Zincir: işaretleri sırayla bağlayan çizgi, 1→2→3→4. Yankı: oyuncunun hareketinin kordan silueti işarette oynar | işaret 20 s, kişi başı 4, zincir 4 s, 25 m |
| 10 | Çizgisel | **Hat** | Senden hedefe 1 m genişliğinde düz şerit, anında çıkar ve 0,2 s'de söner. Ayna/yön hâllerinde (Yansıma, Koruma) 2–3 s kalır. Hareket'te ışınlanma: çıkışta ve varışta kor dissolve, arada çizgi yok (çizgi kimseye işlemez) | menzil uzunluğu × 1 m |
| 11 | Ortak | **Yük halkası** | Dostun bileğinde/silahında senin oyuncu renginde halka + fiil motifi, 5 s sayaç. Dost skill atınca senin motifin **onun şeklinin üstüne** biner (onun primitifi, senin rengin). 5 s dolarsa **boşa gider** (yedek etki yok, K 22:15): halka yalnız sönüş efektiyle kaybolur (öneri, §5.5) | 5 s |
| 12 | Yörüngeli | **Yörünge parçaları** | **taşıyanın yarıçapı + 2 m** yarıçapta (oyuncu 2,35 m, boss 4,5 m; K 22:15) dönen 3 parça (silahın taşıyıcısından küçük mesh). Mermiyi tutunca/çevirince parlar | 3 parça, r + 2 m, 3 s |

**Değme göstergesi (bütün şekiller):** bir hedef etkiyi aldığında, hedefin üstünde fiil renginde 0,15 s'lik tek "değdi" flaşı (`HitFlash` rengi = fiil ailesi). Böylece şekil ne olursa olsun kime işlediği görünür.

---

### 3. Kanal 3: Silah → taşıyıcı (teslim görünüşü)
Silah, etkiyi **neyin taşıdığını** çizer. Taşıyıcının türü mermi/alan kuralını görünür kılmalı (§5).

**Teslim sınıfı (K 21:05, kullanıcı onayı, kesin):**
- **Yakın vuruş:** Yumruk, Kılıç, Çekiç, Kalkan. Varlık yok, darbe silahın kendisinde; VFX temas noktasında.
- **Mermi:** Yay, Top, Kitap. Uçan tek varlık, duvarda/ilk hedefte durur.
- **Alan:** Asa ışını, Küre konisi, Tılsım mührü. Geçer, şekli boyunca etkiler; görüntü yere/havaya serilir, uçan varlık yoktur.

Sınıf yalnız görüntüyü ve Güdümlü'de koşuyu belirler; silaha mekanik ayrıcalık vermez (AS-03).

| Silah | Taşıyıcı | Doğduğu soket / an | Teslim türü (görüntü) | Atlas |
|---|---|---|---|---|
| Yumruk | Eldivenden kor darbe halkası | `Socket_R/L`, VUR serisinin her vuruşunda | **yakın vuruş** | A2 |
| Yay | Kor uçlu ok (mesh + iz) | Yay ipi, bırakma karesi | **mermi** | A1 |
| Büyü Kitabı | Uçan kor harfler (3–6 harf sprite'ı) | Kitap sayfası | **mermi** (harf demeti tek varlık; K 21:05) | A2 + harf atlası |
| Kılıç | **Akan kor izi** (nefes tekniği tarzı, K 20:37): bıçağın arkasında akan, dalgalı şerit mesh; kesik yayı hedefte parlar (§3.1) | Bıçak boyunca (`Vfx_Kabza` → `Vfx_Tip`), `Trail_On/Off` event'leri | **yakın vuruş** | A1 |
| Küre | Avuçtan koni şeklinde kor fışkırması | `Socket_R` (küre eli, `karakter-boru-hatti` Küre), sıkma karesi | **alan** (koni; K 21:05) | A3 |
| Çekiç | Yer çatlağı (zemin decal'i) + kaya kırıntısı | Çekiç başı yere değdiği kare | **yakın vuruş** (K 21:05; çatlak yalnız görüntü) | A1 |
| Top | Gülle (mesh) + duman izi, düştüğü yerde açılma | Namlu ağzı | **mermi** (balistik görüntü, dikey fizik yok: yay yalnız görüntüde) | A1 |
| Asa | Kısa kor ışını (uzatılmış quad, UV kayan); hat boyunca bir anda serilir, uçmaz | Kristal ucu | **alan** (ışın hattı; K 21:05) | A1 |
| Tılsım | Hedefin ayağında beliren mühür decal'i, 0,5 s sonra patlama | Hedef konumu (yol yok) | **alan** (mühür; K 21:05) [gecikme → S-E2] | A3 |
| Kalkan | Kalkan alt kenarının indiği yerde şok yayı + ejder pulu kıvılcımı (VUR kenarla indirme, K 21:05 AS-15) | Kalkan alt kenarı (`Vfx_Core` → kenar) | **yakın vuruş** | A2 |

#### 3.1 Kılıç: akan kor izi (trail)
**Neden:** Silahın mekanik kanunu yok, tarz animasyon ve VFX'ten okunur (K 20:29). Kılıç'ın tarzı Demon Slayer nefes tekniği (`animasyon-kurallari` §2.2.1):
- yerinde iaido (VUR, ≈ 0 m, K 21:05 AS-14)
- Zenitsu şimşek atılışı (yalnız ATIL)
- Tanjiro su nefesi dönüşü (DÖNÜŞ / Yayılan-Zarar)

İz bu tarzın imzası: bıçağın arkasında **akan** kor.

| Klip | İz biçimi | Ömür | Not |
|---|---|---|---|
| VUR (iaido, yerinde) | Tek, keskin, kısa yatay yay; karakterin önünde yerinde kalır (yol yok). Kesiş karesinde tam parlak, sonra hızla incelir | 0,15–0,2 s | Hedefte kesik çizgisi `Kor_Flash` ile 0,1 s parlar |
| ATIL (Zenitsu şimşek atılışı) | Yol boyunca ince, kırık (şimşek) şerit + kuyruk yayı silüeti (§3.4) | 0,25 s | Hareket fiilinin "iz + kıvılcım" motifi (§1) bununla birleşir, ayrı iz açılmaz |
| DÖNÜŞ (Tanjiro, Yayılan-Zarar) | Tam tur yatay halka, **dalgalı ve akan**. UV boyunca kayan doku, kenarda yumuşak dalga | 0,3–0,4 s | Yayılan'ın min(4 m, menzil) halka primitifi (§2) aynı anda açılır. İz halkası görüntüdür, hasar alanı halka primitifidir |
| UZANMA, AĞIR | Kısa düz iz | 0,15 s | |

**Renk:** fiilin renk ailesi (§1). `silah-fiil-120`'deki "sarı kesik izi" şimşek biçimini anlatır [yorum]. Rengi yalnız Zarar fiili sarı-turuncuya yaklaştırır.

**Akış (su nefesi hissi), shader'da:**
- `_FlowOffset` UV.x boyunca kayar (≈ 2 tur/s [tahmin]).
- Kenar alfası sinüsle dalgalanır (genlik %15 [tahmin]).
- Baştan sona `_CoreColor` → fiil rengi → saydam gradyanı.
- Aynı `VFX_Kor` additive varyantı kullanılır (§8.3). Yeni keyword yok.

**Mobil uygulama, iki seçenek:**

| | A. Mesh trail (önerilen) | B. Unity Trail Renderer |
|---|---|---|
| Nasıl | Her karede iki nokta örneklenir: `Vfx_Kabza` ve `Vfx_Tip`. Halka tamponunda N = 16–24 örnek tutulur, aralarına Catmull-Rom ile 2× ara nokta eklenir, tek bir şerit mesh yazılır. Havuzlu, oyuncu başına 1 | Bıçağın ortasındaki boş nesneye `TrailRenderer`. Unity belgesindeki ayarlar: `Time` 0,2–0,4, `Min Vertex Distance` 0,05, `Alignment = TransformZ` (bıçak düzlemine dik eksene hizalı), `Emitting` event'le aç/kapa, `Texture Mode = Stretch` |
| Bıçak düzlemi | Doğru: iki uç noktadan geniş şerit, bıçağın taradığı yüzey | Tek noktayı izler, genişlik sabit eğri. Hızlı dönüşte bıçağın taradığı yüzey yerine bir şerit görünür |
| 30 fps'te hızlı dönüş | Ara nokta eklemesi dönüşteki kırıkları kapatır | Örnekler seyrek, köşeli olabilir. `Corner Vertices` yuvarlar ama yayın kendisini düzeltmez |
| Maliyet | Oyuncu başına ≈ 100–200 köşe, 1 çizim, CPU'da küçük döngü [tahmin] | Benzer köşe, 1 çizim. Hazır bileşen, kod yok |
| Ne zaman | Kılıç (imza), Çekiç ve Asa savuruşları da sonra aynı bileşeni kullanır | Hızlı prototip ve uzak oyuncu LOD'u |

**Öneri:**
- Kılıç için **A (mesh trail)**. Tarzın imzası olduğu için şeridin bıçak yüzeyini doğru taraması gerekir. Mevcut "sabit uzunlukta şerit mesh" kararıyla aynı aileden (§5.1).
- Prototipte bir gün içinde **B** ile görünüm onayı alınabilir, sonra A'ya geçilir.
- **Uzak oyuncu LOD:** N = 8, ara nokta yok, ömür ×0,5 (§7 `uzak_oyuncu.lod`).

**Soket ve event:**
- Kılıç prefabında `Vfx_Kabza` (siperin üstü) ve `Vfx_Tip` (bıçak ucu) boş nesneleri gerekir. `karakter-boru-hatti` §8 `Vfx_*` sözleşmesine eklenir.
- `Trail_On` / `Trail_Off` animation event'leri VUR, ATIL, DÖNÜŞ ve UZANMA kliplerinde (`animasyon-kurallari` §2.2.1).
- Event yoksa yedek: bıçak ucunun hızı > 6 m/s iken otomatik açılır [tahmin].

#### 3.2 Hareket görünümü (K 20:35), VFX payı
Mekanik ortak: yol alarak çekilme, ışınlanma yok. VFX klibe eşlik eder (klip: `animasyon-kurallari` §2.2 ATIL):

| Silah | Görünüm | VFX |
|---|---|---|
| Yay | ip atıp Levi gibi süzülme | Ok + ip şeridi (tasma şeridi, §5.3), varışta kopar |
| Çekiç | fırlatıp Thor gibi peşinden uçma | Çekiç başında kor iz. Varışta yer çatlağı (teslim decal'i, hasar yok) |
| Kalkan | ≤ 3 m omuz hücumu | Kalkan önünde şok yayı (taşıyıcıyla aynı) |
| Kılıç | şimşek atılışı (Zenitsu) | Kırık şimşek izi (§3.1) |
| Asa | ışın yolunda süzülme | Ayak altında uzanan rün ışını |
| Küre | patlama itişiyle fırlama | Kalkışta kor patlaması. (Küre'nin yürüme yavaşlaması ATIL'a değil teslimin 0,4 s toparlanmasına aittir, silahlar.md 22:18: o sürede avuçta kor `_Intensity` 1 → 0,5 söner; ek süre yok) |
| Tılsım | mühür 0,5 s sonra çeker | Varış noktasında mühür decal'i, 0,5 s sayaç, sonra çekiş şeridi |
| Top | gülle gibi fırlatılma | Ayak dibinde patlama + duman izi |
| Kitap | sayfalara dağılıp akış | Gövde dissolve (`_Dissolve`, §4) + sayfa parçacıkları ≤ 30. Beden gerçekten yol alır |
| Yumruk | kısa atılış | Ayak altı kıvılcım (Hareket motifi) |

**Çağırma** yaratığı = silahın tarzı (Yumruk gölge dövüşçü, Çekiç taş dev, Yay şahin…) × elementin dokusu. Tek paylaşılan "kordan beden" shader'ı: silüet mesh'i + çatlak maskesi + dissolve. Yaratık başına ayrı model yok, silah başına tek low-poly silüet (10 mesh, ≤ 1,5k tris).

**Tasma görünüşü** silahtan gelir ama tek mesh'tir (§5.3): ip (Yay), zincir (Top), harf zinciri (Kitap), mühür bağı (Tılsım), rün ışını (Asa), yer çatlağı (Çekiç; zeminde), kor halka bağı (Küre), kor iplik (Kılıç, Yumruk, Kalkan). Fark yalnız şeridin dokusundaki atlas satırıdır.

#### 3.3 Büyü fiilleri: farkı efekt taşır (K 20:39–20:40)
Büyü fiillerinde (Zaman, Kontrol, Şifa, Arındırma, Güçlendirme, Zayıflatma, Yansıma, Çağırma) animasyon fiil başına tek basit klip, silahtan bağımsız (`animasyon-kurallari` §2.6). Bu yüzden okunurluk bütünüyle VFX'tedir:
- **Fiil farkı:** §1 renk ailesi + motif (değişmedi). Sekiz büyü motifi zaten birbirinden ayrı.
- **Silah farkı:** §3 taşıyıcısı aynen biner (Yay'da kor ok, Kitap'ta harf demeti, Top'ta gülle, Tılsım'da mühür…). Tasma görünüşü de silahtan (§3.2 sonu).
- **Doğuş noktası:**
  - **Kural (K 20:58): efekt jesti yapan elden çıkar** (`animasyon-kurallari` §2.6 büyü eli tablosu).
  - Jesti silah eli yapanlar → silahın `Vfx_*` noktası: Kılıç `Vfx_Tip` (bıçak ucu), Küre `Vfx_Core`, Tılsım `Vfx_Core` (mühür), Asa `Vfx_Tip` (kristal), Çekiç `Vfx_Tip` (çekiç başı), Kalkan `Vfx_Core` (kalkan yüzü), Kitap `Vfx_Core` (açık sayfa).
  - Jesti boş el yapanlar → **`Vfx_Buyu`** = boş elin avuç ortası (Humanoid `GetBoneTransform`, ek kemik yok): Yay (sağ el), Yumruk (sol el). Yay'da ok avuçtan çıkar [yorum: büyü klibinde bırakma karesi yok].
  - **Top her zaman `Vfx_Muzzle`** (namlu): jest boş (sol) elde, top sağ elde göğüs önünde namlu ileri; mermi namludan çıkar.
  - An: büyü klibinin `Impact` event'i. Yoğun şarjı (Kontrol, Zayıflatma) `Charge` karesinde, çöküş noktası (§2) doğuş noktasında.
- **Silah yankısı (yeni, ucuz):** `Impact` karesinde elde tutulan silahın koru 0,2 s parlar (`_Intensity` 1 → 1,6, §4). Animasyonda görünmeyen silah tarzını tek parametreyle hatırlatır. Parça yok, yalnız shader değeri.
- **Zaman:** hayalet iz (§1) değişmedi. Animasyondaki eski "ters oynatma" kalktı; geri akış yalnız VFX'te.
- **Yansıma:** eski karşı vuruş klibi kalktı; geri dönen etkide ayna parıltısı + kırılıp dönen kıvılcım yeterli (S-V4 aynen Sonra'da).
- **Sıfat şekli büyüde yalnız VFX'tir** (YERE/DÖNÜŞ/UZANMA klibi oynamaz): Sabit yapısı, Yayılan halkası, Çizgisel hattı, Yörüngeli parçaları §2 primitifleriyle aynen çizilir.

#### 3.4 Ejderha silüeti anları (K 20:42; fiziksel imza hareketlerinde)
Kullanıcının örneği: Çekiç tepeden inerken, Endgame'deki Thor sahnesindeki yıldırım yerine **tepeden inen kor bir ejderha silüeti**. Her silahın 1–2 fiziksel imza hareketinde böyle kısa bir an var. **Mekanik değişmez:** silüet hasar, alan, süre taşımaz; çarpışması yok; yalnız gözle okunan tarz.

**Teknik (hepsi aynı, ucuz):**
- **Tek mesh kartı:** quad (kavisli olanlar 8 üçgenlik şerit), `Ejder_Silüet` Shader Graph: atlas alfası × `_CoreColor` × `_Intensity`, kenarı fiil renginde %30. Additive, ZWrite kapalı, ışık yok.
- **Belirme ve sönme dissolve ile:** paylaşılan 128² gürültü (§4 ile aynı doku). 0,05 s'de kenardan yanarak belirir, ömrünün son %40'ında dissolve ile söner.
- **Ömür 0,2–0,45 s.** Parçacık yok; en çok 8 kıvılcım isteğe bağlı (Çekiç inişi).
- **Silüet atlası:** 1024×512, 8 hücre (256²). Hücreler: (A) inen tam ejderha, kanatlar açık, baş aşağı; (B) pençe; (C) tek kanat; (D) çift kanat; (E) kükreyen baş profili; (F) kuyruk yayı; (G) dikey göz bebeği; (H) boş. ASTC 6×6 ≈ 230 KB [hesap].
- **Yönelim:** dikey anlar Y ekseninde kilitli billboard; yatay anlar (Kılıç kuyruk yayı) kesiş düzlemine yatırılmış kart.
- **Sınırlar:** her oyuncuda aynı anda en çok 1, ekranda en çok 2 silüet (fazlası atlanır, en eskisi önce söner). Kart ekranın en çok ≈ %25'ini kaplar.
- **Uzak oyuncuda kapalı:** yerel oyuncuda her zaman; diğer oyuncularda kameraya ≤ 12 m [tahmin] ve orta/yüksek kalite segmentinde. Düşük segmentte yalnız yerel oyuncu.
- **Yoğun'da** kart ×1,25 büyür ve `_Intensity` 3,0 olur. Başka sıfat silüeti değiştirmez.
- **Doğduğu an:** çoğunda klibin `impact` event'i (ek event yok). Yalnız Çekiç inişi ayrı `Ejder` event'i (Blender marker `ejder`) ister. Liste ve kareler: `animasyon-kurallari` §2.2.3.

| Silah | An (klip) | Silüet | Ömür | Doğuş |
|---|---|---|---|---|
| **Çekiç** | ① `Cekic_VUR` ★ kaldır → zınk | (A) inen tam ejderha: çekicin üstünde belirir, baş aşağı dalarak çekiçle iner; çarpınca yere gömülür, yerde **pul desenli çatlak** açılır (Çekiç taşıyıcısının dokusu) | 0,45 s | `Ejder` (kaldırmanın tepesi) → `impact`'te söner |
| | ② `Cekic_ATIL` Thor uçuşu, varış çakması | (A) küçük ölçekte, inişte | 0,3 s | varış çakmasının `impact`'i |
| **Kılıç** | ① `Kilic_ATIL` ★ Zenitsu şimşek atılışı (K 21:05: VUR'dan taşındı) | (F) kuyruk yayı atılış yolu boyunca, şimşek iziyle aynı hatta savrulur | 0,25 s | ATIL state girişi (kod) |
| | ② `Donus_Kilic` Tanjiro dönüşü | Ayrı kart yok: akan izin dokusunda pul deseni ve kuyruk ucu (§3.1 dokusuna bir atlas satırı) | iz ömrü | `trail_on` |
| **Kalkan** | ① `Kalkan_ATIL` ★ omuz hücumu (K 21:05: VUR'dan taşındı) | (E) kükreyen baş, hücumun varışında kalkanın önünden öne fırlar | 0,25 s | son ATIL parçasının varışı (kod) |
| | ② `Kalkan_DURUS` ★ girişi | Kart yok: kalkan yüzündeki **pul deseni** korla parlar (§4 skill uyanışı, Kalkan maskesi) | duruş girişi 0,2 s | state girişi (kod) |
| **Yumruk** | `Yumruk_VUR` ★ serinin **son** vuruşu (aparkat) | (B) dev pençe, yumruğun ardında yukarı tırmalar | 0,2 s | son `impact` |
| **Yay** | ① `Yay_VUR` ★ bırakma | (D) çift kanat kirişten açılıp kapanır, ok kanatların arasından çıkar | 0,25 s | `impact` (bırakma) |
| | ② `Yay_ATIL` ★ Levi süzülmesi | (D) sırtta açık kanat, yalnız ATIL başında bir kez | 0,3 s | state girişi (kod) |
| **Asa** | `Asa_VUR` ★ kor ışını | (E) kristalin önünde baş profili; ışın ağzından çıkıyor gibi | 0,3 s | `impact` |
| **Küre** | ① `Kure_AGIR` ★ küreyi ileri itme | (G) kürenin önünde dikey göz bebeği açılıp kapanır | 0,25 s | `impact` |
| | ② `Kure_ATIL` ★ patlama itişi | (C) patlamada arkada tek kanat çırpışı | 0,2 s | state girişi (kod) |
| **Tılsım** | `Tilsim_VUR` ★ mühür patlaması | (B) pençe mühürden yukarı fırlar | 0,25 s | **gameplay olayı** "mühür patladı" (0,5 s gecikmiş etki; klip karesi değil, S-E13) |
| **Top** | `Top_VUR` ★ ateş | (E) kükreyen baş namlu ağzında, gülle ağzından çıkar | 0,2 s | `impact` (ateş) |
| **Kitap** | `Kitap_AGIR` ★ kitabı sertçe kapatma | (D) kapaklar kanat gibi kapanır, kitabın üstünde çift kanat silüeti kapanır | 0,25 s | `impact` (kapanma) |

- **Yalnız fiziksel fiillerin klipleri** (VUR, AĞIR, ATIL, DURUŞ, DÖNÜŞ). Büyü klipleri silüet oynatmaz (K 20:40: büyüde fark motifte).
- Aynı klip başka bir fiziksel kombo için oynarsa (ör. `Cekic_VUR` her Zarar skillinde) silüet de oynar. Tutarlı ve istisnasız: an klibe bağlıdır.

**Maliyet tahmini [tahmin, cihazda ölçülmedi]:**
| Kalem | Değer |
|---|---|
| Bellek | Atlas ≈ 230 KB + shader varyantı 1 + havuzda 2 kart/oyuncu |
| CPU | Havuzdan al, 3 float güncelle: ihmal edilir (< 0,02 ms) |
| Draw call | Etkin silüet başına 1 (en çok 2) |
| GPU (asıl maliyet: dolum hızı) | Ekranın ≈ %25'ini kaplayan additive kart, orta segment Android'de ≈ 0,1–0,3 ms, yalnız 0,2–0,45 s. Düşük segmentte yalnız yerel oyuncu |
| İş | Shader 0,5 · atlas çizimi (7 silüet) 1,5 · spawner + event/kod bağlama 0,5 · 10 silah ayarı ve cihaz testi 1 → **≈ 3,5 gün** |

---

### 4. Kor shader: `_Intensity` kuralın sayacı
Shader Graph `Karakter_Kor` ve silah varyantı `Silah_Kor` (değerler `karakter-boru-hatti` §5.2 ve `vt-v2/LavaEmissive.shader` ile aynı):
- **Emission:** `B kanalı × _CoreColor (4,2, 1,05, 0,18) × _Intensity × nabız`
- **Nabız:** `_PulseAmount 0,25`, `_PulseSpeed 0,6`, `_PulseScale 0,35`
- **Diğer:** `_FogStrength`, `_FlowOffset`, fresnel kenar

`_Intensity` olaylara şöyle bağlanır. Sürücü tek bir `KorSurucu` bileşeni; değerleri spec'ten ve animation event'ten alır.

| Olay | Kim | `_Intensity` eğrisi | Kural kaynağı |
|---|---|---|---|
| Boşta | herkes | 1,0 (nabız açık) | — |
| Yoğun şarjı | kullanıcının silahı + kolu | 1 → 2,5 doğrusal, `sarj_s` boyunca; teslimde 3,0 flaş, 0,2 s'de 1'e | K 19:41 |
| **Skill uyanışı** (K 20:42; her skill, fiil fark etmez) | silahın **çatlakları ve rün yuvaları** (B kanalı maskesi) | Ön süre başında 1 → 1,6 (0,1 s), teslime kadar tutulur; toparlanma sonunda 0,25 s'de 1'e. Yoğun ve silah şarjı bunun üstüne biner | `kural-kitabi` §8.4 lore 3: "içindeki ejderha uyanıyor" |
| Silahın kendi ön süresi (Yay otomatik germe 0,35 s, Çekiç kaldır-bekle, Asa akıtma; Kılıç'ta şarj yok, 22:18) | silah | 1 → 1,8 | `silahlar.md` ritim |
| Teslim (`Kor_Flash` event'i) | silah | 2,0 flaş, 0,15 s | — |
| Fedakarlık bedeli | **karakterin gövdesi** | 1 → 3 flaş + `_PulseSpeed` 0,6 → 2,0, 0,5 s | %30 can bedeli |
| Güçlendirme alan dost | **dostun silahı** | +0,6 sabit, süre boyunca; bitince 0,3 s'de söner | 6.x "dostun silahı parlar" |
| Zayıflatma alan düşman | düşman malzemesi (kor varsa) | ×0,5 + desatürasyon | — |
| Silah değişimi | eski ve yeni silah | `_Dissolve` 0→1 + kenar 1→3; yeni silah tersine; süre = `AnimationSec`'in yarıları | 2. tur kararı (kın yok) |
| Çizgisel-Hareket (ışınlanma) | karakterin gövdesi | `_Dissolve` 0→1 (0,1 s) → varışta 1→0 (0,12 s) | 15:55 |
| Can düşük (<%30) | karakter | `_PulseSpeed` ×2 (kalp atışı) | — |

- **Silaha özel lore dokusu (K 20:42, maliyeti yalnız B kanalı maskesi):** Çekiç çakınca yerde **pul biçiminde** çatlak (taşıyıcı decal'i); Kılıç kesişinden kor iz (§3.1); Kitap sayfaları **ejderha derisi** dokusu; Kalkan yüzünde **pul deseni** (skill uyanışında parlar). Doku işi `karakter-boru-hatti` silah maskeleri.
- **Malzeme örnekleri:** oyuncu başına malzeme instance'ı kullanılır, `MaterialPropertyBlock` değil (SRP Batcher uyumu).
- **Dissolve:** `_DISSOLVE_ON` keyword'ü yalnız dissolve sırasında açılır (alpha clip maliyeti yalnız o an). Paylaşılan 128² gürültü dokusu.

---

#### 4.1 Vuruş ağırlığı hasar çarpanına bağlanabilir (öneri, K 22:15)
`silahlar.md`: taban hasar ritimle ölçeklenir (çarpan = toplam süre / 0,55; Çekiç 1,91, Kalkan/Top 1,36, Kitap 1,27, Yumruk/Yay 1,00, Asa 0,91, Tılsım 0,82, Kılıç/Küre 0,73). Animasyona etkisi yok. Efekt motorunda **vuruş ağırlığı** bu çarpandan türetilebilir:

| Ne | Formül (öneri) [test] | Çekiç 1,91 | Kılıç 0,73 |
|---|---|---|---|
| Hitstop (vuran + vurulan klip) | 0,04 + 0,02 × (çarpan − 0,73) / 0,59, sınır 0,04–0,08 s (`animasyon-kurallari` §0 aralığı) | ≈ 0,08 s | 0,04 s |
| Ekran sarsıntısı (yalnız yerel oyuncu) | genlik = 0,05 × çarpan (birim: kamera m), süre 0,1 s | 0,095 | 0,037 |
| Teslim efekti boyu (`Kor_Flash`, değme flaşı) | ölçek = 0,85 + 0,15 × çarpan | ×1,14 | ×0,96 |

- Çarpan JSON'dan okunur (`silahlar` ritim tablosu); efekt kodu sabit tutmaz.
- Yumruk serisi tek darbedir: hitstop ve sarsıntı yalnız **son** parçada.
- Sıfat ağırlığı (Yoğun tek büyük flaş) bunun üstüne biner, çarpanı değiştirmez.
- Erişilebilirlik: ekran sarsıntısı ayardan kapatılabilir olmalı [öneri].

### 5. Mermi, alan, tasma görselleri

#### 5.1 Mermi (varlık, K 16:52)
- **Gövde:** mesh ya da billboard + iz (Trail Renderer değil, sabit uzunlukta şerit mesh; daha ucuz ve havuzlanır). Gövdede çekirdek parlaklığı, izde fiil rengi.
- **Kural görünür olmalı:**
  - Mermi duvara ve yörünge parçasına çarpar (K8).
  - Çarpınca **kıvılcım + duvarda iz** bırakır.
  - Yansıma'da geri dönen mermi **rengini yansıtanın rengine çevirir**: kemik beyazı kenar.
- **Güdümlü mermi:** gövde aynı, arkasında kilit izi (§2-6) ve kıvrılan iz.
- **Havuz:** tür başına en çok 24 örnek. Varlık çarpışması gameplay'den gelir, VFX yalnız "çarptı" olayını dinler.

#### 5.2 Alan (varlık değil)
- **Zemin decal'i:** düz quad + SDF shader, arena düz olduğu için projector gerekmez.
- **İki aşama:** önizleme (dolmayan kenar çizgisi) → etki (dolum + kenar flaşı).
- **Kural görünür olmalı:**
  - Alan duvardan **geçer**; decal duvarın altında da çizilir.
  - Duvar alanı tutmaz.
  - Yörünge parçası alanı tutmaz; parça alan içinde parlamaz.
- **Kaçılabilir alanlar** (Tılsım 0,5 s, Top düşüşü) için önizleme halkası **zorunlu**. Dolma süresi = gerçek gecikme. Telegraph kuralı: "ne kadar dolduysa o kadar az vaktin kaldı."

#### 5.3 Tasma (Kontrol, K §1.7)
- **Mesh:** tek şerit, 12 segment, kullanıcının elinden (`Socket_R` ya da silah soketi) hedefin gövde merkezine.
- **Sarkma:** shader'da yaklaşık zincir eğrisi.
  - Gevşekken sarkık.
  - Uzunluk sınıra varınca **gergin** (düz çizgi, parlaklık ×1,5, titreşim).
  - Gerginlik, kuralın "hedef bağdan öteye gidemez" anının görüntüsüdür.
- **Süre:** 1,5 s [test]. Bitimde ortadan kopar, iki uç dissolve olur.
- **Uç halkaları:**
  - Hedef ucu: fiil (Kontrol) rengi.
  - Kullanıcı ucu: **oyuncu rengi** (iki oyuncu aynı düşmana tasma atarsa ikisi de okunur; A-21 açık).
- **Doku:** silah atlas satırı (§3). Çekiç'te şerit zeminde decal olarak çizilir (yer çatlağı).
- **İşaretli zinciri** (K 17:33) aynı şerit mesh'tir. Fark: oyuncu renginde, gevşek, işaretler arası, 4 s.

#### 5.4 Yapı (Sabit) ve yörünge parçası
- İkisi de varlık. Parlamaları yalnız **mermi** tuttuklarında olur, alan geçerken olmaz (K8).
- **Ölçüler kenardan (K 22:15):** menzil gövde kenarından ölçülür. Kapsül yarıçapı (ayarlanabilir başlangıç): oyuncu 0,35 m, yaratık 0,5 m, yapı 1 m, boss 2,5 m. Alan primitifleri (Yayılan halkası, koni, mühür) **görselde kaynağın yarıçapı kadar büyütülür**: oyuncunun 4 m Yayılan halkası ekranda 4,35 m çizilir, değme kenarla tutarlı görünür. Yörünge yarıçapı = taşıyan + 2 m. Hedefteki "değdi" flaşı hedefin kenarında, kapsül yarıçapına göre ölçeklenir (boss'ta büyük).

#### 5.5 K 22:15 efekt notları
- **Sönüş (öneri, boşa giden skill):** Ortak yükü kullanılmadan ya da Tetikli süresi tetiksiz bittiğinde etki yok. Öneri: halka / kapan işareti 0,2 s'de içe çöküp kül gibi dağılır, `_Intensity` 1 → 0; ses yok ya da çok kısık. Maliyet ≈ 0,25 gün (mevcut halka shader'ına dissolve parametresi). İstenmezse halka sessizce kaybolur.
- **Koruma ve alan tikleri:** bir vuruş bir darbedir; Yumruk'un 3 parçalı serisi Koruma'ya **tek darbe** sayılır, tek parlama. Alan tiklerini ise Koruma her tikte ayrı darbe sayar: **her tikte küçük Koruma parlaması** (kubbe / kalkan yüzünde 0,1 s, normal parlamanın ≈ %40 boyu, aynı fiil rengi). Havuz: tik parlaması kubbe üstünde tek mesh'in `_Flash` değeri, yeni parçacık yok.
- **Kenar freni:** ATIL bir canlının ya da duvarın kenarında durunca (Yoğun-Hareket dahil) temas noktasında kısa **kor kıvılcımı** (8–12 parçacık, 0,2 s, Hareket rengi) + İniş klibi. ATIL fren klibi yok (MVP 96).
- **Güdümlü bitişi:** takip çizgisi geri çekilir, nişan kapanır (§2 satır 6); yakın silahta arkadaki hız izi 0,15 s'de söner.
- **Yansıma:** taban oran 0,85; boss'un tek vuruşunda geri dönen hasar en çok boss canının %5'i. Efekt notu: tavan devreye girdiğinde geri dönen kıvılcım **boyu tavana göre kısılır** (tam oranda değil); oyuncu "neden az döndü" diye şaşırmasın diye ayna parıltısı yine tam oynar. Ayrı efekt gerekmez.
- **Yay:** germe otomatik 0,35 s, erken bırakma yok. Germe süresince kirişte kor dolar (`_Intensity` 1 → 1,8), tam dolunca bırakma; "erken bırak" göstergesi gerekmez.

---

### 6. Çözücü: `VfxPlan = Coz(SkillSpec)`
```text
fonksiyon CozVfx(spec) -> VfxPlan:
  renk   = AILE[ROL[spec.fiil]].ton[spec.fiil];  motif = MOTIF[spec.fiil]
  kenar  = TARAF_KENARI[spec.taraf]                      # keskin / yumuşak / yapışık
  sekil  = PRIMITIF[spec.sifat]                           # §2 tablosu
  tasiyici = TASIYICI[spec.silah]                         # §3 tablosu
  eger spec.fiil in BUYU:                                  # §3.3, K 20:58
      tasiyici.soket = (spec.silah == Top) ? Vfx_Muzzle
                     : (spec.silah in {Yay, Yumruk}) ? Vfx_Buyu(jest_eli(spec.silah))
                     : silahin_vfx_noktasi(spec.silah)
  eger spec.fiil in BUYU:  plan_sonu += SilahYankisi(spec.silah, 0.2s, 1.6)  # Impact karesinde
  sahip  = OYUNCU_RENGI[spec.oyuncu]                      # yalnız işaret/zincir/tasma ucu/yük
  ek     = ELEMENT_KIVILCIM[spec.oyuncu.element]          # yalnız kenar, %15 yoğunluk

  plan = []
  eger spec.sifat == Ortak:  plan += YukHalkasi(dost, sahip, motif); dondur plan   # biniş dostun skillinde
  eger spec.sifat == Tetikli: plan += KapanIsareti(hedef, 5s, sahip); plan.tetikte(Patlama(motif,renk)); plan.suresi_dolunca(Sonus()); dondur plan   # boşa gider, K 22:15
  eger spec.sifat == Fedakarlık: plan += BedelAkisi(kullanici)
  eger spec.sarj_s > 0:  plan += Cokus(soket, spec.sarj_s); Kor(_Intensity, 1→2.5)

  eger spec.kendini_tasir:
      plan += Iz(kullanici, renk) ; eger spec.isinlanma: plan += Dissolve(cik, gir)
  yoksa spec.teslim == MERMI:
      plan += Mermi(tasiyici, renk, kilit=spec.kilit)
  yoksa spec.teslim == ALAN:
      plan += Onizleme(sekil, spec.gecikme) ; plan += Alan(sekil, renk)
  yoksa:  # yakın
      plan += Darbe(tasiyici, soket, renk)

  plan += Sekil(sekil, renk, kenar, olculer=spec)          # halka, hat, sekme, yörünge, yapı, zincir...
  eger spec.fiil == Kontrol:  plan += Tasma(kullanici, hedef, tasiyici.tasma_satiri, sahip, 1.5s)
  plan += DegmeFlasi(her_hedef, renk)
  plan += ElementKivilcim(ek)
  dondur plan
```
Kural: VFX **olayları dinler** (spec, animation event, gameplay "değdi/çarptı/bitti"), hiçbir sonucu kendi hesaplamaz. Event kaçarsa yalnız görüntü eksik kalır.

#### 6.1 Örnekler
| Kombo | Görüntü |
|---|---|
| Kalkan · Zarar · Yoğun | Kalkan kolunda kor çöker (0,35 s, `_Intensity` 2,5; kalkan tepede) → kenarın indiği yerde şok yayı, kor kırmızı, keskin kenar → hedefte tek büyük flaş |
| Asa · Kontrol · Çizgisel | Kemik-altın 1 m hat (0,2 s), hattaki en çok 5 düşmanda kor halka + her birine asadan rün ışını tasması, uçlarda oyuncu rengi |
| Yay · Şifa · Sıçrayan | `Buyu_Sifa` aynalı (sağ el sunar), avuçtan (`Vfx_Buyu`) kor rünlü ok (amber, yumuşak kenar) dosta saplanır, yay 0,2 s parlar → 2 sekme yayı ile sonraki dostlara, her birinde yükselen damla |
| Çekiç · Koruma · Yayılan | Oyuncunun etrafında kemik-altın pul kubbe (her yönden %50) + çekicin dönüş izi |
| Tılsım · Zarar · Sabit | Yerde mühür → kor kırmızı dikenli direk yükselir, 3 m halka, saniyede bir halka nabzı |
| Top · Zaman · Ortak | Dostun bileğinde oyuncu renginde yük halkası (5 s; kullanılmazsa söner, etki yok). Dost Yayılan skilli atınca solgun yeşil-altın halka onun halkasının üstüne biner; dokunulan dostların son 3 s yolu hayalet izle geri akar |
| Kitap · Zarar · Güdümlü | Harf demeti mermisi, arkasında kilit izi, hedefte döner nişan; duvara çarparsa harfler dağılır |
| Küre · Güçlendirme · Yörüngeli | Etrafında 3 küçük kor küre döner; değdiği dostun silahı +0,6 tutuşur |

---

### 7. Parametre tablosu (veri: `vfx-kurallari.json` önerisi)
| Parametre | Değer | Kaynak / durum |
|---|---|---|
| `halka.yaricap` | min(4 m, silah menzili) (Yayılan) | Kitap §3, K 21:10; Yumruk ve Kalkan'da 3 m |
| `halka.acilis_sn` | 0,25 | VFX önerisi |
| `hat.genislik` / `hat.uzunluk` | 1 m / silah menzili | Kitap §3 Çizgisel |
| `hat.omur_sn` | 0,2 (anlık); 2 (ayna); 3 (yön duruşu) | kural-ekleri tablo 10 |
| `sekme.mesafe` / `sayi` | 6 m / 3 | Kitap §3 |
| `yorunge.yaricap` / `parca` / `sure` | 2 m / 3 / 3 s | Kitap §3 |
| `kilit.zorunlu_sn` | 3 | K 17:42 |
| `kilit.vurus_arasi_sn` | 0,3 | K 17:41 |
| `kapan.sure_sn` | 5 | Tetikli |
| `yuk.sure_sn` | 5 | Ortak |
| `isaret.omur_sn` / `kisi_basi` | 20 / 4 | Kitap 9-9 |
| `zincir.omur_sn` | 4 | kural-ekleri tablo 9 |
| `tasma.sure_sn` | 1,5 [test] | K §1.7 |
| `tasma.segment` | 12 | VFX önerisi |
| `yapi.omur_sn` | 6 [test] | Kitap Sabit |
| `yapi.is_alani` | 3 m | Kitap Sabit |
| `tilsim.gecikme_sn` | 0,5 | K 18:39 |
| `sarj.yogun_sn` | 0,35 | K 19:41 |
| `pencere.yogun_sn` | 0,45 | K 16:49 |
| `kor.intensity.sarj_tepe` | 2,5 | öneri |
| `kor.intensity.flas` | 2,0–3,0 | öneri |
| `kor.guclendirme_ek` | +0,6 | öneri |
| `degme.flas_sn` | 0,15 | öneri |
| `hitstop_sn` (yalnız görüntü) | 0,04–0,08 | K §1.14 |
| `zaman.hayalet_iz_sn` | 3 (kayıt anı) | Kitap Zaman (3 s önce) |
| `element.kivilcim_yogunluk` | %15 | öneri |
| `parcacik.oyuncu_basi_max` | 150 | bütçe §8.3 |
| `parcacik.sahne_max` | 800 | bütçe §8.3 |
| `uzak_oyuncu.lod` | parçacık ×0,5, iz kısalır | bütçe |
| `kilic.iz.omur_sn` | VUR 0,18 · ATIL 0,25 · DÖNÜŞ 0,35 · UZANMA 0,15 | §3.1 [test] |
| `kilic.iz.ornek` / `ara_nokta` | 16–24 / 2× (uzak: 8 / 0) | §3.1 [tahmin] |
| `kilic.iz.akis_hiz` | 2 tur/s (`_FlowOffset`) | §3.1 [tahmin] |
| `ejder.uyanis` | `_Intensity` 1 → 1,6, giriş 0,1 s, çıkış 0,25 s | §4 [tahmin] |
| `ejder.siluet.omur_sn` | Çekiç 0,45 · Kılıç/Kalkan/Yay/Küre/Tılsım/Kitap 0,25 · Asa 0,3 · Yumruk/Top 0,2 | §3.4 [test] |
| `ejder.siluet.uzak_m` / `ekran_max` | 12 m / 2 | §3.4 [tahmin] |
| `ejder.harf.oturma_sn` / `parlama_sn` | 0,2 / 0,3 | §1.1 (GameDesign) |
| `buyu.silah_yanki` | `_Intensity` 1 → 1,6, 0,2 s, `Impact` karesinde | §3.3 [tahmin] |

---

### 8. Unity: VFX Graph mı, Particle System mı?

#### 8.1 Doğrulanmış kısıtlar (Unity resmi belgeleri)
Kaynak: https://docs.unity3d.com/Packages/com.unity.visualeffectgraph@17.6/manual/System-Requirements.html
- VFX Graph **compute shader** destekli (`SystemInfo.supportsComputeShaders == true`) ve **SSBO** destekli (`SystemInfo.maxComputeBufferInputsVertex > 0`) donanım ister.
- VFX Graph **OpenGL ES'i desteklemez.**
- VFX Graph **URP'de önizlemeden çıkmadı**; URP'nin desteklediği platformların **yalnız bir kısmını** destekler.
- URP'de **gamma renk uzayı desteklenmez**.

Kaynak: https://discussions.unity.com/t/getting-started-with-the-vfx-graph-in-unity-6/1617550/1 (Unity teknik makalesi)
- Android'de **yalnız compute yeteneği olan üst segment cihazların bir alt kümesi** (yalnız URP ile) desteklenir.

Kaynak: https://docs.unity3d.com/6000.6/Documentation/Manual/android-requirements-and-compatibility.html
- Unity 6 Android'de Vulkan ve OpenGL ES 3.1/3.2'yi destekler. Yeni projede Vulkan varsayılandır.
- Bu, Vulkan'sız ya da Vulkan'ı sorunlu cihazların GLES'e düşeceği anlamına gelir. Orada VFX Graph hiç çalışmaz.

#### 8.2 Karşılaştırma
| | Particle System (Shuriken) | VFX Graph |
|---|---|---|
| Cihaz kapsamı | Her yerde (CPU) | Compute + SSBO + Vulkan/Metal; GLES yok |
| Gameplay olayına bağlama | Kolay (C#, `Emit`, çarpışma callback'i) | GPU'da; CPU'ya geri okuma yok, olay bağlamak zor |
| Az sayıda parçacıklı çok efekt (bizim durum) | Uygun | Her sistemin GPU sabit maliyeti var, küçük efektte verimsiz |
| Binlerce parçacık | Pahalı (CPU) | Güçlü |
| Havuzlama / determinizm | Kolay | Zor |

#### 8.3 Öneri
1. **Ana yol:** Particle System + Shader Graph'lı mesh primitifleri.
   - Halka, hat, tasma, yapı, işaret, kilit izi, yük halkası = **mesh + shader** (parametreli, havuzlu, SRP Batcher uyumlu).
   - Parçacık yalnız kıvılcım, toz, kırıntı, damla için (efekt başına ≤ 30).
2. **VFX Graph kullanılmaz.** Ancak boss sahnesinde "yalnız üst segment" bir süs katmanı istenirse Sonra listesinde (S-V1). O durumda `SystemInfo` kontrolüyle Particle System yedeğine düşer.
3. **Bütçe** [tahmin, cihazda ölçülecek]:
   - Sahne başına ≤ 800 canlı parçacık, oyuncu başına ≤ 150.
   - Saydam overdraw: ekranın en çok %30'u 2 katmandan fazla.
   - VFX CPU ≤ 1,0 ms, GPU ≤ 2,0 ms (orta segment Android).
   - Uzak oyuncuların efektleri ×0,5 parçacık, iz kısa.
   - Ekran dışı efekt simülasyonu durur (Culling Mode: Pause and Catch-up yerine **Pause**; görüntü sonucu taşımadığı için güvenli).
4. **Additive/alpha-blend shader'lar** tek bir "VFX_Kor" Shader Graph ailesinde, 2 varyant: additive, alpha. Keyword sayısı ≤ 4 (shader varyant patlamasına karşı).
5. **HDR/bloom:**
   - Bugün bloom açık (eşik 0,95, çeyrek çözünürlük). Renk tablosu buna göre.
   - `oynanis-analizi-2` mobilde HDR'ı kapatmayı öneriyor; kapanırsa bloom çalışmaz.
   - O durumda kor parlaması için yedek: **sahte hale kartı** (billboard glow sprite) her primitifte açılır (+1 çizim/efekt) → S-E4.

---

### 9. İş ve süre [tahmin]
| İş | Gün |
|---|---|
| Primitif shader'ları: halka, hat, şerit (tasma/zincir/iz), yapı dissolve, işaret, kapan sayacı, kilit izi | 5 |
| `VfxPlan` çözücüsü + olay dinleyicileri + havuz | 2,5 |
| 12 fiil motifi (renk ve motif, primitife takılabilir) | 5 |
| 10 silah taşıyıcısı (mermi/darbe/ışın/mühür/çatlak) | 5 |
| Çağırma kordan beden shader'ı + 10 silüet | 2 |
| `KorSurucu` (`_Intensity` olay tablosu §4) | 1 |
| Mesh trail bileşeni (Kılıç akan izi, §3.1; sonra Çekiç ve Asa da kullanır) | 1 |
| Büyü doğuş soketi (jest eline göre: silahın `Vfx_*`'ı / `Vfx_Buyu` / Top namlusu) + silah yankısı (§3.3) | 0,25 |
| Ejderha silüeti anları (§3.4: shader, atlas, spawner, 10 silah ayarı) | 3,5 |
| Rün → Ejderha Dili harfi (§1.1, kod; glifler hariç) | 0,5 |
| Ejderha motif dokuları (12 hücre, mevcut motif işinin içinde; yalnız yeniden çizim farkı) | 0,25 |
| Cihaz bütçe testi + LOD | 1 |
| **Toplam** | **≈ 27** |

---

### 10. Sonra listesi
- **S-V1 · Boss için VFX Graph süs katmanı** (yalnız compute destekli cihaz, Particle System yedeği). ≈ 3 gün.
- **S-V2 · Element tepkileri:** iki elementin çarpışma efekti (kitap: "Element tepkileri sonra").
- **S-V3 · Ortak'ta iki oyuncunun rengini karıştıran ortak görüntü** (ör. iki rengin örgüsü).
- **S-V4 · Yansıma'da geri dönen merminin izini ters akıtmak.**
- **S-V5 · Ekran kenarı göstergeleri:** ekran dışından gelen kilit ya da tasma için yön oku.
- **S-V6 · Zaman hayalet izinin bütün takıma görünür "kayıt" çizgisi** (sürekli açık; performans riski).
- **S-V7 · Dünya dağı (K 20:42 Sonra'ya):** uzaktaki dağın gözü ya da çatlağı büyük bir anda bir an kor yakar (GameDesign lore 6). Arka plan malzemesinde tek `_Intensity` darbesi; tetik kuralı tasarlanmadı.
- **S-V8 · Hayalet pençe (K 20:42 Sonra'ya):** "kordan yankı = ejderhanın hayaleti": yankıda yarı saydam pençe/kanat/ağız, 0,3 s (GameDesign lore 5). §3.4 atlasını kullanabilir.
- **S-V9 · Harf efektin çekirdeğinde ve fısıltı sesi** (GameDesign lore 2 ve 7): karar yok (kitap AS-13).

---

### 11. Açık sorular
| # | Soru | Çakıştığı karar | Önerim | Değişirse ne yeniden yazılır, kaç gün |
|---|---|---|---|---|
| ~~S-E1~~ | **Kapandı (K 21:05):** Asa ışını ve Küre konisi **alan**dır (Tılsım mührüyle birlikte). Mermi = Yay, Top, Kitap; yakın vuruş = Yumruk, Kılıç, Çekiç, Kalkan (§3) | — | — | — |
| **S-E2** | Tılsım "yol yok" ↔ "mermi varlıktır" (`silahlar.md` Açık A) | 16:52 ↔ 18:39 | VFX alan gibi çizer (ayakta mühür + 0,5 s önizleme), duvar tutmaz | Mermi denirse: avuçtan hedefe görünür mermi + duvar çarpması, ≈ 1 gün; GameDesign hücreleri |
| **S-E3** | Renk = **rol ailesi + fiil tonu** (3 aile). Kullanıcı "fiil rengi" dedi; 12 ayrı renk mi isteniyor? | 19:48 metni | 3 aile + motif (mobil okunurluk) | 12 renk: renk tablosu + motifler yeniden, ≈ 1 gün; okunurluk testi |
| **S-E4** | HDR kapanırsa (oynanis-analizi-2) bloom gider. Hale kartı yedeği kabul mü? | Bloom açık kararı (görsel test sahnesi) | HDR açık kalsın; düşük segmentte hale kartı | HDR kapanırsa tüm renk değerleri yeniden ayarlanır + hale kartları, ≈ 2 gün |
| **S-E5** | Oyuncu rengi (işaretler, K 17:35) fiil rengiyle karışabilir (ör. kırmızı oyuncu + Zarar). Oyuncu renkleri hangi palet? | 17:35 "işaretler oyuncu renginde" | Oyuncu renkleri kor ailesi dışında 5 ton (beyaz, mor, camgöbeği-gri, yeşil, pembe), yalnız uç ve halkada | Palet değişirse §1 tablosu, 0,5 gün |
| **S-E6** | Element kıvılcımı fiil rengini bozar mı? Element kanalı görünsün mü? | §1.13 element "tek küçük durum" | Yalnız kenarda %15 | Kaldırılırsa 0 gün; büyütülürse renk okunurluğu testi 1 gün |
| **S-E7** | Fiil paletinden mavi çıkarılsın mı? (Su elementi #47B8FF ve mana barı mavi; fiil ile element karışmasın) | Kilitli karar yok; öneri | Evet, fiillerde mavi yok; Zaman solgun yeşil-altın | Mavi serbestse Zaman/Koruma soğuk tona geçebilir, 0,25 gün |
| ~~S-E8~~ | **Kapandı (K 21:10):** halka min(4 m, menzil); Yumruk'ta 3 m | — | VFX spec'ten okur, değişiklik gerekmez | 0 gün |
| ~~S-E9~~ | **Kapandı (K 21:05):** Güçlendirme motifi = yürek koru nabzı (§1) | — | — | — |
| ~~S-E10~~ | **Kapandı (K 21:05):** Arındırma motifi = pul dökme (deri değiştirme) (§1) | — | — | — |
| ~~S-E11~~ | **Kapandı (K 21:05):** palet 3 rol ailesi olarak kalır (§1) | — | — | — |
| **S-E12** | Ejderha Dili harf glifleri (24 harf) çizildi mi, kim çizer? Rün çizim şekli harfe yakın mı olmalı? | Tasarım kaydı bulunamadı [kontrol et] | Glifler GameDesign/sanat işi; yoksa geçici olarak rün çizgisinin kendisi kalın kor harf olarak parlar | Glif işi ≈ 1–2 gün (sanat); rün tanıma değişmez |
| | *Not 21:15:* 24 harfin SVG taslağı ve $1 tanıma testi çıktı: `ejderha-glifleri/` (`glif-tablosu.png`, `tanima-raporu.md`). Taslaktır, sanat onayı açık. | | | |
| **S-E13** | Tılsım silüeti klip karesinde değil, gecikmiş patlamada (gameplay olayı) doğar. "Gecikmiş etki klip oynatmaz" kuralına uyar (VFX) ama silüet "imza hareketi" değil "imza etkisi" olur | `animasyon-kurallari` §1 | Kabul (Tılsım'ın imzası mühürdür) | Klipte istenirse: avuç sıkma `impact`'inde küçük pençe, 0 gün |
| **S-E14** | Küre'nin silüeti dikey göz bebeği: eski Kontrol motifiydi (kitap §8.4, sonra kuyruk zinciri oldu). Karışır mı? | — | Karışmaz: Kontrol artık zincir; göz yalnız Küre AĞIR'da | Değişirse Küre'ye (C) kanat, 0 gün |

---

<a id="b8"></a>
