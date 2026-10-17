# 15 — Dodge tasarımı: zincirli dodge

Durum: taslak, 14 Ekim 2026 (Mustafa'nın 09:39 cevabı işlendi). Etiketler `13-vizyon-ve-cerceve.md` biçiminde: **[K]** Mustafa'nın kararı,
**[Ö]** öneri (onay bekler), **[AÇIK]** karar bekleyen soru, **[RED]** reddedilen.
Sayı sınıfları: **(kâğıt)** kuralla belirlenir, **(ölçüm)** oynanarak bulunur. Bu belgedeki sayılar başlangıç
tahminidir; kaynağı yanında yazılıdır.

Kaynaklar: `13` §3, §8, §9 · `09` [K] i-frame (12 Ekim) · `10` O-3.4, O-3.5, O-4.4–O-4.12, O-2.19 · `06` §5 ·
Mustafa'nın 14 Ekim mesajı. Kod okunmadı.

---

## 1. Dodge'un oyundaki yeri

- **[K]** Boss tek atma yetenekleri dodge'a bağımlıdır. Mesele mükemmel dodge değil, doğru yerde doğru dodge
  (`13` §3). "Kalahan affetmez, dodge affeder."
- **[K] [RED]** Mükemmel/kusursuz dodge yok: hak iadesi, ×1,3 vuruş, ağır çekim yok (`13` §3, A-13.13).
- **[K]** Dodge rün dışıdır; her oyuncuda, her silahta aynı davranır (`06` §1, AS-03).
- **[K]** Dodge kendi başına bir başlıktır (Mustafa, 14 Ekim). Hareket rünü skilleri ve Koruma parry'si ayrı
  sistemlerdir; bu belge yalnız sınırlarını çizer (§8).

## 2. Ana fikir: zincirli dodge

**[K] Tarz** (Mustafa, 14 Ekim; Dragon Nest M referansı): "Basıyorsun, i-frame penceresi sağlıyor. Wipe daha
uzunsa bir kere daha basıyorsun, bir hareket daha yapıyor, ikisini birleştiriyor." Verilen sayılar (tek 0,5 s,
zincir 1,3 s, wipe 1,2 s) örnektir, kesin değildir.

Bundan çıkan üç kural:

