# 12 — Sanat incili

Sürüm 1.0 · 12 Ekim 2026 · Kullanıcı: Mustafa

Bu belge oyunun görsel dilini tutar: palet, ışık, sis, malzeme, biçim ve silüet, ölçek, boss'un hareket
görünümü, boss ve ortam efektleri, kompozisyon, arayüz renkleri ve "yapılmaz" listesi. `10-olculer.md` §8'i
(O-8.1–O-8.10) genişletir; oradaki değerler burada aynen geçer, değişmez. Oyuncu skill efektlerinin dili
`07-efekt-motoru.md`'dedir; bu belge onu tekrar etmez, yalnız ortam ve boss tarafıyla nasıl yan yana duracağını
söyler.

Görünüm kuralları oyun sonucunu değiştirmez. Tek istisna okunurluk: bir görünüm kuralı oyuncunun tehlikeyi
görmesini engelliyorsa (ön işaret, dost, kendi karakteri) okunurluk kazanır (SI-1.5).

---

## 0. Nasıl okunur

**Kimlik.** Her madde `SI-x.y` kimliği taşır (Sanat İncili). PR açıklamasına ve görsel kontrol sayfasının
adına yazılır (ör. `SI_2_6_KorButcesi`).

**Kaynak ve doğrulama sütunları** `10-olculer.md` §0'daki anlamdadır (`KOD`, `BELGE`, `ÖNERİ`; `T`, `G`, `K`,
`E`). Ek kaynak: `YÖN` = kullanıcının 12 Ekim'de onayladığı görsel yön (sisli kayada dev taş ejderha başı,
önünde küçük kılıçlı figür; devir notu "Görsel yön").

**Renkler.** Hex değerleri sRGB'dir. Renk uzayı Linear'a geçince (O-8.9) Unity renk alanına aynı hex girilir.
Parlaklık `L*` ile ölçülür (CIE L\*, 0 = siyah, 100 = beyaz); bir pikselin ya da dokunun ortalama L\*'ı
ekran görüntüsünden hesaplanır. "Doygunluk" HSV S'dir (0–1).

**Bu belgede olmayan görünüm uydurulmaz.** Ajan ya da sanatçı burada olmayan bir renk, ışık ya da biçim
gerektiğinde kullanıcıya sorar.

---

## 1. Tek cümle ve beş ilke

> Sisin içinde, dağ kadar eski bir ejderhanın önünde duran küçük bir savaşçı. Dünya soğuk ve gri; yalnız
> ejderhanın canı kor gibi yanıyor.

| Kimlik | İlke | Ne demek (ölçülebilir) | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-1.1 | **Ölçek** | Boss oyuncunun en az 10 katı; ekranda hep ondan büyük bir şey var (§7) | YÖN | K |
| SI-1.2 | **Yaş ve asalet** | Boss canavar değil, eski ve ağırbaşlı: yavaş, az ama büyük hareket; korkunun kaynağı iğrenme değil heybet (§8) | YÖN | E |
| SI-1.3 | **Tek sıcak renk** | Ortam soğuk ve desatüre; tek sıcak renk kor turuncusu ve yalnız vurguda (§2.5) | YÖN, O-8.2, O-8.3 | K |
| SI-1.4 | **Sis derinliği** | Uzaklık sisle okunur: her derinlik katmanı bir öncekinden açık ve soluk (§4, §10) | YÖN, O-8.7 | K |
| SI-1.5 | **Önce okunurluk** | O-8.10 sırası her kuralın üstündedir: 1) boss ön işareti, 2) dostlar, 3) kendi skillin, 4) boss gövdesi, 5) ortam | O-8.10 | K |

---

## 2. Palet

### 2.1 Ortam renkleri

| Kimlik | Ad | Hex | L\* | Nerede |
|---|---|---|---|---|
| SI-2.1 | Gölge | #1C1F24 | 12 | Derin gölge, çatlak içi, en koyu nokta. Saf siyah (#000000) hiçbir yüzeyde yok (O-8.6) |
| SI-2.2 | Lav kayası (zemin, koyu) | #26282C | 16 | Arena zemininin ana tonu |
| SI-2.3 | Lav kayası (zemin, aşınmış) | #3A3D42 | 26 | Zeminde yürünmüş/aşınmış lekeler, zeminin %30–40'ı |
| SI-2.4 | Taş gri-mavi | #3A4048 | 27 | Krater duvarı, ejderha pulları, kayalar (O-8.2) |
| SI-2.5 | Kül | #5B5A57 | 38 | Yatay yüzeylerde birikmiş kül, toz, darbe bulutu (O-8.2) |
| SI-2.6 | İşlenmiş eski taş | #5E6672 | 43 | Dikili taşlar, basamaklar, oyma yüzeyler |
| SI-2.7 | Uzak sis | #4A5260 | 35 | Mesafe sisi (O-8.7) |
| SI-2.8 | Gökyüzü (tepe) | #2E3846 | 23 | Gökyüzü kubbesinin üstü, ortam ışığı gök rengi (O-8.6) |
| SI-2.9 | Gökyüzü (ufuk) | #5E6672 | 43 | Ufukta açılan bant; ejderha silüeti buna karşı okunur (SI-6.3) |
| SI-2.10 | Duman | #2A2A2C | 17 | Boss ateşinin dumanı, çatlak dumanı |
| SI-2.11 | Eski metal | #4A4642 | 30 | Yere saplanmış eski silahlar (ölçek referansı, SI-7.3) |

