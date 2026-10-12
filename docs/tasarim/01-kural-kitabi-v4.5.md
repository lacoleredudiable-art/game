# Bölüm 1: Kural kitabı v4.5

*Kaynak: `karakterler/v4/kural-kitabi-v4.md`*

## Kural Kitabı v4: 12 fiil, 12 sıfat, 10 silah, 144 kombo, 1440 skill

*Sürüm 4.5, 9 Ekim 2026 ~22:40 (UTC+3): K 22:15/22:17 işlendi (1.18 yeni, Bölüm 5 hücreleri, 4.2 Yay/Tılsım), yedek `kural-kitabi-v4.md.bak-2233`. Önceki: sürüm 4.4, ~21:17. K 21:05–21:17b kararları işlendi. Yedek: `kural-kitabi-v4.md.bak`. Birleştiren: A (executor). Bu dosya `v3-kural-kitabi`nin yerine geçer.*

*4.4'te değişenler (K 21:05–21:17b): kın yok (Sonra); Kılıç/Kalkan VUR; teslim sınıfı; mekanik kanun kalıntıları; yol alan beden genel; Yansıma dosta (basılı tut); sıfat iç ölçü menzille kısılır; toparlanma yürüme çarpanı ritim; yolculuk hızları; yaratık canı; yedek hâl yok + menzile yürü; onaylı hücreler. Kalan çelişkiler `acik-sorular.md`'de.*

*4.3'te değişenler: Bölüm 4 baştan (silah = menzil + ritim + tarz, mekanik silah kanunu kaldırıldı, ritim tablosu, yolculuk/Hareket, Tılsım, Asa duruşu, Kitap), Bölüm 6 baştan (`silah-fiil-120.md` 20:36 aynen), 1.9 ön süre adımı, 1.16 Tılsım notu, yeni 1.17 (üretim yöntemi), 3.1 kalkan sıfatlar, 3.2 Yoğun şarjı, yeni Bölüm 8 (animasyon ve ejderha görünümü), 9 (Sonra), 10 (Kapsama kontrolü).*

*Ek 2026-10-09 18:4x: Kullanıcı kararları K 18:39 (Tılsım ritmi) ve K 18:42 (Odaklı-Hareket, Yayılan merkezi) işlendi; ilgili satırlar `K 18:39/18:42` etiketlidir. SIM önerileri (K1…K11, S1…S2) kör test için geri alındı (kitapta yok). Yedekler: `v4/sim/yedek/`.*

**Sayılar:** Bu kitaptaki tüm süreler, metreler, yüzdeler ve çarpanlar **başlangıç değeridir**. Kodda her biri ayrı bir ayar olarak durur; test ve denge aşamasında kod değiştirilmeden ayarlanır. Onay kuralın kendisine verilir, sayıya değil.

**Kaynaklar (yalnız bunlar):**
- `karakterler/v3-kararlar.md`: onaylı kararlar. Çatışmada en yeni kayıt kazanır (sıra: 21:17b > 21:17 > 21:15 > 21:13 > 21:12 > 21:11 > 21:10b > 21:10 > 21:09 > 21:07 > 21:05 > 20:42 > 20:37 > 20:35 > 20:29 > 19:48 > 19:41 > 18:42 > 18:39 > 17:44 > 17:42 > 17:41 > 17:40 > 17:36 > 17:35 > 17:33 > 17:28 > 17:27 > 17:21 > 17:15 > 17:05 > 16:52 > 16:49 > 16:40 > 16:36 > 15:55 > 15:53 > 15:51 > 15:47 > 15:46 > 15:41 > "9 Ekim öğleden sonra" > 12:17 > 11:46 > 11:17 > 11:07–11:13 > 07.10).
- `karakterler/v4/`: run-01 … run-12, `silahlar.md` ve `silah-fiil-120.md` (20:36 sürümleri), `dunya-fizigi.md`, `animasyon-kurallari.md`, `efekt-motoru.md`. `fizik-oneriler.md` yalnız öneri olarak kullanıldı.
- **Sohbet (ana ajan konuşması):** yalnız görünür kısımdaki kullanıcı mesajları (t716u, t717u, t718u, t721u) ve GameDesign'ın lore önerisi. Sohbetin önceki kısmı yalnız kesik bir özet olarak görüldü; orada olup bu dosyalarda olmayan onaylar taranamadı (→ AS-34).
- `karakterler/run-ajanlari/BRIEF.md`.
- Yalnız hücre tabanı için: BRIEF'in "onaylı tablo, 144 hücre" dediği v3 tablosu. Bir hücrenin metni v3-kararlar'da yoksa ve yeni bir karar onu değiştirmiyorsa metin oradan aynen alındı (**T3**). `arsiv/` klasörünün başka hiçbir dosyası kullanılmadı.

**Etiketler**

| Etiket | Anlamı |
|---|---|
| **K** | v3-kararlar'daki onaylı metin. Tırnak içindeki metin aynen kopyalandı, değiştirilmedi. |
| **T3** | v3 tablosundaki metin, aynen alındı. Kararlarda bu hücrenin satır metni yok, sonraki bir karar da onu değiştirmiyor. |
| **V4** | 9 Ekim kararlarından biri eski metni geçersiz kıldı. Hücre o karara göre yeniden yazıldı (kaynak: ilgili rün dosyası). **Kullanıcı onayı bekler.** |
| **Ölçü** | Onaylı metnin ölçülebilir hâli. Sıfatın biçim sayılarından gelir; yeni karar değildir. |
| [test] | Başlangıç sayısı, testte değişir. |
| ÖNERİ | Karar değil. |
| (zayıf) | Çalışıyor ama fiili pek değiştirmiyor. Sıfat başına en çok 2 tane olabilir. |
| (sınırda) | Zayıf sayılmadı, ama zayıfa yakın. |
| → A-n | Bölüm 7'deki açık soru. |

**Birleştirme ilkesi:** Onaylı bir metin yalnız daha yeni bir karar onu açıkça geçersiz kıldığında değişti. Bir rün botu onaylı metni karar olmadan yeniden yazdıysa onaylı metin kaldı, çakışma `v4-degisiklikler.md`'ye yazıldı.

---

### 1. Dünya kuralları

Bütün 144 kombo bu kurallardan çıkar. Hiçbir kural tek bir skill için yazılmaz. K (07.10): "Kural neyi söylüyorsa o olur; trol ya da tuhaf sonuçlar kabul (1440 skill, her şey mükemmel olamaz). İstisna yazılmaz."

#### 1.1 Kombo, çizim, hedef seçme
- **Kombo = fiil + sıfat.** K (07.10): "12 fiil TEK BAŞINA KULLANILMAZ; her kombo fiil+sıfattır. Fiilin varsayılan yönü, hedefi ya da kimin hareket ettiği yoktur; bunları sıfat belirler."
- **Tek çizim.** K: "Kombo: tek çizim; parmak kalkınca biter (M-14 aynen). Dokun-kaldır-dokun kombo DEĞİL"
- **Çizim sırası.** K (9 Ekim): "Çizim sırası: önce fiil, sonra sıfat. Ejderha dili flaşı çizim sırasıyla (ör. `SHEV-ZUL`), Türkçe ad doğal sırada." Örnek: Yoğun Zarar = çizim `AK` → `VORN`, flaş `AK-VORN`. Çizilen rün Ejderha Dili harfine döner (görünüm, 8.4). *(Eski "Kombo = Sıfat + Fiil" sırası geçersizdir; `BRIEF.md` hâlâ eski sırayı yazıyor → AS-19.)*
- **Kombo numarası:** fiil rünü - sıfat rünü (ör. İşaretli Şifa = 2-9).
- **Hedef seçme.** K (07.10): "tüm kombolarda çizim bitince parmak kalkmazsa kısa duraklamadan (~0,2 s) sonra hedef seçme başlar; parmak hedefe kaydırılır, kaldırınca o hedefe gider. Hemen kaldırırsan otomatik hedef (sıfatın varsayılanı). Açı/nişan yok, M-8 korunur."
- **Hedef türleri.** K (07.10): "basılı tutarken canlı, yapı ya da boş nokta seçilebilir; skill oraya gider, orada ne bulursa ona işler"

#### 1.2 Roller ve kime işler
K (9 Ekim): "Rol bölünmesi (4–4–4): Saldırı = Zarar, Çağırma, Zayıflatma, Hareket (Hareket yalnız kullanıcıyı taşır). Tank = Koruma (araya girer, kalkan vermez), Yansıma, Kontrol (düşmanı kullanıcıya bağlar), Kuvvet (hep kullanıcıdan dışarı iter). Destek = Şifa, Güçlendirme, Arındırma (yalnız dosttan zararlı siler), Zaman. Silahlardan rol kaldırıldı." **Not (K 16:49):** "araya girer" geçersiz; Koruma yalnız kullanıcıyı korur, kalkan vermez (tablo satırı ve 2.1 güncel).

