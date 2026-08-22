# Dövüş Sistemi — Alfa Spesifikasyonu

> Bu belge, dövüş sisteminin **tek doğruluk kaynağıdır**. Ajanlara verilen her görev bu
> belgeye referans verir. Buradaki sayılar başlangıç değerleridir ve **hepsi ayarlanabilir
> veri** olarak kodlanır (bkz. [Tasarım Özeti §8](tasarim-ozeti.md#-en-kritik-mimari-kural)).
>
> Tarih: 20 Ağustos 2026 · Durum: alfa, prototip öncesi · Bağlam: [Tasarım Özeti](tasarim-ozeti.md)

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
factor 0.22 · rampDownMs 55 · holdMs 190 · rampUpMs 420 (yumuşak geçiş)
audioLowpassHz 700 · slowmoMinGrade = TEMİZ
```

**Yavaş çekim tavanı yükseltmez, tavana ulaşmanı sağlar.** İptal pencereleri dünya zamanıyla
ölçüldüğü için, dünya yavaşladığında parmağın gerçek zamanda daha çok vakit bulur. Cümle
sınırı hâlâ 4 noktadır (`slowmoBonusDots = 0`, ayarlanabilir).

Kur karşılığı böylece net: bir mükemmel dodge ≈ iki ekstra nokta ≈ yaklaşık iki katı etki.
Ekrandaki "0.45 sn" yazısı, kaç nokta hakkı kazandığını söyleyen şeydir.

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
[Teknoloji Kararları §6](teknoloji-kararlari.md#6-ayarlanabilirlik--ve-telefonda-ayar-sorunu)).
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

## 11. Boss (prototip: tek saldırı)

```
YERE ÇAKMA — windup 640 ms · active 90 ms · recovery 720 ms · radius 5.4 m · damage 22
idle bekleme 700–1500 ms · yaklaşma hızı 2.2 m/s
```

Telegraf okunabilir olmalı: hazırlık evresi + ışık + yükselen ses. Vuruş, aktif pencerenin
başında tek karede çözülür (frame verisiyle düşünmeyi kolaylaştırır).

Ölüm cezası neredeyse sıfır: ölümden sonra ≤2 saniyede tekrar dövüşte (özet §4, Sütun 1).

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