### 2.2 Kor ailesi (tek sıcak renk)

| Kimlik | Ad | Hex | L\* | Nerede |
|---|---|---|---|---|
| SI-2.12 | Kor turuncu | #FF7A1A | 66 | Vurgu rengi (O-8.3): gözler, çatlaklar, kalp, rünler, ejderha motifleri |
| SI-2.13 | Kor çekirdek | #FFD9A0 | 89 | Korun en sıcak merkezi: kalp çekirdeği, ateşin içi; yalnız emission |
| SI-2.14 | Sönen kor | #8A2E0C | 33 | Sönmekte olan çatlak, ateşin kenarı, soğuyan pul yarası |

Kor ailesi albedo (yüzey rengi) olarak kullanılmaz; yalnız emission ve efektte bulunur. Bir yüzey turuncuysa
yanıyor demektir.

### 2.3 Ayrılmış renkler (her birinin tek anlamı var)

| Kimlik | Renk | Hex | Tek anlamı | Kaynak |
|---|---|---|---|---|
| SI-2.15 | Ön işaret kırmızısı | #C21E2B | Boss ya da ortamdan gelen hasar alanı. Hiçbir oyuncu efekti, dekor ya da arayüz bu rengi kullanamaz (tek istisna: büyük saldırı duyurusunun geri sayım çubuğu, SI-11.3) | O-6.2 |
| SI-2.16 | Kesilemez altın | #E8B04A | Ön işaret kenarında: bu saldırı kesilemez | O-6.6 + ÖNERİ (hex) |
| SI-2.17 | Takım renkleri | O-1.26 | Oyuncu halkası ve sahiplik kanalı (07 kanal 4): 1 kor turuncu, 2 buz mavisi, 3 yeşim yeşili, 4 altın, 5 mor | O-1.26 |
| SI-2.18 | Fiil aileleri | 07 §1 | Oyuncu skilleri: saldırı kor kırmızı-turuncu, tank kemik beyazı-altın, destek sıcak yeşil-altın | BELGE |
| SI-2.19 | Su elementi | #47B8FF | Yalnız element kıvılcımı ve mana çubuğu (07 S-E7) | KOD |
| SI-2.20 | Arayüz metni | #E6E1D8 | Bütün arayüz yazıları | ÖNERİ |

Ortamda (dekor, ışık, gökyüzü) bu tablodaki renklerden yalnız kor ailesi bulunabilir; o da SI-2.24'teki
bütçeyle.

### 2.4 Parlaklık bantları ve sınırlar

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-2.21 | Ortam bandı | Ortam yüzeyleri (zemin, duvar, boss pulları, dikili taşlar) ışık altında ekranda L\* 10–45. Uzak katmanlar sisle açılır (§4), yakın katmanlar koyudur | ÖNERİ | K |
| SI-2.22 | Karakter bandı | Oyuncu karakterinin ana kütlesi (gövde ve giysi, silah hariç) ışık altında ortalama L\* 50–70; çevresindeki zeminden en az 30 L\* açık. Bu O-1.25'teki "%30 parlaklık farkı"nın ölçüsüdür | O-1.25 + ÖNERİ (ölçü) | K |
| SI-2.23 | Ortam doygunluğu | Ortam albedosu doygunluk ≤ 0,20; karakter albedosu ≤ 0,35 (takım rengi halkada ve sahiplik kanalında, giysi boyamaz). Kor ailesi, ayrılmış renkler ve efektler sınır dışıdır | O-8.2 + ÖNERİ | T (doku içe aktarılınca ortalama doygunluk ölçülür) |
| SI-2.24 | Kor bütçesi | Efekt yokken (bekleme anı, varsayılan kamera) kor pikselleri ekranın en çok %4'ü; faz 3'te en çok %8. Kor pikseli: ton 15–35°, doygunluk ≥ 0,6, parlaklık (V) ≥ 0,6 | ÖNERİ | K (otomatik sayım, §13) |
| SI-2.25 | En parlak nokta | Efekt yokken ekrandaki en parlak sıcak nokta boss'un gözleridir (faz 3'te kalp). Hiçbir dekor onlardan parlak olamaz | YÖN + ÖNERİ | K |
| SI-2.26 | Beyaz | L\* > 85 olan pikseller yalnız emission, efekt ve arayüzde. Hiçbir albedo #D0D0D0'dan açık değil | ÖNERİ | T |
| SI-2.27 | Gri tonlama testi | Ekran görüntüsü griye çevrilince boss ön işareti, kendi karakterin ve boss'un baş çizgisi ayrı ayrı seçilebilir. Renk olmadan okunmayan bir sahne geçmez (O-12.14 ile aynı mantık) | ÖNERİ | K |

---