| Fiil | Rol | Kime işler | Otomatik hedefte sıfatın hangi kolu seçilir (türetildi, → A-5) |
|---|---|---|---|
| Zarar, Zayıflatma, Çağırma | Saldırı | Yalnız düşman | Düşman kolu (genelde en yakın düşman) |
| Hareket | Saldırı | Yalnız sen taşınırsın (tek ek: İşaretli-Hareket'te işaretli arkadaş, 15:55) | Gidilecek yer için düşman kolu |
| Kontrol, Kuvvet | Tank | Yalnız düşman | Düşman kolu |
| Koruma | Tank | Yalnız sen | Yararlı kolu (sen); dost seçilmez, seçilirse skill boşa gider |
| Yansıma | Tank | Varsayılan sen; basılı tutup sürükleyince seçilen dost (K 21:09). Odaklı-Yansıma kendiliğinden en çok hasar alan dostu seçer. Koruma yalnız sen (değişmedi). | Yararlı kolu (sen); dosta basılı tutunca dost |
| Şifa, Güçlendirme, Arındırma, Zaman | Destek | Yalnız dost ve sen | Yararlı kolu (sen ya da en yakın dost) |

- Düşmana işlemeyen bir fiille düşman seçersen ya da dosta işlemeyen bir fiille dost seçersen skill boşa gider. İstisna yok.
- Dost hasarı yok (M-15). K (F1): "Dost yapıya zarar yok."
- **K (21:15) Yedek hâl yok:** skillin koşulu yoksa (geçerli hedef yok, işaret yok, dost skill atmadı, düşman saldırmadı) skill boşa gider; oyuncu bilinçli atmalı.
- **K (21:17):** Hedef var ama menzil dışındaysa karakter menziline kadar yürür, sonra atar (saldırı ve destek aynı). Skill yalnız şu hallerde boşa gider: geçerli hedef hiç yoksa ya da süresi dolduysa (örn. Ortak 5 s, Tetikli 5 s). Iska/boşa gitme yalnız skillshot'larda (İşaretli nokta, Çizgisel hat, mermi silahları; Sabit yanlış yere dikilince orada durur, boşa gitmez).

#### 1.3 Menzil
- K (15:41): "Menzili silah belirler (yumruktan uzun menzil inandırıcı değil). Yön, hedef, alan, süre, biçim sıfatta kalır; sıfat şekli silahın menzili içinde çizer."
- Silah menzilleri Bölüm 4'te (Yumruk 3 m … Top 18 m) [test].
- **Dost etkisi:** en çok min(silah menzili, 6 m) (`silahlar.md`, 9 Ekim 15:42 kullanıcı kararları).
- **Hareket:** silah menzili, en fazla yer değiştirmedir (`silahlar.md`, 15:42). v3'teki `dash_distance_m` 5 m kalktı.
- **Şeklin sınırı (K 21:10):** skillin çıktığı noktadan (sen ya da işaret) etkinin ulaştığı en uzak nokta silah menzilini aşamaz. Bir sıfatın v3 sayısı (Çizgisel hattı, Yayılan halkası, İşaretli zinciri) silah menzilinden büyükse silah menzilinde kesilir. **Sıfatın iç ölçüleri** (halka yarıçapı, sekme mesafesi vb.) de silah menziliyle kısılır (örn. Yumruk 3 m'de Yayılan halkası 3 m). Kodda ayar bayrağı (`menzil_ic_olcuyu_keser`).

#### 1.4 Yığılma ve sınırlar
- K (20:13): "Aynı tür etki aynı hedefe 3 s içinde azalarak işler: 2. ×0,5, 3. ×0,25 (testte netleşir). Güçlendirme en çok iki kaynak: bir arkadaştan + bir kendinden; fazlası boşa gider."
- K (11:46-3): "Bir skillin kendi tikleri/değmeleri azalma kuralına girmez (azalma yalnız ayrı skiller arasında)."
- **K (16:36): Hasar 3 s azalma kuralına GİRMEZ (kesin). Azalma yalnız etkilere uygulanır.** A-1 kapandı.
- Oranlar gelen etkinin tamamını aşamaz; yansıma oranı en çok 1 (T3).
- Takım 5 kişidir (K 20:13).

#### 1.5 Fedakarlık genel kuralı
K (20:16): "Canının %30'u bedel. Etki yığılma kuralına ve boss irade halkasına takılmaz, hep ×1 işler (ör. Kontrol+Fedakarlık boss iradesini kırar). Aynı hedefe 3 s'de tek Fedakarlık, aynı boss'a 60 s'de bir. Tek başına vuruşta faydası yok (kural böyle)."
K (11:17): "Fedakarlık bedeli MEVCUT canın %30u." K (11:46-2): "Fedakarlık boss 60 s sınırı KİŞİ BAŞI."

#### 1.6 Boss: irade halkası, denge, kontrol
- Boss'un can barının altında takımın ortak irade halkası var. Halka **Açık** iken ilk tam kontrol işler ve hak harcanır. Sonra halka 45 s **Dirençli** olur; Dirençli iken tam kontrol uygulanmaz, ×1,5 denge hasarına döner. Sendelemede boss her şeyi tam yer (T3).
- K (21:48): "Boss: sersemletme/kökleme/korkutma irade halkasından; susturma/yavaşlatma her zaman ama yarı güç."
- K (11:17): "Boss susturması saldırıyı yalnız irade halkası açıkken iptal eder."
- K (11:46-1): "Küçük kontrol etkileri (kısa sersemletme, Yörüngeli değmesi vb.) boss irade hakkını harcamaz, onun yerine denge hasarı verir."
- Boss ağırdır: itilmez, çekilmez; onun yerine denge hasarı alır (T3).
- Zaman artık yalnız dosta işlediği için boss'a işlemez (9 Ekim rol kuralından türetildi). Eski "Zaman'ın durdurma ve geri sarma etkileri boss'ta irade halkasına girer" kuralının konusu kalmadı.

#### 1.7 Kontrol: çeşit ve tasma
- **Çeşit.** K (21:48): "Çeşidi sıfat değil hedefin o anki işi belirler: saldırı/büyü hazırlığı → susturma (iptal), yürüme → kökleme, sana/dosta hücum → korkutma (kaçar), hızlanmış/atılıyor → yavaşlatma, boşta → kısa sersemletme. Sıfatlar kime/alan/süre/biçim belirler." Silah çeşidi seçmez. Dondurma yoktur.
- Çeşitlerin ölçüsü (T3): susturma = süre boyunca skill ve saldırı yok, yürüyebilir; kökleme = yürüyemez, saldırabilir; korkutma = hücum ettiği kişiden uzağa kaçar, saldıramaz; yavaşlatma = yürüme ve saldırı hızı −%50; sersemletme = sürenin yarısı kadar hiçbir şey yapamaz. Öncelik (iki iş aynı anda): saldırı/büyü > atılma/hızlanmış > hücum > yürüme > boşta (ÖNERİ).
- **Tasma.** K (15:46): "Kontrol bağı = tasma: Kontrol işi keser + 1,5 s [test] bağ; uzunluk kuruluş anındaki mesafe, en çok silah menzili; geren durur (düşman kaçamaz, tank uzaklaşamaz). Dikkat çekme (taunt) yok."
- **Tasmanın ucu (A 19:46, K 15:51'e dayanarak):** tasmanın öbür ucu skilli dünyaya çıkaran çapalı gövdedir. Sabit'in tuzağında uç **yapıdır** (yapıya bağlı boss K 15:51 gereği çivilenir; yapı yıkılınca tasma aynı karede kopar, 1.12). Diğer bütün durumlarda (işaret, sekme, arkadaşın skilli dahil) tasma Kontrol'ü kullanan oyuncuya kurulur; işaret ve zincir yapı değildir (1.10). Sıfat yalnız kime, nerede, ne kadar süre olacağını söyler. *(eski: 'Tasma her zaman Kontrol'ü kullanan oyuncuya kurulur. Tuzaktan, işaretten, sekmeden ya da arkadaşın skillinden gelse de öyle')*
- Sıfatın süre çarpanı hem çeşidin hem tasmanın süresine işler (Yoğun yarı → 0,75 s). (türetildi)
- Korkutma ve tasma birlikte: düşman kaçar ama tasma sınırında durur. (kuralın sonucu)
- **Boss tasması.** K (15:51): "Boss bağlama: çapalıya (duvar/yapı/zemin işareti) bağlanırsa çivilenir; irade açıkken 4 s [test], dirençliyken 1,5× denge hasarı. Oyuncuya bağlıysa bağı geren boss oyuncuyu sürükler; boss'u kıpırdatmayan bağ irade harcamaz."

#### 1.8 Ağırlık, itme, çekim
- K (karar 8): "Hareket: kimin gideceğini ağırlık seçer (hafif ağıra gider, çapalıya hep sen gidersin)" K (07.10): "Ağırlık dünya kuralı olarak kalır, fiil onu kendiliğinden tetiklemez."
- Ağırlık sınıfları: **Hafif** (oyuncu, küçük düşman, mermi) · **Ağır** (boss) · **Çapalı** (zemin, Sabit yapıları, zemine konmuş işaret). Hafif olan mesafenin tamamını kat eder, ağır ya da çapalı olan 0 m kıpırdar (`dunya-fizigi.md` §2 Ağırlık → [Bölüm 2 Dünya fiziği](#b2) §2).
- **İtme:** her zaman Kuvvet'i kullanan oyuncudan dışa doğrudur: yön, oyuncunun o anki konumundan hedefe giden düz çizginin devamıdır. Hedef 0,2 m'den [test] yakınsa oyuncunun baktığı yön kullanılır. Taban 3 m [test]. Yalnız hafif itilir. Kuvvet çekmez. Sıfatın yönü skillin şeklini verir (halka dışa açılır, hat ileri uzanır); itme yönünü vermez.
- **Çekim alanı yok.** Mıknatıslı (MAGH) kalktı (K 17:21); sıfat biçimli çekim alanı yoktur. Ağırlık yasasının "hafif ağıra gider" sonucu (İşaretli zinciri, Kontrol tasması) duruyor.
- **Öncelik:** tutma > süreli etki > itme (`dunya-fizigi.md` §5 P-6 → [Bölüm 2 Dünya fiziği](#b2) §5). Eski "çekme" basamağı Mıknatıslı ile birlikte düştü (kayıtta ayrı satır yoktu).
- **Çarpma:** K (F5): "çarpma hasarı dünya kuralından". Birimi yok → A-17.

#### 1.9 Kendini taşıma, ışınlanma, hız devri
- Hareket yalnız kullanıcıyı taşır (K 9 Ekim). Düşmana atılmak, senin hareket etmen demektir.
- **Işınlanma yalnız Çizgisel'den.** K (15:55): "Işınlanma (Minato tarzı) Çizgisel'in işidir: Çizgisel-Hareket oyuncuyu görünmez çizginin ucuna bir anda koyar." Başka hiçbir kombo ve hiçbir silah ışınlamaz. Portal 3-4'lü kombolara kaldı. K (15:55): "3 rünlü kombolarda İşaretli + Çizgisel = işarete ışınlanma (şimdi tasarlanmayacak, not)."
- **Yol alarak giden hareket:** yol boyunca görünür. Yolculuk hızı ve yolda skill atılıp atılamayacağı silah ritminden gelir (K 20:35; tablo 4.3). **K (21:07) Yol alan beden (bütün silahlarda aynı):** Hareket/yolculuk sırasında beden hasar alır, mermiye araya girer, duvar/yapı onu durdurur ve hızı 0 olur. Silaha özel değil.
- **Silahın ön süre adımı Hareket değildir:** Kalkan'ın ağır adımı (ön süre 0,30 s, `silahlar.md` "Ağır adım (Hareket sayılmaz)") silahın ritmidir. Silah menzilinin içinde kalır, Hareket sayılmaz. Kılıç'ın eski "ileri atılış" adımı kalktı: Kılıç Zarar yerinde iaido'dur (`silah-fiil-120.md` 20:36). Omuz hücumu artık Kalkan'ın Hareket görünümüdür (en çok 3 m, K 20:35). Koruma'nın dosta varma adımı yoktur (K 16:49).
- K (15:53): "Hareket kesme + hız devri: yalnız yeni bir kendini taşıma etkisi süren hareketi keser; eski hızın yarısı [test] yeni yöne aktarılır, toplam en çok 1,5× temel hız. Işınlanma ve Zaman dönüşü hız almaz/aktarmaz."

#### 1.10 İşaret ve zincir (İşaretli)
- K (9 Ekim): "İşaretli kuralı: işareti yalnız İşaretli-Arındırma koyar (fiil işlemez), kişi başı en çok 4 işaret, 5. en eskiyi sessizce siler. Diğer 11 İşaretli kombo işareti kullanır: tek işaret seçiliyse skill işaretten çıkar (işaret yoksa oyuncudan); … arkadaşın işareti de kullanılabilir. Hafif olan ağıra gider (ağırlık kuralı). Görsel: işaret küçük kor rünü, … kullanılacak işaretten hedefe önizleme çizgisi; yakın dövüş hareketi oyuncunun yerinde, kor yankısı işarette."
- K (15:47): "İşaret menzili: koyarken silah menzili (dosta min(silah,6 m)); kullanırken 25 m [test] içindeki takım işaretleri; skill işaretten silah menziliyle çıkar."
- 9-12 master onayı, sayılar [test]: işaret 20 s yaşar, kullanılınca silinmez; dosta, düşmana (boss dahil) ve zemine konabilir; canlıya konan işaret onunla yürür. Otomatik seçim: sana en yakın işaret, işaret yoksa skill senden çıkar.
- **K (17:33) Zincir:** Eski "iki işaret arası tek bağ" kalktı. İşaretler **konuluş sırasıyla** zincirlenir (1→2→3→4). Skill zincirin herhangi bir yerine değen her uygun hedefe bir kez işler, hedef başına ×0,85, en çok 5 hedef. Zincir halkası: 1 m genişlik, iki komşu işaret arası en çok 12 m, zincir 4 s kalır; değmeler azalmaya girmez. Görsel: ince kor çizgisi zincir boyunca.
- **K (17:35) Zincir sahibi:** Her oyuncunun zinciri **yalnız kendi** işaretlerini bağlar (en çok 5 oyuncu × 4 işaret = 5 ayrı zincir). Takım 25 m içindeki bütün işaretleri tek tek kullanabilir (skill bir işaretten çıkar); ama zincir yalnız aynı kişinin koyduğu işaretleri birbirine bağlar. Bağ/zincir kullanan skill, skill sahibinin en yakınındaki zinciri seçer; basılı tutarsan başka oyuncunun zincirini seçebilirsin. Her oyuncunun işareti kendi renginde görünür.
- İşaret seçme jesti (ÖNERİ): tek işaret = dokun; zincir kullan = zincirin bir halkasına dokun (otomatik en yakın kendi zincirin).
- İşaret, zincir, Tetikli'nin işareti, Ortak'ın yükü, Güdümlü'nün kilidi yapı değildir. Canları yoktur, yapı sınırına sayılmazlar.

#### 1.11 Zaman kaydı
- K (9 Ekim): "Zaman tek işe indi: hedef kaydın alındığı ana döner (can ve konum; etkilere dokunmaz). Kaydın ne zaman alındığını sıfat belirler (varsayılan 3 s önce; Sabit'te direk dikildiği an; İşaretli'de işaret konduğu an). Dondurma ve ileri sarma kalktı. Tetikli genel kuralı geçerli (dostun bir sonraki hamlesinde)."
- K (11:46-4): "Ölü dostu YALNIZ kayıt direği (Sabit+Zaman) diriltir; diğer geri sarmalar ölüyü hedef alamaz."
- **Sıfat çarpanları Zaman'da** (K 17:28; Rün 12 ÖNERİ-Ç uygulandı): konum her zaman tam döner. Değer çarpanı can farkına işler: dönen can = (kayıttaki can − şimdiki can) × çarpan; kayıttaki canı aşamaz. Süre çarpanı kaydın geriliğine işler: "süre yarı" = 1,5 s önce.
- Zaman dönüşü hız almaz ve aktarmaz, varışta hız 0'dır (15:53). Zaman dönüşü ışınlanma sayılmaz; kararda ayrı geçer.

#### 1.12 Yapılar (yalnız Sabit)
- K (karar 5): "her skill gövdesini yapıya çevirir (canı var, çakılı, saplanır, kanca atılır, yıkılır)". Aynı karardaki "silah şekli" ve "Savunma tek başına canlıya kalkan" ifadeleri 9 Ekim silah ve rol kararlarıyla düştü. Duvarı ve yapıyı yalnız Sabit diker; Koruma da dahil hiçbir fiil tek başına duvar ya da kalkan kurmaz.
- Yapının işini fiil, dokunana verdiği durumu element belirler. Şekli silah değil Sabit verir (9 Ekim silah kuralı; S4'teki "şekil=silah" bununla düştü). Gövde ÖNERİ [test]: **duvar** (Koruma, Yansıma) 4 m en, 2 m boy, 0,5 m kalınlık; **direk** (diğer 10 fiil) 1 m taban, 3 m iş alanı.
- K (11:17): "Yapı sınırı: kişi başı 5, takım başı 25." 6. yapıda oyuncunun en eskisi söner; takım 25'i aşarsa en çok yapısı olanın en eskisi söner (T3).
- Yapı ömrü 6 s [test], can [test]. Yapı yıkılınca üstündeki işaret düşer ve bağ aynı karede kopar.

#### 1.13 Element
K (karar 6): "Element: maç öncesi build'de seçilir, maç içinde değişmez (savaş içi radyal menü kalkar); her element tek küçük durum ekler (veri: yanık/yenilenme/hız/kalkan/arınma/zayıflatma); yapıda dokunana aynı durum. Element tepkileri sonra."

#### 1.14 Dünyanın zamanı
Dünyanın zamanı tektir, kimse için yavaşlamaz: slow-mo yok, genel zaman ölçeği yok. Hitstop yalnız görüntüdedir. Yavaşlatma ve hız etkileri tek bir canlının yürüme ve saldırı hızını değiştirir, zamanı değil (`dunya-fizigi.md` §12.1 → [Bölüm 2 Dünya fiziği](#b2) §12.1).

#### 1.15 Sıfatların uyması gereken kurallar
- K (07.10, M-13): "her sıfatın TEK ana sorusu var (karakteri) ama 5 sorunun hepsini cevaplar: otomatik hedef, yön, alan, süre, biçim. İki sıfat aynı ana soruya aynı cevabı veremez."
- K (20:22): "Bir sıfatın 12 fiilin en az 10'unda çalışması yeter." Yani bir sıfatta en çok 2 zayıf hücre olabilir.
- Sıfat skillin şeklini ya da davranışını değiştirmeli; yalnız "daha çok" yapan sıfat olmaz (BRIEF; Yükselen, Kopya, Akan bu yüzden reddedildi).

#### 1.16 Mermi ve alan (K 16:52)
- **Mermi bir varlıktır:** uçar; ilk değdiği canlıya ya da yapıya çarpar. Sabit duvar tutar; Yansıma geri yollar; araya giren (gövdesiyle) vuruşu alır. Ayrıntı `dunya-fizigi.md` §4.2 ([Bölüm 2 Dünya fiziği](#b2) §4.2).
- **Alan varlık değildir:** yerde bir bölgedir; içindeki herkese işler; duvar tutmaz; araya girilemez. Alan hasarı yakın vuruş gibi işler; yönü alan merkezinden hedefe doğrudur. Koruma'nın şekli bu yöne göre tutar (Çizgisel önü merkeze dönükse tam, arkası dönükse tutmaz; Yayılan %50; Yoğun ilk anı tam durdurur). Dostu alandan korumanın yolu alanı kuranı kesmektir (Kontrol/Kuvvet).
- **K (21:05) Teslim sınıfı:** Yumruk, Kılıç, Çekiç, Kalkan = yakın vuruş; Yay, Top, Kitap = mermi; Asa ışını (anlık hat), Küre konisi, Tılsım gecikmeli mühür = alan. Sıfat şekli bunun üstüne biner (Yayılan halka her zaman alan). Tılsım'ın mührü yol almaz, hedefin ayağına düşer ve 0,5 s sonra açılır (K 18:39).

#### 1.17 Üretim yöntemi: motor kurallardan üretir (K 17:44)
- K (17:44), aynen: "Simülasyona skiller VERİLMEZ, yalnız kurallar (fiil, sıfat, silah, dünya fiziği) verilir. Motor 1440 skilli kurallardan üretir."
- K (17:44), aynen: "Onaylı hücreler gizli hedef listesidir; motor çıktısı hedefle karşılaştırılır. Uymayan yerde kural ayrıntılandırılır (istisna yok), tekrar üretilir; hedefe ulaşana ya da yaklaşana kadar döngü."
- K (17:44), aynen: "Belli aşamalarda hedeften daha iyi çıkan skiller kullanıcıya ayrıca sunulur."
- K (19:41 kayıt satırı), aynen: "Kör test sürprizleri A kararıyla kabul (motor daha iyi, otomatik hedef): 3-11, 3-12, 4-9, 6-6, 10-6, 10-8, 11-1 motor okuması geçerli."
- Kod için anlamı: Bölüm 5'in 144 hücresi elle kodlanmaz. Kural motoru fiil + sıfat + silah + dünya kuralından standart komut listesi üretir (`fizik-komutlari.md`). Bölüm 5 test hedefidir.

---

#### 1.18 K 22:15 ve K 22:17 kararları (aynen, `v3-kararlar.md`)
Bu bölüm kullanıcı kaydının kelimesi kelimesine kopyasıdır; çelişen her eski satırın yerine geçer.

- Taban hasar silahın toplam ritim süresiyle ölçeklenir (saniyedeki hasar eşit, yavaş silah sert vurur). Silah kanunu değil, ritmin sonucu.
- Tılsım toparlanması 0,45 s.
- Yansıma taban oranı 0,85 (Yoğun ×1,35, tavan 1). ŞART: boss'un tek atan (öldürücü) büyük vuruşunda yansıtma sınırlı; dönen hasar tavanı ayarlanabilir (başlangıç: boss azami canının %5'i). Dünya fiziğindeki eski "hasar azalmaz" satırı buna göre değişti (Ya-1: ×0,85).
- Alan tiki: Koruma her tiki ayrı darbe sayar; itme gibi etkiler alanın yalnız ilk değişinde işler. Yumruk serisi ve boss atış dizisi tek darbe.
- Canlılar birbirinin içinden geçemez; kenarda durur (Yoğun-Hareket kenarda frenler).
- Gövde yarıçapları (ayarlanabilir başlangıç): oyuncu 0,35 m, boss 2,5 m, yaratık 0,5 m, yapı 1 m. Mesafeler gövde kenarından ölçülür.
- Güdümlü sigortaları: hedef ölür/kaybolursa kilit biter; bütün Güdümlü kilitleri en çok 8 s (K 22:53). Temel vuruş ve dodge kilidi bozmaz.
- Mana/cooldown: cooldown skill işlediği an başlar; boşa giden skill mana harcar, cooldown'ın yarısını yer; Ortak/Tetikli'de cooldown atıldığı an başlar; Fedakarlık mana ödemez.
- Yay germe otomatik 0,35 s, erken bırakma yok.
- Azalma: ×0,5 etkinin kendi ölçüsüne (süre/mesafe/yüzde), aynı tür = aynı fiil; Güçlendirme ile Zayıflatma toplanır.
- Fedakarlık-Güçlendirme 2 kaynak sınırına uyar (8-8 düzeltilir).
- Sonra: Fedakarlık boss'ta dakikada 5 kez riski.
- K 22:17: Kuvvet itme mesafesi silah menziliyle KISALMAZ; menzil yalnız erişimi kısaltır, itme mesafesi sıfattan gelir. 5-1 buna göre düzeltilir (21:17b'deki "ölçü menzille kısılır" notu 5-1 için geçersiz). İstisna yok.

- **Uygulama (K 22:15'in sonucu):** Ortak ve Tetikli'de yedek hâl yoktur, süre dolunca skill boşa gider (bütün *-7 ve *-11 hücreleri buna göre). Sıfat iç ölçüleri (sekme min(6 m, menzil), halka min(4 m, menzil), kule min(6 m, menzil)) menzille kısılır (K 21:10); **itme mesafesi kısılmaz** (K 22:17). Yörüngeli yarıçapı = taşıyanın gövde yarıçapı + 2 m. Hasar çarpanları ve Tılsım 0,45 s: `silahlar.md` "Hasar ritimle ölçeklenir" tablosu (Kılıç 0,73 · Küre 0,73 · Tılsım 0,82 · Asa 0,91 · Yumruk 1,00 · Yay 1,00 · Kitap 1,27 · Kalkan 1,36 · Top 1,36 · Çekiç 1,91).

### 2. On iki fiil

Fiil yalnız **ne olduğunu** söyler. Kime, nereye, ne kadar süreyle ve hangi şekilde olduğunu sıfat söyler. Ne kadar uzağa ulaştığını silah söyler.

| Rün | Fiil | English | Flaş | Rol | Ne yapar | Taban [test] |
|---|---|---|---|---|---|---|
| 1 | Zarar | Rend | `AK` | Saldırı | K (F1): "tek işi can ya da dayanıklılık düşürmek. İtme Patlama'da, durdurma Kontrol'de. Dost yapıya zarar yok (M-15)." | `base_damage`, denge `base_poise` |
| 2 | Şifa | Mend | `SOL` | Destek | K (F2): "tek işi dost canı ya da dost yapı dayanıklılığını artırmak; düşmana etkisiz. Fazla dolum yalnız Yoğun ile kalkana döner." | `base_heal` |
| 3 | Hareket | Shift | `SHEV` | Saldırı | Yalnız kullanıcıyı taşır. Kim gidecek, ağırlık kuralı seçer; şeklini sıfat belirler. | En çok silah menzili kadar yer değiştirme |
| 4 | Koruma | Ward | `THAL` | Tank | K (16:49): yalnız kullanıcıyı korur; kalkan/can yok; kısa savunma duruşu. Ayrıntısı 2.1'de. | Sıfata göre kısa duruş |
| 5 | Kuvvet | Impact | `KRAG` | Tank | K (F5): "tek işi itmek ve denge bozmak; can hasarı yok; çarpma hasarı dünya kuralından". K (9 Ekim): "hep kullanıcıdan dışarı iter". Çekme yok. | İtme 3 m, `base_poise` |
| 6 | Kontrol | Bind | `NETH` | Tank | Hedefin o anki işini keser (çeşit kuralı 1.7) ve onu sana tasmalar. | Çeşit 1,5 s; yavaşlatma %50; tasma 1,5 s |
| 7 | Zayıflatma | Wither | `GHUL` | Saldırı | K (F7): "hedefin dünyaya verdiği her etkiyi zayıflatır (zarar, kalkan, şifa). Silah seçmez; hız Zaman'ın işi." | −%20, 4 s |
| 8 | Güçlendirme | Kindle | `DRAK` | Destek | K (F8): "dostun dünyaya verdiği her etkiyi güçlendirir (zarar, kalkan, şifa). Silah seçmez; düşmana etkisiz." Dost hızlandırma da burada (K 21:38). En çok 2 kaynak. | +%20 etki, +%15 hız, 4 s; hız ≤ ×1,5 |
| 9 | Arındırma | Purge | `SIL` | Destek | K (9 Ekim): "yalnız dosttan zararlı siler". Tek başına en son konan etkiyi söker (K F9). Düşman mermisini ve alanını silmez; alanın dosta verdiği durumu söker. | En son konan 1 zararlı etki |
| 10 | Yansıma | Echo | `MIR` | Tank | K (F10): "gelen her zararlı etkiyi (zarar, kontrol, zayıflatma) kaynağına geri yollar; süre veriden (2 s). Geri çevirme YALNIZ Yansıma'nın işi." | 2 s, `reflect_ratio` 0,85; boss'un tek atan büyük vuruşunda dönen hasar tavanı boss azami canının %5'i [ayar] (K 22:15) |
| 11 | Çağırma | Conjure | `ORN` | Saldırı | K (F11): "dünyaya kendi başına davranan yardımcı güç koyar (canlı, ruh, bulut, alev…)." Görünüşü silahın tarzından ve elementten, sayısı ve davranışı sıfattan gelir. Yardımcı yalnız düşmana saldırır; korumak, tutmak ve bağlamak Tank fiillerinin işidir. **K (21:12):** çağrılan yaratık bir varlıktır — canı var, düşmanlar onu hedef alıp vurabilir. | 1 yardımcı, 5 s, canı var |
| 12 | Zaman | Timebend | `KHOR` | Destek | Hedef kaydın alındığı ana döner: can ve konum (1.11). Yavaşlatma, hızlandırma, dondurma ve ileri sarma yapmaz. | Kayıt 3 s önce |

**Not (Zayıflatma satırı):** F7 alıntısındaki "hız Zaman'ın işi" ifadesi eskidir: K 21:38 ve 9 Ekim'den beri yavaşlatma Kontrol'ün çeşidi, dost hızlandırma Güçlendirme'nin işidir; Zaman hız değiştirmez (→ AS-20). Kontrol bir **tasmadır** (1.7).

#### 2.1 Koruma'nın taban kuralı (V4, K 16:49)
Kaynak: K (16:49) "Koruma yeniden tanım". Bu kayıt 'araya girer', 'dostun önüne geçer', dost bağı ve kalkan içeren tüm eski Koruma metinlerini geçersiz kılar.
- **Yalnız sen.** Koruma dostu korumaz, bağ kurmaz, kalkan vermez, can eklemez. Kısa süreli bir **savunma duruşu**dur: sana gelen zararı durdurur ya da azaltır. Sıfat bu duruşun şeklini (süre, yön, alan, tetik) belirler.
- **Kalkan yok, emme yok.** Hiçbir Koruma hücresi `shield_absorb` vermez; sınır senin canındır. Durdurulan zarar ne sana ne başkasına geçer.
- **Duvar yalnız Sabit'ten.** Sabit-Koruma çapalı bir duvar diker (yapı kuralı **1.12**); bu kalkan değildir, canı olan yapıdır. Duvar mermiyi tutar, alanı tutmaz (K 16:52; 1.16).
- **Yalnız zarar.** Duruş kontrolü ve zayıflatmayı durdurmaz; onları geri çevirmek Yansıma'nın, sökmek Arındırma'nın işidir.
- **Kesilme.** Duruş süresince sen ölürsen biter. Kontrol, kökleme, sersemletme ya da denge bozma duruşu bozar — **Fedakarlık-Koruma** hariç (dünya sınırını aşar).
- **Tank dostunu koruma yolu (Koruma skilli değil):** Koruma skilli dostla iş yapmaz. **Mermiye** karşı: gövdeyle araya girmek (Hareket ile konum değiştirmek) — mermi ilk değdiği canlıya çarpar (K 16:52). **Alana** karşı: alanı kuranı kesmek (Kontrol/Kuvvet); alanda araya girilemez (K 16:52).
- **Silah:** duruşun nasıl göründüğünü ve hareketini çizer (guard, siper, mühür…); menzili verir. Dosta varma adımı yoktur.

---

### 3. On iki sıfat

#### 3.1 Rün eşleşmesi ve adlar

| Rün | Sıfat | English | Flaş | Durum |
|---|---|---|---|---|
| 1 | Yoğun | Crux | `VORN` | v2 eşleşmesi |
| 2 | Odaklı | Seeker | `VIS` | K (9 Ekim): Rün 2'ye geçti |
| 3 | Sıçrayan | Ricochet | `SKAR` | v2 eşleşmesi |
| 4 | Sabit | Monolith | `KETH` | v2 eşleşmesi |
| 5 | Yayılan | Nova | `BRAN` | v2 eşleşmesi |
| 6 | Güdümlü | Relentless | `HRAK` | K (17:21): Mıknatıslı (`MAGH`) kalktı; yerine Güdümlü |
| 7 | Tetikli | Primed | `TZEK` | BRIEF önerisi (→ A-20) |
| 8 | Fedakarlık | Bloodprice | `ULTH` | BRIEF önerisi (→ A-20) |
| 9 | İşaretli | (öneri: Sigil) | `ZUL` | K (9 Ekim): Rün 9'a eşlendi. `ZUL` kökü 9-12 master kararıyla yeniden kullanıma alındı |
| 10 | Çizgisel | Lance | `SHIR` | BRIEF önerisi (→ A-20) |
| 11 | Ortak | Tandem | `JEV` | K (17:05): düğüm kalktı; yük = dostun sonraki skilli |
| 12 | Yörüngeli | Orbit | `ROTH` | BRIEF önerisi (→ A-20) |

Aktaran (`TOV`) kalktı (K 9 Ekim); `TOV` kökü emeklidir. Kalkan ya da reddedilen sıfatlar (kaynak v3-kararlar): Emici (19:23), Bağlayıcı (S5), Bulandırıcı (S6), Yükselen (S7), Aynalı (S9), Kopya (S10), Akan (S11), Mıknatıslı (17:21), Aktaran (9 Ekim). Bunların hiçbir hücresi geçerli değildir. Rünü iki kez çizmek, o rünün fiil+sıfat kombosudur (ör. `SIL-ZUL` = İşaretli Arındırma).

#### 3.2 Beş cevap

"Otomatik hedef" sütunundaki "düşman kolu / yararlı kolu", Bölüm 1.2'deki rol tablosuna göre okunur.

| Sıfat | Ana soru → cevap | Otomatik hedef | Yön | Alan | Süre | Biçim |
|---|---|---|---|---|---|---|
| **Yoğun** | Nasıl? → tek noktaya toplanmış (K) | Düşman kolu: en yakın düşman · yararlı kolu: sen | İçe, hedefin üstüne çöker | Tek hedef | Tabanın yarısı | 0,35 s şarj + tek darbe, güç ×1,35. Koruma ve Yansıma'da şarj yerine 0,45 s pencere. **K (19:41), aynen:** "Yoğun'un 0,35 s şarjı yalnız düşmana işleyen fiillerde (Zarar, Kuvvet, Kontrol, Zayıflatma); dosta/sana işleyenlerde (Şifa, Güçlendirme, Arındırma, Zaman, Çağırma, Hareket, Koruma, Yansıma) anında. Taraf kuralına dayalı genel kural." |
| **Odaklı** | Kime? → en çok ihtiyacı olana (K S8) | Fiilin kendi ölçüsüyle en çok ihtiyacı olan; eşitlikte en yakın. **K 18:42:** Hareket'te (Saldırı) ölçüt Saldırı fiilleriyle aynıdır: canı (yüzde) en az düşman → bitirici atılma | Doğrudan hedefe, takip yok | Tek hedef | Normal | ×1,0. Basılı tutup seçersen Odaklı seçimi kapanır |
| **Sıçrayan** | Nasıl ilerler? → sekerek (K) | Düşman kolu: en yakın düşman · yararlı kolu: sen | İlk hedeften min(6 m, silah menzili) içindeki sonraki en yakın uygun hedefe (K 21:10) | 3 hedef sırayla, aynı hedefe iki kez sekmez | Normal | İlk ×1,0, sonra en çok 2 sekme, her biri ×0,5. Sekme mesafesi silah menziliyle kısılır (K 21:10). Uygun hedef yoksa ve fiil yapıya işliyorsa yapıya seker, işlemiyorsa biter |
| **Sabit** | Nerede durur? → yere çakılı (K S4) | Önündeki zemin (2 m) | Yerinde | Yapı ve 3 m çevresi | Yapı ömrü 6 s [test] | Canı olan çapalı yapı. İşi fiil, şekli Sabit (duvar/direk), dokunma durumu element |
| **Yayılan** | Ne kadar alana? → etrafına (K) | Merkez sensin; basılı tutarsan seçilen nokta. **K 18:42:** yalnız seni etkileyen fiilde otomatik merkez, fiilin etkilediği tarafın karşısındaki en yakın canlıdır (Hareket: en yakın düşman → ondan dışa kaçış); taraf fiillerinde (düşmana ya da dosta işleyen) değişmez | Merkezden dışa | min(4 m, silah menzili) yarıçap (K 21:10), en çok 5 uygun hedef | Normal | Genişleyen halka, her hedefe bir kez ×0,85; halka silah menziliyle kısılır |
| **Güdümlü** | Nasıl bırakmaz? → hedefe kilitlenir, kendi süresi yok, 8 s tavanı geçerli (K 17:21 + 17:42 + 22:53) | Düşman kolu: en yakın düşman · yararlı kolu: en yakın dost (yoksa sen) · Koruma: sen | Hedefin peşinden | Tek hedef; alan fiillerinde hedefin altında yürüyen bölge | Süre yok (sigortalar K 22:15: hedef ölür/kaybolursa biter; bütün kilitler en çok 8 s, K 22:53); vurana / dünya durdurana / (3 s sonra) sen bozana kadar | **Kilitlenme (K 17:42).** Hedef silah menzili içinde kilitlenir; skill **kendi süresiyle bitmez** (tavan 8 s, K 22:53) — hedefe vurana ya da dünyada bir şey (duvar, araya giren, Yansıma) durdurana kadar sürer. **İlk 3 s:** sen başka bir şey yapamazsın (yön değiştiremez, skill çizemezsin). **3 s'den sonra** başka skill çizersen kilit bozulur. Hafif kontrol seni kesemez. **Takip eden şey silahın vuruşudur (K 17:40):** yakın silahta sen koşarsın, uzak silahta mermi kovalar. Güdümlü mermi varlıktır (duvar tutar, araya giren alır, Yansıma geri yollar → kilit biter). Seni taşıyan etkide duvar/yapı seni durdurur ve kilit biter. Alan etkileri hedefin merkezinde yürüyen bölge olur. Kontrol tasması kilit süresince kopmaz, en çok 8 s. |
| **Tetikli** | Ne zaman olur? → hedef sonraki hamlesini yapınca (K S12) | Düşman kolu: en yakın düşman · yararlı kolu ve Koruma: sen | Hedefin üstünden dışa | Tek hedef | Tetiklenene kadar, en çok 5 s | Görünür işaret, ×1,0. Tetik: saldırı, skill/büyü ya da atılma başlatma (ilk kare). Yürüme, dönme ve durma tetiklemez. **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider (yedek hâl yok). Senin sonraki skillini katlamaz. Koruma'da tetik = sana gelen bir sonraki darbe (K 16:49 örneği); 5 s'de darbe yoksa boşa gider (4-7 onaylı) |
| **Fedakarlık** | Ne pahasına? → kendi canın pahasına (türetildi) | Düşman kolu: en yakın düşman · Destek: en yakın dost (yoksa sen) · Koruma: sen · **Yansıma: en yakın dost (yoksa sen), basılı tutarsan seçtiğin dost** (v3 Fedakarlık Yansıma, onay 09.10 11:11; A 19:46) *(eski: 'Koruma ve Yansıma: sen')* | Senden hedefe | Tek hedef | Normal | Bedel mevcut canın %30'u. ×1; azalmaya ve irade halkasına takılmaz. Aynı hedefe 3 s'de tek; aynı boss'a kişi başı 60 s'de bir. Koruma'da aştığı sınır: duruşu kontrol/kökleme/sersemletme/denge bozamaz (K 16:49 + Fedakarlık genel) |
| **İşaretli** | Nereden? → işaretten / zincirden (K 17:33) | Sana en yakın işaret (kendi ya da arkadaşının, 25 m içinde); işaret yoksa senden. Zincir skillinde: en yakın **kendi** zincirin (basılı tutup başka oyuncunun zinciri) | Tek işaret: işaretten hedefe. Zincir: konuluş sırası 1→2→3→4 | Tek işaret: işaretin taşıyıcısı ya da işarete en yakın uygun hedef, işaretten silah menzili içinde. Zincir: halka 1 m × en çok 12 m, değen en çok 5 hedef | Tek: fiilin normal süresi. Zincir: 4 s [test] | Tek ×1,0; zincirde hedef başına ×0,85. Zincir sahibi kuralı 1.10 (K 17:35) |
| **Çizgisel** | Hangi şekilde? → senden hedefe düz çizgi (K S14) | Düşman kolu: en yakın düşman · yararlı kolu: en yakın dost · hedef yoksa baktığın yön | Senden hedefe düz | Hat: uzunluğu silah menzili, 1 m genişlik, en çok 5 hedef | Anında; Yansıma'da hat 2 s kalır | Hattaki her hedefe ×0,85 |
| **Ortak** | Kiminle olur? → seçilen dostun bir sonraki skilline yüklenir (K 17:05) | Seçilen dost (otomatik: en yakın dost) | Dostun skillinin yönü | Dostun skillinin şekli (halka, çizgi, sekme…) | Yük 5 s bekler | **Düğüm yok.** Seçilen dosta (otomatik: en yakın) fiilini yüklersin; dost 5 s içinde skill atarsa senin fiilin o skillin **şekli ve hedefleriyle** işler. Taraf kuralı: zararlı yalnız düşmana, yararlı yalnız dosta/sana. Hareket yalnız seni taşır (dostun skillinin vardığı yere yol alarak). Koruma yalnız seni korur (duruş dostun skillinin şeklini alır). **K (21:15/21:17):** 5 s'de skill yoksa yedek hâl yok, yük boşa gider. Güç artmaz, şekil değişir. Oyuncu başına 1 yük; Ortak yükü başka Ortak'a binemez. |
| **Yörüngeli** | Nasıl dolaşır? → taşıyanın etrafında dönerek (K S16) | Taşıyan sensin; basılı tutup dost ya da düşman seçebilirsin (düşman yalnız düşmana işleyen fiilde taşıyıcı olur) | Taşıyanın etrafında döner, onunla yürür | 2 m yarıçap, 3 parça | 3 s | Değdiğine işler; aynı şeye en çok saniyede bir, Kontrol ve Zaman'da 3 s'de bir. Değer etkilerinde her değme ×0,35. Boss'a değme irade harcamaz, denge verir |

#### 3.3 Zayıf hücre sayımı (bu kitaptaki hâliyle)

| Sıfat | Zayıf | Sınırda | Kural (en çok 2) |
|---|---|---|---|
| Yoğun | — | — | tamam |
| Odaklı | — | — | tamam |
| Sıçrayan | Koruma | — | tamam |
| Sabit | — | Hareket | tamam |
| Yayılan | Hareket | — | tamam |
| Güdümlü | Koruma, Zaman | — | tamam |
| Tetikli | Şifa, Güçlendirme | Zayıflatma, Arındırma, Zaman | tamam |
| Fedakarlık | Arındırma, Zaman | — | tamam (hasar azalmaz 16:36; Zarar delme 16:40 ile zayıf değil) |
| İşaretli | — (Arındırma ölçü dışı: işaret koyar) | Güçlendirme | tamam |
| Çizgisel | Şifa, Arındırma | Zaman | tamam (→ A-14) |
| Ortak | — | — | tamam (K 21:15: 5 s yedek kalktı) |
| Yörüngeli | Hareket | — | tamam |

---

### 4. On silah: menzil + ritim + tarz

#### 4.1 Silah kuralı (güncel)
- **K (20:29), aynen:** "Silah rolü (kullanıcı, 2026-10-09 20:29): silah = menzil + ritim (saldırı hızı/ön süre) + oynanış tarzı (animasyon; Kılıç samuray, Asa büyücü, Kalkan tank). Mekanik silah kanunu YOK. Aynı menzildeki silahları ritim ayırır (Çekiç ile Kılıç'ın saldırı hızı farklıdır). Seçim sebebi menzil + ritimdir."
- **K (15:41), aynen:** "Menzili silah belirler (yumruktan uzun menzil inandırıcı değil). Yön, hedef, alan, süre, biçim sıfatta kalır; sıfat şekli silahın menzili içinde çizer. 5 Ekim'deki "menzil silahta değil" kararı bununla değişti."
- **K (9 Ekim), aynen:** "Silah kuralı: fiil ne olacağını, sıfat şekli (hedef, yön, alan, süre, biçim) belirler. Silah yalnız dövüş tarzını belirler: hareket, ritim, bundan çıkan küçük oynanış farkı. Menzil ve yön silahtan gelmez." *(Bu kaydın "Menzil … silahtan gelmez" kısmı K 15:41 ile geçersizdir. Yön kısmı geçerlidir.)*
- **Tek cümleyle:** fiil **ne** olacağını, sıfat **yön ve şekli** (hedef, yön, alan, süre, biçim), silah **menzili, ritmi ve tarzı** belirler.
- Silah fiilin tarafını değiştirmez, rol vermez (rol fiilden gelir, 1.2), Kontrol'ün çeşidini seçmez, Sabit'in şeklini vermez, ışınlamaz.
- **Mekanik silah kanunu yoktur.** "Yakın silahların karşılığı" (süper zırh / kesilmez vuruş, yalnız yakın silahta dodge iptali) K 20:29 + **K 21:05** ile **kaldırıldı**. Kalıntılar (`silahlar.md` "kesilmez", animasyon belgelerindeki süper zırh) silinir. Yakın silahlar yalnızca ritimle öne çıkar. *(saat farkı 20:27 / 20:29 → AS-06)*
- `silahlar.md`, aynen: "Ritim yön, hedef, süre ve şekil değiştirmez. Hepsi test değeri."
- **K (21:10b):** Toparlanma sırasında yürüme yavaşlığı ritim değeridir (`toparlanma_yurume_carpani`; varsayılan 1,0, Küre 0,5). Silah kanunu değil.
- `silahlar.md`, aynen: "**Sıfatla birleşme:** sıfatın kendi ön süresi (ör. Yoğun şarjı 0,35 s) silahın ön süresine eklenir; toplam ön süre tavanı test değeri 1,0 s."
- İşaretten çıkan skill: `silahlar.md` aynen: "İşaretten çıkan skillde oyuncu hareketi kendi yerinde yapar, hareketin kordan yankısı işarette belirir. Menzil işarete kadar olan mesafeye uygulanır, işaretten sonrası sıfatın şekline aittir." K (15:47): "skill işaretten silah menziliyle çıkar."
- **Genel görünüm kuralı** (`silah-fiil-120.md` Notlar, aynen): "silah hareketi hep senin yerinde yapılır. Etkinin kurulduğu yer sen değilsen (işaret, korunan dost, tuzak/yapı, yardımcının doğduğu yer), hareketin kordan yankısı orada belirir. Görünümdeki yön ve bağ, kuralın yönüne ve bağına çizilir. Bu kural Yansıma'da korunan dostu ve Çağırma'da doğum yerini de kapsar; o hücrelerde yer sıfattan gelir."
- Silah değiştirince silah kor ışığıyla belirip kaybolur. **K (21:05): Kılıç'ta kın yok;** iaido kınsız çekilir (el kabzada, kılıç geride). Kın Sonra listesinde.

#### 4.2 Menzil, tarz ve ritim tablosu
Menzil = `range_mult` × 6 m [test]. Tarz satırları `silah-fiil-120.md` (GameDesign, 20:36 sürümü) aynen; ritim sayıları `silahlar.md` "Ritim ölçüleri (9 Ekim 20:29 kararı)" tablosu aynen. Bütün sayılar başlangıç değeridir.

| # | Silah | Menzil | Tarz (aynen) | Ön süre (s) | Vuruş | Toparlanma (s) | Ön sürede hareket | Toplam (s) | Not |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Yumruk — Seri Boksör | 3 m | seri boksör. Her şey kısa, keskin ve peş peşe; tek vuruş yerine üçlü seri. | 0,10 | 3 vuruş, 0,30 s içinde | 0,15 | Evet | 0,55 | En hızlı; seri |
| 2 | Yay — Avcı (Levi) | 14 m | sabırlı avcı ve ip ustası (Levi). Ger, nişan, bırak; hareketi iple. | 0,35 (otomatik germe) | 1 | 0,20 | Hayır | 0,55 | Germe otomatik; erken bırakma yok (K 22:15) |
| 3 | Büyü Kitabı — Okuyan | 9 m | okuyan. Kitabı açar, sözü yüksek sesle okur; harfler ve sayfalar uçar. | 0,60 | 1 | 0,10 | Evet (okurken yürür) | 0,70 | Uzun okuma, kısa toparlanma |
| 4 | Kılıç — Samuray (Zenitsu) | 6 m | samuray / Zenitsu. Tek, ani, şimşek kesiş; iaido kınsız (el kabzada, kılıç geride). VUR = yerinde iaido; şimşek atılış yalnız Hareket'te (K 21:05). | 0,20 | 1 | 0,20 | Hayır | 0,40 | Hızlı tek kesiş |
| 5 | Küre — Kor Sıkan | 7 m | kor sıkan. Küreyi avuçta ezer, ışık anında patlar; sonra kısa soğuma. Kürenin konumu yok, hep avuçta. | 0,00 | 1 | 0,40 (`toparlanma_yurume_carpani` 0,5) | — | 0,40 | Anında çıkış; toparlanmada yürüme yarı hız (ritim, K 21:10b) |
| 6 | Çekiç — Ağır Savaşçı (Gimli/Thor) | 6 m | Gimli / Thor. Ağır; kaldır, bekle, çak. Çekici fırlatır ve geri çağırır. | 0,55 | 1 | 0,50 | Hayır | 1,05 | En yavaş; uzun kaldırma |
| 7 | Top — Topçu | 18 m | topçu. Nişan, ateş, geri tepme; gülle düştüğü yerde açılır. Geri tepme yalnız görüntüdür (Hareket dışında oyuncuyu kaydırmaz). | 0,40 (nişan) | 1 | 0,35 | Hayır | 0,75 | Geri tepme yalnız görüntü |
| 8 | Asa — Büyücü | 15 m | ışın büyücüsü. Asadan ışın akıtır, havaya rün çizer. | 0,30 (akıtma) | 1 | 0,20 | Hayır | 0,50 | Kısa akıtma |
| 9 | Tılsım — Mühürcü | 17 m | mühürcü. Avucundan mühür salar; mühür hedefin ayağına düşer, 0,5 s sonra açılır. | 0,00 | 1 | 0,45 | — | 0,45 | Gecikme mühürde (0,5 s), ön sürede değil; toparlanma 0,45 (K 22:15) |
| 10 | Kalkan — Muhafız (Tank) | 3 m | tank. Ağır adım, kenarla indirme (VUR); omuz hücumu yalnız Hareket'te (K 21:05). | 0,30 | 1 | 0,45 | Ağır adım (Hareket sayılmaz) | 0,75 | "Kesilmez" kalıntısı silindi (K 21:05) |

- **Çift ayrımı** (`silahlar.md`, aynen): "**Kılıç–Çekiç (ikisi 6 m):** Kılıç 0,20 + 0,20; Çekiç 0,55 + 0,50. Çekiç'in ön süresi 2,75 katı, toplamı 2,6 katı." · "**Yumruk–Kalkan (ikisi 3 m):** Yumruk 0,10 ön süre, 3 parçalı seri, yürürken; Kalkan 0,30 ağır adım, tek darbe, 0,45 toparlanma."
- Dost etkisinde menzil = min(silah menzili, 6 m). Hareket'te menzil = en fazla yer değiştirme (`silahlar.md`, 15:42).
- `silahlar.md` dosyasının üst "Menzil tablosu"ndaki eski "Dövüş tarzı / Ritim" sütunları bu tabloyla çelişiyor (→ AS-05). Bu kitap yukarıdaki tabloyu esas alır.

#### 4.3 Hareket: yolculuk görünümü ve ritmi
- **K (20:35), aynen:** "İşaretli-Hareket silah görünümleri (kullanıcı, 2026-10-09 20:35): mekanik aynı (işarete yol alarak çekilme, mesafe = silah menzili, ışınlanma yok); görünüm silah tarzından: Yay ip atıp Levi gibi; Çekiç fırlatıp Thor gibi peşinden; Kalkan en çok 3 m omuz hücumu (uzak işarete gidemez); Kılıç şimşek atılış (Zenitsu); Asa ışın yolunda süzülme; Küre patlama itişiyle fırlama + 0,4 s yavaşlama; Tılsım mühür 0,5 s sonra çeker; Top seni oraya fırlatır (top güllesi gibi); Kitap sayfalara dağılıp hızlı yolculuk (gerçek ışınlanma değil); Yumruk kısa atılış."
- **K (20:35), aynen:** "Hareket yolculuğu ritmi (kullanıcı, 2026-10-09 20:35): yolculuk hızı ve yolda skill atılıp atılamayacağı silah ritminden gelir. Top: çok hızlı gülle uçuşu, yolda skill yok. Yay: ipte süzülme, yolda skill/silah değiştirme var. Çekiç: Thor uçuşu, yolda skill yok, varışta çakma. Asa: süzülme, yolda skill var. Küre: patlama fırlaması, yolda skill yok. Diğerleri GameDesign tablosunda."
- Yolculuk hızı tablosu (K 20:35 + **K 21:11**; sayılar başlangıç değeri):

| Silah | Yolculuk hızı | Yolda skill | Kaynak |
|---|---|---|---|
| Top | Çok hızlı, ~30 m/s (gülle) | Yok | Kullanıcı (K 20:35) |
| Yay | Yavaş, ~14 m/s (ipte süzülme) | Var | Kullanıcı (K 20:35) |
| Çekiç | Hızlı, ~22 m/s (Thor) | Yok; varışta yere çakar (görünüm) | Kullanıcı (K 20:35) |
| Asa | En yavaş, ~10 m/s (ışında süzülme) | Var | Kullanıcı (K 20:35) |
| Küre | Hızlı, ~25 m/s (patlama savrulması) | Yok | Kullanıcı (K 20:35) |
| Yumruk | Çok hızlı, ~30 m/s (Top ile eşit; kısa atılış) | Yok | K 21:11 |
| Kılıç | Hızlı, ~25 m/s (şimşek atılış) | Yok; çok kısa sürdüğü için gerek yok | K 21:11 (Kılıç>Yumruk reddedildi) |
| Kitap | Orta, ~18 m/s (sayfa olarak akış) | Yok; beden dağılmış durumda | K 21:11 |
| Tılsım | Orta, ~16 m/s (mühür çekmesi, 0,5 s sonra) | Var | K 21:11 |
| Kalkan | Yavaş, ~12 m/s (ağır omuz hücumu) | Yok | K 21:11 |

- `silahlar.md`, aynen: "Yolculuk hızı yalnız hız; yol, yön ve mesafe sıfattan gelir. Işınlanma yok."
- **Kitap Hareket** görünümü: sayfalara dağılma yalnız görünüm; beden gerçekten yol alır (K 20:35). Hasar / duvar / araya girme → genel yol alan beden kuralı (K 21:07), silaha özel değil.
- Hareket görünümlerinin yalnız İşaretli-Hareket için mi yoksa bütün Hareket sıfatları için mi geçerli olduğu açık (→ AS-25).

#### 4.4 Koruma ve silah
- Koruma yalnız seni korur (K 16:49; 2.1). Silah yalnız duruşun nasıl göründüğünü ve hareketini çizer.
- `silahlar.md`, aynen: "Asa büyü duruşu (yalnız kendi duruşu, kalkan vermez)". `silah-fiil-120.md` Asa Koruma, aynen: "Asaya yaslanıp yerinde duruşa geçer; asanın önünde açılan kor halka duruştur, kalkan değildir (görüntü)".
- `silah-fiil-120.md` Notlar, aynen: "Koruma'da dosta varma adımı yoktur (K 16:49); duruş yerinde açılır. Işınlanma kullanılmaz (Çizgisel'in işi)."
- Kalkan'da "araya girme" yoktur; GameDesign bütün Kalkan hücrelerini K 16:49'a göre yazdı ("araya girmez, yerinde açılır").

#### 4.5 Tılsım
- **K (18:39), aynen:** "Tılsım ritmi (kullanıcı onayı, 2026-10-09 18:39): mühür hedefin ayağına yere düşer, 0,5 s sonra patlar; yol yok, kaçılabilir gecikme (alan gibi karşı oyun)."
- `silah-fiil-120.md`, aynen: "Tılsım'ın dışarı giden her etkisi: mühür hedefin ayağına düşer, 0,5 s sonra açılır." Tılsım Hareket: "Mühür yolun ucuna düşer; 0,5 s sonra parlayıp seni yol boyunca kendine çeker" (K 20:35 "Tılsım mühür 0,5 s sonra çeker" ile aynı).
- Hedefte belirme yoktur (A 20:36, `silahlar.md` Açık). Tılsım teslimi = gecikmeli alan (K 21:05).

---

### 5. 144 hücre (fiil × sıfat)

Her fiil satırı 12 sıfatla. Kod = kombo no · flaş (çizim sırası: fiil → sıfat). Sayılar [test]. Mesafeler Bölüm 1.3'e göre silah menziliyle kesilir.

#### 5.1 Zarar (`AK`) · Saldırı · yalnız düşman

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 1-1 `AK-VORN` | Yoğun | "Saldırı: kısa şarj + tek sert vuruş" (karar 4). Ölçü: 0,35 s şarj, en yakın düşmana tek darbe: taban hasar ×1,35, denge ×1,35. | K |
| 1-2 `AK-VIS` | Odaklı | "Zarar: canı en az düşman." Ölçü: canı (yüzde) en az düşmana taban hasar ×1,0; eşitlikte en yakın. | K |
| 1-3 `AK-SKAR` | Sıçrayan | "Zarar: düşmana vurur, sonra 2 düşmana yarım güçle seker." Ölçü: ilk ×1,0, sonra min(6 m, silah menzili) içindeki 2 düşmana ×0,5 (K 21:10). | K |
| 1-4 `AK-KETH` | Sabit | Dikenli direk: değen ya da 1 m içindeki düşmana saniyede bir taban ×0,25, 6 s (en çok ×1,5; kendi tikleri azalmaz). Ateş görünüşü ve yanık Ateş elementinden gelir. | K (onay 09.10 17:35) |
| 1-5 `AK-BRAN` | Yayılan | "Zarar: etrafındaki en çok 5 düşmana 0,85 kat zarar verir." Ölçü: min(4 m, silah menzili) (K 21:10). | K |
| 1-6 `AK-HRAK` | Güdümlü | Hedefe kilitlenirsin; **karakter durmaz** — hedef silah menziline girene kadar kovalar (yayda neredeyse hiç koşmaz, kılıçta üstüne koşar). Menzile girince 3 **kilit vuruşu** (K 17:41): ıskalamaz, otomatik hedefe dönersin, vuruşlar arası 0,3 s [test], ilk iki taban ×0,6, üçüncü ×1,0 (görünüş silahtan). **Takip eden şey silahın vuruşudur (K 17:40):** yakın silahta sen koşarsın, uzak silahta mermi kovalar. Süreyle bitmez (K 17:42): vuruşlar bitene ya da dünya (duvar / araya giren / Yansıma) durdurana kadar sürer. İlk 3 s başka skill çizemezsin; 3 s'den sonra skill çizersen kilit bozulur. Boss aynı. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 1-7 `AK-TZEK` | Tetikli | "Zarar: düşman saldırıya ya da atılmaya başladığı an işaret patlar." Ölçü: tetik üç olaydır (saldırı, skill/büyü, atılma; ilk kare; K 20:05 "satırlar buna göre okunur"); taban hasar ×1,0; **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider. | K |
| 1-8 `AK-ULTH` | Fedakarlık | K (16:40): canının %30'unu ödersin; vuruş normal ×1 hasar; hedefin **kalkanına, araya giren korumaya ve yansıtmaya takılmaz**, doğrudan cana gider. Hasar zaten azalmaz (K 16:36); aştığı sınır korumalardır. Bedel: mevcut can (11:17). Boss 60 s kişi başı. | K |
| 1-9 `AK-ZUL` | İşaretli | **Tek işaret:** vuruş işaretten çıkar; işaret düşmandaysa o düşmana, değilse işarete en yakın düşmana (işaretten silah menzili içinde) taban hasar ×1,0. **Zincir:** 4 s; zincire değen her düşmana bir kez ×0,85, en çok 5. **İşaret yok:** senden en yakın düşmana ×1,0. Dövüş hareketi oyuncuda, kor yankısı işarette. | K (onay 09.10 17:33) |
| 1-10 `AK-SHIR` | Çizgisel | "Zarar: çizgideki tüm düşmanlar." Ölçü: senden hedefe düz hat (uzunluk silah menzili, 1 m); en çok 5 düşmana ×0,85, anında. | K |
| 1-11 `AK-JEV` | Ortak | Seçilen dosta Zarar yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki düşmanlara taban hasar ×1,0 (örn. Yayılan halkadaki düşmanlar). **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 1-12 `AK-ROTH` | Yörüngeli | "Zarar: bıçaklar." Ölçü: taşıyanın gövde yarıçapı + 2 m etrafında (K 22:15) 3 parça, 3 s; değen düşmana ×0,35, aynı düşmana saniyede bir. | K |

#### 5.2 Şifa (`SOL`) · Destek · dost ve sen

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 2-1 `SOL-VORN` | Yoğun | "Şifa: tek dosta anında büyük can, fazlası kalkan." Ölçü: tek hedef (otomatik sen), anında taban şifa ×1,35; canı taşan kısım 5 s'lik kalkana döner (F2: kalkan oyunda yalnız burada ve elementte kalır; taşma ve kalkan tavanı dengelemeye bırakıldı, 12:17). | K |
| 2-2 `SOL-VIS` | Odaklı | "Şifa: canı en az dost." Ölçü: canı (yüzde) en az dosta taban şifa. | K |
| 2-3 `SOL-SKAR` | Sıçrayan | "Şifa: seni ya da seçtiğin dostu iyileştirir, sonra 2 dosta yarım güçle seker." Sekme mesafesi min(6 m, silah menzili) (K 21:10). | K |
| 2-4 `SOL-KETH` | Sabit | "Şifa kaynak". Ölçü: 3 m içindeki dostlara saniyede bir taban ×0,2, 6 s; dost yapıları da onarır. | K |
| 2-5 `SOL-BRAN` | Yayılan | "Şifa: etrafındaki dostları ve seni 0,85 kat iyileştirir." Ölçü: min(4 m, silah menzili) (K 21:10), en çok 5 hedef. | K |
| 2-6 `SOL-HRAK` | Güdümlü | Kilitlenen dosta (otomatik: en yakın dost; yoksa sen) peşinden gider; değdiği sürece saniyede bir taban şifa ×0,35. Kendi süresi yok (K 17:42), 8 s tavanı geçerli: dünya durdurana veya 3 s'den sonra sen skill çizene kadar. İlk 3 s kilitlisin. Kendine kilitlenirse yerinde iyileşirsin. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 2-7 `SOL-TZEK` | Tetikli | Dost bir sonraki hamlesini yaptığı an iyileşir; sana koyarsan bir sonraki hamlende iyileşirsin. Tetik ilk karedir (saldırı, skill/büyü ya da atılma başlatma): işaretli kişi hamlesine başladığı an taban şifa; **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider. | K (zayıf) (metin temizlendi 19:4x: K 20:05 notu Durum'a taşındı) |
| 2-8 `SOL-ULTH` | Fedakarlık | "Fedakarlık Şifa (onay 09.10 11:07): Canının %30'unu ödersin; en yakın dost (basılı tutarsan seçtiğin dost) normal şifanın tamamını ×1 alır, azalma kuralına takılmaz. Aynı dosta 3 s'de tek Fedakarlık." **K (21:15/21:17):** geçerli dost yoksa skill boşa gider (yedek hâl yok); dost menzil dışındaysa karakter menziline kadar yürür, sonra atar. | K (21:15) |
| 2-9 `SOL-ZUL` | İşaretli | **Tek işaret:** işaret dosttaysa o dosta, değilse işarete en yakın dosta (sen dahil; işaretten min(silah, 6 m)) taban şifa ×1,0. **Zincir:** 4 s; zincire değen her dosta ve dost yapıya bir kez ×0,85, en çok 5; işaretler dostlardaysa bağ onlarla birlikte uzanır. **İşaret yok:** sana taban şifa. | K (onay 09.10 17:33) |
| 2-10 `SOL-SHIR` | Çizgisel | "Şifa/Arındırma zayıf (çizgide dost varsa)." Ölçü: hattaki dostlara ×0,85, en çok 5. | K (zayıf) |
| 2-11 `SOL-JEV` | Ortak | Seçilen dosta Şifa yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki dostlar (sen dahil) taban şifa ×1,0 alır (örn. Yayılan Zarar halkasındaki dostlar). **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 2-12 `SOL-ROTH` | Yörüngeli | "Şifa: küre." Ölçü: değen dosta ×0,35, saniyede bir, 3 s; taşıyan merkezde olduğu için kendisi iyileşmez. | K |

#### 5.3 Hareket (`SHEV`) · Saldırı · yalnız sen taşınırsın · mesafe en çok silah menzili

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 3-1 `SHEV-VORN` | Yoğun | Ölçü: en yakın düşmana silah menzili kadar, tabanın yarı süresinde atılırsın; canlılar birbirinin içinden geçmez, düşmanın gövde kenarında frenlersin (K 22:15). Düşman yoksa skill boşa gider (K 21:17). | K |
| 3-2 `SHEV-VIS` | Odaklı | **K 18:42:** canı (yüzde) en az düşmana (eşitlikte en yakın) silah menzili kadar **yol alarak atılırsın** (bitirici). *(eski: 'düşmanlardan en uzak güvenli nokta' — kaçış metni düştü)* | K (18:42) |
| 3-3 `SHEV-SKAR` | Sıçrayan | "Sıçrayan = sen düşmandan düşmana sekersin" (07.10). Ölçü: en yakın düşmana silah menzili kadar atılırsın; sonra min(6 m, silah menzili) içindeki sonraki en yakın düşmana menzilin ×0,5'i kadar, iki kez; aynı düşmana iki kez gitmezsin. Düşman yoksa skill boşa gider (K 21:17). Tek boss'ta yalnız ilk atılma. | K (S3'teki dost taşıyan satır 9 Ekim "yalnız kullanıcı" kuralıyla düştü) |
| 3-4 `SHEV-KETH` | Sabit | Sıçrama taşı: **yalnız sen** üstüne basınca, dikildiği yöne (dikildiği anda senden taşa doğru) silah menzili kadar fırlarsın; 3 s'de bir, 6 s boyunca. Dostlar basınca bir şey olmaz. | K (21:17b) |
| 3-5 `SHEV-BRAN` | Yayılan | Yalnız sen kayarsın: merkezden dışa düz, silah menzili ×0,85. **K 18:42:** otomatik merkez **en yakın düşman** → ondan dışa kaçış; basılı tutarsan seçtiğin merkezin min(4 m, silah menzili) içindeysen ondan dışa. *(eski: 'otomatikte merkez sensin, skill boşa gider')* | K (18:42) |
| 3-6 `SHEV-HRAK` | Güdümlü | Hedefe (otomatik: en yakın düşman; silah menzili içinde kilit) **direkt atılıp mesafeyi kapatırsın** (vuruş yok; K 17:40). Süreyle bitmez (K 17:42): hedefe varana ya da duvar/yapı durdurana kadar sürer; durunca kilit biter. İlk 3 s yön değiştiremez / skill çizemezsin; 3 s'den sonra skill çizersen kilit bozulur. Hafif kontrol seni kesemez. Boss'a sen gidersin (ağırlık). **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 3-7 `SHEV-TZEK` | Tetikli | "Hareket: bir sonraki saldırına başladığın an otomatik hedefin yanına atılırsın; saldırın atılmayla başlamış olur." Ölçü: işaret sende; bir sonraki hamlenin ilk karesinde en yakın düşmanın yanına silah menzili içinde atılırsın; **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider. | K |
| 3-8 `SHEV-ULTH` | Fedakarlık | "Fedakarlık Hareket (onay 09.10 11:13): canının %30'u; atılma azalmaya ve sınırlara takılmaz, yavaşlatma ya da kökleme engelleyemez." Ölçü: **sen** en yakın düşmana doğru (basılı tutarsan seçtiğin yere) silah menzili kadar atılırsın. | K |
| 3-9 `SHEV-ZUL` | İşaretli | "İşaretli-Hareket = atılma (Levi tarzı). Ağırlık kuralı geçerli: işaret ağır bir nesnede (duvar/boss) olduğu için hafif olan oyuncu işarete doğru çekilir. Oyuncu yol boyunca görünür. İşaretli arkadaş da onunla birlikte çekilir." (15:55) **Yolda skill / silah değiştirme:** silah ritmine göre (K 20:35; tablo 4.3) — Yay ve Asa'da var; Top, Çekiç, Küre'de yok; diğerleri tabloda. Ölçü: işaret 25 m içinden seçilir, çekilme en çok silah menzili kadardır, işaret daha uzaksa o yolda durursun (türetildi). **Zincir (K 17:33):** zinciri izlersin — önce 1. işarete, sonra 2.'ye, sonra 3.'ye çekilirsin. **K (21:15/21:17):** işaret yoksa skill boşa gider (yedek hâl yok). | K (21:15) (→ A-7, A-8) |
| 3-10 `SHEV-SHIR` | Çizgisel | Çizgi ucuna ışınlanma: oyuncu senden hedefe uzanan görünmez çizginin ucuna bir anda konur. Hat en çok silah menzili; yol almadan, hiçbir şeye değmeden varırsın, varışta hız 0. Otomatikte en yakın düşmanın yanına. | K (metin temizlendi 19:4x; v3'te otomatik en yakın dosttu) |
| 3-11 `SHEV-JEV` | Ortak | Seçilen dosta Hareket yüklenir; dost 5 s içinde skill atınca sen o skillin vardığı son noktaya **yol alarak** atılırsın, en çok silah menzili (ışınlanma yok). **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (A 19:41, sürpriz) |
| 3-12 `SHEV-ROTH` | Yörüngeli | Yörünge parçası sensin: taşıyanın çevresinde gövde yarıçapı + 2 m (K 22:15) yarıçapta 3 s dönersin, o yürürse onunla gidersin; değdiğin kimseye bir şey olmaz. Taşıyan otomatikte en yakın dost (dost yoksa en yakın düşman); basılı tutup seçebilirsin. | K (A 19:41, sürpriz) |

#### 5.4 Koruma (`THAL`) · Tank · yalnız sen · kalkan yok · savunma duruşu (2.1)

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 4-1 `THAL-VORN` | Yoğun | "0,45 saniyelik tam savuşturma. O anda gelen tek darbe tamamen durur." (K 16:49) Ölçü: 0,45 s pencere; pencerede sana gelen **tek** darbe tamamen durur, fazlası geçmez. Kalkan yok. | K (onay 09.10 17:36) |
| 4-2 `THAL-VIS` | Odaklı | 2,5 s [test]; son 3 s'de sana en çok zarar veren düşmanın **yönünden** gelen zarar tamamen durur. Kimse vurmadıysa en yakın düşman yönü. Diğer yönler korunmaz. | K (onay 09.10 17:36) |
| 4-3 `THAL-SKAR` | Sıçrayan | 3 s pencere: sana gelen **ilk** darbe tamamen durur; sonraki en çok 2 darbe (aynı pencerede) her biri %50 azalır. Sekme hedef değiştirmez (yalnız sen). | K (onay 09.10 17:36) (zayıf) |
| 4-4 `THAL-KETH` | Sabit | "Koruma duvar" (S4). Ölçü: önüne 4 × 2 × 0,5 m çapalı duvar; **mermiyi tutar** (K 16:52), **alanı tutmaz**. Yakın vuruşun duvardan geçip geçmediği açık (KG-37). Canı biterse yıkılır. Duvar yapıdır, kalkan değildir; seni doğrudan buff'lamaz. | K (onay 09.10 17:36) |
| 4-5 `THAL-BRAN` | Yayılan | "2 saniye her yönden gelen zarar %50 azalır." (K 16:49) Ölçü: 2 s, 360°, sana gelen her zararın %50'si geçer. | K (onay 09.10 17:36) |
| 4-6 `THAL-HRAK` | Güdümlü | Kilit sensin (Koruma yalnız seni korur). Duruş: sana gelen zarar %50 azalır; kendi süresi yok, 8 s tavanı geçerli (K 17:42, 22:53) — 3 s'den sonra skill çizersen biter (ilk 3 s iptal edilemez). Hafif kontrol bozamaz. Kalkan yok. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) (zayıf) |
| 4-7 `THAL-TZEK` | Tetikli | "Sana gelen bir sonraki darbe tamamen durur." (K 16:49) Ölçü: görünür kapan sana konur; sana gelen bir sonraki darbe (saldırı/mermi isabeti) tamamen durur; 5 s'de darbe yoksa skill boşa gider (K 21:15/21:17). | K (onay 09.10 17:36) |
| 4-8 `THAL-ULTH` | Fedakarlık | Mevcut canının %30'unu ödersin. 2,5 s [test] her yönden gelen zarar tamamen durur; duruşu kontrol, kökleme, sersemletme ya da denge bozma **bozamaz** (Fedakarlık'ın aştığı dünya sınırı). Azalmaya takılmaz. Aynı sana 3 s'de tek Fedakarlık. | K (onay 09.10 17:36) |
| 4-9 `THAL-ZUL` | İşaretli | **Tek işaret:** 2,5 s; işaretin yönünden (işaretten sana uzanan hat) gelen zarar tamamen durur. **Zincir:** 4 s; zincir hattının her iki yönünden gelen zarar %85 azalır. **İşaret yok:** duruş senin önüne döner; önden gelen zarar tamamen durur, yandan ve arkadan gelen tutulmaz. | K (A 19:41, sürpriz) |
| 4-10 `THAL-SHIR` | Çizgisel | "3 saniye boyunca sadece önünden gelen vuruşları tamamen tutarsın." (K 16:49) Ölçü: 3 s; baktığın yönde 1 m genişlik × silah menzili derinliğinde koridor; yalnız bu koridordan gelen zarar tamamen durur. Yan ve arka korunmaz. Duvar oluşmaz. | K (onay 09.10 17:36) |
| 4-11 `THAL-JEV` | Ortak | Seçilen dosta Koruma yüklenir; dost 5 s içinde skill atınca **yalnız senin** duruşun o skillin şeklini alır: halka → 2 s her yönden %50; çizgi → o hat yönünden 3 s tam; sekme → ilk darbe tam + sonraki 2 × %50; tek hedef → 2,5 s o yönden tam; tek noktaya toplanan (tek sert darbe / tetik) → ilk darbe tam; yer kuran (yapı / zincir) → o yerin yönünden tam; yörünge → mermiler durur, yakın vuruş ve alan tutulmaz. Dost korunmaz. **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (21:15; şekil sınıfları 17:36) |
| 4-12 `THAL-ROTH` | Yörüngeli | Senin (taşıyan sensin; basılı tutup düşman seçilmez — Koruma düşmana işlemez) gövde yarıçapı + 2 m (K 22:15) etrafında 3 parça, 3 s. Parçaya değen **düşman mermisi** parçada durur, sana ulaşmaz (S16 onaylı "mermi tutan parçalar"; merminin parçaya çarpması KG-33). **Alan saldırısı** parçayı aşar (K 16:52: alana araya girilemez). Parça arasından geçen vuruş tutulmaz. Emme / kalkan yok. | K (onay 09.10 17:36) |

#### 5.5 Kuvvet (`KRAG`) · Tank · hep senden dışa · çekme yok · boss itilmez, denge alır

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 5-1 `KRAG-VORN` | Yoğun | 0,35 s şarj; en yakın tek düşman senden dışa (3 m × 1,35) itilir — itme ölçüsü menzille kısılmaz, menzil yalnız erişimi kısaltır (K 22:17); itilme süresi tabanın yarısı; denge ×1,35. Boss itilmez, denge ×1,35. | K (21:17b; itme K 22:17) |
| 5-2 `KRAG-VIS` | Odaklı | "Kuvvet: sana/dosta saldıran en yakın düşman." Ölçü: o düşman senden dışa 3 m. | K |
| 5-3 `KRAG-SKAR` | Sıçrayan | "Kuvvet: düşmanı iter, sonra darbe sıradaki 2 düşmana yarım güçle seker." Ölçü: ilk düşman senden dışa 3 m (denge ×1,0); min(6 m, silah menzili) içindeki 2 düşman (K 21:10) senden dışa 1,5 m (itme menzille kısalmaz, K 22:17) (denge ×0,5). İtme yönü sekme yönü değil, senden dışadır. | K |
| 5-4 `KRAG-KETH` | Sabit | "Kuvvet iten sütun" (S4). Ölçü: dikildiği an ve sonra 2 s'de bir (0, 2, 4, 6. s), direğin 3 m'sindeki düşmanlar **senin o anki konumundan** dışa 3 m itilir. İtilen düşman çarpabilir. Boss: her itmede denge ×1,0. Yapının/parçanın her vuruşu yeni darbedir; skillin kendi tekrarı olduğu için azalma uygulanmaz (K 22:54, K 11:46-3). | K (21:13) |
| 5-5 `KRAG-BRAN` | Yayılan | "Kuvvet yalnız düşmanı iter" (S2). Ölçü: halkadaki (min(4 m, silah menzili), K 21:10) en çok 5 düşman senden dışa 2,55 m (3 m × 0,85; itme menzille kısalmaz, K 22:17). Merkezi başka yerde seçsen de itme senden dışadır. Boss: denge ×0,85. | K |
| 5-6 `KRAG-HRAK` | Güdümlü | Sen kilitli düşmanı kovalayıp **önünde iterek sürersin** (K 17:40). Saniyede 1 m [test] sürekli itme; sürüklemenin tamamı tek darbe (K 22:37). Düşman duvara çarparsa ezilir (çarpma hasarı → A-17). Boss itilmez; kilit süresince saniyede denge ×0,35. Kendi süresi yok, 8 s tavanı geçerli (K 17:42, K 22:54): dünya durdurana (duvar seni ya da itmeyi keser) veya 3 s'den sonra sen skill çizene kadar. İlk 3 s kilitlisin. Çekme yok. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 5-7 `KRAG-TZEK` | Tetikli | İşaret en yakın düşmana konur. Düşman bir sonraki hamlesine başladığı an senden dışa 3 m itilir ve taban denge hasarı alır; **K (21:17):** 5 s'de tetik yoksa skill boşa gider. Kuvvet hamleyi kesmez (kesmek Kontrol'ün işi). | K (21:17b) |
| 5-8 `KRAG-ULTH` | Fedakarlık | "Fedakarlık Kuvvet (onay 09.10 11:10): Canının %30'unu ödersin; en yakın düşman normal itmenin tamamıyla 3 m itilir, azalma kuralına takılmaz. Boss'ta ağırlık kuralı kalır: yerinden oynamaz, denge hasarını tam ×1 alır." Yön: senden dışa. Boss 60 s kişi başı. | K |
| 5-9 `KRAG-ZUL` | İşaretli | **Tek işaret:** darbe işaretten çıkar; işaretli düşman (ya da işarete en yakın düşman, işaretten silah menzili içinde) **senden** dışa 3 m itilir. **Zincir:** 4 s; zincire değen her düşman bir kez senden dışa 2,55 m, en çok 5. **İşaret yok:** en yakın düşman senden dışa 3 m. Boss itilmez, denge alır. | K (onay 09.10 17:33) |
| 5-10 `KRAG-SHIR` | Çizgisel | Hattaki (silah menzili, 1 m) en çok 5 düşman senden dışa, yani hat boyunca ileri 2,55 m itilir. Koridor açılmaz. Boss: denge ×0,85. | K (21:17b) |
| 5-11 `KRAG-JEV` | Ortak | Seçilen dosta Kuvvet yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki düşmanlar **senden** dışa 3 m itilir (şekil yalnız kimi seçer; yön hep senden dışa). Boss itilmez, denge ×1,0. **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 5-12 `KRAG-ROTH` | Yörüngeli | "Kuvvet: dışa iter." Ölçü: değen düşman senden dışa 1 m, aynı düşmana saniyede bir; taşıyan dost ya da düşman olsa da yön senden dışa. Boss: değme başına denge ×0,35. Yapının/parçanın her vuruşu yeni darbedir; skillin kendi tekrarı olduğu için azalma uygulanmaz (K 22:54, K 11:46-3). | K (21:13) |

#### 5.6 Kontrol (`NETH`) · Tank · işi keser (çeşit hedefin işinden) + sana tasma (1.7)

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 6-1 `NETH-VORN` | Yoğun | Önce 0,35 s şarj, sonra tek sert darbe: en yakın tek düşman, o an ne yapıyorsa o çeşit; süre 0,75 s (boştaysa 0,4 s sersemletme); yavaşlatma %68 (×1,35 derin). Tasma 0,75 s (0,4 s sersemde 0,4 s). Boss: kısa sersemletme küçük kontroldür (11:46-1), diğer çeşitler irade halkasından. | K (21:15) |
| 6-2 `NETH-VIS` | Odaklı | "Kontrol: büyü/saldırı hazırlayan düşman." Ölçü: hazırlayan yoksa en yakın düşman; çeşit çoğu zaman susturmadır (hazırlık iptal); 1,5 s; tasma 1,5 s. | K (21:13) |
| 6-3 `NETH-SKAR` | Sıçrayan | "Kontrol: düşmanın işini keser, sonra 2 düşmana daha seker; boss'ta irade kuralı geçerli." Ölçü: her biri o an ne yapıyorsa o çeşit; üçü de **sana** tasmalanır (ilk 1,5 s, sekmeler 0,75 s [test]); tasma sekme noktasından değil senden; sekme mesafesi silah menziliyle kısılır (K 21:10). Boss sekme hedefiyse küçük kontrol, yalnız denge. | K (21:13; ölçü K 21:10) |
| 6-4 `NETH-KETH` | Sabit | "Kontrol tuzak" (S4). Ölçü: basan düşman o an ne yapıyorsa o çeşidi yer (çoğu zaman kökleme); 1,5 s; aynı düşmana 3 s'de bir; tuzağa (çapalı yapıya) tasmalanır (uzunluk o anki mesafe, en çok silah menzili); yapıya bağlı boss çivilenir; yapı yıkılınca tasma aynı karede kopar. | K (21:15) |
| 6-5 `NETH-BRAN` | Yayılan | "Kontrol: çevrendeki en çok 5 düşmanın o anki işini keser; boss'ta irade kuralı geçerli." Ölçü: halka min(4 m, silah menzili) (K 21:10); her biri kendi işine göre çeşit; hepsi 1,5 s sana tasmalanır. | K (21:13; ölçü K 21:10) |
| 6-6 `NETH-HRAK` | Güdümlü | Kilitlenen düşmanı (otomatik: en yakın) kontrol etkisinin kendisi kovalar; değdiği an düşman o an ne yapıyorsa o çeşit kesilir ve **tasma kilit süresince kopmaz, en çok 8 s** (K 17:42, K 22:54). İlk 3 s iptal edemezsin; 3 s'den sonra skill çizersen kilit/tasma biter. Boss: irade kuralı; tasma kilitliyken (boss gereni sürükler). **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (A 19:41, sürpriz) |
| 6-7 `NETH-TZEK` | Tetikli | "Kontrol: düşmanın bir sonraki hareketi başladığı anda kesilir (hazırlanan büyüyü ya da atılmayı iptal etmenin en kesin yolu); boss'ta irade kuralı." Ölçü: tetik anındaki işe göre çeşit (saldırı/büyü → susturma, atılma → yavaşlatma); 1,5 s; tetik anında sana tasma. **K (21:17):** 5 s'de tetik yoksa skill boşa gider. | K (21:13) |
| 6-8 `NETH-ULTH` | Fedakarlık | "Kontrol (20:16 onaylı): boss'un irade beklemesi dolmamış olsa bile keser; aynı boss'a 60 s'de bir." Ölçü: mevcut canın %30'u; o an ne yapıyorsa o çeşit, tam güç, 1,5 s; tasma da iradeye bakmaz; 60 s kişi başı. | K (21:13) |
| 6-9 `NETH-ZUL` | İşaretli | **Tek işaret:** işaretli düşman (ya da işarete en yakın düşman, işaretten silah menzili içinde) o an ne yapıyorsa o çeşitle kesilir, 1,5 s, ve sana tasmalanır. **Zincir:** 4 s; zincire değen her düşman (en çok 5) bir kez kesilir ve zinciri kurana tasmalanır. **İşaret yok:** en yakın düşman. | K (onay 09.10 17:33) |
| 6-10 `NETH-SHIR` | Çizgisel | "Kontrol/Zayıflatma: çizgideki tüm düşmanlar." Ölçü: hattaki (silah menzili) en çok 5 düşmanın her biri kendi işine göre çeşit, 1,5 s, ve sana tasma. | K (21:13) |
| 6-11 `NETH-JEV` | Ortak | Seçilen dosta Kontrol yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki her düşman o an ne yapıyorsa o çeşit, 1,5 s, ve **sana** tasmalanır (tasma yükleyene). Boss'ta irade kuralı. **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 6-12 `NETH-ROTH` | Yörüngeli | "Kontrol: keser." Ölçü: değen düşman o an ne yapıyorsa o çeşit, 1,5 s; aynı düşmana 3 s'de bir; taşıyan kim olursa olsun sana tasmalanır. Boss'a değme küçük kontrol (11:46-1): irade harcamaz, tasma yok, denge ×0,35. | K (21:13) |

#### 5.7 Zayıflatma (`GHUL`) · Saldırı · yalnız düşman

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 7-1 `GHUL-VORN` | Yoğun | "Zayıflatma=sert kısa debuff". Ölçü: önce 0,35 s şarj, sonra tek sert darbe: en yakın tek düşman −%27, 2 s. | K (21:15) |
| 7-2 `GHUL-VIS` | Odaklı | "Zayıflatma: en çok zarar veren düşman." Ölçü: son 5 s'de en çok zarar veren düşman −%20, 4 s. | K |
| 7-3 `GHUL-SKAR` | Sıçrayan | "Zayıflatma: düşmanı zayıflatır, 2 düşmana daha yarım güçle seker." Sekme mesafesi min(6 m, silah menzili) (K 21:10). | K |
| 7-4 `GHUL-KETH` | Sabit | "Zayıflatma totem". Ölçü: 3 m içindeki düşmanlar alanda kaldıkça −%20, 6 s. | K |
| 7-5 `GHUL-BRAN` | Yayılan | "Zayıflatma: çevrendeki düşmanların verdiği etkiyi düşürür." Ölçü: min(4 m, silah menzili) (K 21:10), en çok 5, −%17 (×0,85), 4 s. | K |
| 7-6 `GHUL-HRAK` | Güdümlü | Kilitlenen düşmana (otomatik: en yakın) peşinden; değince −%20, kilit süresince (yeniden değme süreyi tazeler). Kendi süresi yok, 8 s tavanı geçerli (K 17:42, K 22:54): dünya durdurana veya 3 s'den sonra sen skill çizene kadar. İlk 3 s kilitlisin. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 7-7 `GHUL-TZEK` | Tetikli | "Zayıflatma: düşmanın bir sonraki saldırısı zayıflamış olarak çıkar." Ölçü: tetik üç olay; ilk karede −%20, 4 s; **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider. | K (sınırda) |
| 7-8 `GHUL-ULTH` | Fedakarlık | "Fedakarlık Zayıflatma (onay 09.10 11:10): Canının %30'unu ödersin; en yakın düşman (basılı tutarsan seçtiğin düşman) normal zayıflatmanın tamamını normal süresiyle ×1 alır, azalma kuralına takılmaz." Boss'un irade halkasına takılmaz; aynı boss'a kişi başı 60 s'de bir (20:16, 11:46). | K (21:15) |
| 7-9 `GHUL-ZUL` | İşaretli | **Tek işaret:** işaretli düşman (ya da işarete en yakın düşman, işaretten silah menzili içinde) −%20, 4 s. **Zincir:** 4 s; zincire değen her düşman bir kez −%17, 4 s, en çok 5. **İşaret yok:** en yakın düşman −%20. | K (onay 09.10 17:33) |
| 7-10 `GHUL-SHIR` | Çizgisel | "Kontrol/Zayıflatma: çizgideki tüm düşmanlar." Ölçü: −%17, 4 s, en çok 5. | K |
| 7-11 `GHUL-JEV` | Ortak | Seçilen dosta Zayıflatma yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki düşmanlar −%20, 4 s. **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 7-12 `GHUL-ROTH` | Yörüngeli | "Zayıflatma/Güçlendirme/Arındırma: değene." Ölçü: değen düşman −%20, 4 s; yeniden değme süreyi tazeler. | K |

#### 5.8 Güçlendirme (`DRAK`) · Destek · dost ve sen · en çok 2 kaynak

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 8-1 `DRAK-VORN` | Yoğun | "Güçlendirme=güçlü kısa buff". Ölçü: tek dost (otomatik sen) +%27 etki, +%20 hız, 2 s. | K |
| 8-2 `DRAK-VIS` | Odaklı | "Güçlendirme: en çok zarar veren dost." Ölçü: son 5 s; +%20 etki, +%15 hız, 4 s. | K |
| 8-3 `DRAK-SKAR` | Sıçrayan | "Güçlendirme: seni ya da dostu güçlendirir, 2 dosta daha yarım güçle seker." Sekme mesafesi min(6 m, silah menzili) (K 21:10). | K |
| 8-4 `DRAK-KETH` | Sabit | "Güçlendirme sancak". Ölçü: 3 m içindeki dostlar alanda kaldıkça +%20 etki, +%15 hız, 6 s. | K |
| 8-5 `DRAK-BRAN` | Yayılan | "Güçlendirme: sen ve çevrendeki dostlar güçlenirsiniz." Ölçü: min(4 m, silah menzili) (K 21:10), en çok 5, ×0,85. | K |
| 8-6 `DRAK-HRAK` | Güdümlü | Kilitlenen dosta (otomatik: en yakın dost; yoksa sen) peşinden; değince +%20 etki, +%15 hız, kilit süresince (2 kaynak sınırı; yeniden değme tazeler). Kendi süresi yok, 8 s tavanı geçerli (K 17:42, K 22:54). İlk 3 s kilitlisin; sonra skill kilidi bozar. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 8-7 `DRAK-TZEK` | Tetikli | "Güçlendirme: dostun bir sonraki saldırısı güçlenmiş olarak çıkar." Ölçü: işaret sende ya da seçtiğin dostta; tetik üç olay; +%20/+%15, 4 s. | K (zayıf) |
| 8-8 `DRAK-ULTH` | Fedakarlık | Canının %30'unu ödersin; en yakın dost (basılı tutarsan seçtiğin dost) normal güçlendirmenin tamamını ×1 alır, azalma kuralına takılmaz; 2 kaynak sınırına uyar (K 22:15). | K (DÜZELTME 09.10 11:11; metin temizlendi 19:4x) |
| 8-9 `DRAK-ZUL` | İşaretli | **Tek işaret:** işaretli dost (ya da işarete en yakın dost; işaretten min(silah, 6 m)) +%20 etki, +%15 hız, 4 s. **Zincir:** 4 s; zincire değen her dost bir kez +%17/+%13, 4 s, en çok 5. **İşaret yok:** sen. 2 kaynak sınırı geçerli. | K (onay 09.10 17:33) (sınırda) |
| 8-10 `DRAK-SHIR` | Çizgisel | "Güçlendirme: çizgideki dostlar." Ölçü: +%17 etki, +%13 hız, 4 s. | K |
| 8-11 `DRAK-JEV` | Ortak | Seçilen dosta Güçlendirme yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki dostlar +%20 etki, +%15 hız, 4 s (2 kaynak sınırı). **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 8-12 `DRAK-ROTH` | Yörüngeli | "Zayıflatma/Güçlendirme/Arındırma: değene." Ölçü: değen dost +%20/+%15, 4 s; yeniden değme tazeler. | K |

#### 5.9 Arındırma (`SIL`) · Destek · yalnız dosttan zararlı etki · düşmana işlemez

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 9-1 `SIL-VORN` | Yoğun | Tek dosttan (otomatik sen) en son konan 2–3 zararlı etki birden sökülür (kesin sayı testte). | K (onay 09.10 17:27) |
| 9-2 `SIL-VIS` | Odaklı | Üstünde en çok zararlı etki olan dosttan (sen dahil; eşitlikte en yakın) en son zararlı etki sökülür. | K (onay 09.10 17:27) |
| 9-3 `SIL-SKAR` | Sıçrayan | Seni (otomatik) ya da seçtiğin dostu temizler; sonra min(6 m, silah menzili) içinde (K 21:10) üstünde zararlı etki olan en yakın 2 dosta seker ve her birinden en son zararlı etki sökülür. Uygun dost yoksa zararlı etkisi olan dost yapıya seker, o da yoksa biter. | K (onay 09.10 17:27) |
| 9-4 `SIL-KETH` | Sabit | Arınma taşı: 3 m içindeki dostlardan ve senden saniyede bir en son zararlı etki sökülür, 6 s. Düşman mermisini ve alanını silmez; alanın içindeki dosta verdiği durumu söker. | K (onay 09.10 17:27) |
| 9-5 `SIL-BRAN` | Yayılan | Halkadaki (min(4 m, silah menzili), K 21:10) en çok 5 dosttan (sen dahil) en son zararlı etki sökülür. | K (onay 09.10 17:27) |
| 9-6 `SIL-HRAK` | Güdümlü | Kilitlenen dosta (otomatik: en yakın dost; yoksa sen) peşinden; değince ve sonra saniyede bir en son zararlı etki sökülür. Düşmana işlemez. Kendi süresi yok (K 17:42), 8 s tavanı geçerli. İlk 3 s kilitlisin; sonra skill kilidi bozar. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 9-7 `SIL-TZEK` | Tetikli | İşaret sana (basılı tutarsan seçtiğin dosta) konur. İşaretli kişi bir sonraki hamlesine başladığı an en son zararlı etkisi sökülür; **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider. Düşmana konamaz. | K (onay 09.10 17:27) (sınırda) |
| 9-8 `SIL-ULTH` | Fedakarlık | Canının %30'unu ödersin; en yakın dosttan en son zararlı etki sökülür, yani normal Arındırma'nın aynısı. Fedakarlık'ın aşacağı bir engel yok: sökme zaten azalmaz ve boss'a işlemez; yalnız bedel ödenir. | K (21:15; onay 09.10 11:11, zayıf) |
| 9-9 `SIL-ZUL` | İşaretli | "işareti yalnız İşaretli-Arındırma koyar (fiil işlemez)" (9 Ekim). Ölçü: seçilen yere (otomatik: durduğun zemin) işaret konur; koyma erişimi silah menzili, dosta min(silah, 6 m) (15:47); dosta, düşmana (boss dahil) ve zemine; 20 s [test], kullanılınca silinmez; kişi başı 4, 5. konunca en eskisi silinir; takımın herkesi kullanabilir. | K (ölçü dışı: fiil işlemez) |
| 9-10 `SIL-SHIR` | Çizgisel | Hattaki (silah menzili, 1 m) en çok 5 dosttan en son zararlı etki sökülür, anında. | K (onay 09.10 17:27) (zayıf) |
| 9-11 `SIL-JEV` | Ortak | Seçilen dosta Arındırma yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki dostlardan (sen dahil) en son zararlı etki sökülür. Düşmana işlemez. **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 9-12 `SIL-ROTH` | Yörüngeli | "Zayıflatma/Güçlendirme/Arındırma: değene." Ölçü: değen dosttan en son zararlı etki sökülür, aynı dosta saniyede bir, 3 s. Taşıyan sen ya da seçtiğin dost; düşman taşıyamaz; taşıyan kendini temizleyemez. | K (21:13) |

#### 5.10 Yansıma (`MIR`) · Tank · gelen zararlı etkiyi kaynağına döndürür · varsayılan sana; dosta basılı tutup sürükleyince (K 21:09)

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 10-1 `MIR-VORN` | Yoğun | "Yansıma: 0,45 s dar pencere, sert dönüş." Ölçü: pencerede gelen zararlı etki kaynağa 0,85 × 1,35 oranla döner (en çok 1); boss tek atma tavanı (%5) Yoğun'da da geçerli (K 22:15). | K |
| 10-2 `MIR-VIS` | Odaklı | "Yansıma: en çok zarar alan dost." Ölçü: son 3 s'de en çok zarar alan dosta 2 s yansıma (K 21:09: Odaklı kendiliğinden seçer). | K (21:09) |
| 10-3 `MIR-SKAR` | Sıçrayan | "Yansıma: geri dönen etki kaynağa çarptıktan sonra 2 düşmana daha yarım güçle seker." Sekme mesafesi min(6 m, silah menzili) (K 21:10). | K |
| 10-4 `MIR-KETH` | Sabit | "Yansıma ayna duvarı" (S4). Ölçü: duvara **çarpan** düşman mermisi ve yakın vuruş saldırısı yapı ömrü boyunca kaynağına döner. **Alan duvara çarpmaz** (K 16:52), bu yüzden alan saldırısı ve alan etkisi dönmez (KG-35). | K |
| 10-5 `MIR-BRAN` | Yayılan | "Yansıma: etrafında 2 saniye duran bir halka kurar; sana ya da içindeki dostlara gelen zararlı etki kaynağına 0,85 kat geri döner." Ölçü: oran = taban 0,85 × Yayılan 0,85 ≈ 0,72; boss tek atma tavanı (%5) geçerli (K 22:15). Halka min(4 m, silah menzili) (K 21:10). | K (21:09) |
| 10-6 `MIR-HRAK` | Güdümlü | Kilitlenen hedefe (otomatik: en yakın dost, dost yoksa sen) peşinden yürüyen yansıma (koruma eşlikçisi); gelen zararlı etki kaynağına döner. Kendi süresi yok (K 17:42), 8 s tavanı geçerli: dünya durdurana veya 3 s'den sonra sen skill çizene kadar. İlk 3 s kilitlisin. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (A 19:41, sürpriz; K 21:09) |
| 10-7 `MIR-TZEK` | Tetikli | "Yansıma: düşmanın bir sonraki saldırısı kendisine geri döner." Ölçü: işaret en yakın düşmanda; tetik anındaki saldırısı kaynağına `reflect_ratio` oranında döner. **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider (yedek hâl yok). | K (21:15) |
| 10-8 `MIR-ULTH` | Fedakarlık | Canının %30'unu ödersin; en yakın dosta (basılı tutarsan seçtiğin dosta) 2 s'lik normal yansıma kurulur; yakalanan etki kaynağına tam ×1 döner, azalma kuralına takılmaz. **K (21:15/21:17):** geçerli dost yoksa skill boşa gider; dost menzil dışındaysa yürür. Boss'un kendi kontrolü geri dönerse irade halkasını aşar; aynı boss'a kişi başı 60 s'de bir (11:46). | K (onay 09.10 11:11; K 21:09/21:15) |
| 10-9 `MIR-ZUL` | İşaretli | **K (21:09):** Yansıma önce sana kurulur (2 s). Basılı tutup sürükleyince seçilen dosta (işaret varsa işaretli / işarete en yakın dost; işaretten min(silah, 6 m)) kurulur; geçerli dost yoksa skill boşa gider (K 21:15/21:17). **Zincir:** 4 s ayna hattı; ona çarpan düşman saldırısı ve mermisi kaynağına döner. | K (21:09, 21:17b) |
| 10-10 `MIR-SHIR` | Çizgisel | "Yansıma: çizgi 2 s kalır, düşman saldırılarını geri çevirir (alan kontrolü)." Ölçü: hat silah menzili, 1 m. | K |
| 10-11 `MIR-JEV` | Ortak | Seçilen dosta Yansıma yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki dostlara 2 s yansıma (K 21:09). **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider. | K (onay 09.10 17:15; K 21:09/21:15) |
| 10-12 `MIR-ROTH` | Yörüngeli | "Yansıma: çarpan mermi döner." Ölçü: parçaya çarpan düşman mermisi kaynağına döner, 3 s. | K |

#### 5.11 Çağırma (`ORN`) · Saldırı · yardımcı yalnız düşmana saldırır

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 11-1 `ORN-VORN` | Yoğun | Anında (Yoğun şarjı yalnız düşmana işleyen fiillerde, K 19:41): yanında 2,5 s yaşayan, 1,35 kat güçlü tek bir yardımcı çıkar ve en yakın düşmana kendi başına saldırır. | K (A 19:41, sürpriz; şarj K 19:41 Yoğun kararına göre yok) |
| 11-2 `ORN-VIS` | Odaklı | "Çağırma: canı en az düşmana gider." Ölçü: yardımcı canı en az düşmanın yanında doğar ve 5 s ona saldırır. | K |
| 11-3 `ORN-SKAR` | Sıçrayan | "Çağırma: yardımcı yürümez, hedeften hedefe sıçrayarak saldırır." Ölçü: ilk tam, 2 sekme yarım. Sekme mesafesi min(6 m, silah menzili) (K 21:10). | K |
| 11-4 `ORN-KETH` | Sabit | "Çağırma kule" (S4). Ölçü: yerinde duran yardımcı yapı; min(6 m, silah menzili) (K 21:10) içindeki düşmanlara yapı ömrü boyunca saldırır; canı var (yapı). | K |
| 11-5 `ORN-BRAN` | Yayılan | "Çağırma: tek büyük yardımcı yerine etrafına 3 küçük yardımcı çıkar; her birinin gücü testte belirlenir." | K |
| 11-6 `ORN-HRAK` | Güdümlü | Yardımcı kilitlenen düşmana (otomatik: en yakın) peşinden saldırır; hedef değiştiremezsin. Kendi süresi yok, 8 s tavanı geçerli (K 17:42, K 22:54): yardımcı vurana / dünya durdurana veya 3 s'den sonra sen skill çizene kadar kilitli kovalar; kilit bitince kalan sürede (toplam yardımcı ömrü 5 s [test]) normal AI. Tutmaz, köklemez, bağlamaz. İlk 3 s sen de kilitlisin. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) |
| 11-7 `ORN-TZEK` | Tetikli | "Çağırma: yardımcı, hedef bir sonraki hamlesini yaptığı yerde ortaya çıkar." Ölçü: işaret en yakın düşmanda; tetik anında yardımcı orada doğar, 5 s saldırır. | K |
| 11-8 `ORN-ULTH` | Fedakarlık | "Fedakarlık Çağırma (onay 09.10 11:12): Canının %30'unu ödersin; yanına normal yardımcı çıkar, normal süresi kadar kalır; vuruşları azalma kuralına takılmaz, her biri tam ×1." Yardımcı boss'un irade halkasına takılmaz; aynı boss'a kişi başı 60 s'de bir (20:16, 11:46). Yardımcının canı var (K 21:12). | K (21:15; → A-1 kapalı) |
| 11-9 `ORN-ZUL` | İşaretli | **Tek işaret:** yardımcı işarette doğar, 5 s savaşır (işaret düşmandaysa ona saldırır), erişimi işaretten silah menzili. **Zincir:** ilk işarette doğar, bağ boyunca hücum eder (yoldaki düşmanlara bir kez ×0,85), ikinci işarette kalır, 5 s savaşır. **İşaret yok:** yanında doğar. | K (onay 09.10 17:33) |
| 11-10 `ORN-SHIR` | Çizgisel | "Çağırma: yardımcı çizgi boyunca hücum." Ölçü: yoldaki düşmanlara ×0,85, ucunda kalır ve 5 s savaşır. | K |
| 11-11 `ORN-JEV` | Ortak | Seçilen dosta Çağırma yüklenir; dost 5 s içinde skill atınca yardımcı o skillin vardığı yerde doğar, 5 s savaşır. **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 11-12 `ORN-ROTH` | Yörüngeli | "Çağırma: yardımcı etrafta döner." Ölçü: taşıyanın etrafında 3 s döner, değdiği düşmana ×0,35, aynı düşmana saniyede bir. | K |

#### 5.12 Zaman (`KHOR`) · Destek · hedef kaydın alındığı ana döner (can + konum) · etkilere dokunmaz · ölüyü yalnız kayıt direği getirir

| No · Flaş | Sıfat | Ne olur | Durum |
|---|---|---|---|
| 12-1 `KHOR-VORN` | Yoğun | Şarj yok, anında: sen (otomatik) ya da seçtiğin tek dost **1,5 s önceki** can ve konumuna döner (Yoğun "süre yarı"). ×1,35 kayıttaki canı aşamadığı için etkisizdir. | K (21:15) |
| 12-2 `KHOR-VIS` | Odaklı | Canı (yüzde) en az dost (sen dahil; eşitlikte en yakın; ölü atlanır) 3 s önceki can ve konumuna döner. | K (onay 09.10 17:28) |
| 12-3 `KHOR-SKAR` | Sıçrayan | İlk hedef (otomatik sen) 3 s önceki can ve konumuna tam döner; sonra min(6 m, silah menzili) içindeki (K 21:10) en yakın 2 canlı dosta seker, onlar 3 s önceki konuma döner, can farkı ×0,5. | K (onay 09.10 17:28) |
| 12-4 `KHOR-KETH` | Sabit | "kayıt direği; dikildiği an çevresindeki dostları kaydeder, kırılınca/süre bitince dostlar kayıttaki can ve yere döner." (21:46) + "Kayıt direği ölen dostu da geri getirir." (11:17). Ölçü: 3 m içindeki dostlar (sen dahil), 6 s; kayıttan sonra ölen dost dönüş anında kayıttaki can ve konumla dirilir; etkilere dokunmaz. | K (→ A-9) |
| 12-5 `KHOR-BRAN` | Yayılan | Halkadaki (min(4 m, silah menzili), K 21:10) en çok 5 dost (sen dahil) 3 s önceki konumuna döner, can farkı ×0,85. | K (onay 09.10 17:28) |
| 12-6 `KHOR-HRAK` | Güdümlü | Kilitlenen dosta (otomatik: en yakın dost; yoksa sen) peşinden; **ilk değmede** kilit anından 3 s önceki can ve konumuna döner (değince biter — "vurdu"). Etkilere dokunmaz; ölüyü getirmez. Kendi süresi yok, 8 s tavanı geçerli (K 17:42, K 22:54) ta ki değene, dünya durdurana veya 3 s'den sonra sen skill çizene. İlk 3 s kilitlisin. **Sigortalar (K 22:15):** hedef ölür ya da kaybolursa kilit biter; bütün kilitler en çok 8 s (K 22:54); temel vuruş ve dodge kilidi bozmaz. | K (onay 09.10 17:42) (zayıf) |
| 12-7 `KHOR-TZEK` | Tetikli | İşaret sana (basılı tutarsan seçtiğin dosta) konur. İşaretli kişi bir sonraki hamlesine başladığı an (ilk kare) tetikten 3 s önceki can ve konumuna döner; hamle iptal olmaz. **K (21:15/21:17):** 5 s'de tetik yoksa skill boşa gider. Düşmana konamaz. | K (onay 09.10 17:28) (sınırda; → A-10) |
| 12-8 `KHOR-ULTH` | Fedakarlık | Mevcut canın %30'u; en yakın dost (basılı tutarsan seçtiğin dost; dost yoksa sen) 3 s önceki can ve konumuna döner; azalmaya takılmaz, can farkı her zaman tam. Aynı dosta 3 s'de tek Fedakarlık. | K (onay 09.10 17:28) (zayıf) |
| 12-9 `KHOR-ZUL` | İşaretli | Kayıt anı = işaretin konduğu an (9 Ekim). **Tek işaret:** işaretli dost (ya da işarete en yakın dost; işaretten min(silah, 6 m)) işaretin konduğu andaki can ve konumuna döner. **Zincir:** 4 s; zincire değen her dost (en çok 5) bir kez ilk seçilen işaretin anına döner. **İşaret yok:** sen, 3 s önce. İşaret 20 s yaşadığı için kayıt en çok 20 s geriye gider. | K (onay 09.10 17:28) (→ A-11) |
| 12-10 `KHOR-SHIR` | Çizgisel | Hattaki (silah menzili, 1 m) en çok 5 dost 3 s önceki konumuna döner, can farkı ×0,85. Otomatik: en yakın dost. | K (onay 09.10 17:28) (sınırda; → A-14) |
| 12-11 `KHOR-JEV` | Ortak | Seçilen dosta Zaman yüklenir; dost 5 s içinde skill atınca o skillin şeklindeki dostlar (sen dahil) 3 s önceki can ve konumuna döner, ×1,0. Etkilere dokunmaz; ölüyü getirmez. **K (21:15/21:17):** 5 s'de skill yoksa yük boşa gider (yedek hâl yok). | K (onay 09.10 17:15) |
| 12-12 `KHOR-ROTH` | Yörüngeli | Değen dost 3 s önceki can ve konumuna döner; aynı dosta 3 s'de bir (yani 3 s'lik yörüngede en çok bir kez). Taşıyan etkilenmez. Düşmana işlemez. | K (onay 09.10 17:28) |

**Hücre durumu sayımı:** 144 hücre: **K 125** (içinde bu tur onaylı `K (onay 09.10 …)`: 58) · **K + V4 11** · **T3 0** · **T3 + V4 1** · **V4 7** (hâlâ onay bekler). Zayıf 12 (2-7, 2-10, 3-5, 3-12, 4-3, 4-6, 4-11, 8-7, 9-8, 9-10, 12-6, 12-8).

---

### 6. Silah × fiil: 120 hücre

Kaynak: `silah-fiil-120.md` (GameDesign, 9 Ekim **20:36 sürümü**; yedeği `silah-fiil-120.md.bak`). Tarz satırları, ritim satırları ve 120 hücre **aynen** alındı, hiçbir hücre yeniden yazılmadı. Kurala aykırı görünen yerler hücrenin içinde değiştirilmedi; ⚠ ile işaretlendi ve `acik-sorular.md`'ye yazıldı. Her hücre **bu dövüşçü bu fiili nasıl yapar** sorusunu cevaplar. Hedefi, yönü, alanı, süreyi ve biçimi sıfat verir; menzili ve ritmi silah verir (Bölüm 4).

**Bütün hücreler için ortak okuma kuralları** (hücre metnini değiştirmez, nasıl okunacağını söyler):
- **Mekanik kanun yok (K 20:29):** hücreler görünüm ve harekettir. Hücredeki hiçbir söz (ör. "en sert", "kesilmez", "ağır") ayrı bir oyun kuralı doğurmaz; sayılar ritim tablosundan (4.2) gelir.
- **Koruma:** silah yalnız duruşun nasıl göründüğünü ve hareketini çizer. Kalkan vermez, dosta varmaz, dost bağı kurmaz, araya girmez (K 16:49, 2.1). "Okla savuşturur gibi", "darbeyi gövdesiyle karşılar" gibi ifadeler görüntüdür; tek tek mermiyi durdurma kuralı değildir (Yörüngeli parçası hariç, o sıfatın biçimi).
- **Kuvvet:** her hücre "senden dışa" der; kural 1.8. Hiçbir silah çekmez.
- **Kontrol:** "bağın ucuna kilitler" tasmadır (1.7). `silah-fiil-120.md` Notlar, aynen: "'Bağın ucu' sıfattan gelir: çoğu sıfatta sensin, Sabit'te tuzak yapısıdır." Çeşidi hedefin o anki işi belirler. **Dikkat çekme (taunt) yoktur** (K 15:46).
- **Hareket:** her silahta yol alarak; ışınlanma yalnız Çizgisel-Hareket'te (K 15:55). Yolculuk hızı ve yolda skill 4.3'te.
- **Zaman:** yalnız kayda dönüş. `silah-fiil-120.md`, aynen: "Zaman satırları sadece geri sarmayı gösterir (dondurma ve ileri sarma kaldırıldı)."
- **Çağırma:** `silah-fiil-120.md`, aynen: "Çağırma'da yaratığın görünümü silahın tarzından gelir (Yumruk'ta dövüşçü gölgesi, Çekiç'te taş dev); yaratığın sayısı ve davranışı sıfattan gelir."
- **Ön süre adımı Hareket değildir:** Kalkan'ın "ağır adım"ı ritmin parçasıdır (4.2), silah menzilini aşmaz, Hareket sayılmaz. Kılıç Zarar artık yerinde iaido'dur; eski "ileri atılıp kesiş" kalktı.
- **Animasyon:** fiziksel fiil hücreleri (Zarar, Koruma, Kuvvet, Hareket) silaha özel koreografidir; büyü fiili hücreleri basit büyü animasyonunun görünüşüdür (Bölüm 8.2).

#### 6.1 Yumruk (3 m) — Seri Boksör
Tarz: seri boksör. Her şey kısa, keskin ve peş peşe; tek vuruş yerine üçlü seri. Ritim: 0,10 s ön süre, 3 vuruş, yürürken.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Kroşe ve aparkat serisi; hasar birkaç hızlı vuruşa bölünür |
| Şifa | Yerinde iki kısa yumruk sallar; kordan yankısı dostun omzuna "kendine gel" diye vurur, şifa vuruşla girer |
| Hareket | Kısa atılış: boksör dalışıyla yol boyunca keskin bir sıçrayış, en fazla 3 m |
| Koruma | Yerinde yan adımla guard alır; ritmi hızlı açılıp hemen kapanan guard, ardından karşı vuruşa hazır |
| Kuvvet | Jab, jab, direkt; son yumrukla hedef senden dışa itilir |
| Kontrol | Yerinde ense tutuşu yapar; kordan bir el hedefin ensesini kavrar ve onu bağın ucuna kilitler |
| Zayıflatma | Böbrek ve kaburgaya peş peşe kısa vuruşlar; hedef sendeler |
| Güçlendirme | Eldivenlerini iki kez tokuşturur; kordan yankısı dostun yumruğuna tokuşur |
| Arındırma | Yerinde iki hızlı şaplak savurur; yankısı dostun sırtında patlar, kötü etkiyi silkeler |
| Yansıma | Gelen vuruşu eldivenle karşılayıp karşı yumrukla geri yollar (parry-counter) |
| Çağırma | Yere yumruk vurur, kordan bir dövüşçü gölgesi fırlar ve onunla birlikte seri atar |
| Zaman | Geriye doğru bir adım ve guard; dost kayda, boksörün köşesine çekilir gibi döner |

#### 6.2 Yay (14 m) — Avcı (Levi)
Tarz: sabırlı avcı ve ip ustası (Levi). Ger, nişan, bırak; hareketi iple. Ritim: 0,35 s otomatik germe, erken bırakma yok (K 22:15).
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Ger, nişan, bırak (germe otomatik 0,35 s, K 22:15) |
| Şifa | Ucunda kor rünü olan ok dosta saplanır, saplandığı an iyileştirir |
| Hareket | Yolun ucuna ipli ok atar; ipe asılıp Levi gibi kendini çeker |
| Koruma | Yerinde yayını çapraz tutar; duruş boyunca gelen mermiyi okla savuşturur gibi durur (görüntü) |
| Kuvvet | Ağır uçlu ok; çarptığı hedefi senden dışa savurur |
| Kontrol | Hedefe iple ok saplar; ip hedefi bağın ucuna kilitler |
| Zayıflatma | Silah koluna ince bir ok; vuruşu körelir |
| Güçlendirme | Dostun yanına ok saplar, ok yanarak güç verir |
| Arındırma | Dostun üstünden sıyıran bir ok, kötü etkiyi koparıp götürür |
| Yansıma | Gelen mermiye ok atıp havada çarpıştırır, mermi sahibine döner |
| Çağırma | Göğe ok atar, düştüğü yerde kordan bir şahin doğar ve saldırır |
| Zaman | Yayı geri gerer, dost gerilen yay gibi kayda doğru çekilir |

#### 6.3 Büyü Kitabı (9 m) — Okuyan
Tarz: okuyan. Kitabı açar, sözü yüksek sesle okur; harfler ve sayfalar uçar. Ritim: 0,60 s okuma, okurken yürür; harfler sözden kopup hedefe uçar.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Lanet satırını okur, kor harfler hedefe çarpar |
| Şifa | Şifa duasını okur, harfler dostun yaralarına yapışıp kapanır |
| Hareket | Okurken bedeni sayfalara dağılır; sayfa sürüsü yol boyunca hızla, görünür uçar (ışınlanma değil), yolun ucunda sayfalardan toplanır |
| Koruma | Yerinde okuyarak duruşa geçer; harfler gövdesine yazılır, darbeyi kendi gövdesiyle karşılar (görüntü) |
| Kuvvet | Kitabı sertçe kapatır; kapanan sayfaların sesi hedefi senden dışa iter |
| Kontrol | Hedefin adını okur; harfler zincir olup hedefi bağın ucuna kilitler |
| Zayıflatma | Hedefin kusurlarını okur; harfler silahına kazınır, vuruşu körelir |
| Güçlendirme | Kahramanlık destanı okur, harfler dostun silahına işlenir |
| Arındırma | Sayfayı yırtar, dostun üstündeki kötü etki yırtılan sayfayla gider |
| Yansıma | Saldırıyı okuyup tersten okur, harfler saldırıyı geri çevirir |
| Çağırma | Bir sayfayı yere atar, sayfadan harflerden bir yaratık kalkar |
| Zaman | Sayfaları geri çevirir, dost geri çevrilen sayfayla kayda döner |

#### 6.4 Kılıç (6 m) — Samuray (Zenitsu)
Tarz: samuray / Zenitsu. Tek, ani, şimşek kesiş; iaido kınsız (el kabzada, kılıç geride) (K 21:05). Ritim: 0,20 s ön süre, tek kesiş, 0,20 s toparlanma.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Yerinde el kabzada, tek şimşek kesiş (iaido, kınsız); kesik izi hedefte parlar |
| Şifa | Kılıcı yere saplar, diz çöker; kor kılıçtan dosta akar |
| Hareket | El kabzada eğilir; şimşek gibi yol boyunca atılır (Zenitsu), geçtiği yerde sarı kesik izi kalır; yol görünür, ışınlanma değil |
| Koruma | Yerinde kılıcı yarım çeker (kınsız); gelen darbeyi tek ani hamleyle savuşturur |
| Kuvvet | Kılıcın yassı yüzüyle tek sert savuruş; hedef senden dışa savrulur |
| Kontrol | Yerinde kılıcı ileri uzatır; kordan bir kılıç hedefin boynuna dayanır ve onu bağın ucuna kilitler |
| Zayıflatma | Silah kolunu sıyıran kısa kesik; vuruşu körelir |
| Güçlendirme | Kılıcı kaldırıp selam verir, dostun silahı parlar |
| Arındırma | Yerinde tek dairesel kesiş; kordan yankısı dostun etrafında döner, kötü etkiyi keser atar |
| Yansıma | Gelen saldırıyı tek ani kesişle karşılar, sahibine savurur |
| Çağırma | Kılıcı havaya atar, havada kordan bir samuray tutar ve saldırır |
| Zaman | Kılıcı geri çeker, kabzada kilitler (çıt); dost kayda döner |

#### 6.5 Küre (7 m) — Kor Sıkan
Tarz: kor sıkan. Küreyi avuçta ezer, ışık anında patlar; sonra kısa soğuma. Kürenin konumu yok, hep avuçta. Ritim: 0 s ön süre, 0,40 s toparlanma (`toparlanma_yurume_carpani` 0,5; K 21:10b).
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Küreyi sıkar, avuçtan kor fışkırır |
| Şifa | Küreyi avuçta sıkar; sıcak ışık anında dosta patlar, iyileştirir |
| Hareket | Küreyi avucunda ezer; patlamanın itişiyle yol boyunca savrulur |
| Koruma | Küreyi göğsünde sıkar; kürenin parlaması duruşu besler, gelen darbeyi ışıkla karşılar (görüntü) |
| Kuvvet | Küreyi avuçta ezer; ışık patlaması hedefi senden dışa çarpar |
| Kontrol | Küreyi sıkar; kor bir halka hedefi bağın ucuna kilitler |
| Zayıflatma | Küreyi sıkar, isli kor hedefe fışkırır; hedef kavrulur, vuruşu körelir |
| Güçlendirme | Küreyi sıkar; ateşi anında dosta sıçrar, dost tutuşur |
| Arındırma | Küre parlayıp yanar, dostun üstündeki kötü etkiyi yakar |
| Yansıma | Küreyi kaldırır, gelen saldırı kürede kırılıp sahibine döner |
| Çağırma | Küreyi avuçta ezer; fışkıran kordan bir yaratık doğar (doğum yeri sıfattan) |
| Zaman | Küre soğur ve ters yanar, dost kayda döner |

#### 6.6 Çekiç (6 m) — Ağır Savaşçı (Gimli/Thor)
Tarz: Gimli / Thor. Ağır; kaldır, bekle, çak. Çekici fırlatır ve geri çağırır. Ritim: 0,55 s kaldırma, tek darbe, 0,50 s toparlanma.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Kaldır, bekle, zınk diye çak |
| Şifa | Çekici yere hafifçe vurur, yerden yükselen sıcaklık dostu iyileştirir |
| Hareket | Çekici yol boyunca fırlatır; Thor gibi peşinden uçar, yolun ucunda çekici yakalar |
| Koruma | Yerinde çekici tepesinde döndürür; dönen çekiç duruş boyunca gelen saldırıları savurur (görüntü) |
| Kuvvet | Çekici öne doğru savurur; hedef senden dışa fırlar |
| Kontrol | Çekici çakar; yer çatlağı hedeften bağın ucuna uzanır ve hedefi oraya kilitler |
| Zayıflatma | Silahına vurup ezer; vuruşu körelir |
| Güçlendirme | Çekici iki eliyle kaldırıp yere çakar, savaş çığlığı atar (Gimli) |
| Arındırma | Yere vurur, sarsıntı dostun üstündeki kötü etkiyi silkeler |
| Yansıma | Gelen saldırıya çekiçle vurup geri gönderir (beyzbol hissi) |
| Çağırma | Yeri döver, topraktan bir taş dev yükselir |
| Zaman | Çekici fırlatıp geri çağırır; çekiç eline dönerken dost da kayda döner |

#### 6.7 Top (18 m) — Topçu
Tarz: topçu. Nişan, ateş, geri tepme; gülle düştüğü yerde açılır. Geri tepme yalnız görüntüdür (Hareket dışında oyuncuyu kaydırmaz). Ritim: 0,40 s nişan, 0,35 s toparlanma.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Nişan, ateş; yerinde geri tepmeyle sarsılır, gülle düştüğü yerde patlar |
| Şifa | Kor dolu yumuşak bir mermi dostun yanına düşer, açılınca iyileştirir |
| Hareket | Namluyu yolun tersine ateşler; patlama seni yol boyunca fırlatır |
| Koruma | Namluyu çapraz tutup duruşa geçer; gelen mermiyi namluyla savuşturur gibi durur (görüntü) |
| Kuvvet | Ağır gülle hedefe çarpar, onu senden dışa iter |
| Kontrol | Zincirli gülle hedefe dolanır; zincir hedefi bağın ucuna kilitler |
| Zayıflatma | Duman mermisi, hedef isle kaplanır |
| Güçlendirme | Dostun yanına sancak mermisi atar, sancak dikilir |
| Arındırma | Su mermisi dostun üstünde patlar, kötü etkiyi yıkar |
| Yansıma | Gelen mermiyi namluyla karşılayıp geri ateşler |
| Çağırma | Yumurta gibi bir gülle atar, düştüğü yerde yaratık çıkar |
| Zaman | Namluyu geri çeker, gülle namluya geri yuvarlanır; dost kayda döner |

#### 6.8 Asa (15 m) — Büyücü
Tarz: ışın büyücüsü. Asadan ışın akıtır, havaya rün çizer. Ritim: 0,30 s akıtma, 0,20 s toparlanma.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Asadan kısa kor ışını |
| Şifa | Asadan dosta yumuşak bir ışık akışı |
| Hareket | Asayı yola doğrultur, ışın yolu açılır; ışının üstünde süzülerek gider |
| Koruma | Asaya yaslanıp yerinde duruşa geçer; asanın önünde açılan kor halka duruştur, kalkan değildir (görüntü) |
| Kuvvet | Asayı ileri doğrultur; ışın darbesi hedefi senden dışa iter |
| Kontrol | Havaya rün çizer; rünün ışını hedefi bağın ucuna kilitler |
| Zayıflatma | Hedefin üstüne solgun bir rün asar |
| Güçlendirme | Asanın kristalini dosta doğrultur, dost parlar |
| Arındırma | Asanın ucuyla dostun etrafında halka çizer, halka kötü etkiyi eritir |
| Yansıma | Asanın önünde dönen bir rün aynası açar |
| Çağırma | Havaya rün çizer, rünün içinden yaratık çıkar |
| Zaman | Asayı ters çevirir, rünler geri akar, dost kayda döner |

#### 6.9 Tılsım (17 m) — Mühürcü
Tarz: mühürcü. Avucundan mühür salar; mühür hedefin ayağına düşer, 0,5 s sonra açılır. Ritim: 0 s ön süre, 0,45 s toparlanma (K 22:15); gecikme mühürde.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Avucunu sıkar; mühür hedefin ayağına düşer, 0,5 s sonra patlar |
| Şifa | Avucunu açar; şifa mührü dostun ayağına düşer, 0,5 s sonra açılır |
| Hareket | Mühür yolun ucuna düşer; 0,5 s sonra parlayıp seni yol boyunca kendine çeker |
| Koruma | Avucundaki mührü göğsüne basar; mühür duruş boyunca parlar, gelen darbeyi karşılar (görüntü) |
| Kuvvet | Mühür hedefin ayağına düşer, 0,5 s sonra patlayıp hedefi senden dışa savurur |
| Kontrol | Bağlama mührü hedefin ayağına düşer; 0,5 s sonra hedefi bağın ucuna kilitler |
| Zayıflatma | Lanet mührü hedefin ayağına düşer, 0,5 s sonra açılır |
| Güçlendirme | Güç mührü dostun ayağına düşer, 0,5 s sonra açılır |
| Arındırma | Arınma mührü dostun ayağına düşer; 0,5 s sonra kırılır, kötü etkiyi de götürür |
| Yansıma | Ters mühür korunanın ayağına düşer; 0,5 s sonra açılır, gelen saldırı mühürde tersine döner |
| Çağırma | Çağırma mührü yere düşer, 0,5 s sonra içinden yaratık çıkar |
| Zaman | Geri sarma mührü dostun ayağına düşer; 0,5 s sonra ters dönerek açılır, dost kayda döner |

#### 6.10 Kalkan (3 m) — Muhafız (Tank)
Tarz: tank. Ağır adım, omuz, gövdeyi darbenin önüne koyma. Ritim: 0,30 s ağır adım, tek darbe, 0,45 s toparlanma.
| Fiil | Nasıl yapar |
|---|---|
| Zarar | Ağır bir adım, kalkanın kenarıyla tek sert indirme |
| Şifa | Yerinde kalkanı göğsüne çekip yere dayar; kordan yankısı dostta belirir, dost yerinde toparlanır |
| Hareket | Kalkanı omza koyup omuz hücumu; ağır adımlarla en fazla 3 m |
| Koruma | Yerinde ağır adımla çöker, kalkanı öne dayar; darbeyi gövdesiyle karşılar, duruş boyunca yavaş yürür |
| Kuvvet | Kalkanı öne bastırıp ağır adımla iter; hedef senden dışa itilir |
| Kontrol | Yerinde kalkanı ileri bastırır; kordan bir kalkan hedefe yaslanır ve onu bağın ucuna kilitler |
| Zayıflatma | Kalkanın kenarıyla silah koluna vurur; vuruşu körelir |
| Güçlendirme | Kalkanı yere vurup savaş çığlığı atar; dostlar yerlerinde cesaretlenir |
| Arındırma | Kalkanı yere vurup silkeler; kordan yankısı dostun üstünde çınlar, kötü etki dökülür |
| Yansıma | Kalkanı açıyla tutar, gelen saldırı sektirilip sahibine döner |
| Çağırma | Kalkanı yere diker, kalkanın arkasından bir muhafız yükselir |
| Zaman | Kalkanı geri çeker, dost kalkanın arkasındaki kayda döner |

**`silah-fiil-120.md` Notlar** (aynen; ⚠ işaretli olanlar açık soruya gitti):
- **Genel görünüm kuralı (9 Ekim):** silah hareketi hep senin yerinde yapılır. Etkinin kurulduğu yer sen değilsen (işaret, korunan dost, tuzak/yapı, yardımcının doğduğu yer), hareketin kordan yankısı orada belirir. Görünümdeki yön ve bağ, kuralın yönüne ve bağına çizilir. Bu kural Yansıma'da korunan dostu ve Çağırma'da doğum yerini de kapsar; o hücrelerde yer sıfattan gelir.
- "Bağın ucu" sıfattan gelir: çoğu sıfatta sensin, Sabit'te tuzak yapısıdır.
- Tılsım'ın dışarı giden her etkisi: mühür hedefin ayağına düşer, 0,5 s sonra açılır.
- Koruma (tank): kalkan, duvar ya da yapı vermez; yalnız kullanıcıyı koruyan savunma duruşudur (K 16:49). Dostun önüne geçmez, araya girmez. Yapı yalnızca Sabit sıfatından gelir. Süre sıfatın işidir; hücrelerdeki hız farkı ritimdir.
- Kontrol (tank): düşman her zaman kullanıcıya bağlanır (tasma; dikkat çekme/taunt yok, K 15:46); Kontrolün çeşidi hedefin o anki durumundan gelir, silah seçmez; hücreler yalnızca bağlama hareketini yazar.
- Koruma'da dosta varma adımı yoktur (K 16:49); duruş yerinde açılır. Işınlanma kullanılmaz (Çizgisel'in işi).
- Zaman satırları sadece geri sarmayı gösterir (dondurma ve ileri sarma kaldırıldı).
- **Hareket görünümleri (K 20:35):** Yay ip/Levi, Çekiç fırlat-peşinden uç (Thor), Kalkan ≤3 m omuz hücumu, Kılıç şimşek atılışı (Zenitsu), Asa ışın yolunda süzülme, Küre patlama itişi, Tılsım mühür 0,5 s sonra çeker, Top seni fırlatır, Kitap sayfalara dağılıp görünür hızlı yolculuk, Yumruk kısa atılış. Işınlanma yok; yol, yön ve mesafe sıfattan, üst sınır silah menzili.
- Kitap'ın ve Küre'nin yeni ritimleri Tetikli, İşaretli ve Sabit ile çakışmaz: Kitap konum ya da koşul seçmez, sadece cast'i hareket halinde yapar; Küre konumlanmaz, avuçta kalır.
- Çağırma'da yaratığın görünümü silahın tarzından gelir (Yumruk'ta dövüşçü gölgesi, Çekiç'te taş dev); yaratığın sayısı ve davranışı sıfattan gelir.

*(Son kontrol 22:40: taunt ifadesi `silah-fiil-120.md` güncel metniyle değiştirildi (K 15:46, AS-02 kapandı); kın yok ve VUR düzeltmesi (K 21:05) `silah-fiil-120.md` ve animasyon belgelerine işlendi.)*

---

### 7. Açık sorular (yalnız gerçekten karara bağlanmamış olanlar)

> **Güncel ve tam liste `acik-sorular.md`** (AS-01 …). Aşağıdaki A-n numaraları başka belgelerde anıldığı için korunuyor; her biri orada bir AS maddesine eşlendi (AS-36).

**Kullanıcı kararı gerekenler (önem sırasıyla):**

- **A-1 · Hasar azalır mı?** — **KAPANDI (K 16:36):** Hasar 3 s azalma kuralına GİRMEZ. Azalma yalnız etkilere. Fedakarlık-Zarar artık delme mekaniğine bağlı (K 16:40). "Sert kontrol kısa olsa da irade harcar" (11:54) hâlâ kayıtta yok; küçük kontrol kuralı (11:46-1) geçerli.
- **A-2 · Yansıma dosta kurulabilir mi?** — **KAPANDI (K 21:09):** Varsayılan kullanıcıya kurulur; basılı tutup sürükleyince seçilen dosta. Odaklı-Yansıma kendiliğinden en çok hasar alan dostu seçer. Koruma yalnız kullanıcı.
- **A-3 · Mıknatıslı 3 zayıf** — **KAPANDI (K 17:21):** Mıknatıslı kalktı; yerine Güdümlü (`HRAK`). Güdümlü zayıf hücreleri: Koruma, Zaman (en çok 2).
- **A-4 · Koruma zarar paylaşımı** — KAPANDI (K 16:49): Koruma yalnız kullanıcıyı korur; dost bağı ve zarar yönlendirme yok. Soru düştü.
- **A-5 · Otomatik hedef rol tablosu** (1.2): Hareket ve Çağırma'da gidilecek hedefin "düşman kolu" olması türetildi (Rün 3, 5, 7, 8, 10, 11 böyle okuyor; Rün 1 Çağırma ve Hareket'i "sen" okuyor). Koruma artık yalnız sen (K 16:49). Onay?
- **A-6 · Menzil sınırı** — **KAPANDI (K 21:10):** sıfatın iç ölçüleri silah menziliyle kısılır. Kodda `menzil_ic_olcuyu_keser` bayrağı. (Güdümlü: kilit silah menzili içinde alınır; takip kendi süresiyle bitmez, tavan 8 s — K 17:21 + 17:42 + 22:53.)
- **A-7 · Eşit ağırlık:** İşaretli-Hareket'te işaret hafif bir canlıdaysa (arkadaş, küçük düşman) kim kime gider? Seçenekler: ortada buluşmak, kimse kıpırdamaz, bağı kuran gider.
- **A-8 · İşaretli-Hareket'te birlikte çekilen arkadaş:** hangi arkadaş (üstünde senin işaretin olan mı, herhangi bir takım işareti olan mı, kaç kişi)? Aynı yoldan ve aynı hızda mı gelir? O sırada kendi Hareket skillini atarsa çekilme kesilir mi? Üstünde tasma ya da kökleme varsa ne olur?
- **A-9 · Kayıt direği:** kayıt anında zaten ölü olan dost dirilir mi (önerilen: hayır, yalnız kayıttan sonra ölen dirilir)? 5 kişi × 5 yapıyla her 6 s'de diriltmeye sınır gerekir mi (ör. aynı dost 60 s'de bir [test])?
- **A-10 · Zaman kayıt anı:** Tetikli Zaman'da kayıt "tetikten 3 s önce" mi (bu kitap) yoksa "işaretin konduğu an" mı? Ortak Zaman (K 17:05, düğüm kalktı): bu kitap "3 s önce" tutar; yük anı kayıt değildir.
- **A-11 · İşaretli Zaman:** zemine konan işaret kimin kaydını tutar (öneri: işaret konduğunda yakındaki bütün dostların)? Bağda hangi işaretin anı geçerli (öneri: ilk seçilen)?
- **A-12 · İşaretsiz İşaretli:** işaret yoksa skill senden çıkar ve sıfat şekil değiştirmez. Kabul mü, yoksa basılı tutup seçilen noktaya yalnız o skill için geçici işaret mi konsun (Rün 9 Ö-5A)?
- **A-13 · Çağrılan yaratığın canı** — **KAPANDI (K 21:12):** yaratık bir varlıktır; canı var, düşmanlar onu hedef alıp vurabilir.
- **A-14 · Çizgisel hattının başı sen misin?** Evetse yararlı fiillerde sen de hattasın; bu Çizgisel Şifa, Arındırma ve Zaman'ın zayıflığını azaltır.
- **A-15 · Otomatikte boşa giden Hareket hücreleri:** Yayılan Hareket ve Yörüngeli Hareket otomatik hedefte hiçbir şey yapmıyor (zayıf). Kabul mü? Yörüngeli Hareket'te merkez bir düşman (boss) olabilir mi?
- **A-16 · Yapıya işaret konur mu?** 15:51 ve 15:55 kararlarının metni duvara ya da yapıya konmuş işareti varsayıyor ama açık bir "konur" kararı yok. Kanca = yapıdaki işarete İşaretli-Hareket ile çekilme yeterli mi?

**Teste ya da dengelemeye bırakılabilecekler:**

- **A-17 · Çarpma hasarı birimi:** itilen düşman bir şeye çarparsa 1 m kalan itme kaç hasar eder? Çarpılan yapı dayanıklılık kaybeder mi?
- **A-18 · Yoğun Koruma tutma** — KAPANDI (K 16:49): hücre yalnız 0,45 s tam savuşturma; tutma yok.
- **A-19 · Büyü Kitabı ve Küre'nin ritmi** — **KAPANDI (K 20:29 + K 21:10b):** ritim sayıları 4.2'de; Küre `toparlanma_yurume_carpani` 0,5 (ritim, silah kanunu değil).
- **A-20 · Rün eşleşmesi:** Rün 6, 7, 8, 10, 11, 12'nin sıfat eşleşmesi BRIEF'te "A'nın önerisi" diye geçiyor. Bütün belgeler bu eşleşmeyle yazıldı, ama açık bir kullanıcı onayı kayıtta yok.
- **A-21 · Çekim ve kökleme** — **Çekim kısmı KAPANDI (K 17:21):** Mıknatıslı çekim alanı yok. Kalan: Aynı düşmana iki oyuncu tasma atarsa ne olur? Tasma, silah menzilinden uzak bir tuzaktan kurulursa uzunluk nasıl hesaplanır?
- **A-22 · Arındırma çarpanları:** Sıçrayan ×0,5, Yayılan ve Çizgisel ×0,85, Ortak küçük hâl ×0,5 tek bir etkiye nasıl uygulanır (kısmi sökme mi)?
- **A-23 · Ad çakışması:** Tetikli'nin "işareti" ile İşaretli'nin işareti aynı kelimeyi kullanıyor. Öneri: Tetikli'ninkine "kapan". Kontrol'ün bağı → bu kitapta **tasma**; İşaretli'nin bağı → bu kitapta **zincir** (K 17:33). Kalan: Tetikli "işareti" adı.


---

### 8. Animasyon ve görünüm kuralları

Bu bölüm oyun sonucunu değiştirmez. Mekanik Bölüm 1–6'dadır; görünüm ondan okunur. Ayrıntı: `animasyon-kurallari.md` (klip ve zamanlama), `efekt-motoru.md` (VFX). İki belge bu bölümle çelişirse bu bölüm kazanır; çelişkiler `acik-sorular.md`'de.

#### 8.1 Animasyon kurala bağlıdır (K 19:48)
- K (19:48), aynen: "Animasyon da kurala bağlanır (skill sistemi gibi). İki katman: (1) koreografi: silah başına etkileyici imza hareketleri (Kalkan omuz atma, dash, dodge takla, Çekiç tepede döndürme vb.), sade tut-yürü-dur değil; (2) kurala bağlı efekt motoru (VFX): fiil rengi/niyeti, sıfat şekli."
- K (19:48), aynen: "Hareket kombosu örneği: dodge bir kez → zıplama; havadayken (inmeden) tekrar → takla."
- K (19:48), aynen: "Kural: silah imza hareketi verir, fiil niyeti seçer, sıfat zamanlama/yön ayarlar, efekt kurala göre biner."
- K (20:29): oynanış tarzı animasyondan okunur (4.1). Her silahın tarzı gözle tanınmalı.
- Dünyanın zamanı tektir (1.14): slow-mo yok; hitstop yalnız görüntüde.

#### 8.2 Fiziksel fiil = silaha özel koreografi, büyü fiili = kendi basit büyü animasyonu (sohbet, kullanıcı)
- Kullanıcı (sohbet t717u; animatör belgelerinde "K 20:39"), aynen: "ya mesela kilicla savunma hareketleri ya da asayla ya da kureyle bunlar hep dusunulmeli basit yani sadece ugrastirici.zaman kontrol gibi seyler zaten buyu tarzi seyler sifa arindir falan inanilmaz animasyon isteyen şeyler degil"
- Kullanıcı (sohbet t718u, düzeltme; animatör belgelerinde "K 20:40"), aynen: "ortak degil hepsine birer animasyon yeter geldi basit animasyonlar yani buyu animasyonlari.ozel animasyon fiziksellerde lazim"
- **Kural:**
  - **Fiziksel fiiller: Zarar, Koruma, Kuvvet, Hareket.** Her silah bu dört fiili kendi özel koreografisiyle yapar (imza hareketleri, 6. bölüm hücreleri). Koruma'nın savunma duruşu her silahta ayrı tasarlanır (Kılıç, Asa, Küre dahil).
  - **Büyü fiilleri: Şifa, Kontrol, Zayıflatma, Güçlendirme, Arındırma, Yansıma, Çağırma, Zaman.** Her büyü fiilinin **kendine ait** basit bir büyü animasyonu vardır. Tek ortak büyü animasyonu **yoktur** (t718u düzeltmesi). Farkı efekt taşır.
  - Dört fiziksel fiil listesi ana ajanın sohbetteki özetidir ("Zarar, Koruma, Kuvvet ve Hareket"); A 21:03 ile kesinleşti (AS-12 kapandı). Büyü animasyonu silahtan bağımsız 8 klip; jest eli silaha göre tablodan (A 21:03, AS-11 kapandı).
- `animasyon-kurallari.md` 20:50 sürümü bu kuralı "K 20:39–20:40" etiketiyle işledi: 4 fiziksel yuva (VUR, AĞIR, DURUŞ, ATIL) silah başına; 8 büyü fiili `Buyu_<Fiil>` klibi, silahtan bağımsız; BAĞ ve DESTEK yuvaları ile "Zaman = VUR'un tersi" kalktı. Aynı belgede kalan "süper zırh" ve "dodge iptali" satırları K 20:29 ile çelişir (→ AS-03). Not: "K 20:39–20:40" etiketi `v3-kararlar.md`'de yok (→ AS-32).

#### 8.3 Kılıç: Demon Slayer tarzı (sohbet, kullanıcı)
- Kullanıcı (sohbet t716u), aynen: "mesela zarar yayilma tanjiro dönerek kafasini kesiyordu ya ilk sezonlarda demonun o tarz efektli bir sey olabilir yani demon slayer tarzi kilic iste"
- Okuma (ana ajanın cevabı, kullanıcı itiraz etmedi): Kılıç-Yayılan-Zarar = Tanjiro su nefesi gibi dönerek yatay kesiş. Halka Yayılan'dan, dönüş ve kesiş Kılıç'ın tarzından, akan kor izi efektten gelir. **Mekanik değişmez:** 4 m içindeki en çok 5 düşmana ×0,85 (1-5). Kılıç'ın imza listesine "Demon Slayer nefes teknikleri tarzı, dönen kesiş ve akan kor izi" eklenir.
- Kılıç tarzı (4.2): Zenitsu şimşek kesişi. Kılıç Hareket: Zenitsu şimşek atılışı (K 20:35).

#### 8.4 Ejderha lore'u görünümde (GameDesign önerisi + sohbet)
Hepsi **yalnız görünüm**, mekaniğe dokunmaz, mobilde ucuz. Art bible: tek sıcak renk kor turuncusu, bloom yalnız korda ve rünlerde (GameDesign).
**Durum:** ana ajan sohbette 1, 3 ve 4'ü önerdi, Kontrol motifini değiştirdi ve "efekt motoruna ekletiyorum" dedi. Kullanıcı bir sonraki mesajında (t721u) Çekiç silüeti fikrini ekledi. Kullanıcı kararı 20:42 olarak `v3-kararlar.md`'ye yazıldı (AS-13 kapandı).

1. **Rün çizimi Ejderha Dili harfine döner** (GameDesign öneri 1, aynen): "oyuncunun parmağıyla çizdiği çizgi, 0,2 s içinde THAM-OK harfine oturur. Fiil harfi ve sıfat harfi yan yana parlar (örn. VORN-AK [eski sıra; güncel: `AK-VORN`, 1.1, AS-16]). Bu bir decal ve tek bir emissive dokudan ibaret." *(Örnekteki sıra çizim sırasıyla çelişir: çizim önce fiil sonra sıfattır, flaş `AK-VORN` olmalı → AS-16. Bu kitap 1.1'i uygular.)*
2. **Fiil = ejderha bedeninin bir parçası** (GameDesign öneri 3, aynen): "Renk ayrımı yerine motif ayrımı kullanılır, çünkü palet dar. Öneriler: Zarar pençe izi, Koruma pul deseni, Şifa nefes buğusu, Hareket kanat çırpışı izi, Kuvvet kuyruk savurma yayı, Kontrol göz/dikey göz bebeği, Zaman halkalı pul, Çağırma kemikten doğan biçim, Yansıma pulda parlama, Zayıflatma is ve kurum. Renk hep gri-yeşil + kor, yalnız yoğunluk değişir."
   - **Kontrol düzeltmesi** (ana ajan, sohbet): "Kontrol artık bir tasma, o yüzden onun motifi göz değil, ejderha kuyruğundan bir zincir olsun." → Kontrol motifi = **ejderha kuyruğundan zincir** (tasmanın görüntüsü).
   - Güçlendirme = kalpte kor atışı, Arındırma = pul dökümü (deri değiştirme); palet 3 rol ailesi, motif birincil (A 21:03; AS-17 ve AS-18 kapandı).
3. **Silah ejderhanın kalıntısıdır** (GameDesign öneri 4, aynen): "skill sırasında silahın rün yuvaları ve çatlakları korla parlar, sanki içindeki ejderha uyanıyor. Tarzla bağlantılar: Çekiç çakınca yerde pul biçiminde çatlak açar; Kılıç kesişinden kor izi kalır; Kitabın sayfaları ejderha derisi gibi doku taşır; Kalkanın yüzünde pul deseni parlar. Bunun maliyeti tek bir emissive maske."
4. **İmza hareketlerde ejderha silüeti** (sohbet):
   - Kullanıcı (t721u), aynen: "mesela fiziksel skillerde mesela cekicin kareografilerinde ejderha motifleri cikabilir thorun yildirimla saldirdigi sahne var ya endgamede ejderha silueti yildirim degil de tepeden inerken mesela"
   - Okuma (ana ajan): Çekiç tepeden inerken Thor'un yıldırımı yerine korla yanan bir ejderha silüeti iner; çarpınca yerde pul desenli çatlak açılır. Her silahın bir-iki imza hareketine böyle bir silüet anı tasarlanır (ör. Kılıç kesişinde kuyruk savrulur, Kalkan hücumunda pul parlar). Yalnız fiziksel fiillerin imza hareketlerinde (8.2). Mobil için hafif.
5. **Sonra listesine** (9): GameDesign öneri 5 "kordan yankı = ejderhanın hayaleti" (yarı saydam pençe/kanat/ağız, 0,3 s) ve öneri 6 "uzaktaki dağın gözü ya da çatlağı bir an kor yakar". Öneri 2 (harf efektin çekirdeğinde) ve öneri 7 (fısıltı ses, kısa isim yazısı) da Sonra listesinde (AS-13 kapandı).

#### 8.5 Görünüm sözlüğü (kodda adlar)
- **İşaret** (İşaretli): küçük kor rünü, oyuncu renginde (K 9 Ekim, K 17:35). **Zincir:** ince kor çizgisi. **Tasma** (Kontrol): ejderha kuyruğu zinciri motifi (8.4). **Kapan** (Tetikli'nin işareti; ad önerisi A-23).
- Ejderha Dili flaşı çizim sırasıyla (`AK-VORN`), Türkçe ad doğal sırada (Yoğun Zarar) (1.1).

---

### 9. Sonra listesi (şimdi tasarlanmaz)
Kullanıcının kilit kuralı: "Yeni fikirler Sonra listesine." (`OKU-BENI.md`)
- 3-4 rünlü kombolar: portal, iki nokta arası geçiş, dostla yer değiştirme (K 20:17, 20:18). İşaretli + Çizgisel = işarete ışınlanma (K 15:55).
- Can azaldıkça güç kazanma, can belli yüzdeyi geçince normale dönme (Olaf tarzı) (K 17:21).
- Top × Yayılan "düştüğü yerde halka" (K 18:42 (3), reddedildi, Sonra'ya).
- Element tepkileri (K karar 6).
- Sıfat isimlerini daha havalı yapmak (K 20:38; İngilizce adlar 11:17'de onaylı).
- Ejderha hayaleti yankısı ve dağ efekti (8.4 madde 5).
- Lore öneri 2 (harf efektin çekirdeğinde) ve öneri 7 (fısıltı ses, kısa isim yazısı) (8.4 madde 5).
- Şifa taşması ve kalkan tavanı/birikmesi: dengeleme aşaması (K 12:17).

---

### 10. Kapsama kontrolü

Her kaynak karar → bu kitaptaki maddesi. Kaynak satırı `v3-kararlar.md` için **V3:satır**, diğer dosyalar için dosya:satır. Durum: **✔** karşılandı · **✖ geçersiz** (daha yeni bir karar kaldırdı; hangi karar yazılı) · **? belirsiz** (`acik-sorular.md` maddesi).

#### 10.1 `v3-kararlar.md` (07.10 – 9 Ekim 20:35)

| Kaynak | Karar (kısa) | Kitapta | Durum |
|---|---|---|---|
| V3:3 | Sıfat sayıları veriden (Yoğun ×1,35, Sıçrayan 2×0,5, Yayılan ×0,85 5 hedef, Yansıma 2 s, Çağırma 5 s) | 3.2; 2 (Yansıma, Çağırma) | ✔ (Kopya/Akan sayıları ✖ V3:71–72) |
| V3:4 | Tek çizim, parmak kalkınca biter; dokun-kaldır-dokun kombo değil | 1.1 | ✔ |
| V3:5 | Aynalı genel kuralı | — | ✖ Aynalı reddedildi (V3:70) |
| V3:6 | Yoğun genel kuralı | 3.2, x-1 hücreleri | ✔ (Kuvvet "yere çakar" ✖ 9 Ekim, 5-1) |
| V3:7 | Sabit = gövde yapıya döner | 1.12, 3.2 | ✔ ("şekil=silah" ✖ 9 Ekim silah kuralı) |
| V3:8 | Element maç öncesi, tek küçük durum | 1.13 | ✔ |
| V3:9 | Kontrol o anki işi keser | 1.7 | ✔ |
| V3:10 | Hareket: ağırlık seçer | 1.8 | ✔ (9 Ekim: yalnız kullanıcı taşınır, 1.9) |
| V3:12 | Zaman yönsüz alan, sıfat belirler | 1.11 | ✖ 9 Ekim "Zaman tek işe indi" (V3:186); KG-25 hâlâ açık → AS-21 |
| V3:15 | F1 Zarar | 2 | ✔ |
| V3:16 | F2 Şifa | 2, 2-1 | ✔ |
| V3:17 | F3 Hareket | 2, 1.9 | ✔ |
| V3:18 | F4 Koruma (canlıya kalkan, duvar yalnız Sabit) | 2.1 | ✖ kalkan kısmı (K 16:49); duvar kısmı ✔ |
| V3:19 | F5 Kuvvet | 2, 1.8 | ✔ (Aynalı ile çekme ✖) |
| V3:20 | F6 Kontrol | 1.7 | ✔ |
| V3:21 | F7 Zayıflatma ("hız Zaman'ın işi") | 2 + not | ✔ / "hız Zaman'ın" ✖ (21:38, 9 Ekim) → AS-20 |
| V3:22 | F8 Güçlendirme | 2 | ✔ |
| V3:23 | F9 Arındırma (iki yönlü) | 2, 5.9 | ✖ 9 Ekim "yalnız dosttan zararlı siler" |
| V3:24 | F10 Yansıma, 2 s | 2, 5.10 | ✔ (kime → K 21:09) |
| V3:25 | F11 Çağırma | 2 | ✔ |
| V3:26 | F12 Zaman | — | ✖ 9 Ekim |
| V3:27 | F12-boss | 1.6 | ✖ Zaman yalnız dosta (1.6 notu) |
| V3:28 | Fiil tek başına kullanılmaz; yön/hedef sıfattan | 1.1 | ✔ |
| V3:29–34 | Yön/hedef fiillerden çıkarıldı; ağırlık dünya kuralı | 2, 1.8 | ✔ |
| V3:35 | Basılı tutup hedef seçme (~0,2 s) | 1.1 | ✔ |
| V3:36 | S1 Yoğun önerisi | 3.2 | ✖ V3:40 yerine geçti |
| V3:37 | Hedef türleri (canlı/yapı/boş nokta) | 1.1 | ✔ |
| V3:38 | S2 Emici | 3.1 | ✖ Emici çıktı (V3:42) |
| V3:39 | M-13: tek ana soru, 5 cevap | 1.15 | ✔ |
| V3:40 | Yoğun kesin satırı | 3.2, x-1 | ✔ (Zaman "donar" ✖ 9 Ekim) |
| V3:42–56 | 12 sıfat seti (19:23) | 3.1 | Kısmen: Yoğun, Yayılan, Sıçrayan, Sabit, Odaklı, Tetikli ✔; diğerleri ✖ (3.1 listesi) |
| V3:57 | Yükselen–Yumruk çakışması | — | ✖ Yükselen reddedildi (V3:67) |
| V3:58 | Taraf kuralı | 1.2 | ✔ ("Hareket yararlı" ✖ 9 Ekim rol kuralı: Hareket Saldırı, yalnız sen) |
| V3:59 | S2 Yayılan | 3.2, x-5 | ✔ (Hareket "sen ve dostlar" ✖ 9 Ekim; Zaman ✖) |
| V3:60, 62 | Hareket hep sen / öznesini sıfat belirler | 1.9 | ✖ ikisi de 9 Ekim "Hareket yalnız kullanıcıyı taşır" ile |
| V3:61 | İlke: kural ne derse o, istisna yok | 1 (giriş) | ✔ |
| V3:63 | S3 Sıçrayan (Hareket dost taşır) | 3.2, 3-3 | ✔ / Hareket satırı ✖ 9 Ekim |
| V3:64 | S4 Sabit (yapılar, ömür 6 s) | 1.12, x-4 | ✔ (Zaman sütunu ✖; yapı sınırı V3:179) |
| V3:65–72 | Bağlayıcı, Bulandırıcı, Yükselen, Aynalı, Kopya, Akan reddi | 3.1, 1.15 | ✔ |
| V3:73 | S12 Tetikli | 3.2, x-7 | ✔ |
| V3:75 | Takım 5; azalma 3 s ×0,5/×0,25; Güçlendirme 2 kaynak | 1.4 | ✔ |
| V3:76 | S13 Fedakarlık | 1.5, 3.2 | ✔ ("yığılmaya takılmaz" hasar için anlamsız, K 16:36) |
| V3:78–79 | Geçiş/portal 3-4 rüne; temel ışınlanma 2'li sette | 1.9, 9 | ✔ |
| V3:80 | 10/12 esnemesi; Işınlanan reddi | 1.15 | ✔ |
| V3:81 | S14 Çizgisel | 3.2, x-10 | ✔ (Kuvvet koridor ✖ 5-10; Koruma 2 s duvar ✖ K 16:49; Zaman ✖) |
| V3:83 | S15 Aktaran | 3.1 | ✖ 9 Ekim |
| V3:85 | S16 Yörüngeli | 3.2, x-12 | ✔ |
| V3:87 | S17 Ortak (düğüm) | 3.2 | ✖ K 17:05 |
| V3:89 | S18 Mıknatıslı | 3.1, 1.8 | ✖ K 17:21 |
| V3:90 | 12 sıfat tamam; isimler sonra havalı | 3.1, 9 | ✔ |
| V3:91 | Yavaşlatma Kontrol'ün varyasyonu | 1.7 | ✔ |
| V3:92 | Zaman yeni yön (geri sar/dondur/ileri sar) | — | ✖ 9 Ekim |
| V3:93–106 | Zaman × 12 (21:46) | 5.12 | ✖ 9 Ekim; yalnız kayıt direği fikri ✔ (12-4) |
| V3:107 | Kontrol çeşidi hedefin işinden; boss | 1.7, 1.6 | ✔ |
| V3:113–114 | Yoğun Çağırma | 11-1 | ✔ |
| V3:118–128 | Yayılan satırları | x-5 | ✔ (Koruma kalkanı ✖ K 16:49; Arındırma düşman kısmı ✖ 9 Ekim; Hareket ✖; Zaman ✖) |
| V3:131–142 | Sıçrayan satırları | x-3 | ✔ (Koruma ✖ K 16:49; Arındırma düşman ✖; Hareket dost ✖; Zaman ✖) |
| V3:145–159 | Tetikli satırları + boss notu | x-7 | ✔ (Koruma dost ✖ K 16:49; Kuvvet "tersine" ✖ 9 Ekim; Arındırma düşman ✖; Zaman ✖) |
| V3:162–178 | Fedakarlık satırları (11:07–11:13) | x-8 | ✔ (Zarar "azalmayı aşar" ✖ K 16:36/16:40; Güçlendirme düzeltilmiş hâli ✔ 8-8) |
| V3:179 | 11:17: Mıknatıslı Arındırma; boss susturması; Fedakarlık mevcut can; kayıt direği ölüyü getirir; yapı 5/25; İngilizce adlar + 7 kök | 1.6, 1.5, 12-4, 1.12, 2, 3.1 | ✔ (Mıknatıslı Arındırma ✖ K 17:21). "7 yeni kök"ün hangileri olduğu kayıtta yok → AS-34 |
| V3:180 | 11:46 (1)–(6) | 1.6, 1.5, 1.4, 1.11, 4 (Kitap yeni), — | ✔; (6) Aktaran ✖ |
| V3:181 | 12:17: taşma/kalkan tavanı dengelemeye; silahlar skilli değiştirmeli | 2-1, 9; 4.1 | ✔ / "silah skilli değiştirmeli" ✖ K 20:29 (mekanik kanun yok) |
| V3:184 | Silah kuralı (9 Ekim) | 4.1 | ✔ (menzil kısmı ✖ K 15:41) |
| V3:185 | Rol 4–4–4 | 1.2 | ✔ ("araya girer" ✖ K 16:49) |
| V3:186 | Zaman tek işe indi | 1.11 | ✔ |
| V3:187 | Aktaran kalktı, İşaretli Rün 9, Odaklı Rün 2 | 3.1 | ✔ |
| V3:188 | İşaretli kuralı (yalnız İşaretli-Arındırma koyar, 4 işaret) | 1.10, 9-9 | ✔ ("iki işaret arası bağ" ✖ K 17:33 zincir) |
| V3:189 | Çizim sırası fiil → sıfat | 1.1 | ✔ |
| V3:190 | Açık: hareket kesme, boss bağlama | 1.9, 1.7 | ✔ kapandı (15:53, 15:51) |
| V3:191 | 15:41 menzil silahta | 1.3, 4.1 | ✔ |
| V3:192 | 15:46 tasma, taunt yok | 1.7, 6 | ✔ (`silah-fiil-120` Notlar'da taunt kalıntısı → AS-02) |
| V3:193 | 15:47 işaret menzili | 1.10 | ✔ |
| V3:194 | 15:51 boss bağlama | 1.7 | ✔ |
| V3:195 | 15:53 hız devri | 1.9 | ✔ |
| V3:198–201 | 15:55 İşaretli-Hareket atılma; ışınlanma Çizgisel; 3 rün notu | 1.9, 3-9, 9 | ✔ |
| V3:204–205 | 16:36 hasar azalmaz | 1.4 | ✔ |
| V3:206 | 16:40 Fedakarlık-Zarar deler | 1-8 | ✔ (mermi + araya giren çatışması KG-36 → AS-27) |
| V3:209–210 | 16:49 Koruma yalnız sen | 2.1, 5.4, 4.4 | ✔ |
| V3:213–215 | 16:52 mermi varlık, alan değil | 1.16 | ✔ (sınıflandırma → AS-27) |
| V3:218–220 | 17:05/17:15 Ortak yükü | 3.2, x-11 | ✔ |
| V3:223–226 | 17:21 Güdümlü; Olaf Sonra | 3.2, 9 | ✔ |
| V3:227 | 17:27 Arındırma hücreleri | 5.9 | ✔ |
| V3:228 | 17:28 Zaman hücreleri; sayılar başlangıç değeri | 5.12, giriş "Sayılar" | ✔ |
| V3:229 | 17:33 İşaretli zinciri | 1.10 | ✔ |
| V3:230 | 17:35 zincir sahibi | 1.10 | ✔ |
| V3:231 | Grup 6 (1-4, 11-6) | 1-4, 11-6 | ✔ |
| V3:232 | 17:36 Koruma 12 hücre | 5.4 | ✔ |
| V3:233 | 17:40 Güdümlü-Zarar/Hareket/Kuvvet | 1-6, 3-6, 5-6 | ✔ |
| V3:234 | 17:41 kilit vuruşu | 1-6 | ✔ (Yumruk serisiyle birleşmesi → AS-30) |
| V3:235 | 17:42 Güdümlü süreyle bitmez | 3.2 | ✔ |
| V3:238–240 | 17:44 simülasyon yöntemi | **1.17 (yeni)** | ✔ (önceki sürümde yoktu) |
| V3:241 | 18:39 Tılsım ritmi | 4.5, 4.2 | ✔ |
| V3:242 | 18:42 Odaklı-Hareket, Yayılan merkezi, Top×Yayılan Sonra | 3-2, 3.2, 9 | ✔ |
| V3:243 | 19:41 Yoğun şarjı tarafa göre | 3.2 | ✔ (önceki sürümde yalnız hücrelerde vardı) |
| V3:244 | Kör test sürprizleri A kararıyla kabul | 1.17 | ✔ (A kararı; "kullanıcı onayı" olup olmadığı → AS-34) |
| V3:247–249 | 19:48 animasyon kurala bağlı; dodge; silah imza/fiil niyet/sıfat zaman | **8.1 (yeni)** | ✔ |
| V3:250 | 20:29 silah = menzil + ritim + tarz, kanun yok | 4.1, 4.2, 6 | ✔ |
| V3:251 | 20:35 İşaretli-Hareket görünümleri | 4.3 | ✔ (bütün Hareket için mi → AS-25) |
| V3:252 | 20:35 yolculuk ritmi | 4.3 | ✔ (5 silah "Öneri" → AS-07) |

#### 10.2 `run-ajanlari/BRIEF.md`

| Kaynak | Karar | Kitapta | Durum |
|---|---|---|---|
| BRIEF:3 | Kombo = SIFAT + FİİL | 1.1 | ✖ 9 Ekim çizim sırası (V3:189) → AS-19 |
| BRIEF:11–19 | Kullanıcı ilkeleri (istisna yok, taraf, azalma, somut yaz, [test]) | 1, 1.2, 1.4, giriş | ✔ ("Savaşa yeni karar ekleme yok. Sınıf yok" ✔ 1.2: rol fiilden) |
| BRIEF:31–45 | Rün eşleşmesi | 3.1 | ✔ (Rün 2 Aktaran satırı ✖; Rün 6,7,8,10,11,12 "öneri" → A-20 / AS-36) |
| BRIEF:47–53 | 11:46 kararları | 1.4–1.6, 1.11 | ✔ ((6) ✖) |
| BRIEF:55–57 | 9 Ekim 15:35 özeti | 1.2, 1.11, 3.1, 1.1 | ✔ |

#### 10.3 `silahlar.md` ve `silah-fiil-120.md` (20:36)

| Kaynak | Karar / içerik | Kitapta | Durum |
|---|---|---|---|
| silahlar.md:7–12 | Silah üç şeyi belirler; taraf/rol değiştirmez; işaretten çıkan skill | 4.1 | ✔ |
| silahlar.md:9 | Asa büyü duruşu kalkan vermez | 4.4 | ✔ |
| silahlar.md:17–28 | Menzil tablosu (range_mult) | 4.2 | ✔ menzil; eski tarz/ritim sütunları → AS-05 |
| silahlar.md:30–33 | 15:42 kullanıcı kararları (Kitap/Küre yeniden, dost menzil min 6, Hareket menzili) | 1.3, 4.2 | ✔ (15:42 `v3-kararlar`'da yok → AS-34) |
| silahlar.md:43–69 | Ritim ölçüleri, çift ayrımı, sıfatla birleşme | 4.1, 4.2 | ✔ (Kalkan "kesilmez" → AS-03; Küre yavaşlaması → AS-04) |
| silahlar.md:71–85 | Yolculuk hızı | 4.3 | ✔ (Öneri satırları → AS-07) |
| silahlar.md:88 | Kitap Hareket beden yol alır, hasar alır (A 20:36) | 4.3 | ? A kararı → AS-08 |
| silahlar.md:36 | Tılsım hedefte belirme yok (A 20:36) | 4.5 | ✔ (K 18:39 ile aynı) |
| silah-fiil-120.md:9 | Mekanik kanun yok; yakın silahlar ritimle | 4.1 | ✔ (saat 20:27 → AS-06) |
| silah-fiil-120.md:13–181 | 10 Tarz satırı + 120 hücre | 4.2, 6 | ✔ (K 21:05: kın yok, VUR düzeltmesi GameDesign/animatör) |
| silah-fiil-120.md:186–195 | Notlar | 4.1, 6 | ✔ (190 taunt → AS-02) |

#### 10.4 Sohbet (ana ajanın konuşması; yalnız görünür kısım)

| Kaynak | Karar | Kitapta | Durum |
|---|---|---|---|
| t716u | Kılıç Demon Slayer tarzı; Yayılan-Zarar Tanjiro dönüşü | 8.3 | ✔ görünüm; v3-kararlar'a yazıldı (AS-13 kapandı) |
| t717u | Savunma hareketleri (Kılıç, Asa, Küre) düşünülmeli; büyü fiilleri büyük animasyon istemez | 8.2 | ✔ |
| t718u | "ortak değil hepsine birer animasyon"; özel animasyon fizikselde | 8.2 | ✔ (AS-11 ve AS-12 kapandı, A 21:03) |
| GameDesign lore 1, 3, 4 + ana ajan Kontrol zinciri | Rün → harf; fiil motifleri; silah çatlakları kor | 8.4 | ✔ v3-kararlar 20:42 (AS-13 kapandı) |
| t721u | Çekiç inişinde ejderha silüeti; imza hareketlerde silüet | 8.4 (4) | ✔ |
| GameDesign lore 5, 6 | Hayalet pençe, dağ efekti → Sonra | 9 | ✔ |
| GameDesign lore 2, 7 | Harf efekt çekirdeğinde; fısıltı ses | — | Sonra listesi (9) |
| GameDesign soruları 1–6 (20:36) | Kalkan araya girmez; yakın silah kalıntıları; Asa/Tılsım metinleri; Küre/Top benzerliği; Tılsım 0,5 s; Kitap hasarı | 4.4; 4.1; 4.4–4.5; —; 4.3, 4.5; 4.3 | ✔ 1, 2, 3, 5 / ? 4 → AS-09 / ? 6 → AS-08 |
| Sohbet özeti (kesik, ~37 bin karakter görünmüyor) | Özette görünen: kilit kuralları, silah = tarz, ağırlık, hedef seçme | giriş, 4.1, 1.8, 1.1 | **Taranamadı:** özetin kesik kısmındaki onaylar → AS-34 |
| t722u–t723u | Kod sabaha bekler; önce belgeler | — | Süreç kararı, oyun kuralı değil |

#### 10.5 Bulunamayan ya da belirsiz kalan kararlar
1. ~~"Kın yok" kararı kayıtsız~~ **KAPANDI:** K 21:05 ile kayda girdi (kın yok, Sonra listesinde; AS-01 kapandı).
2. **"K 20:37" (Kılıç nefes tekniği / akan iz)**: animasyon ve efekt belgeleri bu etiketle anıyor; `v3-kararlar.md`'de yok. İçerik t716u ile uyumlu (AS-32).
3. **Ejderha lore 1, 3, 4 ve Kontrol zinciri**: v3-kararlar 20:42 ile kayda geçti (AS-13 kapandı).
4. **Fiziksel fiil listesi** (Zarar, Koruma, Kuvvet, Hareket): A 21:03 ile kesinleşti (AS-12 kapandı).
5. **Yolculuk hızı Öneri satırları** (Kılıç, Yumruk, Kalkan, Tılsım, Kitap) (AS-07).
6. **Kitap Hareket'te hasar** (A 20:36) (AS-08).
7. **"9 Ekim 15:42 kullanıcı kararları"** (`silahlar.md`): `v3-kararlar.md`'de satırı yok.
8. **"7 yeni Ejderha Dili kökü"** (V3:179): hangi kökler olduğu yazılı değil.
9. **Kitapta "K (kural kazanır, A 19:46)" ve "A 19:41, sürpriz" etiketli hücreler**: A kararıdır; V3:244 yalnız 7 hücreyi kullanıcı onaylı sayıyor. Diğer A 19:46 hücreleri (E14, E20, E21, E23, E24 dayanaklı) kullanıcı onayı bekler (AS-35).
10. **Sohbetin kesik özet kısmı**: taranamadı (AS-34).

---

<a id="b2"></a>
