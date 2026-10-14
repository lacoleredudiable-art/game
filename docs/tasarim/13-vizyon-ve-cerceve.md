# Vizyon ve çerçeve (13 Ekim 2026 sohbeti)

Bu dosya kural kitabının **üstündeki** katmanı tutar: kim oynar, ne kadar, hangi his, zorluk, yetenek katmanı,
zaman ekonomisi. `01-kural-kitabi-v4.5.md` ve `09-sonraki-kararlar.md` bunun altında çalışır.

Buradaki hiçbir madde dilime (kök `AGENTS.md`: Kılıç + Yay, rünler 1-2-3-4-6-9, Ağların Kraliçesi) kendiliğinden
girmez. Dilime giriş ayrı bir [K] kararıdır. Bu dosyadan kod yazılmaz; `14-kodlama-plani.md` ne diyorsa o yapılır.

Etiketler:
- **[K]** kullanıcı kararı (tarih: 13.10 aksi yazılmadıkça).
- **[Ö]** ajan önerisi; kullanıcı karar vermedi. Karar değildir, koda girmez.
- **[AÇIK]** soru; kimin cevaplayacağı yazılı (K = kullanıcı, M = oyunda ölçüm).
- **[RED]** reddedildi; neden yazılı.
- Her sayı için sınıf: **(kâğıt)** kuralla belirlenir, **(ölçüm)** oynanarak bulunur, yazılan değer başlangıç
  tahminidir. Kaynak yoksa "tahmin" yazılır.

---

## 1. Kim, nerede, ne kadar

- **[K]** 5 arkadaş, akşam, kahvehane, 3 saat. Oynadıktan sonra ertesi günü iple çekmeliler.
- **[K]** Finansal hedef yok. Amaç oyunu tam haliyle arkadaşlara göstermek. Tasarım hevesi projeyi taşıyan şey;
  kod aşaması her seferinde duvar.
- **[K]** LoL benzetmesi: 5 arkadaş gidip LoL oynar gibi keyif. Biz kendimize özel oyun tasarlıyoruz.

Ortamdan türetilen kısıtlar (Ö, tahmin):
- Gürültü → ses ipucuna güven yok; her uyarı görsel + titreşim.
- Aynı masa → oyun içi iletişim gereksiz; tersine oyun masayı **konuşturmalı** (asimetrik bilgi: uyarıyı yalnız
  hedeflenen görür → "ağ bende, çekilin!").
- Ağ belirsiz: 150–250 ms ping ve 20–30 sn kopmalar olağan sayılır. Kopmada 30 sn içinde aynı dövüşe dönüş.
- Telefon 3 saat elde: pil saatte ≤ %25–30 (tahmin), ısı kısması kabul.
- Bir akşam gelmeyen geride kalmamalı → güç farkı yok (bkz. 2).

## 2. Oyun kimliği

- **[K]** LoL gibi: herkes eşit roller ve yetenekler. **Gelişim (güç ilerlemesi) yok.** Amaç zindanı geçmek.
- **[K]** Rün sistemi, 160 karakterli LoL çeşitliliğine olanak sağlamalı.
- **[K]** 5v5 ileride düşünülebilir; önce bossa karşı. Sürekli LoL teamfight havası.
- **[K]** Çift anlamlı rün onaylandı: aynı rün ilk çizilince fiil, ikinci çizilince sıfat. 6 rün + silah = build.
- **[Ö]** Build = "şampiyon". C(12,6) × 2 silah = 1.848 build (hesap). Build 6 rakamla yazılır ("2-4-6-9-11-12")
  ve satranç açılışı gibi masada dolaşır. Zindan öncesi seçim ekranı = champ select ritüeli.
- **[K]** Yaratıcılık hedefi: souls-like'taki gibi tuhaf ama çalışınca etkileyici buildler (yumruk atan büyücü,
  healer samuray). Oyun sırasında "A kişisi bu build baya OP" anı. Deadlock'taki Solomon gibi savaş alanında
  satranç kuran yapılar.