## 3. Işık

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-3.1 | Güneş | Alacakaranlık; yükseklik 20°, yön kuzeybatı (azimut 315°), renk #F2C79A, şiddet 0,85, gölge şiddeti 0,4 | O-8.5 | K |
| SI-3.2 | Karşı ışık (bilerek) | Boss kuzeyde, oyuncu güneyden bakar; güneş önden-soldan gelir. Sonuç: ejderhanın oyuncuya dönük yüzü gölgede, baş ve sırt çizgisi ışıkla kenarlanır, gözlerin koru gölgede parlar. Güneş yönü değişmez, çünkü bu kompozisyonun kendisidir | YÖN + ÖNERİ | K |
| SI-3.3 | Ortam ışığı | Gök #2E3846, zemin #1A1C20, düşük; karanlık bölgeler tamamen siyah olmaz | O-8.6 | K |
| SI-3.4 | Kor dolgu (aşağıdan sıcak ışık) | Çatlakların aydınlattığı his gerçek ışıkla değil şöyle verilir: (a) ışık sondalarına (light probe) zeminden kor yansıması pişirilir; (b) boss ve dikili taş shader'ında alt yüze dikey gradyan: zemine değen yerde #FF7A1A %15 katkı, 2 m'de %8, 6 m'de 0 | ÖNERİ | K |
| SI-3.5 | Gerçek zamanlı ışık bütçesi | Yalnız güneş gölge düşürür. Ek nokta ışığı aynı anda en çok 2, ikisi de gölgesiz: (1) faz 3 kalp koru: menzil 14 m, şiddet 2,0, renk #FF7A1A, nabız SI-9.6; (2) boss ateşi sürerken ağız ucunda: menzil 10 m, şiddet 1,5, ateşle birlikte söner. Çatlaklar, gözler, rünler ışık değil emission'dır | ÖNERİ | G (aynı anda 2'den fazla ek ışık sarı log) |
| SI-3.6 | Gölge | Gölge mesafesi 40 m, 2 kaskad (yüksek kalite); düşük kalitede 1 kaskad, 25 m. Boss gölgesi oyuncuların üstüne düşer; karakteri okunur tutan SI-3.7'dir | ÖNERİ | E |
| SI-3.7 | Karakter kenar ışığı | Oyuncu karakteri ve silahı shader'da Fresnel kenar ışığı taşır: renk #CFD8E0, üs 3, şiddet 0,35. Boss gölgesinde ve kül fırtınasında silüeti korur. Boss ve ortamda kenar ışığı yok | ÖNERİ | K |
| SI-3.8 | Renk işleme | Doygunluk −35, kontrast −14, pozlama +0,15, vinyet 0,12 | O-8.8 | K |
| SI-3.9 | Ton eşleme (tonemapping) | ACES. Kontrol: #FF7A1A emission ekranda turuncu kalmalı (ton 20–32°). ACES sarıya kaydırırsa Neutral'a geçilir; ikisinden biri seçilir, ikisi birden kalmaz | ÖNERİ | K |
| SI-3.10 | Bloom | Eşik 0,95, şiddet 0,65 (O-8.8). Bloom yalnız emission'dan doğar: hiçbir albedo ışık altında eşiği geçmez | O-8.8 + ÖNERİ | K |

---

## 4. Sis ve atmosfer

### 4.1 Mesafe sisi

Üstel sis, yoğunluk 0,012, renk #4A5260 (O-8.7). Bu yoğunlukta sis payı:

| Uzaklık (kameradan) | 6 m | 18 m | 30 m | 60 m | 150 m | 250 m |
|---|---|---|---|---|---|---|
| Sis payı | %7 | %19 | %30 | %51 | %83 | %95 |

