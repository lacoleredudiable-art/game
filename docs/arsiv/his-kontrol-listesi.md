# His Kontrol Listesi (ARŞİV)

> **ARŞİV, 29 Ağustos 2026.** Bu tur kapandı: beş sorudan dördü telefonda "evet" aldı,
> beşincisi (iki oyuncu) hiç denenmedi. Yeni bir his turu yapılacaksa şablon burada; sonuçlar
> `docs/durum.md`'de özetli. Aşağıdaki cevaplar kapanmış turun kaydıdır.

> **Bu dosya telefonda, elde doldurulur.** Masa başında cevaplanan hiçbir satırı geçerli sayma —
> prototipin bütün amacı [§13'ün beş sorusunu](dovus-sistemi-v1.md#13-prototipin-cevapladığı-sorular)
> gerçek bir telefonda cevaplamak.
>
> **Sıra bağlayıcıdır:** öncekine "evet" denmeden sonrakine geçilmez. 2. soruya "hayır" çıkarsa
> tasarım baştan düşünülür; o yüzden 3–5'i doldurmak zaman kaybı olur.

## Nasıl doldurulur

1. APK'yı kur (`build/android/dovus-prototip.apk`), telefonu **yatay** tut.
2. Kare süresi göstergesini aç: sağ-alt **AYAR** → en altta **ÖLÇÜM (T11)** → *Kare süresi
   göstergesi: AÇIK*. Sol-alt köşede ortalama ms · fps · en kötü kare yazar. Ölçüm bitince kapat
   (kapalıyken hiç çizilmiyor). Panel açıkken sağ-alt düğme **KAPAT** olur; kartın sağ-üstünde
   de bir KAPAT var — ikisi de kapatır (2. oturumda perde düğmeyi yutuyordu, düzeltildi).
3. Her soru için önce alt maddeleri tek tek dene, sonra **Cevap** satırını doldur.
   Cevap yalnızca **evet / hayır** olabilir; "kısmen" yazma — kısmen, gerekçe satırına yazılır.
4. Bir sayıyı değiştirdiysen **aynı turda** "Ayarlanan sayılar" tablosuna yaz. AYAR panelindeki
   her değişiklik telefonda `tuning.json`'a kaydedilir; panelin altındaki **JSON'U KOPYALA** ile
   panoya alıp buraya yapıştırabilirsin.

## Ölçüm ortamı

| Alan | Değer |
|---|---|
| Telefon (marka/model) | POCO / Xiaomi `2412DPC0AG` (`rodin_global`), Mali-G720 MC7 |
| Android sürümü | 16 (HyperOS) |
| Ekran (çözünürlük · tazeleme hızı) | 2712×1220 yatay · 520 dpi (tazeleme hızı ölçülmedi) |
| APK | `build/android/dovus-prototip.apk` — development, IL2CPP, ARM64 |
| Hedef kare hızı | 60 (`PrototypeTuning.TargetFrameRateHz`) |
| Tarih / oturum süresi | 22–23 Ağustos 2026 · 6 oturum (aşağıda) |
| Oyuncu (kim oynadı) | 1 / 3 / 4 / 5 / 6: sahibi · 2: ajan (`adb` ekran + `tuning.json`) |

> **1. oturum (sahibi, kısa).** Oyun açıldı ve oynandı; **iki parmak aynı anda sorunsuz**
> (sol çubuk + sağ çizim). Gösterge açılmadı; AYAR paneli kapanmadı (perde düğmeyi yutuyordu).
>
> **2. oturum (kablo).** Yeni APK kuruldu, gösterge `tuning.json` ile açıldı. **16,6 ms / 60 fps**,
> en kötü 16,8–16,9. Dünya rengi düzgün. HyperOS uzaktan dokunuşu (`input tap`) reddettiği
> için yürüyüş/telegraf/cümle satırları elde. Panel kapanışı sahibi doğruladı (sıkıntı yok).
>
> **3. oturum (sahibi, elde).** Soru 1'in bütün alt maddeleri: "hepsi iyi, hisler iyi".
> Panik dodge ve disk yayı donanımda ilk kez doğrulandı. **Soru 1 kapandı.**
>
> **4. oturum (sahibi, elde).** Soru 2 duvarı aşıldı: boss görünüyor, pencere ödül *ve*
> yeniden pozisyon. Ama "cümlelerin karşılığını anlamıyorum", "çeşitlilik yok", "skill
> kullanmanın bedeli yok". **Soru 2 kapandı, 3–5 tıkandı.**
>
> **5. oturum (23 Ağustos, masa başı değerlendirme — telefonda oynanmadı).** 4. oturumun
> "okunmuyor" teşhisi düzeltildi: sahibi mekaniği **okuyor**. Kendi ifadesiyle: *"ne olduğunu
> anlıyorum, dodge atıyorum, yavaş çekim geliyor, cümleleri spamlıyorum, boştayken de cümle
> kuruyorum; kurduğum cümlelerin anlamını basit animasyonla görüyorum, basılı tutunca farklı
> oluyor, 2'li çizince başka 3'lü çizince başka. O yüzden mekanik çalışıyor dedim. Ama çok
> soyut kalıyor. Ve boss fight'ı tekdüze."* Yani sorun **okunurluk değil temsil ve içerik**:
> soyutluk (T14), tek saldırı (T13) ve harcamanın gittiği bir yer olmaması (T12).
> Bu oturumda üç karar alındı — bkz. "Turda alınan kararlar".
>
> **6. oturum (23 Ağustos akşam, sahibi, elde).** Faz 3.5'in tamamı (T12 + T13 + T14) tek
> APK'da telefona kuruldu ve oynandı. Sahibinin cevabı **toplu onay**: *"bunların hepsi geçti."*
> Sorulan dört başlık: boss canının inmesi ve ölüm/revive (T12), çakma varyantının **vuruştan
> önce** ayırt edilmesi (T13), üç fiilin hareketten ayrışması + düz vuruşun jab olması (T14),
> ve spam yerine uzatma davranışının doğması (soru 3). **Soru 3 ve 4 kapandı.**
> Alt maddeler tek tek ayrıntılanmadı — onay başlık düzeyinde kaydedildi.

## Kare bütçesi

Her satırı **en az 20 saniye** o durumda kalarak ölç; "en kötü" sütunu göstergedeki en kötü
kareyi yazar (0.5 sn'lik pencerede aranır).

| Senaryo | Ortalama ms | En kötü ms | fps | Not |
|---|---|---|---|---|
| Boşta arena (hareketsiz) | 16,6 | 16,8–16,9 | 60 | 2. oturum ekran; ölüm/dönüş de aynı kilit |
| Yürürken + boss yaklaşırken |  |  |  |  |
| Telegraf + yere çakma anı |  |  |  |  |
| 4 noktalı cümle + kapanış patlaması |  |  |  |  |
| Yavaş çekim boyunca |  |  |  |  |
| ~30 kapanış sonrası (yerdeki iz tavanı 60) |  |  |  |  |
| AYAR paneli açıkken |  |  |  |  |
| Uygulama açılışı → ilk kare (sn) |  |  |  |  |

**Şüphelenilecek yerler** (kare düşerse önce buralara bak, hepsi ölçüm öncesi biliniyordu):

- İkinci (Overlay) kamera fazladan bir render geçişi — beşgen/mürekkep için (T8 katmanlama).
- `SentenceEngine.PublishState` her karede yeni `SentenceWord[]` ayırıyor; cümle kurulurken
  GC baskısı yaratabilir (bilinen açık, Core'da düzeltilmedi).
- Yerdeki iz tavanı `GroundScarCapCount = 60` — telefonda uydurma bir sayı, burada sınanıyor.
- Afterimage sayısı (`AfterimageCount`) ve kapanış patlamasının LineRenderer'ları.

---

## Soru 1 — Sırtın ürperiyor mu?

> **Telefonda tam zamanında dodge atıp bossun saldırısını sıyırdığında sırtın ürperiyor mu?**

Alt maddeler:

- [x] Telegrafı (yer diski + hazırlık pozu + yükselen ton) ne kadar **erken** okuyabiliyorsun?
      Diski hiç görmeden mi vuruluyorsun? Hangisi uyarıyor: disk mi, poz mu, ses mi?
      → **3. oturum (sahibi):** sorun yok. Hangisinin uyardığı ayrıntılanmadı.
- [x] Dodge diski başparmağın doğal yayında mı? Uzanmak zorunda kalıyor musun?
      (`DodgeButtonOffsetXDp` 80 / `OffsetYDp` −140 / `RadiusDp` 34 — hepsi uydurma sayı)
      → **3. oturum (sahibi):** sorun yok. Offset değişmedi.
- [x] Çizim parmağı ekrandayken **ikinci parmakla panik dodge** çalışıyor mu? (donanımda hiç
      denenmedi)
      → **3. oturum (sahibi):** sorun yok. Donanımda ilk kez.
- [x] MÜKEMMEL / HARİKA / TEMİZ farkı **elde** hissediliyor mu, yoksa hepsi aynı mı?
      (eşikler 90 / 160 / 220 ms — masa başı kararı)
      → **3. oturum (sahibi):** hepsi iyi / hisler iyi. Eşik değişmedi.
- [x] Sıyırma anındaki his katmanında ne eksik, ne fazla? (hitstop · kamera yumruğu · sarsıntı ·
      impact frame · afterimage kuyruğu · alçak geçiren ses)
      → **3. oturum (sahibi):** hisler iyi. Katman kırpılmadı / eklenmedi.
- [x] Kaçan dodge'da sık sık "MENZİL DIŞI (derece yok)" mu yazıyor? (öyleyse
      `BossApproachStopPadM` = 0.35 m ödül bandını kesiyor)
      → **3. oturum (sahibi):** sorun olarak görülmedi. `BossApproachStopPadM` duruyor.
- [x] Ölüm cezası gerçekten hafif mi (≤2 sn), yoksa ölmek can mı sıkıyor?
      → **3. oturum (sahibi):** iyi.

**Cevap:** ☑ evet ☐ hayır
**Gerekçe (tek cümle, telefondayken yaz):** 1. oturum "zamanlamayı tutturmak tatmin ediciydi"; 3. oturum "hepsi iyi hisler iyi".

> Soru 1 **kapandı** (23 Ağustos, sahibi). Ayrıntılı "disk mi poz mu" ayrımı yok; genel his evet.

---

## Soru 2 — Yavaş çekim ödül mü, ceza mı?

> **Yavaş çekim penceresinde cümle kurmak, bossu okumanın ödülü gibi mi hissediyor — yoksa
> gözünü bossdan mı ayırıyor?** Bütün türün etrafından dolaştığı duvar bu.

Alt maddeler:

- [x] Yavaş çekimde **kaç nokta** yazabildin? (masa başı ölçümü: 350–400 ms temposunda 4,
      yavaş çekim yokken 2–3)
      → normal: (söylenmedi) · yavaş çekimde: **4'ten fazla, 6–7 bile.** Spec tavanı 4 —
      ya mürekkep tavan sonrası devam ediyor ya tavan tutulmuyor. Açık.
- [x] Cümleyi yazarken **bossu görebiliyor musun**, yoksa gözün beşgene mi kilitleniyor?
      → **Bossu görüyorum.** Duvar sorusu (göz bossdan ayrılıyor mu) hayır değil.
- [x] Yavaş çekim toplam 1555 ms gerçek zaman sürüyor ve bu sürede boss neredeyse duruyor.
      Bu bir **kaçış** gibi mi kullanılıyor (savunma sömürüsü)?
      → **İkisi de:** "ödül gibi de olur kaçış da olur, yeniden pozisyon alma gibi."
- [ ] İptal penceresini **dalganın nerede olduğuna bakarak** bilebiliyor musun, yoksa köşedeki
      debug sayısına mı bakıyorsun? (§8/T2'nin "bedava kazanç" iddiası burada sınanıyor)
      → Cümle sonucu okunmadığı için bu maddeye girilmedi.
- [x] Yavaş çekim cümlenin ortasında bitince ne oluyor — cümle yarıda mı kalıyor, sinir bozucu mu?
      → **Anlaşılmadı.** "Cümlelerin karşılığını tam göremediğim için pencerenin ortada
      bitip bitmediğini anlamıyorum."

**Cevap:** ☑ evet (ödül gibi) ☐ hayır (gözümü bossdan ayırıyor)
**Gerekçe:** Boss görünüyor, pencere ödül *ve* yeniden pozisyon. Asıl boşluk cümle sonucunun okunmaması — farklılık var, karşılık anlaşılmıyor.

> Duvar (göz bossdan ayrılıyor mu) **aşılmadı**; mühürleri dövüş öncesine kaydırma tartışması
> açılmıyor. 3–5'e geçilir. Cümle okunurluğu soru 3–4'ün konusu.

---

## Soru 3 — Yazıyor muyum, yönetiyor muyum?

> **Parmağımla karakteri yazıyor muyum, yoksa yönetiyor muyum?** (Her ön-ek geçerli olduğu için
> sonucu izlerken uzatabiliyor muyum?)

Alt maddeler:

- [x] Etkiyi **havada izlerken** cümleyi uzatmayı gerçekten deniyor musun, yoksa dokunuşları
      baştan planlayıp seri mi basıyorsun?
      → **5. oturum: seri basıyor.** "Cümleleri spamlıyorum." İzlerken uzatma davranışı
      doğmuyor. Sebep muhtemelen ekonomik değil bilgisel: harcamanın bir karşılığı olmadığı
      bir sistemde optimal oynayış zaten spam'dir (T12 bunu değiştiriyor).
- [x] Bekletme (dwell, 220 ms) yığınını **bilerek** kullanıyor musun? Parmağın durduğunu fark
      ediyor musun?
      → **5. oturum: evet, bilerek.** "Basılı tutunca farklı oluyor." §3'ün K3 ekseni elde
      fark ediliyor — donanımda ilk doğrulama.
- [x] Toparlanma kilidini kesmek (düz vuruş / yeni fiil / dodge) bir **beceri** gibi mi geliyor,
      yoksa kilit hiç fark edilmiyor mu? (kalan süre şu an yalnızca debug metninde)
      → **4. oturum:** skill kullanmanın bedeli yok — kilit beceri olarak görünmüyor.
- [x] Merkez (düz vuruş / erken kapanış) ile köşe (rün) arasındaki fark parmağında net mi?
      Yanlışlıkla düz vuruş attığın oluyor mu?
      → **6. oturum: geçti** (T14 düz vuruşa kendi jab silüetini verdi; toplu onay).
- [ ] Beşgen ölçüleri elde doğru mu: yanlış rün kaydediliyor mu, çizerken beşgeni gözden
      kaçırıyor musun? (`PentagonRadiusDp` 100 · `DotHitRadiusDp` 30 · merkez norm 0.72/0.40)
      → ____________________________________________
- [ ] Hece sesi + 30 ms titreşim, her nokta için **ayrık** bir onay veriyor mu (§2), yoksa
      birbirine mi karışıyor?
      → ____________________________________________
- [x] Sol çubuk + sağ çizim aynı anda sorunsuz mu? Çubuk kayboluyor/yapışıyor mu?
      → **1. oturum: sorunsuz.** İkisi aynı anda çalıştı; donanımda ilk doğrulama (bugüne
      kadar yalnızca enjekte edilmiş sanal dokunuşlarla sınanmıştı).

**Cevap:** ☑ yazıyorum ☐ yönetiyorum
**Gerekçe:** 6. oturum: harcamanın karşılığı (boss canı, T12) gelince spam davranışı yerini uzatmaya bıraktı; dwell ve nokta sayısı zaten okunuyordu. Sahibi toplu onay verdi.

> **4. oturum:** "çeşitlilik yok skillerde sorun yok ama 3 tane nokta açık diye mi artık
> çeşitlilik yok üstüne de skill kullanmanın bir bedeli de yok."
>
> **5. oturum düzeltmesi:** 4. oturumun "cümle sonucu okunmuyor" teşhisi yanlıştı. Sahibi
> cümlenin nokta sayısını ve dwell'i **görüyor**; göremediği şey harcamanın nereye gittiği
> (boss canı kozmetik) ve sonucun ne olduğu (silüetler ayrık değil). Kutu Faz 3.5 sonrası
> yeni bir telefon turunda işaretlenecek — masa başında işaretlenmesi geçersizdir.

---

## Soru 4 — Canlanıyor mu, hasar mı verdim?

> **Çizdiğim şeyin dünyada canlandığını izliyor muyum, yoksa hasar mı verdim?**

Alt maddeler:

- [x] Sıfat eklendiğinde etkinin **silüeti havadayken** değişiyor mu ve bunu gözle yakalıyor
      musun? (`5` halka → `5-1` tek hatta toplanma → `5-1-2` hat boyunca sürü)
      → **5. oturum: evet, değişimi görüyor.** "2'li çizince başka, 3'lü çizince başka."
      Yani §8/T3 (sıfat silüeti değiştirir) elde tutuyor. Sorun değişimin görünmemesi değil,
      değişen şeyin **ne olduğunun soyut kalması**.
- [x] Kapanış patlaması cümlenin **bittiğini** hissettiriyor mu, yoksa sadece bir ışık mı?
      → **6. oturum: geçti** (toplu onay; artık boss canından hasar da düşüyor).
- [ ] Yerdeki kalıcı izler dövüşün geçmişi gibi mi duruyor, yoksa çöp gibi mi birikiyor?
      (tavan 60)
      → ____________________________________________
- [x] Düz vuruş, normal cümlenin görselini kullanıyor (SARSINTI halkası). "Vuruş mu, cümle mi"
      hissi karışıyor mu? → T14 bunu ayırıyor; tur sonrası yeniden sorulacak
      → **6. oturum: karışmıyor.** Jab silüeti ayrık (toplu onay).
- [x] Bossun tepkisi (geri tepme kalıcı, sarsılma, kaldırma) okunuyor mu? Boss'un can barının
      hiç azalmaması rahatsız edici mi? (bar bilerek kozmetik — boss hasarı yok)
      → **5. oturum: evet, rahatsız edici.** Sahibi hasarı sayılarla görmek istedi ("hasarı
      sayılarla görsem his verir"). Karar: boss canı gerçek olacak (T12), ama uçan hasar
      sayısı §8/T3 ve §12 gereği ekranda yazmayacak — bkz. "Turda alınan kararlar".
- [ ] §10 renk ayrımı telefonda ayakta mı: oyuncu efektlerinde hiç kırmızı-turuncu yok,
      telegraf her şeyin üstünde okunuyor mu? (özellikle vurulma vinyeti sırasında)
      → ____________________________________________

**Cevap:** ☑ izliyorum ☐ hasar verdim
**Gerekçe:** 6. oturum: T14 üç fiili hareketten ayırdıktan sonra "soyut kalıyor" şikâyeti geçti; T12'nin inen boss canı harcamayı görünür yaptı ama his hasar sayacına değil tezahüre bağlı kaldı (hasar sayısı hâlâ varsayılan kapalı).

---

## Soru 5 — İki oyuncu, iki farklı çözüm

> **İki oyuncu aynı bossu farklı cümlelerle geçebiliyor ve ikisi de çözümü kendi bulmuş gibi
> hissediyor mu?**

Bu soru tek başına cevaplanamaz: **ikinci bir oyuncu gerekir.** İkisi de birbirinin oynayışını
görmeden, en az 10'ar dakika oynasın.

- [ ] Oyuncu A'nın en çok kullandığı üç cümle: ____________________________________________
- [ ] Oyuncu B'nin en çok kullandığı üç cümle: ____________________________________________
- [ ] İkisi aynı cümleye mi yakınsıyor? ____________________________________________
- [x] Üç rünün (İĞNE / SÜRÜ / SARSINTI) **silüetleri** yeterince ayrık mı, yoksa hepsi
      "bir efekt" gibi mi görünüyor?
      → **4. oturum (tek oyuncu):** çeşitlilik yok. Farklılık olduğunu biliyor, karşılığı
      bağlayamıyor — silüetler ayrık değil.
      → **6. oturum: ayrık.** T14'ten sonra hareket karakteri (fırlama / üşüşme / yükselme)
      elde ayırt ediliyor. Sorunun kalan yarısı (iki oyuncu aynı cümleye mi yakınsıyor)
      hâlâ açık — **ikinci oyuncu gerekiyor**.
- [ ] Her ikisi de çözümü **kendi** bulmuş gibi mi hissediyor?
      → ____________________________________________

**Cevap:** ☐ evet ☐ hayır
**Gerekçe:** ____________________________________________

> Herkes aynı cümleyi çiziyorsa bir eksen çökmüş demektir. Düzeltmesi **rün eklemek değil**,
> rünlerin türlerini daha keskin ayırmaktır (§13).
>
> **5. oturum:** sahibi "5 ründen baya fazla skill çeşidi nasıl olacak" diye sordu. Sayı
> sorun değil: üç rünle 120, beş rünle **780** farklı cümle var (1 fiil + en fazla 3 sıfat,
> tekrar serbest). Eksik olan iki şey `docs/dovus-sistemi.md` §4'e yazıldı — silüetlerin tür
> olarak ayrışmaması (T14) ve **durum/etkileşim tablosunun hiç olmaması**. İkincisi Faz 3.5'in
> değil, ondan sonraki ilk büyük sistemin konusu ([özet §5](../tasarim-ozeti.md#5-yaratıcı-build-sistemi)).

---

## Turda alınan kararlar (5. oturum, 23 Ağustos)

Sahibiyle karara bağlandı, spec güncellendi, **yeniden tartışılmayacak.** Görev metinleri
`docs/gorev-listesi.md` "Faz 3.5"te.

| Karar | Ne | Nereye yazıldı |
|---|---|---|
| Boss ölüyor | Boss canı **120**; kapanış ödülü (§5: 1,0 / 2,4 / 4,4 / 7,0) `closingDamagePerEffect = 1.0` ile 1'e 1 hasara çevriliyor. En iyi oynanışta ~17, karışıkta 25–30 kapanış | §5 "Etkinin hasara çevrilmesi", §11 |
| Ölüm bir noktalama işareti | Can 0 → kısa yavaş çekim + çökme pozu → **tam canla yeniden doğuş**. Zafer ekranı/ilerleme yok, ayar turu kesilmiyor | §11 |
| Kapanış türü korunuyor | Hasar eklendi ama tür ekseni bırakılmıyor: son rün fiziksel tepkiyi seçer (havalandırma / geri tepme / yerinde sarsılma) ve **hasar türe göre değişmez** | §5, T12 yasakları |
| Hasar sayısı ekranda yok | Sahibi sayı istedi; §8/T3 ve §12 yasaklıyor. Uzlaşma: ayar panelinde **varsayılan kapalı** bir ölçüm göstergesi — kumpas, his kanalı değil | §5, T12 madde 5 |
| Yeni saldırı yok, varyant var | Aynı YERE ÇAKMA'nın üç ritmi (YAKIN / GEÇ / GENİŞ). Kısıt: varyant vuruştan **önce** ayırt edilebilir olmalı | §11 "Çakma varyantları" |
| Silüet ayrımı hareketten | İĞNE fırlar · SÜRÜ dağınık üşüşür · SARSINTI yerden yükselir. Hâlâ primitive | T14 |
| Çeşitlilik durum tablosundan gelecek | Rün havuzunu büyütmek değil; ıslak/yanıyor/zırhı kırık durumları + etkileşim kuralları. Faz 3.5'ten sonraki ilk büyük sistem | §4, gorev-listesi Faz 4 notu |

## Ayarlanan sayılar

Turda AYAR panelinden değiştirilen her değer. "Kalsın mı" sütunu, değişikliğin koda/spec'e
geçip geçmeyeceğini söyler — dolduran kişi karar verir, sonraki ajan uygular.

| Alan | Eski | Yeni | Neden | Kalsın mı |
|---|---|---|---|---|
|  |  |  |  |  |
|  |  |  |  |  |
|  |  |  |  |  |

## Turda çıkan yeni sorunlar

Telefonda görülüp masa başında görülmemiş her şey. Kod düzeltmesi gerektirenler buradan
`docs/durum.md`'nin "Bilinen açıklar" bölümüne taşınır.

1. **AYAR paneli kapanmıyordu** — perde aç/kapat düğmesini yutuyordu. Kodda kapandı
   (`SetAsLastSibling` + kartta KAPAT). **Sahibi 23 Ağustos'ta doğruladı: kapanıyor, sıkıntı yok.**
2. **Dünya macentaydı** (shader strip). `AndroidBuilder` Always Included listesine yazıyor;
   2. oturum ekranında kapandı.
3. **~~Cümle sonucu okunmuyor~~ → ~~temsil soyut kalıyor~~ → 6. oturumda kapandı.**
   4. oturumda "okunmuyor" diye kaydedildi, 5. oturumda "temsil eksiği" diye düzeltildi,
   **T14** hareket karakterini ayırdıktan sonra sahibi geçti dedi.
4. **~~Mürekkep cümle sınırını göstermiyor~~ → T11.1 kapattı** (`InkTrail.Break`). Cihazda
   yavaş çekim göz doğrulaması hâlâ yok.
5. **Üç açık rün çeşitlilik üretmiyor.** İki sebebi vardı: silüetlerin tür olarak ayrışmaması
   ve durum/etkileşim tablosunun hiç olmaması. **İlk yarısı T14 ile kapandı** (6. oturum);
   ikincisi hâlâ açık ve tek oyuncuyla ölçülemez — `docs/dovus-sistemi.md` §4.
6. **~~Skill bedeli yok~~ — kapandı.** Toparlanma kilidi **T11.1** (`RecoveryLockHud`),
   boss canı **T12**; 6. oturumda harcamanın karşılığı hissedildi.
7. **~~Boss fight'ı tekdüze~~ — kapandı.** **T13**'ün üç ritmi (YAKIN/GEÇ/GENİŞ) 6. oturumda
   vuruştan önce ayırt edildi.

## Tura girerken bilinen sorunlar

Bunlar T11'den **önce** biliniyordu; telefonda doğrulanması ya da çürütülmesi bekleniyor.
Kaynak: `docs/durum.md` "Bilinen açıklar".

- **Panik dodge donanımda doğrulandı (3. oturum, sahibi: sorun yok).** (Soru 1)
- **Ayar panelinin Slider/Button'ları gerçek parmakla denenmedi.** `InputSystemUIInputModule`
  ile `EnhancedTouch` aynı donanım kuyruğunu okuyor; çakışma editörde `onClick.Invoke()` ile
  dolaylı test edildi.
- **`tuning.json` kalıcılığı gerçek APK kapat/aç ile denenmedi** (editörde play durdur/başlat ile
  doğrulandı). İlk iş: bir slider değiştir, uygulamayı **tamamen** kapat, aç, değer duruyor mu bak.
- **Tepki yazısının bandı beşgenin üst rününe değebilir.** Yüksek yoğunluklu ekranda geometriden
  türetildi, ölçülmedi. (Soru 3/4)
- **Vurulma vinyeti sağ kenardaki "geç kaldın" yazısının üstüne biniyor** — okunaklılık telefonda
  kontrol edilmeli. (Soru 1)
- **Derecelendirme eşikleri (90/160/220 ms) masa başı kararı.** İlk ayarlanacak sayılar bunlar.
- **`BossApproachStopPadM` = 0.35 m yüzünden TEMİZ/SIYIRDI bantları kaçan dodge'da hacim dışı
  kalıyor** (yana dodge'da erişilebilir). Ödül bandının ne kadar erişilebilir olduğu ölçülmeli.
  → **T13'ün GENİŞ varyantı (radius 8.0 m) bunu ölçülebilir biçimde açıyor**; kabul kriteri
  olarak yazıldı.
- **Yavaş çekim 1555 ms sürüyor ve savunma avantajı da veriyor** (T8.2). İlk kısılacak sayı bu
  olabilir. (Soru 2)
- **~~Toparlanma kilidi kalıcı HUD'da görünmüyor~~** → **T11.1 kapattı.** (Soru 3)
- **Hece sesleri sinüs tıkırtısı**, müzikal kalite yok (§9 yapısı var). (Soru 3)
- **30 kapanış üst üste, gerçek oyunda hiç sayılmadı** — izole testte tavan doğrulandı. Telefonda
  30 kapanış yapıp yerdeki izin görsel yoğunluğuna bak. (Soru 4 + kare bütçesi)
