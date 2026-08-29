# Dövüş Sistemi — Alfa Spesifikasyonu v1 (ARŞİV)

> **ARŞİV, 29 Ağustos 2026.** Rünler ve mekanikler yeniden tasarlanıyor; bu belge artık
> **tek doğruluk kaynağı değil**. Güncel belge `docs/dovus-sistemi.md` — pazarlıksız kısıtlar,
> kodda çalışan mekanikler ve bütün sayılar orada. Burası kararların **gerekçesini** ve terk
> edilmiş yolları tutuyor. Çelişki hâlinde yeni belge geçerlidir.

> Bu belge, dövüş sisteminin **tek doğruluk kaynağıdır**. Ajanlara verilen her görev bu
> belgeye referans verir. Buradaki sayılar başlangıç değerleridir ve **hepsi ayarlanabilir
> veri** olarak kodlanır (bkz. [Tasarım Özeti §8](../tasarim-ozeti.md#-en-kritik-mimari-kural)).
>
> Tarih: 23 Ağustos 2026 · Durum: alfa, ilk telefon turu yapıldı (§13'ün 1. ve 2. sorusu
> **evet**) · Bağlam: [Tasarım Özeti](../tasarim-ozeti.md)

---

## 1. Tek Paragrafta Sistem

Sol başparmak karakteri yürütür. Sağ başparmak, beşgen dizilmiş 5 noktanın üzerinde
sürüklenerek **cümle kurar**: ilk dokunulan nokta fiildir (ne yapıyorum), sonrakiler sıfattır
(nasıl yapıyorum). Cümle çizilirken sonuç **zaten dünyada olur ve elinin altında şekil
değiştirir**. Beşgenin ortasına **tıklamak** düz vuruştur; cümle kuruluyken aynı tıklama
cümleyi erkenden kapatır ve ödemesini alır. Dodge, beşgenin dışındaki ayrı ve ekrana sabit
düğmedir; cümle sürerken basılırsa yatırımı iptal eder. Bossu doğru okuyup tam zamanında
sıyırırsan yavaş çekim penceresi açılır; o pencerede normalde sığmayacak uzunlukta bir cümle
kurabilirsin.

Oyunun her an sorduğu tek soru: **bossa bakarak, kaç nokta daha sığdırabilirim?**
Bu bir hafıza sorusu değil, okuma sorusudur. Sistemin doğruluk ölçütü budur.

---

## 2. Girdi Düzeni

| Girdi | Eylem |
|---|---|
| Sol yarı, sürükleme | Sanal çubuk — hareket |
| Sağ yarı, beşgen 5 nokta, sürükleme | Cümle kurma |
| Beşgen merkezi, **kısa dokunma**, cümle yokken | Düz vuruş (tek noktalık, anında kapanan cümle) |
| Beşgen merkezi, **kısa dokunma**, cümle kurulurken | Erken kapanış — cümle o uzunluğun ödemesini alır |
| Dodge düğmesi (beşgenin dışında, ekrana sabit), **kısa dokunma** | Dodge (cümle sürüyorsa iptal eder) |
| Noktada bekleme | Kelimeyi yoğunlaştırma |

- Merkez ve dodge düğmesi **yalnızca tıklamayla** çalışır: `tapMaxMs = 180`,
  `tapMaxMoveDp = 12`. Bu eşiklerin dışında kalan temas, çizim olarak yorumlanır.
- **Dodge neden çubuğun yanında değil:** sol çubuk dinamiktir, parmağın indiği yerde doğar.
  Yanına konan düğmenin sabit bir yeri olmaz, yani kas hafızası kurulamaz. Dodge da beşgen
  gibi ekrana sabittir.
- **Merkez neden düz vuruş:** beşgenin ortası eldeki en hızlı ve en kesin hedef. Dövüşün en
  sık yapılan eylemi oraya oturur; dodge'un orada olması ise çizimi bitirip ortaya basma
  refleksini cezaya çeviriyordu.
- Beşgen ekrana sabittir (kas hafızası), dünyaya değil. Yarıçap ve konum ayarlanabilir,
  sağ/sol el için aynalanabilir.
- **Dokunsal geri bildirim zorunlu:** her kaydedilen nokta kısa titreşim + hece sesi verir.
  Kolun tuş hissi bizde yok; yerine sesi ve titreşimi koyuyoruz. Bu süs değil, girdinin
  onay kanalıdır — olmazsa oyuncu göze bakmak zorunda kalır ve bossu kaçırır.

---

## 3. Gramer

Üç kural. Hepsi **karşılık**, hiçbiri uzlaşım değil — yani ezberlenmez, türetilir.
Yeni bir kural eklenecekse tek testi şudur: *oyuncu bunu hiç denemeden tahmin edebilir mi?*

**K1 — Konum rolü belirler.** İlk dokunulan nokta **fiil**, sonrakiler **sıfat**tır.
Aynı rün iki farklı işi yapar. Bu yüzden `5-1` ile `1-5` aynı şey değildir; hangisinin fiil
olduğu değişir. Alan bedavaya iki katına çıkar.

**K2 — Mesafe büyüklüğü belirler.** Beşgende komşu noktaya **kısa sıçrama** hafif/hızlı bir
sıfattır; karşıya **uzun sıçrama** ağır/yavaş bir sıfattır. Parmak mesafeyi zaten hissettiği
için türetilebilir, ve bedeli zaman olduğu için kendini dengeler.
Bedel **asla ergonomik zorluk olmamalı** — el boyu ve telefon boyu gücü belirlememeli.

**K3 — Tekrar yoğunlaştırır.** Aynı kelimeyi iki kez söylemenin iki yolu var, ikisi de zaman
öder: noktada **beklemek** (`dwellMs = 220`, en fazla `dwellMaxStacks = 2`) ya da o noktaya
**geri dönmek** (bir sıfat yuvası harcar).

İki yol iki farklı şey öder ve karıştırılmamalı. Beklemenin bedeli **parmağın ve dövüşün
süresi**: boss bu sırada durmaz, telegraf işler, cümle gerçek zamanda uzar. Bedeli §5'teki
iptal penceresi **değildir** — bekleme boyunca o pencere donar, bekleme biter bitmez kaldığı
yerden işler. Donmasaydı iki yığın (2 × 220 ms) fiilin 420 ms'lik penceresine hiç sığmaz ve
`dwellMaxStacks = 2` yazılı ama oyunda ulaşılamayan bir sayı olurdu. Geri dönmenin bedeli
ise zaten yuvanın kendisi.

> Bilerek eklemediğimiz eksenler: saat yönü, çizgilerin kesişmesi, şeklin simetrisi.
> Hepsi cazip ama her yeni eksen öğrenilecek bir şeydir ve öğrenilecek şey ezbere kayar.
> Alanı büyütmek gerekirse **rün havuzu** ve **durum tablosu** büyütülür, gramer değil.

---

## 4. Rünler

### Rün kabul testi

Bir rün setine karar vermenin doğru yolu isim seçmek değil, ölçütü sabitlemek. Aday bir rün
şu beşinden birini geçemiyorsa sete girmez:

1. **Hem fiil hem sıfat olarak anlamlı mı?** Sıfat rolünde bir şey ifade etmeyen rün,
   cümlenin ikinci yarısında ölü ağırlıktır.
2. **Sıfat olarak silüeti değiştiriyor mu, sayıyı mı?** Sayı değiştiren rün §12'deki tuzağa
   düşer: gramer görünmez olur, oyuncu ezbere döner.
3. **Fiil olarak dünyada yaşıyor mu?** Anlık vuran fiil, sıfat kabul edemez (§8/T2).
4. **Diğerlerinden tür olarak mı farklı, derece olarak mı?** Hasar ekseninde kıyaslanabilen
   rünlerden biri hep en iyisi olur ve yaratıcılık ölür.
5. **İsminden tahmin edilebiliyor mu?** Tahmin edilemeyen rün, ezberlenecek bir şeydir.

Bu ölçüt tersinden okununca set kendiliğinden doğuyor: **iyi bir rün seti, aynı zamanda bir
etkiyi değiştirmenin eksenleri setidir.** Bir etkiyi daraltabilir, çoğaltabilir, savurabilir,
tutabilir ya da sürdürebilirsin — rünler bunlar.

### Hedef set (beş nokta dolu hâli)

İşlevler gerçek karardır; isimler tema kararına bağlıdır (özet §12 hâlâ açık) ve değişmesi
hiçbir şeyi bozmaz.

| Nokta | İşlev | Prototip ismi | Fiil olarak | Sıfat olarak | Hece |
|---|---|---|---|---|---|
| 1 | daralt / odakla | İĞNE | delici atılış, tek hedef | daralt, odakla, zırh del | *hi* |
| 2 | çoğalt / yay | SÜRÜ | yayılan sürü dalgası | çoğalt, genişlet | *hu* |
| 3 | tut / katılaştır | KABUK | kabuk kaldır, savunma duruşu | katılaştır, durdur, tut | *ho* |
| 4 | sürdür / birik | ZEHİR | zehir salgısı, yakın bulut | zehir kat, süreye yay | *he* |
| 5 | savur / kaldır | SARSINTI | yere çakma, şok dalgası | kinetik kat, savur, havalandır | *ha* |

### Prototip seti: üç rün

**İlk turda yalnızca 1 (İĞNE), 2 (SÜRÜ) ve 5 (SARSINTI) açık.** Sebepleri:

- Üçü de kabul testinin 2. maddesini temiz geçiyor: sıfat rolünde silüeti gözle görülür
  biçimde değiştiriyorlar (geniş dalga daralır, tek şey çoğalır, hedef havaya kalkar).
- §13'ün ilk dört sorusunu cevaplamaya üç rün yeter; beşinci soru genişlik ister ve sırası
  ikinci turdur.
- Üstünde en çok çalışılmış örnek zaten bu üçüyle kuruluyor: `5` → `5-1` → `5-1-2`.

**KABUK ve ZEHİR ikinci turda eklenir.** İkisi de sıfat rolünde kolayca "sayı değiştiren"
şeye kayar — "tut" bir yavaşlatma yüzdesine, "sürdür" bir zamanla-hasar değerine. Bu, tezahür
katmanı henüz kanıtlanmamışken testi zehirler: oyuncu grameri gözle öğrenemez, ezbere döner,
ve biz "sistem çalışmıyor" sonucunu yanlış yerden çıkarırız. Eklenirken ikisi de kabul
testinin 2. maddesinden ayrıca geçirilecek.

> Rün seti **veridir** — bu tasarımda değiştirilmesi en ucuz şey. Pahalı olan iki şey §3'teki
> gramer ve §8/T2'deki "anlık vuran fiil olamaz" kısıtıdır. Rünleri uzun uzun düşünmek yerine
> ilk telefon turundan sonra yenilemek doğru sıra.

### İlk turun sonucu: "çeşitlilik yok" (23 Ağustos)

Sahibi telefonda *"skillerde sorun yok ama çeşitlilik yok, 3 nokta açık diye mi"* dedi.
Cevap **hayır, rün sayısı yüzünden değil.** Beş rün, 1 fiil + en fazla 3 sıfat ve tekrar
serbest olduğu için `5 + 25 + 125 + 625 = 780` farklı cümle üretiyor; üç rünle bile 3 + 9 +
27 + 81 = **120 cümle** var. Elle yazılmış hiçbir yetenek listesi buna yaklaşmaz. Sıkıntı
sayıda değil, **ayırt edilebilirlikte**.

Ayırt edilebilirliğin iki kaynağı var ve prototipte ikisi de eksik:

1. **Silüetler tür olarak ayrışmıyor** — üçü de "bir efekt" gibi görünüyor. §13'ün 5. sorusu
   bunun düzeltmesini zaten yazmış: rün eklemek değil, türleri keskinleştirmek.
2. **Durum ve etkileşim tablosu hiç yok.** Cümleler birbirinden farksız hissediyor çünkü hepsi
   aynı boş bossa aynı şeyi yapıyor. [Özet §5](../tasarim-ozeti.md#5-yaratıcı-build-sistemi)'in üç
   katmanından (taşıyıcı / yük / durum tablosu) prototipte yalnızca birincisi tam: bossun
   ıslak, zırhı kırık ya da havada olması hiçbir cümlenin anlamını değiştirmiyor. Belge o
   katmanı "asıl sihir burada" diye işaretlemiş ve *"8 durum + kurallar, elle yazılmış 40
   yetenekten kat kat az iş"* demiş.

> **Çeşitlilik, durum tablosuyla gelir — rün havuzuyla değil.** Rün havuzunu büyütmek (KABUK
> ve ZEHİR'i açmak, sonra sınıf başına ayrı havuz) ucuz ve sırası gelecek; ama boş bir bossa
> beş rün de üç rün kadar tekdüze hisseder. Durum tablosu prototipten sonraki ilk büyük sistem.

### Türetilebilirlik örnekleri

Bunların hiçbiri elle tanımlanmamıştır; kurallardan doğar.

- `5-1` → şok dalgası tek hatta toplanır: yeri yaran dar bir fay hattı
- `5-1-2` → o hattın çatlaklarından sürü fışkırır, hat boyunca çoklu vuruş
- `5-1-2-4` → hat üzerinde kalıcı zehir şeridi bırakır
- `1-5` → delici atılış, deldikten sonra hedefi savurur (fiil değişti, cümle değişti)
- `5-1-1` → odaklamayı iki kez söylersin, hat daha da daralır ve derin deler

---

## 5. Cümle Kuralları

**Cümle = 1 fiil + en fazla 3 sıfat** (`maxSentenceDots = 4`). Dördüncü sıfat okunabilirliği
bitirir ve boss zaten cezalandırır.

**Her ön-ek geçerlidir.** Fiil, dokunulduğu anda dünyada başlar. Sıfatlar sonradan gelip
**zaten yaşayan** etkiyi değiştirir. "Çiz, bitir, sonucu bekle" yok.

**Uzatma iptal penceresine bağlıdır.** Sıradaki noktaya, o anki vuruşun penceresi kapanmadan
basılmalı. Pencereler **dünya zamanıyla** ölçülür (yavaş çekimde gerçek zamanda uzarlar —
mekanik tam olarak budur):

| Vuruş | Pencere |
|---|---|
| Fiil | 420 ms |
| 1. sıfat | 360 ms |
| 2. sıfat | 300 ms |

Pencere kapanırsa cümle kendiliğinden **çözülür** (kapanış vuruşu gelir). Noktada bekleme
(§3) süresince pencere durur; yoğunlaştırmak sıradaki noktaya basma hakkını yemez.

**On nokta çizmek "aynı komboyu tekrarlamak" değildir.** `1-2-3-4-5-1-2-3-4-5` şöyle bölünür:
`1-2-3-4` (cümle) → `5-1-2-3` (yeni cümle, fiil artık 5) → `4-5…` (üçüncü cümle başlar).
Kombo listesi yoktur; bu yüzden aynı on nokta farklı yerden başlatılınca bambaşka bir dizi
cümle üretir. Cümlenin nerede bittiği **ezberlenmez, görülür**: vuruş tamamlanır, sonuç
patlar, mürekkep söner.

### Kapanış vuruşu ve ödül

Ödül cümlenin içine yayılmaz, **sonuna** konur: son nokta ayrı ve daha büyük bir kapanış
vuruşu üretir.

- **Kapanışın türü son rüne bağlıdır** (zehirle kapatırsan yerde birikinti, sarsıntıyla
  kapatırsan havalandırma, kabukla kapatırsan sabitleme). Ödül *sadece hasar* olamaz;
  öyle olursa şekil ekseni süse döner.
- **Yarıda kalan cümle hiç ödeme yapmaz.** Vurulursan ya da cümle sürerken dodge düğmesine
  basarsan yatırdığın zaman batar. Uzun cümle "her zaman daha iyi" değil, **kumar**dır.

| Cümle | Süre | Toplam etki | Saniyedeki | Toparlanma |
|---|---|---|---|---|
| 1 nokta | 0.25 sn | 1.0 | 4.0 | 0.18 sn |
| 2 nokta | 0.50 sn | 2.4 | 4.8 | 0.26 sn |
| 3 nokta | 0.80 sn | 4.4 | 5.5 | 0.38 sn |
| 4 nokta | 1.20 sn | 7.0 | 5.8 | 0.55 sn |

Eğrinin şekli kasıtlıdır: uzatmanın karşılığı gerçekten var, ama kazanç düzleşiyor. Dördüncü
nokta neredeyse hiçbir şey katmaz — onu ancak **bedavaysa**, yani yavaş çekim penceresinde
çizersin. Uzunluğu dizginleyen dört fren: azalan getiri, tamamlama şartı, uzayan toparlanma,
ve açıklığın boyutunu bossun belirlemesi.

### Etkinin hasara çevrilmesi (23 Ağustos kararı)

Prototipin ilk turunda "toplam etki" hiçbir yere gitmiyordu: boss can göstergesi kozmetikti,
Core'da boss hasarı yoktu ve "boss nasıl ölür" hiç tanımlı değildi. Telefon turunun sonucu
bunu doğruladı — sahibi *"skill kullanmanın bedeli yok"* ve *"cümlelerin karşılığını
anlamıyorum"* dedi. Harcamanın gittiği bir yer olmadığı sürece optimal oynayış spam'dir.

Tablodaki **toplam etki, bossun canından düşen sayıdır**: `closingDamagePerEffect = 1.0`
(ayarlanabilir, başlangıç değeri 1'e 1). Yani 4 noktalı cümle 7, düz vuruş 1 hasar verir.
Boss canı 120 (§11) → en iyi oynanışta ~17 kapanış, karışık oynanışta 25–30 kapanış.

**İki koşul birlikte tutulmak zorunda, biri tek başına yeterli değil:**

1. **Kapanışın türü son rüne bağlı kalır.** Hasar eklenince tür ekseni bırakılırsa §12'nin
   "ödülün sadece hasar olması" tuzağına düşülür: oyuncu en yüksek sayıyı veren cümleyi bulur,
   780 cümle 1 cümleye iner ve gramer ölür. Sarsıntıyla kapatmak savurur, kabukla kapatmak
   sabitler, zehirle kapatmak yerde birikinti bırakır — hasar bunun **yanına** gelir, yerine
   geçmez.
2. **Hasar sayısı ekranda yazmaz** (§8/T3, §12). Oyuncu harcamasını boss canının azalmasından
   okur, uçan sayılardan değil. Ayar panelinde varsayılan kapalı bir hasar göstergesi
   bulunabilir — o bir **kumpas**, his kanalı değil; ayar yaparken cümlenin ne yaptığını
   ölçmek içindir ve his kanıtı olarak kullanılmaz.

### Düz vuruş ve erken kapanış

**Düz vuruş, gramerin dışındadır ama ekonomisi tablodan gelir.** Merkeze tıklamak tek noktalı
bir cümlenin anında kapanmasıdır: yukarıdaki tablonun 1 nokta satırı (0.25 sn, 1.0 etki,
0.18 sn toparlanma). Altıncı bir rün **değildir** — merkez bir kelime değil, bir düğmedir;
üstüne sıfat binmez. Hangi fiille vurduğu veridir (prototipte 5/SARSINTI).

**Erken kapanış**, cümle kurulurken merkeze basmaktır: pencerenin kendiliğinden dolmasını
beklemeden o uzunluğun ödemesini alırsın. Dodge'dan farkı budur ve fark pazarlıksızdır —
merkez **öder**, dodge **batırır**. Oyuncu ikisini karıştırırsa sistem cezalandırıcı hissedilir.

### Toparlanma girdi kilididir

Tablodaki toparlanma süresi bir poz değil, **girdinin kilitli olduğu süredir**. Kilit üç şeyle
kesilir: düz vuruş, yeni bir fiil (köşeye basmak) ve dodge.

Kesme, uzunluk kumarının karşılığındaki beceri eksenidir: kısa cümle kurup araya düz vuruş
dokuyan oyuncu, kilidi erken keserek saniyedeki etkisini yükseltir. 1 noktalık cümlede kesilen
0.18 sn, 4 noktalıkta 0.55 sn — yani kesme becerisi kısa cümleyi ödüllendirir, ama uzun cümle
hâlâ daha çok toplam etki verir. İki eksen birbirini dengeler.

- **Ödül kesilen süredir, bir çarpan değildir.** "İptal edilmiş vuruş %X fazla vurur" diye bir
  kural olamaz (§12, ve sıfatların sayı değiştirememesiyle aynı gerekçe).
- **Kapanış patlaması kesilmez.** Kesilen şey yalnızca oyuncunun kilidi; ödenmiş kapanış
  kendi zamanlamasıyla (§8/T5'teki gecikme ve sessizlikle) yine gelir.
- **Toparlanmadayken dodge ücretsizdir.** Kilidi keser, ama ödenmiş kapanışı geri almaz;
  iptal cezası yalnızca cümle **kurulurken** işler.
- Kilit süreleri ve nelerin kestiği ayarlanabilir veridir; telefonda §13'ün 3. sorusuyla
  birlikte ayarlanır.

---

## 6. Dodge ve Derecelendirme

```
startupMs 20 · iframeStartMs 0 · iframeMs 260 · distance 3.8 m
durationMs 260 · curveExp 3.2 · glideTailMs 220 · cooldownMs 420
```

Hareket eğrisi `s(u) = 1 - (1-u)^curveExp`, ardından `glideTailMs` boyunca sönen artık hız —
"yağ gibi kayma" hissinin kaynağı, dodge'un sert durmaması.

**Sıyırma tespiti:** bossun vuruş anında oyuncu etki hacminin içindeyse ve dokunulmazlık
penceresi açıksa sıyırma sayılır.

| Ölçü | Tanım |
|---|---|
| `gap` | vuruş anı − dodge basma anı (ne kadar geç bastıysan o kadar hassas) |
| `reaction` | dodge basma anı − telegraf başlangıcı (ekranda gösterilen sayı) |

| Derece | Eşik (`gap`) | Yazı |
|---|---|---|
| MÜKEMMEL | ≤ 90 ms | "tepki süren mükemmel" |
| HARİKA | ≤ 160 ms | "neredeyse kusursuz" |
| TEMİZ | ≤ 220 ms | "iyi okudun" |
| SIYIRDI | 221 ms – pencere sonu | "biraz erken bastın" |

**Eşiklerin hepsi i-frame penceresinin içinde kalmak zorunda.** `gap`, başarılı bir dodge'da
tanımı gereği `iframeMs`'den küçüktür — pencere kapandıktan sonra gelen vuruş zaten isabet
eder ve derece değil *sebep* üretir. Dolayısıyla son eşik (`temizGapMaxMs`) pencereden büyük
olursa en alt derece hiç doğmaz ve §7'deki `slowmoMinGrade` işlevsizleşir: her başarılı dodge
yavaş çekim alır. Pencere şu an 260 ms, son eşik 220; aradaki bant SIYIRDI'dır ve ödül vermez.
`iframeMs` değişirse eşikler de değişmeli.

Vurulma hâlinde de sebep yazılır — dodge bastı ama i-frame bitmişse "erken bastın",
hiç basmamışsa "geç kaldın". Her ölüm açıklanabilir olmalı.

**Ekranda gösterim:** sağ kenarda, parlak (katmanlı glow), büyük punto: `0.45 sn` +
derece yazısı + seri sayacı. Yazının animasyonu **ölçeklenmemiş saatle** çalışır, yani
dünya yavaşken bile net ve keskin görünür.

---

## 7. Yavaş Çekim

```
factor 0.22 · rampDownMs 55 · holdMs 900 · rampUpMs 600 (yumuşak geçiş)
audioLowpassHz 700 · slowmoMinGrade = TEMİZ
```

**Yavaş çekim tavanı yükseltmez, tavana ulaşmanı sağlar.** İptal pencereleri dünya zamanıyla
ölçüldüğü için, dünya yavaşladığında parmağın gerçek zamanda daha çok vakit bulur. Cümle
sınırı hâlâ 4 noktadır (`slowmoBonusDots = 0`, ayarlanabilir).

Kur karşılığı böylece net: bir mükemmel dodge ≈ iki ekstra nokta ≈ yaklaşık iki katı etki.
Ekrandaki "0.45 sn" yazısı, kaç nokta hakkı kazandığını söyleyen şeydir.

**Süre neden bu kadar uzun (T8.2'de ölçülerek değişti).** İlk sayılar `holdMs 190 · rampUpMs
420` idi ve o profille yavaş çekim **hiçbir** dokunuş temposunda kelime sayısını değiştirmiyordu
— ödül kozmetikti. Sebep faktör değil profilin yönü: iptal pencereleri cümle büyüdükçe daralıyor
(420 → 360 → 300) ama yavaş çekim zamanla zayıflıyor, yani en dar pencere yavaş çekim bittikten
sonraya düşüyordu. Kazanç, ihtiyaç olmayan yere (ilk boşluk) gidiyordu. Yukarıdaki süre, bu
bölümün kendi kurundan (iki ekstra nokta) geriye çözüldü. Ölçülen kelime sayıları:

| dokunuş aralığı | yavaş çekim yok | 190/420 | **900/600** |
|---|---|---|---|
| 350 ms | 3 | 3 | 4 |
| 400 ms | 2 | 2 | **4** |
| 450 ms | 2 | 2 | 3 |

Daha uzun tutmak (ör. `holdMs 1200`) her tempoda 4 verir ve beceri bandını siler — ödül
otomatikleşir. Bu yüzden süre yukarıdan da sınırlı.

---

## 8. Tezahür Kuralları (en kritik bölüm)

Spellisimo'nun hatası çizimde değil çıktıdaydı: jest, animasyonun yerine geçmişti. Şekli
çiziyorsun, sonuç anında bir istatistik olarak beliriyor — yani kılık değiştirmiş bir menü
seçimi. Aşağıdaki beş kural bunun panzehiri.

**T1 — Girdi sonucun kendisi değil, gövdeye verilen emirdir.** Karakter her rün için bir
hamle yapar; dizi bir koreografiye dönüşür ve sen onu izlersin.

**T2 — Hiçbir fiil anlık vuramaz.** Sıfatın değiştirebilmesi için etkinin dünyada hâlâ
yaşıyor olması gerekir: dalga yol alır, sürü yürür, kabuk yükselir. Bu, rün seti seçimini
bağlayan bir kısıttır. Bedava kazancı: **iptal penceresi görünür olur** — kalan süreyi
arayüz çubuğundan değil, dalganın nerede olduğuna bakarak bilirsin.

**T3 — Her sıfat silüeti değiştirmek zorunda, sayıyı değil.** "%30 daha fazla hasar" diye bir
sıfat olamaz. Dalga toplanır, hat daralır, şekil değişir. Oyuncu grameri gözle öğrenemezse
ezbere döner.

**T4 — Sonuç kalıcı dünya değişimi bırakır.** Sayı değil: kabuk çatlar ve çatlak kalır, zehir
yere birikir ve orada durur. Kanıt bırakmayan vuruş olmamış vuruştur.

**T5 — Bedel görünür, sonuç gecikir.** Uzun cümlenin sonunda toparlanma pozu vardır (açık,
nefes nefese). Kapanış, kısa bir sessizlikten sonra patlar. Anında gelen sonuç işlem gibi
hissettirir; yarım saniye gecikip gelen sonuç olay gibi.

Ayrıca özet §6'daki bedava his araçları burada da geçerli: hitstop, impact frame, kamera
yumruğu, hız rampası, afterimage, vuruş sonrası sessizlik. **Düşman tepki vermeli.**

### Başlangıç sayıları

Bu tablo tahmindir; asıl değerler telefonda elde bulunacak (bkz.
[Teknoloji Kararları §6](../teknoloji-kararlari.md#6-ayarlanabilirlik--ve-telefonda-ayar-sorunu)).
Kodda sayı uydurmamak için buradan alınır.

| Ayar | Değer | Ne |
|---|---|---|
| `hitstopPerfectMs` | 90 | Sıyırma anında dondurma |
| `hitstopPlayerHitMs` | 130 | Oyuncu vurulunca |
| `hitstopBossHitMs` | 70 | Bossa isabet |
| `impactFrameMs` | 33 | Tek karelik beyaz/ters renk patlaması |
| `postHitSilenceMs` | 120 | Vuruş sonrası sessizlik |
| `cameraPerfectZoomKick` | 0.14 | Sıyırmada FOV sıçraması |
| `cameraDodgeZoomKick` | 0.08 | Normal dodge'da |
| `cameraRollDeg` | 1.5 | Kısa rotasyon yumruğu |
| `shakePerfectPx` | 6 | Sıyırma sarsıntısı |
| `shakeHitPx` | 14 | Vurulma sarsıntısı |
| `shakeDecay` | 6 | Sarsıntı sönme hızı |
| `afterimageCount` | 7 | Dodge sırasında hayalet kopya |
| `afterimageLifeMs` | 320 | Kopyanın ömrü |

Tepki yazısı (§6 gösterim) için: `readoutSizePx` 96, `readoutGlow` 34, `readoutHoldMs` 900,
`readoutFadeMs` 500, `readoutPunchScale` 1.45.

> **Tepki süresi bir ayar değildir.** Çalışma anında ölçülür (`basma anı − telegraf
> başlangıcı`). "0.45 sn" belgede yalnızca örnektir; sabit olarak kodlanmaz.

---

## 9. Ses ve Hece

Her rünün bir hecesi var (§4 tablosu). Cümle uzadıkça hece dizisi ve perde yükselir,
kapanışta çözülür. Skyrim'in çığlığının **yapısı** alınır (ha / ha-hi / ha-hi-hu), ekonomisi
alınmaz — orada uzun her zaman daha güçlüdür, dolayısıyla karar yoktur.

Üç işe yarar: göze bakmadan onay, kayıt/seyir değeri, ve **co-op** — takım arkadaşın senin
cümlenin oluştuğunu duyar, kimin ne kurduğu kulaktan bellidir.

Ses ayrıca yavaş çekimde alçak geçiren filtreden geçer (§7).

---

## 10. Renk Dili (pazarlıksız)

Özet §6 kuralı: **kırmızı-turuncu yalnızca düşman tehdidine ayrılmıştır.**

| Katman | Renk |
|---|---|
| Boss telegrafı ve saldırıları | `#FF4D24`, `#FF9A3C` — başka hiçbir yerde kullanılmaz |
| Oyuncu efektleri | camgöbeği `#5FF0FF`, mor `#B98CFF` |
| Mürekkep izi | mor → camgöbeği geçişi |
| Zehir birikintisi | asit yeşili `#9BE83C` |

Boss telegrafı her zaman en üst ve en okunabilir katmandadır. Kendi efektin bossun
telegrafını yutarsa oyun adaletsiz hisseder ve 1. sütun çöker.

---

## 11. Boss (prototip: tek saldırı, üç ritim)

```
YERE ÇAKMA — windup 640 ms · active 90 ms · recovery 720 ms · radius 5.4 m · damage 22
idle bekleme 700–1500 ms · yaklaşma hızı 2.2 m/s
boss canı 120 · oyuncu canı 22
```

Telegraf okunabilir olmalı: hazırlık evresi + ışık + yükselen ses. Vuruş, aktif pencerenin
başında tek karede çözülür (frame verisiyle düşünmeyi kolaylaştırır).

Ölüm cezası neredeyse sıfır: ölümden sonra ≤2 saniyede tekrar dövüşte (özet §4, Sütun 1).

**Boss ölümü prototipte bir noktalama işaretidir, bitiş değil.** Can 0'a düşünce kısa bir
yavaş çekim + çökme pozu gelir, sonra boss **tam canla yeniden doğar**. Gerekçe: ayar turunun
kesilmemesi gerekiyor — zafer ekranı, ilerleme veya ödül kurgusu bu belgenin kapsamı dışında
ve [özet §12](../tasarim-ozeti.md#12-açık-konular)'de hâlâ açık.

### Çakma varyantları

İlk tur tek saldırıyla yapıldı ve amacına ulaştı: §13'ün 1. sorusu (*sırtın ürperiyor mu*)
**evet**. Turda çıkan yeni şikâyet tekdüzelik. Düzeltmesi **yeni bir saldırı değil**, aynı
çakmanın üç ritmi. Sebep: yeni saldırı öğrenilecek yeni bir desen demek; aynı saldırının
varyantı ise **aynı** deseni her seferinde yeniden okumaya zorlar. Telegraf okuma bir kez
öğrenilen refleks olmaktan çıkıp sürekli sorulan bir soruya döner.

| Varyant | windup | radius | Ayırt edici tell |
|---|---|---|---|
| YAKIN | 640 ms | 5.4 m | temel — mevcut davranış |
| GEÇ | 900 ms | 5.4 m | ton daha yavaş yükselir; hazırlık pozu daha uzun tutar |
| GENİŞ | 640 ms | 8.0 m | disk baştan itibaren gözle görülür biçimde büyük |

**Pazarlıksız kısıt: varyant vuruştan ÖNCE ayırt edilebilir olmalı.** Özet §4/Sütun 1 "her
ölüm adil olmalı" diyor, §7 ise zorluğun *icra hassasiyetinden* gelmesini, gizemden veya
şanstan gelmemesini şart koşuyor. Windup sırasında hangi varyant olduğu okunamıyorsa varyant
sistemi bir zar atışına döner ve 1. sütun çöker. Her varyantın tell'i bu yüzden tabloda ayrı
bir sütun: süre farkı **sesle**, menzil farkı **diskle** okunur.

**GENİŞ'in yan faydası ödül bandını açması.** Dodge 4.05 m taşıyor ve boss ~1.70 m'de duruyor;
5.4 m'lik hacimde `gap` 197 ms'yi geçen dodge hacmin dışına çıkıyor, yani TEMİZ ve SIYIRDI
bantları kaçan dodge'da hiç doğmuyor (ölçüm tablosu `docs/durum.md`, T8 denetimi 8. madde) ve
§7'nin `slowmoMinGrade = TEMİZ` ayarı fiilen HARİKA'ya kayıyor. 8.0 m'lik hacimde aynı dodge
hacmin içinde kalır ve alt dereceler gerçekten üretilir. Yani varyant hem çeşitlilik hem
derecelendirmenin kapalı kalan yarısını açıyor.

> Üç varyantın sayıları **başlangıç değeridir** ve telefonda ayarlanacak; `docs/durum.md`'de
> uydurma olarak kayıtlı. Sabit olan şey sayılar değil, yukarıdaki ayırt edilebilirlik kısıtı.

---

## 12. Kaçınılacaklar

| Tuzak | Neden ölümcül | Panzehir |
|---|---|---|
| Dizinin anlamı tabloda yazması | Şifre olur, ezberlenir, keşif ölür | Her dizi kuralla çözülür; geçersiz dizi yok |
| Uzunluk = güç | Herkes en uzunu spamlar, boss okumak gereksizleşir | Azalan getiri + tamamlama şartı + toparlanma |
| Ödülün sadece hasar olması | Şekil ekseni süse döner | Kapanışın türü son rüne bağlı |
| Sıfatın sayıyı değiştirmesi | Gramer görünmez olur, ezbere dönülür | T3: silüet değişecek |
| İptal ödülünün sayı olması | Gramerin yanına ikinci bir "kombo çarpanı" sistemi doğar | Ödül kesilen **süre**dir (§5) |
| Gücün ergonomik zorluğa bağlanması | El/telefon boyu dengeyi belirler, el ağrır | Bedel mesafe/zaman olur |
| Gramere yeni eksen eklemek | Öğrenilecek şey artar, ezbere kayar | Rün havuzunu ve durum tablosunu büyüt |
| Girdi katmanının gizemli olması | Ezber üretir | Girdi mantıklı, **keşif dünyada** |

> **Ezber girdiden çıkar, keşif dünyaya konur.** "Hangi kombo ne yapar" hiç ezberlenmeyecek;
> "hangi durumda ne işe yarar" yıllarca keşfedilecek.

---

## 13. Prototipin Cevapladığı Sorular

Sırayla, ve öncekine "evet" denmeden sonrakine geçilmez:

> **Durum (23 Ağustos, telefon turu — ayrıntı `docs/his-kontrol-listesi.md`):**
> 1. soru **evet** (*"hepsi iyi, hisler iyi"*). 2. soru **evet**, duvar aşıldı (boss görünüyor,
> pencere ödül olarak da yeniden pozisyon olarak da kullanılıyor). 3–5 **açık**, ve üçünün
> tıkandığı yer aynı: mekanik okunuyor ama temsil soyut kalıyor, bossun tek saldırısı tekdüze
> ve harcamanın gittiği bir yer yok. Düzeltmesi T11.1–T14 (bkz. `docs/gorev-listesi.md`);
> sanat katmanı ondan sonra.

1. **Telefonda tam zamanında dodge atıp bossun saldırısını sıyırdığında sırtın ürperiyor mu?**
   (Özet §10'un asıl sorusu.)
2. **Yavaş çekim penceresinde cümle kurmak, bossu okumanın ödülü gibi mi hissediyor —
   yoksa gözünü bossdan mı ayırıyor?** Bütün türün etrafından dolaştığı duvar bu.
3. **Parmağımla karakteri yazıyor muyum, yoksa yönetiyor muyum?** (Her ön-ek geçerli
   olduğu için sonucu izlerken uzatabiliyor muyum?)
4. **Çizdiğim şeyin dünyada canlandığını izliyor muyum, yoksa hasar mı verdim?**
5. **İki oyuncu aynı bossu farklı cümlelerle geçebiliyor ve ikisi de çözümü kendi bulmuş
   gibi hissediyor mu?** Herkes aynı cümleyi çiziyorsa bir eksen çökmüş demektir; düzeltmesi
   rün eklemek değil, rünlerin türlerini daha keskin ayırmaktır.

2. soruya "hayır" çıkarsa mühürler dövüş öncesi hazırlığa kayar ve tasarım baştan düşünülür.
Bunu 6. ayda değil 2. haftada öğrenmek prototipin bütün amacıdır.
