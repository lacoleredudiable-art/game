# 18 — Kül Ejderi (boss tasarım belgesi)

Sürüm 1.1 · 17 Ekim 2026 · Kullanıcı: Mustafa

Bu belge oyunun ilk boss'unu `10-olculer.md` §6.1 şablonunun bütün alanlarıyla tanımlar. Ağların Kraliçesi'nin
yerine geçer (karar A3: örümcek tamamen kalkar). Görünüm kuralları `16-sanat-incili.md`'dedir; burada yalnız
boss'a özgü olanlar var. Arena yerleşimi (çatlakların, taşların, eski silahların tam yeri) `19-krater-arenasi`
belgesinde yazılacak; bu belge arenadan yalnız boss'un kullandığı noktaları sabitler.

**Verilen kararlar (12 Ekim):** yön olduğu gibi onaylı (A1); ceza zorluğa göre (A2); ilk hedef tek oyuncu +
2 yapay zekâ dostu, yalnız faz 1 (A4).

**Verilen kararlar (17 Ekim, [K]):** v2 model esas (K1, gövde ve kanat ölçüleri `art-kaynak/kul-ejderi/v2/ejder.blend`'den
ölçüldü); menzil parçanın en yakın yüzeyine ölçülür (K2, KE-2.6); faz 2'de pençeler yerinde kalır (K3); Kor Mührü
ödülü çökme, kalbe vurulmaz (K4, KE-7.8); Kanat Savuruşu'nda kanat güney kenara kadar süpürür (K5, KE-6.6); kanat
koru faz 2'de yanar (K6, SI-5.3); uyanış ilk girişte 4 s, "Tekrar"da 2 s (K7, KE-5.3); faz 3 sabit döngü (K8,
KE-7.6); Kül Yağmuru'nda takım cezası yok (K9, KE-6.13).

---

## 0. Nasıl okunur

- Kimlik `KE-x.y`. PR'da ve test adında yazılır (ör. `KE_4_2_PenceCakmasi_OnIsaret`).
- Kaynak ve doğrulama sütunları `10-olculer.md` §0'daki anlamdadır. Bu belgedeki sayıların çoğu `ÖNERİ`dir:
  gri kutu denemesinde (`17` C2) telefonda onaylanır ya da değişir.
- **Koordinatlar:** arena merkezi (0, 0). X doğu, Z kuzey, Y yukarı; metre. Arena sınırı 18 m daire (O-7.2).
- **Hasar** oyuncu azami canının yüzdesi (oyuncu canı 1000, O-13.1); Normal zorlukta. Zorluk çarpanı §9.
- **Ön işaret** O-6.2 görünümündedir (kırmızı kenar, dolan iç); kesilemez saldırıda kenar altındır (O-6.6).
- **Kesilebilir:** ön işaret süresince Kontrol fiiliyle kesilir; boss'un iradesi Açık olmalıdır (02 Bo-2, Bo-3).
  Kesilen saldırı iptal olur, boss 1,5 s sendeler ve bu süre açık kalma sayılır. Kesmeden sonra boss 45 s
  Dirençli olur (üstünde "Dirençli" yazısı, SI-11.5); Dirençli iken kesme girişimi denge hasarına döner.

---

## 1. Kimlik ve hikâye

| Kimlik | Madde | Değer |
|---|---|---|
| KE-1.1 | Ad | Kül Ejderi |
| KE-1.2 | Hikâye (tek paragraf, oyunda yazı olarak yok, sanata ve repliklere yön verir) | Dünyadan eski bir ejderha, sönmüş bir yanardağın kraterinde uyur. Gövdesi kraterin kuzey yamacına yaslanıp yüzyıllar içinde kayayla kaynaşmış; pulları kaya, kanı kor. Eski çağın savaşçıları onu üç dikili taşla mühürlemiş; taşların üstündeki Ejderha Dili harfleri mühürdür. Kraterde yere saplanmış kılıçlar, onu uyandırmaya gelip düşenlerden kalmıştır. Ejderha kötü değildir; uyandırılmaktan hoşlanmaz ve kendine yakışanı yapar: dövüşür |
| KE-1.3 | Ton | Asil, yaşlı, ağırbaşlı (SI-1.2). Öfke yok; sabır ve yük var |

---

