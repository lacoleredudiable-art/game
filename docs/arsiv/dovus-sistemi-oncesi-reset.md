# Dövüş Sistemi

> **Durum: yeniden tasarım açık (29 Ağustos 2026).** Rün seti ve mekanikler sıfırdan
> düşünülüyor. Alfa spesifikasyonunun tamamı `docs/arsiv/dovus-sistemi-v1.md`'de duruyor —
> **bağlayıcı değil**, oradaki bir karar buradakiyle çelişirse geçerli olan burasıdır.
>
> Bu belge dört iş yapar:
> **§1** değişmeyecek kısıtlar · **§2** kodda şu an çalışan mekaniklerin tarifi ·
> **§3** bütün sayılar (ajan sayı uydurmasın diye) · **§4** yeni tasarımın yazılacağı yer.
>
    10|> Prototip kodu duruyor ve çalışıyor; §2 bir tasarım kararı değil, **envanterdir**.
> Yeni tasarım §2'nin herhangi bir satırını değiştirebilir.

---

## 1. Pazarlıksız kısıtlar

Bunlar rün setinden ve mekanikten bağımsız; hangi tasarıma geçilirse geçilsin geçerli.
`AGENTS.md`'deki değişmez kuralların tasarım tarafı.

    20|1. **Hiçbir fiil anlık vurmaz.** Etki dünyada yaşamak zorunda (yol alır ya da sürer). Anlık
   vuran fiil sonradan gelen sıfatı kabul edemez — girdi, sonucun kendisi değil gövdeye
   verilen emirdir. Yaşayan etkinin bedava kazancı: kalan süre arayüz çubuğundan değil,
   etkinin nerede olduğuna bakarak okunur.
2. **Sıfat silüeti değiştirir, sayıyı değil.** "%30 daha fazla hasar" diye bir sıfat olamaz.
   Oyuncu grameri gözle öğrenemezse ezbere döner.
3. **Hiçbir dizi elle tanımlanmaz.** Kombo tablosu, geçerli diziler listesi, "şu sıra şunu
   yapar" eşleştirmesi yok. Her dizi kuralla çözülür, geçersiz dizi yoktur.
4. **Sonuç kalıcı dünya değişimi bırakır.** Kanıt bırakmayan vuruş olmamış vuruştur.
5. **Bedel görünür, sonuç gecikir.** Anında gelen sonuç işlem gibi hissettirir; kısa
    30|   sessizlikten sonra gelen sonuç olay gibi.
6. **Her ölüm açıklanabilir.** Zorluk icra hassasiyetinden gelir, gizemden veya şanstan
   gelmez. Boss tehdidi her zaman en okunabilir katmanda; kendi efektin telegrafı yutamaz.
7. **Kırmızı-turuncu yalnızca boss tehdidi.** Oyuncu efektleri camgöbeği/mor.
8. **Ayarlanabilir her şey veri.** His sayısı koda gömülmez.
9. **Güç ergonomiye bağlanmaz.** El boyu ve telefon boyu dengeyi belirlemez; bedel mesafe
   ya da zaman olur.

### Kaçınılacaklar

    40|| Tuzak | Neden ölümcül |
|---|---|
| Dizinin anlamının tabloda yazması | Şifre olur, ezberlenir, keşif ölür |
| Uzunluk = güç | Herkes en uzunu spamlar, bossu okumak gereksizleşir |
| Ödülün sadece hasar olması | Şekil ekseni süse döner, 780 cümle 1 cümleye iner |
| Sıfatın sayıyı değiştirmesi | Gramer görünmez olur, ezbere dönülür |
| İptal/kesme ödülünün sayı olması | Gramerin yanına ikinci bir "kombo çarpanı" sistemi doğar |
| Gramere yeni eksen eklemek | Öğrenilecek şey artar, ezbere kayar |
| Girdi katmanının gizemli olması | Ezber üretir; ezber girdiden çıkar, keşif dünyaya konur |

    50|---

## 2. Kodda şu an çalışan mekanikler (envanter)

