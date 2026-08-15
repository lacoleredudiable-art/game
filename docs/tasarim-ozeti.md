# Tasarım Özeti

> Bu belge, projenin ilk tasarım konuşmasının çıktısıdır. Amacı, yeni bir sohbetin
> sıfırdan bağlam kurmadan devam edebilmesini sağlamaktır.
> Tarih: 15 Ağustos 2026 · Durum: tasarım aşaması, henüz kod yok

---

## 1. Proje Nedir

Mobil, çok oyunculu, **aksiyon tabanlı kooperatif boss dövüşü** oyunu.
Referans noktası: **Dragon Nest** (Türkiye'de "Ejder Yuvası" adıyla yayınlanan mobil sürüm).

**Amaç:** Ticari başarı değil. 20-30 kişilik bir arkadaş grubunun aylarca oynayacağı,
teknik ve görsel olarak gerçekten etkileyici bir oyun yapmak. Kişisel hedef: "zor bir şey
yapabildiğini görmek."

**Bunun getirdiği özgürlükler** (tasarımın temelini bunlar belirliyor):

- Para kazanma kaygısı yok → beceriye dayalı tasarım serbest, P2W mekaniği yok
- İçerik hacmi baskısı yok → 4-6 boss aylarca yeter
- Geniş kitle kaygısı yok → oyun acımasız derecede zor olabilir
- Hile kaygısı düşük → ağ mimarisi radikal biçimde basitleşebilir

---

## 2. Terk Edilen İlk Fikir (ve Nedeni)

İlk plan, Rise of Kingdoms tarzı bir makro strateji katmanı + 3D hack&slash mikro katman
hibritiydi. **Bu yön terk edildi.** Gerekçeler:

- RoK'un ekonomisi (timer, grind, hızlandırıcı) **para kazanmak için** vardır. Monetizasyon
  yoksa geriye sadece bekleme kalır.
- Makro/MMO katmanı işin ~%80'ini yer, en az etkileyici sonucu üretir. "Wow" tepkisi
  kaynak sayacında değil, dövüşte.
- Projenin sahibi asıl olarak dövüş tarafına tutkulu; strateji katmanı referanstan miras
  kalmış bir iskeleydi.

**Karar: piramit ters çevrildi.** Aksiyon dövüşü oyunun kendisi, sosyal/meta katman onun
etrafında ince bir kabuk.

---

## 3. Referans Analizi: Dragon Nest Neden Özeldi

- **Non-targeting dövüş:** hedef kilitleme yok, gerçek dodge, i-frame'li yuvarlanma,
  havada combo, fizik tepkili düşmanlar
- **Instance'lı yapı:** açık dünya değil; kasaba hub + 4-8 kişilik zindanlar. Yani teknik
  olarak "MMO kostümü giymiş lobi oyunu" — mimarinin en zor kısmı zaten yok
- **Beceri > ekipman:** düşük seviyeli iyi oyuncu, yüksek seviyeli kötü oyuncuyu yenebiliyordu
- **Ölçek:** Çin'de ilk ay 700.000 eşzamanlı oyuncu, 3 yıl üst üste Golden Plume ödülü,
  iki adet CG sinema filmi. Batı'da bilinmemesi coğrafi/yayıncılık kaynaklı

**Neden bir daha yapılmadı?** Sebep mühendislik değil, **ticaret**: beceriye dayalı oyunda
güç satılamaz, ve elle yazılan boss içeriği ölçeklenmez. Bu iki sebep de bu projede geçersiz.
Yani stüdyolar için ticari intihar olan tasarım, bu proje için erişilebilir hedef.

### Anahtar sosyal gözlem

Ejder Yuvası'nda parayı basmış "güçlü adamlar" bossu geçemiyor, ortalama ekipmanlı ama iyi
oynayan bir oyuncu geçiyordu — ve herkes onu çağırıyordu.

> **Zamana/paraya dayalı üstünlük hınç doğurur, beceriye dayalı üstünlük hayranlık doğurur.**

30 kişilik grupta oyunu aylarca yaşatacak şey, aranızdan bir efsane çıkmasıdır. Bu
tasarlanabilir bir şeydir (bkz. §7).

---

## 4. Üç Tasarım Sütunu

Bu üçü oyunun kalbi. Hepsi eşit derecede önemli, birbirlerini besliyorlar.

### Sütun 1 — Boss bir bilmecedir

Boss deseni oynadıkça, ölerek öğrenilir. Gecelerce uğraşılır.

**Pazarlıksız iki şart:**

1. **Her ölüm adil olmalı.** Oyuncu neden öldüğünü anlamalı. Okunabilir telegraflar
   (ses + ışık + animasyonun hazırlık evresi). Rastgele tek vuruşta öldürme yok.
2. **Tekrar denemek neredeyse bedava olmalı.** Ölümden sonra 10-15 saniyede tekrar dövüşte.
   Yükleme ekranı, uzun yürüyüş, ekipman tazeleme yok. "Gecelerce uğraşmak" ancak deneme
   maliyeti sıfıra yakınsa eğlencedir — Souls'un ve Dragon Nest'in asıl sırrı budur.

### Sütun 2 — Beş kişinin uyumu

Boss mekanikleri **tek başına çözülemez** olmalı: eşzamanlı iki noktada durma, tutan/arkaya
geçen ayrımı, debuff temizleme. Roller gerçekten farklı olmalı (tank / şifa / debuff / hasar).

**Bedeli:** her sınıf ayrı bir tam dövüş sistemi demek (kendi frame verisi, animasyonu,
dengesi). 5 sınıf = 5 kat iş.
**Karar: 2-3 sınıfla başla**, ama rolleri net ayrık olsun. İki kişi aynı sınıfı oynayabilir.

### Sütun 3 — Dodge, ve kendi yeteneğinle dodge'lamak

En derin sütun. "Yeteneğimle dodge'ladım" demek, **her yeteneğin frame verisi var** demek:
hazırlık süresi, aktif kareler, toparlanma, dokunulmazlık penceresi, iptal pencereleri.

Bu, dövüş oyunu seviyesinde bir dövüş sistemi. Yetenek listesi "hasar butonları" değil,
bir **hareket ve savunma dili** olur; ustalık, hangi yeteneği hangi anda kaçış olarak
kullanacağını bilmektir.

> **Projenin aradığı "zor iş" budur.** Backend değil, envanter senkronizasyonu değil.
> Ve güzel tarafı: tek kişilik bir sahnede, tek düşmanla, hiç network olmadan
> geliştirilip mükemmelleştirilebilir. En değerli iş, en izole edilebilir iş.

---

## 5. Yaratıcı Build Sistemi

**Hedef:** oyuncunun kendi karakterini/yeteneklerini kurması. "İki şeyi birleştirdim ve
güçlü bir şey çıktı" hissi.

**Çözülmesi gereken klasik hastalık:** "10 skill var ama 5'i işe yarıyor." Üç sebebi var:

1. Yetenekler tek eksende (hasar) kıyaslanıyor → matematiksel olarak hep bir en iyisi olur.
   **Çözüm:** yetenekleri derece olarak değil **tür** olarak farklılaştır (biri taşır, biri
   havalandırır, biri durum uygular, biri arenayı değiştirir). Kıyaslanamayan şey domine edilemez.
2. Bütün savaşlar aynı problemi soruyor → tek cevap yeter.
   **Çözüm:** bosslar farklı araçlar zorunlu kılsın.
3. Tasarımcı comboları elle yazıyor → oyuncunun keşfedeceği hiçbir şey yok.

### Mimari: yetenek yazma, alfabe yaz

Üç katman:

- **Taşıyıcı** — etki nasıl ulaşıyor? (mermi, koni, yere çakma AoE, içinden geçme atılması,
  kendine kalkan, tut-ve-fırlat)
- **Yük** — ulaşınca ne yapıyor? (hasar, zehir birikimi, havalandırma, çekme, zırh kırma, yavaşlatma)
- **Durumlar + etkileşim tablosu** — asıl sihir burada. Islak / yanıyor / zehirli / sersem /
  havada / zırhı kırık. Ve kurallar: zehir+ateş = patlama, ıslak+yıldırım = zincir,
  havadaki hedef +%40 hasar alır, zırhı kırık düşman fırlatılabilir.

Oyuncunun bulduğu kombinasyonu **kimse elle tasarlamamıştır** — etkileşim tablosundan doğar.
Yaratıcılık hissi buradan gelir, yetenek sayısından değil.

**Ve bu yol daha ucuz:** 8 durum + kurallar, elle yazılmış 40 yetenekten kat kat az iş.
İlk plandaki "3-4 temel animasyon + kodla eklenen VFX ile çeşitlilik" fikri bu mimarinin
zorunlu sonucu — ikisi aynı şey. Silah, animasyon setini ve taşıyıcıyı belirler.

**Bir yeteneğin dokunulmazlık penceresi de bir "yük" sayılır** → build kurmak, kendi savunma
dilini tasarlamak demek. Aynı bossa iki oyuncu bambaşka girer.

**İncelenecek oyunlar:** Noita (asa kurma), Magicka, Path of Exile (gem sistemi),
Divinity OS2 (element yüzeyleri), Risk of Rain 2, Genshin (tepkime geri bildirimi).

**İki tuzak:**

- **Fazla özgürlük felç eder.** Küçük alfabe, vahşi etkileşim (Noita'nın dehası bu).
  Başlangıç: ~6 taşıyıcı, ~8 yük, birkaç değiştirici.
- **Sistem okunabilir olmalı.** Durumlar ikonla görünsün, tepkime tetiklendiğinde ekranda
  patlasın. Oyuncu alfabeyi öğrenmezse deney yapmaz.

**Kırık build = hata değil, oyunun en iyi içeriği.** Stüdyolar bunu PvP ve para yüzünden
budar; burada budamaya gerek yok. (İstisna: beceri ligi PvP eklenirse orada kısıtlı set kullanılır.)

---

## 6. Epik Dövüş Hissi

**Hedef dil:** Solo Leveling'in Ant King dövüşü, Demon Slayer'ın renkli efektleri.
Oynayana "noluyoo" dedirtmeli.

### Temel gerçek: epiklik %80 zamanlama ve kamera, %20 sanat varlığı

**Bedava olanlar (hepsi kod, performans maliyeti ~sıfır — mobil için kritik):**

| Teknik | Ne yapar |
|---|---|
| **Hitstop** | Vuruşta 2-6 kare dondurma. Tek başına en etkili şey |
| **Impact frame** | Çarpma anında tek karelik beyaz/ters renk patlaması (gerçek anime tekniği) |
| **Kamera yumruğu** | Kısa FOV sıçraması + rotasyon + sarsıntı |
| **Hız rampası** | Savurmayı hızlandır, çarpmayı yavaşlat |
| **Anticipation / follow-through** | Vuruş öncesi gerilme, sonrası savrulma. "Ağır ve yıkıcı" hissinin kaynağı |
| **Vuruş sonrası sessizlik** | Boşluk, gürültüden vurucudur |
| **Afterimage / smear** | Hızlı hareketlerde hayalet kopyalar |

**Ses, o hissin yarısıdır.** Derin sub-bass + metalik çığlık + sessizlik.
**Düşman tepki vermeli** — sarsılmayan bossa vurmak dünyanın en kötü hissidir.

### Zenitsu kalıbı (en ucuz + en etkili anlatım)

Demon Slayer'da Zenitsu'nun ilk Yıldırım Nefesi sahnesi: **vücut ve kılıç neredeyse sabit.**
Tüm animasyon **iki pozdan** ibaret:

1. **Duruş ve gerilim** (0.5–1.5 sn) — tek pozda donma, toplanan efekt, yükselen ses,
   kamera yaklaşır, zaman yavaşlar
2. **Gidiş** (2–4 kare) — karakter yok olur; bulanıklık, afterimage, hız çizgileri, şerit
3. **Varış pozu** — diğer tarafta donmuş, sabit
4. **Gecikmiş sonuç** — sessizlik, sonra kesik belirir, sonra düşman yığılır

> "Noluyoo" hissi hareketten değil, **boşluktan** doğar. Beyin eksik kısmı doldurur ve
> hayal ettiği şey, çizilebilecek her şeyden etkileyicidir. (Japonca: *ma* — anlamlı duruş.)
> Anime bu tekniği animasyon pahalı olduğu için icat etti. Aynı kısıt burada da geçerli.

**Kalıbın üç faydası:** (a) çoğaltılabilir — aynı yapı, farklı palet ve ses = yeni ulti;
(b) build sistemine kilitlenir — "içinden geçme atılması" taşıyıcısının görsel ifadesi;
(c) uzun hazırlık = yüksek risk → ancak bossu okuyup açıklık bulduğunda kullanılır,
yani gösteri, en derin mekaniğin ödülü olur.

**Sınır:** bu bir **noktalama işareti**, cümlenin kendisi değil. Her saldırı böyle olursa
lapaya döner. Temel saldırılar gerçek animasyon ve anında tepki ister. Ne kadar nadirse o kadar büyük.

### Efekt üretimi ve iki uyarı

VFX sıfırdan yapılmayacak, Asset Store'dan alınacak. Aranacak isimler: **Hovl Studio**,
**Gabriel Aguiar**, **Kripto289**. Kendi yazılacak kısım: mesh trail'ler (kılıç yayları).

⚠️ **Mobilde şeffaflık katildir.** Anime VFX = büyük yarı saydam katmanlar = overdraw =
mobil GPU ölümü. Az ama büyük parçacık, billboard yerine mesh, tam ekran saydam katmandan
kaçınma, flipbook atlas, sıkı parçacık limiti. Epiklik bütçesini parçacığa değil zamanlamaya harca.

⚠️ **Epiklik ile okunabilirlik birbirini yer.** Küçük telefon ekranında kendi efektin bossun
telegrafını yutarsa Sütun 1 çöker ve oyun adaletsiz hisseder.
**Kural: renk dili ayrılır.** Belirli bir renk (ör. kırmızı-turuncu) yalnızca düşman
tehdidine ayrılır, oyuncu efektlerinde asla kullanılmaz. Oyuncu efektleri farklı paletten
gelir ve hızla söner. Boss telegrafı her zaman en üst, en okunabilir katmanda.

---

## 7. Zorluk Felsefesi ve Efsane Üretimi

**Soru:** "10 yılda 3 kişinin yapabildiği kombo" gibi şeyler var mı, ve oyuncuyu soğutmaz mı?

**Var:** Geometry Dash extreme demon'ları (bazıları dünyada birkaç kişi tarafından geçildi),
osu! (Cookiezi'nin Blue Zenith FC'si), Daigo'nun 2004 parry'si, Mario 64 "yarım A basışı",
FFXIV Ultimate raid'leri (oyuncuların yalnızca %birkaçı bitirir), WoW Mythic world-first
(yüzlerce deneme).

**Soğutur — ama sadece zorunlu olduğunda.** Üç kural:

1. **İsteğe bağlı olacak.** FFXIV Ultimate'ı %97 oyuncuyu soğutmaz çünkü girmek zorunda değiller.
2. **Varlığı görünür olacak.** Geçemeyen de faydalanır: izler, imrenir, geçene saygı duyar.
   Fayda geçmekten değil, var olduğunu bilmekten gelir.
3. **Uçurum değil merdiven olacak.** Kademeli zorluk (Normal → Savage → Ultimate).

**Ve:** zorluk **icra hassasiyetinden** gelmeli; gizemden veya şanstan değil.
"Zor ama öğrenilebilir" motive eder, "zor çünkü rastgele" nefret üretir.

### Efsaneyi tasarlamak

30 kişilik grupta "dünyada 3 kişi yaptı" ile "bizden sadece Ahmet yapabiliyor" psikolojik
olarak aynı şeydir. Üç şart:

- **Beceri görünür olmalı:** temizleme süresi sıralaması, hasarsız bitirme rozeti, en yüksek
  combo, ve en önemlisi **tekrar kaydı (replay)** — grup sohbetine atılan kayıt, oyunun
  kendisinden daha eğlencelidir
- **Carry mümkün ama bedava olmamalı:** iyi oyuncu zayıf ekibi sırtlayabilmeli (yoksa
  çağrılmaz), ama diğer 4 kişi de bir şey yapmalı (yoksa seyirciye döner)
- **Beceri tavanı yüksek olmalı:** "yeterince iyi" ile "ustalık" arası mesafe büyük olmalı.
  Tavanı yükselten şey Sütun 3'tür → **dövüş derinliği doğrudan sosyal statü üretir**

> ⚠️ **En önemli kural:** Yukarıdaki efsanelerin hiçbiri **tasarlanmadı.** Hepsi derin bir
> sistemin içinden kendiliğinden çıktı. İş, "zor kombo tasarlamak" değil; sistemi yeterince
> derin kurmak. Sistem derinse imkânsız kombo zaten doğar. Yapay zorluk eklemek hem sığ
> hem sinir bozucu olur.

---

## 8. Teknik Mimari Kararları

### Ağ (Network)

- ❌ **Netcode for GameObjects'e gerek yok** (ilk plandaki karar iptal). NGO açık dünya /
  ağır senkron için. Burada instance'lı, küçük oturumlar var.
- ✅ **Hile kaygısı yok (30 arkadaş, PvE)** → **oyuncuya kendi hareketi ve kendi i-frame'i
  üzerinde tam yetki verilir.** Boss ve düşmanlar host'ta simüle edilir.
  Bu tek karar, aksiyon netcode'unun en zor problemini (dodge doğrulaması) tamamen ortadan
  kaldırır ve 200ms gecikmede bile dodge'un tereyağı gibi hissetmesini sağlar.
  Dragon Nest de kabaca bunu yapıyordu.
- Kasaba/hub senkronizasyonu ucuzdur (kimse dövüşmüyor, 300ms fark etmez).

### Backend

- **C# (ASP.NET Core + PostgreSQL)** önerisi: ekonomi/hasar matematiği tek bir shared
  assembly'de olur, hem sunucuda otorite hem Unity'de önizleme olarak **aynı kod** kullanılır.
  Başka dil seçilirse tüm matematik iki kez yazılır — 3 kişilik ekipte pahalı hata.
- 30 kişi için tek küçük VPS (aylık birkaç euro) fazlasıyla yeter.
- Giriş: davet kodu + cihaz kimliği. Hesap sistemi gereksiz.
- **Sürekli çalışan simülasyon olmasın:** olay tabanlı + tembel değerlendirme
  ("son hesaplama zamanı"ndan farkı çarp).

### 🔴 En kritik mimari kural

> **Ayarlanabilir olan her şey kod değil, VERİ olacak.** (ScriptableObject / config asset)

Hitstop süresi, i-frame penceresi, dodge mesafesi, boss telegraf süresi, kamera sarsıntı
şiddeti — hepsi Unity inspector'dan tıklanıp değiştirilebilmeli.

**Sebep:** dövüş hissi ~500 küçük ayar denemesiyle bulunur. Her deneme kod değişikliği +
derleme gerektiriyorsa 50 tane yapılır ve bırakılır; oyun hiçbir zaman doğru hissetmez.
**Zevki olan kişi, programcıya ihtiyaç duymadan deneme yapabilmeli.**

### Motor

Unity 6. Animasyonlar sıfırdan yapılmayacak (Asset Store humanoid + hack&slash paketleri).
AI ile 3D üretim binalar/dekor için uygun, **animasyonlu karakter için değil** — Ant King
hazır rig'lenmiş bir modelden gelmeli.

---

## 9. Ekip ve İş Bölümü

| Kişi | Rol |
|---|---|
| Proje sahibi | Tasarım, oyun hissi, ayar, içerik, yön. Kod yazmıyor; Unity'yi az çok biliyor, öğrenecek |
| Yazılım mühendisi | Mimari ve geri dönüşü pahalı olan sistemler |
| 3. kişi | Henüz tanımsız |

**Mühendis sahiplenir:** dövüş çatısı mimarisi (frame verisi, durum makineleri), yetenek
kompozisyon sistemi, ağ katmanı, backend.
**Proje sahibi sahiplenir:** his, sayılar, boss desenleri, efekt zamanlaması, oyunun ne olduğu.

**Mühendise iletilecek iki uyarı:**

1. **Oyun kodu iş yazılımı değil.** Deneyimli mühendisler altyapıyı fazla mühendislik yapmaya
   meyilli (ECS, soyutlama katmanları, "önce networking'i sağlam kuralım") — sonuç: 6 ay
   geçer, ortada oyun yoktur. His katmanında hızlı deneme, temiz koddan değerlidir.
2. **Yazılım mühendisliği Unity'ye ~%70 aktarılır.** Aktarılmayan: kare bazlı düşünme,
   animasyon sistemleri, fizik tick vs render tick, mobil GPU kısıtları, oyun hissi.

**Not:** Proje sahibinin teknik okuryazarlık kazanması şart (kodu okuyabilmek, Unity'de
sayı değiştirip çalıştırabilmek, sorunu tarif edebilmek) — programcı olması değil.
Tahmini süre: yaparak öğrenerek 3-4 hafta. Ayrıca hedefin kendisi ("yapabildiğimi görmek")
bunu zaten gerektiriyor.

---

## 10. İlk Prototip (Hedef)

**Kapsam — greybox, hiçbir sanat varlığı yok:**

- Bir kapsül oyuncu, daha büyük bir kapsül boss
- Oyuncu: hareket + 3 vuruşluk temel saldırı zinciri + **i-frame'li dodge**
- Boss: 2 saldırı, ikisinde de okunabilir hazırlık evresi, en az biri dodge zorunlu
- His katmanı: hitstop, kamera sarsıntısı, vuruş parlaması, impact frame, derin çarpma sesi
- **Tüm ayarlar inspector'dan değiştirilebilir**
- **Telefona build alınıp gerçek dokunmatik ekranda oynanır** (sanal çubuk + 2 buton)

### Prototipin cevapladığı TEK soru

> **Telefonda, tam zamanında dodge atıp bossun saldırısını sıyırdığında sırtın ürperiyor mu?**

**Evet ise:** geri kalan her şey (build sistemi, boss çeşitliliği, 3D, co-op, backend)
uzun ama belirsizliği düşük bir yol.
**Hayır ise:** bunu 6. ayda değil 2. haftada öğrenmiş olursun; kontrol şeması veya platform
yeniden düşünülür.

**Kaba tahmin:** mühendisle beraber çatı 1-2 hafta (tam zamanlı değil), ardından 1-2 hafta
saf ayar (bu kısım tamamen proje sahibinin işi, elde telefon, tek başına).

**Önemli:** greybox aşaması "sıkıcı teknik hazırlık" değil — §6 gereği epiklik testinin
kendisi. Doğru hitstop'a ve sese sahip bir küp, bunları yapmayan güzel bir 3D modelden
daha epik hisseder.

---

## 11. Canlı Referans

**Dragon Nest M: Classic** şu anda mobilde yayında ve aktif güncelleniyor (orijinal Kore
ekibi Eyedentity). **Auto-battle yok**, tüm yetenekler elle, dodge oyuncunun sorumluluğunda,
nest bossları çok fazlı ve telegraf tabanlı. Haziran 2026'da 60 seviye güncellemesi geldi.

**Neden önemli:** "mobil dokunmatikte hassas dodge zor" endişesi çözülmüş bir problem.
İndirilip incelenebilir: kontrol şeması, telegraf süreleri, i-frame cömertliği, buton
yerleşimi **ölçülebilir**. Tasarım tartışmasını tahminden ölçüme çevirir.

---

## 12. Açık Konular

- [ ] **Tema:** karanlık biyolojik böcek/karınca dünyası (biopunk) kalsın mı? Ant King ana
      sınıf mı? Yuva = zindan, kraliçe = nest bossu kurgusu bu temaya iyi oturuyor
- [ ] **Sınıf yapısı:** hangi 2-3 sınıf, rolleri ne?
- [ ] **Zindan/ilerleme kurgusu:** hub nasıl, ilerleme neye bağlı, haftalık ritim var mı?
- [ ] **Ekipman mı build mi?** İlerleme hangisinden geliyor?
- [ ] **Beceri ligi PvP** eklenecek mi? (ekipmanın etkisiz kılındığı 1v1)
- [ ] **Görsel karakter özelleştirme** kapsam dışı mı? (mekanik/build tarafı öncelikli
      olduğu teyit edildi; görsel özelleştirme pahalı bir iş kalemi)
- [ ] **3. ekip üyesinin rolü**
- [ ] **Mobil mi, mobil + kontrolcü mü?** Dövüşün hassasiyet tavanını bu belirliyor
- [ ] Kalıcı proje/oyun ismi (şu anki repo adı geçici)