- **[Ö]** "Vay anasını" skill'den değil etkileşimden çıkar: az sayıda evrensel dünya kuralı, çok sayıda
  kombinasyona istisnasız uygulanır (Divinity "yağ yanar" örneği). 200.000 skill değil, 10–15 dünya kuralı.

## 3. Zorluk ve ölüm

- **[K]** Referans: Dragon Nest Mobile, Green Dragon Nest Stage 5 **Kalahan**
  (https://youtu.be/YZvvOE0K05o). 5 kişi 50–60 deneme; geçemediler ama tekrar denediler çünkü **neden
  başaramadıklarını biliyorlardı** (tank dodge atamadı, healer/DPS öldü, hasar yetmedi).
- **[K]** Zindan zor olmalı. Boss **%5'te düşürmeli, planlı.** "Ah ulan az kalmıştı" hissi.
- **[K]** Ölünce diriliş yok. Biri ölünce 4v boss devam eder; mükemmel oynayınca kazanmak mümkün olmalı,
  **~10.000'de 1**.
- **[K]** Boss tek atma yetenekleri; dodge'a bağımlı. Mesele mükemmel dodge değil, doğru yerde doğru dodge.
- **[K] [RED] Mükemmel dodge kaldırıldı.** İlk zamanların prototipiydi, gerekli değil. "Dodge dodge'dur.
  Kalahan affetmez, dodge affeder." README ve kural kitabındaki mükemmel dodge satırları bu karara göre
  güncellenmeli (→ A-13.13).

## 4. Başarısızlığın okunabilirliği (Ö; K'nın Kalahan anlatımından türetildi)

50–60 denemeyi taşıyan şey zorluk değil, **her wipe'ın tek cümlelik bir sebebe inmesi.**

- Her ölümün sahibi belli: kim, hangi mekanik, ne yapmalıydı, boss yüzde kaçtaydı. Wipe ekranı hakem; masa
  tartışması kısa sürer, sonraki deneme hemen başlar. Örnek: "Tank — Kuyruk Savurma — dodge 0,3 sn geç."
- Üç wipe sınıfı; her ölüm tam birine düşer, "şanssızdık" denemez:
  (a) tek atma mekaniği kaçırıldı; (b) sürdürülebilir hasar yetmedi (healer kontrolü); (c) hasar yetmedi
  (DPS kontrolü / enrage).
- **Her boss mekaniği tek bir fiil kontrolüdür.** Boss tablosunun sütunları: mekanik → hedef kuralı → cevap veren
  fiil → başarısızsa wipe sınıfı → kim ölür. "Bu bossa Arındırma lazım" cümlesi girişte söylenir.
- **Her kontrolün ≥ 3 fiil yolu.** Hasar kontrolü yalnız Zarar'la değil: Çağırma, Zayıflatma, Yansıma'nın geri
  yolladığı hasar da sayar. Böylece tuhaf buildler sert boss'ta yaşar.
- Rol ölümü farklı okunur: tank ölürse zincir wipe; healer ölürse 60–90 sn sonra çöküş; DPS ölürse enrage'e
  yetişememe.
- Wipe'a oylamayla anında dönüş (4 kişi "bitti" → yeniden başla). Diriliş yok ama **vazgeçme ucuz.**

Fiil → wipe sınıfı eşlemesi (Ö):

| Wipe sınıfı | Cevap veren fiiller | Adres |
|---|---|---|
| (a) tek atma kaçırıldı | dodge (rün dışı), Hareket, Kontrol/Kuvvet (kesme), Arındırma (mekanik debuff ise), Zaman (? → A-13.6) | mekaniğin sahibi |
| (b) sürdürülebilir hasar | Şifa, Koruma, Yansıma, Zayıflatma (boss hasarı kolu) | Destek |
| (c) hasar / enrage | Zarar, Çağırma, Güçlendirme, Zayıflatma (kırılganlık kolu) | Saldırı |

## 5. Boss anatomisi (Ö; sayılar tahmin)

| Parametre | Başlangıç | Sınıf | Gerekçe |
|---|---|---|---|
| Tek atma mekaniği | 3–4 / boss, faz başına 1 yeni | kâğıt | 5 kişi 3 saatte ezberlesin, ilk akşam bitiremesin |
| Faz sayısı | 3; son faz en ölümcül | kâğıt | %5 anı son fazda üretilir |
| Zor kısma ulaşma | ≤ 2 dk | ölçüm | 50 deneme × uzun giriş = işkence |
| Deneme (tam koşu) | 5–7 dk | ölçüm | 3 saatte 20–27 deneme |
| Deneme arası | ≤ 60 sn; wipe → başla ≤ 20 sn | ölçüm | kaybetmenin cezası zaman olmamalı |
| Checkpoint | yok | kâğıt | risk tek parça; erken fazlar kısa |
| Boss boş zaman | ≤ 10 sn aralıklar | ölçüm | teamfight hissi kesilmesin |
| Telegraf | 0,8–1,5 s, görsel + titreşim | ölçüm | gürültülü masa |
| Ekran kalabalığı | aynı anda görünür tehdit tavanı | kâğıt (sayı: ölçüm) | 6 inç ekranda 5 kişi + boss |
| Enerji/stamina tavanı | **yok** | kâğıt | 3 saatlik seansı öldürür |

- **Planlı %5:** son fazda yeni tek atma + hasar artışı birlikte gelir. DPS kontrolü "ilk iki fazı temiz geçen
  ortalama takım son fazın ortasına ulaşır" diye ayarlanır; wipe'lar boss %5–20'deyken yoğunlaşır. Wipe ekranında
  boss canı büyük yazılır.
- **4v boss matematiği:** gerekli toplam DPS, 5 kişinin kusursuz oynayışının ~%80'i. 4 kusursuz kişi tam sınırda
  (teorik mümkün, pratik değil).
- **Deneme ekonomisi:** 6 dk + 30 sn → ~27 deneme / 3 saat. Kalahan ölçüsü 50–60 deneme = **boss başına 2–3
  akşam.** 1. akşam %30, 2. akşam %5, 3. akşam geçiş. Ertesi gün kancası bu eğriden çıkar; ayrı "daily" sistemi
  gerekmez. Risk: içerik açlığı (→ A-13.8).
- Boss katmanlı öğrenilmeli: aynı boss 3 saatte 3–4 kez "yeni" gelmeli (kademe/değiştirici).

## 6. Tehdit modeli [AÇIK A-13.2]

"Tank dodge atamadı → wipe" hikâyesi boss'un hedef seçimine bağlı. Fiil tablosunda taunt yok.

| Seçenek | Ne | Karşı sav |
|---|---|---|
| (a) Klasik aggro | hasar + taunt sayacı | Taunt tek rüne bağlanır, build özgürlüğünü kırar; sayaç görünmez, wipe adresi bulanır |
| (b) Yakınlık | boss en yakına vurur | Eşit konumlu masada "en yakın" sürekli değişir, boss döner durur |
| (c) Senaryolu hedef (Dragon Nest) | her mekaniğin hedef kuralı yazılı ("Kuyruk: en yakına. Ağ: en uzağa. Şarj: rastgele hasarcıya") | Boss makine gibi hisseder; 50 denemede ezber tamamlanır |

**[Ö] (c).** Gerekçe: "neden öldüğümüzü biliyorduk" ancak hedef kuralı yazılıysa mümkün; "rol seçimdir" ancak tank
bir rüne değil **konum ve zamanlama sorumluluğuna** bağlıysa mümkün. Tank = o mekaniğin geldiği yerde durmayı
üstlenen kişi; kural kitabında "tank" kişi değil görev. Ezber riskine panzehir: dizilim sabit, hedef seçimi kural
içinde rastgele ("rastgele hasarcıya") → "şarj bende!" masa konuşması. Sonuç: her mekanik satırında **hedef kuralı
sütunu zorunlu**; Kontrol'de taunt kolu gerekmez.

K'nın olası itirazı (ciddiye alınır): Kalahan hem ezberlenebilirdi hem hâlâ korkutuyordu; ikisi nasıl birlikte
durur. Sonraki tasarım sorusu bu.

## 7. Rün sistemi ve çizim

- **[K] Gramer:** 2'li = bileşik rün (fiil + sıfat). 3'lü = bileşik fiil + sıfat. 4'lü = bileşik fiil + bileşik
  sıfat (ilk iki rün bileşik rün, ikinci iki rün bileşik rün). 144 hücre elle tasarlandı; motor kurallardan
  üretiyor (eğitim verisi 144).
- **[K]** Sayılar: 6 rün × 2 silah → 72 ikili; 4'lüye çıkınca 200.000+ seçenek. Soru: bu kadar seçenek nasıl
  yedirilir.
- **[Ö] Üç alan, üç sınır:**

| Alan | Büyüklük | Kim taşır |
|---|---|---|
| Tasarım (tüm havuz, 4'lü) | 200.000+ | motor + denge; oyuncu görmez |
| Seçim (6 rün + silahla çizilebilen) | ~3.000 | lobide keşif |
| Dövüş (1 sn telegraf altında gerçekten çizilen) | **5–8 / oyuncu / dövüş**; fazlarla 10–12 | oyuncunun kası |

  Sınır oyuncu başına, dövüş başına. Masa 5 × 8 = 40 kombo aynı denemede döner; derinlik masanın toplamında ve
  dövüşten dövüşe rotasyonda aranır. Cevap: 1 milyar skill koysan da bir oyuncuya bir dövüşte 25 kullandıramazsın
  (türlerden türetildi, ölçülmedi).
- **[Ö] Tahmin edilebilirlik testi (asıl parametre):** parçalarını bilen oyuncu, hiç çizmediği kombonun ne
  yapacağını çizmeden söyleyebilmeli. Motorun ürettiği her skill bu teste tabi; "yasal ama şaşırtıcı" sonuç kimlik
  değil gürültüdür. Sıfat kolları tek bir basit birleşme kuralıyla birleşir; istisna eklendikçe test düşer.
  Otomatik doğrulama: her üretilen skill üç wipe sınıfından tam birine düşmeli, adı parçalarından okunmalı,
  toparlanma sınıfı tanımlı olmalı.
- **[Ö] Çizim uzunluğu = maliyet:** girdi süresi cast süresidir, ayrı cast bar gerekmez. Dokunuş = düz vuruş;
  2'li = gündelik; 3'lü = büyük CD, güvenli pencere; 4'lü = ulti, planlı an. 4'lü yalnız senin 6 rününden çizilir →
  build'in **münhasır imzası**; işi skill nüfusunu artırmak değil.
- **[Ö]** 4'lü tek skill, tek toparlanma (zincir sayılmaz; zincir hissi silah değişiminden gelir).
- **[Ö] Yanlış çizim politikası:** aynı çizim her zaman aynı skill; yuvarlama/tahmin yok (tek atma mekaniğinde
  yanlış skill ölümdür). Yanlış çizim boşa gider, ucuz bedel, net geri bildirim (titreşim + altıgen kızarır).
  Çizim kalitesi **ikili** (geçerli/geçersiz), güce etkisi yok. Köşe toleransı geniş.
- **[Ö]** Canlı önizleme: parmak gezerken o ana kadarki çizimin skill adı yazar (bileşik ad okunur, 200.000 isim
  ezberlenmez).
- **[Ö]** Lobide çalışma kuklası + kombo defteri (çiz, gör, isimlendir, kaydet). Kaydedilen kombolar dövüşte
  altıgende hayalet yol. Bir boss bir oyuncudan 5–8'den fazla farklı kombo istememeli.
- **[Ö]** İlerleme yok ama bilgi var: **boss müfredatı** (1. boss 2'liyle geçilir, 2. boss bir mekaniğinde 3'lü
  ister; kademe oyuncuya değil boss'a) ve **kodeks** (keşfedilen kombolar; güç yok, koleksiyon var).
- **[K→Ö] Yapısal / işlevsel:** K build düzeyinde ayrım önerdi (klasik buildler vs dünyayı değiştiren, üretim
  yapan, zamanı büken, arkadaş ışınlayan). Ö: etiket **rünün** olsun, build'in değil; build'in eksendeki yeri 6
  seçimden çıkar. İşlevsel uç: Zarar, Şifa, Yoğun, Odaklı, Güçlendirme. Yapısal uç: Çağırma, Sabit, İşaretli,
  Ortak, Tetikli, Yörüngeli, Zaman. → A-13.3.
- **[Ö] "Ne bulursa" listesi:** hedef kuralı "canlı, yapı ya da boş nokta; orada ne bulursa ona işler" yarı yolda.
  Skillerin ürettiği nesneler listeye girerse satranç doğar:

| Dünya nesnesi | Üreten | Başkaları onu ne yapabilir (→ A-13.4) |
|---|---|---|
| Çağrılan varlık (canı var, K 21:12) | Çağırma | Şifa iyileştirir mi? Kuvvet iter mi? Yansıma konur mu? Hareket-İşaretli ona ışınlar mı? |
| İşaret | İşaretli | gidilecek yer, hedef, Ortak yükünün adresi |
| Sabit şey | Sabit | Kuvvet iter, Sıçrayan seker, Yörüngeli döner |
| Zaman kaydı | Zaman | dost için alınır mı; Ortak ile dostun kaydına dönülür mü |
| Yük (dostun sonraki skilli) | Ortak | dostun skilli senin sıfatını mı alır |

  Örnek (Ö): build 4-7-9-11-12: Çağırma-Sabit piyon, Çağırma-Yörüngeli dönen parça, Çağırma-Tetikli tuzak,
  Zaman-Ortak dostun sonraki skillini geri alma. Neredeyse hiç vurmaz, tahta kurar.

## 8. Yetenek katmanı

- **[K]** Zekâ ve strateji katmanı yetmez; **yetenek** de istiyor: reaksiyon hızı, parmak hızı, "o çizimi yapmak
  zor ama yaptı ve çok OP bir şey oldu" hissi. Mortal Kombat yetenek ister mi, biz nasıl entegre ederiz.
- **[Ö] Beş kas** ve mobildeki durumu:

| Kas | Örnek | Mobil |
|---|---|---|
| Reaksiyon | Super Hexagon, FPS | en zayıf: dokunma→ekran 60–120 ms, başparmak ekranı kapatır |
| Zamanlama / öngörü | ritim oyunları, Sekiro deflect | **en güçlü** |
| İcra | MK/SF girdileri, Tekken just-frame | orta; çizim tam bu |
| Nişan | LoL skillshot, Brawl Stars | iyi |
| Dikkat bölme | StarCraft, raid | iyi, tavan düşük |

  Karar önerisi: eksen **zamanlama + icra + nişan + takım senkronu**; ham reaksiyon yok (mobilde adaletsiz,
  wipe adresini "gecikme yedik"e bulandırır). Yetenek öğrenilebilir olmalı, refleks değil.
- **[Ö] Girdi çizim kalır;** üçüncü girdi türü (eğim, akor, flick) eklenmez. Üzerine iki zamanlama katmanı:
  - **Koruma = parry.** Tablo zaten "kısa savunma duruşu, kalkan yok" diyor. Tek atma mekaniğini tam zamanında
    Koruma ile karşılamak boss'u sersemletir: yüksek risk (kaçırırsan ölürsün), büyük ödül (masanın burst
    penceresi), görünür (4 kişi görür). **Hiçbir mekanik parry zorunlu kılmaz, hepsi parry'ye açık** (Souls).
  - **Tetikli = just-frame.** Çizimi bitirip parmağı tutmak, boss'un vuruş karesinde bırakmak → farklı/ikiye
    katlanan etki. Pencere dar ama öngörülebilir (boss animasyonu sabit). Başarı flaş + titreşimle işaretlenir.
  - **Çizim hızı = cast süresi.** Hız zamana dönüşür, **güce değil** ("titreme meta" olmasın).
  - **Nişan:** basılı tut-kaydır zaten var; İşaretli/Sabit/Sıçrayan ile konum hassasiyeti.
  - **Takım zamanlaması:** Ortak ve sersemleme eşiği (5 kişi aynı 3 saniyede yüklenir) = masa yeteneği;
    kahvehane için en uygun tür ("üç, iki, bir").
- **[Ö] "Zor çizdi ve OP oldu" dört şartı:** zorluk görünür; ödül ayrık ve büyük (durum değişikliği, %15 değil);
  bedel gerçek; seyrek (dövüş başına 1–3 fırsat). Boss tablosuna girer: kaç parry penceresi, kaç just-frame anı,
  kaç 4'lü fırsatı.
- **[Ö] Adalet paketi:** girdi tamponu (pencereden önce başlayan çizim pencere açılınca işlenir); köşe toleransı;
  zamanlama pencereleri cihaz dokunma gecikmesinin ≥ 2 katı (ölçüm, M-13.2); parry/dodge kararı cihazda, host
  toleranslı; dodge coyote time.
- **[Ö]** Ölçülen yetenek deneme kartına girer: ortalama çizim süresi, parry sayısı/başarı, just-frame, 4'lü
  sayısı, dodge zamanlaması.

## 9. Zaman ekonomisi: toparlanma, iptal, kombo

- **[K]** LoL'de üst seviye oyuncu her kaynağı değerlendirir (Yunara Q/R oto atak sıfırlama). Bizde oto atak
  sıfırlama / animasyon iptali gibi bir şey gerekli mi, yük mü. Aynı build'le giren iki arkadaş arasında "neden
  sen çok vurdun" farkı bu ayrıntılardan çıkmalı.
- **[K]** İki silah animasyon iptali imkânı verir: skill 1 sn sürecekken silah değiştirip diğer silahla başka
  skille geçmek; kombo hissi, ara vermeden hasar.
- **[K]** Kombo: ara vermeden vurmak çok zor olmalı; boss telegrafına rağmen **milyonda bir, hiç komboyu kırmadan
  boss'u kesen** bir yetenek çıkabilmeli.
- **[Ö] Teşhis:** usta gizli tuş bilmiyor, **zamanı kaynak olarak görüyor**; DPS'in paydasını küçültüyor. İptal =
  çizelgeleme problemi.
- **[Ö] Skill üç fazlı:** hazırlık, etki, toparlanma. Kurallar (kâğıt):
  - Hazırlık yalnız dodge ile kesilir; dodge hakkı yanar, CD yanmaz (bedava kandırmaca olmasın).
  - Etki kesilmez.
  - Toparlanma kesilebilir; öncelik **dodge > silah değişimi > düz vuruş sıfırlama.**
  - Etki geçtiyse CD yanar.
  - Silah değişiminin kendi kısa toparlanması var ve iptal edilemez (sonsuz zincir olmasın); iki silahın CD
    havuzu ayrı (dönüşümlü oynamaya iter).
  - Düz vuruş sıfırlama (weave): kombo bırakıldığı an düz vuruş sayacı sıfırlanır; bedel yok, ödül küçük ve
    sürekli; acemi atlar, usta hep yapar. Sayaç görünür (küçük halka).
  - Girdi tamponu var. İptal kararı cihazda, host toleranslı.
  - Sayılar (ölçüm, M-13.4): üç toparlanma süresi (kısa/orta/uzun = 2/3/4'lü), silah değişimi CD'si, tampon
    penceresi, düz vuruş kadansı.
- **[Ö] Kombo kademesi (D–C–B–A–S):** vurulmak kırar; dodge kırmaz (dodge sonrası kısa pencerede vuruş gelirse
  zincir sürer); parry zinciri büyütür; çizim süresi ve toparlanma kırmaz; yalnız "N sn hasar yok" kırar. Ödül:
  **gösterge büyük** (masa görür, "S ile bitirdi"), **güç küçük ve tavanlı.** Tek atma mekanikleri stat'a kapalı.
- **[Ö] Tavan/taban kuralı:** iptaller ve kombo **tavanı yükseltir, tabanı değil.** DPS kontrolü (enrage) hiç
  iptal yapmayan takıma göre ayarlanır; ustalar boss'u erken keser. Aksi halde wipe adresi "swap cancel
  bilmiyorsun" olur; bu adres değil, dışlamadır.
- **[Ö] İptal politikası:** her iptal kural kitabında ya vardır ya kapatılır. Üretken motor kazara iptal
  doğuracaktır; "keşfedildi ve eğlenceli" olanlar açıkça terfi ettirilir, sessizce bırakılmaz. Motor "toparlanma
  yok" skill üretirse o skill her şeyi iptal eden araca döner → doğrulama testine girer.
- **[Ö] Ölçülen kaynaklar (deneme kartı):**

| Kaynak | Usta ne yapar | Ölçü |
|---|---|---|
| Zaman | hızlı çizer, toparlanmayı iptalle satar | aktiflik %, iptal sayısı |
| CD'ler | hiçbir CD dolu beklemez | CD'nin dolu durduğu saniye |
| Boss pencereleri | sersemlemeye en pahalı 4'lüyü sığdırır | pencere başına hasar |
| Dodge hakkı (2 / 6 sn) | yalnız gerektiğinde, ikincisini saklar | boşa dodge |
| Takım kaynağı (Ortak, Zaman kaydı, Güçlendirme) | dostun buff penceresine burst denk getirir | buff altı hasar oranı |

## 10. Masa içi rekabet ve ertesi gün (Ö)

- Deneme sonu istatistik kartı: hasar, kaldırma, dodge, kaç kez düştü + 8/9'daki ölçüler. Sabit arkadaş grubunda
  istatistik toksik değil, laf atma hammaddesi; bu, kitle kararını netleştirir.
- Akşam çetelesi; seans sonu tek hüküm: gecenin MVP'si ve **çayı kim ısmarlıyor.**
- Ertesi günü istetecek dört şey (en az ikisi her akşam): yarım kalan iş (%5 wipe), denenmemiş oyuncak (son 15
  dakikada düşen rün/kombinasyon), ortak randevu (günlük sıfırlama **sabah erken**, "yarın akşam" gerçek yeni gün
  olsun), görünür ustalaşma (masa tablosu, yabancı liderlik tablosu değil).
- İlerleme yatay (çeşitlilik); para ile güç farkı kesinlikle yok (K: gelişim yok).
- Wipe ekranında "çizim hatası" ile "skill işe yaramadı" ayrı sayılır; "Tank, dodge geç" ile "Tank, çizim hatası"
  farklı dersler.

## 11. Reddedilenler

| Ne | Kim | Neden |
|---|---|---|
| Mükemmel dodge | **[K]** | prototip kalıntısı; dodge dodge'dur |
| Dikey güç ilerlemesi | **[K]** | gelişim yok; bir akşam gelmeyen geride kalır |
| Enerji/stamina tavanı | Ö | 3 saatlik seansı öldürür |
| Build düzeyinde yapısal/işlevsel ayrımı | Ö (K kararı bekliyor, A-13.3) | "ilginç" buildler vitrin olur, Kalahan'a getirilmez |
| Gizli iptal / bug-özellik | Ö | 200.000 skill içinde gerçek/bug ayrımı yapılamaz |
| Çizim kalitesinin güce etkisi | Ö | okunabilirliği öldürür; ikili karar |
| Taunt rünü / görünmez aggro sayacı | Ö (A-13.2'ye bağlı) | tek rüne bağımlılık; wipe adresi bulanır |
| Üçüncü girdi türü (eğim, akor, flick) | Ö | yük artar, keskinlik azalır; kahvehanede eğim olmaz |
| Checkpoint | Ö | risk tek parça; erken fazlar kısa tutulur |
| Ham reaksiyon isteyen (telegrafsız) mekanik | Ö | mobil gecikme; adaletsiz hissedilir |
| 4'lü komboyu iki skill zinciri saymak | Ö | arada iptal noktası doğar; tek skill tek toparlanma |

## 12. Açık sorular

Kullanıcı kararı (kâğıt sınıfı):

| # | Soru | Ö'nün durduğu yer |
|---|---|---|
| A-13.1 | Tek atma mekanikleri kalkan ve şifayı deler mi? | Evet (Dragon Nest). Aksi halde "Koruma spam" tek meta |
| A-13.2 | Tehdit modeli (bkz. 6) | (c) senaryolu hedef; "tank" = görev |
| A-13.3 | Yapısal/işlevsel etiketi rün mü build mi? | rün |
| A-13.4 | "Ne bulursa" matrisi: 5 dünya nesnesi × 12 fiil | doldurulacak; boş hücre = bulunmamış eksik |
| A-13.5 | Bileşik sıfat anlamı: (fiil+sıfat) sıfat olarak nasıl davranır? (ör. Çağırma-Sabit sıfat olarak) | — |
| A-13.6 | Zaman fiili hangi wipe sınıfına cevap verir? Telegrafı uzatıyorsa (a) ve en sosyal fiil; yalnız geri sarıyorsa (b) | — |
| A-13.7 | Rastgelelik dozu | dizilim sabit, hedef seçimi kural içinde rastgele; son fazda bir varyant |
| A-13.8 | İçerik açlığı: boss/2–3 akşam → ayda 10–12 boss. Üretim hızı nasıl? | hard versiyon, yapı kısıtlı koşular, süre tablosu; yine de ana risk |
| A-13.9 | Parti 5 sabit mi, 2–5 ölçekli mi? | 5'e tasarla, aşağı ölçekle |
| A-13.10 | Ağ topolojisi: yerel/hotspot mu, yalnız internet mi? | aynı masa → yerel öncelikli |
| A-13.11 | Hareket'in yalnız Saldırı'da olması bilinçli mi? (tank/destek konum aracı yalnız dodge) | bilinçliyse tasarıma yazılsın |
| A-13.12 | Destek otomatik hedefi "en yakın dost" mu "en çok hasar alan" mı? | Odaklı-Yansıma'daki kol Şifa'da da olsun; sürükleme istisna |
| A-13.13 | Mükemmel dodge hangi belgelerden/kodlardan silinecek? | README + kural kitabı dodge bölümü + `10-olculer.md` dodge maddeleri; liste çıkarılacak |
| A-13.14 | Kalahan hem ezberlenebilir hem korkutucuydu; ikisi birlikte nasıl durur? | — |

Oyunda ölçüm (başlangıç tahmini yazılır, oynanarak bulunur):

| # | Ölçü | Başlangıç tahmini | Nasıl |
|---|---|---|---|
| M-13.1 | 2/3/4 rünlük çizim süresi ve hata oranı | — | kahvehane, terli başparmak, 5 kişi, gerçek cihaz |
| M-13.2 | Cihaz dokunma→ekran gecikmesi | 60–120 ms (genel Android) | ölçüm aracı |
| M-13.3 | Parry / just-frame penceresi | ≥ 2 × M-13.2, 200 ms altına inilmez | oynanarak |
| M-13.4 | Toparlanma süreleri (3 sınıf), silah değişimi CD, tampon, düz vuruş kadansı | — | oynanarak |
| M-13.5 | Deneme süresi, kazanma oranı (%40–60 hedef), wipe→başla süresi | 5–7 dk / ≤ 20 sn | oynanarak |
| M-13.6 | Pil / saat | ≤ %25–30 | 3 saat seans |
| M-13.7 | Boss %5–20 arası wipe yoğunluğu (planlı %5 çalışıyor mu) | — | wipe kayıtları |

## 13. Park (dilim dışı; kaybolmasın)

- Kaydedilen kombolar altıgende hayalet yol.
- Lobide çalışma kuklası, kombo defteri, isimlendirme ve paylaşma.
- Kodeks (koleksiyon, güç yok).
- Boss "hard" versiyonu; yapı kısıtlı koşular ("healersız"); süre tablosu.
- Düz vuruş halkası.
- Çayı kim ısmarlıyor hükmü; akşam çetelesi; MVP.
- Günlük sıfırlama sabah erken.
- Boss hedef seçiminde "tank = boss'un gideceği yerde duran kişi".
- Kâğıt üstünde boss masa oyunu: 3 faz, mekanikler, roller; bir akşam arkadaşlarla, kod yok. "Neden öldük" cümlesi
  masada kurulabiliyor mu, "az kalmıştı" hissi çıkıyor mu.
- 5v5.
- Lore, animasyon, efekt: dilim sonrası (telegraf okunabilirliği hariç; o oynanış).
- Geçiş anı kutlaması (ekran görüntüsü, tek seferlik); bir sonraki hedef aynı gece görünür.