**Girdi.** Sol yarı dinamik sanal çubuk (hareket). Sağ yarı beşgen 5 nokta, sürükleyerek
cümle. Beşgen merkezi kısa dokunmayla düz vuruş; cümle kurulurken aynı dokunma erken kapanış
(**öder**, iptal etmez). Beşgenin dışında ekrana sabit disk dodge (cümle sürerken **batırır**).
Beşgen ve disk ekrana sabittir, sağ/sol el için aynalanır. Her kaydedilen nokta kısa titreşim
+ hece sesi verir — gözle onay beklemek bossu kaçırtır.

**Gramer.** Üç kural, hepsi karşılık: (K1) ilk dokunulan nokta fiil, sonrakiler sıfat — `5-1`
    60|ile `1-5` aynı şey değil. (K2) komşu noktaya kısa sıçrama hafif/hızlı sıfat, karşıya uzun
sıçrama ağır/yavaş sıfat. (K3) tekrar yoğunlaştırır: noktada beklemek ya da o noktaya geri
dönmek. Bekleme iptal penceresini **dondurur**; geri dönmek bir sıfat yuvası harcar.

**Cümle.** 1 fiil + en fazla 3 sıfat. Her ön-ek geçerli: fiil dokunulduğu anda dünyada başlar,
sıfatlar zaten yaşayan etkiyi değiştirir. Uzatma iptal penceresine bağlı ve pencereler **dünya
zamanıyla** ölçülür. Pencere kapanırsa cümle kendiliğinden çözülür. Ödül cümlenin sonuna konur:
kapanış vuruşunun **türü son rüne bağlıdır**, toplam etki bossun canından düşer. Yarıda kalan
cümle hiç ödeme yapmaz — uzun cümle "daha iyi" değil, kumardır. Kapanıştan sonra toparlanma
bir **girdi kilididir** ve üç şeyle kesilir: düz vuruş, yeni fiil, dodge. Ödül kesilen
süredir, çarpan değil.
    70|
**Dodge ve derecelendirme.** Hareket eğrisi `s(u) = 1 - (1-u)^curveExp`, ardından sönen artık
hız (kayma kuyruğu). Bossun vuruş anında oyuncu etki hacmindeyse ve i-frame açıksa sıyırma.
Derece `gap` (vuruş anı − basma anı) ile verilir; ekranda gösterilen sayı `reaction`
(basma anı − telegraf başlangıcı) ve bu bir ayar değil, çalışma anında ölçülür. Vurulmada da
sebep yazılır ("erken bastın" / "geç kaldın"). **Kısıt:** en alt derece eşiği i-frame
penceresinden küçük kalmalı, yoksa o derece hiç doğmaz ve yavaş çekimin alt sınırı kayar.

**Yavaş çekim.** Tavanı yükseltmez, tavana ulaşmanı sağlar: pencereler dünya zamanıyla
ölçüldüğü için dünya yavaşlarken parmak gerçek zamanda daha çok vakit bulur. Cümle sınırı
hâlâ 4 nokta. Kur: bir mükemmel dodge ≈ iki ekstra nokta. Profilin **yönü** faktörden daha
    80|önemli — süre kısa tutulursa en dar pencere yavaş çekim bittikten sonraya düşer ve ödül
kozmetik kalır; çok uzun tutulursa her tempoda 4 nokta gelir ve beceri bandı silinir.

**Boss.** Tek saldırı (YERE ÇAKMA), üç ritim: YAKIN / GEÇ / GENİŞ. Vuruş aktif pencerenin
başında tek karede çözülür. **Varyant vuruştan önce ayırt edilebilir olmak zorunda** — süre
farkı sesle (ton daha yavaş yükselir, poz daha uzun tutulur), menzil farkı diskle okunur.
Aynı varyant en fazla 2 kez üst üste gelir. Can 0'a düşünce yavaş çekim + çökme pozu, sonra
tam canla yeniden doğuş: prototipte ölüm bir noktalama işareti, bitiş değil. Oyuncu ölümünün
cezası ≤2 saniye.

**Tezahür.** Etki seyahat eder, silüete morph olur, kapanışta patlar ve yerde kalıcı iz
    90|bırakır. Silüet eksenleri: odakla / del / yay / kaldır. Sıfat bu eksenleri oynatır, hiçbir
sayıyı çarpmaz.

