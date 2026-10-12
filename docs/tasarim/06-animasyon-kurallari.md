# Bölüm 6: Animasyon kuralları

*Kaynak: `karakterler/v4/animasyon-kurallari.md`*

## v4 · Animasyon kuralları: silah imza verir, fiil niyeti seçer, sıfat zamanlar

Hazırlayan: Animatör (A'nın görevi), 9 Ekim 2026, 19:49 kararına göre. Güncelleme 20:39: K 20:29–20:37 (silah tarzı, Hareket görünümleri, Kılıç nefes tekniği; §0, §2.2, §2.2.1, §3 satır 9, §8.1, S-13/S-14). **Güncelleme 20:50: K 20:39–20:40, fiziksel / büyü fiili ayrımı** (§0, §1, §2.1–2.6, §3, §4, §6, §8, §9, §11 S-15…S-20, §12). Çakışan eski metin bu kararla geçersizdir. **Güncelleme 20:55: K 20:42 ejderha silüeti anları, doğduğu kareler §2.2.3** (görüntü `efekt-motoru` §3.4); klip sayısı değişmez. **Güncelleme 21:05: K 20:58 eşleşme denetimi onayı** (`eslesme-denetimi.md` Ö1–Ö5 + Top biçim klipleri): büyü eli tablosu ve göğüs önünde taşıma (§2.6, §4.2, §4.3), Asa AĞIR = ileri itme (§2.2), Top'a kendi DÖNÜŞ/UZANMA (§2.2, §8). MVP 94 → **96**. **Güncelleme 21:10: K 21:05** (AS-14 Kılıç VUR yerinde iaido; AS-15 Kalkan VUR kenarla indirme, omuz hücumu ATIL'da; AS-03 süper zırh / yakın-dodge iptali silindi; kın kesin yok; Kontrol/Yansıma/Zayıflatma büyü kesin; teslim sınıfı kesin): §0, §2.1, §2.2, §2.2.1, §2.2.3, §4.2–4.3, §6.1, §11 kapanışları. Klip sayısı değişmez (96). **Güncelleme 22:20: K 22:15** (grup kontrolü, kullanıcı onayı): Ortak'ın 5 s yedeği ve Tetikli'nin süre sonu yok, yük/kapan boşa gider (§0, §3); kenar freni = İniş + kor kıvılcımı, ATIL fren MVP'ye girmez (§5.3); Güdümlü'nün iki sigortası + çıkış geçişi (§3, §5.2); Yay germesi otomatik 0,35 s, erken bırakma yok (§2.2); Tılsım toparlanması 0,45 s; kapsül yarıçapları ve kenardan menzil (§5.4). Hasar çarpanı animasyonu değiştirmez. Klip sayısı yine 96. **Güncelleme 22:25 (silahlar.md 22:18):** Küre yavaşlaması ek süre değil, 0,4 s toparlanmada yürüme ×0,5 → `Kure_Soguma` klibi kalktı, **MVP 96 → 95**; Kılıç'ta şarj yok (ön süre = iaido bekleyişi); Çekiç kaldır-bekle-çak, tepede döndürme yalnız Koruma; itme menzille kısalmaz (bu belgede itme ölçüsü yoktu).
Kaynak: `v3-kararlar.md` (19:48 "Animasyon ve efekt sistemi"), `kural-kitabi-v4.md` (§1–7), `silahlar.md`, `silah-fiil-120.md`, `dunya-fizigi.md` §6–8, `kural-ekleri-dongu.md`, `sim-kural-ekleri.md` (G2), `animasyon-plani.md`, `karakter-boru-hatti.md`. `arsiv/` kullanılmadı. Oyun reposuna dokunulmadı.
Eş belge: `efekt-motoru.md` (VFX). Bu belge klip ve zamanlamayı, o belge görüntüyü kurala bağlar.

> **Kilit kuralı (kullanıcı):** Onaylı kurallar test edilene kadar değişmez. Yeni fikirler §10 **Sonra** listesinde. Kilitli bir kararla çakışan her öneri §11'de **açık soru** olarak yazıldı; her değişiklik önerisinde neyin yeniden yazılacağı ve kaç gün süreceği var.
> Gün tahminleri tek kişilik iş günü, **[tahmin]**: Mixamo'dan alıp hizalama ≈ 0,25 gün/klip, Blender'da düzeltme ≈ 0,5 gün/klip, Blender'da sıfırdan ≈ 1 gün/klip.

---

### 0. Kısa özet

- **Fiziksel / büyü ayrımı (K 20:39–20:40):** özel koreografi yalnız **4 fiziksel fiilde** (Zarar, Koruma, Kuvvet, Hareket) × her silah. **8 büyü fiilinin** (Zaman, Kontrol, Şifa, Arındırma, Güçlendirme, Zayıflatma, Yansıma, Çağırma) her biri **kendi basit büyü klibini** oynar; silahtan bağımsızdır, fark efekt motorundadır (§2.6).
- **Tek kural, istisna yok:** fiziksel fiilde `klip = SilahSeti[silah][ yuva( niyet(fiil), biçim(sıfat) ) ]`, büyü fiilinde `klip = Buyu[fiil]` (jesti yapan el silaha göre, §2.6 tablosu; Unity state aynalaması), zamanlama = `sıfat + silah ritmi`, süre = kural motorunun JSON'u. Elle seçilmiş hücre matrisi (`animasyon-plani` §2.3) kalkar; yerine §4'teki algoritma gelir.
- **Fiil → klip:** Zarar→VUR, Kuvvet→AĞIR, Koruma→DURUŞ (silahın savunma duruşu, §2.2.2), Hareket→ATIL; 8 büyü fiili → `Buyu_<Fiil>` (8 klip). BAĞ ve DESTEK yuvaları kalktı. **Zaman = VUR'un tersi kuralı kalktı**; Zaman'ın kendi klibi var (§2.6).
- **Sıfat → biçim:** 12 sıfatın her biri ya klip yuvasını değiştirir (fiziksel fiilde Sabit→YERE, Yayılan→DÖNÜŞ, Çizgisel→UZANMA, Yörüngeli→yarım DÖNÜŞ; her fiilde Ortak→PAS, Tetikli→İŞARET; büyü fiilinde şekil yalnız VFX), ya zamanlamayı (Yoğun şarjı, Fedakarlık bedeli), ya yönü ve yolu (Odaklı, Güdümlü, Sıçrayan, İşaretli). Tablo §3.
- **Silah başına 7 yuva (4 fiziksel + 3 biçim), 2-4'ü imza (★):** Kalkan kenarla indirme (VUR) ve omuz hücumu (ATIL), Çekiç kaldır-bekle-çak (tepede döndürme yalnız Koruma duruşu), Kılıç yerinde iaido (VUR) ve Zenitsu şimşek atılışı (ATIL), Küre patlamayla fırlama vb. Tablo §2.
- **Silah = menzil + ritim + oynanış tarzı (K 20:29):** silahın mekanik kanunu yok. Silahlar arasındaki farkın çoğu animasyondan gelir; her silahın tarzı **gözle tanınmalı**. Kılıç'ın tarzı Demon Slayer "nefes tekniği": Zenitsu şimşek atılışı + Tanjiro su nefesi dönüşü, arkasında akan kor izi (§2.2.1).
- **Hareket görünümü silahtan (K 20:35):** İşaretli-Hareket'in 10 silahtaki görünümü onaylandı. Yay ip/Levi, Çekiç Thor, Kalkan ≤ 3 m omuz hücumu, Kılıç Zenitsu, Top gülle… Bunlar silahın ATIL klibidir; klip sayısı değişmez (§2.2, §3 satır 9).
- **Hareket kombosu:** dodge = zıplama (yükseklik yalnız görüntü), havadayken tekrar = takla (ikinci kendini taşıma: hız devri kuralı K 15:53 aynen işler), Hareket skilleri birbirini keserek dash zinciri kurar. §5.
- **Animator Controller kalır** (Timeline değil). Katman 2 → **3** (ek: kısa additive katman). **Root motion kapalı kalır.** §6–7.
- **Klip sayısı (K 20:58 sonrası):** MVP **111 → 95 (−16)**, hedef **156 → 149 (−7)**; Revive yoksa 94 / 148 (22:18: Küre soğuma klibi kalktı, −1). (K 20:40'ta 94'tü; Top'a kendi DÖNÜŞ/UZANMA'sı +2.) 85'lik eski plana göre MVP +11. §8.

---

### 1. Üç katman ve sorumluluk sınırı

| Katman | Kim karar verir | Animasyonda karşılığı | Asla yapmaz |
|---|---|---|---|
| **Silah** | `silahlar.md` dövüş tarzı + ritim | Hangi **klip seti** (7 yuva: 4 fiziksel + 3 biçim) ve ritim davranışı (büyü klibinde de tutma/hizalama ritimden) (şarj tutma, seri, kanal, soğuma) | Menzil, hasar, süre belirlemez |
| **Fiil** | Kural kitabı §2 | Fiziksel fiilde **niyet yuvası** (VUR, AĞIR, DURUŞ, ATIL); büyü fiilinde **kendi büyü klibi** (§2.6) | Görüntü biçimi seçmez |
| **Sıfat** | Kural kitabı §3 (beş soru) | Fiziksel fiilde yuvayı **biçim yuvasına** çevirir (DÖNÜŞ, UZANMA, YERE); her fiilde zamanlamayı ve yönü ayarlar | Yeni klip istemez; sıfat klipleri silah setinin içindedir |
| **Kural motoru** | Skill JSON'u (`prezentasyon-katmani.json`, sim) | Süreler, vuruş anı, yol, hedef | — |
| **Efekt motoru** | `efekt-motoru.md` | Animation event'lerinde VFX doğar | Oyun sonucu üretmez |

Değişmeyen ilkeler (`animasyon-plani` §1.8'den, kilitli):
- **Zamanın tek kaynağı JSON.** Klip, vuruş anına `ActionSpeed = klipVuruşSn / jsonVuruşSn` (0,7–1,4) ile hizalanır. Animation event yalnız sunumdur.
- **Gecikmiş etki klip oynatmaz.** Bir etki oyuncunun cast anından sonra işliyorsa (Tetikli tetiği, Sabit yapının nabzı, Yörüngeli parçanın değmesi, Zaman dönüşü), oyuncunun o anki hareketine dokunulmaz; yalnız VFX oynar. Böylece oyuncunun kontrolü elinden alınmaz. **Boşa giden etki (K 22:15):** Ortak'ın yükü dost kullanmadan biterse ya da Tetikli'nin süresi tetiksiz dolarsa skill boşa gider: ne klip ne efekt oynar (yedek etki yok). Öneri: yalnız kısa bir "sönüş" (`efekt-motoru` §5.5).
- **Dünya zamanı tektir** (K4 §1.14): slow-mo yok. Hitstop yalnız vuranın ve vurulanın klibinde **0,04–0,08 s** `ActionSpeed = 0`. Gameplay saati durmaz.
- **İşaretten çıkan skill:** oyuncu hareketi **kendi yerinde** yapar, kordan yankısı işarette belirir (`silahlar.md`).

---

### 2. Silah klip setleri (7 yuva, imza ★) ve büyü klipleri (8)

#### 2.1 Yuvalar
**Fiil sınıfı (K 20:39–20:40):**
- **Fiziksel (4):** Zarar, Koruma, Kuvvet, Hareket → silaha özel koreografi.
- **Büyü (8):** Zaman, Kontrol, Şifa, Arındırma, Güçlendirme, Zayıflatma, Yansıma, Çağırma → fiil başına kendi basit büyü klibi (§2.6).
- Kural kitabıyla doğrulama: kitapta "fiziksel/büyü" diye bir sınıf yok; roller (Saldırı/Tank/Destek) bu ayrımı vermez. Tutarlı ölçüt: **etkiyi gövde ya da silah teması taşıyor mu** (vurma, itme, duruş, taşınma). Bu ölçütle liste kullanıcının listesiyle aynı. Hücrelerde gövde jesti olan üç büyü fiili (Kontrol, Yansıma, Zayıflatma) açık soru: §11 S-15…S-17.

| Yuva | Base state (kod adı) | Ne zaman | Yeni mi? |
|---|---|---|---|
| **VUR** | `BasicStrike` (hedefte B varyantı) | Zarar | eski H |
| **AĞIR** | `CastPierce` (tutma karesi + bırakma) | Kuvvet | eski Ş |
| **DURUŞ** | `CastGuard` (loop) | Koruma = silahın **savunma duruşu** (§2.2.2) | eski S |
| **ATIL** | **`Atil`** (yeni, loop) | Hareket (her yol parçası) | **yeni** |
| **YERE** | `CastSlam` | fiziksel fiilde Sabit | eski A |
| **DÖNÜŞ** | `Spin` (zaten var) | fiziksel fiilde Yayılan, Yörüngeli (ilk yarı) | **yeni klip** |
| **UZANMA** | **`Uzanma`** (yeni) | fiziksel fiilde Çizgisel | **yeni** |
| **BÜYÜ** | **`Buyu`** (yeni, Upper katman, `Mirror` parametreli) | 8 büyü fiili, her biri kendi klibi; silahtan bağımsız | **yeni** (8 klip) |
| ~~BAĞ~~ | `CastShoot` | **kalktı** (Kontrol → `Buyu_Kontrol`) | — |
| ~~DESTEK~~ | `CastChannel` | **kalktı** (Şifa, Güçlendirme, Arındırma → kendi büyü klipleri) | — |

**Teslim sınıfı (K 21:05, kesin):** yakın vuruş = Yumruk, Kılıç, Çekiç, Kalkan · mermi = Yay, Top, Kitap · alan = Asa ışını, Küre konisi, Tılsım mührü. Animasyonda tek etkisi Güdümlü'dür: yakın vuruşta oyuncu koşar (§3 satır 6), mermide ve alanda koşmaz. Görüntüsü `efekt-motoru` §3. Sınıf hiçbir silaha ayrı mekanik kural (süper zırh, kesilmezlik, dodge farkı) vermez (K 20:29, AS-03).

YERE, DÖNÜŞ, UZANMA **biçim yuvalarıdır**: MVP'de lokomosyon ailesi başına (7), hedefte silah başına (10). Silah başına zorunlu olan 4 fiziksel yuvadır.

Kalan 4 eski yuva (H/Ş/A/S) adlarıyla eşleşir; M (BAĞ) ve D (DESTEK) state'leri koddan silinmez, override'da boş kalır (geri dönüş ucuz olsun). Yeni olan ATIL, DÖNÜŞ, UZANMA, BÜYÜ'dür.

#### 2.2 Silah başına içerik
Kaynak hücreler `silah-fiil-120.md`. Yuva içeriği, o yuvaya düşen fiziksel hücrelerin çoğunluğundan seçildi. Büyü fiili hücrelerinin silaha özel jesti (ör. Kılıç Arındırma "dairesel kesiş") büyü klibine düşer, silah tarzını VFX taşır. Liste §2.3'te.

| Silah (ritim) | VUR (Zarar) | AĞIR (Kuvvet) | DURUŞ (Koruma) | ATIL (Hareket) | YERE (Sabit)¹ | DÖNÜŞ¹ | UZANMA¹ |
|---|---|---|---|---|---|---|---|
| **Yumruk** (seri) | ★ kroşe-aparkat serisi (2-3 vuruş tek klipte) | düz direkt, tam ağırlık | yüksek guard, ayak ucunda sekme (§2.2.2) | ★ kısa atılış: slip + dalış (boksör yan adımından) | yere yumruk, kor gölge fırlar | ★ dönen ters yumruk (spinning backfist) | jab-lunge: uzun direkt, öne kayarak |
| **Yay** (ger-bırak; germe otomatik 0,35 s, erken bırakma yok, K 22:15) | ★ ger, nişan, bırak | ağır ok, geri sarsılma | yay çapraz siper (§2.2.2) | ★ ip atıp süzülme (Levi): ipli ok hedefe, ipte süzülür; yolda ger-bırak serbest (Upper) | ★ göğe ok | dönerek yelpaze atış | diz çöküp düz nişan |
| **Kitap** (yürürken okuma) | satırı okuyup sayfayı savurma | ★ kitabı sertçe kapatma | açık kitap siper (§2.2.2) | ★ sayfalara dağılma: kitabı açar, beden sayfa akışına döner (dissolve VFX), varışta toplanır; beden gerçekten yol alır, ışınlanma değil | sayfayı yırtıp yere atma | dönerken sayfalar etrafa saçılır | kitabı ileri uzatıp satır ışını |
| **Kılıç** (nefes tekniği: ani, tek teslim) | ★ yerinde iaido (K 21:05 AS-14, ≈ 0 m): alçak bekleyiş, el kabzada (kınsız), tek ani yatay kesiş, bitiş pozunda donma, yavaş geri dönüş; kısa kor iz | yassı yüzle sert savuruş | ★ orta guard, ani savuşturma (§2.2.2) | ★ şimşek atılış (Zenitsu): çok alçak, kılıç geride, hızlı yolculuk (~25 m/s, K 21:11); geçtiği yolda kesik izi | kılıcı yere saplayıp diz çökme | ★ su nefesi (Tanjiro): dönerek yatay kesiş, arkasında akan kor izi | uzun hamle (thrust-lunge), bıçak boyunca iz |
| **Küre** (anında; 0,4 s toparlanmada yürüme ×0,5) | avuçta sıkma, kor fışkırır | ★ küreyi ileri itme | küre siperi, avuçla ışık itişi (§2.2.2) | ★ küreyi yere vurup patlama itişiyle fırlama, ardından 0,4 s yavaşlama (Küre soğuma) | küreyi yere bırakma | ★ kolla geniş yay çizen dönüş (kor halka) | kol tam uzanır, sıkma |
| **Çekiç** (kaldır, bekle, çak; tepede döndürme yalnız DURUŞ) | ★ kaldır → zınk çak | ★ öne geniş savuruş (hedef senden dışa, K yön kuralı) | ★ tepede döndürme loop'u (§2.2.2) | ★ Thor: çekici fırlatıp peşinden uçma (kol ileri, gövde yatay), varışta yere çakma (VUR'un teslim yarısı) | iki elle yeri dövme | dönen çekiç (1,5 tur) | önüne çakma, çatlak düz koşar |
| **Top** (balistik) | ★ nişan → ateş → geri tepme | ağır gülle: doldur + ateş | namlu çapraz siper (§2.2.2) | ★ top seni fırlatır: ayak dibine ateş, gülle gibi kıvrık ve çok hızlı uçuş | ayak dibine havan | ★ topu kalçada döndürüp çevre ateşi | diz çöküp düz ağır atış |
| **Asa** (kısa kanal) | ★ asadan kısa kor ışını | asayı iki elle ileri itme, ışın darbesi (K 20:58) | asa çapraz savuşturma (§2.2.2) | asaya tutunup ışın yolunda süzülme (en yavaş; yolda skill var) | uçla yere rün çizme | ★ asayı etrafında çevirme (twirl) | asa ileri, uzun ışın kanalı |
| **Tılsım** (anında) | ★ avucu sıkma | mühürü öne basma | mühür siperi (§2.2.2) | mühürü basar, mühür 0,5 s sonra çeker (kayma) | ★ diz çöküp yere mühür basma | ★ iki avuçla etrafa mühür saçma | kolu ileri açma, mühür hattı |
| **Kalkan** (ağır adım) | ★ ağır adım + kalkan kenarıyla indirme (K 21:05 AS-15) | ★ iki elle kalkan itişi (tam gövde) | ★ kalkan önde çökme, ağır yürüyüş (§2.2.2) | ★ omuz hücumu koşusu (≤ 3 m, kalkan önde; uzak işarete gidemez). Kalkan'ın omuz hücumu yalnız burada | kalkanı yere dikme | kalkanla geniş dairesel savuruş | uzun omuz hücumu (düz hat) |

¹ Biçim yuvası, yalnız fiziksel fiilde. MVP'de aile klibi (Ağır: YERE Çekiç+Top, DÖNÜŞ/UZANMA yalnız Çekiç; Büyücü: Asa+Küre+Tılsım), hedefte silah başına. **Top'un DÖNÜŞ ve UZANMA'sı MVP'de kendi klibi** (K 20:58: çekiç dönüşü ve önüne çakma topla absürttü, `eslesme-denetimi` §4). Eski BAĞ ve DESTEK sütunları K 20:40 ile kalktı; o jestler büyü klibine + VFX'e düşer (§2.3).

İmza sayısı (★, yeni tablo): Yumruk 3, Yay 3, Kitap 2, Kılıç 4, Küre 3, Çekiç 4, Top 3, Asa 2, Tılsım 3, Kalkan 4. Eski BAĞ/DESTEK imzaları (Asa rün çizme, Kitap destan okuma) büyü fiillerine geçtiği için düştü; o tarz artık VFX'te (`efekt-motoru` §3.3).

**ATIL = Hareket görünümü (K 20:35).** Mekanik bütün silahlarda aynı: işarete yol alarak çekilme, mesafe = silah menzili, ışınlanma yok. Görünüm silahın tarzından gelir ve tablodaki ATIL hücresidir. Yolculuk hızı ve yolda skill atılıp atılamayacağı `silahlar.md` "Yolculuk hızı" tablosundan:
- **Yolda skill VAR** (Yay, Asa, Tılsım): ATIL klibi Base katmanda alt gövdeyi taşır. Teslim Upper katmanda oynar (§5.2 "ATIL + UPPER"). Bu üç ATIL klibinin üst gövdesi nötr ve silahı kullanmaya hazır olmalı.
- **Yolda skill YOK** (Top, Çekiç, Küre, Kılıç, Yumruk, Kalkan, Kitap): ATIL tam gövdedir.
- **Varış jesti** yeni klip değildir:
  - Çekiç'in varış çakması VUR'un teslim yarısıdır.
  - Küre'nin yavaşlaması ayrı süre değildir (silahlar.md 22:18): 0,4 s toparlanma boyunca yürüme yarı hızda. Klip yok; kod lokomosyon hızını ×0,5 yapar, Base Run klibi hızla yavaşlar (Animator speed parametresi), üst gövde VUR'un toparlanma karelerini oynar.
  - Kitap'ın dağılma/toplanması dissolve VFX'idir (`efekt-motoru` §3).
- **Kalkan:** ≤ 3 m sınırı mekaniktir; klip, kısa omuz hücumu koşusu loop'udur.
- **Sıfatlarla:** İşaretli dışındaki Hareket sıfatlarında da aynı ATIL klibi oynar, yalnız yön ve yol değişir (§3). Bu, 20:35 kararının ATIL'ın tamamına genişletilmiş okumasıdır [yorum]. Kullanıcı yalnız İşaretli için demek istediyse §11 S-13.

##### 2.2.1 Kılıç tarzı: nefes tekniği, akan iz (K 20:37)
Referans Demon Slayer. Kılıç'ı diğer 6 m silahtan (Çekiç) ayıran şey ritimdir:
- Kılıç: 0,20 s ön süre + 0,20 s toparlanma.
- Çekiç: 0,55 + 0,50.

Bu ritim gözle okunur olmalı.

| Klip | Referans | Gövde | Zamanlama (30 fps) [tahmin] | İz |
|---|---|---|---|---|
| **VUR** (Zarar) | **Yerinde iaido** (K 21:05 AS-14) | Alçak bekleyiş, el kabzada, kılıç geride (kın kesin yok, K 21:05). **Yer değiştirme ≈ 0 m**: kesiş kalça dönüşüyle, ayaklar kaymaz | Ön süre 0,20 s ≈ 6 kare: 4 kare sakin bekleyiş + 1–2 kare kesiş. Bitiş pozunda 3–4 kare donma. Toparlanma 0,20 s (kılıç alçak bekleyişe iner) | Kısa yatay kor iz, yalnız kesiş karelerinde, yerinde |
| **ATIL** (Hareket) | **Zenitsu şimşek atılışı** (Kılıç'ta yalnız burada) | Gövde yere paralele yakın, kılıç geride. Kısa loop: yolculuk ~25 m/s (K 21:11; en hızlı Yumruk) | Giriş 2 kare, loop 6–8 kare | Yol boyunca kesik izi + kuyruk yayı silüeti (`efekt-motoru` §3, §3.4) |
| **DÖNÜŞ** (Yayılan) | Tanjiro su nefesi | Dönerek **yatay** kesiş. Gövde alçak, bıçak bel hizasında yatay düzlemde tam tur. Su gibi akış: kesik yayı kesintisiz, bitişte yumuşak yavaşlama, sert duruş yok | Tur ≈ 10–12 kare, kesiş boyunca hız sabit (su), son 4 kare yavaşlama | **Akan kor izi:** bıçağın arkasında dalgalı şerit, 0,3–0,4 s ömür |
| UZANMA, AĞIR | — | Mevcut | — | İz yalnız bıçak hızlıyken (`Trail_On/Off`) |

- **Event'ler:** `Trail_On` / `Trail_Off` animation event'i VUR, ATIL, DÖNÜŞ ve UZANMA kliplerinde kesişin ilk ve son karesine konur. Görüntü `efekt-motoru` §3.1'de.
- **İz rengi** fiilin renk ailesidir (`efekt-motoru` §1). `silah-fiil-120`'deki "sarı kesik izi" ifadesi şimşeğin şeklini anlatır, rengini değil [yorum].
- **Nefes:** VUR ve DÖNÜŞ'ün bekleyiş karelerinde göğüs / omuzda 2–3 karelik derin nefes hareketi (Spine2 yukarı). Kılıç'ta **şarj yok** (silahlar.md 22:18): 0,20 s ön süre iaido bekleyişidir, kor yalnız skill uyanışıyla 1 → 1,6 (`efekt-motoru` §4). Klip başına DZ işi, yeni klip yok.
- **Klip sayısı değişmez:** VUR, ATIL, DÖNÜŞ yuvaları zaten vardı; değişen içerik ve iz.
Kullanıcının örnekleri:
- **Kalkan omuz atma** = ATIL omuz hücumu (K 21:05 AS-15) + AĞIR kalkan itişi. VUR artık kenarla indirme. Kalkan fırlatılmaz (S-12 kapandı).
- **Dash** = her silahın ATIL'ı (§5).
- **Dodge takla** = ortak set (§5).
- **Çekiç tepede döndürme** = DURUŞ.

##### 2.2.2 Savunma duruşları (Koruma = DURUŞ, K 20:39)
Koruma yalnız seni korur, kısa savunma duruşudur (kural kitabı §2.1). Her silahın kendi duruşu vardır: **giriş (≈ 0,1 s) + loop**. Yoğun'da 0,45 s pencere, diğerlerinde `sure_s` (§3). Sıfat şekli (kubbe, yön, alan) VFX'tir. Kaynak hücre: `silah-fiil-120` Koruma satırları; kın yok (S-14).

| # | Silah | Savunma duruşu (loop) | Duruştaki hareket | Klip |
|---|---|---|---|---|
| 1 | Yumruk | Yan adım, kollar çene önünde yüksek guard, ayak ucunda | Guard seri ritmiyle açılıp kapanır, karşı vuruşa hazır | `Yumruk_DURUS` |
| 2 | Yay | Yarım çömelme, yay gövde önünde çapraz (sol el kabzada) | Gelen mermiyi yayla savuşturur gibi kısa yaylar | `Yay_DURUS` |
| 3 | Kitap | Kitap açık, göğüs önünde dik siper; boş el sayfaya basar, gövde hafif geri | Sayfa nefes gibi kabarıp iner; harfler gövdeye yazılır (VFX) | `Kitap_DURUS` (eski "okuma loop'u" savunmaya çevrildi) |
| 4 | **Kılıç** ★ | Ayaklar geniş ve alçak, kılıç gövde önünde, uç ileri-yukarı (orta guard); boş el önde denge | Loop'ta 2 karelik ani yarım savuşturma (parry) titremesi. Giriş: alçak bekleyişten guard'a kalkış | `Kilic_DURUS` |
| 5 | **Küre** | Küre göğse çekilir, boş el açık avuçla kürenin önünde; gövde yarım yana döner (dar profil) | Avuç kısa ışık itişleri yapar, küre parlaması duruşu besler | `Kure_DURUS` (eski "parlama loop'u" savunma hareketine çevrildi) |
| 6 | Çekiç ★ | Çekiç tepede, iki elle | Tepede döndürme; dönen çekiç saldırıları savurur (görüntü) | `Cekic_DURUS` |
| 7 | Top | Diz kırık, namlu gövde önünde çapraz, omuz namluya yaslı | Namluyla savuşturur gibi kısa sarsılma | `Top_DURUS` |
| 8 | **Asa** | Asa iki elle gövde önünde çapraz, alt ucu yere yakın; önde kor halka (VFX) | Asayı yarım çevirerek savuşturma (çubuk savunması) | `Asa_DURUS` (eski "asaya yaslanma" savunma hareketine çevrildi) |
| 9 | Tılsım | Avuç mührü göğse basar, diğer kol önde çapraz siper | Mühür parlar, kol kısa siper hareketi | `Tilsim_DURUS` |
| 10 | Kalkan ★ | Ağır adımla çöker, kalkanı öne dayar | Duruş boyunca yavaş yürüyüş (üst gövde loop'u) | `Kalkan_DURUS` |

Kullanıcının örnekleri (Kılıç savunma duruşu, Asa ve Küre savunma hareketleri) 4, 5 ve 8. satırlardır.

##### 2.2.3 Ejderha silüeti anları: hangi karede doğar (K 20:42)
Görüntü `efekt-motoru` §3.4. Yalnız fiziksel imza klipleri; mekanik değişmez; büyü klipleri silüet oynatmaz. Kareler 30 fps, ritim tablosundan (`blender-duzeltme-rehberi` §3) [tahmin, DZ sonrası `klipler.csv`'den kesinleşir].

| Silah | Klip | Doğduğu event / kare | Söndüğü an |
|---|---|---|---|
| Çekiç ① | `Cekic_VUR` ★ | **`Ejder`** (yeni event; Blender marker `ejder`) = tepe tutuşunun ilk karesi ≈ `impact` − 6 kare (−0,2 s) | `impact`'te yere gömülür, +0,25 s dissolve |
| Çekiç ② | `Cekic_ATIL` ★ | varış çakmasının `impact`'i (VUR'un teslim yarısı) | +0,3 s |
| Kılıç ① | `Kilic_ATIL` ★ (K 21:05: VUR'dan taşındı, VUR artık yerinde iaido) | ATIL state girişi (kod; kuyruk yayı yol boyunca savrulur) | +0,3 s |
| Kılıç ② | `Donus_Kilic` ★ | `Trail_On` (kart yok, iz dokusu) | `Trail_Off` + iz ömrü |
| Kalkan ① | `Kalkan_ATIL` ★ (K 21:05: omuz hücumuyla birlikte VUR'dan taşındı) | son ATIL parçasının varışı (kod, `Inis` girişi) | +0,25 s |
| Kalkan ② | `Kalkan_DURUS` ★ | state girişi (kod, event yok; pul deseni shader'da) | +0,2 s |
| Yumruk | `Yumruk_VUR` ★ | serideki **son** `impact` (aparkat; klipte birden çok `impact` varsa sonuncusu) | +0,2 s |
| Yay ① | `Yay_VUR` ★ | `impact` (bırakma) | +0,25 s |
| Yay ② | `Yay_ATIL` ★ | ATIL state girişi (kod) | +0,3 s |
| Asa | `Asa_VUR` ★ | `impact` (ışının doğduğu kare) | +0,3 s |
| Küre ① | `Kure_AGIR` ★ | `impact` | +0,25 s |
| Küre ② | `Kure_ATIL` ★ | ATIL state girişi (patlama) | +0,2 s |
| Tılsım | `Tilsim_VUR` ★ | **klip karesi değil:** gameplay olayı "mühür patladı" (+0,5 s, gecikmiş etki) | +0,25 s |
| Top | `Top_VUR` ★ | `impact` (ateş karesi) | +0,2 s |
| Kitap | `Kitap_AGIR` ★ | `impact` (kapanma) | +0,25 s |

- Yeni event yalnız **`Ejder`** (Çekiç VUR). Diğerleri mevcut `impact`/`Trail_On` event'ini dinler, klip başına event bütçesi (§8.2) aşılmaz.
- Yoğun'da şarj tutması (`Charge`) silüeti geciktirir: silüet event'e bağlı, saate değil.
- Aynı klip başka fiziksel komboda da oynarsa silüet de oynar (an klibe bağlı, istisna yok).

#### 2.3 Hücre metninden ayrılan jestler (VFX taşır, klip yok)
Deterministik kural bazı hücrelerin kelimesi kelimesine jestini başka klibe düşürür:
- **Büyü fiili hücreleri (8 × 10 = 80 hücre, K 20:40):** silaha özel jest (Kılıç Arındırma "dairesel kesiş", Kılıç Çağırma "kılıcı havaya atar", Yumruk Şifa "omuza vuruş", Kalkan Şifa "kalkanı dostun önüne koyar", Asa Kontrol "rün çizme", Kitap Güçlendirme "destan okuma"…) fiilin büyü klibine düşer. Silah tarzı efekt motorunda kalır (`efekt-motoru` §3.3). Bu kullanıcının kararıdır ("farkı efekt motoru taşır").
- **Zaman:** eski "VUR klibinin tersi" kalktı. Hücrelerdeki "geri savurur", "sayfaları geri çevirir" fikri `Buyu_Zaman`'ın geri sarma jestinde ve hayalet iz VFX'inde yaşar.
- **Fiziksel hücreler:** ≈ 6 hücrede klip jestten farklı [sayım: elle, yaklaşık].

#### 2.4 Ortak set (bütün silahlar)
Genel cast / genel kanal yedekleri K 20:40 ile kalktı: her fiilin kendi klibi var (MVP 11 → 9, hedef 15 → 13).

| Klip | Kullanım | MVP | Hedef |
|---|---|---|---|
| **Zıpla** | Dodge 1. basış (yay şeklinde, yükseklik görüntüde) | ✔ | ✔ |
| **Takla** | Dodge 2. basış (havada) | ✔ | ✔ |
| **İniş** | Zıpla/Takla/ATIL sonu, 0,15 s | ✔ | ✔ |
| **İşaret** (üst gövde) | Tetikli, İşaretli-Arındırma (işaret koyma) | ✔ | ✔ |
| **Pas** (üst gövde) | Ortak (fiili dosta yükleme). Eski `Throw` klibinin yerine geçer, state `Throw` | ✔ | ✔ |
| **Bedel** (additive) | Fedakarlık: göğse vuruş, kor sızar | ✔ | ✔ |
| Hit hafif / ağır, Death | — | ✔✔✔ | ✔✔✔ |
| Backstep, Sidestep (mirror) | Kilitliyken dodge yaslanması (hedef), diskte var | — | ✔✔ |
| Kalkma | Yere düşmeden sonra | — | ✔ |
| ATIL fren | Dash zincirinde keskin yön değişimi | — | ✔ |
| Revive | Koşullu (ucuzsa) | (1) | (1) |

Eski `Dodge` (roll) klibi **Takla'nın kaynağı olur**. Mixamo'da havada ön takla klibi yoksa roll klibi Blender'da havaya taşınır.

#### 2.5 Lokomosyon (değişmedi)
7 aile (Kılıç, Kalkan, Ağır 2 el, Yumruk, Yay, Büyücü, Kitap). MVP Idle + Run, hedefte + Walk + Durma.

#### 2.6 Büyü klipleri (K 20:39–20:40)
Kural: her büyü fiilinin **kendi** basit klibi var, ortak hareket yok. Klipler "inanılmaz" olmak zorunda değil; okunurluğu **silüet yönü** + efekt motoru taşır.

**Kaç klip? Öneri: fiil başına 1 klip, el tutuşunu aynalama çözer (MVP 8).**

| Seçenek | MVP klip | Okunurluk | İş [tahmin] |
|---|---|---|---|
| A · Silah başına (8 × 10) | 80 | En çok tarz, ama fark zaten VFX'te | ≈ 25–40 gün |
| B · Lokomosyon ailesi başına (8 × 7) | 56 | Aile içinde aynı | ≈ 18–28 gün |
| C · El tutuşu başına (8 × 3: silah sağda / solda / iki elde) | 24 | Tutuş doğal | ≈ 8–12 gün |
| **D · Fiil başına 1 klip + Mirror (öneri)** | **8** | 8 ayrı silüet; silah eli nötr | **≈ 3 gün** |

D'nin işleyişi:
- **Klip yalnız üst gövde** (`Mask_UpperBody`, Upper katman). Alt gövde Base'teki Idle/Run'dan gelir: yürürken de, dururken de aynı klip.
- **Büyü eli tablosu (K 20:58).** Klip **sol elle yapılan jest** olarak hazırlanır; diğer kol **göğüs önünde taşıma** pozundadır. Hangi elin jest yapacağı silaha göre seçilir, Animator state'inde `Mirror` (yalnız Humanoid, Unity 6 kılavuzu "Animation States") `BuyuAyna` parametresiyle açılır:

| Silah | Silah hangi elde | Jesti yapan | `BuyuAyna` | `Grip_L` | VFX doğuşu (`efekt-motoru` §3.3) |
|---|---|---|---|---|---|
| Kılıç | sağ | **silah eli** (kılıçla jest) | açık | — | `Vfx_Tip` (bıçak ucu) |
| Küre | sağ | **silah eli** (küreli el) | açık | — | `Vfx_Core` (küre) |
| Tılsım | sağ | **silah eli** (mühürlü avuç) | açık | — | `Vfx_Core` (mühür) |
| Asa | sağ (iki elli) | **silah eli** (asayla jest) | açık | 0 | `Vfx_Tip` (kristal) |
| Çekiç | sağ (iki elli) | **silah eli** (tek elle, Thor gibi) | açık | 0 | `Vfx_Tip` (çekiç başı) |
| Kalkan | sol önkol | **silah kolu** (kalkanla jest) | kapalı | — | `Vfx_Core` (kalkan yüzü) |
| Kitap | sol | **silah eli** (kitapla jest) | kapalı | — | `Vfx_Core` (açık sayfa) |
| Yay | sol | boş el (sağ, çekme eli) | açık | — | `Vfx_Buyu` (avuç) |
| Top | sağ (iki elli) | boş el (sol) | kapalı | 0 | **her zaman `Vfx_Muzzle`** (namlu) |
| Yumruk | iki el boş | sol el | kapalı | — | `Vfx_Buyu` (avuç) |

- **Göğüs önünde taşıma:** jest yapmayan kol dirsek ≈ 90°, el göğüs/bel önünde; silah varsa ileri bakar (**Top namlusu ileri**, Yay önde dik). Jest boş eldeyse diğer el (silahsız) yumruk hâlinde göğüs önünde. Jest silah elindeyse silah jestle birlikte hareket eder; başlangıç ve bitiş pozunda o da göğüs önündedir. **Çekiç tek elle taşınırken başı ileri bakar.** Silah yanda sarkmaz.
- **Jest kol yönüyle okunur, avuç şekliyle değil:** silah tutan elde `El_Kavra` 100 kalır; açık avuç ve pençe (Zayıflatma %50) yalnız boş elde. Silahla yapılan jestte yön aynıdır: Şifa'da silah öne sunulur, Güçlendirme'de havaya kalkar, Yansıma'da önde dik durur.
- Büyü klibinin sayısı değişmez (8). Aynalama ve `Grip_L` yalnız state parametresi.
- **Silah ritmi:** her büyü klibinde `Charge` karesi var (impact'ten ≈ 4–6 kare önce). Silahın ön süresi (Çekiç 0,55 s) bu karede tutularak karşılanır; `ActionSpeed` 0,7–1,4 içinde kalır.
- **Yoğun şarjı** yalnız düşmana işleyen büyülerde (Kontrol, Zayıflatma), aynı `Charge` karesinde.

| Klip | Fiil | Silüet (jest eli; okunur yön; silah elinde avuç şekli yerine kol yönü) | Kareler / event | Kaynak önerisi |
|---|---|---|---|---|
| `Buyu_Sifa` | Şifa | Avuç yukarı bakar, göğüsten öne-yukarı yavaşça **sunar**; omuz yumuşar | Charge → Impact (avuç en önde) | PC Shared_Magic_Heal + DZ tek el |
| `Buyu_Guclendirme` | Güçlendirme | Yumruk sıkılır, başın üstüne **kalkar**, göğüs açılır | Charge → Impact (tepe) | MX "fist pump" [kontrol et] |
| `Buyu_Arindirma` | Arındırma | Açık avuçla önden **yana süpürme** (silme) | Charge → Impact (süpürmenin ortası) | MX → A3 |
| `Buyu_Zayiflatma` | Zayıflatma | Pençe (El_Kavra %50) yukarıdan **aşağı bastırır** | Charge (pençe yukarıda) → Impact (aşağıda) | PC Caster_1H_Cast + DZ |
| `Buyu_Kontrol` | Kontrol | Kol ileri uzanır, yumruk sıkılır, dirsek **geri çekilir** (tasma çekişi) | Charge (kol önde açık) → Impact (sıkış) | MX → A3 |
| `Buyu_Yansima` | Yansıma | Avuç öne **dik** ("dur"), bilekten yarım çevirme | Impact (avuç dik) → `Tut` (pencere / 2 s boyunca donar) | A3 (basit) |
| `Buyu_Cagirma` | Çağırma | Avuç yukarı bakar, **aşağıdan göğse kaldırır** (yerden çağırma) | Charge (avuç kalça hizasında) → Impact (göğüs hizası) | MX → A3 |
| `Buyu_Zaman` | Zaman | Önde **saat yönünün tersine daire** (geri sarma) | Charge → Impact (daire kapanınca) | A3 (basit) |

- Sekiz yön birbirinden farklı: öne-sun, yukarı, yana, aşağı, çek, dik dur, aşağıdan kaldır, daire. Telefonda silüetten ayrılır [test: O2 benzeri onay].
- Uzunluk 0,4–0,7 s [tahmin]; Yansıma'nın `Tut` karesi dışında loop yok.
- VFX doğuş noktası **jesti yapan elden** (yukarıdaki tablo): silah eliyse silahın `Vfx_*` noktası, boş else avuç (`Vfx_Buyu`); **Top'ta her zaman namlu** (`efekt-motoru` §3.3).

---

### 3. Kural tablosu: 12 sıfat → biçim, zamanlama, yön

Niyet sınıfları:
- **TESLİM** = VUR, AĞIR, YERE ve Yansıma dışındaki 7 büyü klibi (bir şey çıkarır)
- **DURUŞ** = Koruma (silahın savunma duruşu) ve Yansıma (zamanlama sınıfı DURUŞ, klibi `Buyu_Yansima` + `Tut`)
- **ATIL** = Hareket (kendini taşıma)

Biçim değişikliği yalnız ilgili sınıfa uygulanır. Bu bir istisna değil, sınıfın kuralıdır: duruşun "şekli" kubbe/yön VFX'idir, Hareket'in "şekli" yoldur.

**Büyü kuralı (K 20:40):** biçim yuvaları (YERE, DÖNÜŞ, UZANMA) yalnız **fiziksel** fiile uygulanır. Büyü fiilinde tablonun "TESLİM'de" sütunundaki YERE/DÖNÜŞ/UZANMA yerine **o fiilin büyü klibi** oynar; sıfatın şekli (yapı, halka, hat, yörünge) yalnız VFX'tir. Zamanlama, yön, İşaret, Pas, Bedel ve Güdümlü koşusu aynen işler.

| # | Sıfat | TESLİM'de | DURUŞ'ta | ATIL'da (Hareket) | Zamanlama | Yön |
|---|---|---|---|---|---|---|
| 1 | **Yoğun** | Niyet klibi. Fiil düşmana işliyorsa (Zarar, Kuvvet, Kontrol, Zayıflatma) **şarj tutma** 0,35 s: klip `Charge` karesinde `ActionSpeed=0`, kor `_Intensity` 1→2,5 dolar. Diğer fiillerde anında (K 19:41) | DURUŞ **0,45 s pencere**: hızlı giriş (0,06 s crossfade), pencere bitince çıkış | ATIL, anında | şarj / pencere | hedefe dön |
| 2 | **Odaklı** | Niyet klibi | DURUŞ | ATIL, canı en az düşmana (bitirici, K 18:42) | normal | hedefe kilitli dönüş (yaw snap 0,08 s) |
| 3 | **Sıçrayan** | Niyet klibi **bir kez**; sekmeler VFX | DURUŞ | ATIL **3 parça**, her sekme bir yol parçası (dash zinciri geçişi) | normal | ilk hedefe |
| 4 | **Sabit** | **YERE** (fiziksel fiilde: yapı dikilir; büyüde büyü klibi + yapı VFX) | **YERE** (Koruma: duvar dikilir; Yansıma: büyü klibi) | **YERE** (sıçrama taşı) | YERE klibi, yapı vuruş karesinde doğar | dikilecek yere |
| 5 | **Yayılan** | **DÖNÜŞ** (tam; büyüde büyü klibi + halka VFX) | DURUŞ (halka kubbe VFX) | ATIL merkezden **dışa** (otomatik merkez = en yakın düşman, K 18:42) | normal | dönüş; ATIL'da merkezin tersine |
| 6 | **Güdümlü** | Yakın silah (Yumruk, Kalkan, Kılıç, Çekiç; `silah-fiil-120` "yakın silahlar"): **koşu** (Base: lokomosyon Run, hız = takip hızı) + **Upper: niyet klibi** her kilit vuruşunda (Zarar: 3 vuruş, 0,3 s arayla, ilk ikisi VUR'un teslim yarısı, sonuncusu tam). Uzak silah: niyet klibi bir kez, mermi kovalar (VFX) | DURUŞ, **iptal edilemez** (kilit sende) | ATIL doğrudan hedefe | ilk 3 s giriş kilitli | hedefe sürekli |
| 7 | **Tetikli** | **İşaret** klibi (üst gövde, silahlı elle). Tetik anında klip yok (gecikmiş etki) | İşaret | İşaret; tetikte ATIL başlar (oyuncu o an başka şey yapıyorsa ATIL keser, H-1). Süre tetiksiz dolarsa boşa gider, klip yok (K 22:15) | kısa | hedefe |
| 8 | **Fedakarlık** | **Bedel** (additive, 0,25 s) + niyet klibi | Bedel + DURUŞ | Bedel + ATIL | bedel teslimden önce | hedefe |
| 9 | **İşaretli** | Niyet klibi **yerinde**, yankı işarette (VFX). İşareti koyan tek skill (**İşaretli-Arındırma**, kitap 9-9) → **İşaret** klibi | DURUŞ | ATIL: işarete **yol alarak** çekilme, mesafe = silah menzili, ışınlanma yok. Görünüm silahın ATIL'ı (K 20:35): Yay ip atıp Levi gibi süzülme, Çekiç fırlatıp Thor gibi peşinden uçma + varışta çakma, Kalkan ≤ 3 m omuz hücumu, Kılıç şimşek atılışı (Zenitsu), Asa ışın yolunda süzülme, Küre patlama itişiyle fırlama + 0,4 s yavaşlama, Tılsım mühür 0,5 s sonra çeker, Top gülle gibi fırlatır, Kitap sayfalara dağılıp akış, Yumruk kısa atılış. Hız ve yolda skill: `silahlar.md` yolculuk tablosu. Zincirde işaretten işarete parça parça | normal | işarete |
| 10 | **Çizgisel** | **UZANMA** (büyüde büyü klibi + hat VFX) | DURUŞ (önden yön VFX) | **Işınlanma:** ATIL'ın ilk 0,1 s'si + kor dissolve çıkış/giriş + İniş; hız almaz/vermez (H-4) | anında | hat yönünde |
| 11 | **Ortak** | **Pas** (dosta yükleme). Dostun skilli kendi klibini oynar, sende klip yok. Yük kullanılmazsa boşa gider, klip yok (K 22:15) | Pas | Pas; dost skill atınca **ATIL** dostun skillinin vardığı noktaya (bu senin kendini taşıman, klip oynar) | kısa | dosta |
| 12 | **Yörüngeli** | **DÖNÜŞ, yalnız ilk yarı** (normalize 0→0,5: parçaları salar; büyüde büyü klibi, parçalar VFX) | DURUŞ (parçalar VFX) | ATIL, teğet yön: dostun etrafında 2 m yörünge (gövde sensin) | normal | dönüş |

Notlar:
- **Ortak-Hareket'te ATIL** dost skill atınca başlar. Bu "gecikmiş etki klip oynatmaz" kuralına uymaz; bilerek böyle: kendini taşıma oyuncunun gövdesini taşır, poz zorunludur. Kural metni: *"Gövdeyi taşıyan gecikmiş etki ATIL oynatır; gövdeyi taşımayan gecikmiş etki klip oynatmaz."* Tetikli-Hareket de buna girer. Tek cümle olduğu için istisna değil, kuraldır.
- **Zaman fiili (K 20:40 ile değişti):** "VUR ters oynar" kuralı **kalktı**. Zaman `Buyu_Zaman`'ı **ileri** oynatır (geri sarma hissi jestin kendisinde: saat yönünün tersine daire). Hedefin geri dönüşü hayalet iz VFX'idir, klip oynamaz (gecikmiş etki kuralı). `AnimPlan.ters` alanı kullanılmaz.
- **Yansıma (K 20:40 ile değişti; oran K 22:15: taban 0,85, boss'un tek vuruşunda geri dönen hasar en çok boss canının %5'i; animasyonu etkilemez):** `Buyu_Yansima` oynar, `Tut` karesinde pencere boyunca (2 s; Yoğun'da 0,45 s) donar. Eski "karşı vuruş (VUR teslim yarısı)" kalktı; geri dönen etki yalnız VFX (ayna parıltısı). S-5 kapandı.

---

### 4. Kombo çözümü: deterministik algoritma

#### 4.1 Girdi ve çıktı
- **Girdi:** kural motorunun ürettiği `SkillSpec`. Hücre metni değil, kuralların hesapladığı alanlar:
  - `silah`, `fiil`, `sifat`, `taraf`, `kendini_tasir`, `yol[]` (parçalar), `sarj_s`, `pencere_s`, `sure_s`, `vurus_anlari[]`, `isaret_koyar`, `yuk` (Ortak), `tetik` (Tetikli), `yapi` (Sabit), `kilit` (Güdümlü), `bedel` (Fedakarlık), `hedef`.
- Bunlara ek olarak oyuncunun anlık durumu: hareket hızı, havada mı, kilitli mi.
- **Çıktı:** `AnimPlan`, sıralı adım listesi. Her adım: `{katman, state, başla_norm, bitir_norm, hız, tut_karesi, tut_sn, ayna, grip_l, yaw_hedefi, eventler}` (eski `ters` alanı K 20:40 ile kalktı).
- Aynı `SkillSpec` her cihazda **aynı AnimPlan**'ı üretir: rastgelelik yok, kare süresine bağlı karar yok.

#### 4.2 Sözde kod
```text
FIZIKSEL = { Zarar:VUR, Kuvvet:AĞIR, Koruma:DURUŞ, Hareket:ATIL }          # K 20:39
BUYU     = { Zaman, Kontrol, Şifa, Arındırma, Güçlendirme, Zayıflatma, Yansıma, Çağırma }
SINIF  = { VUR,AĞIR,YERE,BUYU_* → TESLİM;  DURUŞ, BUYU_Yansima → DURUŞ;  ATIL → ATIL }
BUYU_AYNA = {Kılıç, Küre, Tılsım, Asa, Çekiç, Yay}       # §2.6 tablosu (K 20:58)
IKI_ELLI  = {Çekiç, Top, Asa}
DUSMAN_FIILI = {Zarar, Kuvvet, Kontrol, Zayıflatma}
YAKIN = {Yumruk, Kalkan, Kılıç, Çekiç}
HAREKETLI_CAST = {Yumruk, Kalkan, Kitap}

fonksiyon Coz(spec, durum) -> AnimPlan:
  plan = []
  buyu  = spec.fiil in BUYU
  yuva  = buyu ? "Buyu_"+spec.fiil : FIZIKSEL[spec.fiil];  sinif = SINIF[yuva]
  ayna  = buyu ve spec.silah in BUYU_AYNA                # Animator state Mirror
  ikiel_birak = buyu ve spec.silah in IKI_ELLI           # Grip_L IK ağırlığı 0

  # 1) Gövde dışı bağlayıcılar: cast anında yalnız jest, etki sonra (VFX)
  eger spec.sifat == Ortak:
      plan += Adim(UPPER, "Pas");  plan.erteleme(Ortak_yuk_ATIL eger sinif==ATIL)
      dondur plan
  eger spec.sifat == Tetikli  veya  spec.isaret_koyar:
      plan += Adim(UPPER, "Isaret", yaw=spec.hedef)
      eger sinif==ATIL: plan.erteleme(tetikte ATIL)      # gövdeyi taşıyan gecikmiş etki
      dondur plan

  # 2) Biçim yuvası (sıfat): yalnız fiziksel fiil; büyüde şekil VFX
  eger buyu:                                  gec
  yoksa spec.sifat == Sabit:                  yuva = YERE                       # her fiziksel sınıfta
  yoksa sinif == TESLİM:
      eger spec.sifat == Yayılan:             yuva = DÖNÜŞ
      eger spec.sifat == Yörüngeli:           yuva = DÖNÜŞ;  aralik = [0, 0.5]
      eger spec.sifat == Çizgisel:            yuva = UZANMA
  yoksa sinif == ATIL ve spec.sifat == Çizgisel:
      dondur [Adim(BASE,"Atil",0,0.1), Vfx(DISSOLVE_CIK), Isinla, Vfx(DISSOLVE_GIR), Adim(BASE,"Inis")]

  # 3) Bedel (Fedakarlık) önde, additive
  eger spec.sifat == Fedakarlık:  plan += Adim(ADDITIVE, "Bedel", süre=0.25)

  # 4) Yol: kendini taşıyan her şey ATIL parçalarıdır (Güdümlü yakın koşusu dahil)
  eger sinif == ATIL:
      her parca p in spec.yol:  plan += Adim(BASE, "Atil", döngü, hız=p.hız/ATIL_ref, yaw=p.yön)
      plan += Adim(BASE, "Inis")
      dondur plan
  eger spec.sifat == Güdümlü ve spec.silah in YAKIN ve sinif == TESLİM:
      plan += Adim(BASE, "Locomotion", hız=spec.takip_hızı, yaw=spec.hedef, süre=varana_kadar)
      her t in spec.vurus_anlari:                       # Zarar: 3 vuruş, 0,3 s arayla
          plan += Adim(UPPER, yuva, başla=son_vurus_mu(t)?0:teslim_yarisi, hizala=t)
      dondur plan

  # 5) Zamanlama
  tut = 0
  eger spec.sifat == Yoğun ve spec.fiil in DUSMAN_FIILI:  tut = spec.sarj_s          # 0,35 s
  eger sinif == DURUŞ:  süre = (spec.sifat == Yoğun) ? spec.pencere_s : spec.sure_s  # 0,45 s
  tut = max(tut, silah_ritim_tutma(spec.silah, spec))   # Yay otomatik germe, Çekiç kaldır-bekle, Asa akıtma (JSON'dan); Kılıç'ta şarj yok

  # 6) Katman: oyuncu yürüyorsa üst gövde
  katman = (durum.hiz > 0.15 ve sinif != ATIL) ? UPPER : BASE
  eger buyu:  katman = UPPER                              # büyü klibi hep üst gövde
  eger spec.silah in HAREKETLI_CAST ve sinif == TESLİM:  katman = UPPER_eger_hareketli

  plan += Adim(katman, yuva, aralik, ayna=ayna, grip_l=(ikiel_birak?0:1), tut_karesi="Charge", tut_sn=tut,
               hizala=spec.vurus_anlari[0], yaw=spec.hedef, döngü=(sinif==DURUŞ), süre)

  # 7) Silah ritmi kuyruğu
  eger spec.silah == Küre ve sinif == TESLİM:  plan.yurume_carpani(0.5, 0.4)   # klip yok, toparlanma 0,4 s (silahlar.md 22:18)
  dondur plan
```

Yan kurallar (bunlar da istisnasız, durumdan gelir):
- **Silah kanunu yok (K 20:29, K 21:05 AS-03):** süper zırh ve "yalnız yakın silahta dodge iptali" kuralları silindi. Vurulma (`Hit`) ve dodge her silahta aynı davranır (§5).
- **Güdümlü kilidi:** ilk 3 s yeni skill ve dodge girişi yok sayılır. Dodge kısmı yoruma açık → §11 S-4.
- **Güdümlü sigortaları (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s sürer (K 22:54; kendine, dosta ve düşmana). Kilit bitince **çıkış geçişi**: yakın silahta takip koşusundan lokomosyona 0,15 s crossfade (Base `Run` → `Idle/Run`, hız devri yok) ve Upper ağırlığı 0,1 s'de 0; oynayan vuruş yarıda kesilmez, bitince çıkar. Yeni klip yok.
- **Hizalama:** `ActionSpeed` 0,7–1,4 dışına çıkarsa klip değil JSON uyarısı verilir (editör testi).

#### 4.3 Örnek çözümler (12 sıfatın hepsi; 21 test vakası)
| # | Kombo | Çözüm (sırayla) |
|---|---|---|
| 1 | **Kalkan · Zarar · Yoğun** | VUR (ağır adım + kenarla indirme), `Charge` karesinde (kalkan tepede) 0,35 s tutma, kor dolar → teslim. Yer değiştirme yok; omuz hücumu yalnız Hareket'te |
| 2 | **Çekiç · Koruma · Yoğun** | DURUŞ (tepede döndürme), 0,06 s giriş, **0,45 s** pencere, çıkış. Şarj yok (Koruma düşman fiili değil) |
| 3 | **Çekiç · Zarar · Yayılan** | DÖNÜŞ (1,5 tur çekiç), Çekiç ritmi: tepede kısa tutma (JSON) → halka vuruş karesinde |
| 4 | **Kılıç · Zarar · Güdümlü** | Base: Kılıç Run (takip hızı) + Upper: VUR teslim yarısı @t1, @t1+0,3; tam VUR @t1+0,6 (×0,6/×0,6/×1,0). İlk 3 s giriş kilitli |
| 5 | **Yay · Zarar · Güdümlü** | VUR bir kez (otomatik 0,35 s germe → bırak; erken bırakma yok, K 22:15). Mermi kovalar (VFX/varlık), oyuncu serbest |
| 6 | **Asa · Kontrol · Çizgisel** | `Buyu_Kontrol` aynalı (asa eli ileri uzanır, geri çeker; `Grip_L` 0, VFX kristalden). Çizgisel hat ve tasma VFX'tir (efekt-motoru §5), UZANMA oynamaz (büyü fiili) |
| 7 | **Tılsım · Zarar · Sabit** | YERE (diz, yere mühür) → dikenli direk vuruş karesinde doğar; sonraki nabızlar VFX |
| 8 | **Yumruk · Şifa · Ortak** | Pas (üst gövde, yürürken). Dostun sonraki skilli kendi klibini oynar; senin fiilinin motifi onun şekline biner (VFX) |
| 9 | **Kitap · Zarar · Tetikli** | İşaret (yürürken, üst gövde). Tetik: yalnız VFX |
| 10 | **Top · Kuvvet · Fedakarlık** | Bedel (additive 0,25 s) → AĞIR (doldur-ateş). Yoğun değil, şarj yok |
| 11 | **Küre · Arındırma · İşaretli** | İşaret (işareti yalnız İşaretli-Arındırma koyar) → yürüme yavaşlaması yok (TESLİM değil, İşaret klibi) |
| 12 | **Küre · Zayıflatma · İşaretli** (işaret var) | `Buyu_Zayiflatma` yerinde (pençe aşağı), yankı işarette → 0,4 s toparlanmada yürüme ×0,5 (klip yok) |
| 13 | **Kılıç · Zaman · Yörüngeli** | `Buyu_Zaman` **ileri**, aynalı (kılıçla geri sarma dairesi, VFX bıçak ucundan). Yörünge parçaları VFX. Eski "DÖNÜŞ ters" kalktı |
| 14 | **Yumruk · Hareket · Sıçrayan** | ATIL ×3 parça (dalış → yön değiştir → dalış), parça arası dash zinciri geçişi → İniş |
| 15 | **Kalkan · Hareket · Odaklı** | ATIL (siper altında kayış), canı en az düşmana → İniş |
| 16 | **Tılsım · Hareket · Çizgisel** | Atil 0–0,1 → dissolve çıkış → ışınlanma → dissolve giriş → İniş, hız 0 |
| 17 | **Asa · Yansıma · Yayılan** | `Buyu_Yansima` aynalı (asa önde dik), `Tut` karesinde 2 s donma; halka kubbe ve geri dönen etki VFX. Karşı vuruş yok |
| 18 | **Çekiç · Çağırma · Güdümlü** | `Buyu_Cagirma` aynalı (çekiç tek elle aşağıdan kalkar, `Grip_L` 0, VFX çekiç başından). Çekiç yakın ama Güdümlü-Çağırma'da kovalayan **yardımcıdır**, sen değilsin. `kendini_tasir=false` olduğundan koşu yok. Spec bunu verir, algoritma sormaz |
| 19 | **Yay · Şifa · Odaklı** | `Buyu_Sifa` **aynalı** (Yay sol elde → jest sağ elde), en çok ihtiyacı olan dosta dönüş |
| 20 | **Kılıç · Koruma · Yoğun** | `Kilic_DURUS` (orta guard), 0,06 s giriş, 0,45 s pencere, loop'ta savuşturma titremesi |
| 21 | **Top · Şifa · Odaklı** | `Buyu_Sifa` aynasız: sol (boş) el sunar, top sağ elde göğüs önünde namlu ileri; kor mermi **namludan** (`Vfx_Muzzle`) |

Son satır kuralın gücünü gösterir: **"koşar mıyım?" sorusunu fiil/sıfat adı değil, spec'teki `kendini_tasir` ve `yol` cevaplar.** Kural kitabı değişirse animasyon tablosu değişmez.

---

### 5. Hareket kombosu (dodge, zıplama, takla, dash zinciri)

#### 5.1 Kural (kullanıcı 19:48) ve kitapla bağı
- **Dodge bir kez → zıplama.** Yol ve süre bugünkü `DodgeMotion`'dan. Yükseklik **yalnız klipte** (Y eğrisi pozda): collider düzlemde kalır.
  - Kitapta dikey fizik kuralı yok; `fizik-oneriler` "v4'te dikey fizik kapalı" öneriyor (KG-23 açık). Görsel zıplama ikisini de bozmaz.
  - Zıplamanın alan saldırısından kaçırıp kaçırmayacağı → §11 S-1.
- **Havadayken (inmeden) tekrar → takla.** Takla **ikinci bir kendini taşımadır.** K 15:53 kuralı olduğu gibi işler, yeni kural gerekmez:
  - eski yol kesilir (H-1)
  - eski hızın yarısı yeni yöne aktarılır (H-2)
  - toplam en çok 1,5× (H-3)
  - Dodge'un "kendini taşıma" sayılması → §11 S-2.
- Havada en çok **1 takla** [test]. Üçüncü basış yok sayılır.

#### 5.2 Durum makinesi
```text
YER   --dodge-->              ZIPLA (Zıpla klibi, yol = DodgeMotion)
ZIPLA --dodge (inmeden, 1 kez)--> TAKLA (Takla klibi, H-1/H-2/H-3 hız devri)
ZIPLA|TAKLA --yol bitti-->    INIS (0,15 s)  --> YER
herhangi --Hareket skilli-->  ATIL (yeni yol keser, hız devri)        # dash zinciri
ATIL  --Hareket skilli-->     ATIL (yaw snap 0,08 s; hedefte "ATIL fren" klibi)
ATIL  --dodge-->              ZIPLA (dodge kendini taşımaysa keser + devir; değilse yok sayılır → S-2)
ATIL|ZIPLA --saldırı/şifa/silah değiştirme--> aynı durum + UPPER'da teslim (H-1: kesmez)
INIS  ilk 0,1 s --skill-->    skill (iniş toparlanması iptal)
Çizgisel-Hareket (ışınlanma) / Zaman dönüşü --> varış hızı 0 (H-4), INIS
Güdümlü ilk 3 s --> dodge ve skill girişi yok sayılır (S-4)
Güdümlü kilit bitti (hedef öldü/kayboldu ya da 8 s doldu, bütün kilitler) --> 0,15 s çıkış geçişi --> YER (lokomosyon)
ATIL  --kenara/canlıya değdi (Yoğun-Hareket dahil)--> INIS (0,15 s) + kor kıvılcımı VFX
dışarıdan itme, tasma --> durum değişmez (H-5), ATIL'ın üstüne Hit additive yok
```

#### 5.3 Dash zinciri
- Dash zinciri yeni bir mekanik değil, H-1/H-2/H-3'ün görüntüsüdür.
- Arka arkaya Hareket skilleri (ya da ATIL → dodge → ATIL) her seferinde eski yolu keser.
- `Atil` state'i kendinden kendine **0,06 s** crossfade ile girer. Klip hızı = yeni hız / referans hız, yani hız devri klipte de görünür.
- Yön farkı 90°'yi geçerse hedefte "ATIL fren" klibi araya girer (0,1 s, hedef setinde).
- **Kenarda fren (K 22:15):** canlılar birbirinin içinden geçemez, kenarda durur; Yoğun-Hareket (3-1) kenarda frenler. **Seçim: İniş + kor kıvılcımı** (ATIL fren klibi MVP'ye alınmadı, MVP 96 kalır). Gerekçe: İniş zaten MVP'de, 0,15 s ve hız 0'a inen gövdeyi okunur gösteriyor; kıvılcım çarpma noktasını işaretler (`efekt-motoru` §5.5); ATIL fren klibi +1 klip ve ≈ 0,25–0,5 gün olurdu, telefonda 0,1 s'lik farkı İniş'ten ayırmak zor [yorum]. Hedef sette ATIL fren gelince kenar freni de ona bağlanabilir.

#### 5.4 Kapsül ve menzil (K 22:15, ayarlanabilir başlangıç değerleri)
- Kapsül yarıçapı: oyuncu **0,35 m**, yaratık 0,5 m, yapı 1 m, boss **2,5 m**. Menzil **gövde kenarından** ölçülür.
- Animasyona etkisi: kökten değil kenardan ölçüldüğü için klipte değişen bir şey yok; ATIL varış noktası hedef kenarı (merkez − yarıçap), kod hesaplar.
- Yörüngeli'nin yarıçapı = taşıyanın yarıçapı + 2 m (oyuncuda 2,35 m). Yarım DÖNÜŞ klibi bundan etkilenmez; parça yörüngesi VFX'tir (`efekt-motoru` §5.4).
- İz VFX'i zincir boyunca kopmaz (`efekt-motoru` §4).

---

### 6. Animator Controller mı, Timeline mı?

**Öneri: Animator Controller kalır.** Base + silah başına Override, mevcut `ActorVisual.PlayAction` / `CrossFadeInFixedTime`. Timeline gameplay'de kullanılmaz.

| | Animator Controller + Override | Timeline |
|---|---|---|
| 1440 skill | 7 yuva × 10 silah + 8 büyü klibi (silahtan bağımsız, `Mirror` parametreli), kural klibi seçer | Her kombo için bir Timeline asset'i ya da runtime'da kurulan graph gerekir |
| Kesme (dodge, hız devri) | State geçişi, crossfade | PlayableDirector durdurulup başlatılır, ara poz yok |
| 5 oyuncu | Aynı override asset'i paylaşılır | Oyuncu başına PlayableDirector + bağlama |
| Mevcut kod | Değişmez, state adları korunur | Yeniden yazım |
| Uygun olduğu yer | Bütün oynanış | Boss girişi, ara sahne, kostüm vitrini |

AnimPlan'ın "sıralı adım" yapısı, bir kod sıralayıcısının (`AnimPlanRunner`) Animator'a `CrossFade` çağrıları göndermesiyle oynar. Timeline'a gerek yoktur.

**Yeni state'ler:**
- Base: `Atil`, `Uzanma`, `Takla`, `Inis`
- Upper: `UpperIsaret`; Pas için mevcut `UpperThrow`/`Throw`; **`UpperBuyu`** (8 büyü klibi override'la değil, fiil parametresiyle seçilir; state `Mirror` = `BuyuAyna` bool parametresi). BAĞ/DESTEK state'leri silinmez, boş kalır
- Additive: `Bedel`

Bu state'ler kod tarafında `ActorVisual`'a eklenecek iştir (repo, §9). Playables API ile özel graph → Sonra (§10).

#### 6.1 Katmanlar: 2 → 3
| # | Katman | Maske | Ne oynar | Ağırlık |
|---|---|---|---|---|
| 0 | Base | tam gövde | lokomosyon, TESLİM (dururken), DURUŞ, ATIL, Zıpla/Takla/İniş, Death | 1 |
| 1 | UpperBody | `Mask_UpperBody` | yürürken TESLİM, **bütün büyü klipleri (dururken de)**, Güdümlü kilit vuruşları, İşaret, Pas, Kitap okuma | 0/1 (0,1 s) |
| 2 | **Additive** (yeni) | tam gövde, additive | Bedel (Fedakarlık), dosta gelen Güçlendirme/Şifa titremesi | çoğu zaman 0 |

- **Gerekçe:** Bedel, oynayan klibi kesmeden üstüne binmelidir. Bunu Upper katmanla yapmak koşu sırasındaki teslimi keserdi. (Süper zırh sarsılması K 21:05 AS-03 ile kalktı; katmanın tek ağır gerekçesi Bedel.)
- **Maliyet:** Ağırlığı 0 olan katmanın state makinesi yine güncellenir. Poz değerlendirmesinin atlanıp atlanmadığı **doğrulanmadı**, cihazda profillenmeli.
- **Yedek:** Additive katman pahalı çıkarsa Bedel, Upper'da 0,25 s'lik bir klip olur.
- **Önceki plan "en çok 2 katman" diyordu** (benim önerimdi, kullanıcı kilidi değil). Değişiklik:
  - `animasyon-plani` §1.7 madde 2 yeniden yazılır, **0,25 gün**.
  - Kod: Animator'a katman ve `Bedel` state'i, **0,5 gün** [tahmin].

---

### 7. Root motion

**Öneri: kapalı kalır** (`applyRootMotion=false`, `animasyon-plani` §1.5/§1.6 madde 4). Koreografi bunu değiştirmez. Gerekçe:

1. **Mesafe kuraldan gelir:** silah menzili, Hareket'te en fazla yer değiştirme (`silahlar.md` 15:42), hız devri 0,5 / 1,5×, Yayılan ×0,85. Root motion klibin mesafesini dayatır, kuralı ezer.
2. **Co-op ve sim:** 5 cihaz aynı yolu görmeli. Kare atlayan telefonda root motion'ın birikimi sapar. Sim (sim/) kod hareketini birebir yeniden üretiyor, klip eğrisini üretemez.
3. **Kalkan ve Kılıç'ın ön süre adımı** "silah menzilini aşmaz, Hareket sayılmaz" (kitap §6). Bu bir kod sınırıdır, root motion bunu garanti edemez.
4. **Culling:** uzak oyuncular `Cull Update Transforms`. Root motion açık olsaydı ekran dışındayken yol kaybolurdu.

**Koreografiyi kaybetmemek için:**
- Klipler XZ Bake Into Pose **kapalı** indirilir. Gövde collider'ın üstünde kalır; adımın hissi klipte, yolu kod yapar.
- Zıpla ve Takla'da **Y pozda** kalır (yükseklik yalnız görüntü).
- ATIL loop'unun hızı yol hızına bağlanır (§5.3).
- Klibin kök eğrisinden hız profili çıkarıp yalnız **kolaylaştırma eğrisi** olarak kullanmak (mesafe yine kuraldan) → Sonra (§10, S-a).

---

### 8. Mobil bütçe ve klip sayısı

#### 8.1 Klip sayısı (K 20:40 + K 20:58 sonrası; önceki 111/156'ya göre fark)
| Grup | Önceki MVP (20:37) | **Yeni MVP** | Önceki hedef | **Yeni hedef** | Fark (MVP/hedef) | Not |
|---|---|---|---|---|---|---|
| Ortak | 11 | **9** | 15 | **13** | −2 / −2 | Genel cast + genel kanal yedeği kalktı (her fiilin kendi klibi var) |
| Revive | 1 | 1 | 1 | 1 | 0 | koşullu |
| Lokomosyon | 14 | 14 | 28 | 28 | 0 | |
| VUR (Zarar) | 10 | 10 | 20 | 20 | 0 | |
| AĞIR (Kuvvet) | 10 | 10 | 20 | 20 | 0 | |
| DURUŞ (Koruma, savunma duruşu) | 10 | 10 | 10 | 10 | 0 | içerik savunmaya çevrildi (§2.2.2) |
| ATIL (Hareket) | 10 | 10 | 10 | 10 | 0 | |
| BAĞ (Kontrol) | 10 | **0** | 10 | **0** | −10 / −10 | → `Buyu_Kontrol` |
| DESTEK (Şifa, Güçl., Arınd.) | 10 | **0** | 10 | **0** | −10 / −10 | → 3 büyü klibi |
| YERE (Sabit) | 10 | **7** | 10 | 10 | −3 / 0 | yalnız fiziksel Sabit; MVP'de aile başına (DÖNÜŞ/UZANMA gibi) |
| DÖNÜŞ + UZANMA | 14 | **16** | 20 | 20 | +2 / 0 | K 20:58: Top'un kendi DÖNÜŞ/UZANMA'sı (Ağır ailesinden ayrıldı) |
| **BÜYÜ** | 0 | **8** | 0 | **16** | +8 / +16 | fiil başına 1 + Mirror (§2.6); hedefte +8 iki-el varyantı (S-19) |
| Küre soğuma | 1 | **0** | 1 | **0** | −1 / −1 | silahlar.md 22:18: yavaşlama = toparlanmada yürüme ×0,5, klip yok |
| Ortak additive (dokunma) | 0 | 0 | 1 | 1 | 0 | |
| **Toplam** | **111** | **95** | **156** | **149** | **−16 / −7** | Revive yoksa **94 / 148** |

Okuma:
- MVP **111 → 95 (−16)**: −20 (BAĞ + DESTEK), −3 (YERE aile başına), −2 (genel yedekler), +8 (büyü), +2 (Top DÖNÜŞ/UZANMA, K 20:58), −1 (Küre soğuma, 22:18). İlk plana (85) göre +10.
- Hedef **156 → 149 (−7**; Küre soğuma −1 dahil): büyü iki-el varyantları (+8) hedefte gelir; YERE hedefte yine silah başına.
- Silah başına özel klip: MVP 4 fiziksel (40) + aile başına 3 biçim (21). 8 büyü klibi 10 silahın hepsinde aynıdır.
- MVP'de DÖNÜŞ/UZANMA/YERE aile başına paylaşılır: Asa, Küre ve Tılsım aynı büyücü klibini kullanır, elde farklı prop vardır.
- **Zaman:** artık kendi klibi var (`Buyu_Zaman`). "Ters oynatma" kuralı kalktı (§3 notu).
- 20:29–20:37 kararları sayıyı değiştirmedi; Yay_ATIL Aşama 3'e kaydı, ≈ +0,5 gün [tahmin].

#### 8.2 Bellek ve CPU
- **Klip belleği:** Humanoid, Optimal sıkıştırma, rotasyon hatası 0,5° (kılıç/asa 0,25°). 1 s'lik klip onlarca KB düzeyinde. 150 klip ≈ **4–10 MB** [tahmin, ölçülmedi].
- **Yalnız maçta kullanılan silahların** override asset'leri ve klipleri yüklenir (Addressables grubu silah başına), en çok 5 oyuncu × taşıdığı silahlar.
- **CPU:**
  - Katman 3 olunca Animator maliyeti artar. Hedef değişmedi: **Animator + skinning ≤ 2 ms** (5 oyuncu + boss, orta segment Android).
  - Additive katman çoğu zaman ağırlık 0'da; ölçülecek (§6.1).
- **Kemik:** ≤ 45 (Mixamo no-fingers 25 + kumaş zinciri). Değişmedi. Zıpla/Takla ek kemik istemez.
- **Culling:** değişmedi. Yerel oyuncu ve boss `Always Animate`, diğerleri `Cull Update Transforms`.
- **Animation event bütçesi:** klip başına en çok 4 event (`Trail_On/Off`, `Vfx_Socket`, `Kor_Flash`, `Charge_Ready`). K 20:42 `Ejder` event'i yalnız `Cekic_VUR`'da (orada Trail yok); diğer silüetler `impact`'i dinler (§2.2.3).

#### 8.3 Ek iş (gün, [tahmin])
| İş | Gün |
|---|---|
| ATIL ×10 (Mixamo'dan 4: dalış, geri sıçrama, kayma, leap slam; Blender 6) | 7 |
| DÖNÜŞ/UZANMA ×16 (Mixamo spin/lunge + Blender prop uyarlama; Top 2'si K 20:58) | 5,5 |
| Ortak +5 yeni (Zıpla, İniş Mixamo; Takla roll'dan; İşaret, Pas, Bedel Blender) | 2,5 |
| Büyü ×8 (K 20:40; 2 PC/DZ, 4 MX→A3, 2 A3 basit; tek el, üst gövde) | 3 |
| YERE aile klibi ×7 (silah YERE'lerinden uyarlama) | 0 (eski plandaki 10 YERE'nin yerine) |
| **Klip toplamı (MVP)** | **≈ 18** |
| *Eski plandan düşen:* BAĞ ×10, DESTEK ×10, YERE ×3, genel yedek ×2 | *≈ −6…−12 (0,25–0,5 gün/klip)* |
| `AnimPlan` çözücüsü + birim testleri (21 örnek §4.3 test vakası) | 3 |
| Dodge durum makinesi (§5.2) + hız devri bağlantısı | 2 |
| Yeni state'ler, additive katman, override binder'ın 7 yuvaya uyarlanması | 1,5 |
| `UpperBuyu` state, `BuyuAyna` Mirror parametresi, büyüde `Grip_L` IK ağırlığı 0 | 0,5 |
| **Kod toplamı** | **≈ 7** |
| `animasyon-plani.md` §2.1–2.5 ve §1.7'nin bu belgeye göre yeniden yazımı | 1 |
| **Genel** | **≈ 26** (eski plandan düşen klipler sayılırsa net ≈ 14,5–20,5) |

Hedef sete geçişte eski plandaki hedef farkına ek olarak ≈ 7 klip gelir (DÖNÜŞ/UZANMA'nın silah başına ayrılması +6, ATIL fren +1): ≈ 5 gün daha [tahmin].

---

### 9. Neyin yeniden yazılacağı (bu belge kabul edilirse)

| Belge / kod | Değişiklik | Gün |
|---|---|---|
| `animasyon-plani.md` §2.1 İlke | "12 sıfat yeni klip istemez" → sıfat biçim yuvası seçer (DÖNÜŞ, UZANMA, YERE, İşaret, Pas); fiziksel / büyü ayrımı (K 20:40) | 0,25 |
| `animasyon-plani.md` §2.3 matris | Elle eşleme kalkar, yerine §4 algoritması. Matris yalnız "kontrol tablosu" olarak kalabilir | 0,5 |
| `animasyon-plani.md` §2.4–2.5 | 7 yuva + 8 büyü klibi tablosu + yeni sayım (95/149) | 0,25 |
| `animasyon-plani.md` §1.7 | Katman 3, yeni state'ler | 0,25 |
| Oyun kodu (repo, **bu görevde dokunulmadı**) | `AnimPlanRunner`, `ActorVisual` yeni state'ler, `DodgeMotion` ikinci basış, override binder | ≈ 6,5 |

---

### 10. Sonra listesi (şimdi tasarlanmaz)

- **S-a · Kök eğrisinden hız profili:** klibin kök XZ eğrisini içe aktarmada normalize edip `MotionTemplate`'e kolaylaştırma eğrisi olarak vermek. Mesafe kuraldan, his klipten. ≈ 2 gün.
- **S-b · Playables API ile özel graph:** Animator state sayısı yönetilemez olursa. ≈ 5–8 gün.
- **S-c · Silah değiştirme kombosu:** ATIL sırasında silah değiştirip başka silahın teslimini oynatmak (H-1 izin veriyor). Dissolve + Upper teslim zaten çalışır; özel "havada değiştir" klibi ≈ 1 gün.
- **S-d · Kılıç "havaya atma" Çağırma jesti** ayrı YERE varyantı olarak. +1 klip.
- **S-e · Dodge yön klipleri** (ileri zıplama / yana takla / geri takla ayrı). +3 klip.
- **S-f · Boss'a karşı imza bitirici** (Odaklı-Hareket bitirici vuruşuna özel klip). +10 klip.
- **S-h · Kın (K 21:05 Sonra'ya):** kalça soketi + kın mesh'i + çekme/sokma jestleri. Şimdilik kınsız. ≈ 2 gün.
- **S-g · İki oyunculu kombo pozları** (Ortak yükü alındığında iki karakterin eş zamanlı jesti).

---

### 11. Açık sorular (kilitli kararlarla çakışanlar ve kullanıcı kararı gerekenler)

| # | Soru | Çakıştığı karar | Önerim | Değişirse ne yeniden yazılır, kaç gün |
|---|---|---|---|---|
| **S-1** | Dodge zıplaması alan saldırısından ya da yerden giden şeylerden (Çekiç çatlağı) kaçırır mı? | Dikey fizik tanımlı değil (KG-23); öneri "kapalı" | **Hayır**, zıplama yalnız görüntü; dodge'un kaçırma kuralı neyse o | Evet denirse: dikey durum ("havada") kural motoruna, sim'e ve alan çarpışmasına girer. `dunya-fizigi` §6 + sim + kod ≈ 4–6 gün |
| **S-2** | Dodge bir "kendini taşıma etkisi" midir (H-1 hız devri ona da işler mi)? Takla bunun üstüne kuruldu | K 15:53 metni yalnız skill'leri anıyor | **Evet**: takla ve dash zinciri ek kural istemez | Hayır denirse: takla için ayrı mesafe/hız kuralı yazılır. §5 + `DodgeMotion` ≈ 1,5 gün |
| **S-3** | Yoğun şarjı (0,35 s) silahın kendi şarjıyla (Yay germe, Çekiç kaldır-bekle; Kılıç'ta şarj yok) **toplanır mı, büyüğü mü** alınır? | Yoğun K 19:41; "toplam süre sıfatın bütçesinde kalır" (`silahlar.md`) | Büyüğü (max): bütçe kuralına uyuyor | Toplanırsa: JSON süreleri değişir, klip değişmez. Sim ayarı ≈ 0,5 gün |
| **S-4** | Güdümlü'nün ilk 3 s'si "kilitlisin": dodge da kilitli mi? | K 17:42 yalnız "yeni skill"i söylüyor | Dodge kilitli değil, ama kilidi **bozmaz** (kendini taşıma değilse) | §5.2 bir satır ≈ 0,25 gün |
| ~~S-5~~ | **Kapandı (K 20:40):** Yansıma artık `Buyu_Yansima`; karşı vuruş klibi yok, geri dönen etki yalnız VFX | — | — | — |
| **S-6** | Tılsım: "hedefte belirme, yol yok" ile "mermi varlıktır" çatışması (`silahlar.md` Açık A) | 16:52 | Animasyonu etkilemez (YERE/VUR aynı). VFX'i etkiler: `efekt-motoru` S-E2 | — |
| **S-7** | Kitap ve Küre ritmi geçici (A-19). Klipleri en sona bırakayım mı? | — | Evet, ikisinin ATIL/DÖNÜŞ klipleri sırada sonda | — |
| ~~S-8~~ | **Kapandı (K 21:05, teslim sınıfı kesin):** yakın vuruş = Yumruk, Kılıç, Çekiç, Kalkan; mermi = Yay, Top, Kitap; alan = Asa ışını, Küre konisi, Tılsım mührü. Küre yakın değil | — | Güdümlü koşusu yalnız bu dört yakın silahta | — |
| **S-9** | Büyü hücrelerinin 80'i ve ≈ 6 fiziksel hücre, metindeki jestten farklı klip oynar (§2.3). K 20:40 büyü kısmını açıkça kabul ediyor | Hücreler GameDesign'ın onaylı metni | Kabul: metin görüntü hedefi, VFX taşır. `silah-fiil-120` büyü hücrelerine "jest = büyü klibi" notu düşülmeli (GameDesign) | Hücre başına varyant istenirse: o hücreler için +1 klip ve çözücüye istisna tablosu (kullanıcının "istisna yok" kuralına aykırı). ≈ 0,5 gün/hücre |
| **S-10** | Katman sayısı 2 → 3 (additive) | Önceki plan (benim, kullanıcı kilidi değil) | 3, profillemeyle | Ret: Bedel Upper'a; 0,25 gün |
| **S-11** | Havada skill atılabilir mi (ZIPLA sırasında TESLİM)? | Kural yok | Evet, Upper katmanda; skill zıplamayı kesmez (H-1) | Hayır denirse dodge sırasında giriş kuyruğa alınır, 0,5 gün |
| ~~S-12~~ | **Kapandı (K 21:05 AS-15):** Kalkan'ın omuz hücumu ATIL'dadır, kalkan fırlatılmaz; VUR kenarla indirme | — | — | — |
| **S-13** | Hareket görünümleri (K 20:35) yalnız İşaretli-Hareket için mi, yoksa silahın bütün Hareket skilleri (ATIL) için mi? | Karar metni "İşaretli-Hareket silah görünümleri", ama aynı saatteki "Hareket yolculuğu ritmi" bütün Hareket'e yazılmış | Bütün ATIL (tek klip, §2.2). Klip sayısı aynı | Yalnız İşaretli denirse: 10 ek ATIL varyantı (İşaretli'ye özel) → MVP 96 → 106, ≈ +5–8 gün klip işi [tahmin] |
| ~~S-14~~ | **Karar verildi (K 21:05): kın kesin YOK**, Sonra listesine gitti (§10 S-h). İaido kınsız: el kabzada, kılıç alçakta; Zaman büyü klibi | — | — | — |

| ~~S-15~~ | **Kapandı (K 21:05):** Kontrol büyü fiili (`Buyu_Kontrol`) | — | — | — |
| ~~S-16~~ | **Kapandı (K 21:05):** Yansıma büyü fiili (`Buyu_Yansima`) | — | — | — |
| ~~S-17~~ | **Kapandı (K 21:05):** Zayıflatma büyü fiili (`Buyu_Zayiflatma`) | — | — | — |
| **S-18** | Büyü klibi kaç tane: fiil başına 1 + Mirror mı, el tutuşu (24) / aile (56) / silah (80) başına mı? | K 20:40 "en ucuz okunur şekilde öner" | **Fiil başına 1 + Mirror (8)**, §2.6 | Tutuş başına: +16 klip, ≈ +5–8 gün; silah başına: +72 klip, ≈ +20–35 gün [tahmin] |
| **S-19** | İki elli silahlarda (Çekiç, Top, Asa) büyü süresince silah tek elde: Top tek elde kötü görünür mü? | K 20:58 kısmen çözdü: Çekiç ve Asa jesti silahla yapar, Top göğüs önünde namlu ileri taşınır | MVP'de tek el + göğüs önünde; cihazda hâlâ kötüyse hedefte 8 iki-el varyantı | +8 klip, ≈ 3 gün [tahmin] |
| **S-20** | Sabit fiziksel fiilde YERE'yi MVP'de aile başına (7) paylaşmak; büyü fiilinde Sabit'in yapısı yalnız VFX | K 20:40 "sadeleştir" (öneri benim) | Kabul | Silah başına istenirse +3 klip MVP, ≈ 1 gün |
| **S-21** | **Top namlu kalkışı kuyruğu** (`Top_BuyuAtis`, `eslesme-denetimi` Ö6): büyü jestinin ardından Upper'da 0,25 s namlu kalkışı + hafif geri tepme. Top'un 7 "mermi atar" büyü hücresini K'den T'ye çıkarır | K 20:40 "tek ortak hareket kullanılmayacak" ile sınırda: jestler ayrı, kuyruk 8 fiilde ortak | **Açık, eklenmedi** (K 20:58). Önerim: cihaz testinde Top büyüleri zayıf okunursa ekle | +1 klip, ≈ 0,5 gün |

---

### 12. Uygulama sırası (öneri)
1. §4 çözücüsünün sözde kodu kural motoruyla aynı repoda birim testi olarak (§4.3'ün 21 satırı = 21 test).
2. Ortak set: Zıpla, Takla, İniş, dodge durum makinesi. Kullanıcının istediği kombo en erken görünür.
3. Yakın silahların ATIL'ı ve imzaları (Kalkan, Kılıç, Çekiç, Yumruk).
4. Savunma duruşları (Kılıç, Asa, Küre önce: içerik değişti, §2.2.2).
5. 8 büyü klibi (tek seferde, Mirror testiyle).
6. DÖNÜŞ/UZANMA/YERE aile klipleri.
7. Uzak silahlar; en son Kitap ve Küre (A-19).

---

<a id="b7"></a>