Anlamı: oynanış alanı (≤ 18 m) neredeyse net; ejderhanın gövdesi (30–60 m) yarı sisli, bu yüzden dev görünür;
dağ silüetleri (60–250 m) sisin içinde kaybolur ve arenanın sonu görünmez (O-7.7).

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-4.1 | Mesafe sisi | Yukarıdaki tablo; dövüşte değer yalnız SI-4.6 ile değişir | O-8.7 | K |
| SI-4.2 | Sise bağışık öğeler | Sis payı en çok %20 (shader'da sis katkısı kısılır): boss ön işareti, boss gözleri, kalp koru, oyuncu efektleri, takım halkası, karakter kenar ışığı (SI-3.7) | ÖNERİ | K |
| SI-4.3 | Yer sisi | Zeminde 0–1,2 m yüksekliğinde sürüklenen yumuşak katman: en çok 6 büyük kart, derinlik solması (depth fade) açık, 0,3 m/s sürüklenir. Arena merkezinde (r < 12 m) opaklık ≤ %15, kenarda (12–18 m) %25, duvar dışında (18–30 m) %40. Saydam katman bütçesinden 1 katman yer (O-9.6) | YÖN + ÖNERİ | K, E (GPU profil) |
| SI-4.4 | Havada kül | Kameranın çevresinde 20 m'lik kutuda yavaş düşen gri pul: en çok 120 parçacık, boy 2–6 cm, düşüş 0,4 m/s, renk #8C8A84 %60, hafif dönüş | YÖN + ÖNERİ | G (parçacık sayısı) |
| SI-4.5 | Kor kıvılcımı | Çatlaklardan yükselen küçük kıvılcımlar: ekranda en çok 60, ömür 2–4 s, yükselme 0,6 m/s, renk kor ailesi | YÖN + ÖNERİ | G |
| SI-4.6 | Kül fırtınası (faz 2) | Faz 2 başında 3 s içinde sis yoğunluğu 0,012 → 0,028 ve sis rengi #4A5260 → #5B5A57. Havada kül 120 → 300, yatay sürüklenme doğudan batıya 3 m/s. Bu yoğunlukta 18 m'de sis payı %40, 30 m'de %57. Faz 3 başında 3 s'de 0,018'e iner (kalp görünsün) | YÖN + ÖNERİ | K, E |
| SI-4.7 | Ortam parçacık payı | Atmosfer parçacıkları (kül + kıvılcım) toplamı faz 1 ve 3'te ≤ 180, faz 2'de ≤ 360. Kalan bütçe (O-9.3: ekranda 1500) skill ve boss efektlerinindir | ÖNERİ | G |
| SI-4.8 | Gökyüzü | Kubbe tepede #2E3846, ufukta #5E6672'ye açılan gradyan; güneş diski yok (güneş sis arkasında), bulut kartları yavaş (0,5 °/s'den az döner). Yıldız, ay, renkli gün batımı yok | YÖN + ÖNERİ | K |

---

## 5. Malzeme

Kural: az ayrıntılı doku, net renk blokları (O-8.1). Malzemeler mat; parlaklık yalnız metal kenarlarda.

| Kimlik | Malzeme | Albedo | Pürüzsüzlük (smoothness) | Metalik | Emission | Doku kuralı |
|---|---|---|---|---|---|---|
| SI-5.1 | Lav kayası zemin | #26282C, %30–40'ı #3A3D42 lekesi | 0,15 | 0 | Çatlak maskesinde kor #FF7A1A, şiddet 1,5; yavaş nabız 0,2 Hz, ±%20. Çatlakların toplam alanı zeminin %3–6'sı | Tekrar eden doku 4 m'den kısa aralıkla tekrarlanmaz; üstüne büyük ölçekli (16 m) leke maskesi |
| SI-5.2 | Krater duvarı ve kaya | #3A4048 | 0,10 | 0 | Yok (yalnız faz 2'de duvar dibindeki çatlaklar, şiddet 1,0) | Yukarı bakan yüzeylerde (yüzey normalinin Y'si > 0,7) #5B5A57 kül birikimi |
| SI-5.3 | Ejderha pulu | #3A4048 (duvarla aynı aile, ±5 L\*) | 0,12 | 0 | Pul aralarında ince kor çizgisi; şiddet faz çarpanıyla (SI-9.6). Faz 1'de yalnız boyun ve göğüs | Pul kenarlarında kül; pullar yukarıdan kül tutar. Boss'u duvardan ayıran renk değil biçim, kor ve hareket tozudur (SI-6.3) |
| SI-5.4 | Ejderha gözü | — | — | — | #FF7A1A çekirdek #FFD9A0, şiddet 3,0 × faz çarpanı; dikey göz bebeği #1C1F24 | Göz akı yok; göz kor bir çatlak gibidir |
| SI-5.5 | Ejderha boynuz ve pençesi | #5B5A57 (kaya pullarından açık, eski kemik-taş) | 0,20 | 0 | Yok | Uçlarda aşınma, boyuna çatlaklar |
| SI-5.6 | Dikili taş | #5E6672 | 0,10 | 0 | Yüzüne oyulmuş Ejderha Dili harfleri (07 §1.1 harf atlası) sönük kor, şiddet 0,3. Takım görevinde bu taş hedefse 3,0'a çıkar ve nabız 1 Hz | Kırık üst uç, kenarlarda yontu izi; kül yalnız üstte |
| SI-5.7 | Eski silah (saplanmış) | #4A4642 | 0,25 | 0,6 | Yok | Paslı, kırık; kabzalarda bez kalıntısı #5B5A57 |
| SI-5.8 | Oyuncu karakteri | Ana kütle L\* 50–70: keten, açık deri, kemik tonları (#8C8A84–#A9A49A çevresi); koyu ayrıntılar en çok %30 | Kumaş 0,2; deri 0,3; metal 0,5 | Metal parçalarda 0,8 | 07 §4 kor shader'ı (karakter `_Intensity`) | Doygunluk ≤ 0,35; takım rengi giysiye boyanmaz (halkada, SI-2.17) |
| SI-5.9 | Silah | 04/07'deki silah görünümü | Metal 0,5; ahşap 0,2 | 0,8 / 0 | 07 §4 `Silah_Kor` | — |

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-5.10 | Doku yoğunluğu (texel) | Karakter ≈ 1150 px/m (2048 doku, 1,78 m boy); ortam yakın plan 256 px/m; boss yüzeyi tekrarlayan kaya detay dokusu + kendine özgü maske dokusuyla oyuncuya yakın yerlerde (kafa, pençe) 256 px/m. Doku boyları O-15.7 | O-15.7 + ÖNERİ | T |
| SI-5.11 | Normal haritası | Yalnız büyük form (pul, çatlak, kaya kırığı). Kumaş dokusu, deri gözeneği gibi küçük ayrıntı normal haritada yok | ÖNERİ | K |
| SI-5.12 | Islaklık, ayna parlaklığı | Yok. Ortamda pürüzsüzlük > 0,3 yüzey yok | ÖNERİ | T |

---

## 6. Biçim ve silüet

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-6.1 | Biçim dili | **Boss:** ağır yatay kütleler, köşeli kaya blokları, kama ve yarık; yuvarlak, yumuşak, şişkin biçim yok. **Oyuncu:** dikey, ince, silahı silüetin dışına taşar. **Ortam:** kırık dikeyler (dikili taşlar) ve yatay krater çizgisi. Üç biçim dili birbirine karışmaz | YÖN + ÖNERİ | K |
| SI-6.2 | Silüet testi | Model düz siyaha boyanıp 64 px yükseklikte gösterilince tanınır: boss için baş, boynuz ve pençe; oyuncu için silah türü (10 silah birbirinden ayrılır) | ÖNERİ | K |
| SI-6.3 | Baş çizgisi | Varsayılan kamerada ejderhanın baş ve boyun çizgisi arkasındaki sis/gökyüzünden en az 15 L\* koyu okunur (koyu silüet, açık sis). Kraterin kuzey sırtı ejderhanın sırtıdır; sırt çizgisi ufkun üstünde kalır | YÖN + ÖNERİ | K |
| SI-6.4 | Ayrıntı dağılımı | Büyük form %70, orta form (pullar, kaburga hissi veren kaya katmanları) %20, küçük ayrıntı %10. Küçük ayrıntı yalnız oyuncunun 6 m'den yakın gördüğü yerde: ejderha başı, pençeler, dikili taş dipleri | ÖNERİ | K |
| SI-6.5 | Oyuncu silüeti | Karakter + silahın silüeti, çevresindeki efektler açıkken de en az %60'ı görünür (O-9.2 ile birlikte) | ÖNERİ | K |

---

## 7. Ölçek

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-7.1 | Boss oranı | Boss'un toplam uzunluğu ya da boyu oyuncu boyunun (1,78 m, O-1.1) 10–30 katı (18–53 m). Arenaya giren parçası (baş) en az 4 katı (≥ 7 m). Kül Ejderi: 45 m (25 kat), baş 7 m yüksek, 9 m uzun | YÖN + ÖNERİ | T |
| SI-7.2 | Ekranda boss | Hedef kilitliyken boss'un görünen parçası ekran yüksekliğinin %25–60'ını kaplar; oyuncu %18–25 (O-1.24). Ejderhanın tamamı ekrana hiç sığmaz, bu bilinçlidir; gözleri kilitliyken her zaman kadrajdadır | YÖN + ÖNERİ | K |
| SI-7.3 | İnsan ölçeği referansı | Her kamera konumundan 10 m içinde en az bir insan boyu nesne görünür: yere saplanmış eski savaşçı silahları (1,1–1,3 m; arenada 12–20 adet, çoğu duvar dibinde ve dikili taş çevresinde; çarpışmasız), basamak 0,18 m (O-7.8). Bunlar "önceki savaşçılar burada düştü" hikâyesini de anlatır | YÖN + ÖNERİ | K |
| SI-7.4 | Ara ölçek | Dikili taşlar 6–9 m (oyuncunun 3,4–5 katı): oyuncu ile ejderha arasındaki basamak. Göz, oyuncu → silah → taş → ejderha başı → ejderha gövdesi → dağ sırasıyla büyüklüğü okur | YÖN + ÖNERİ | K |
| SI-7.5 | Büyüklüğün ipuçları | Büyük şey yavaş hareket eder (§8), uzak parçası sisle solar (§4), değdiği yerde toz kalkar (SI-9.4), sesi alçaktır (O-11). Bunların biri eksikse boss küçük görünür | YÖN | E |

---

## 8. Boss'un hareket görünümü (asalet)

Saldırı süreleri ve alanları boss tasarım belgesindedir. Burada yalnız hareketin nasıl görüneceği var.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-8.1 | Nefes | Beklemede 6 s'lik nefes döngüsü: göğüs ve boyun %2 genişler; her soluk verişte burun deliklerinden kül-duman (#2A2A2C, 1,5 s, en çok 20 parçacık) | YÖN + ÖNERİ | K, E |
| SI-8.2 | Göz kırpma | 8–12 s arası rastgele, 0,4 s (yavaş). Kırparken kor söner, açılınca 0,2 s'de geri gelir | ÖNERİ | E |
| SI-8.3 | Baş takibi | Baş, hedef oyuncuya en çok 30 °/s döner (gövdenin 90 °/s sınırı O-6.5 ayrı). Hedef değişince baş önce durur, 0,3 s bekler, sonra döner: göz, dönmeden önce bakar | ÖNERİ | E |
| SI-8.4 | Hazırlık ve vuruş | Her saldırıda hazırlık görünümü ön işaretin bütün süresini kaplar (yavaş kalkış, nefes alma, pençe kaldırma); vuruş hızlıdır, 0,15–0,30 s. Vuruştan sonra açık kalma süresinde (O-6.4) boss ağırca toparlanır: toz çöker, baş yavaş kalkar. Geri sıçrama, titrek geri çekilme yok | YÖN + ÖNERİ | E |
| SI-8.5 | Durgunluk | Saldırılar arasında boss kıpır kıpır etmez: aynı anda en çok iki parça hareket eder (ör. baş + bir pençe) | YÖN + ÖNERİ | E |
| SI-8.6 | Konuşma anı | Replik sırasında baş durur, oyunculara bakar, göz koru ×1,5 olur; ağız az açılır (çene en çok 10°). Replik bitince 0,5 s sessizlik | YÖN + ÖNERİ | E |
| SI-8.7 | Yasak hareketler | Tiz çığlık, baş sallama ve titreme, salya ve dil, böcek gibi hızlı uzuv hareketi, saldırı arasında sürekli kükreme. Kükreme dövüşte en çok iki kez: uyanışta ve faz 2 başında | YÖN | E |

---

## 9. Boss ve ortam efektleri

Oyuncu efektleri `07`'dedir; sınırlar O-9.1–O-9.6.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-9.1 | Boss efekt malzemesi | Kor, kül, duman, taş kırıntısı. Boss ateşi: çekirdek #FFD9A0, gövde #FF7A1A, kenar #8A2E0C, arkasından duman #2A2A2C %70. Fiil aileleri (07) boss'ta kullanılmaz | YÖN + ÖNERİ | K |
| SI-9.2 | Hasar = ön işaret | Hasar veren her şey (boss saldırısı ve ortam tehdidi) O-6.2 ön işaretini taşır. Dekoratif kor ve kıvılcım hiçbir zaman kırmızı kenar ya da dolan alan biçimi taşımaz. Oyuncu, kırmızıyı görünce kaçar; kırmızı görmezse güvendedir | O-6.2 + ÖNERİ | K |
| SI-9.3 | Ortam tehditlerinin görünümü | **Kor çatlağı alevi:** çatlak emission 1,5 → 4,0 ön işaret süresince yükselir, kırmızı ön işaret çatlağı çevreler, sonra dikey alev (yükseklik 3 m, 0,4 s). **Düşen kül parçası:** yerde kırmızı kenarlı büyüyen gölge, parça vuruştan 0,3 s önce görünür, çarpınca SI-9.4 tozu. **Titreyen zemin:** kırmızı halka içinde zemin dokusunda titreşim (vertex, genlik 2 cm) ve küçük taş sıçramaları | ÖNERİ | K |
| SI-9.4 | Darbe tozu | Boss pençesi, başı ya da düşen parça yere değince toz halkası: yarıçap = darbe alanı + 1 m, renk #5B5A57, ömür 1,2 s, en çok 80 parçacık. Kamera sarsıntısı O-5.13 | ÖNERİ | K |
| SI-9.5 | Boss ve oyuncu koru karışmasın | Boss koru yavaştır (nabız ≤ 0,8 Hz) ve dumanla gelir; oyuncunun kor efektleri keskin ve kısadır (0,2–0,6 s, 07). Aynı turuncu iki farklı ritimle okunur | ÖNERİ | E |
| SI-9.6 | Faz koru | Çatlak, göz ve pul arası emission çarpanı: faz 1 ×1,0 · faz 2 ×1,6 · faz 3 ×2,4. Faz geçişinde 2 s'de yükselir. Faz 3'te göğüsteki kalp açılır: çekirdek #FFD9A0, kalp atışı nabzı 0,8 Hz (±%30), ekrandaki en parlak sıcak nokta | YÖN + ÖNERİ | K |
| SI-9.7 | Pul kırılması (can katmanı) | Her can katmanı bitince bir pul kopar: 0,6 s kor patlaması (en çok 60 kıvılcım), pul düşer ve zeminde kalıcı dekor olarak kalır (çarpışmasız). Yerinde kor yarası kalır (emission 2,0 × faz çarpanı); beş katman = beş görünür yara. Hangi pulun kırılacağı boss belgesinde | YÖN + ÖNERİ | K |
| SI-9.8 | Ekran kaplama | Boss ateşi dahil hiçbir boss efekti ekranın %25'inden fazlasını kaplamaz (O-9.1). İstisna yalnız sinematik anlar (SI-10.4), oyuncu kontrolü yokken | O-9.1 | K, G |
| SI-9.9 | Beyaz flaş | Tam ekran beyaz flaş yok. En çok %30 opak, 0,1 s, yalnız faz geçişinde | ÖNERİ | K |
| SI-9.10 | Ölüm | Ejderha yığılır, kor 3 s'de söner (gözler en son), kül pul pul kalkar ve sis 5 s'de dövüş öncesi yoğunluğa döner. Ölüm süresi ve sınırları boss belgesinde (O-6.21 yerine geçecek) | YÖN + ÖNERİ | E |

---

## 10. Kamera ve kompozisyon

Kamera ölçüleri `10-olculer.md` §5'tedir. Burada yalnız görüntünün dizilişi var.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-10.1 | Yön | Boss her zaman kuzeyde. Ufuk çizgisi ekranın üst üçte birinde (30° eğimle doğal olarak, O-5.2) | YÖN | K |
| SI-10.2 | Derinlik katmanları | (1) **Ön plan** kameraya 0–3 m: havada kül, ara sıra duvar kenarındaki kaya. (2) **Orta** oyuncular ve dikili taşlar. (3) **Arka** ejderha ve krater duvarı. (4) **Uzak** dağ silüetleri 60–250 m (O-7.7). (5) **Gökyüzü**. Her katman bir öncekinden en az 8 L\* açık ve daha düşük kontrastlı (sisle, §4) | YÖN + ÖNERİ | K |
| SI-10.3 | Alan derinliği (bulanıklık) | Yalnız sinematik anlarda; oyun sırasında yok (mobil maliyet ve okunurluk). "Ön planda flu figür" bu anlarda kurulur: kamera oyuncunun arkasında alçakta (1 m), oyuncu flu, ejderha başı net | YÖN + ÖNERİ | K |
| SI-10.4 | Sinematik anlar | Arena girişi, boss uyanışı (2 s, O-17.1), faz geçişleri, ejderhanın ölümü. Her biri en çok 4 s; ekranın üstünde ve altında %8'lik siyah bant 0,3 s'de girer. Oyuncu bu sürede hasar almaz; süreler boss belgesinde | YÖN + ÖNERİ | E |
| SI-10.5 | Boss içi kadraj | Kamera boss gövdesinin içine girmez (O-5.15); boss kameranın önüne geçerse %35 opaklık (O-5.11). Ejderha başı için bu oran %50: göz koru görünür kalsın | O-5.11 + ÖNERİ | K |

---

## 11. Arayüz renkleri

Arayüz ölçüleri `10-olculer.md` §12'dedir. Burada yalnız renk.

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| SI-11.1 | Panel ve yazı | Panel #1C1F24 %80 opak; yazı #E6E1D8; ikincil yazı #8C8A84. Neon, parlak mavi, mor arayüzde yok (takım renkleri hariç) | ÖNERİ | K |
| SI-11.2 | Boss can çubuğu | Dolgu #FF7A1A → bitişe doğru #8A2E0C gradyan; katmanlı (×5 → ×0, boss belgesinde); arka #1C1F24 | ÖNERİ | K |
| SI-11.3 | Büyük saldırı duyurusu | Ekran üst ortada yazı #E6E1D8 (ör. "3 saniye sonra Kül Nefesi"), altında dolan geri sayım çubuğu #C21E2B. Ön işaret kırmızısının arayüzdeki tek kullanımı budur | ÖNERİ | K |
| SI-11.4 | Takım görevi duyurusu | Yazı #E6E1D8, kor çerçeve #FF7A1A, solda görev simgesi, sağda "0/3" sayacı | ÖNERİ | K |
| SI-11.5 | Dirençli yazısı | Boss üstünde #8C8A84, 0,6 s; hasar sayısı boyutunda (O-10.3) | ÖNERİ | K |

---

## 12. Yapılmaz listesi

Her madde bir örnekle. Mevcut arena ve boss bu listenin çoğunu ihlal ediyor; yeni içerik bunlardan uzak durur.

| Kimlik | Yapılmaz | Neden | Bugünkü örnek |
|---|---|---|---|
| SI-12.1 | Doygun yeşil, çimen, ağaç, yaprak | Palet soğuk ve desatüre; yeşil yalnız destek fiilinin efektinde ve takım renginde okunur | Yeşil arena zemini |
| SI-12.2 | İğrenme üzerine kurulu yaratık: örümcek, böcek, çok bacak, ağ, koza, salya | Korkunun kaynağı heybet (SI-1.2). Çok bacak ve hızlı titrek hareket sinir bozar ve ölçek okumasını bozar | Ağların Kraliçesi (örümcek), ağ dokusu, kozalar |
| SI-12.3 | Arenada başka dev yaratık kalıntısı (iskelet, kaburga, kafatası) | Sahnedeki tek dev, ejderhadır; ikinci dev ölçek basamağını (SI-7.4) bozar. Ölüm hikâyesini eski silahlar anlatır (SI-7.3) | 7 kaburga (O-7.9) |
| SI-12.4 | Köy, ev, ahşap, kumaş tente | Krater yaşanan bir yer değil; insan izi yalnız savaşçı kalıntısı ve oyma taş | Yıkık köy duvarı (O-7.3, O-7.7) |
| SI-12.5 | Mor, pembe, neon, parlak mavi ortam ışığı | Tek sıcak renk ilkesi (SI-1.3); mavi su elementine ayrılmış (SI-2.19) | — |
| SI-12.6 | Güneşli gün, açık gökyüzü, renkli gün batımı, ay ve yıldız | Alacakaranlık ve sis yönün kendisi | Bugünkü zayıf sis (O-8.7 notu) |
| SI-12.7 | Dekor için kor (meşale sırası, sıcak lamba, turuncu bayrak) | Kor yalnız ejderhanın canının ve rünlerin rengi; dekorda harcanırsa vurgu değeri biter | — |
| SI-12.8 | Parlak, plastik, ıslak malzemeler | SI-5.12 | — |
| SI-12.9 | Küçük ayrıntı kalabalığı (her yüzeyde ince desen, yazı, süs) | Telefonda gürültü olur; büyük form okunmaz (SI-6.4) | — |
| SI-12.10 | Sevimli, çocuksu oranlar, iri gözler | O-1.2 (7,4 kafa) ve asalet | — |
| SI-12.11 | Lens parlaması, renk sapması (chromatic aberration), film greni, hareket bulanıklığı | Okunurluğu ve kare hızını düşürür, sahneye bir şey katmaz | — |
| SI-12.12 | Ekranı kaplayan efekt, uzun beyaz flaş | O-9.1, SI-9.9 | Boss saldırısının ekranı kaplaması (12 Ekim videosu) |
| SI-12.13 | Kırmızı (#C21E2B) dekor ya da oyuncu efekti | Kırmızı = kaç (SI-9.2) | — |
| SI-12.14 | Boss'un sinirli, çığlık atan, kıpır kıpır hali | SI-8.7 | Örümceğin hızlı ve sık saldırı ritmi |

---

## 13. Kontrol yöntemi

Görsel kontroller (`K`) şu sayfalarla yapılır. Her sayfa otomatik ekran görüntüsünden üretilir ve tek
sayfada toplanır; kullanıcı onaylamadan içerik oyuna girmez (O-16.8 ile aynı yol).

| Kimlik | Sayfa | İçerik | Geçer |
|---|---|---|---|
| SI-13.1 | Ortam turu | Kamera duvar dibinde 8 yönde + arena merkezinde 4 yönde, varsayılan eğimde; 12 görüntü | Boş gökyüzü ya da zemin kenarı yok (O-7.7); her görüntüde 10 m içinde insan ölçeği nesnesi (SI-7.3); ufuk üst üçte birde (SI-10.1) |
| SI-13.2 | Gri tonlama | Ortam turunun 12 görüntüsü + boss saldırı hazırlığı anı, gri tonlamada | SI-2.27 |
| SI-13.3 | Parlaklık ölçümü | Oyuncu karakteri arena merkezinde, kilit açıkken; karakter ana kütlesinin ve çevresindeki zeminin ortalama L\*'ı otomatik ölçülür | SI-2.21, SI-2.22 (fark ≥ 30) |
| SI-13.4 | Kor sayımı | Efekt yokken faz 1, 2, 3 görüntüleri; kor pikseli oranı otomatik sayılır | SI-2.24 (%4 / %8); en parlak sıcak nokta gözler ya da kalp (SI-2.25) |
| SI-13.5 | Silüet | Boss ve 10 silahlı karakter düz siyah, 64 px | SI-6.2 |
| SI-13.6 | Sis katmanları | Faz 1 ve faz 2 (kül fırtınası), kilitli kamera | Oyuncu, ön işaret ve göz koru okunur (SI-4.2, SI-4.6) |

---

## 14. Referanslar ve neyin alınacağı

| Kaynak | Alınacak | Alınmayacak |
|---|---|---|
| Kullanıcının referans görseli (sisli kayada taş ejderha başı, küçük kılıçlı figür) | Ana kompozisyon: ölçek zıtlığı, sis, taş pul, kor göz | — |
| Shadow of the Colossus | Ölçek, sessizlik, boss'un manzaranın parçası olması, desatüre ışık | Tırmanma mekaniği |
| Elden Ring, Placidusax | Yatan, yaşlı, kırık ejderha; ağırbaşlılık; fırtınalı gökyüzü | Altın ışık yoğunluğu, ayrıntı yoğunluğu |
| Monster Hunter ejderhaları | Ağırlık hissi, darbe tozu, hazırlık okunurluğu | Yeşil ve doygun ortamlar |
| Dragon Nest Mobile, Yeşil Ejderha Yuvası 5 | Kısa ve yarı saydam efektler (0,3–0,6 s), hep okunan silüet, duyuru yazısı + geri sayım | Parlak, doygun renk paleti |

---

## 15. Diğer belgelerle ilişki

- `10-olculer.md` §8 (O-8.1–O-8.10) aynen geçerli; bu belge onun ayrıntısıdır. Sanat konusunda iki belge
  çelişirse kullanıcıya sorulur; bu sürümde çelişki yok.
- `10-olculer.md` §7'deki şu maddeler bu belgenin yapılmaz listesiyle çelişir ve krater arenası belgesiyle
  değişecek: O-7.3 (yıkık köy duvarı ve kaburgalar), O-7.4 (sütun ve kozalar → dikili taşlar), O-7.6 (taş-kül
  zemin ve ağ dokusu → lav kayası), O-7.7 (yıkık köy halkası → krater yamacı), O-7.8 (köy kapısı → basamak ve
  saplanmış silahlar), O-7.9 (kaburgalar kalkar). O zamana kadar bugünkü arena olduğu gibi kalır.
- `10-olculer.md` §6.3 (Ağların Kraliçesi) bu belgenin SI-12.2 maddesiyle çelişir; boss tasarım belgesiyle
  değişecek. Ağların Kraliçesi'nin ileride ikinci boss olarak kalıp kalmayacağı açık sorudur.
- Boss tasarım belgesi (Kül Ejderi) ve krater arenası belgesi bu belgenin kurallarıyla yazılır.