**Ses.** Her rünün bir hecesi var; cümle uzadıkça hece dizisi ve perde yükselir, kapanışta
çözülür. Üç işe yarıyor: göze bakmadan onay, seyir değeri, ve co-op (takım arkadaşın senin
cümlenin oluştuğunu duyar).

---

## 3. Sayılar

   100|> **Ajan buradan alır.** Bir his değeri gerekiyorsa bu tablolardan; yoksa varsayılan koyup
> `docs/durum.md`'ye "uydurma" olarak geçer. Hepsi çalışma anında ayar panelinden
> değiştirilebilir; buradakiler **varsayılanlar**.
>
> `[u]` = spec'te hiç olmayan, iş yapabilmek için uydurulmuş değer.

**Girdi:** `tapMaxMs` 180 · `tapMaxMoveDp` 12 · yürüme 4.5 m/s `[u]` · arena yarım kenarı
12 m `[u]` · çubuk 72 dp `[u]` · nokta titreşimi 30 ms `[u]` · beşgen merkezi x 0.72 / y 0.40 `[u]`

**Cümle:** `maxSentenceDots` 4 · iptal pencereleri **420 / 360 / 300 ms** · `dwellMs` 220 ·
   110|`dwellMaxStacks` 2 · `closingDamagePerEffect` 1.0

| Cümle | Süre | Toplam etki | Saniyedeki | Toparlanma (girdi kilidi) |
|---|---|---|---|---|
| 1 nokta | 0.25 sn | 1.0 | 4.0 | 0.18 sn |
| 2 nokta | 0.50 sn | 2.4 | 4.8 | 0.26 sn |
| 3 nokta | 0.80 sn | 4.4 | 5.5 | 0.38 sn |
| 4 nokta | 1.20 sn | 7.0 | 5.8 | 0.55 sn |

Eğrinin düzleşmesi kasıtlı: dördüncü nokta neredeyse hiçbir şey katmaz, onu ancak bedavaysa
   120|(yavaş çekim penceresinde) çizersin.

**Dodge:** `startupMs` 20 · `iframeStartMs` 0 · `iframeMs` 260 · `distanceM` 3.8 ·
`durationMs` 260 · `curveExp` 3.2 · `glideTailMs` 220 · `cooldownMs` 420
→ ölçülen gerçek yer değiştirme **4.05 m** (3.8 + kayma kuyruğu)

**Derece eşikleri (`gap`):** MÜKEMMEL ≤ 90 ms · HARİKA ≤ 160 ms · TEMİZ ≤ 220 ms ·
SIYIRDI 221 ms – pencere sonu (ödül vermez)

**Yavaş çekim:** `factor` 0.22 · `rampDownMs` 55 · `holdMs` 900 · `rampUpMs` 600 ·
   130|`audioLowpassHz` 700 · `slowmoMinGrade` TEMİZ · `slowmoBonusDots` 0

**Boss:** can 120 · oyuncu canı 22 `[u]` · idle bekleme 700–1500 ms · yaklaşma 2.2 m/s ·
active 90 ms · recovery 720 ms · damage 22 · `maxSameVariantStreak` 2

| Varyant | windup | radius | Tell |
|---|---|---|---|
| YAKIN | 640 ms | 5.4 m | temel |
| GEÇ | 900 ms `[u]` | 5.4 m | ton daha yavaş yükselir, poz daha uzun |
| GENİŞ | 640 ms | 8.0 m `[u]` | disk baştan itibaren gözle görülür biçimde büyük |

   140|**His:** `hitstopPerfectMs` 90 · `hitstopPlayerHitMs` 130 · `hitstopBossHitMs` 70 ·
`impactFrameMs` 33 · `postHitSilenceMs` 120 · `cameraPerfectZoomKick` 0.14 ·
`cameraDodgeZoomKick` 0.08 · `cameraRollDeg` 1.5 · `shakePerfectPx` 6 · `shakeHitPx` 14 ·
`shakeDecay` 6 · `afterimageCount` 7 · `afterimageLifeMs` 320

**Tepki yazısı:** `readoutSizePx` 96 (**tavan**, bant genişliğine göre düşürülür) ·
`readoutGlow` 34 · `readoutHoldMs` 900 · `readoutFadeMs` 500 · `readoutPunchScale` 1.45

