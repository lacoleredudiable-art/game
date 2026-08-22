# His Kontrol Listesi

> **Bu dosya telefonda, elde doldurulur.** Masa başında cevaplanan hiçbir satırı geçerli sayma —
> prototipin bütün amacı [§13'ün beş sorusunu](dovus-sistemi.md#13-prototipin-cevapladığı-sorular)
> gerçek bir telefonda cevaplamak.
>
> **Sıra bağlayıcıdır:** öncekine "evet" denmeden sonrakine geçilmez. 2. soruya "hayır" çıkarsa
> tasarım baştan düşünülür; o yüzden 3–5'i doldurmak zaman kaybı olur.

## Nasıl doldurulur

1. APK'yı kur (`build/android/dovus-prototip.apk`), telefonu **yatay** tut.
2. Kare süresi göstergesini aç: sağ-alt **AYAR** → en altta **ÖLÇÜM (T11)** → *Kare süresi
   göstergesi: AÇIK*. Sol-alt köşede ortalama ms · fps · en kötü kare yazar. Ölçüm bitince kapat
   (kapalıyken hiç çizilmiyor).
3. Her soru için önce alt maddeleri tek tek dene, sonra **Cevap** satırını doldur.
   Cevap yalnızca **evet / hayır** olabilir; "kısmen" yazma — kısmen, gerekçe satırına yazılır.
4. Bir sayıyı değiştirdiysen **aynı turda** "Ayarlanan sayılar" tablosuna yaz. AYAR panelindeki
   her değişiklik telefonda `tuning.json`'a kaydedilir; panelin altındaki **JSON'U KOPYALA** ile
   panoya alıp buraya yapıştırabilirsin.

## Ölçüm ortamı

| Alan | Değer |
|---|---|
| Telefon (marka/model) |  |
| Android sürümü |  |
| Ekran (çözünürlük · tazeleme hızı) |  |
| APK | `build/android/dovus-prototip.apk` — development, IL2CPP, ARM64 |
| Hedef kare hızı | 60 (`PrototypeTuning.TargetFrameRateHz`) |
| Tarih / oturum süresi |  |
| Oyuncu (kim oynadı) |  |

## Kare bütçesi

Her satırı **en az 20 saniye** o durumda kalarak ölç; "en kötü" sütunu göstergedeki en kötü
kareyi yazar (0.5 sn'lik pencerede aranır).

| Senaryo | Ortalama ms | En kötü ms | fps | Not |
|---|---|---|---|---|
| Boşta arena (hareketsiz) |  |  |  |  |
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

- [ ] Telegrafı (yer diski + hazırlık pozu + yükselen ton) ne kadar **erken** okuyabiliyorsun?
      Diski hiç görmeden mi vuruluyorsun? Hangisi uyarıyor: disk mi, poz mu, ses mi?
      → ____________________________________________
- [ ] Dodge diski başparmağın doğal yayında mı? Uzanmak zorunda kalıyor musun?
      (`DodgeButtonOffsetXDp` 80 / `OffsetYDp` −140 / `RadiusDp` 34 — hepsi uydurma sayı)
      → ____________________________________________
- [ ] Çizim parmağı ekrandayken **ikinci parmakla panik dodge** çalışıyor mu? (donanımda hiç
      denenmedi)
      → ____________________________________________
- [ ] MÜKEMMEL / HARİKA / TEMİZ farkı **elde** hissediliyor mu, yoksa hepsi aynı mı?
      (eşikler 90 / 160 / 220 ms — masa başı kararı)
      → ____________________________________________
- [ ] Sıyırma anındaki his katmanında ne eksik, ne fazla? (hitstop · kamera yumruğu · sarsıntı ·
      impact frame · afterimage kuyruğu · alçak geçiren ses)
      → ____________________________________________
- [ ] Kaçan dodge'da sık sık "MENZİL DIŞI (derece yok)" mu yazıyor? (öyleyse
      `BossApproachStopPadM` = 0.35 m ödül bandını kesiyor)
      → ____________________________________________
- [ ] Ölüm cezası gerçekten hafif mi (≤2 sn), yoksa ölmek can mı sıkıyor?
      → ____________________________________________

**Cevap:** ☐ evet ☐ hayır
**Gerekçe (tek cümle, telefondayken yaz):** ____________________________________________

---

## Soru 2 — Yavaş çekim ödül mü, ceza mı?

> **Yavaş çekim penceresinde cümle kurmak, bossu okumanın ödülü gibi mi hissediyor — yoksa
> gözünü bossdan mı ayırıyor?** Bütün türün etrafından dolaştığı duvar bu.

Alt maddeler:

- [ ] Yavaş çekimde **kaç nokta** yazabildin? (masa başı ölçümü: 350–400 ms temposunda 4,
      yavaş çekim yokken 2–3)
      → normal: ______ · yavaş çekimde: ______
- [ ] Cümleyi yazarken **bossu görebiliyor musun**, yoksa gözün beşgene mi kilitleniyor?
      → ____________________________________________
- [ ] Yavaş çekim toplam 1555 ms gerçek zaman sürüyor ve bu sürede boss neredeyse duruyor.
      Bu bir **kaçış** gibi mi kullanılıyor (savunma sömürüsü)?
      → ____________________________________________
- [ ] İptal penceresini **dalganın nerede olduğuna bakarak** bilebiliyor musun, yoksa köşedeki
      debug sayısına mı bakıyorsun? (§8/T2'nin "bedava kazanç" iddiası burada sınanıyor)
      → ____________________________________________
- [ ] Yavaş çekim cümlenin ortasında bitince ne oluyor — cümle yarıda mı kalıyor, sinir bozucu mu?
      → ____________________________________________

**Cevap:** ☐ evet (ödül gibi) ☐ hayır (gözümü bossdan ayırıyor)
**Gerekçe:** ____________________________________________

> "Hayır" ise burada dur. Mühürleri dövüş öncesi hazırlığa kaydırma tartışması açılır
> (§13). Aşağıdaki soruları doldurma.

---

## Soru 3 — Yazıyor muyum, yönetiyor muyum?

> **Parmağımla karakteri yazıyor muyum, yoksa yönetiyor muyum?** (Her ön-ek geçerli olduğu için
> sonucu izlerken uzatabiliyor muyum?)

Alt maddeler:

- [ ] Etkiyi **havada izlerken** cümleyi uzatmayı gerçekten deniyor musun, yoksa dokunuşları
      baştan planlayıp seri mi basıyorsun?
      → ____________________________________________
- [ ] Bekletme (dwell, 220 ms) yığınını **bilerek** kullanıyor musun? Parmağın durduğunu fark
      ediyor musun?
      → ____________________________________________
- [ ] Toparlanma kilidini kesmek (düz vuruş / yeni fiil / dodge) bir **beceri** gibi mi geliyor,
      yoksa kilit hiç fark edilmiyor mu? (kalan süre şu an yalnızca debug metninde)
      → ____________________________________________
- [ ] Merkez (düz vuruş / erken kapanış) ile köşe (rün) arasındaki fark parmağında net mi?
      Yanlışlıkla düz vuruş attığın oluyor mu?
      → ____________________________________________
- [ ] Beşgen ölçüleri elde doğru mu: yanlış rün kaydediliyor mu, çizerken beşgeni gözden
      kaçırıyor musun? (`PentagonRadiusDp` 100 · `DotHitRadiusDp` 30 · merkez norm 0.72/0.40)
      → ____________________________________________
- [ ] Hece sesi + 30 ms titreşim, her nokta için **ayrık** bir onay veriyor mu (§2), yoksa
      birbirine mi karışıyor?
      → ____________________________________________
- [ ] Sol çubuk + sağ çizim aynı anda sorunsuz mu? Çubuk kayboluyor/yapışıyor mu?
      → ____________________________________________

**Cevap:** ☐ yazıyorum ☐ yönetiyorum
**Gerekçe:** ____________________________________________

---

## Soru 4 — Canlanıyor mu, hasar mı verdim?

> **Çizdiğim şeyin dünyada canlandığını izliyor muyum, yoksa hasar mı verdim?**

Alt maddeler:

- [ ] Sıfat eklendiğinde etkinin **silüeti havadayken** değişiyor mu ve bunu gözle yakalıyor
      musun? (`5` halka → `5-1` tek hatta toplanma → `5-1-2` hat boyunca sürü)
      → ____________________________________________
- [ ] Kapanış patlaması cümlenin **bittiğini** hissettiriyor mu, yoksa sadece bir ışık mı?
      → ____________________________________________
- [ ] Yerdeki kalıcı izler dövüşün geçmişi gibi mi duruyor, yoksa çöp gibi mi birikiyor?
      (tavan 60)
      → ____________________________________________
- [ ] Düz vuruş, normal cümlenin görselini kullanıyor (SARSINTI halkası). "Vuruş mu, cümle mi"
      hissi karışıyor mu?
      → ____________________________________________
- [ ] Bossun tepkisi (geri tepme kalıcı, sarsılma, kaldırma) okunuyor mu? Boss'un can barının
      hiç azalmaması rahatsız edici mi? (bar bilerek kozmetik — boss hasarı yok)
      → ____________________________________________
- [ ] §10 renk ayrımı telefonda ayakta mı: oyuncu efektlerinde hiç kırmızı-turuncu yok,
      telegraf her şeyin üstünde okunuyor mu? (özellikle vurulma vinyeti sırasında)
      → ____________________________________________

**Cevap:** ☐ izliyorum ☐ hasar verdim
**Gerekçe:** ____________________________________________

---

## Soru 5 — İki oyuncu, iki farklı çözüm

> **İki oyuncu aynı bossu farklı cümlelerle geçebiliyor ve ikisi de çözümü kendi bulmuş gibi
> hissediyor mu?**

Bu soru tek başına cevaplanamaz: **ikinci bir oyuncu gerekir.** İkisi de birbirinin oynayışını
görmeden, en az 10'ar dakika oynasın.

- [ ] Oyuncu A'nın en çok kullandığı üç cümle: ____________________________________________
- [ ] Oyuncu B'nin en çok kullandığı üç cümle: ____________________________________________
- [ ] İkisi aynı cümleye mi yakınsıyor? ____________________________________________
- [ ] Üç rünün (İĞNE / SÜRÜ / SARSINTI) **silüetleri** yeterince ayrık mı, yoksa hepsi
      "bir efekt" gibi mi görünüyor?
      → ____________________________________________
- [ ] Her ikisi de çözümü **kendi** bulmuş gibi mi hissediyor?
      → ____________________________________________

**Cevap:** ☐ evet ☐ hayır
**Gerekçe:** ____________________________________________

> Herkes aynı cümleyi çiziyorsa bir eksen çökmüş demektir. Düzeltmesi **rün eklemek değil**,
> rünlerin türlerini daha keskin ayırmaktır (§13).

---

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

1.
2.
3.

## Tura girerken bilinen sorunlar

Bunlar T11'den **önce** biliniyordu; telefonda doğrulanması ya da çürütülmesi bekleniyor.
Kaynak: `docs/durum.md` "Bilinen açıklar".

- **Panik dodge donanımda hiç denenmedi.** Çizim parmağı + ikinci parmakla diske basma yalnızca
  enjekte edilen sanal dokunuşlarla sınandı. (Soru 1)
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
- **Yavaş çekim 1555 ms sürüyor ve savunma avantajı da veriyor** (T8.2). İlk kısılacak sayı bu
  olabilir. (Soru 2)
- **Toparlanma kilidi kalıcı HUD'da görünmüyor**, yalnızca debug metninde. Oyuncu kestiği süreyi
  göremiyorsa §5'in beceri ekseni görünmez kalır. (Soru 3)
- **Hece sesleri sinüs tıkırtısı**, müzikal kalite yok (§9 yapısı var). (Soru 3)
- **30 kapanış üst üste, gerçek oyunda hiç sayılmadı** — izole testte tavan doğrulandı. Telefonda
  30 kapanış yapıp yerdeki izin görsel yoğunluğuna bak. (Soru 4 + kare bütçesi)