1. **Birinci basış = zıplama.** Hemen çıkar, kısa bir hasarsızlık penceresi açar (`06` §5.1, O-4.4).
2. **İkinci basış = takla.** Birinci hareket sürerken basılırsa hareketi uzatır, yönü değiştirebilir.
3. **Pencereler birleşir.** İkinci basış birinci pencere kapanmadan gelirse iki pencere tek, kesintisiz ve
   daha uzun bir hasarsızlık penceresi olur. Zincirin içinde iki pencere arasında boşluk kalmaz.
   (Birleşme penceresi kaçınca ne olduğu §3.2'de: sıradan yeni dodge, zincir değil.)

Tasarımdaki işlevi: boss saldırıları **süreleriyle** sınıflanabilir. Kısa saldırıya tek dodge yeter; uzun
saldırı zinciri ister, yani iki hakkın ikisini birden harcatır. "Doğru yerde doğru dodge" böylece iki
kararlı olur: ne zaman başlayacağım, ve bu saldırı tek mi zincir mi istiyor.

## 3. Kurallar (kodlayacak ajan için)

### 3.1 Birinci basış
- **[Ö]** Basış anında dodge başlar; çift basışı anlamak için **beklenmez** (gecikme hissi olmasın; O-3.4'te
  dodge'a geçiş 0,03 s). İkinci basış ayrı bir olaydır, birincisini geri almaz.
- Yön: joystick yönü; joystick boşsa karakterin baktığı yön (O-4.4).
- Hareket iki fazlı: patlama + süzülme (O-4.4a, onaylı K 12 Ekim).
- Hasarsızlık penceresi basış anında açılır (O-4.6).
- 1 hak harcar (O-4.7).

### 3.2 İkinci basış ve birleşme penceresi
- **[Ö] Birleşme penceresi = birinci basışın hasarsızlık penceresi.** İkinci basış bu pencere açıkken gelirse
  **zincir** olur:
  - Takla başlar; birinci hareketin kalan yolu kesilir, hızın yarısı yeni yöne aktarılır (`06` §5.1 H-1/H-2/H-3,
    kendini taşıma kuralı aynen işler).
  - Yön ikinci basış anındaki joystick yönüdür (boşsa birinci dodge'un yönü).
  - 1 hak daha harcar. Yani zincir iki hakkı birden yer.
  - Hasarsızlık penceresi kapanmadan uzar (§3.3).
- **[Ö] Birleşme penceresi kapandıktan sonra** (birinci hareket hâlâ süzülürken) gelen basış zincir değildir:
  süzülmeyi kesen **sıradan yeni bir tek dodge** olur (kendi `T_tek` penceresi, takla yok, 1 hak). Özel bir
  kural yoktur; zincir sayılmaz. Birinci pencerenin bittiği an ile bu basış arasında korunma yoktur, ama bu
  bilinçli bir "boşluk mekaniği" değildir, yalnız sıradan dodge davranışıdır. Birleşme penceresi hareketle
  (0,65 s) eşitlenmez; `T_tek` ayrı ayarlanır.
- Havada en çok 1 takla; üçüncü basış yok sayılır (`06` §5.1). Hak kalmadıysa ikinci basış yok sayılır ve
  boş-hak geri bildirimi verir (§5).

### 3.3 Birleşen pencerenin uzunluğu
**[K] Model A** (Mustafa, 14 Ekim: "Birleşen pencere ben de A diyorum"). B reddedildi.

| Model | Kural | Artı | Eksi |
|---|---|---|---|
| **A [K]** Sabit toplam | Zincir penceresi **birinci basıştan itibaren** sabit `T_zincir`. İkinci basışın ne zaman geldiği (birleşme penceresi içinde) süreyi değiştirmez | Öngörülebilir; boss tablosu tek sayıyla yazılır ("bu saldırı zincir ister"); gizli "en geç bas" optimizasyonu yok, yani mükemmel dodge'un arka kapıdan dönmesi yok | Hızlı çift basış ile geç ikinci basış aynı ödülü alır; ikinci basışın zamanlaması anlamsızlaşır (bilinçli) |
| **B [RED]** Basıştan uzatma | Pencere ikinci basıştan itibaren `T_ikinci` daha sürer; toplam = ikinci basış anı + `T_ikinci` | İkinci basışı geç yapmak ödüllenir, ustalık tavanı | Toplam değişken, boss tablosu yazılamaz; "son karede bas" bir mükemmel-zamanlama oyunu olur, `13` §3 [RED] ile sürtüşür |

### 3.4 Sayılar

**[K] Sayılar oynanıştan sonra belirlenir; kesin değerler dengeleme aşamasına (Aşama 8 ayar masası) kalır.
Şimdilik test değerleri cömert seçilir ki fark telefonda açıkça hissedilsin** (Mustafa, 14 Ekim).

| Ölçü | Bugünkü belge | **Test değeri** | Sınıf |
|---|---|---|---|
| Tek hasarsızlık penceresi `T_tek` | 0,26 s (O-4.6, `09` [K] 12 Ekim) | **0,5 s** (Mustafa örneği) | ölçüm, dengelemede |
| Zincir penceresi `T_zincir` (birinci basıştan) | 0,52 s (O-4.6, takla) | **1,3 s** (Mustafa örneği) | ölçüm, dengelemede |
| Birleşme penceresi | O-4.5'te "ilk basıştan sonra 0,2 s" | **`= T_tek`** (0,5 s) | kâğıt [Ö] |
| Tek dodge mesafe / süre | 3,8 m; patlama 0,25 s + süzülme 0,40 s (O-4.4, O-4.4a) | aynı | ölçüm |
| Zincir mesafe | ×1,6 → 6,1 m (O-4.5); `06` H-3 tavanı ×1,5 | **×1,6** (kesin test değeri) | ölçüm, dengelemede |
| Hak / dolum | 2 hak, 6 s (O-4.7) | aynı | ölçüm |

Test değerleri `09` [K] 12 Ekim'deki 0,26 / 0,52 s'nin yerine geçmez; o satır "test değeri 0,5 / 1,3 s,
kesin değer dengelemede" diye güncellenir.

**[Ö] Kâğıt ilişkileri** (sayılar değişse de korunur):
- `T_zincir ≥ 2 × T_tek`. Bugünkü belge tam 2×, Mustafa örneği 2,6×. Oran büyüdükçe zincir "uzun saldırı
  aracı" olarak tek dodge'dan ayrışır.
- `T_tek ≥ 2 ×` cihaz dokunma gecikmesi (`13` §8 adalet paketi, M-13.2).
- **[K] Dodge ile geçilmeyen boss saldırısı yoktur** (Mustafa, 14 Ekim). Kâğıt kuralı: her boss saldırısının
  etkin süresi ≤ `T_zincir`. Dengelemede `T_zincir` değişirse boss saldırı süreleri de onunla birlikte kontrol
  edilir.
- Boss saldırı süresi sınıfları (boss tablosuna sütun olur):
  - **kısa:** etkin süre ≤ `T_tek` → tek dodge yeter.
  - **uzun:** `T_tek` < süre ≤ `T_zincir` → zincir ister (iki hak).
  Mustafa örneğinde 1,2 s'lik wipe "uzun" sınıfındadır.

## 4. Diğer kurallarla ilişkisi (değişmez, hatırlatma)

- **Kesme ([K] 16 Ekim):** dodge skill'i her fazda (hazırlık, etki, toparlanma) hemen keser. Hazırlık kesilirse
  skill atılmaz, mana ve CD yanar, iade yok (O-3.5). Etki geçtiyse CD yanar. Dodge sürerken skill ve düz vuruş
  çalışmaz. Öncelik: dodge > silah değişimi > düz vuruş. (Eski [Ö] "CD yanmaz / etki kesilmez" reddedildi.)
- **Darbe alma:** ağır darbe ve kalkmanın ilk 0,3 s'si dodge ile kesilir (O-3.8, O-3.9). Her durumdan dodge'a
  geçiş vardır, ölüm hariç (O-3.11).
- **Silah değiştirme:** sırasında dodge yok (O-2.19).
- **Güdümlü kilidi:** ilk 3 s'de dodge kilidi bozmaz (`06` S-4 önerisi; açık).
- **Girdi tamponu:** toparlanmada verilen dodge 0,15 s saklanır (O-4.12).
- **Engeller ve arena:** dodge ve takla engelden geçmez, kenardan 0,15 m önce durur; arena dairesi içinde kalır
  (O-4.9, O-4.11). Engelde durmak hasarsızlık penceresini kısaltmaz.
- **Kombo kademesi:** dodge kombo kademesini kırmaz (`13` §9).
- **Konum sahipliği:** dodge ve takla konumu tek sahip olarak yazar (AGENTS.md değişmez kural 5); takla ikinci
  bir gizli konum yazarı değil, aynı sahibin yeni yoludur.
- **Çok oyunculu:** dodge yerel tahminle anında çalışır (O-14.4); hasarsızlık kararı cihazda, host toleranslı
  (`13` §8).

## 5. Görünür ve hissedilir olan

- **Parlama (O-4.6a):** hasarsızlık penceresi boyunca karakter kenarında kor turuncusu parlama.
  **[Ö] Zincirde parlama sönmeden sürer.** Birleşmenin tek kanıtı budur: oyuncu "tuttu" diye görür.
  Birleşme penceresi kaçmışsa (§3.2) parlama söner, yeni dodge ile yeniden yanar.
- **Hareket:** birinci basış yay çizen zıplama, ikinci basış havada takla (`06` §5). Ayak zemin altına inmez
  (O-1.23).
- **İz:** yalnız süzülme fazında, oyuncunun arkasında (O-4.10).
- **[Ö] Hak göstergesi:** dodge butonunda iki hak dilimi. Zincir ikisini birden boşaltır; masa kimin hakkının
  bittiğini görebilir.
- **[K] Hak iadesi yoktur** (16 Ekim: "yaptığının sonucuna katlanırsın"); engele çarpıp yol alamayan dodge da hak yakar.
- **[Ö] Boş hak:** hak yokken basış dodge yapmaz; buton kısa sarsılır, hafif titreşim. Sessizce yutulmaz.
- Mükemmel dodge'a ait ağır çekim, özel ses, hak iadesi yoktur ([RED]).

## 6. Karşı savlar (başka oyunlarda ne oldu)

- **Dragon Nest M (referans, Mustafa'nın tarifi; oyun ayrıca incelenmedi):** zincirlenen kaçış hareketi ve
  güvenliği birlikte uzatır. Tuttuğu yer: tek kural, okunur, öğrenilebilir.
  Bizde fark: hak sayısı 2 olduğu için zincir hakların tamamı demek. Risk, zincirin "her şeye bas" refleksine
  dönmesi; karşılığı 6 s boş kalma bedelidir. Her saldırı tek başına dodge'lanabilir; ama boss iki "uzun"
  saldırıyı 6 s içinde arka arkaya atarsa ikincisi için hak kalmayabilir. Bu bir hak yönetimi sorusudur
  (`13` §9 "ikincisini saklar"), dodge'lanamayan saldırı değildir. Boss tasarımında kullanılıp
  kullanılmayacağı → D-7.
- **Dark Souls (yuvarlanma):** sabit i-frame, zincir yok. Tuttuğu: tek basışın okunurluğu. Tutmayan: uzun
  saldırılara cevap "geç yuvarlan" ustalığına kalır, bu da mükemmel-zamanlama oyunudur; bizde [RED]. Model A
  bu yüzden önerildi.
- **Bayonetta / Genshin benzeri "tam zamanında kaçış" ödülleri:** kaldırıldı (`13` §3); zincir ödülü zamanlama
  değil kaynak (hak) harcamasıdır.
- **Mobil çift dokunuş riski:** çift dokunuşu bekleyen tasarımlar birinci basışı geciktirir. §3.1'deki
  "beklenmez" kuralı bunun için.
- **En güçlü itiraz:** zincir, tank/destek gibi Hareket rünü olmayan oyunculara (A-13.11) neredeyse bir
  Hareket skilli verir (6 m + uzun hasarsızlık). Bu Hareket rününün değerini düşürebilir. Ölçülecek: Hareket
  skillinin yolculuk hasarsızlığı (`09` [K]) zincirden açıkça daha uzun/uzak kalmalı.

## 7. Açık sorular

| Kod | Soru | Seçenekler | Öneri |
|---|---|---|---|
| **D-7** | Boss, hak tüketmek için iki "uzun" saldırıyı 6 s içinde arka arkaya atabilir mi? | evet (hak yönetimi sınavı) · hayır (aralık ≥ dolum süresi) | **[K] 16 Ekim: hayır** (alfa için; aralık ≥ 6 s) |

Kapananlar (16 Ekim): D-7 → [K] hayır, aralık ≥ 6 s · takla yönü engele bakıyorsa → joystick yönüne, engelde durur · hak iadesi yok.

Kapananlar (14 Ekim): D-1 sayılar → test değeri 0,5 / 1,3 s, kesin değer dengelemede · D-2 → [K] Model A ·
D-3 mesafe → test değeri ×1,6, kesin değer dengelemede · D-5 coyote time → **kaldırıldı**: hasar karesinden
sonra gelen basış korumaz (`13` §3 [RED] mükemmel dodge'un sonucu; `13` §8'deki "dodge coyote time" ifadesi
silinir) · D-6 → ikinci basış birinci basışla aynı kurallara uyar (Güdümlü kilidi, iniş; silah değişiminde
zaten dodge yok) · D-4 birleşme penceresi `= T_tek`; sonrasında basış sıradan yeni dodge (§3.2).

## 8. Sınır: Koruma parry'si (ayrı konu)

- Parry Koruma rününden gelir, çizilen bir skilldir; dodge rün dışıdır, butonla yapılır.
- Parry de hasarsızlık verir (Mustafa, 14 Ekim) ama ek olarak boss'u sersemletebilir (`13` §8 [Ö]); dodge boss'a
  hiçbir şey yapmaz.
- Dodge hakları ile parry kaynakları (mana, CD) paylaşılmaz.
- Bu belgede tanımlanmayan, parry belgesine kalanlar: parry penceresi, dodge ↔ parry birbirini keser mi, parry
  parlaması dodge'unkinden nasıl ayrılır (iki farklı görünüm önerilir, masa hangisinin olduğunu görmeli).

## 9. Mevcut belgelerle çelişkiler (karar sonrası güncellenecek)

- **Kapandı (16 Ekim, belge temizliği):** `10` O-4.5, O-4.6, O-4.7, O-4.8, O-10.4, O-10.5, `09` i-frame satırı, `06` H-3 notu, `13` §8 coyote ve README bu belgeye göre güncellendi.
- O-4.5: "0,2 s içinde" ve "toplam süre ×1,4 (0,35 s)" iki fazlı dodge'la (O-4.4a, 0,65 s) uyuşmuyor.
- O-4.6 / `09` [K]: 0,26 / 0,52 s → test değeri 0,5 / 1,3 s, kesin değer dengelemede.
- `06` §5.1 H-3 ×1,5 tavanı dodge taklasına uygulanmaz; dodge taklası ×1,6 kullanır (Mustafa, 14 Ekim: test
  değerleri cömert). `06` satırı ayrı belge PR'ında güncellenir.
- `13` §5 telegraf 0,8–1,5 s; saldırı etkin süresi ise ≤ `T_zincir` olmalı (iki farklı ölçü, karıştırılmasın).
- **Belge kuralı:** dodge PR'ı yalnız bu belgeyi ekler; eski belgelerdeki (`10` O-4.5, O-4.6, O-4.8, O-10.4,
  `09`, `06`, `13` §8 coyote) çelişen satırlara dokunmaz. Bu liste çelişkilerin kaydıdır; öncelik bu belgededir.
  Eski satırlar ayrı bir belge PR'ında güncellenir.
- `plan/asama-1-dodge.md` PR 1.2 eski O-4.5'e göre yazılmış; bu belgeye göre güncellenmeli.
- O-4.8, O-10.4 (kusursuz dodge titreşimi), O-10.5 `10`'da **[RED]**; A-13.13 ✅ (17 Ekim 2026).

## 10. Kodlayan ajana yön

**Bu belge kodlamak için yeterlidir; tasarım sorusunu kodlayan ajan Mustafa'ya sormaz.** Belgede cevabı
olmayan bir durum çıkarsa en yakın kuralı uygular, PR'da "belgede yok, şunu varsaydım" diye yazar. [Ö] etiketli
maddeler (5a–5e: anında çıkış, sönmeyen parlama, iki hak dilimi, boş hak sarsıntısı, `T_zincir ≥ 2 × T_tek`)
kodlanacak varsayılandır; telefonda beğenilmezse değişir.

- Okuma sırası: bu belge → `10` O-4.x → `06` §5 → `kural-motoru-v4.json` / `DodgeTuning`.
- Sayı uydurma: §3.4'teki test değerleri kullanılır, tek yerden (tuning) okunur; PR'da "test değeri,
  dengelemede kesinleşecek" diye yazılır. `T_tek`, `T_zincir`, birleşme penceresi ayrı ayarlanabilir alanlar olsun (Aşama 8 ayar
  masası bunları telefonda değiştirecek).
- Testler gerçek yoldan: sahte dodge dokunuşu → hasar alıp almadığı, konum, oynayan klip. Önerilen adlar:
  `O_4_5_Zincir_PencereBirlesir` (birleşme penceresinde ikinci basış → `T_zincir` boyunca boss vuruşu hasar
  vermez), `O_4_5_GecIkinciBasis_SiradanDodge` (birleşme penceresi kapandıktan sonra basış → takla oynamaz, yeni
  `T_tek` penceresi basış anında açılır, 1 hak harcanır, zincir sayılmaz),
  `O_4_7_Zincir_IkiHakYer`, `O_4_6a_Parlama_ZincirdeSonmez`.
- Yapılmayacak: mükemmel dodge'un herhangi bir parçası; birinci basışı çift basış için bekletmek; ikinci gizli
  konum yazarı.

**Telefonda göreceğin:**
- Dodge'a bir bas: yay çizen zıplama, kısa turuncu parlama.
- Zıplarken tekrar bas: havada takla, daha uzağa gider, turuncu parlama hiç sönmeden sürer, iki hak birden
  boşalır.
- Parlama bittikten sonra basarsan: takla yok, sıradan yeni bir dodge çıkar (yeni kısa parlama, 1 hak).
- 1,2 s süren bir boss saldırısı: tek dodge ile (0,5 s) vurulursun, zincirle (1,3 s) hasar almadan geçersin.