**Renk (pazarlıksız):** boss telegrafı `#FF4D24` / `#FF9A3C` — başka hiçbir yerde
kullanılmaz. Oyuncu camgöbeği `#5FF0FF`, mor `#B98CFF`. Mürekkep izi mor → camgöbeği.
   150|Asit yeşili `#9BE83C`.

**Ölçüm/bütçe:** hedef 60 fps · kare süresi penceresi 0.5 sn `[u]` · yerdeki iz tavanı 60 `[u]`

**Tezahür sayıları** (`ManifestationTuning`) tamamı `[u]`: hız/yarıçap/silüet adımları, sönme
0.35 sn, kapanış patlaması 0.45 sn, delici hız katkısı 0.25, koridor yarı genişliği 2.2 / 0.35.
Bunlar hiç telefonda ayarlanmadı.

---

## 4. Yeniden tasarım (boş — sahibi doldurur)

   160|Buraya yazılan şey bağlayıcı olur ve görevler buradan doğar. §1 bu bölümün üstünde kalır:
yeni tasarım §1'i esnetmek istiyorsa o **ayrı bir karardır**, sessizce geçilmez.

Rün seti veridir ve bu tasarımda değiştirilmesi en ucuz şey; pahalı olan §1'in 1. ve
2. maddesi. Yeni bir rün önerisi şu beş soruyu geçmeden sete girmez:

1. Hem fiil hem sıfat olarak anlamlı mı?
2. Sıfat olarak **silüeti** mi değiştiriyor, sayıyı mı?
3. Fiil olarak dünyada yaşıyor mu?
4. Diğerlerinden **tür** olarak mı farklı, derece olarak mı? (Hasar ekseninde
   kıyaslanabilen rünlerden biri hep en iyisi olur ve yaratıcılık ölür.)
   170|5. İsminden tahmin edilebiliyor mu?

`tools/skill-preview/` bu bölümü doldurmanın aracı: `runes.json`'a aday seti yaz,
`python3 preview.py 5-1-2-4` gramerin o setle ne ürettiğini Unity'ye hiç dokunmadan anlatır.

### 4.1 Rün seti

*(boş)*

### 4.2 Mekanikler

   180|*(boş — girdi düzeni, gramer, cümle ekonomisi, kapanış, dodge/yavaş çekim ilişkisi)*

### 4.3 Boss repertuarı

*(boş)*

### 4.4 Dövüş dışı yapı: run / oturum / ilerleme

*(boş — prototipte hiç yok; boss ölümü şu an noktalama işareti, bitiş değil)*

   190|### 4.5 Turdan gelen, tasarımla kapanacak açıklar

Prototip turunun cevaplanmamış soruları. Kod açıkları `docs/durum.md` §4'te.

- **Çeşitlilik sayıda değil, ayırt edilebilirlikte tıkandı.** Üç rün bile 120, beş rün 780
  cümle üretiyor; sahibi buna rağmen "çeşitlilik yok" dedi. İki sebep bulundu: silüetler tür
  olarak yeterince ayrışmıyor (bir tur keskinleştirildi, yetmedi), ve **durum/etkileşim
  tablosu hiç yok** —
  bossun ıslak, zırhı kırık ya da havada olması hiçbir cümlenin anlamını değiştirmiyor.
  Rün havuzunu büyütmek bunu **çözmez**: boş bir bossa beş rün de üç rün kadar tekdüze hisseder.
- **Dördüncü sıfat için uzatma penceresi yok.** Dördüncü noktada cümle hemen kapanış üretiyor.
   200|- **Yavaş çekim penceresi hem ödül hem kaçış olarak kullanılıyor** (sahibi "ikisi de" dedi).
  1555 ms sürüyor ve savunma avantajı da veriyor; bu bilinçli ama ölçülmedi.
- **Boss ölümünün ne olduğu tanımsız.** Zafer ekranı, ilerleme, ödül kurgusu hiç düşünülmedi.
- **İki oyuncunun aynı bossu farklı cümlelerle geçmesi** hiç denenmedi. Herkes aynı cümleyi
  çiziyorsa bir eksen çökmüş demektir; düzeltmesi rün eklemek değil, türleri keskinleştirmek.