## 2. Gövde, ölçü ve çarpışma

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| KE-2.1 | Toplam uzunluk | 45 m (oyuncunun 25 katı, SI-7.1) | YÖN | T |
| KE-2.2 | Gövde | Kuzeyde sfenks gibi yatar: göğsün ön yüzü duvar çizgisinde (Z ≈ 18), gövde Z 18–58 arasında, sırtın en yüksek yeri 15 m (doğrulunca 18 m). Arka bacaklar gövdenin iki yanında, Z 34–39'da görünür. Kuyruk batı yamacında kıvrılır (Z 40–56, X −18'e kadar); görünür ama saldırmaz. İki kanat dinlenmede sırtın üstünde yarı katlı durur: uçları X ±31, Z 26–59, en yüksek yeri 20 m (17 Ekim'de 30° indirildi; KE-6.6). Boyun göğsün ortasından çıkıp başa iner. Gövde çarpışması duvarın çarpışmasıdır; baş, boyun, pençeler, kanatlar ve kuyruk canlıdır. Yerleşim `19-krater-arenasi.md` §3 | v2 model (`art-kaynak/kul-ejderi/v2/ejder.blend`, 17 Ekim ölçümü) | K |
| KE-2.3 | Baş (faz 1, yatarken) | 9 m uzun, 7 m yüksek. Burun ucu (0, 7), kafatasının arkası (0, 16,5); çenenin alt yüzü yerden ~2,5 m yukarıda. Boyun Z = 16,5–18 arasında göğse girer | YÖN + v2 model | T |
| KE-2.4 | Ön pençeler | İki pençe, her biri 5,2 m uzun, 4,2 m geniş; tırnak ucu Z 11,3, topuk Z 16,5. Sol (batı) pençe merkezi (−8, 13,7), sağ (doğu) (8, 13,7). Bacak dirsekten geriye bükülür, omuz göğse (Z ≈ 19, 10 m yükseklik) girer | Tripo bacak modeli (12 Ekim, `ejder_on` v0) | T |
| KE-2.5 | Çarpışma (oyuncu boss'un içine giremez) | Baş: kapsül, yarıçap 3,0 m, eksen Z, (0, 3, 9) – (0, 3, 15). Her pençe: kapsül, yarıçap 2,0 m, yükseklik 4 m, merkez (±8, 2, 13,7); bacak: kapsül, yarıçap 1,5 m, (±7,5, 0, 15) – (±7,4, 10, 19). Boyun ve gövde: duvar. Faz 2 ve 3'te konumlar KE-6'daki gibi değişir | ÖNERİ | T, G |
| KE-2.6 | Tek gövde yarıçabı kuralı | `02` D-1'deki "boss 2,5 m" tek kapsülü bu boss'ta geçersiz; boss parçalardan oluşur. Menzil, parçanın oyuncuya en yakın yüzeyine üç boyutlu ölçülür, merkezine değil (K 22:15 kenardan ölçüm kuralı; [K] 17 Ekim). Örnek: faz 2'de oyuncu Z 2'de, başın alt yüzüne 11,7 m (Yay 14,1 m ile vurur), başın merkezine 17,1 m | ÖNERİ + [K] | T |
| KE-2.7 | Hareket | Boss yürümez, yer değiştirmez. Baş, kuzeyden (güneye bakış yönü) sağa ve sola en çok 60° döner, en çok 30 °/s (SI-8.3). O-6.5'teki 90 °/s gövde dönüşü bu boss'ta kullanılmaz | ÖNERİ | T |

### 2.1 Vurulabilir bölgeler

| Kimlik | Bölge | Hangi fazda | Hasar çarpanı | Not |
|---|---|---|---|---|
| KE-2.8 | Baş | 1, 2, 3 | ×1,0 | Faz 2'de baş 10–24 m yüksekte (alt yüzü 10 m): yakın silahlar yalnız sendelemede (KE-8.1) ulaşır; menzil ≥ 14 m silahlar (Yay, Asa, Top, Tılsım) her zaman ulaşır (KE-2.6) |
| KE-2.9 | Pençeler | 1, 2, 3 | ×0,6 | Taş zırh. Yakın silahların güvenli hedefi |
| KE-2.10 | Göğüs | 2 | ×1,0 | **[AÇIK] (K):** v2 modelde faz 2'de göğsün ön yüzü Z ≈ 18'de (arena sınırı), alt yüzü 8,6 m yükseklikte; arena içinden en yakın yüzeye ~7,2 m, Kılıç (6 m) ulaşmaz. Eski metin (alt yüz 4 m, Z 12–17) modelle tutmuyor. Göğüs yalnız menzilli bölge mi olsun, yoksa doğrulmuş pozda göğüs öne mi gelsin? |
| KE-2.11 | Kalp | 3, yalnız görünür | — | Vurulmaz, hasar almaz ([K] 17 Ekim). Kor Mührü başarısında plakalar açılır, kalp atar ve ekrandaki en parlak sıcak noktadır (SI-9.6). Ödül KE-7.8'deki çökmedir. Model: kalp (0, 2–9, 21–23), göğüs plakalarının arkasında |

---

## 3. Can, denge, katmanlar

| Kimlik | Madde | Değer | Kaynak | Doğrulama |
|---|---|---|---|---|
| KE-3.1 | Can | `scale.boss_max_hp` (bugün 15000); oyuncu sayısıyla O-6.7'ye göre ölçeklenir. Yapay zekâ dostu oyuncu sayılır | KOD + ÖNERİ | T |
| KE-3.2 | Hedef süre | Tam dövüş (3 faz) ~450 s (7,5 dk); faz 1 ~180 s (ilk hedef, A4). Can, bu süreler tutacak biçimde dengelemede ayarlanır | ÖNERİ | E |
| KE-3.3 | Fazlar | Faz 1 Uyanış %100–60 · Faz 2 Doğruluş %60–25 · Faz 3 Kalbin Koru %25–0 | YÖN | T |
| KE-3.4 | Can katmanları | 5 katman, her biri canın %20'si; can çubuğu "×5 → ×0" sayar (SI-11.2). Her katman bitince bir pul kırılır (SI-9.7): %80 sol pençe pulu, %60 alın pulu, %40 sağ pençe pulu, %20 boyun pulu, %0 ölüm. Modelde ayrı parçalar: `ejder_pul_sol_pence` (−8, 13,8, 1 m), `ejder_pul_alin` (0, 11, 6,3 m), `ejder_pul_sag_pence` (8, 13,8, 1 m), `ejder_pul_boyun` (0, 17,5, 11 m) | YÖN + ÖNERİ | T, K |
| KE-3.5 | Faz eşiği koruması | Faz eşiğine gelen hasar fazlası taşmaz: can eşikte durur, geçiş başlar (bir vuruşla iki faz atlanmaz) | ÖNERİ | T |
| KE-3.6 | Denge | `scale.boss_poise_bar` (bugün 300); dolunca sendeleme (KE-8.1) | KOD | T |
| KE-3.7 | Öfke (enrage) | Dövüşün 765. saniyesinde (hedefin 1,7 katı, O-6.8): saldırı sonrası açık kalma yarıya iner, küçük tehdit aralığı 2 s olur, replik KE-11.6 | O-6.8 + ÖNERİ | T |

---

## 4. Tehdit ritmi

Dragon Nest analizinden: oyuncu sürekli diken üstünde olmalı, ama her tehdit önceden okunmalı.

| Kimlik | Katman | Kural | Kaynak | Doğrulama |
|---|---|---|---|---|
| KE-4.1 | Küçük tehdit (ortamdan) | 3–4 s'de bir (düzgün rastgele), §4.1'deki üç tehditten biri. Aynı anda 1 + ⌊(oyuncu sayısı − 1) / 2⌋ tane (1–2 kişi: 1, 3–4: 2, 5: 3). Büyük saldırı duyurusu ve takım görevi sürerken durur | Devir + ÖNERİ | T |
| KE-4.2 | Boss saldırısı | Önceki saldırının açık kalma süresi bittikten 1,5–3 s sonra boss fazın havuzundan bir saldırı seçer (ağırlıklar §5–§7). Aynı saldırı üst üste gelmez | ÖNERİ | T |
| KE-4.3 | Büyük duyurulu saldırı | Fazın 30. saniyesinde ilki, sonra 45–60 s'de bir. En az 3 s önce yazı + geri sayım (SI-11.3); her zaman kesilemez (altın kenar) | Devir + ÖNERİ | T |
| KE-4.4 | Takım görevi | Her fazda 1–2 kez (faz 3'te ana mekanik, KE-7.8). Duyuru yazısı + sayaç (SI-11.4) | Devir | T |
| KE-4.5 | En uzun tehditsiz süre | 8 s. Bu sürede hiçbir ön işaret başlamazsa bir küçük tehdit hemen gelir | Devir | G (8 s'yi aşarsa kırmızı) |
| KE-4.6 | Çakışma | Küçük tehdidin alanı, etkin bir büyük saldırı ya da takım görevinin güvenli alanıyla örtüşmez. Aynı oyuncunun üstünde aynı anda en çok 2 ön işaret | ÖNERİ | T |
| KE-4.7 | Hedef seçme | Saldırının kendi kuralı yoksa: son 10 s'de en çok hasar veren %50, en yakın %30, rastgele %20. Aynı oyuncu üst üste 3 kez seçilmez. Dikkat çeken yem önceliklidir (O-6.19) | O-6.19 + ÖNERİ | T |

### 4.1 Küçük tehditler (her fazda)

Görünümleri SI-9.3. Yerleri arena belgesindeki çatlak listesinden seçilir.

| Kimlik | Tehdit | Ön işaret | Alan | Hasar | Hedef |
|---|---|---|---|---|---|
| KE-4.8 | Çatlak Alevi | 1,0 s | 1,5 × 4 m şerit (en yakın çatlak parçası) | %15 | Rastgele bir oyuncuya 6 m içindeki çatlak |
| KE-4.9 | Düşen Kül Parçası | 1,2 s | Daire r 1,5 m, ön işaret başladığında oyuncunun durduğu yerde (takip etmez) | %12 | Rastgele oyuncu |
| KE-4.10 | Titreyen Zemin | 0,9 s | Daire r 3 m | %10 | Rastgele bir oyuncunun 2–4 m yakınındaki nokta |

Küçük tehditler kesilemez (kaynak ejderha değil), açık kalma yoktur.

---

## 5. Faz 1 — Uyanış (%100–60)

Ejderha yatar. Yalnız baş ve iki pençe saldırır. Öğretici faz: her mekanik tek başına ve açık okunur.

### 5.1 Başlangıç

| Kimlik | Madde | Değer |
|---|---|---|
| KE-5.1 | Uyku | Arena girişinde ejderha uyur: gözler kapalı, nefes döngüsü (SI-8.1), küçük tehdit yok |
| KE-5.2 | Uyanma tetiği | Bir oyuncu Z > −2 çizgisini geçerse ya da ejderhaya hasar verirse |
| KE-5.3 | Uyanış sineması | **İlk giriş:** 4 s (SI-10.4): gözler açılır (kor 0,6 s'de yanar), baş hafifçe kalkar, replik KE-11.1, kükreme (dövüşteki iki kükremeden ilki, SI-8.7). **"Tekrar"da:** 2 s: gözler açılır, baş kalkar; replik ve kükreme yok. İkisinde de tetik KE-5.2 ve oyuncular bu sürede hasar almaz ([K] 17 Ekim; 13 §14'teki "6 m" tetiği yerine). Sonra ilk saldırı 2 s içinde |

### 5.2 Saldırılar

| Kimlik | Saldırı | Ağırlık | Ön işaret | Etkin süre | Alan | Hasar | Kesilebilir | Açık kalma | Hedef |
|---|---|---|---|---|---|---|---|---|---|
| KE-5.4 | **Pençe Çakması** | %45 | 1,0 s: pençe yavaşça kalkar | 0,2 s | Daire r 4 m; merkez hedef oyuncunun konumu, ama pençenin 10 m'sinden uzaksa 10 m'ye çekilir | %25 | Evet | 1,2 s: pençe yerde kalır (vurma penceresi) | Pençenin tarafındaki (X işareti aynı) en yakın oyuncu; o tarafta kimse yoksa diğer pençe |
| KE-5.5 | **Baş Savuruşu** | %30 | 1,2 s: baş bir yana çekilir | 0,3 s | Yay: başın burnundan ölçülen r 8 m, 150°, başın önünde | %30 + 4 m itme | Evet | 1,0 s: baş savuruşun sonunda durur | KE-4.7 |
| KE-5.6 | **Kor Soluğu** | %25 | 1,5 s: boyun ve göğüs koru dolar, nefes çekme sesi | 2,0 s; baş hedefi 15 °/s ile izler | Koni 40°, 14 m, burundan | 0,25 s'de bir %8 (en çok %64) | Evet | 1,5 s: baş yere iner, ağızdan duman | KE-4.7 |
| KE-5.7 | **Kor Dalgası** (büyük, duyurulu) | — | 3 s yazı + geri sayım ("3 saniye sonra Kor Dalgası"); baş kalkar, çatlaklar parlar | Halka başın önünden dışa 8 m/s ile genişler, kalınlık 1,5 m, 1 m yükseklik; güney kenara ~3,2 s'de varır | Bütün arena (dikili taşın arkası korunur: taştan ejderhaya giden çizginin gölgesi) | %45 | Hayır | 2,0 s: baş yere iner | Herkes |

Kor Dalgası'ndan kurtulmanın iki yolu var, ikisi de öğretilir: halka içinden dodge ile geçmek (halka
oyuncunun üstünden 0,19 s'de geçer, dodge hasarsızlığı tek basışta 0,5 s, O-4.6) ya da dikili taşın arkasına girmek.

### 5.3 Takım görevi: Uyku Mührü

| Kimlik | Madde | Değer |
|---|---|---|
| KE-5.8 | Ne zaman | Can %85'e ve %70'e düştüğünde (fazda 2 kez) |
| KE-5.9 | Duyuru | "Mühürler sönüyor — 15 saniyede üç taşı yeniden yakın" + 0/3 sayacı. Üç dikili taşın oymaları sönük kordan söner, taşların çevresinde r 2 m beyaz çember belirir |
| KE-5.10 | Yapılacak | Her taşın çemberinde 2 s kesintisiz duran bir oyuncu o taşı yakar (oymalar 3,0 şiddetle yanar, SI-5.6). Üç taş 15 s içinde yakılmalı. Bir oyuncu birden çok taşı sırayla yakabilir |
| KE-5.11 | Ejderha bu sırada | Yalnız Pençe Çakması kullanır (taşlara yakın olana), küçük tehditler durur |
| KE-5.12 | Başarı | Ejderha 4 s sendeler (KE-8.1'deki görünüm, denge çubuğu dolmadan), replik yok |
| KE-5.13 | Başarısızlık | §9 ceza tablosu |

Tek oyuncu + 2 yapay zekâ dostunda (A4) dostlar en yakın yanmamış taşa gider; oyuncunun gidecek taşı
kalmayabilir.

---

## 6. Faz 2 — Doğruluş (%60–25)

### 6.1 Geçiş

| Kimlik | Madde | Değer |
|---|---|---|
| KE-6.1 | Geçiş sineması | 4 s: ejderha ön ayaklarına kalkar, ikinci ve son kükreme, alın pulu kırılır (%60 katmanı), replik KE-11.2. Kül fırtınası başlar (SI-4.6). Bütün ön işaretler silinir, oyuncular hasar almaz |
| KE-6.2 | Yeni gövde konumu | Baş 10–24 m yükseklikte, Z 8,5–18 üstünde (merkezi ~(0, 16, 12)); göğsün ön yüzü Z ≈ 18'de, alt yüzü 8,6 m (KE-2.10). Pençeler faz 1'deki yerinde kalır (±8, 11,4–16,2) ([K] 17 Ekim, model `Poz_Dogrulmus`). Çarpışma: pençe kapsülleri KE-2.5 gibi; bacak kapsülü r 1,6 m, pençeden omza; baş ve göğüs oyuncunun ulaşamayacağı yükseklikte, çarpışmasız |
| KE-6.3 | Kamera | Baş 10 m'den yükseğe çıktığında kilit bakış noktası başın yerine göğsün ortasıdır (yükseklik 7 m). `10-olculer` §5'e yeni madde olarak işlenecek |

### 6.2 Saldırılar

| Kimlik | Saldırı | Ağırlık | Ön işaret | Etkin süre | Alan | Hasar | Kesilebilir | Açık kalma | Hedef |
|---|---|---|---|---|---|---|---|---|---|
| KE-6.4 | **Pençe Çakması** (yüksekten) | %35 | 1,2 s | 0,2 s | Daire r 5 m (merkez KE-5.4 gibi, 11 m sınırı) | %30 | Evet | 1,2 s | KE-5.4 gibi |
| KE-6.5 | **Tepeden Kor Soluğu** | %35 | 2,0 s: arenanın bir kenarında başlangıç çizgisi ve yön oku | 3,0 s | 4 m genişliğinde şerit, arenayı batıdan doğuya ya da doğudan batıya baştan sona süpürür (6 m/s) | 0,25 s'de bir %10 | Evet (yalnız menzilli Kontrol ulaşır) | 1,5 s | Şeridin başlangıcı, en çok oyuncunun bulunduğu yarının karşı kenarı |
| KE-6.6 | **Kanat Savuruşu** | %30 (en az 20 s arayla; beklemedeyken ağırlığı öbür iki saldırıya eşit bölünür) | 2,0 s: bir kanat sırttan açılıp arenanın üstüne kalkar (`Poz_Kanat_*_Acik`), kırmızı alan dolar | 0,4 s: kanat önce kuzeybatıda yere iner (`Poz_Kanat_*_Supurme_Basi`), sonra güney kenara süpürür (`Poz_Kanat_*_Supurme_Sonu`; uç Z −18,5'e ulaşır, en alçak nokta 1,6 m, toprağa girmez). Kemik zinciri 42 m, güney ucu omuzdan 46–47 m: süpürmede kanat 1,0'dan 1,2 kata büyür ([K] 17 Ekim). Görünen = vuran: kanadın alt yüzü 4 m altından geçtiği alan, pençe çizgisinin güneyinde (Z < 10) üçte birin %87'si. **[AÇIK] (K):** Z 10–18 şeridinde (pençenin iki yanı) kanat alçaktan geçemez, pençenin içinden geçer; bu şerit vuruş alanından çıksın mı (pençe dibi güvenli yer olur), yoksa kanat yüksekten geçse de vursun mu? | Arenanın doğu ya da batı üçte biri: X > 4 ya da X < −4 olan bütün alan (arenanın ~%36'sı). Taraf: o an daha çok oyuncunun bulunduğu taraf; eşitse rastgele | %50 + 6 m arena ortasına doğru itme | **Hayır** (fazın tek kesilemez normal saldırısı, O-6.6) | 1,5 s: kanat yavaşça geri katlanır | Alan sabit (taraf seçimi yukarıdaki gibi) |
| KE-6.7 | **Göğün Çöküşü** (büyük, duyurulu) | — | 3 s yazı + geri sayım; ejderha başını göğe kaldırır | 8 yanan kaya, 0,6 s arayla düşer; her biri 1,2 s ön işaretli | Her kaya daire r 3 m; ilk 5'i oyuncuların o anki konumlarına (oyuncu sayısı < 5 ise kalanı rastgele), son 3'ü rastgele; merkezleri arası en az 4 m | %40 (kaya başına) | Hayır | 2,0 s | Herkes |

Kanat Savuruşu (karar 12 Ekim: kuyruk saldırı için kullanılmaz; v2'de kuyruk batı yamacında görünür ama dekordur) oyuncuları arenanın ortasına iter; kenarda oyalanan oyuncu cezalanır, kalabalık ortada toplanır ve Tepeden Kor Soluğu'nun şeridine açık hale gelir.

### 6.3 Takım görevi: Kül Yağmuru

| Kimlik | Madde | Değer |
|---|---|---|
| KE-6.8 | Ne zaman | Can %50'ye ve %35'e düştüğünde |
| KE-6.9 | Duyuru | "Kül yağıyor — 8 saniyede bir taşın arkasına geçin" + kaç oyuncunun güvende olduğunu gösteren sayaç (ör. 2/3) |
| KE-6.10 | Güvenli alan | Her dikili taşın ejderhaya göre arkasında, taşın 1 m arkasından başlayan 3 m genişliğinde, 6 m uzunluğunda gölge şeridi; zeminde beyaz kenarla gösterilir |
| KE-6.11 | Yapılacak | 8 s dolduğunda bütün yaşayan oyuncular bir gölge şeridinin içinde olmalı. Ejderha kuzeyden bütün arenaya kül soluğu verir (2 s) |
| KE-6.12 | Ejderha bu sırada | Saldırmaz; küçük tehditler durur |
| KE-6.13 | Başarısızlık | Şerit dışındaki her oyuncu kül soluğundan %100 alır (ölür, diriltilebilir; Antrenman'da can 1'de durur). Takıma ayrıca ceza yok ([K] 17 Ekim: yapay zekâ dostunun hatası oyuncuyu cezalandırmaz). Cehennem'de §9 geçerlidir (tur yanar) |

---

## 7. Faz 3 — Kalbin Koru (%25–0)

### 7.1 Geçiş

| Kimlik | Madde | Değer |
|---|---|---|
| KE-7.1 | Geçiş sineması | 4 s: ejderha göğsünü krater zeminine indirir, göğüs plakaları çatlar, kalp görünür ama plakalar kapalı kalır. Replik KE-11.3. Kül fırtınası hafifler (SI-4.6), faz koru ×2,4 (SI-9.6). Model `Poz_Gogus_Yerde`: baş 6–19 m yükseklikte, Z 7–18 üstünde; göğsün ön yüzü Z ≈ 18'de, alt yüzü 2,9 m; kalp (0, 2,3, 22), plakaların arkasında |

### 7.2 Saldırılar

| Kimlik | Saldırı | Ağırlık | Ön işaret | Etkin süre | Alan | Hasar | Kesilebilir | Açık kalma | Hedef |
|---|---|---|---|---|---|---|---|---|---|
| KE-7.2 | **Pençe Süpürmesi** | %40 | 1,6 s: pençe yana açılır | 0,4 s | Yay, pençeden r 10 m, 180°, pençenin önünde | %35 + 5 m itme | Evet | 1,5 s | Pençenin tarafındaki en yakın oyuncu |
| KE-7.3 | **Kor Halkası** | %30 | 1,4 s: kalp plakalarının arasından kor parlar | Halka kalpten 8 m/s ile 12 m'ye kadar genişler, kalınlık 1,5 m. **[AÇIK] (K):** kalp Z 13'ten Z 22'ye geçti (KE-2.11); 12 m'lik halka artık yalnız Z ≥ 10'u kaplar (eskiden Z ≥ 1). Yarıçap 21 m'ye mi çıksın, yoksa kuzeydeki bant mı kalsın? | Halka (içinden dodge ile geçilir) | %30 | Evet | 1,2 s | Herkes |
| KE-7.4 | **Geniş Kor Soluğu** | %30 | 1,8 s | 2,5 s; baş 10 °/s ile izler | Koni 70°, 12 m | 0,25 s'de bir %8 | Evet | 1,5 s | KE-4.7 |
| KE-7.5 | **Kraterin Kalbi** (büyük, duyurulu) | — | 3 s yazı + geri sayım: "İçeriye" ya da "Dışarıya" | İki dalga, 1,5 s arayla: önce iç daire (r 0–8 m) ya da dış halka (r 8–18 m) yanar, sonra öbürü | Bütün arena, iki parça | %45 (dalga başına) | Hayır | 2,0 s | Herkes |

Kraterin Kalbi'nde yazı ilk yanacak bölgeyi söyler: "İçeriye" = önce dış halka yanar, oyuncu içeride durur,
sonra dışarı çıkar. 1,5 s'lik geçiş süresi yürüyerek 6,4 m/s ile ~9 m'dir (O-1.16); 8 m çizgisinin
yakınında duran rahat geçer.

### 7.3 Takım görevi: Kor Mührü (fazın ana mekaniği)

| Kimlik | Madde | Değer |
|---|---|---|
| KE-7.6 | Ne zaman | Faz 3 sabit bir 40 s'lik döngüdür ([K] 17 Ekim; KE-4.3'teki duyurulu saldırı aralığı bu fazda geçerli değil). Döngü fazın 5. saniyesinde başlar: **0 s** Kor Mührü duyurusu (en çok 10 s) → başarıda hemen çökme (12 s, KE-7.8) → normal saldırılar → **28 s** Kraterin Kalbi duyurusu (KE-7.5; dalgalar ve açık kalma ~34,5 s'de biter) → normal saldırılar → **40 s** yeni Kor Mührü. Saniyeler ÖNERİ, gri kutuda ölçülür |
| KE-7.7 | Duyuru | "Kalp mühürleniyor — 10 saniyede üç taşta aynı anda durun" + 0/3 sayacı; üç taşın çemberi (r 2 m) belirir |
| KE-7.8 | Yapılacak | 10 s içinde üç taşın çemberinde **aynı anda** birer oyuncu, birlikte 3 s kesintisiz durur. Başarınca kalp plakaları açılır ve ejderha **12 s** çöker: baş yere iner (`Poz_Sendeleme` görünümü, baş 0–15 m), **bütün vurulabilir bölgeler ×2,0** hasar alır. Kalp vurulmaz; atar ve ekrandaki en parlak sıcak noktadır (KE-2.11, SI-9.6). Kılıç ve Yay ödülü eşit alır ([K] 17 Ekim). **[AÇIK] (K):** çökme sırasında denge dolarsa sendeleme ×1,5 ile ×2,0 çarpılır mı, en büyüğü mü geçer? |
| KE-7.9 | Ejderha bu sırada | Görev süresince yalnız Pençe Süpürmesi kullanır; çökme süresince saldırmaz |
| KE-7.10 | Başarısızlık | §9 ceza tablosu; Zor'da ayrıca can fazın başına (%25) döner |
| KE-7.11 | Oyuncu sayısı 3'ten azsa | Tek oyuncu ya da iki oyuncuda (yapay zekâ dostu yoksa) taş sayısı oyuncu sayısına iner: en yakın taşlardan başlayarak |

### 7.4 Ölüm

| Kimlik | Madde | Değer |
|---|---|---|
| KE-7.12 | Ölüm | Can %0: son pul (boyun) kırılır, replik KE-11.5, ejderha 3 s'de yığılır, kor söner (gözler en son), kül kalkar, sis 5 s'de dövüş öncesine döner (SI-9.10). Sonuç ekranı ölümün başından 6 s sonra (O-17.3'ün 2 s'si bu sürenin içinde değil, sonrasında) |

---

## 8. Sendeleme (her fazda)

| Kimlik | Madde | Değer |
|---|---|---|
| KE-8.1 | Sendeleme | Denge çubuğu dolunca 7 s (O-6.20): faz 1 ve 3'te baş yere düşer, gözlerin koru söner; **faz 2'de ejderha yere çöker ve baş 7 s boyunca yatarkenki yerine iner** (yakın silahlar başa ulaşır). Bütün bölgeler ×1,5 hasar alır. Sonraki 10 s tam kontrol işlemez (O-6.20) |

---

## 9. Zorluk ve ceza

Karar A2: ceza zorluğa göre. **Kural her zorlukta aynıdır:** ön işaret süreleri, alanlar, görev süreleri ve
saldırı sırası değişmez. Zorluk yalnız sayıyı ve cezayı değiştirir. Uyarı her zaman nettir.

| Kimlik | Madde | Antrenman | Normal | Zor | Cehennem |
|---|---|---|---|---|---|
| KE-9.1 | Boss hasar çarpanı | ×0,5 | ×1,0 | ×1,25 | ×1,5 |
| KE-9.2 | Boss can çarpanı | ×0,8 | ×1,0 | ×1,3 | ×1,6 |
| KE-9.3 | Takım görevi kaçarsa | Ceza yok; "Görev kaçtı" yazısı ve ne yapılması gerektiği bir cümleyle | Bütün takım azami canının %40'ı kadar hasar alır | Bütün takım %70; Kor Mührü'nde ayrıca can %25'e döner | **Tur yanar**: ejderha kraterin korunu salar, bütün takım ölür (replik KE-11.7) |
| KE-9.4 | Diriltme | Ölüm yok (can 1'de durur) | Sınırsız (O-14.6: dost yanında 3 s) | Takım başına 3 | Takım başına 1 |
| KE-9.5 | Faz seçimi | Faz 1, 2 ya da 3'ten başlanabilir; Kor Mührü ve Kül Yağmuru ayrı ayrı denenebilir | Hayır | Hayır | Hayır |
| KE-9.6 | Açılma | Her zaman açık | Her zaman açık | Normal'i bitirince | Zor'u bitirince |

Kül Yağmuru'nda şerit dışında kalan oyuncu Antrenman dışında her zorlukta ölür (KE-6.13); Normal ve Zor'da takıma
ayrıca ceza gelmez. Cehennem'de görev kaçarsa tur yanar.

---

## 10. Hedef seçme ve yapay zekâ dostları (ilk hedef için)

| Kimlik | Madde | Değer |
|---|---|---|
| KE-10.1 | Boss'un dosta bakışı | Yapay zekâ dostu hedef seçmede oyuncu gibi sayılır (O-6.19'daki %70 / %30 ayrımı bu boss'ta yok) |
| KE-10.2 | Dostların temel davranışı | Ön işaretin dışına en kısa yoldan çıkar (0,3 s tepki gecikmesiyle, kusursuz değil); büyük saldırıda dikili taş gölgesine ya da dodge'a gider; takım görevinde en yakın boş taşa gider; boş zamanda biri pençeye, biri başa (menzilli ise) saldırır |
| KE-10.3 | Dostların hatası | Dostlar her 5 ön işaretten 1'inde geç kalır (oyuncu kendini yalnız hissetmesin, ama işi taşımasınlar) |

---

## 11. Replikler

Ekranın alt ortasında altyazı (#E6E1D8, SI-11.1). Her biri en çok 8 kelime. Replik sırasında ejderha durur
(SI-8.6). **Seslendirme sonra yapılacak (kullanıcı kararı, 12 Ekim);** o zamana kadar replikler yalnız
altyazıdır, ejderha replik sırasında alçak bir hırıltı çıkarır.

| Kimlik | An | Replik |
|---|---|---|
| KE-11.1 | Uyanış | "Yine mi küçük alevler. Yaklaşın." |
| KE-11.2 | Faz 2 başı | "Ben bu dağdan önce vardım." |
| KE-11.3 | Faz 3 başı | "Kalbimi görmek mi istiyorsunuz? Bakın öyleyse." |
| KE-11.4 | İlk başarılı Kor Mührü | "Taşlar... hâlâ hatırlıyor." |
| KE-11.5 | Ölüm | "Kor sönmez. Yalnız bekler." |
| KE-11.6 | Öfke | "Yeter." |
| KE-11.7 | Cehennem'de tur yanınca | "Uyuyun, küçük alevler." |
| KE-11.8 | Bütün takım ölünce (diğer zorluklar) | "Bir dahakine." |

---

## 12. Boss'un kullandığı arena noktaları

Arena belgesi bunları değiştirmez; geri kalan her şeyi o belirler.

| Kimlik | Nokta | Konum |
|---|---|---|
| KE-12.1 | Dikili taşlar | (−9, 4), (9, 4), (0, −10); her biri taban 2 × 1,5 m, yükseklik 6–9 m, çarpışma kutusu tabanla aynı, 6 m |
| KE-12.2 | Görev çemberleri | Her taşın güney yüzünün 2 m önünde, r 2 m. (0, −10) taşınınki kuzey yüzünde (ejderhaya bakan yüz) |
| KE-12.3 | Gölge şeritleri (Kül Yağmuru) | Her taşın ejderha başına göre arkasında (KE-6.10) |
| KE-12.4 | Oyuncu başlangıcı | (0, −14); çok oyunculuda bu nokta çevresinde 2 m arayla yay (O-7.5) |
| KE-12.5 | Uyanma çizgisi | Z = −2 |
| KE-12.6 | Çatlaklar | Arena belgesinde; küçük tehditlerin Çatlak Alevi'ni taşıyacak en az 12 çatlak parçası, oynanış alanına yayılmış, birbirinden en az 4 m uzak |

Dikili taşlar arası mesafeler: (−9, 4)–(9, 4) 18 m; yan taşlardan (0, −10)'a 16,6 m. Yürüme hızıyla (6,4 m/s)
en uzak taşa ~3 s. Uyku Mührü'nün 15 s'si ve Kor Mührü'nün 10 s'si bu mesafelerle rahat ama boş geçmeyecek
şekilde seçildi; gri kutuda denenir (`17` C2).

---

## 13. Diğer belgelere etkisi (B4'te işlenecek)

| Belge | Madde | Değişiklik |
|---|---|---|
| `10-olculer.md` | §6.3 Ağların Kraliçesi (O-6.10–O-6.21) | Kalkar; yerine bu belgeye bağlantı |
| `10-olculer.md` | O-6.6 Kesilemez saldırı | "Normal saldırılardan boss başına 1–2 kesilemez; büyük duyurulu saldırılar her zaman kesilemez" |
| `10-olculer.md` | O-6.5 Dönme hızı | "Yürüyen boss'lar için"; Kül Ejderi'nde baş dönüşü KE-2.7 |
| `10-olculer.md` | O-6.8 Öfke, O-17.4 hedef süre | Kül Ejderi'nde hedef 450 s, öfke 765 s (KE-3.2, KE-3.7) |
| `10-olculer.md` | §5 Kamera | Yeni madde: baş 10 m'den yüksekse kilit bakış noktası göğüs (KE-6.3) |
| `10-olculer.md` | O-17.1 dilim akışı | Uyanış tetiği ve sinema KE-5.2, KE-5.3 |
| `02-dunya-fizigi.md` | D-1 boss gövde yarıçapı | "Boss başına, boss belgesinde" (KE-2.6) |
| Kod | `Resources/Bosses/aglarin-kralicesi.json`, örümcek asset'leri | Silinir (A3); yerine `kul-ejderi.json` |
