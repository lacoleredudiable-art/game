# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Bu dosyanın tek amacı, sıradaki
> ajanın repoyu taramadan nerede kaldığımızı anlaması. Kısa tut: ne bitti, ne üretildi,
> nerede sapma var.

> **16 Eylül 2026 — doküman sıfırlaması:** `dovus-sistemi.md`, `tasarim-ozeti.md`,
> `teknoloji-kararlari.md`, `his-kontrol-listesi.md`, `t0-kurulum.md`, `alis-sepeti.md`,
> `animasyon-omurgasi.md` **silindi** (beşgen/3-rün alfa prototipine aitti, altıgen/6-element
> sistemine geçildi, kafa karıştırıyordu). Bağlayıcı kaynak:
> `element-sistemi.json`. [Element Sistemi](element-sistemi.md) yalnızca insan-okunur
> açıklayıcı/tarihsel nottur. **Aşağıdaki eski oturum kayıtlarında** hâlâ "beşgen",
> "rün", ya da silinen dosyalara link geçebilir — onlar o an doğruydu, güncel mimariyi
> yansıtmazlar; körü körüne referans alma.

**Son güncelleme:** 30 Eylül 2026 (PR #33 Play testi geçti, master'a merge) ·
**Dal:** `master` · **Sıradaki:** 3-10 dash sonrası kapıdan geri dönüş

> **30 Eylül — PR #33 Play testi (`de4bbb0`): merge edildi.** Master (`9ce4f94`, #29) dala
> alındı; tek çakışma `durum.md` (iki taraf da tutuldu). `dotnet test` 532/532, `tools/AtomSim`
> ve GameCompile 0 hata, Unity derlemesi temiz, konsolda yalnız bilinen URP/ShaderGraph hataları.
> 4× tarama (`play-sweep/pr33-4x-r2.csv`): **Yumruk 144, Kalkan 143, Top 144, Kılıç 144**;
> `yerde` 576/576 (en kötü ayak 0,00 m), gövde 0, hata 0. Top 1-3, 6-3, 5-8, 6-8, 7-8 yerde geçti.
> Top 1-2 kalıp sonu merkeze 1,40 m (temas 1,35). Yumruk 1-4 / Kalkan 1-4 hasar verdi. Yumruk 2-2
> kalıp 2,20 sn, toplam 2,60 sn. Kılıç 10-2 3,22 sn (beklenen 3,18) geçti. Tek kalan Kalkan 3-6
> `sure`: kalıp bang'den 0,16 sn sonra (pay 0,15) — o kare MCP ilerleme sorgusuyla (editörde
> derleme) çakıştı; tek başına 3/3 geçti (kalıp 0,80 sn). En yakın merkez 3-2 `icinden_emme`
> 0,11–0,19 m (tasarım gereği geçiş, `govdeye_girmedi` geçti).

> **30 Eylül — tarama artıkları.** Top 1-2: Emici yerinde tutulan kalıba geri tepme eklenmez; geri çekilme boss yarıçapının dışında biter; tarama simülasyonu tutma bayrağını geçer. Yumruk/Kalkan 1-4: planlanan durak hem JSON kenarını hem kendi küresini ıskalıyorsa kapanış yalnız JSON kenarından (0,75 m); kılıç 1-4 ek hamle almaz. Yumruk 2-2: dört vuruş aynı anda ve zaman aşımı notu 11,3 sn (eşik 6,4) tek karelik saat sıçraması; dünya karesi en çok 100 ms. Unity Play / 1440 tekrar yok.
**Son güncelleme:** 30 Eylül 2026 (PR #29 Play testi geçti, master'a merge) ·
**Dal:** `master` · **Sıradaki:** 3-10 dash sonrası kapıdan geri dönüş; Top geri tepme fazında ayak havada (1-3, 6-3, 5-8, 6-8, 7-8)

> **30 Eylül — PR #29 Play testi (`8b104d5`): merge edildi.** Master (`4b17de0`) dala alındı;
> tek çakışma `durum.md` (iki taraf da tutuldu). `dotnet test` 527/527, `check.py` ve Unity
> derlemesi temiz, konsolda yalnız bilinen URP/ShaderGraph hataları. 4× tarama
> (`play-sweep/pr29-4x.csv`): **Kılıç 144/144, Asa 144/144**; `yerde` 0/0, gövde 0, sıçrama 0,
> hata 0. 9-10, 2-6, 11-8, 9-11, 9-12, 10-8, 10-9, 11-7, 1-3, 8-12 iki silahta geçti (9-10 ışını
> 8,34 m konumdan çıkarıldı, kalıp sonu merkeze 1,50 m). Gövdeye en yakın 3-2 `icinden_emme`
> 0,15 m (tasarım gereği geçiş). Elle (panel yöntemleri, 1×, Kılıç `[1,2,3,8,9,10]`): can %15 +
> 1-2 → aura kalıp başında açık, kalıbın 33/33 karesi boyunca açık (can 0,15→0,40, %20 t=0,46'da
> aşıldı), kalıp bitince 2. karede kapalı (can 0,40). 3-10: 2 bağlı kapı, geçiş iki yönde çalışır
> (5,75 m) ama dash bitince oyuncu geri döner (bkz. Bilinen açıklar). Dost ekle + 9-10: tam yer
> değişimi (0,00 m), dostun Y'si 1,00→1,00 (iki dostla ve yalnız panel dostuyla). 8-1: boss
> kapıdan geçince vuruş ölçeği 0,70, 2,02 sn. Kimse boss içinde değil (oyuncu en yakın 1,35 m,
> dost 1,85 m).

> **30 Eylül — Play kalanları (Kılıç 141 / Asa 142).** Master (`aaa81d4`, #32+#28) bu dala alındı.
> 9-10 ışını konum hesabından da düşülür (sıçrama zaten düşüyordu); yer değiştirmede herkes kendi
> zemin yüksekliğini korur. 2-6 kancası, kalıp dostun berisinde bittiyse oyuncuyu tekrar oturtmaz
> (ayak ~1 m bunu bozmuyordu). Dost yokken 2-6 boss'u hedefler. 11-8'de dostun tetik kapsülü
> motoru itmesin diye tetikler duvar sayılmaz. Sınır eşiği cast anında bakılır; aynı skill'in
> can çalması (%15→%31) aura'yı skill bitene kadar kapatmaz, bitince sonraki Tick bakar.
> `dotnet test` 527/527. Unity Play bu turda yok.

> **30 Eylül — tarama: 9-10 ışını işaretli, vakalar temiz başlıyor.** 9-10 yer değiştirmesi
> tek karede uzun bir ışındır; tarama yalnız bu işaretli kareyi (ve blink'i) sıçrama saymaz.
> Başka adım muaf değil. Kalıp bitince her skill oyuncuyu boss'tan itmiyordu artık — 1-3 Asa,
> 8-12 ve 2-6'nın fazladan sıçraması buydu. 2-6 kalıbı zaten doğru yerde biterse tekrar
> oturtulmaz. Her vaka kapı, sınır, takım ve dostun yerini sıfırlar; 9-10'dan sonra kayma kalmaz.
> `dotnet test` 480/480. Unity Play bu ortamda yok.

> **29 Eylül — Portal, Sınır modu ve Takım kombosu artık işliyor** (donmuş 144 kombo metni).
> Yeni dosyalar: `Core/Border/BorderMode`, `Core/Portal/PortalSystem`, `Core/Team/TeamComboSystem`
> ve `IAllyPlayer`. Sahneye `PortalBorderTeamHost` bağlar; Play'de sol üst **Takım dene** menüsü
> 1–4 sahte dost çıkarır. Hız, hasar ve can çalma mevcut borunun çarpanlarına eklenir.
> **Sapma:** 12-6 JSON "4 sn / self_haste 0" yerine metin (ip uzaklaşınca kopar, hız yedeği +%50).
> 8-1: boss kapıdan geçince 2 sn küçülür; vuruş yarıçapı, boyu ve eni ×0,7. Taret aralığı
> metinde yok, 1 sn. 7-6 zırh süresi `StatusTuning.ArmorBreakMs`. Kapı yarıçapı JSON
> `portal_trigger_radius_m` (1 m). Silah kesme silahlar dalında.

**Önceki:** 30 Eylül 2026 (birleşik test #32/#31/#29 → yalnız #31 merge) ·
**Dal:** `docs/combined-test-30sep` · **Sıradaki:** PR #28 (silahlar) testi; #32 ve #29 bulut ajanlarında düzeltiliyor

**Son güncelleme (master notu):** 30 Eylül 2026 (PR #32 ayaklar yere merge edildi; PR #29 portal/sınır/takım Play testi, merge yok) ·
**Dal:** `master` · **Sıradaki:** #29 Kılıç 9-10 / 2-6 / 11-8 + sınır–can çalma çakışması; Top 1-2, Yumruk/Kalkan 1-4, Yumruk 2-2

**Son güncelleme:** 30 Eylül 2026 (PR #33 silah tarama artıkları Play testi, merge yok) ·
**Dal:** `master` · **Sıradaki:** Top geri tepme fazında ayak havada (1-3, 6-3, 5-8, 6-8, 7-8); #29 Kılıç 9-10 / 2-6 / 11-8 + sınır–can çalma çakışması

> **30 Eylül — PR #33 Play testi (`cursor/silah-tarama-artiklari-a2ff`): merge edilmedi.** Master
> (`4b17de0`, #32 ayak + 4×) alındı. `GameClock`: önce gerçek kare 100 ms ile sınırlanır, sonra
> `SimulationScale` çarpılır. 4× tarama (`play-sweep/pr33-4x.csv`): **Yumruk 144, Kalkan 144, Top
> 139, Kılıç 143**. Dört hedef vaka geçti: Top 1-2 merkeze 1,35 m; Yumruk/Kalkan 1-4 hasar var;
> Yumruk 2-2 kalıp 2,20 sn. Kalan 5 Top `yerde` (1-3, 6-3, 5-8, 6-8, 7-8) geri tepmede ayak
> 0,26–0,83 m: yatay faz artık o karede zemine yapışır; hop/leap/slam/hover havada kalır, tarama
> onları atlar. Skill kimliği yazılmadı. Kılıç 10-2 `yutan_girdap` 3,39/3,18 sn: kalıp 3,18 sn,
> pay +0,20; aynı kalıp diğer silahlarda 3,20–3,28 ve geçer. Kılıca özel süre çarpanı yok;
> 4× kare artığı payı 0,01 aşıyor. Eşik gevşetilmedi. `tools/AtomSim` `SkillVisualTuning`'i
> link'liyor. `dotnet test` 498/498, AtomSim derleniyor. Unity Play bu düzeltmede yok.

> **30 Eylül — PR #32 ve #29 Play testi.** **#32 merge edildi** (`6cccf68`): master alındı
> (çakışma: `durum.md`, `PlaySweep.cs`, `ManifestationDirector.WeaponSwap.cs`,
> `MotionTemplateBody.cs`, iki taraf da tutuldu), `dotnet test` 493/493, derleme temiz. İlk 4×
> tarama 272/288'di: master'ın ertelenmiş son karesi (`_stopAfterSample`) sıçrama/süzülme
> kalıplarında yer ölçüsüne giriyordu → `7f12087` biten koşucunun karesini `yerde`den çıkarır.
> Sonra Kılıç 144/144, Asa 144/144, `yerde` 0/0, 7-3/7-6/7-11 ayak 0,00, gövdeye girme 0.
> Elle (Kılıç `[3,6,5,2,1,4]`): 3-6 çekme → 5-2 Emici → 1-2 → 1-4, zemin karelerinde ayak
> ±0,001 m, bitişten 0,3 sn sonra ±0,001 m, boss'a en yakın 1,35 m (yarıçap 0,85).
> **#29 merge edilmedi** (`feat/portal-border-team`, dala push yok): master + #32 alındı
> (çakışma: `durum.md`, `ActorStatus.cs`), `dotnet test` 523/523, derleme temiz. 4× tarama
> **Kılıç 141/144** (kural ≥142), Asa 142/144; `yerde` 0/0, gövde 0, sıçrama 0. Kalanlar: 9-10
> iki silah `konum` (ışın tek karede 8,34 m, sınır 2,51 m), 2-6 iki silah `tek_sistem` (kalıp
> hedefi AllyDummy, kalıp 3,40 m + başka 0,48 m), 11-8 Kılıç `tek_sistem` (başka 0,18 m, sınır
> 0,15). 9-11, 9-12, 10-8, 10-9, 11-7, 1-3, 8-12 iki silahta geçti. Takım elle: can %15 + 1-2 →
> sınır **yalnız 1 kare** açık (1-2 can çalması canı %15→%31'e çıkarıyor, eşik 0,20 → Tick
> kapatıyor); can %80 → kapalı. 3-10: 2 kapı, oyuncu 1,50 m. Panel dostu + 9-10: tam yer
> değişimi (0,00 m) ama dostun Y'si 1,00→0,00. 8-1: boss kapıdan geçince vuruş ölçeği 0,70,
> 2,01 sn. Kimse boss içinde değil (en yakın 1,35 m).

> **30 Eylül — boss 5 cm havada.** 4× taramada yalnız 7-3, 7-6, 7-11 (iki silah, iki koşu).
> Ayak kilidi görselin lossyScale'ine bölüyordu; boss kapsülünün Y ölçeği 1,3 ve görsel fit'i
> büyük olduğu için düzeltme eksik kalıyor, stagger pozu ~5 cm havada duruyordu. Kayma artık
> ebeveyn ölçeğiyle. Kaldırma yüksekliği iç sayaç: sıfırın altına inince kapsül zeminine yapışır,
> Home Y havadaki örnekten yükselmez. Eşik gevşetilmedi. Unity Play bu düzeltmede yok.

> **30 Eylül — PR #28 Play testi (`b45ee56`, master + #31 alınmış): kural gereği merge edilmedi, sahip
> kararıyla merge edildi (`674d889`); Top 1-2 açığı master'da.** `dotnet test` 485/485,
> `check.py` geçti, Unity derlemesi temiz, konsolda yalnız bilinen URP/ShaderGraph paket hataları.
> Tüm silahlar 1× (4× #32 ile geliyor, merge edilmedi), iki parça: ilk 1261 vaka + kalan 179
> (Kitap 35, Küre 144; ilk koşu Play kapanınca kesildi). **1436/1440:** Yumruk 142, Kalkan 143,
> Top 143, diğer yedi silah 144. Kalanlar: (1) Yumruk ve Kalkan 1-4 hasar/etki yok (boss 2,42 m).
> (2) Yumruk 2-2 zaman aşımı (kalıp 10,9 sn, beklenen 2,2). (3) **Top 1-2:** oyuncu kalıp
> simülasyonundan 1,5–1,6 m farklı yerde biter (boss ~2,1 m çekilir); tam taramada bir kez
> **gövdeye girdi** (merkeze 1,03 m, temas 1,35, t=1,38). 3 tekrar: konum 3/3 kaldı, gövde 0/3.
> Skor kuralı tuttu, "oyuncu boss içinde değil" kuralı tam taramada bir kez bozuldu → merge yok.
> Kısa elle kontroller bu turda tekrar koşulmadı (aşağıdaki `4de6aba` ölçümleri duruyor).

> **30 Eylül — tarama artıkları (`hepsi.csv` 1395/1440).** Top'un 0,5 m geri tepmesi hasarlı kalıbın son fazı (`geri_tepme`, 0,12 sn); bu faz varken oyuncuya ayrı konum yazılmaz. Düz top atışı ışının ucunda değil, ilk değdiği boss yüzeyinde patlar ve iter. Yumruk/Kalkan menzil kapısı sessizce yutmaz: menzil dışı hedef kurulur, eksik yol kalıbın başında `kapan` hamlesidir. 6-2 geri adımı cast başındaki menzili silmez. Küre 8-11 kalıbı tek karede bitince tarama onu ikinci kayma sanıyordu; bitiş bir sonraki karenin başına kalır. Yumruk 2-9'da koşucu tarama saatinden geri kalırsa yetişir (0,51 sn → kalıp süresi). Yumruk 2-6 kalıbı zaten 0,52 sn; vaka 0,87 sn'de boşta olup saat 9,6 sn'de kesildi. Kancayı `distance_m` ile kesmek 6-1'i de kısaltır, yapılmadı. Küre basılı tutma / çift dokunuş dinlemez: HUD silah düğmesi (editörde R) eldeyse hedefe yollar, değilse çağırır; yol 0,4 sn. Çizim alanına dokunmaz. Q silah değiştirir. Unity Play bu ortamda yok.

> **30 Eylül — on silah pasifi bağlandı.** Kılıç yayı yakın vuruşta açı kontrolü (144°, dost fiilde yaydaki herkes; swap 180°). Büyü Kitabı sayacı hasardan önce artar, ara dolunca sıfırlanır. Çekiç sersem süresi JSON'dan, 4 sn bekleme yalnız sersem oturunca. Tılsım şifa/kalkan/buff ×1,2, fiil uyumu aranmaz. Kalkan +%20 ilk skill vuruşunda biter. Asa süre uzayınca aynı dilimle yeni vuruş ekler. Çekiç poise ve silah poise çarpanı giden poise'a girer. Yumruk sırt çarpanı yalnız `sirt_vurusu`; gramerde ikinci `arkadan_x1.5` yok. Rün yuvası pasifine dokunulmadı. Unity Play yok.

> **30 Eylül — PR #28 Play testi (`4de6aba`, pasif commit'inden önce): merge edilmedi.** Unity derlemesi temiz, `dotnet test` 455/455,
> konsolda yalnız bilinen URP/ShaderGraph paket hataları. Tüm silahlar taraması (`hepsi.csv`):
> Kılıç, Çekiç, Yay, Asa, Tılsım, Büyü Kitabı 144 · Küre 143 · Kalkan 139 · Yumruk 136 · Top 113.
> Hiçbir kombo sonunda oyuncu boss içinde değil, sıçrama yok. **Hata nedenleri:**
> (1) Top 31 vaka: top geri tepmesi (0,50 m) tarama "ikinci hareket sistemi" sayıyor — kural mı
> yanlış, geri tepme mi şablona girmeli, karar gerekli. (2) Yumruk ve Kalkan'da 1-3, 1-4, 5-1,
> 5-4, 7-4 cast menzil kapısında reddediliyor (3 m başlangıç). (3) Yumruk 2-6 zaman aşımı ve
> 2-9 süre aşımı (şablon uzun). (4) Yumruk 6-2 hasar/etki yok. (5) Küre 8-11 konum: şablon dışı 1,41 m hareket.
> **Elle (harness, gerçek dokunma/F2 tuşu enjekte edilemedi, yöntem çağrısıyla):** kurulum ekranında
> 10 silah seçilebiliyor, 5 çiftin hepsi uygulandı; F2 (`CycleWeapon`) 10 silahı sırayla geziyor;
> değiştirme 251 ms. Silah kesme: 1-1 %90'ında değiştirince şablon kesiliyor, sıradaki 1-8 hemen
> kabul (0 ms / değiştirmeden 68 ms), vuruş 500 ms / 567 ms. Küre: 0,45 sn basılı = yerleştir,
> 0,25 sn = yok, çift dokunma = geri çağır; 0,4 sn'de noktaya gidiyor, 2 sn beklemeden sonra
> ele dönüyor. Top skill vuruşu: boss 0,50 m, küçük hedef 3,00 m, oyuncu ~0,5 m geri, boss'a en
> yakın 1,50 m. Kalkan: değiştirmeden sonraki ilk vuruş 3 sn kalkan kuruyor, hasar emiliyor.
> Tılsım: ilk vuruş boss'tan bir olumsuz etki siliyor. Kitap: ilk cast mana harcamadı (100→100),
> ikinci 12 harcadı (Kılıç 12). Hasar: 1-9 246–256K (kritik 359K), 5-9 168–173K (kritik 270K);
> 7-6 sonrası ilk düz vuruş 28K, 7-9 sonrası 32K.

> **30 Eylül — silahlar hasar borusuna bağlandı.** Master (PR #27 dodge, PR #30 boru/zırh/×4000) bu dala alındı. Kalkanın 15 puanlık kısa kalkanı dodge'dan sonra borunun son kalkan aşamasında erir. Silah `base_armor` oyuncunun zırhına gider (Kalkan 25, Çekiç 15, Top 10). JSON `ignore_armor` artık zırhın tamamını silmez, delme %50'de kalır (1-9 ve 5-9). Zafiyet'ten sonraki ilk düz vuruş, kilit boşsa ya da etki bang'den önce yok olduysa da menzildeki boss'a vurur. Tarama: sıçrama sınırı 25 m/s × kare + 0,2 m; 1-2 ve 4-2 yerinde; menüde 10 silah ve tüm silahlar. Unity Play bu ortamda yok.

**Son güncelleme (master notu, eski):** 30 Eylül 2026 (PR #27 + #30 master'a merge) ·
**Dal:** `master` · **Sıradaki:** yeni sıçrama ölçüsüyle Kılıç/Asa taramasını bir kez daha koş

> **30 Eylül — birleşik test: yalnız PR #31 merge edildi (`8686a72`).** Kılıç + Asa Play taraması:
> üçü birlikte (#32+#31+#29, 4×) 132/132; yalnız #32 (4×) 141/141; #31+#29 (1×) 135/134;
> yalnız #31 (1×) **144/144**. `dotnet test` her kombinasyonda yeşil (#31 tek: 450/450), konsolda
> yalnız bilinen URP/ShaderGraph paket hataları, oyuncu hiçbir vakada boss içinde bitmiyor
> (3-2 tasarım gereği içinden geçer). **#31 Play:** pasif [1,9], 1-4 skill'i 1,6 m'den, taban
> 159K; Yoğunlaştırma aktif ×1,38 (JSON ×1,35), Odaklama ×1,62 (×1,15 × %50 delme ≈ ×1,53, ±%5
> sapma), ikisi ×2,04. **#32 atlandı:** 7-3/7-6/7-11 boss yerden 0,05 m (silah başına 3 yerde
> hatası, kural ≤3 tutuyor ama skor 141). **#29 atlandı:** 9-10 takası tek karede 4,66 m + sonraki
> 6 vaka kaymış konum, 2-6/1-3 `tek_sistem`, 8-12 tek karede 2,25 m. Ayrıntı PR yorumlarında.
> #29 master'a birleşirken `ActorStatus`/`ManifestationDirector` çakışır; takım çarpanları
> `ComputeOutgoingHit` ve `ApplyDamage` içindeki `taken`/`outMult`'a taşınmalı. Takım paneli ve
> yere basma elle kontrolleri koşulmadı.

> **30 Eylül — ayak zemini, tarama 4×.** Skill kökü cast başındaki Y'yi zemin sanıyor, Emici
> çekme kökü mutlak `y=0`'a indiriyordu (kapsül merkezi ~1 m; ayaklar zeminin altına).
> Hover/channel bitince yükseklik sonraki cast'in zemini oluyordu. `Grounding` + `VerticalCurve`:
> havadaki faz (`hop/leap/slam/hover`, yüksekliği olan `channel`) eğriyi izler, bitiş ve dodge /
> silah değişimi / sersemletme / ölüm kısa smoothstep ile çakılı zemine iner. Görsel local Y her
> kare spawn ofsetine döner, en alçak ayak o zemine kilitlenir. Boss ve dost dummy aynı
> bileşende. Play Sweep `yerde` sütunu: havada değilken ayak payı 0,05 m × (kare / 1/60);
> bitişten 0,30 sn sonra 0,03 m (hızdan bağımsız). Menü `Hız/4x` varsayılan, `Hız/1x` ayıklama.
> `Time.timeScale` ve `GameClock.SimulationScale` aynı çarpan; `fixedDeltaTime` 1× adımında
> kalır. Cast arası bekleme iki fizik adımı. `1440 kombo - Tüm silahlar` katalogdaki silahları
> tarar. Unity Play bu turda koşulmadı.

> **30 Eylül — pasif yuva (0–2) çalışır.** Tetikleyen cast pasifi kendine uygulamaz.
> Bağlama yavaşı `1 − apply_slow`. Sabitleme oyuncuyu köklemez; sonraki vuruşlara poise ×1.5
> ve alan/zone ömrüne `lifetime_add` ekler. Poise çarpanı giden vuruşta. Odaklama süresi 5 sn,
> `ignore_armor` = %50 delme. Kopyalama süresi 5 sn, sonraki cast 0,3 sn sonra bir kez %50 yankı.
> Sıçrama 6 m içinde seker; yalnız boss varsa `bounce_targets` kadar ek vuruş. Akış vuruşu
> `channel_sec` / `tick_rate_mult` / `flow_tick_fraction` ile DoT bırakır (−%30 tek başına kalmaz).
> `passive_slot_system` silah uyumu istemez. Eski `PassiveDirector` ölü; diriltilmedi.
> Unity Play yok.

> **30 Eylül — karar: 3-6 kare takılması.** `PlaySweep` sıçrama sınırı artık sabit 0,6 m değil:
> 25 m/s × kare süresi (en az 1/60 sn) + 0,2 m (60 FPS'te 0,62 m, 38 ms'de 1,15 m). Oyuncu ve
> boss için aynı. PR #27 ve #30 master'a merge edildi. Yeni ölçüyle Play taraması koşulmadı.

> **30 Eylül — PR #30 ikinci Play testi: Kılıç 143/144, Asa 143/144. Merge edilmedi.**
> `dotnet test` 434/434, Unity derlemesi temiz; konsolda yalnız bilinen URP/ShaderGraph paket
> hataları. Oyuncu hiçbir kombo sonunda boss içinde değil. Tek hata iki silahta da **3-6**:
> `as` fazında tek karede 0,73 m sıçrama (araç eşiği 0,6 m). Yay 19,2 m/s, 60 FPS'te kare
> başı 0,32 m; 0,73 m ≈ 38 ms'lik bir kare. İlk Kılıç koşusunda 3-6 geçti, 1-6 (yavaş
> editör karesi) kaldı; 3×(3-6 Asa + 1-6 Kılıç) mini koşu 6/6 geçti. Yani takılan karede
> çıkıyor ama iki tam koşuda aynı yerde tekrarladı; 30 FPS'te bu yay kare başı ~0,64 m olur.
> Kural "tek kare sıçrama yok" dediği için merge yok — karar sahibinde (eşik mi, yay hızı mı).
> 1-2, 4-2, 5-2, 10-2, 11-2, 12-2 iki silahta "yerinde", başlangıçtan 0,00 m, sıçrama yok;
> 3-2 arkaya iniyor, 6-2 geri. **Elle:** boss'a doğru dodge 1,5/1,8/2,0/2,2 m'den hep yakın
> yüzde 1,50 m'de duruyor (temas 1,35); en büyük kare adımı 0,61 m @17 ms — açık alandaki
> normal dodge adımı 0,62–0,66 m, fazladan atlama yok. Boss zırhı 100. Düz vuruş 23,8–25,8K.
> Skill: 1-1 205–221K, 1-9 361–371K ve 5-9 251–269K (ikisi de `zirh_yoksay`, ×2 zırhsız →
> bant üstü), kritik 5-9 394K. 7-6: 0,6 sn'de zırh 100→70, düz vuruş ~24,7K→28,6K (+%16).
> 7-9: 2,27 sn'de 100→50, düz vuruş →32,7K (+%32), 1-1 ~213K→292K (+%37). Zafiyet
> skill'inden hemen sonraki ilk düz vuruş 0 hasar verdi (ok=True), ikincisi normal.
> Bu makinede editör boşta kalınca kareler yavaşlıyor; tarama sırasında sistem uyanık tutuldu.

> **30 Eylül — Play kalanları, kod.** Dodge boss'a doğru atılınca yakın yüzde durur
> (`DodgeEdge.StopBeforeCrossing`); uzak yüze ışınlanmaz. Atış, tutma ve dönüş fazları
> kenar itmesiyle yer değiştirmez; yürüyen kalıbın kendi adımı durur. 5-2, 10-2, 11-2,
> 12-2 boss oyuncunun içinden geçse de oyuncu yerinde kalır. 1-2 ve 4-2
> oyun davranışı aynı (oyuncu yerinde, boss temasa gelir); tarama beklentisi artık "yerinde".
> Boss zırhı JSON: normal 100 (%50), sert 150 (%40 alınır). Canlı dövüş normal zırhı okur;
> sert mod anahtarı yok. Skill gücü zırhtan önce ×2 (`SkillPreArmorScale`) — 175–222K bandı
> durur, delme ve kırılma sonra işler. Düz vuruş tabanı `BasicStrikePower` 12,5 → zırh sonrası
> ~25K (20–30K). Zafiyet 7-6 −%30, 7-9 −%50 yalnız zırh puanını düşürür; `armor_break` ×1,2
> artık üstüne binmez. `dotnet test` 434/434, `python3 tools/GameCompile/check.py` geçti.
> Unity Play bu düzeltmelerde yok.



> **30 Eylül — PR #30 Play testi: Kılıç 138/144, Asa 140/144. Merge edilmedi** (kural 142+).
> `dotnet test` 429/429, Unity derlemesi ve konsol temiz. Master'da son koşu 142/143 idi.
> **Yeni kalanlar (Emici):** 5-2 (iki silah) ve 11-2 (iki silah) `cek`/`em` fazında oyuncu
> tek karede ~2,93 m sıçrıyor; 10-2 Kılıç aynı (faz `yut`); 12-2 Kılıç "yerinde" yerine
> "geri" bitiyor. Hepsinde isabet merkezi boss'ta (0,00 m), oyuncu 1,50 m'ye oturuyor.
> **Araç beklentisi eski:** 1-2 ve 4-2 (iki silah) oyuncu yerinde (0,00 m), boss temasa
> (1,35 m) geliyor — PR'ın istediği bu, ama araç kalıp simülasyonuna göre "ön" bekliyor.
> 3-2 içinden geçip arkaya iniyor (geçti), 6-2 Kılıç sıçrama yok (geçti).
> **Elle ölçüm:** dodge çubuk yönüne ~4,1 m (kayıtlı ayar 3,8 m + süzülme), çubuksuz geri;
> 2 hak, 3. basış reddedilir, hak 4,0 / 8,0 sn'de dolar; skill ve düz vuruşu keser (hasar 0);
> Stun/Stasis'te dodge yok. Boss'a doğru dodge hiç içeride bitmiyor (≥1,50 m, temas 1,35)
> ama 1,5–2,2 m'den atılınca boss'un **üstünden tek karede 2,6–3,0 m atlayıp** arkaya iniyor.
> F8: dodge'suz 88.000 hasar; basış→vuruş 249 ms hasar 0, PERFECT yok; 67/116 ms PERFECT,
> hak geri, sonraki vuruş ×1,30 bir kez (hasar alınca da korunur); 450 ms geç → 88.000.
> Can: boss 88.000.000, oyuncu 400.000. Yüzen sayılar: düz vuruş 13,5–14,5K (beklenen
> 20–30K değil), Zafiyet vuruşu 18–22K, skill 175–222K; kritik "279.7K!" altın, 32 px
> (normal 25 px); oyuncuya "88K" kırmızı; iyileştirme "+32K" yeşil. M biçimi görülmedi
> (1M üstü vuruş yok). Bu makinedeki kayıtlı ayarda `ShowDamageNumbers` kapalı — ölçüm
> için oturumda açıldı. **Zafiyet:** 7-1/7-9 sonrası boss `ArmorBreak` → gelen hasar ×1,20,
> sonraki 1-1 ~×1,2. Zırh kırılması (−%18/−%22) ise etkisiz: boss zırhı JSON'da 0,15, LoL
> formülünde bu ~%0,15 azaltma demek (zırh birimi uyuşmuyor, bkz. Bilinen açıklar).

> **29 Eylül — hasar ve zırh.** Bütün vuruş, zehir ve boss hasarı tek borudan geçer:
> güç × saldırı, kritik (varsayılan %10, ×1,5), buff'lar bir kez, zırh delme, LoL zırhı,
> en fazla %90 azaltma, ±%5 sapma. İyileştirme zırha girmez. Tehdit değeri her olayda durur.
> Can ve hasar JSON'u bozmadan ×4000 (oyuncu ~400 bin, boss ~88 milyon; 150–250 milyon
> hedefi sonra `max_hp` ile). Yüzen sayılar K/M, havuzlu. Zırh: silah alanı (şimdilik 0),
> pasif, geçici buff. Zafiyet (7-6, 7-9) motor değeri −%20 zırhı gerçekten düşürür;
> karttaki “−%30” metni değişmedi. Emici: 3-2 hâlâ içinden geçer, 1-2 ve 4-2 yerinde kalıp
> boss'u çeker, 6-2 geri çekilirken tek karede yığılmaz. Unity Play bu turda yok.
> Dodge i-frame'i borudan önce yutar (`ActorStatus` ve `PlayerVitals`). Mükemmel sıyırmanın
> sonraki vuruş çarpanı buff aşamasında bir kez tüketilir; ıskalama ve yenilmezlikte harcanmaz.
> `dotnet test` 429/429, `python3 tools/GameCompile/check.py` geçti. Unity Play yok.

> **Dodge (Dragon Nest tarzı).** Kısa kayma ~4 m / 0,25 sn, dokunulmazlık dodge başında ~0,3 sn.
> 2 hak, her biri ~4 sn'de dolar. Skill ve düz vuruş her an kesilir. Mükemmel sıyırma sonraki
> vuruşu ×1,3 yapar. Deneme: Play'de sol üst "Dodge deneme" veya F8. `BossTuning.Damage` hâlâ 0.
> Unity Play doğrulanmadı.

> **30 Eylül — dördüncü koşu: Kılıç 142/144, Asa 143/144. Merge edilmedi.** `feat/motion-templates-2`
> (`b9df90b`) merge edildi. `dotnet test` 402/402, Unity derlemesi ve konsol temiz (yalnız bilinen
> URP/ShaderGraph paket hataları). **Araç düzeltmesi:** Emici çekmeleri boss'u her vakada oyuncuya
> taşıyor, kayma birikiyordu (Asa taramasının sonunda boss z=-25, dost başlangıç yerinde, 24 m uzakta;
> 2-6 Asa "20–46 m uçuş" buydu). Araç artık her vakadan önce boss'u oturum başındaki yerine koyuyor.
> Düzeltme öncesi koşu Kılıç 141, Asa 140 idi.
> **Kalanlar:**
> - **3-2 Emici Adım (iki silah):** oyuncu hiç kıpırdamıyor (başlangıçtan 0,00 m), kalıp içinden
>   geçip arkaya inmeli (simülasyon farkı 5,5 m). "Emici çekmede oyuncu yerinde" kuralı bu kalıbı da
>   durduruyor gibi. Boss 1,10 m geliyor.
> - **6-2 Emici Bağ (Kılıç):** `cek` fazında oyuncu tek karede 0,66–0,67 m sıçrıyor (t=0,82);
>   tam taramada ve iz koşusunda tekrarlandı. Merge kuralını bu bozdu.
> - İlk koşuda 5-10 bir kez kalıbı 7,6 sn sürdü (beklenen 0,36); iz ve ikinci koşuda geçti: kararsız.
> **Hızlı kontroller (iz `kontrol-detay.txt`):**
> - **1-2 / 4-2 (iki silah):** çakışma ve itme yok, geçiyor. Ama oyuncu yerinde kalmıyor: 1,5–1,65 m
>   ileri yürüyor, boss kıpırdamıyor. 4-2 Kılıç izinde kalkan etkisi görülmedi (tam taramada geçti).
> - **Asa 2-6:** boss'un içinden geçmiyor, sıçrama yok, dostun yakınında bitiyor (3,40 m yol, dost
>   başlangıca ~3,8 m). Yere inme doğrudan ölçülmedi (iz Y yazmıyor).
> - **Asa 3-3 (Kılıç'tan hemen sonra silah değişimi):** arkaya iniyor, simülasyon farkı 0.
> - **Dash pozu:** hızlı fazlarda koşu yok (3-6: 0/35 kare koşu); Kılıç `CastPierce`, Asa `CastShoot`
>   pozu 1× tutuluyor.

> **29 Eylül — Play taraması üçüncü koşu: Kılıç 141/144, Asa 140/144.** `feat/motion-templates-2`
> (`00ee852`, Emici çekme oyuncuya, 3-3 arkaya) merge edildi. `dotnet test` 399/399, Unity derlemesi
> temiz. **Araç düzeltmesi:** önceki Emici kombosunun boss çekmesi (`BossReactor.PullActive`) bir
> sonraki vakaya taşınıyordu (ilk karede boss 0,95 m'de, 4-12 10,5 sn zaman aşımı). Araç artık
> çekme bitene kadar bekliyor. Düzeltme öncesi koşu Kılıç 140, Asa 137 idi. İz: `kontrol-detay.txt`.
> **Kalanlar (skill koduna dokunulmadı):**
> - **1-2 Emici Vuruş (iki silah):** boss oyuncuya çekilirken oyuncu 2. ve 3. pençede geri geri
>   yürüyor (oynatma eksi), başladığı yere dönüyor (kalıp "ön", gerçek "başlangıç"). Kalıbın durma
>   payı, yaklaşan boss'tan kaçıyor gibi. Vuruşlar değiyor (38 hasar).
> - **2-2 Emici Şifa (iki silah):** oyuncu 0,15–0,30 m kıpırdıyor, simülasyon 0,5–0,65 m ileri diyor.
>   Simülasyon boss'u sabit sayıyor; boss 1,65 m yaklaşınca adım gerekmiyor. Büyük ihtimal araç
>   beklentisi, oyun hatası değil. İz koşusunda geçti.
> - **4-2 Emici Kalkan (iki silah):** boss oyuncunun içine giriyor (Asa merkeze 0,57 m, temas 1,35)
>   ya da oyuncuyu 0,87 m itiyor (Kılıç). Muhtemel kök: temas noktası vuruş anındaki oyuncu yerine
>   göre sabitleniyor, kalıp oyuncuyu ileri taşımaya devam ediyor. İz koşusunda geçti: kararsız.
> - **2-6 Bağlayıcı Şifa (yalnız Asa):** dost kancası oyuncuyu dostun (boss'a 2,8 m) 17–46 m ötesine
>   fırlatıyor, boss'un içinden geçiyor (0,5–0,9 m), tek karede 1–2,5 m sıçrıyor. Bitişte oyuncu
>   Y=5 m havada kalmıştı. Kılıç'ta geçiyor.
> **Elle kontrol (Kılıç izi):** 5-2 / 12-2 / 2-2 / 6-2'de boss oyuncuya 0,32–0,67 sn'de, kare başı
> en çok 0,09–0,13 m ile geliyor, uzağa itme yok; vuruş iniyor (5-2 26,6, 6-2 6,1 hasar + Root). 3-2
> tek kare sıçrama yok, boss 0,69 m çekildi. Asa 3-3 tam taramada geçti (arkaya iniyor), iz koşusunda
> silah değişiminin hemen ardından önde bitti: kararsız. Yeni kod: girdap hacmi `bossInside`
> şartı olmadan her tikte boss'u oyuncuya çekiyor (`ManifestationDirector.MechanicWorld.cs`).
> **Bacaklar:** 3-6 ayak/gövde oranı artık 1,00 (Kılıç ve Asa). Ama hızlı atılmada koşu klibi
> (0,733 sn, 2 adım) 7–10 kat hızlı oynuyor: 3-2 Kılıç ort. 9×, 3-6 8,8× → saniyede ~24 adım,
> 60 fps'te her 2–3 karede bir adım. Gözle saçma/bulanık görünür; atılma pozu gibi bir karar gerekir.

> **29 Eylül — Play taraması ikinci koşu: Kılıç 141/144, Asa 142/144.** `feat/motion-templates-2`
> (`8bdbd3c`) bu dala merge edildi, Unity derlemesi temiz (konsolda yalnız bilinen URP/ShaderGraph
> paket hataları). Araç düzeltmesi: `MotionTemplateBody` / `SkillMotionDriver` artık sahne
> bağlanırken, cast anında ve her örnek karede yeniden aranıyor (build ekranı açıkken `_body` boş
> kalıyordu, 1-1 yanlışlıkla kalıyordu). Yeni menü `Dovus/Play Sweep/144 kombo - Asa`.
> Tablolar `docs/play-sweep/kilic.csv`, `asa.csv`; kalanlar `*-detay.txt`; 7 kombonun kare kare izi
> `kontrol-kilic-detay.txt`. 1-1 artık geçiyor (kalıp oynadı, simülasyon farkı 0).
> **Kalanlar (skill koduna dokunulmadı):**
> - **3-2 Emici Adım (Kılıç + Asa):** oyuncu boss'un içinden geçerken (merkeze 0,09 m) boss tek
>   karede 1,45 m dışarı fırlıyor. `BossReactor.AdvancePull` → `KeepSeparated` her kare boss'u
>   oyuncudan temas mesafesine itiyor; içinden geçen kalıpta bu bir ışınlanma. Tam taramada bir kez
>   oyuncu da tek karede 7,79 m sıçradı (t=0,62), iz koşusunda tekrarlanmadı.
> - **6-2 Emici Bağ (Kılıç):** boss oyuncudan **uzağa** 3,25 m kayıyor (merkez 3 → 7,85 m),
>   vuruş boşa düşüyor (hasar 0). Plan `cek düşman [sana_dogru]` diyor. Asa'da 0,53 m uzaklaşıyor, geçiyor.
> - **5-2 Emici Patlama (Kılıç):** tam taramada boss 2,52 m kaydı, oyuncu 1,02 m geri itildi
>   (konum yerinde→geri). İz koşusunda boss kıpırdamadı, geçti: kararsız.
> - **12-2 Emici Zaman:** geçti ama boss oyuncudan 3,6 m uzağa gidiyor (merkez 3 → 6,63). Muhtemel
>   ortak kök: girdap merkezi boss'un ötesinde, çekme `volume.Center`'a gidiyor → oyuncudan uzağa.
> - **3-3 Sıçrayan Adım (yalnız Asa):** konum arka bekleniyor, önde bitiyor; kalıp simülasyonu da
>   önde (fark 0,11 m). Araç beklentisi tasarım etiketinden (`land: behind`) geliyor; Asa'da kalıp
>   arkaya ulaşmıyor olabilir.
> **Elle kontrol:** 3-6 artık boss merkezinden geçmiyor (en yakın 1,50 m ≥ temas 1,35), ama ayak
> hâlâ kayıyor: ayak/gövde oranı 0,43 (önce 0,24–0,36; 1,0 ideal). 3-2 çekme yumuşamadı (yukarı).
> 3-12 titreme yok (0 ileri-geri, simülasyon farkı 0). 2-2 boss'a 26,6 hasar, oyuncu +11, dost +11.
> Asa'da toparlanma 0,46–0,53 sn (tablo 0,26), süre kontrolünü geçiyor, bilgi.

> **29 Eylül — Play taraması (Bölüm B, ilk koşu): 103/144 geçti (Kılıç).** Araç: menü `Dovus/Play Sweep/144
> kombo - Kılıç` (`Game/Editor/PlaySweep.cs`). Boss durdurulmuş, merkez 3 m, her cast öncesi
> status/bağ/bölge temizlenir, can/mana dolar. Kontroller: cast, isabet, kalıp sonu konumu
> (kalıbın çevrimdışı simülasyonuna göre), gövdeye girmedi, tek sistem, konsol hatası, süre, tek
> kare sıçrama/titreme. Tablo `docs/play-sweep/kilic.csv`, kalanlar `kilic-detay.txt`.
> **41 kalanın 6 kökü var, konsol hatası 0:**
> - **27 kombo — kalıp hedefi oyuncunun kendisi.** Kendine/dosta skill'de `_templateAim =
>   pending.Target` = Player. Kalıp kendinden 1,15 m (0,5+0,5+0,15) uzak durmaya çalışır, her kare
>   1,15 m sıçrar. 3-12 titremesi budur. 3-3, 3-7, 3-8, 3-12, 4-8, 4-9, 8-8…8-12, 9-7…9-12, 10-5,
>   10-7…10-12, 11-6, 11-8, 11-10, 11-11. Bazıları boss'un içine girer (3-3, 3-7, 3-8, 8-12, 9-10…).
> - **4 kombo — Emici (sıfat 2) çekme boss'u tek karede ışınlar:** 3-2 (3,00 m), 5-2 (1,24),
>   6-2 (3,26, isabet de yok), 12-2 (3,64). Kural 4 ihlali.
> - **4 kombo — 3 m'de cast reddedildi (menzil kapısı):** 1-3, 1-4, 5-1, 6-1.
> - **3 kombo — 3 m'den isabet yok:** 1-5 (dönüş yerinde, menzil 1,6 m), 7-1, 7-5.
> - **2 kombo — boss merkezinden geçer:** 3-6 aş fazı (0,06 m), 6-12 (0,11 m).
> - **1 kombo — oturumun ilk cast'i kalıbı atlar:** build ekranı kapanır kapanmaz 1-1 kalıpsız,
>   gramerle 1,5 m kayar. Aynı oturumda tekrar atınca geçer.
> Araç notları: açık build ekranı saati durdurur (araç önce build'i uygular). Play açılışında bir
> paket PlayerLoop'u sıfırlıyor; araç tick'i `EditorApplication.update` ile geri takar. Editör
> odakta değilken tarama yavaşlar. Play sırasında script kaydetmek taramayı siler (domain reload).

> **29 Eylül — üçüncü tarama (Kılıç 141/144, Asa 140/144).** Emici çekme sürerken kalıp hedefe
> yürümez; oyuncu yerinde vurur, boss gelir. Varış her kare oyuncunun o anki temas noktasıdır,
> iç içe geçmez, oyuncu itilmez. Girdap yalnız içindeki boss'u çeker (`bossInside` geri geldi).
> 2-6 dost kancası dostun yakın kenarında, yerde biter; silah menzili uzatamaz, boss'un içinden
> geçmez. Koşu klibi en çok 2×; üstünde `dash_fast` (CastPierce, `anim_bridge`) tutulur.
> 3-3 arkaya iniş cast başlangıcına göredir; silah çarpanı ve swap yolu değiştirmez.
> `dotnet test` 402/402. `python3 tools/GameCompile/check.py` geçti. Unity Play yok.

> **29 Eylül — ikinci tarama kalanları.** Kılıç 141/144, Asa 142/144 sonrası. Emici çekme artık
> girdap merkezine değil, oyuncunun önündeki temas noktasına gider (kenar kenara, 0,4 sn).
> Çekme sürerken KeepSeparated yok; 3-2'de boss tek karede dışarı fırlamaz. Hedef ışınlanırsa
> nişan en çok 40 m/s kayar (3-2'deki 7,79 m oyuncu sıçramasının yolu). 3-3 son sıçrama arkaya
> iner; silah menzili kalıp mesafesini kısaltamaz. 3-6 bacak oynatması tavana 12 kata çıktı.
> `dotnet test` 399/399. Oyun katmanı derlendi. Unity Play yok.

> **29 Eylül — Play sweep kökleri (kod, Unity Play yok).** Kılıç taramasındaki 41 kalanın
> kökleri bu dalda ele alındı. Kendine/dosta giden 27 kalıp artık atıcıyı duruş hedefi yapmıyor
> (dost, düşman, bakış veya hedefsiz); 3-12 titremesi bu yüzden. Emici çekme (3-2, 5-2, 6-2, 12-2)
> tek karelik ışın yerine 0,4 sn smoothstep; stasis ve saldırı kilidi bağışıklığı keser, gövdeler
> iç içe geçmez. Menzil kapısı JSON kenar menzili + kapanış mesafesi (1-3, 1-4, 5-1, 6-1, 3 m).
> 1-5 / 7-1 / 7-5 isabeti JSON hitbox'ı veya uçuş hattı. 3-6 ve 6-12 arkaya iniş boss'un çevresinden
> yay çizer. 2-2 emme boss'a planındaki eksi canı yazar, iyileştirme aynı paydan gelir.
> 3-6 bacak sönümü kapatıldı (ayak kayması Play'de bakılacak). İlk cast: katalog başarısız okumada
> kilitlenmez, gövde Bind'de hazır, Play Update'ten sonraysa LateUpdate işler.
> `dotnet test` 393/393. `python3 tools/GameCompile/check.py` geçti. Unity Play yok.

> **29 Eylül — PR #25 Unity Play, Bölüm A (11 madde, 16 koşu).** Konsolda script hatası yok
> (3 ShaderGraph/URP paket hatası var, bizim değil). Araç: `Dovus/Play Sweep` (ayrı dal
> `feat/play-sweep`). Boss durdurulmuş, Kılıç, cast `TryDebugCastSkill`. Boss r 0,85, oyuncu r
> 0,50, temas 1,35 m. Ham tablo `docs/play-sweep/partA.csv`, kare kare iz `partA-detay.txt`.
> **Sonuç 11/16 koşu geçti; 11 maddeden 8'i tamam, 3'ü kaldı.**
> 1. **3-6 KALDI.** Çekişte bacak koşuyor: 32/35 kare Locomotion, Speed 2,2, playback 1,49.
>    Ama gövde 9,4 m/sn, ayak/gövde hız oranı 0,24 (4,5 m) / 0,36 (3 m): ayak kayıyor.
>    **Aş fazı boss'un tam merkezinden geçiyor:** merkeze 0,04–0,08 m (t≈0,96, temas 1,35).
>    Kalıpta `side: 1`, `height_m: 0,95` var ama yan sapma 0,00 m. Arkaya iniş doğru (1,50 m),
>    Root + Slow var, hasar 0 (base 0). Aş fazında taban Locomotion, üstte CastSlam.
> 2. **1-5 GEÇTİ.** Dönüşte gövde yaw kare başı ~11° (11°→229° / 0,33 sn), taban CastSweep
>    normalize 0,06→0,47 ilerliyor: heykel dönüşü değil, klip oynuyor. Vuruş 0,60 sn, 34 hasar,
>    6 kare hitstop. Ekranda turuncu yer tutucu küre dönüşü kapatıyor, gözle doğrulanamadı.
> 3. **3-4 GEÇTİ.** 2,5 m: dash merkeze 1,50 m'de duruyor (içine girmiyor), 2 sn bekliyor,
>    dönüş koşarak 3,6 m/sn, kare başı en fazla 0,06 m, başlangıca (2,50 m) dönüyor. 4,5 m de aynı.
>    Bekleme sonunda gövde tek karede 180° dönüyor.
> 4. **3-9 GEÇTİ.** 4,5 m: t=0,73'te tek karede 6,0 m blink, arkaya 1,50 m. 2,5 m: 4,0 m blink,
>    arkaya 1,50 m, hiçbir kare gövde içinde değil.
> 5. **3-2 KALDI.** Oyuncu içinden geçip öte tarafta duruyor. Ama **boss t=0,38'de tek karede
>    3,00 m sıçrıyor** (oyuncuya doğru çekme anlık); o karede oyuncu merkeze 0,36 m. Kalıp sonu
>    merkeze 2,65 m, sonra boss yavaşça çekiliyor (1,87 m'ye).
> 6. **3-12 KALDI.** Süzülme boyunca her kare 1,89 m ↔ 3,04 m arası gidip geliyor (1,15 m,
>    179 kare ileri-geri). Playback her kare işaret değiştiriyor, ayak/gövde oranı 0,02.
>    Çubuk (1,0) ile 7,7 m gidiyor: parmak yönlendiriyor ama titreme sürüyor.
> 7. **2-2 GEÇTİ (sayı), görsel KALDI.** Duruyor (sapma 0,35 m = kalıp `drift_m`). Kalıp 2,15 sn,
>    4 vuruş 1,07 / 1,57 / 2,07 / 2,57 sn, hepsi boss üstünde. Oyuncu +11, dost +11 can.
>    **Boss hasarı 0** (log "can→düşman -26,6" diyor). Çizilen tek çizgi oyuncu→dost, renk
>    turuncu-kahve (0,77/0,36/0,15). Boss'tan oyuncuya kırmızı akış yok.
> 8. **5-3 GEÇTİ.** 3 vuruş: 0,52 sn (boss'a 0,74 m), 0,63 (0,00), 0,83 (0,35). 28 hasar.
> 9. **5-6 GEÇTİ.** Fitil 0,52 sn'de boss kenarında (merkeze 0,85 m), patlama 0,87 sn'de boss'a
>    0,85 m / oyuncuya 2,65 m. 28 hasar, Root. Sonra bağ boss'u 0,28 m çekiyor.
> 10. **5-9 GEÇTİ.** Boss sabit ve 1,5 m kaydırılmış iki koşuda patlama boss'a 0,00 m, 32,2 hasar.
> 11. **12-7 GEÇTİ (sayı).** Vuruş 0,60 sn, boss'a 0,56 m (oyuncuya 3,56 m). Oyuncu 0 m kıpırdadı.
>     Blind + Slow ×0,70. Ekranda kum saati yok: küçük camgöbeği nokta + büyük halka (yer tutucu).
> Araç notu: ilk turdaki "süre" kalışları araç hatasıydı. Gerçek bang `_pending.BangAtWorldMs`
> (castMult dahil); 12-7 uyumsuz silahla ~0,15 sn geç bang yapar. Bir önceki Bağlayıcı cast'in bağı
> (`_mechanicLinks`) sonraki casta Root/Slow taşıyordu; araç artık bekleyip temizliyor.

> **29 Eylül — animasyon kancası (Unity Play yok).** Her faz `anim` anahtarı taşır
> (windup, lunge, dash, backstep, sidestep, spin, leap, land, hook_throw, recover, cast).
> `anim_bridge` anahtarı state/trigger'a bağlar; silah klibi yalnız bu tablo. Eksik anahtar
> bir kez uyarır, Locomotion'a düşer. Kalıp oynarken bacak hızı 0'a çekilmez; kalıbın
> hızı bakışa göre ileri/geri/yan yazılır. Geri adım koşu klibini tersten oynatır. Dönüş
> (Yayılan Vuruş) CastSweep klibi ve gövde dönüşü birlikte gider. Yan kayma klibi controller'da
> yok; Strafe sayısı yazılıyor, bacaklar şimdilik koşu hızında döner. Sabit Adım yakın dash'te
> boss kenarında durur, içine girmez. Odaklı Adım 4,5 m ve 2,5 m'den arkaya iner, ara kare
> gövdenin içinde değildir. Core test 384/384. Oyun katmanı derlendi. Unity Play yok.

> **29 Eylül — hareket kalıbı bölüm 2–3 (Unity Play yok).** Aile 15–42 de oynanır.
> 42 aile, 101 kalıp, 144 skill. Hiçbir kombo "kalıp bekliyor" demez. Sayılar
> `motion-templates.json` içinde; yoksa bir kez uyarı ve yedek. Sabit Adım (3-4) çapayı
> koyup boss'a atılır, 2 sn sonra işarete kısa kenar-güvenli atılmayla döner. Odaklı Adım
> (3-9) 0,3 sn bekleyip boss'un arkasına bir kez iner; gramerin ışınlanması kapalı. Aynı
> kalıp Yoğun Adım'da (3-1) da arkaya iner. Emici Adım (3-2) boss'un içinden geçip öte
> kenarda durur. Etiketler (Portal, Sınır modu, Takım kombosu, Silah kesme) hâlâ yalnız
> veridir, mekaniği yok. Yürüyen balon kalıbı 4 sn (Akan Zaman 12-12). Yayılan Yansı (10-5)
> metni 2 sn der; gövde aynı kalıp olduğu için 4 sn yürür. Core test 379/379.
> `python3 tools/GameCompile/check.py` oyun katmanını derledi. Unity Play yok.

> **29 Eylül — 3. Unity Play, PR #24 birleşti.** Konsolda script hatası yok; Windows'ta
> `dotnet test` 376/376. 3-6 (Kılıç) üç koşu: merkez 4,5 m ve 2,5 m boss durdurulmuş, 4,5 m boss
> yapay zekası açık. Üçünde de boss'un arkasına bir kez iniyor (merkeze 1,50–1,55 m), sonraki
> 2,3 sn'de kayma 0, tek karede sıçrama 0, taraf değişimi yok. Log: "yer değiştirme kalıpta,
> Root 1,5sn". 3-4 ve 3-9 o Play'de yoktu. Parmak basılı yükleme, animasyon ayak kayması ve
> Kılıç dışı silahlar o turda ölçülmedi.

> **29 Eylül — kalıp konumu sahiplenir (PR #24, Unity Play yok).** Kalıp oyuncuyu oynatıyorsa
> o cast'te oyuncunun yerini yalnız kalıp değiştirir. `yer_degistir` ve `hedefin_arkasina`
> ışınlanması atlanır; kalıpta eğri ya da arkaya iniş yoksa gramer mesafesi yedek olarak
> kalıba yazılır. `isaret_geri_don` işareti yine konur, dönüş kısa kenar-güvenli bir atılma
> fazıdır (ışınlanma değil). Kök, yavaşlatma, hasar, çekme, itme, portal sürer. Kalıp
> oyuncuyu oynatmıyorsa gramer eskisi gibi ışınlar. Atlanan adım skill başına bir kez loglanır.
> Gramerin oyuncuyu oynatan konum adımı üç skill'de: Sabit Adım (3-4), Bağlayıcı Adım (3-6),
> Odaklı Adım (3-9). 3-6 kalıbı oynar ve boss'un arkasına bir kez iner. 3-4 ve 3-9 o turda
> yoktu (aile 24 ve 23); bölüm 2–3'te bağlandı, eski ışınlanma kapandı. Derleme testi `python3`,
> sonra `python`, sonra `py -3` dener; hiçbiri yoksa düşmez, atlar. Core test 376/376.
> Unity Play bu turda yok.

> **29 Eylül — Play düzeltmeleri, 2. Unity Play (PR #24 açık, birleştirilmedi).** Konsolda
> script hatası yok. Boss durdurulmuş, cast `TryDebugCastSkill`, Kılıç. **1-1** merkez 3,0 /
> 2,6 / 1,6 m'den atıldı; hepsi boss merkezine 1,50 m'de (temas 1,35) durdu, sonra kayma 0 ✓.
> **1-3** iki vuruş 20 + 20 ✓. **1-5** 360°, kayma 0, 34 ✓. **5-1** turuncu `#FF6B14` fitil boss
> kenarında; patlama fitilde (fitile 0,00 m) 37,8 ✓; boss `Home` ile 3 m yürütülünce patlama
> fitilde kaldı, boss'a değmedi ✓. **Build değişip aynı karede merkez** iki kez vurdu ✓.
> **3-6** kanca boss'a gidiyor, arkasına iniyor (merkeze 1,50 m), Root 1,5 sn ✓ (hasar 0,
> JSON `base_damage: 0`). **Ama** 0,13 sn sonra oyuncu 2,86 m ışınlanıp boss'un önüne
> dönüyor ✗ — bkz. Bilinen açıklar. Windows'ta `dotnet test` 367/368: yeni derleme testi
> `python3` çağırıyor, Windows'ta o ad Store saplaması; `python tools/GameCompile/check.py` geçiyor.

> **29 Eylül — Play düzeltmeleri (PR #24).** `ManifestationDirector.MotionTemplate.cs`
> CS0150 gitti (`or` ile `||` ayrıldı). Oyun betikleri artık Unity'siz de derleniyor:
> `python3 tools/GameCompile/check.py` (Unity 2021 referans DLL + küçük saplama).
> Kanca (3-6) düşmana kilitlenir, boss'un öte kenarında durur. Sekme vuruşları (1-3)
> hedefin üstüne biner. Saplama kenardan kenara durur, gövdenin içine girmez.
> Fitil (5-1) saplandığı yerde kalır, boss yürürse peşinden gitmez. Skill menzili
> kenardan kenara; lunge mesafesi kapıya eklenir. Build değişince ilk merkez vuruşu
> bayat "casting" yüzünden yutulmaz. `cross_section: width` fiil 3 (line) ve fiil 7'de
> durur: line/box ikinci sayı tam genişlik; işaretsiz capsule/sphere yarıçap.

> **29 Eylül — hareket kalıbı bölüm 1, Unity Play (PR #24 açık, birleştirilmedi).**
> Dal Unity'de **derlenmiyor**: `ManifestationDirector.MotionTemplate.cs:136` CS0150
> (`is "self" or ... or hit.Shape == "sphere"`; `... or "behind" || hit.Shape == "sphere"` olmalı).
> Editör sessizce eski master derlemesini koşturuyor; `dotnet test` Game katmanını derlemediği
> için 362/362 yeşil. Satır yerelde düzeltilip ölçüldü (commit yok). Boss durdurulmuş, cast
> `TryDebugCastSkill` ile (parmak basılı değil). Sonuç: **1-1** yükleme 0,34 sn + saplama, 54
> hasar ✓ (ama bkz. açıklar). **1-3** sol/sağ sekme var, iki vuruş da boşa (0 hasar) ✗.
> **1-5** 360° yerinde, kök kayması 0,00 m, 34 hasar ✓. **3-6** kanca oyuncunun kendisine
> kilitleniyor, boss'un 2,2 m önüne iniyor, 0 hasar ✗. **5-1** sapla + 1,5 m geri, patlama
> +1,60 sn, 37,8 hasar ✓. **Düz vuruş** `12,1,8,6,2,5` ile vuruyor ✓ (1,9 / 2,6 m vurdu, 4,5 m ıskaladı).

> **29 Eylül — hareket kalıbı, bölüm 1.** Sıfat artık yalnız sayı değiştirmez.
> `motion-templates.json` (`Resources/ElementSystem/` ve `docs/`, aynı dosya) 144 komboyu
> 101 kalıba ve 42 aileye bağlar. Koşucu `Core/Motion`: kök yer değiştirme, faz süresi,
> hedefe yapışma, bakış, faz vuruşu. Sayı yoksa bir kez uyarı ve yedek. **Aile 1–14 oynanır**
> (36 kalıp, 49 skill). Aile 15–42 kayıtlıdır; atılınca eski davranış sürer ve o aile için
> bir kez "Hareket kalıbı bekliyor" yazılır. Portal / Sınır modu / Takım kombosu / Silah kesme
> etiketleri veridedir, mekaniği yok. Çok vuruşta skill hasarı paylara bölünür; toplam eski
> tek vuruşla aynı. Süreler JSON'da (yankı 0,3 sn, fitil ~1,5 sn, seri 3 sn, sıçrayıp çakılma
> 0,6 sn). Ara mesafeler his yedeği, aynı dosyada. Üretim: `tools/build-motion-templates.py`.
> Core test 362/362. Unity Play yok.

> **29 Eylül — düz vuruş yuva sırasından bağımsız.** Merkez, rün 1 hangi slotta olursa olsun
> Saldırı atar. `12,1,8,6,2,5` ile de jab gelir. Eski "slot 1 = rün 1" açıkı kapandı.

> **29 Eylül — fiil 3 ve 7 genişliği.** `3m × 0.8m` ve `3m × 1m` yarıçap değil genişlik
> (küreler "yarıçap" der, bunlar demez). `cross_section: width`. Oyun yarıçapı yarıya indi;
> görünür genişlik 0,8 m ve 1 m. Fiil 1 kapsülü Play'de yarıçap olarak kaldı (0,5 m).

> **29 Eylül — hitbox boyutu okuma kuralı (sahip kararı).** `base_size` "A × B" = uzunluk ×
> yarıçap (koni: menzil × açı). Uzunluk yuvarlak uçlar dahil toplam boy, saldıranın
> kenarından ölçülür; B Unity OverlapCapsule gibi yarıçap. `HitboxSizing` artık B'yi
> yarılamıyor. Fiil 3 ve 7 bu okumayla iki kat genişledi; aynı gün `cross_section: width`
> ile görünür genişlik yazılan metreye çekildi (yukarı). Fiil 1 yarıçap kaldı. Düz vuruş: göğüste, gövde kenarından 1.5 m, 0.5 m yarıçap
> (`StrikeCapsule`, `ManifestationTuning.BasicStrikeRadiusM` JSON'dan). Menzil kontrolü ve
> otomatik hedef kenardan kenara. Play: kenar mesafesi 2.0/1.6 m ıskaladı, 1.4/1.0/0.3 m
> vurdu. Core test 343/343.

> **29 Eylül — motor adım 3, Unity Play.** Build `1,12,8,6,2,5`, geçici editör betiğiyle
> (commit edilmedi). Bakış: boss seçili, çubuk yana ve geriye basılıyken üç vuruşluk zincirde
> gövde–boss açısı her karede 0°. Seçim yok, boss menzil dışı: vuruş boyunca gövde 87°'de
> kaldı, çubuğun hareket yönü ~40°'ye kaydı; vuruş bitince yürüyüşe döndü. Şifa (2-1): dost
> 5 m'de (kenara 4.5 m), seçim yok → dost 11 → 22/22. Yoğun Zaman kartı "hızı %30 düşer";
> boss 1.0 sn ×0.70, yavaşken ~1.5 m/s yürüdü, sonra ~2.2 m/s. Yükselen Zaman kartı "3 sn";
> hız ×1.50, ~3.2 sn (gerçek saat). Düz vuruş ilk turda kaldı: kapsül yarıçapı
> `TravelHitRadiusM` = 1.15 m idi, boss kenarına 2.6 m'den vuruyordu (yukarıdaki kuralla
> düzeltildi). Core test 341/341 (`EngineStep3Tests` satır sonu düzeltmesiyle; Windows
> CRLF'de 1 test düşüyordu).

> **29 Eylül — motor adım 3, dost menzili ve kartlar.** Dost hedefi skill'ler (şifa, kalkan, buff)
> silahtan bağımsız 6 m (`global_rules.ally_skill_range_m`). Seçili dost menzildeyse o, yoksa en
> yakın, o da yoksa kendine. Alan yoksa bir kez uyarı ve yedek 6 m. Yoğun Zaman etkisi aynı
> (boss normal hızın %70'i, yani %30 yavaş; çarpan 0.7). Kart metni artık "%30 düşer" diyor.
> Başka hız kartında çarpan/azalma çelişkisi yoktu; Yükselen Zaman +%50 hızı zaten doğruydu,
>   süresi 3 sn oldu (`tempo_duration_sec`, hasar buff'ı ile aynı) ve kartta "3 sn" yazıyor.
> Core test 341/341. Unity Play yok.

> **29 Eylül — motor adım 3, menzil ve bakış.** Düz vuruş menzili artık his ayarı değil:
> fiil 1 kapsülü (`hitbox_vfx`, 1.5 m). JSON'da yoksa bir kez uyarır ve eski yedek 2.4 m kalır.
> Aynı sayı isabet kontrolünde ve otomatik hedef menzilinde de kullanılır.
> Saldırı sırasında gövdeyi çubuğa çeken ikinci dönüş (nişan, hız yönü) kalktı. Seçili hedef
> varsa vuruş boyunca ona bakılır; yoksa menzildeki düşmana; o da yoksa bakış kalır, çubuk
> çevirmez. Geri giderken de hedefe bakılır. Dash yönlü nişanını korur. Saldırı bitince
> yürüme eskisi gibi. Core test 336/336. Unity Play bu turda yok.

> **29 Eylül — motor adım 2, Unity Play.** PR #22 merge edildi, Play'de `HexagonInput.TryDebugCastSkill`
> ile build `12,1,8,6,2,5` üzerinden denendi. Yoğun Zaman (12-1) 0/2/4. sn'de üç kez atıldı: her
> biri boss'a 1000 ms ×0.70 yavaş basıyor, son atıştan sonra yavaş tam 1 sn'de düştü, birikme yok.
> Yükselen Zaman (12-8) oyuncuya 1000 ms ×1.50 hız verdi (`tempo_duration_sec`). Yoğun Bağ (6-1)
> boss hazırlığının %69'unda 1500 ms sersemlik bastı, hazırlık aynı karede kesildi; bitince
> bağışıklık açıldı, boss hemen yeni hazırlığa girip 650 ms sonra vurdu. Yoğun Şifa (2-1) dost
> 3.2 m uzaktayken dosta gitmedi (menzil 0.8 m, kural gereği kendine düştü, oyuncu full olduğu için
> "zaten full"); dostun 0.6 m yanında dost 11 → 22/22 oldu. Core test 333/333.

> **29 Eylül — motor adım 2, yavaşlatma süresi.** Yoğun Zaman'ın gücü doğruydu (0.70) ama tempo bağı
> her tikte 1 sn'yi kalan sürenin üstüne ekliyordu (oyunda ~156 sn). Artık aynı kaynak süreyi
> yeniler (`max(kalan, yeni)`), farklı kaynaklar toplanmaz; en güçlü yavaşlatma kalır, süre bitince
> etki düşer. Hız da aynı kuralda. Sersemlik, kör, sessizlik, silahsız, kışkırtma, korku, durağanlık
> ve geri itme de yeniden gelince süre eklemez. Kalkan, yanma, zehir, yenilenme, gizlilik, zırh kırma,
> zayıflatma, ağır yara ve hasar azaltma zaten yeniliyordu; dokunulmadı. Pasif yuva JSON'da
> `same_passive: süre_uzar` dediği için hâlâ süre ekler. Hitstop ve Lav'ın ömür uzatması durum değil,
> duruyor. Core test 333/333. Unity Play bu turda yok.

> **29 Eylül — motor adım 2.** Boss köklenince yerinde vuruş atabilir, hücum/sıçrama/atış başlatamaz.
> Sersemlik hiçbir saldırıyı başlatmaz ve hazırlığı keser; bitince kısa bağışıklık var (adım 1'deki
> kök penceresi). Yavaşlatma, yürüyüşle aynı oranda hazırlığı ve toparlanmayı uzatır. İyileştirme,
> kalkan ve buff: seçili dost menzildeyse ona, değilse en yakın dosta, o da yoksa kendine gider.
> Dosta giden becerinin zararlı eki, kart düşman/vuran demiyorsa boss'a inmez. Kartın adlandırdığı
> tür uygulanır (Yoğun Bağ stun, Bulandırıcı Kalkan yavaş, Yükselen Adım hız, Bağlayıcı Zaman
> ikisine haste, Yükselen Zaman +%50 haste). Tempo bağı artık 0.2 sn dilim değil; süre ve güç
> JSON'dan, yoksa yedek 1 sn / 0.7 ve bir kez uyarı. Düz vuruş menziline dokunulmadı.
> Core test 329/329. Unity Play bu turda çalıştırılmadı.

> **29 Eylül — motor adım 1.** Kök artık aynı kaynaktan yenilenir, farklı kaynaklar toplanmaz
> (en uzun süre kalır) ve süre bitince 0.5 sn bağışıklık başlar (JSON'da `root_immunity_sec` yok,
> bir kez uyarı). Skill hasar/süre/soğuma/mana/menzil/yarıçap `SkillNumberCatalog` ile
> `Resources/ElementSystem/element-sistemi.json` üzerinden okunur; skill'in kendi
> `cc_duration_sec` değeri genel CC tablosunun üstüne yazılmaz. Canlı CC süreleri
> (`cc_priority`) oyun açılınca `StatusTuning` yedeğinin üstüne yazılır. Core test 322/322.
> Unity derlemesi editör komutuyla doğrulandı; Play dumanı bu turda çalıştırılmadı.

> **29 Eylül — hedefleme (PR #20).** Skill'ler varsayılan olarak hedefli: ikinci rün kabul
> edilmeden menzilde düşman yoksa cast olmaz (`hedef yok` / `menzil dışı`, mana ve soğuma
> gitmez). Seçim tık/dokunuşla; sol yarı çubuk ve altıgen düğmeleri seçim sayılmaz. Düz
> vuruş menzildeki düşmana döner, yoksa bakılan yöne boş sallar ve eski hedefi taşımaz.
> Geri giderken saldırı gövdeyi çubuk yönüne çevirmez. Yön isteyenler yalnız fiil 3
> (Hareket, `engine.action = dash`, 12 skill). Başka skill'de `aim_mode` yok; opt-in
> `engine.aim_mode` (`directional` / `skillshot` / `ground_aimed`). Unity Play bu turda
> çalıştırılmadı — yalnız Core `dotnet test` ve kod bağlama kontrolü.

> **29 Eylül — telefon yerleşimi (f5-phone).** Telefonda (445 dpi, kısa kenar ~438 dp) altıgen
> tepsi ekran yüksekliğinin ~%84'ünü kaplıyordu. `HexagonLayoutScreen.PixelsPerDp` artık
> `min(dpi/160, kısaKenarPx / HudFitShortSideDp)`; `HudFitShortSideDp = 600` (önerilen) —
> telefonda HUD ~0.73 ölçek. Joystick yarıçapı fiziksel dp'de kalır (`PhysicalDpToPixels`).
> Editörde telefonu taklit: Play'de `HexagonLayoutScreen.DebugDpiOverride = Screen.height*160/438`
> + sahneyi yeniden yükle. Tepsi `HexagonCenterXNorm 0.78`; element chip sağ üstte
> (`ElementMenuAnchor 0.86/0.72`); AYAR ve V6 düğmeleri üst şeritte BUILD'in solunda.
> **Pasif paneli kaldırıldı:** pasif rünün altıgen düğmesinin üstünde "PASİF" rozeti
> (`HexagonView.PassiveBadges.cs`; etkinken yeşil + kalan sn). Yukarıdaki "Pasif HUD sabit
> yuvalar" notu artık geçersiz. **Kamera eğimi:** sağ yarıda dikey sürükleme
> (`CameraPitchMinDeg -8` / `MaxDeg 35` / `OrbitInvertPitch`, önerilen), editörde sağ tık sürükle.
> `TuningVersion 18`. **Ayak zemini:** `AttachVisual` modeli skinned mesh'in dolgulu hazır
> bounds'una oturtuyordu (Synty'de ~10 cm havada); zemin artık ilk animasyon pozunun bake
> edilmiş köşelerinden (oyuncu/ally/boss taban 0.000). Boy ölçeği bilerek eski (dolgulu) ölçümde
> bırakıldı — bkz. Bilinen açıklar. **Ayak yönü:** Mixamo oyuncu loco kliplerinde "Original"
> kök yönü gövdeden ~42° sapık; `MixamoAnimatorBind.AlignPlayerLocoFeet` gerçek Animator ile
> ortalama ayak yaw'ını ölçüp `rotationOffset` yazar (Idle -1 / Walk 19 / Run 52; koşuda sol
> ayak ~66°→19°). Klipler gitignore'da — başka makinede `Bind` yeniden çalıştırılmalı.
> Joystick görseli ilk dokunuşa kadar ekran ortasında beyaz disk olarak kalıyordu (düzeltildi).
> AYAR düğmesi build ekranı açıkken gizli (başlığın üstüne biniyordu). Xiaomi'de
> `adb shell input tap` INJECT_EVENTS ile reddediliyor — savaş ekranı telefonda elle açılmalı.

> **28 Eylül — premium combat HUD ve repo içi ikonografi (PR #19 devamı).** "Basit placeholder"
> reddine göre HUD yeniden kuruldu. `Resources/UI/Runes/` altında bağlayıcı v6 kimlikleri için
> **12 ayrı 256×256 RGBA ikon** var: 1 Saldırı, 2 İyileştirme, 3 Hareket, 4 Savunma,
> 5 Patlama, 6 Kontrol, 7 Zayıflatma, 8 Güçlendirme, 9 Arındırma, 10 Yansıma, 11 Çağırma,
> 12 Zaman (`rune-{id}-{ad}.png`). Bunlar eski Ateş/Su/Hava/Toprak/Aydınlık/Karanlık yüzleri
> değildir; her silüet fiil/sıfat rolünden çizildi. `Resources/UI/Weapons/` altında Yumruk,
> Hançer, Mızrak, Kılıç, Balta, Çekiç, Top, Asa, Tılsım, Kalkan için 10 ayrı ikon var.
> PNG + deterministic Unity `.meta` üretimi `tools/generate-hud-icons.py`; runtime eşleme
> `CombatIconCatalog.cs` (`RuneIconCatalog` / `WeaponIconCatalog`). Asset yoksa yalnız o zaman
> iki harfli rün/silah adı fallback'i.
>
> **Combat chrome:** `HexagonView` artık cam backplate + ince altıgen bağlantı çizgisi + başlıklı
> savaş tepsisi; rünler 68 dp çap, merkez saldırı aktif silah ikonunu taşır, dodge 80 dp,
> swap 72 dp ve yedek silah rozeti gösterir. Rune rim rengi v6 kimlik rengidir (element değil);
> kabul edilen çizim noktası parlar/punch yapar, cooldown maskesi ve hazır pop korunur.
> Build ekranındaki 12 rün kartı/altıgen slotu ve 10 silah chip'i de aynı katalogları kullanır.
> Skill kartı büyük başlık + prose + sağ uyum etiketi + renk şeridi oldu; uyumsuz hâl sarı
> (`×0.8`, cast `×1.2`) ve kart görünürken pop/fade alır. Boss 460×24 dp HP + urgency cast,
> oyuncu AVCI/HP/MP/ALLY cam kümesi; oyuncu ve boss low-HP pulse. Pasif HUD artık seçilen
> 0–2 rünü `RuneManager.Changed` ile sabit yuvalarda gösterir (`HAZIR`/timer); build değişiminden
> kalan aktif eski etki bitene kadar boş görünür yuvayı kullanır. Element chip/radial merkezi,
> tüm seçenek yarıçapı ve yarım boylarıyla safe-area içine clamp edilir.
>
> Tüm yeni palet, metin hiyerarşisi, panel/ikon/padding ve motion değerleri `HudTheme`;
> konum, dp boyu, touch alanı, radial/pasif/vitals ölçüleri `PrototypeTuning` v17 alanlarında
> Inspector açıklamalarıyla. Gramer/executor/cooldown/i-frame/global zaman kuralları değişmedi.
>
> **Editor Play smoke (cloud'da Unity yok; kullanıcıdan özellikle isteniyor):** Prototype Play →
> 6 rün + 2 silah seç; 12 rün/10 silah ikonunun ayrı olduğunu ve slot sırasını doğrula; safe
> area içinde tepsi/68 dp rün/80 dp dodge/72 dp swap çakışmasın; bir ve iki rün çizince rim +
> skill kartı, uyumsuzda sarı kart; swap sonrası merkez/swap/yedek ikonları; element chip'e
> basılı tutup 6 seçim (slow-mo yok); 0/1/2 pasif yuvası ve timer; oyuncu/boss düşük can pulse,
> üç boss cast'inde urgency; cooldown/hasar/camera/dodge/skill kuralları ve konsol temizliği.
>
> **Doğrulama:** üretici ikinci çalıştırmada birebir çıktı (`git diff --exit-code`);
> 22/22 PNG 256×256 RGBA + boş olmayan alfa ve 22/22 `.meta` doğrulandı; bağımsız salt-okunur
> C# incelemesinde compile blocker kalmadı. Bu cloud imajında `dotnet` ve Unity Editor yok:
> güncel HUD revizyonu burada derlenemedi/Play edilemedi. PR'ın HUD öncesi commit'i
> `dotnet test tools/CoreTests/CoreTests.csproj` **313/313 yeşildi**; HUD follow-up'ı Core'a
> dokunmadı. Telefon yoğunluk/thumb-reach/overdraw ve gerçek motion gözü doğrulanamadı.

> **28 Eylül — görünür his turu (ölçek/kamera/tempo/atmosfer/HUD).** Gerçek stack
> doğrulandı: `Prototype.unity` Synty Hero Knight + Rock Golem prefablarını koddan kuruyor;
> Cinemachine yok, mevcut `FollowCamera` shake/orbit/hareket yönünü taşıyor. Asıl ölçek hatası:
> oyuncu 2.625×, boss 3.3× zorlanıyor ve boss görseli ayrıca kapsülün 1.7×/1.3× non-uniform
> kök ölçeğini miras alıyordu. `AttachVisual` artık etkin renderer bounds'unu ölçüp oyuncuyu
> **1.78 m**, boss'u **5.0 m** (2.8×) hedef boya getiriyor, parent ölçeğini nötrleyip ayağı
> zemine oturtuyor. Kamera runtime'daki `(0,12,-14)` tepeden override'ını kaldırdı; mevcut
> stack omuz pivotu `(0.45,1,-0.35)` + 3.4 m mesafe, 56° FOV, boss'a yumuşak yaw/framing ve
> kamera-göreli dodge kullanıyor. Tam çubuk koşu 7.5→**6.4 m/sn** (ölçülen Axe run 5.52'ye
> 1.16×), düşük stick yürüyüşü ~2.7 m/sn; ivme/fren 52/64. Dodge varsayılanı
> 3.8 m/260 ms + 220 ms kuyruk → **3.2 m/190 ms + 120 ms kuyruk**, 360 ms cooldown;
> global timescale yok. Atmosfer tek güçlü sıcak key + düşük ambient, gölgesiz hafif soğuk rim,
> sis ve restrained bloom (1.8→0.42); tüm sayılar `PrototypeTuning` Inspector alanlarında,
> dodge `CombatTuning.Dodge`/ayar panelinde. HUD: boss barı 280×12→420×20 dp, oyuncu
> barları 210×15 dp ve HP/MP/ALLY etiketli; rune hit yüzleri 26→30 dp, dodge 36 dp,
> skill kartı 300×58 dp + uyum renk şeridi. Hex/radial/sarı uyumsuzluk/pasif/swap sistemleri
> korunuyor.
>
> **Editor smoke:** Play → build seç → oyuncu/boss ayağı zeminde ve oran ~1:2.8; ileri/yan
> koş + düşük stick yürüme ayrımı; boss çevresinde sağ sürükle ve bırak (kamera boss'a yumuşak
> döner, oyuncu alt-merkezde); farklı yönlere dodge; boss bar/cast, HP-MP, 6 rune, sarı
> uyumsuz skill kartı; key/rim/sis ve yalnız parlak VFX'te hafif bloom. Konsolda
> `[VisualScale] Player/Boss target=...` satırlarını kontrol et.
>
> **Doğrulama:** `dotnet test tools/CoreTests/CoreTests.csproj` **313/313 yeşil**.
> **Kalan/doğrulanamadı:** Bu cloud imajında Unity 6000.4.4f1 Editor yok; Play görseli ve
> telefon kare süresi doğrulanamadı. Gerçek sunum ses miksajı, hitstop yoğunluğunun kullanıcı
> gözü/kulağıyla ayarı ve Mixamo dosyaları gitignored olduğu için kliplerin başka makinede
> yeniden indirilmesi açık.

> **28 Eylül — Oyuncu yürüme/koşu/duruş yenilendi (dal `feat/feel-player-loco`).**
> Sahibi: "yürüyüşü koşuşu duruşu çirkin". Sebep: Sword&Shield klipleri olmayan kalkan için sol
> kolu önde tutuyordu, eski idle ~50° sallanıyordu. Yerine Mixamo Pro Melee **Axe** seti
> ("Standing Idle/Walk/Run" + açıklama "…With Axe"; `tools/mixamo-jobs/player.json` artık `desc`
> ile tam eşleşiyor). Eski klipler `Assets/Art/Mixamo/_old_loco/` altında (binder okumaz).
> Klipler gitignored → başka makinede `tools/mixamo-download.mjs` + "Dovus/Synty/Bind Mixamo
> Animator" yeniden çalıştırılmalı. Binder artık yürüme/koşu kliplerinin **zemin hızını ölçer**
> (ayak basışındaki kayma hızı medyanı, model birimi: yürüme 0.93, koşu 2.10 → dünya 2.44 / 5.52
> m/s) ve blend eşiklerini buna koyar; `LocoRunSpeed`/`LocoPlayback` parametreleri, state'te
> `speedParameter=LocoPlayback`, `iKOnFeet`. `ActorVisual.SetLocomotion` gerçek hızı model
> birimine çevirir; koşu doğal hızını aşınca oynatma hızı oranla artar, tavan
> `PrototypeTuning.LocoMaxPlaybackMult = 1.5` (**önerilen**). Play ölçümü: 7.5 m/s'de oynatma
> 1.36× (tavan altı, kayma yok); 3.4 m/s yürümede yürüme/koşu karışımı. Ölçüm yoksa eski
> 0/0.4/1 eşiklerine düşer. **Açık:** Kalkan benzeri silahta sol kol kalkan tutmaz; vuruş/cast
> klipleri hâlâ Sword&Shield üslubu; 720°/s dönüş ani. **APK:** `d3ef794` build'i (67.8 MB)
> `adb install -r` Success (YXQC5PTGUCEQMNV4), build ekranına kadar açıldı; oynanış hissi
> sahibinde. Not: Play'den çıkar çıkmaz `delayCall` ile kuyruğa alınan build geç tetiklendi
> (domain reload); `adb` PATH'te değil → Unity SDK `platform-tools\adb.exe`.

> **28 Eylül — Skill görseli gramerden doğuyor: madde × yol × silüet (dal `feat/skill-vfx-grammar`).**
> Sahibi geri bildirimi: "her skill aynı lavlı kayayı atıyor", "1 silah × 1 fiil olsa da 120 farklı
> şey olmalı" — elle sıfat→görsel eşlemesi reddedildi (kural 6). Yeni: `Core/Mechanic/MechanicVisual.cs`
> (`MechanicVisualComposer.Compose(plan, SkillVisualTuning)` → `VisualRecipe`, saf C#). Madde = fiil
> kimliği (`Substance/{verb}`), dizilim = `Body.Path` (temas/dürtme/saplama/yay/ağır yay/çarpma/
> yerleşim/taşınan/hat/gövde), silüet = `Body` bayrakları (bulut→pus, katı→duvar, tek hedef→tek iri
> parça, delici→uzama, sarsılmaz, dikey sütun, rampa, güdüm, çekim, büyür, zincir sekme, kopya, ayna,
> bağ ipi, çapa halkası, akış, gövdeye bağlı); boyut `Body.SizeM`'den. Test
> (`MechanicVisualTests`): her silahta 144/144 farklı reçete, toplam 1440/1440; maddeden bağımsız
> dizilim silah başına 25–54. Unity: `ComposedSkillVfx` reçeteyi oynatır (`HitboxVfxRegistry.TryCompose`
> önce, madde yoksa eski `Delivery/*` giydirmesi). Her fiilin **katı cismi** var: 1 kaya
> (`RockDebris_Low`), 4 kaya dikeni, 10 buz mızrağı (paket mesh'i); diğerleri koddan üretilen
> düşük poligon şekil (`ProceduralChunkMesh`: 2 yaprak, 3 ok, 5 dikenli patlama, 6 zincir halkası,
> 7 balçık, 8 kristal, 9 yıldız, 11 dikilitaş, 12 kum saati) — binder'da `"shape:<ad>"`
> (`VfxLibrary.Entry.ChunkShape`). Cisimler yerden çıkar/batar, mermideki takla atar ve yol boyunca
> iz bırakır; malzeme URP/Lit + element rengi + hafif ışıma (paket Ice.mat pembe/hata shader'ı,
> kullanılmıyor). Zemin yüksekliği `FeelVfx.GroundY` (arenada zemin collider'ı yok).
> Tüm sayılar `Core/Tuning/SkillVisualTuning.cs` (**hepsi önerilen**, `VfxLibrary.Composition`);
> kod varsayılanı değişince asset'teki kopya elle sıfırlanmalı (`lib.Composition = new …`).
> Editor Play doğrulandı: Kılıç 1-5 kaya yayı, Çekiç 1-5 2.7 m önde halka + sütun, 1-9 tek iri kaya,
> Top 1-5 uçan kaya + yerde iz, Asa 1-5 hat boyunca kaya; 12 fiil yan yana galeri ve 5-7'nin 10 silahta
> ayrı dizilimi. **Doğrulanamadı:** 1440'ın hepsi gözle görülmedi; 11 dikilitaş ile 12 kum saati
> uzaktan benzer; telefonda parçacık+mesh maliyeti (Top izi 0.1 sn'de bir parça) ve URP/Lit
> `_EMISSION` varyantının build'de kalıp kalmadığı ölçülmedi.

> **28 Eylül — Somut skill efektleri: CFX Remaster Free + Particle Pack bağlandı (dal `feat/feel-f5-phone`).**
> Sahibi iki paketi Asset Store'dan import etti (`JMO Assets/`, `UnityTechnologies/ParticlePack/`,
> ikisi de `.gitignore`'da). Sahibi geri bildirimi: boss slam'indeki taş/lav patlaması (EarthShatter)
> "somut" — "bütün skillerde bunu arıyorum". Yapılan: `HitboxVfxRegistry` primitive'in üstüne teslim
> yoluna göre paket efekti giydiriyor (`Delivery/{executor}/{şekil}` → `Delivery/{executor}` →
> `Delivery/{şekil}`); giydirme varsa yarı saydam primitive gizlenir (zemin `cylinder` hariç), collider
> aynen kalır. Eşleme `Dovus/Feel/Bind VFX Packs` (`VfxPackBinder`) ile `Resources/VfxLibrary.asset`'e
> yazılır: Projectile→FireBall, MeleeHitbox→CFXR4 Sword Hit (patlama/sphere→CFXR Explosion 1),
> FieldAura→EarthShatter, Summon/line→IceLance, Movement→CFXR Magic Poof, SelfState→CFXR3 Magic Aura,
> cone→CFXR Fire Breath; his efektleri FX_HitSpark/Crit/Dodge/Slam/Crack/FireCone ve `Impact/*` stilleri
> de CFXR'a bağlı. Paket prefab'ı element rengine `VfxLibrary.Tint` ile çekilir
> (`ImpactTintStrength` 0.75, `HitSparkTintStrength` 0.5 — önerilen), `Entry.ReferenceSizeM` ile
> saldırı boyutuna ölçeklenir (referans boyutlar gözle — önerilen). Editor Play: boss slam taş+ateş,
> 1-5 yakın vuruş kıvılcımı, 6-5 alan skillinde taş/lav halkası ekranda.
> **Tuzak (unity-notlari'na aday):** Particle Pack "Starter Assets" import'u `manifest.json`'a ~17 paket
> (postprocessing v2, progrids preview…) + `UNITY_POST_PROCESSING_STACK_V2` define'ı + TMP örnekleri
> ekliyor; geri alındı. Paket geri çözümlemesinden sonra Synty `Generic_Basic.shadergraph` hata
> shader'ına düştü (karakterler pembe) → `ImportAsset(ForceUpdate)` ile düzeldi.
> **Doğrulanamadı:** mermi (FireBall), çağırma (IceLance), hareket ve self skilleri Play'de tek tek
> görülmedi; ölçek/renk göz ayarı sahibinde; telefonda parçacık maliyeti ölçülmedi.

> **28 Eylül — His turu Faz 5 (kısmi): Mixamo setleri + build shader'ı (dal `feat/feel-f5-phone`).**
> Sahibi mixamo.com'a Cursor tarayıcısında girdi; `tools/mixamo-download.mjs` ile
> `mixamo-jobs/player.json` 13/13 (Sword And Shield idle/walk/run/slash/attack/kick/block/impact/
> death + roll + 1H/2H magic + power up) ve `boss.json` 7/7 (Mutant idle/walk/jump attack/roaring/
> flexing/dying + Big Hit To Head) indirildi → `Dovus/Synty/Bind Mixamo Animator` çalıştırıldı.
> Editor Play: oyuncu `Player_Idle/Walk/Run` blend + `Player_Strike_A/B/C` vuruş döngüsü,
> boss `Boss_Walk` + `Boss_Slam` oynuyor. **Not:** `unity/Assets/Art/Mixamo/` `.gitignore`'da —
> klipler yalnız bu makinede; başka makinede aynı iki komut tekrar çalıştırılmalı.
> CastPierce/CastSweep hâlâ ortak `Melee_Thrust/Melee_Slash` (oyuncu setinde karşılığı yok).
> `AndroidBuilder.RuntimeShaders`'a `Universal Render Pipeline/Particles/Unlit` eklendi
> (FeelVfx, CastFlash, LivingEffectView, LavaDecor, BillboardVfx `Shader.Find` ile arıyordu;
> build'de düşerdi). **Açık:** APK yeniden alınmadı (ilk deneme Play'e girilince kesildi),
> telefon bağlı değil → kare süresi / overdraw / parçacık bütçesi ölçülmedi, telefon his turu yok.
> Editor'de `StickKnob` bir kez fare bırakılmadan oyun görünümünden çıkınca aktif kaldı
> (`MoveInput.IsActive` parmak id'si takılı) — telefonda tekrar edip etmediği bakılmadı.

> **28 Eylül — His turu Faz 4: VFX + SFX (dal `feat/feel-f4-vfx-sfx`).**
> VFX çözümleme: `Core/Presentation/VfxKeyChain` (`VFX_{Element}_{Fiil}_{Sifat}` →
> `VFX_{Element}_{Fiil}` → `VFX_{Fiil}`, testli) + `VfxLibrary` SO (`Resources/VfxLibrary.asset`,
> şimdilik boş tablo: anahtar → prefab + ömür). Sıra: tablo → `Resources/Vfx/Hitbox/{k}` →
> `Resources/Vfx/{k}` → prosedürel. `HitboxVfxRegistry` ve `PlaceholderFactory` (anahtar
> `Trail/{stil}`, `Impact/{stil}`) bu zincirden geçer. His efektleri sabit `FX_*` anahtarlı
> (`FX_HitSpark`, `FX_CritSpark`, `FX_DodgeDust`, `FX_FootDust`, `FX_BossStepDust`,
> `FX_SlamShockwave`, `FX_GroundCrack`, `FX_FireCone`); prefab yoksa `FeelVfx` koddan kurar:
> isabet kıvılcımı (element rengi, kritikte büyük), dodge tozu, ayak/boss adım tozu, slam şok
> halkası + toz + zemin çatlağı (prosedürel doku, `FxTween` ile söner), alev konisi (koni açısı
> ve menzil saldırıdan). `SfxLibrary` SO (`Resources/SfxLibrary.asset`, boş = varsayılanlar) +
> `SfxDirector` (10 sesli 2D havuz, pitch/volume rastgele, olay başına min aralık, aynı klibi
> üst üste çalmaz). Klipler `Resources/Sfx/{olay}/` — **Kenney CC0** (Impact/Interface/RPG/
> Sci-fi, `Sfx/CREDITS.txt`, 58 ogg ≈ 1 MB). Olaylar: `hit`, `crit`, `player_hurt`, `dodge`,
> `perfect_dodge`, `boss_windup/slam/fire/roar/step`, `ui_tap`, `footstep`, `cast_{family}`
> (element-sistemi.json `verbs[].family`: strike/mend/motion/guard/control/disrupt/purge/special).
> Bağlantı: `PresentationFx` (BossDirector `AttackWindupStarted`/`AttackStruck`/`BossPhaseChanged`,
> DodgeMotion `SlideStarted`, CombatFeel `Exchanged`, HexagonInput `DotAccepted`) +
> `ManifestationDirector.NotifyBossStruck` (kıvılcım + hit/crit) + `ShoutSkill` (cast sesi) +
> `FootstepEmitter` (mesafe tabanlı adım; oyuncu + boss). Hepsi yalnız sunum, hasar zamanlaması
> değişmedi. CFX Remaster Free / Particle Pack klasörleri `.gitignore`'da
> (`JMO Assets/`, `EffectExamples/`, `ParticlePack/`).
> **Uydurma/önerilen:** `VfxLibrary` prosedürel alanlarının tamamı; `SfxLibrary` varsayılan
> ses/pitch/aralık tablosu; `FootstepStrideM`=2.2, `BossFootstepStrideM`=2.4; klip seçimleri
> (kükreme için Kenney'de yaratık sesi yok → alçak frekans patlama yer tutucu).
> **Doğrulandı (Editor Play):** derleme + konsol temiz; çalan olaylar: ui_tap, cast_strike, hit,
> dodge, boss_windup, boss_slam, boss_fire, boss_roar, boss_step, player_hurt, footstep;
> slam halkası + çatlak + kıvılcım ekranda; `dotnet test` 308/308.
> **Doğrulanamadı:** `crit` ve `perfect_dodge` sesleri oyunda tetiklenmedi (koşul gerektiriyor);
> CFX / Particle Pack import'u sahibinde (Asset Store oturumu) — paket gelince prefab'lar
> `VfxLibrary`'ye sürüklenir; seslerin kulak kontrolü ve miks dengesi; alev konisi göz kontrolü.

> **28 Eylül — His turu Faz 3: HUD cilası (dal `feat/feel-f3-hud`).**
> uGUI'de kalındı; TMP (ugui 2.0 içi) essentials `Assets/TextMesh Pro`'ya alındı. Ortak katman:
> `HudTheme` (SO, `Resources/HudTheme` yoksa varsayılan örnek; renk/dp/juice süreleri) +
> `UiJuice` (unscaled punch/shake/fade/pulse). Font: `Resources/Fonts/HudFont.ttf` = **Russo One**
> (OFL, `OFL.txt` yanında; Lilita One `ğ ş İ` içermediği için elendi); TMP asset'i
> `Dovus/UI/Build HUD Font Asset` menüsü üretir (`HudFont SDF.asset`, statik atlas). HUD
> metinleri `HudTheme.LegacyFont`'a geçti (debug panelleri hariç).
> Eklenenler: `BarJuice` (ghost bar tut→eri, hasarda beyaz / heal'de yeşil flash; oyuncu barı
> düşük canda nabız); boss adı + alt başlık + faz çentiği + **cast barı** (`BossDirector.
> CurrentAttackKind`/`WindupProgress01`, isim `Resources/Bosses/karadul.json`'dan `BossHudData`);
> faz 2'de "FAZ 2 ÖFKE" banner'ı + boss barı sarsıntısı; `CombatOverlayHud` (düşük can kenar
> vinyeti, hasar yönü kaması, ekran dışı boss oku, ZAFER + süre / YENİLDİN + dönüş sayacı;
> kanvas FeelCanvas'ın üstünde, sort 210); hex butonlarda basış punch'ı (`HexagonInput.
> DotAccepted` → `HexagonView.NotifyPressed`), cooldown bitince hazır pop'u, kapalı/cooldown'da
> `DisabledTint`; `StatusIconStrip` game-icons.net ikonları (`Resources/Icons/Status`, CC BY 3.0,
> `CREDITS.txt`; yoksa glif), yeni statüde punch, bitmeye yakın yanıp söner; hasar sayıları
> isabet noktasında (`ManifestationDirector.HudFeed`: boss gövde yarıçapında darbe yönü) ve
> element renginde, kritik altın "N!".
> **Uydurma/önerilen:** `HudTheme` alanlarının tamamı (renkler, dp boyları, GhostHold 0.35 /
> Drain 0.6/sn, Flash 0.12, LowHpFrac 0.3, pulse 2.2 Hz, banner 1.6+0.4 sn, punch 1.25/1.08/
> 0.88/1.18, status blink <1.5 sn @4 Hz).
> **Doğrulandı (Editor Play):** derleme temiz; boss adı/alt başlık, cast barı ("Yere Çakma",
> "Cehennem Nefesi"), %50'de faz banner'ı; ekran dışı ok (boss kamera arkasına alınınca açık,
> geri gelince kapalı); düşük can vinyeti + hasar yönü bayrakları; boss ölünce "ZAFER Süre
> mm:ss"; 20/21 statü ikonu yükleniyor (`None` hariç); `dotnet test` 305/305.
> **Doğrulanamadı:** hasar sayıları bu makinede panel ayarıyla kapalı (`ShowDamageNumbers`,
> tuning.json'da yoksa false) — göz kontrolü yapılmadı; ghost bar/flash ve hex punch
> animasyonlarının göz kontrolü; telefon dp ölçeğinde okunurluk (Faz 5).

> **28 Eylül — His turu Faz 2: oyuncu animasyon seti (dal `feat/feel-f2-player-anim`).**
> Hepsi yalnız görsel; hasar/etki doğumu zamanlaması değişmedi. `ActorVisual.PulseBasicStrike`
> düz vuruşta `BasicStrike → BasicStrikeB → BasicStrikeC` döner (`BasicStrikeComboResetSec`
> vuruşsuz geçince A'ya döner); menzilli silahta (`weapons[].type == "ranged"`: top/asa/tılsım)
> `CastShoot` oynar ve mermi cast'i (`CastPierce`) de `CastShoot`'a düşer. Yeni
> `ActorVisual.PlayAction`: `Speed ≥ UpperBodyCastMinSpeed` iken Cast/vuruş `UpperBody`
> maskeli katmanında (`Upper*` state) oynar, bacaklar Locomotion'da kalır; tam gövde
> state'leri (Dodge/Hit/Death/durağan cast) üst katmanı `Empty`'ye çeker. Rooted cast'te motor
> durur → Speed sönümlenir → bacaklar idle'a iner (ayrı kural yok). `AnimationBridge.StatePlayer`
> ile v6.1 `PlayBinding` yolu da ActorVisual'dan geçer (crossfade + üst gövde).
> `AnimationBridge.StartFrameTimer` + `ManifestationDirector.CastPresentation.cs`: v6.1 skill'de
> oynayan state'in prezentasyon karşılığı (`animator_state` eşlemesi; menzilli → `cast_*`,
> yakın → `melee_*` tercih) `spawn_vfx_at_frame` anında sağ elde `CastFlash` parçacığı
> (element rengi; değerler BangBurst'ten). Bind aracı `UpperBasicStrike/B/C` ekler.
> **Uydurma/önerilen:** `BasicStrikeComboResetSec`=1.2, `UpperBodyCastMinSpeed`=0.15.
> **Doğrulandı (Editor Play):** vuruş döngüsü A B C A; koşarken cast → `UpperCastPierce` +
> taban Locomotion; skill kapanışında `CastFlash` 1 kez; konsol hatasız; `dotnet test` 305/305.
> **Doğrulanamadı:** Mixamo kılıç seti indirilmedi (token yok) — `tools/mixamo-jobs/player.json`
> hazır: `node tools/mixamo-download.mjs <token> unity/Assets/Art/Mixamo/Player tools/mixamo-jobs/player.json`
> → Bind menüsü; şimdilik A/B/C = Melee_Thrust/Slash/Punch, CastShoot = Melee_Thrust.
> Üst gövde karışımının göz kontrolü; koşu başlangıç/duruş ve küçük/büyük darbe klipleri yok.

> **28 Eylül — His turu Faz 1: boss animasyonu (dal `feat/feel-f1-boss-anim`).**
> `BossVisual` artık state crossfade ile sürülür: `Locomotion` (idle/walk blend, `LocoSpeed`
> = zemin hızı / klip kök hızı → kayma yok), `BossSlam`/`BossBreath` (windup),
> `BossRoar` (faz 2 girişi), `BossStagger` (meşgulken ve `BossStaggerMinGapSec` içinde
> yutulur), `BossDeath`. Windup senkronu: `ActionSpeed = impactNorm × klipSüresi / windupSn`
> → tell zamanı `BossTuning` windup verisinden gelir, klipten değil. Yaklaşırken
> `BossTurnRateDegPerSec` ile döner (windup `FacePlayer` snap'i oynanış, dokunulmadı).
> `BossDirector`: `BossPhase`, `BossPhaseChanged`, `CurrentAttackKind`, `WindupProgress01`,
> `Vitals` (Faz 3 HUD için). `BossTelegraph.SetShape`: yay < 180° ise disk yerine koni
> (fan mesh). `MixamoAnimatorBind` yeniden yazıldı: `Mixamo/Player/` ve `Mixamo/Boss/`
> klasörleri önce, paylaşılan `Mixamo/*.fbx` yedek; tek klipli FBX'lerin klip adı dosya adına
> çevrilir ("mixamo.com" sorunu); oyuncu tarafına BasicStrikeB/C, CastShoot ve
> `UpperBody` maskeli katman eklendi (Faz 2 kullanacak). İndirme: `tools/mixamo-download.mjs`
> + `tools/mixamo-jobs/{boss,player}.json` (token: mixamo.com `localStorage.access_token`;
> `node tools/mixamo-download.mjs <token> unity/Assets/Art/Mixamo/Boss tools/mixamo-jobs/boss.json`
> → `Dovus/Synty/Bind Mixamo Animator`). **Uydurma/önerilen:** `BossTurnRateDegPerSec`=240,
> `BossSlamImpactNorm`=0.42, `BossConeImpactNorm`=0.40, `BossStaggerMinGapSec`=0.6,
> `BossAnimCrossFadeSec`=0.15, `BossWalkClipMps`=1.4. **Doğrulandı (Editor Play):** zorlanan
> windup `BossSlam`'e girer, `ActionSpeed` 0.39–0.55; can %50 altı → faz 2 + roar; konsol
> hatasız; `dotnet test` 305/305. **Doğrulanamadı:** mutant klip seti indirilmedi (token
> yok) — şimdilik paylaşılan Melee_Thrust/Spell_Cast/Hit/Death yedekleri; job listesindeki
> Mixamo adları sunucuda doğrulanmadı; koni telegraf göz kontrolü.

> **28 Eylül — His turu Faz 0: bağlanmamış his ayarları (dal `feat/feel-f0-wiring`).**
> `KinematicMotor` artık `MoveAccelMps2`/`MoveDecelMps2` ile ivmelenir, `TurnRateDegPerSec`
> ile döner (eskiden anlık). Çubuk büyüklüğü hızı ölçekler: ölü bölge üstü
> `MinStickSpeedFrac`=0.4 (yürüme) → tam çubuk 1 (koşu). Animator `Speed` = gerçek hız /
> `WalkSpeedMps`, `AnimSpeedDampSec`=0.08 sönümlü. `ActorVisual` aksiyon state'lerine
> `AnimCrossFadeSec`=0.06 crossfade ile girer (aynı state tekrarında sert restart).
> Bossa doğrudan isabet (kapanış hasarı) → `CombatFeel.OnBossStruck`: `HitstopBossHitMs`
> (70, artık canlı) + `BossHitShakePx`=4 + boss `HitFlash`; yankı/minion isabeti yalnız
> parlama. Art arda isabet `BossHitHitstopMinGapMs`=140 içinde hitstop yığmaz. Oyuncu
> vurulunca gövde sıcak renkte parlar. Yeni `HitFlash` (MaterialPropertyBlock `_BaseColor`,
> `HitFlashMs`=90, `HitFlashStrength`=0.85). `SkillFeel.CameraKick` gömülü sayıları
> `FeelTuning.SkillKick*`/`SkillShake*`'e aynen taşındı. `LivingEffectView.EnsureBangBurst`
> "duration while playing" spam'i düzeldi (AddComponent sonrası Stop). Test kancası:
> `MoveInput.SetScriptedDirection`. **Uydurma/önerilen:** MinStickSpeedFrac, AnimSpeedDampSec,
> AnimCrossFadeSec, BossHitHitstopMinGapMs, BossHitShakePx, HitFlashMs, HitFlashStrength.
> **Doğrulandı (Editor Play):** tam çubuk 7.5 m/s, kısmi (0.2) 3.41 m/s + Speed 0.45;
> `OnBossStruck` → hitstop aktif + boss parlıyor; konsol hatasız; `dotnet test` 305/305.
> **Doğrulanamadı:** telefonda ivme/dönüş hissi; parlamanın görsel yoğunluğu göz kontrolü.

> **28 Eylül — label-only mechanic_grammar atomları dünyaya indi.** Runtime etiketi okumaz:
> yeni saf-Core `MechanicWorldProfile` gövde/atom/modlardan yetenek çıkarır; Unity adaptörü
> `ManifestationDirector.MechanicWorld` bunları yaşayan nesnelere bağlar. **Artık dünya
> etkisi olanlar:** Duvar/Çit (ömürlü gerçek collider; oyuncu geçemez), Klon/Ayna klon/
> Taret/Muhafız/Suikastçı (ayrı aktör silüeti; taret sabit, klonlar oyuncu-kopya davranışı,
> silah yoluna göre yakın saldırı veya görünen atış), Geri sarma (boss konum geçmişi +
> hazırlanan saldırıyı iptal), Zaman alanı (sürekli hacim; boss Slow, oyuncu/ally Haste;
> global timescale yok), Güdüm (projectile hedefi her kare kovalar; melee hedef kilidi),
> Dosta/Uzak yansıtma (ally/iniş noktasında ömürlü alan; içindeki oyuncuya gelen hasarı
> boss'a döndürür). Audit'te bulunan diğer label-only atomlardan: uzak Sis stealth/blind,
> Girdap sürekli çekme, Bağ/Tasma (can paylaşımı + hasar yönlendirme + yenilenen CC ve
> hedefe göre boss/ally tether; düşman ucu `link_len_m` tasmalı), Yem kopya, Arınma alanı,
> Durum aktarma, Buff silme, koruyucu HP-eşiği tetik,
> `rise_delay_sec` / `mark_delay_sec`, Bataklık/Akıntı sürekli tikleri dünyaya bağlandı.
> Portal, işaretle-geri dön, arkaya ışınlanma, yer değiştirme, dondurma/hızlanma/knockup
> zaten canlıydı. `TimedHistory<T>` ve profil eşleme Core testleri eklendi.
>
> **Hâlâ ertelenen atom envanteri (sebep):** düşman mermisi olmadığı için Emici yutma,
> Mermi geri gönderme/kesen perde ve Bölünen yansıma düşman mermisi üzerinde denenemez
> (perde hacmi/collider dünyada); tek boss olduğu için Seken/Zıplayan minyonun "sonraki
> hedef" seçimi ve Delici'nin ikinci hedefi yok; boss AI kapsam dışı olduğu için Yem kopya
> aggro çekmez ve Ters kontrol hedef girdisini çeviremez; gerçek co-op aktörü olmadığı için
> ally Haste/Stealth StatusBoard'da canlı olsa da hareket/görünüşe yansımaz. Taşma stat
> merdiveni, Emme/çalma'nın yarar→düşman stat dönüşümü, Ters kopya, Arınıp güçlenme,
> Kusursuz savuşturmanın pencere bazlı ilk-vuruş ayrımı, Ayna/Kıskaç ikinci hasar gövdesi,
> Yankı'nın önceki skill snapshot'ı, rampa gücünün 1→`ramp_max` sayısal artışı ve Akan
> hareketin kesintisiz glide'ı executor hasar/zaman sahipliğini ayırmayı gerektiriyor.
> Bunlar artık sessiz label-only değil; bu listede açıkça ertelendi. Presentation/boss AI/
> telefon kapsamına dokunulmadı. Bağlayıcı docs/Resources/upload SHA-256 birebir:
> `29d06672086111ec8baebc29e3531cf9c40f132d8cf9e56427499b37fcd66f4f`.
> **Doğrulandı:** `dotnet test tools/CoreTests/CoreTests.csproj` **305/305**.
> **Editor doğrulaması (yerel, Kılıç, merge öncesi):** Unity derlemesi temiz, Play'de
> exception yok. 4-4 3×3 m `MechanicWall` 2.5 sn (oyuncu önüne ışın duvara çarpar);
> 11-4 taret ve 11-11 iki klon doğar; 12-10 isabet → "geri sarma 2sn + cast iptal";
> 10-1 `MechanicReflector` alanı; 12-12 alan → boss Slow; 6-6 `MechanicTether` + boss
> Root/Slow; 2-7 `MechanicCloud` + boss Blind; 3-11 `MechanicPlayerDecoy`. Denenmedi:
> yansıtıcının hasarı gerçekten döndürmesi, alan içindeki oyuncuya Haste, güdümün izi.
> Audit follow-up: Girdap hareketi legacy knockback yerine bağlayıcı
> `vortex_pull_mps × tick_dt` ile `BossReactor.Home` üzerinde ilerler.

> **28 Eylül — mechanic_grammar BAĞLAYICI + oyunda (dal `feat/mechanic-grammar`).** Sahibi
> atom gramerini onayladı. Kurallar `element-sistemi.json` → `mechanic_grammar`'a taşındı
> (sürüm 6.1.1 kaldı; taslak `docs/atom-grammar-taslak.json` silindi). Motor Core'da:
> `Core/Mechanic/` (`MechanicRules` JSON okur, `MechanicGrammar.Compose(fiil, sıfat, silah)`
> plan üretir, `MechanicLabeler`/`MechanicDescriber` etiket + Türkçe açıklama). `tools/AtomSim`
> artık bu dosyaları link'ler (kopya yok). Simülasyon düzeltmeleri: **yansıma konumu**
> (`self_effect_location`: gövdesi sende değilse Yumruk → dokunulan dost, uzak yollar →
> indiği alan; 64 "uzakta ama sende" skill → 0) ve **uyumsuz silah** (`incompatible_weapon`:
> `uyumsuz_cizim` 0.8 etki / 1.2 cast; oyunda bu ceza zaten `EquipmentBonusResolver`'da,
> gramer tekrar uygulamaz). **Oyunda** (`ManifestationDirector.MechanicGrammar.cs`): her
> cast'te plan kurulur, `[Mechanic]` log + skill HUD notuna kısa ad ("Dondurma", "Portal"...)
> yazılır; eşleme atom türüne göre (skill'e özel dal yok): düşmana tempo → Stun (dondur) /
> Slow, hareket → Root / Stun (havada, sersem), körlük → Blind, çekme → pull, yukarı fırlat →
> Stun `knockup_sec`, yer değiştirme → boss'un karşı tarafına (dash bitince); kendine tempo →
> Haste, gizlen (bulut sendeyse) → Stealth, hedefin arkasına ışınlan (dash bitince), işaretle-
> geri dön, portal çifti (`portal_life_sec`, yeni `portal_trigger_radius_m` = 1.0 önerilen).
> Eski motorun verdiği aynı durum tekrar uzatılmaz (`ApplyOnce`). **Doğrulandı (Editor Play,
> Kılıç):** 12-4 isabet → boss Stun 1 sn; 12-8 Haste; 3-9 boss arkasına; 3-10 A kapısına giren
> B'den çıkar; 3-4 2.2 sn sonra işarete döner; 3-6 temas → boss'un arkası. **Uydurma/önerilen
> sayılar:** `mechanic_grammar.params` tamamı; arkaya ışınlanma mesafesi =
> `BasicStrikeRangeM × 0.5`. **Bu turda ayrıca:** diğer ajanın `runtime-design-gaps` işi
> incelendi ve master'a alındı; koni hitbox'ı (SizeB açı, genişlik değil — Kontrol 30 m alan
> açıyordu) düzeltildi. `dotnet test` **303/303**; AtomSim A=0 B=0 D=0, 463 skill yalnız
> genel desende (ör. "Sert", "Delici").

> **28 Eylül — v6.1.1 runtime boşlukları (pasif/hitbox/mobility/radial).** Build ekranı
> seçili altılı içinden 0-2 pasif rünü `P` rozetiyle seçip korur; ikinci çizilen (sıfat)
> pasif slotundaysa `passive_duration_default` kadar açılır, aynı pasif kalan süreye eklenir,
> farklıları üst üste biner. Uyumsuz fiil-silah pasifi tetiklemez. Slot pasifinin engine
> hasar/lifesteal/reflect/hitbox ve root/slow/blind etkileri canlı hasar/status yollarına bağlı;
> 0 süreli 9/11 işaretlenebilir ama tetik no-op'tur. `hitbox_vfx` artık
> `base × weapon_size_mult × sifat_override.size_mult` kullanır: fiil 1 kapsül ve fiil 5
> 3 m küre dahil tüm executor yolları; `VFX_{Element}_{FiilId}_{SifatId}` registry asset
> arar, yoksa element renkli şekil üretir. `mobility_cc`: fiil+sıfat+silah mobilitesi,
> JSON CC süre/öncelik/aynı-CC süre ekleme, startup dodge kesmesi ve poise kırılması
> (hasar > eşik → iptal + 1 sn stun) bağlı. JSON poise tier eşlemesi vermediği için oyuncu
> prototipte nötr `orta=25`; mobility toplamı 0 üç kademeli eksenin ortası `slowed_move`
> kabul edildi. Element menüsü: E veya sol HUD chip basılı → 6'lı radial, JSON 300 ms,
> hasarda iptal, slow-mo yok; seçim yalnız isim/VFX boyasıdır. Radius/chip boyutu JSON'da
> olmadığı için `PrototypeTuning` varsayılanları eklendi. **Sunum yapılmadı:** ses, hitstop,
> shake, animation event ve boss Slam animator parametreleri dokunulmadı.
> **Doğrulama:** `dotnet test` **285/285** yeşil (5 yeni uçtan uca Core testi);
> Unity Editor bu ortamda yok, Play/dokunmatik/görsel ölçüm doğrulanamadı.

> **28 Eylül — atom grameri simülasyonu (dal `feat/atom-grammar-sim`; üstteki kayıtla ONAYLANDI).**
> Sahibi: "skiller çok benzer; duvar/portal/klon/zaman alanı gibi mekanikler elle atanmasın,
> fiil+sıfat+silah anlamından motor çıkarsın". Taslak kural seti `docs/atom-grammar-taslak.json`
> (bağlayıcı DEĞİL): Skill = Sıfat.kural(Silah.teslim(Fiil.atomlar)) → çakışma → etiket.
> Fiil = hitbox NE (atomlar: değer/hız/konum/varlık/yön), silah = teslim yolu (tier C),
> sıfat = hitbox NASIL (atom TÜRÜNE yazılmış tek kural, kombo tablosu yok).
> `tools/AtomSim` (`dotnet run --project tools/AtomSim`) 1440 skill üretir →
> `tools/AtomSim/out/rapor.md` + `skills.csv`. Son tur: A (sıfat nitel fark) 0 ihlal,
> B (aynı silahta aynı skill) 0, D (çelişki) 0; yol adı hariç bir skill 10 silahta ort.
> 9.9 farklı davranış; Top=Tılsım 9 ve Çekiç=Tılsım 6 skill'de örtüşüyor (Sabit/Yükselen
> silahı eziyor). Taslak `params` sayılarının tamamı önerilen/uydurmadır.
> **Oyun motoruna ve `element-sistemi.json`'a dokunulmadı** — sahip onaylarsa
> `mechanic_grammar` olarak taşınır ve Core'a yazılır.

> **28 Eylül — nişan yönü (boss hasar yemiyor).** Sahibi: "boss hasar yemiyor artık".
> Sebep: `ResolveAimFacing` dururken kamera orbit yaw'ını kullanıyordu; kamera oyuncunun
> arkasını izlemediği için vuruş sabit dünya yönüne (+z) gidiyordu. Menzil düzeltmesindeki
> 70° soft-aim konisi bu yönle kıyaslandığından boss'un yan/arka tarafından tüm tek hedefli
> vuruşlar (düz vuruş, melee kapsül, mermi) ıskalıyordu; `FaceBoss` ise karakteri görsel
> olarak boss'a çevirdiği için oyuncu "vurdum ama hasar yok" görüyordu. Düzeltme: nişan =
> hız, yoksa karakterin yüzü (kamera yok); `FaceAim` karakteri vuruşun gerçek yönüne çevirir
> (boss'a yalnız koni içindeyse). **Doğrulandı (Editor Play, 1-6, 2 m):** boss'un doğu/batı/
> kuzeyinden yüzü dönük → 40 hasar; 45° sapmayla → soft-aim vurdu; sırtı dönük → 0.
> **Doğrulanamadı:** telefonda joystick ile his.

> **28 Eylül — fiil executor'ları (3 Hareket, 7 Zayıflatma, 10 Yansıma, 11 Çağırma).**
> Sahibi: "fiilleri ve hareket skillerini yaz". Artık stub fiil yok; `SkillExecutorRouter`
> 7 → silah tipine göre Melee/Projectile (1/5 gibi), 3 → `Movement`, 10 → `SelfState`,
> 11 → `Summon`. Core: `VerbExecutionData` `hitbox_vfx.fiil_hitbox` (şekil/boyut/süre) ve
> `mobility_cc.i_frame` (3-7 400 ms dash sırasında, 11-10 300 ms minion doğarken) okur;
> `ApplyVerbHitboxSizing` boyutu `base × weapon.range_mult × sıfat hitbox ölçeği` ile
> uygular (`hitbox_formula`). Dash mesafesi `verb_base.dash_distance_m` (3 m), i-frame =
> Stasis. Game: `MovementExecutor` (dash yolunu kapsülle süpürür, hedefe bir kez uygular),
> `SelfStateExecutor` (oyuncuyu izleyen disk), `SummonExecutor` (minion boss'a yürür,
> menzilde vurur), `ManifestationDirector.VerbExecution.cs`. Güçlendirme = `buff_damage` +
> `self_damage_buff` (kapanış ve minion hasarına çarpan); Yansıma = `reflect_ratio` →
> `ActorStatus.GrantReflect` (pasif yansımasına eklenir). `duplicate_cast` (sıfat 11)
> `duplicate_delay_sec` sonra ikinci atış; 3-3 `bounce_targets` = ikinci dash sekmesi
> (`sifat_override` chain_count 2) ×`bounce_damage_mult`. Hata düzeltmesi: düşmanca sıfat
> modları (slow/root/burn/poison/silence) self fiillerde oyuncuya yazılıyordu (3-6 oyuncuyu
> köklüyordu) → artık hedef board'a; `accuracy_debuff` → Blind; lifesteal v6 anahtarı
> `lifesteal`. **Uydurma varsayılanlar (`ManifestationTuning`, JSON'da yok):** minion vuruşu
> 6, aralık 1 sn, hız 3.5 m/sn, erişim 1.4 m, boy 0.6 m. **Karar:** buff/yansıma cast anında
> verilir (JSON zamanlama söylemiyor). **Doğrulandı:** `dotnet test` 280/280; Editor Play'de
> 3-6 dash boss'u kökledi+yavaşlattı, oyuncu köklenmedi; 3-3 iki atış (×1, ×0.5, ~6 m);
> 3-7 3 m + i-frame görüldü, 3-1 3 m i-frame yok; 7-1 önde 2.5 m → hasar + ArmorBreak,
> arkada → 0; 11-1 minion 4.5 sn'de 32 hasar; 11-11 iki minion; 10-1 yansıma 0.5, süre
> sonunda 0. **Doğrulanamadı:** telefonda his; Güçlendirme buff'ının sayısal etkisi Play'de
> ayrıca ölçülmedi; minion görseli placeholder küre.

> **28 Eylül — savaş içi silah swap (`weapon_skill_interaction.swap`).** Sahibi: "2 silah
> seçip savaşırken değiştirme JSON'da yazmıyor mu". Core: `WeaponSwapRules.FromJsonRoot`
> (enabled / weapons_carried=2 / cooldown_sec=1.2 / animation_sec=0.25 / cancels_combo /
> dodge_cancels_swap / recovery_cancel) + `WeaponSwapState` (zaman parametre);
> `PlayerStateNode.CanSwap` + `PlayerStateMachine.AllowsSwap` (`state_machine.can_swap`);
> `ChainDirector.Reset()`. Game: `ManifestationDirector.WeaponSwap.cs` (swap animasyon
> bitince `EquippedWeapon` değişir → çarpan/uyum/pasif/executor yolu otomatik; combo
> zinciri sıfırlanır; dodge swap'ı iptal eder; Recovering'de swap kilidi keser). Build
> ekranı: 10 silah sırası, 2 seçim zorunlu (1 başlangıç, 2 yedek). Savaş HUD'u: altıgenin
> sol-altında dodge'un simetriği swap düğmesi (aktif ad + "⇄ yedek" + radial bekleme);
> klavye **Q**. **Uydurma/karar:** bekleme swap başlarken başlar ve dodge iptali beklemeyi
> sıfırlamaz (JSON sessiz); build seçilmeden varsayılan yedek = birincilden farklı sınıftaki
> ilk silah (Kılıç → Top); `WeaponSwapButtonRadiusDp=28`. Swap animasyonu görsel olarak
> stub (0.25 sn yalnız zamanlama; buton turuncuya döner). **Doğrulandı:** `dotnet test`
> 273/273 (5 yeni); Editor Play'de build ekranı silah sırası, swap Kılıç→Top tamamlandı,
> hemen ikinci istek `AlreadySwapping`, swap + dodge → "dodge iptal etti" ve silah
> değişmedi. **Doğrulanamadı:** swap düğmesine gerçek dokunuş / Q tuşu (kod yolu dodge
> düğmesiyle aynı), telefonda düğme yeri.

> **28 Eylül — menzil / arkadan vurma.** Sahibi: "arkam dönük uzaktan yumrukla vuruyorum,
> düz hasar yiyor; skill de aynı". Sebepler: (1) düz vuruş ve executor'sız fallback yolu
> hasarı menzile bakmadan veriyordu (menzil yalnız boss sarsılma görselindeydi);
> (2) soft-aim 8 m içinde yönü koşulsuz boss'a çeviriyordu; (3) melee executor kapsülü
> bang yarıçapıyla (3.6 m'ye kadar) şişip oyuncunun arkasına taşıyordu; (4) düşman alanı
> boss'un üstünde açılıyordu. Düzeltme: düz vuruş `IsBossInStrikeCapsule` (bakış yönünde
> `BasicStrikeRangeM`, kalınlık `TravelHitRadiusM`, arkaya taşmaz); fallback hasar/boss
> statüsü `IsClosingInRange` ile kapılı; soft-aim `PrototypeTuning.SoftAimConeDeg=70`
> yarım açı (**uydurma varsayılan**); tek hedefli melee kapsülü `TravelHitRadiusM`, yalnız
> Patlama (fiil 5) alan kalır; düşman FieldAura etkinin ucunda açılır.
> **Doğrulandı (Editor Play):** düz vuruş sırtı dönük 1.8 m → 0, önde 6 m → 0, önde 2.2 m →
> hasar; 1-1 sırtı dönük 1.8 m → 0; 1-4 önde 2.2 m → 40. 5-5 7 m'den vurur — veride
> `bang=9.72 m` (sıfat 5 alanı büyütüyor), tasarım gereği. **Doğrulanamadı:** 3/7/10/11
> fallback fiillerinin canlı menzili, düşman alan fiillerinin (6/12) yeni konumda tick'i.

> **28 Eylül — PR #15 düzeltmeleri.** Üç executor `Time.deltaTime` yerine
> `GameClock.WorldDeltaMs` ile ilerler (`SkillExecutor.WorldDeltaSec`); build menüsü açıkken
> mermi/alan/vuruş penceresi donar, `TimeDirector` ölçeğini izler. Koda gömülü üç sayı
> `ManifestationTuning`'e taşındı: `ExecutorProjectileMinHeightM=0.35`,
> `ExecutorBurstForwardFrac=0.35`, `ExecutorFieldDiskAlpha=0.6` — **uydurma varsayılan**
> (PR'daki değerler, kaynak yok). **Doğrulandı:** `dotnet test` 268/268, Unity derleme
> temiz; Editor Play'de Kılıç 1-1 → `MeleeHitbox` boss'a 54 hasar, Top (ranged) 1-1 →
> `Projectile` 77.8 hasar; `GameClock.Paused` iken uçan mermi 3 sn aynı konumda kaldı.
> **Doğrulanamadı:** alan (FieldAura) fiillerinin canlı tick'i, telefonda executor hissi.

> **27 Eylül — F2 debug silah döngüsü.** `V611DebugPanel` F2 + küçük butonla canonical
> 10 silahı id sırasıyla döndürür ve wrap eder; panelde güncel ad + effective
> `melee/projectile` yolu görünür. Tek canlı state `ManifestationDirector.EquippedWeapon`;
> `SkillPreviewHud`, `SkillFactory` uyum/çarpanları, animasyon key'i ve
> `SkillExecutorRouter` her cast'te bunu okur. Log:
> `[WeaponCycle] id=… name=… type=melee|ranged canonicalType=…`.
> JSON `medium` Kılıç/Mızrak effective melee kalır; yalnız canonical `ranged` projectile.
> **Doğrulandı:** `dotnet test` **268/268**; 10 canonical silahın fiil 1/5 route'u
> effective ranged/melee sınıfına göre testte doğrulandı. **Doğrulanamadı:** Unity
> Editor bu cloud imajında yok; F2 wrap + canlı melee/projectile Play smoke kullanıcıda.

> **27 Eylül — SkillExecutor prototipi.** `SkillMotor` adı/rolü değişmeden saf Core
> `SkillExecutorRouter` eklendi. İki-rün kapanışında fiil 1/5 explicit `weapon.type` ile
> melee overlap veya hareketli projectile'a; 2/4/6/8/9/12 süreli tick field'a gider.
> JSON `medium` (Mızrak/Kılıç) isim tahmini yapılmadan yakın temas, yalnız `ranged`
> projectile kabul edilir. 3/7/10/11 stub log + eski LivingEffect yoluna güvenli düşer.
> Game'de tam üç executor: `MeleeHitboxExecutor` (%30–70 Physics overlap; Patlama tek
> geniş sphere), `ProjectileExecutor` (kinematic primitive, presentation hızı +
> weapon range_mult, hit/range despawn), `FieldAuraExecutor` (presentation
> lifetime/tick + ince ground disk). Hasar/heal/status mevcut
> `ManifestationDirector`/vitals yoluna callback ile döner; animasyon köprüsü no-op olsa
> da fizik çalışır. `ManifestationTuning.ExecutorFieldTickSec=1` prezentasyon katmanındaki
> ortak tick varsayılanından gelir; melee pencere oranı görev kabul kriteridir.
> **Doğrulandı:** `dotnet test` **267/267** yeşil; router 1/5 için melee+ranged,
> alan fiilleri ve stub fiilleri kapsıyor. **Doğrulanamadı:** Unity Editor bu cloud
> imajında yok; canlı overlap/projectile/field Play görüntüsü alınamadı.

> **27 Eylül — build seçim ekranı (v6 7b).** Sahibi: "skiller ekranın ortasında saçma,
> 12 ründen 6 seçemiyorum". Yeni `BuildSelectScreen`: Play açılışında tam ekran; 12 rün
> kartı (fiil yüzü, sıfat yüzü, kategori, base_effect), seçim sırası = altıgen slotu,
> sağda altıgen önizleme (slota dokun = çıkar), ◀ ▶ ile `ana_classes_80` hazır build ve
> eşleşen class adı, `SAVAŞA BAŞLA`. Açıkken `GameClock.Paused` dünya saatini durdurur;
> altıgen/çubuk/orbit girdisi susar. Sağ üst `BUILD` / B savaş içinde yeniden açar.
> `V611DebugPanel` liste pick-6'sı kaldırıldı (F1 smoke + E element kaldı).
> `SkillPreviewHud` ekran ortasından altıgenin üstüne taşındı; çizimde ve cast sonrası
> `SkillPreviewHoldSec` (FeelTuning.ReadoutHoldMs 900) kadar görünür. `ShoutSkill` artık
> her cast'te dev `ReactionReadout` yazısını basmıyor (o kanal dodge/tepki/iyileşme için).
> Uydurma: `SkillPreviewWidthDp=240`, `SkillPreviewGapDp=10`, önizleme yüksekliği 48dp.
> Pasif yuva (0-2) seçimi ekranda yok — build pasifsiz uygulanır.
> **Doğrulandı:** Unity derleme temiz, `dotnet test` 256/256; Play'de ekran açılışta
> geliyor (dünya saati 0'da duruyor), sahibi hazır class ile iki kez uyguladı
> (`[BuildSelect] build=[1,5,6,4,8,3] class=Savaş Lordu` …), `SAVAŞA BAŞLA` → cast 1-5
> → önizleme altıgen üstünde, ortada dev yazı yok. **Doğrulanamadı:** telefonda dokunmatik
> ve dar ekran yerleşimi.

### v6.1.1 bağlayıcı uygulama sırası 1–9

`[x]` kod + statik veri kontrolü tamam; Unity Play gerektirenler ayrıca açık yazılır.

1. [x] **JSONLoader:** `ElementSystemJsonLoader`, tek canonical Resources kopyasını
   v6.1.1/binding/cardinality ile doğrular ve parse sonucunu cache'ler.
2. [x] **SO üretimi:** runtime 12 `RuneSO` + 10 `WeaponSO` + 6 `ElementSO` üretir.
   `Dovus → Import Element System v6.1.1 Assets` aynı veriden kalıcı asset üretir;
   importer bu ortamda Unity olmadığı için çalıştırılamadı.
3. [x] **SkillFactory:** 12×12=144 benzersiz `Skill`; seçili 6 ründe 6×6=36 skill.
   Element yalnız isim boyası, silah/uyumsuz çarpanları factory sonucundadır.
4. [x] **RuneManager:** 12'den tekrarsız 6 seçim ve build içinden 0–2 pasif rün doğrular.
5. [x] **SkillMotor → SkillFactory:** canlı `ManifestationDirector` iki-rün çözümünü
   factory'den alır; tek-rün çizim önizlemesi motor üzerinde kalır (skill değildir).
6. [x] **AnimationDatabase:** 120 JSON adı controller clip adlarında aranır; yoksa mevcut
   `Cast*` state fallback'i, o da yoksa temiz no-op/uyarı. Görsel Play kanıtı yok.
7. [ ] **UI:** 7a `SkillPreviewHud` altıgen üstünde (isim/prose/uyum rengi);
   7b savaş öncesi `BuildSelectScreen` (12 kart, pick-6, hazır class) bağlı; element
   cycle debug panelde; 28 Eyl: build ekranında 2 silah seçimi + savaş içi swap düğmesi.
   Radial element menü ve pasif yuva seçimi eksik.
8. [ ] **Playtest:** startup preflight JSON→SO 12/10/6→36 build skill→1-1'i doğrular;
   1 build + Kılıç + 1 element ve F1 fight smoke bağlı. Unity Editor olmadığı için
   görsel/fight kanıtı yok.
9. [ ] **Eski SO/listeleri sil:** yapılmadı; 1–8 Play'de doğrulanmadan yapılmayacak.

Kilitli davranış: yalnız 2-rün skill; silah-fiil uyumsuzluğu ×0.8 hasar / ×1.2 cast,
pasif kapalı, sarı (uyumlu yeşil); sıfat uyumsuzluğu yok; Zaman aktör `Slow`/`Haste`,
global `Time.timeScale` değil. Elle doğrulama: `docs/COMPAT.md`.

> **27 Eylül — üç dal birleşti (`feat/circular-arena-feel`).** Daire salon + Synty/Mixamo
> commit'i, `cursor/hexagon-cleanup-8244` ve `cursor/adopt-element-system-v6-1-079a`
> merge edildi. Çakışmalarda oyun mantığı v6 (noktalar = `RuneLoadout` slotu, element
> değil), adlar Hexagon; kırmızı-turuncu kuralı çıkarılmış kaldı. `Dovus.Game.Editor.asmdef`'e
> eksik `Dovus.Core` referansı eklendi (importer derlenmiyordu). Synty paketi ve Mixamo FBX
> lisans gereği `.gitignore`'da, yerelde kalır.
> **Doğrulandı:** `dotnet test` **256/256** (v6 270 − silinen slow-mo testleri); Unity
> 6000.4 derleme temiz; Play'de `[BindingReady] JSON 6.1.1 → SO 12/10/6 → buildSkills=36
> → smoke=Ateşli Yoğun Vuruş → weapon=Kılıç → element=Ateş`, altıgende build
> `[1,5,6,2,7,9]` etiketleri + `SkillPreviewHud` görünüyor. **Açık:** COMPAT 3–8
> (F1 smoke, pick-6, element cycle, uyum rengi, Zaman class) elle denenmedi; boss
> controller'ında `Windup/Slam/Idle/Stagger` trigger'ı yok (Synty/Mixamo controller uyarısı);
> `LivingEffectView.EnsureBangBurst` particle süresi hatası.

> **27 Eylül — v6.1.1 runtime doğrudan yüklüyor.** `SkillMotorLoader` →
> `SkillMotor`: 12 çift-yüzlü rün, 144 adet 2-rün fiil×sıfat skill, 6-of-12
> `RuneLoadout`, 80 ana class ve 6 element boya verisi. `PrototypeBootstrap` Inspector'daki
> ana class id + 0-2 pasif rün id'sini kullanır (varsayılan class 1). 10 silahın JSON
> çarpanları + `compatible_verbs` canlı; uyumsuz fiil = hasar ×0.8, cast ×1.2,
> pasif kapalı, sarı UI; uyumlu = tam, yeşil UI. Sıfat uyumsuzluğu yok. Zaman yalnız
> hedef `Slow` / oyuncu `Haste`; global zaman ölçeğine çağrı yok. Element boya katmanı
> hasar matematiğine girmez. Unity doğrulaması: `docs/COMPAT.md`.
> **Stub:** silah seçimi, pasif rün ve silah identity-passive efektlerinin tamamı,
> `hitbox_vfx` prefabları, özel clip/event'ler ve tam VFX/ses presentation.
> **Doğrulandı:** yerel .NET 8 ile Core derlendi; `dotnet test` **270/270 yeşil**.
> JSON yapısı, ayna eşitliği ve diff doğrulandı. **Doğrulanamadı:** Unity derleme /
> Editor Play / telefon (ortamda Unity Editor yok).

> **27 Eylül — v6.1.1 bağlayıcı veri güncel.** Docs + Resources aynalandı; yeni üst
> seviye anahtarlar: `ana_classes_80`, `skills_prose_144`, `hitbox_vfx`, `mobility_cc`,
> `uyumsuz_cizim`, `presentation`, `changelog_v6_1`, `design_warnings`. Zaman prose'u
> yalnız tempo (`enemy_slow` / `self_haste`), global slow-mo yok; class 41/50 ayrıştırılmış
> adları kanonik JSON'daki haliyle korundu.

> **27 Eylül — v6.1 bağlayıcı tasarım kilidi.** `docs/element-sistemi.json` ve
> Resources kopyası tam v6.1 ile değiştirildi; önceki v5.3
> `docs/archive/element-sistemi-v5.3.json` altında saklandı. Yeni sistem: 12 çift yüzlü
> rün (fiil+sıfat), 12'den tekrarsız 6-rün build, 2-rün gramerinden 144 skill,
> 0-2 pasif yuva. Element prototipte yalnız VFX/isim katmanı (`element_mult=1.0`);
> silahlar çarpan + animasyon + hitbox katmanı. Engine sayıları JSON'da bulunur.
> Multiplayer için global slow-mo yok; Zaman yalnız `enemy_slow` / `self_haste`
> (`no_global_timescale=true`) uygular. Bu ilk veri-kilidi commit'indeki loader açığı
> aynı dalın v6.1.1 runtime adaptasyonunda (yukarıda) kapatıldı.
> JSON bütünlüğü, 12×12=144 skill ve iki kopyanın birebirliği doğrulandı.
> **Doğrulanamadı:** `dotnet test` (bu ortamda `dotnet` kurulu değil).

> **23 Eylül — güncel tasarım temizliği.** `Pentagon*` dosya/tip/adları `Hexagon*`
> olarak değiştirildi; 6 nokta davranışı korunuyor. Merkez düz vuruş/erken kapanış,
> dodge altıgen dışında ayrı düğme. Perfect-dodge yavaş çekim sistemi (`SlowmoTuning`,
> `TimeDirector.TriggerSlowmo`, tuning UI/preset/testler) kaldırıldı; hitstop korunuyor.
> README/element spec/ajan kuralları güncellendi; kırmızı-turuncu kuralı değişmezlerden
> çıkarıldı. `dotnet test tools/CoreTests/CoreTests.csproj`: **244/244 yeşil**.
> **Doğrulanamadı:** Unity Editor bu ortamda yok; Play mode/Unity script derlemesi.

> **18 Eylül — 100 m çap daire zindan.** Long_Hall (avize/sütun/tavan görüşü
> kesiyordu) kalktı. `CircularArena`: r=50 m disk + 18 m yüksek çevre duvarı,
> tavansız. `ArenaClamp` daire sınır; `KinematicMotor` CapsuleCast + push-out
> (duvar içinden geçme). Karakter %50 büyük (`PlayerVisualScale=2.625` /
> `BossVisualScale=3.3`). Walk **7.5 m/s**. Locomotion tam stick = **Running**
> (Walking değil). Idle fidget freeze; cast 1.0. TuningVersion **15**.
> **Doğrulanamadı:** Play / telefon APK (bu oturumda Unity Play yok).

> **17 Eylül — Sert/sabit anim pass.** Idle → Fighting Idle; cast klipleri
> overdrive+0.65. Controller: exitTime 0.78 / blend 0.02s; cast speed ~1.5;
> `CharacterAnimSpeed=1.35`. Bind + APK telefona. **Doğrulandı:** Mixamo export
> + Bind log + install; his sahibi gözü.

> **17 Eylül — Karakter ölçek.** `PlayerVisualScale=1.75` / `BossVisualScale=2.2`
> (`PrototypeTuning`) — Long_Hall’da küçük silüet. Inspector’dan ayarlanır.

> **17 Eylül — Skill anim ayrımı.** `ActorVisual` SetTrigger kullanıyordu; Mixamo
> controller'da trigger yok → hep aynı/idle. Artık `Animator.Play` +
> `animation_type` map (projectile/aoe/self/dash/melee ayrı). PulseActor skill
> tipine göre. **Doğrulandı:** Play map tablosu; his sahibi gözü.

> **17 Eylül — T-pose fix.** Nested Synty prefab'da Animator override Instantiate'te
> düşüyordu (`ctrl=null`). Unpack + controller kaynak/wrapper'a yazıldı; Bind artık
> GUID silmiyor. **Doğrulandı:** Play 3× Animator `Player_Synty`/`Boss_Synty`, CastSweep.

> **17 Eylül — Anim çeşitlendirme.** Önce çoğu skill `CastChannel`/`CastGuard`’a
> düşüyordu (hep aynı Spell_Cast). Map: projectile→Pierce, aoe→Sweep,
> self/channel→Channel, dash→**Dodge** (roll). Clip seçimi dosya adına göre.
> **Doğrulandı:** map string’leri; Play his sahibi gözü.

> **17 Eylül — Mixamo full anim set.** 10 FBX (Idle/Walking/Running/Melee_*/
> Spell_Cast/Dash/Hit/Death) → `Assets/Art/Mixamo/`. Controllers
> `Player_Synty`/`Boss_Synty` (CastPierce/Sweep/Slam/Channel/Guard + Locomotion
> blend) → Synty prefabs. `tools/mixamo-download.mjs` + `MixamoAnimatorBind`.
> **Doğrulandı:** Play Humanoid + Locomotion + CastSweep tetik.

> **17 Eylül — Animasyon bütçe kararı.** Synty ANIMATION paketleri alma
> (Sidekick uyumsuz). Mixamo $0 yeterli; KayKit yedek (~$0 itch / ~$12 Store).

> **17 Eylül — Synty Long_Hall arenası.** Promo salon: Demo.unity `Long_Hall`
> (+ URP lights) → `ArenaVisual_Synty`. Kapalı salon (sütun/avize/banner),
> boş platform değil. `Dovus/Synty/Bind Demo Hall Arena`. Atmosphere torch/bloom.
> **Doğrulandı:** Play bounds ~38×76 m, 96 light.

### Sahip kararları (17 Eylül)

| Konu | Karar |
|---|---|
| `UseFormulaDamage` | **true** — formulas + crit_system canlı |
| StatusTuning ↔ status_durations | deneme süresinde **böyle kalsın** |
| Enforce mana/CD | deneme süresinde **false kalsın** |
| state_machine bağlama | **bağlandı** — SyncWorld + çizim/dodge/hareket kapısı |
| fiil `action` özel motor | **gerekli** — henüz yazılmadı (sıradaki) |
| invisible_link / tear | **bağlandı** (Karabasan hat / Hiçlik yarığı) |

> **17 Eylül — Karabasan hattı (Ateş-Karanlık) fix.** Sahip: "koruma silme
> yazıyor, 3lü 4lü farketmiyor". (1) Reality `NoteSkill(e.Id)` ShoutSkill
> adını eziyordu → readout'a yazma kaldırıldı (erase hâlâ uygulanır, debug not).
> (2) `delayed_detonation` her ElementOrigin=Karabasan'da değil, yalnız sıfat
> `trigger_profile=delayed_detonation` (Geciktirme / 2'li) iken. 3'lü Yayma
> anında bang + 2.5× alan. ShoutSkill alt satıra length role. Test:
> `KarabasanLine_Fold2_3_4_DifferByNameAndAdjective`. `dotnet test` **258**.
> **Doğrulanamadı:** telefon APK (bu oturumda yeniden yükleme yok).

## JSON ↔ oyun — derin boşluk matrisi (17 Eylül tarama)

Kaynak: `docs/element-sistemi.json` × `SkillMotor` parse × `ManifestationDirector`/Game.
A=oyunda hissedilir · B=kod var kapalı/kısmi · C=parse/Core only · D=motor okumaz.

### Katman özeti

| JSON bölümü | Parse | Game | Not |
|---|---|---|---|
| elements / verbs / adjectives / lengths | A | A | Resolve + LivingEffect köprüsü |
| mechanics → StatusKind | A | A | Stealth eklendi; confuse yok (sıfat→Blind+Slow) |
| action string (40 fiil) | A (taşınır) | **B/C** | Çoğu action yok sayılır; yalnız mechanics/hitbox işler |
| formulas + crit_system | A (DamageCalculator) | **A** | `UseFormulaDamage=true` (17 Eyl sahip) |
| resource / cooldown rules | A | **B** | Enforce* bayrakları false |
| status_durations | D | **B** | StatusTuning sabit; shield 50/5s hizalandı, stun/root vb. hâlâ kısa |
| passives | A | **A/B** | crit/armor/reflect/dashCD/damage_taken bağlı; reveal_radius yok |
| active_modes (ulti) | A | **A** | cast/dash + afterimage/taunt/aura/screen_edges |
| chain_mechanics | A | **B** | çarpan+HUD; finisher dünya efekti yok |
| zone_layer | A | **A/B** | spawn+CC root/slow; zone_lock/block wall collider yok |
| space_layer | A | **A/B** | blink/zenitsu/stealth_shift + **invisible_link + tear** |
| time_layer | A | **A** | echo+extend+delayed_detonation+death_delay |
| reality_layer | A | A | revive_block / erase |
| state_machine | A | **A** | PlayerStateMachine ↔ SentencePhase/dodge/CC |
| equipment_system | A | **A** | 28 Eyl: build'de 2 silah + savaş içi swap |
| status_interaction_table | A | A | Rebuild + 3 özel satır |
| prezentasyon hitbox `target_ally` | — | **B** | 6 fiil HitboxFound=false |
| atoms / lore / examples | D | D | kasıtlı |
| ui_rules | kısmi | B | zone transparency evet; readout 1500≠900 |

### Fiil `action` — isim var, özel motor yok (atlanan hissi)

Bunlar JSON’da ayrı action; oyun çoğunlukla yalnız `mechanics` uygular:

| action | fiil örneği | Gerçekte olan |
|---|---|---|
| revive | dirilis | cleanse only — diriltme yok |
| clone | golge_klonu | fear only — klon yok |
| confuse | yon_sasirtma | blind+slow — StatusKind.Confuse yok |
| mark | iz_birakma | slow only — işaret debuff yok |
| reflect | adaptif_zirh | shield only — yansıtma yok |
| reveal | ifsa | blind — stealth açma yok |
| gather_enemies | dusman_toplama | knockback — pull yok |
| block_area / zone_lock | kaya/lav | root (+zone) — duvar collider yok |
| transfer / erase / absorb / balance / stamina_drain | … | weaken/DR/stasis vb. kısmi |

### Sıfat `engine_modifiers` — bağlı vs atlanan

**Bağlı:** damage_mult, hitbox_*, lifetime_add, trajectory_override, cast_time_mult (SkillMobility), apply_slow/root/burn/poison/silence/knockback/DR/pull/lifesteal/stealth/confuse, burn_damage_mult, crit_chance_add (Game).

**Atlanan (JSON’da var):** apply_invulnerability_frames, apply_armor_shred, apply_stamina_drain, absorb_damage_to_heal, reflect_projectiles, reveal_stealth, spawn_minion, duplicate_cast, cancel_enemy_cast, cooldown_mult, pierce_armor_flat, ignore_resist, accuracy_debuff, max_targets, tick_rate_mult, trigger_delay/profile, target_auto_lock.

### Pasif / ulti — kalan boşluklar

PassiveDirector: `RevealRadiusMult` hâlâ uygulanmıyor.

### Öncelik önerisi (kalan)

1. Fiil `action` özel motorları (revive/clone/mark…)  
2. (deneme sonrası) süre otoritesi + Enforce  
3. Hexagon rename / Kenney VFX  

> **17 Eylül — ulti VFX polish.** `visual.aura` / `screen_edges` parse +
> `ActiveModeVfx` (halka + kenar vignette, oyuncu camgöbeği/mor paleti).
> Fırtına `afterimage_count` → AfterimageTrail.CountOverride; Aşılmaz
> `taunt_radius_m` → boss Taunt yenileme. `dotnet test` **257** yeşil.
> **Doğrulanamadı:** Unity Play ulti görselleri.

> **17 Eylül — Play mode fix + state_machine bağlama.** `playerVitals` CS0841
> (SpaceHost.Bind sırası) düzeltildi. `PlayerStateMachine.SyncWorld` öncelik:
> dead&gt;stunned&gt;dodging&gt;rooted&gt;casting(pending bang)&gt;drawing&gt;recovering&gt;idle.
> HexagonInput çizim/dodge + KinematicMotor hareket kapısı. `dotnet test` yeşil.
> **Doğrulanamadı:** Unity Play his (drawing’de hareket kilidi).

> **17 Eylül — Karabasan hat + Hiçlik yarığı.** `ISpaceDirector` / `SpaceDirector`
> (Core) + `Game/Directors/SpaceDirector` host: link 0.5s tick (3 dmg / 1.5 heal),
> 12m kopma, owner hasarda kopma; tear 30 ilk geçiş (boss+ally+player), süre bitince
> kapan animasyonu. Sayılar `SpaceLayerTuning` (JSON'da drain/range yok — görev).
> `dotnet test` **254** yeşil. **Doğrulanamadı:** Unity Play / telefon VFX.

> **17 Eylül — UseFormulaDamage=true.** Sahip: formül+crit açılsın; status süreleri
> ve Enforce deneme süresinde eski hali. `dotnet test` yeşil beklenir.
> **Doğrulanamadı:** Unity Play hasar/crit hissi.

> **17 Eylül — kararsız paket.** time_layer `delayed_detonation` (Karabasan bang
> hasarı delay_sec sonra) + `death_delay` (Cehennem ölüm → çökme ertelenir).
> Sıfat: `apply_pull` / `apply_lifesteal` / `apply_stealth` / `apply_confuse`→Blind+Slow.
> Pasif: CritChanceAdd, DamageTakenMult, ArmorAdd, ReflectRatioAdd, DashCooldownMult.
> Ulti: cast_time_mult × attack_speed_mult (bang gecikmesi), dash_cooldown_mult.
> `dotnet test` **248** yeşil. **Doğrulanamadı:** Unity Play / telefon.

> **17 Eylül — shield/stealth + zone CC okunurluğu.** `savunma` mechanics
> `shield`+`damage_reduction`; `gizlilik`/`hiz_gorunmezlik` → `stealth`.
> `StatusKind.Stealth`: hasar yutar, boss Approach/vuruş keser, yarı saydam.
> Shield tuning JSON `status_durations.shield` (50 / 5s). Zone CcKind → disk alfa 0.85.
> `dotnet test` **244** yeşil. **Doğrulanamadı:** Unity Play / telefon.

> **17 Eylül — space_layer → SkillMotion.** `SkillMotionMotor.Resolve(..., SpaceEffects)`:
> short_blink/stealth_shift → blink mesafe+iframe (JSON otorite); phase_blink.distance_m
> → Zenitsu engage. Portal köprüsü zaten `StateBridgeBoard` (sabitleme×2).
> `invisible_link` / `tear` 17 Eyl'de SpaceDirector ile bağlandı (aşağıdaki oturum).
> `dotnet test` **241** yeşil (o gün).
> **Doğrulanamadı:** Unity Play / telefon blink mesafeleri.

> **17 Eylül — Bağlama 11: reality + zone CC.** `SkillMotor.RealityEffects` parse;
> `ManifestationDirector` ElementOrigin ↔ revive_block / partial_erase / full_erase;
> `PlayerVitals.SetReviveBlockedGate` respawn kapısı; zone spawn `skill.Mechanics`
> root/slow → `ZoneInstance.CcKind`, tick’te boss board yenileme; `BossDirector.Approach`
> `EffectiveBlocksMovement` / `MoveSpeedMult` dinler. `dotnet test` **237** yeşil.
> **Doğrulanamadı:** Unity Play / telefon (Cehennem revive_block, Kaya zone root).

> **17 Eylül — LivingEffect + length ekonomisi.** `SkillWorldPlanner` /
> `LivingEffect.ApplyPlan` / `FromSkill` / bang scale / sıfat `apply_*` / HUD.
> `SkillMobility`: length×verb mobility (motion istisnası); bang gecikmesi ×
> `LengthCastMult`×sıfat `cast_time_mult`; mana × `LengthResourceCostMult`;
> Building≥3 Slow/Root yenileme. Bang’te trajectory `vfx_trail_type` →
> PlaceholderFactory trail. `yayma` JSON + Resources kopyası düzeltildi.
> `dotnet test` **234** yeşil. **Doğrulanamadı:** Unity Play / telefon APK.

> **17 Eylül — Telefon build.** `dovus-prototip.apk` (~42.5 MB, CleanBuildCache)
> → `adb install -r` Success (YXQC5PTGUCEQMNV4) + launcher açıldı. Not: incremental
> Bee "Not rebuilding Data files" eski APK'yı bırakabiliyor; taze için
> `BuildOptions.CleanBuildCache` veya `Library/Bee/Android` wipe.
> **Önemli:** o APK hâlâ eski HUD (`b800260`); taze master HUD için yeniden build.

> **17 Eylül — Düz vuruş yan etki fix.** Merkez jab `BasicStrikeDot` (varsayılan
> Ateş) yüzünden skill gibi işleniyordu: mana `ApplyResourceCost`, peşpeşe jab →
> Ateş zinciri (`BeginChainClosing`), pasif/ulti tetik, boss `BossKnockbackM×0.55`
> geri itme. `FireClosing`: basic’te zincir/pasif/ulti/mana/CD yok; pending zincir
> bonusuna dokunulmaz. `ApplyBossClosingBasic` yalnızca sarsıntı (knock=0).
> `HexagonInput.TriggerCenter` mana/CD kapısını atlar. `dotnet test` **227** yeşil.
> `master`'a merge edildi.

> **17 Eylül — HUD layout/vitals fix.** Telefon: hex 108dp+YNorm 0.18 alt
> rünleri kesiyordu; dodge Hava üstüne biniyordu; Hava ikonsuz düz disk;
> can barları kalın/düz. `FittedRadiusPx` safe+dodge boşluğuna kısar;
> dodge hex sağ-alt dışı; gölge+rim; prosedürel Hava rüzgâr ikonu.
> Vitals: ince pill bar. Tuning v13: radius 98dp + dodge clearance 40dp.
> `dotnet test` **227** yeşil. `master`'a merge edildi.

> **17 Eylül — His / HUD / kamera / iz.** Premium HUD: boss üst orta +
> `StatusIconStrip` (tüm `StatusKind`, `StatusBoard.TryGet` + süre halkası);
> oyuncu HP/FP sol üst glass; ally `StatusBoard` (`team_has_debuffs`).
> SafeArea clamp (`HexagonLayoutScreen.SafeRectPx`); hex YNorm 0.30,
> dodge/merkez büyütüldü. Orbit: sağ boşluk drag (widget-only claim) +
> `KinematicMotor` kamera-göreli hareket. Soft aim `SoftAimRangeM` (8m,
> boss menzilde soft-lock). Floating hasar pool. `AnimationBridge.MapToQuaterniusState`;
> bang → `PlaceholderFactory.CreateImpact`. Hava rengi muted teal.
> Canvas adı `HexagonCanvas`. `dotnet test` **227** yeşil.
> Mockup otorite: `docs/mockups/hud-premium-modern.png`.

> **16 Eylül — Bağlama 10: AnimationBridge + PresentationValidator Game'e.**
> `ManifestationDirector.ShoutSkill` → `PresentationValidator` /
> `PresentationCatalog.TryGetAnimation(AnimationType)` → `AnimationBridge.Play`
> (`ActorVisual.Animator`). `PulseRune` (`PulseActor`) **silinmedi**. Eksik
> Quaternius state → SafeSetFloat gibi sessiz atla (`LastPlayApplied=false`,
> konsol hatası yok). MCP Play: Ateş×2 → `melee_thrust`/`Melee_Thrust`
> applied=false; aynı köprü `CastPierce` → applied=true + state CastPierce.
> `dotnet test` 226 yeşil.

> **16 Eylül — Bağlama 9: Equipment sabit kuşam + match bonus.** `PrototypeBootstrap`:
> `EquipmentCatalog` → sabit **Alev Kılıcı** (Ateş); seçim UI yok.
> `ManifestationDirector.ApplyClosingDamage`: `outMult` × `EquipmentBonusResolver.Resolve`
> (fiil çekirdeği = `ElementId` ilk rün → CoreName; bileşik Alev/1-1 → Ateş).
> MCP Play: Ateş×2 → eqMult **1.1**, dealt **8.316** (= ClosingDamageMath 7.56×1.1);
> Su×2 → eqMult **1.0**. `dotnet test` 226 yeşil.

> **16 Eylül — Bağlama 8: TimeEffectDirector echo + extend_lifetime.** `SkillMotor`
> `time_layer` parse (`TimeEffectNode`, max_active_fields=2). `ManifestationDirector`:
> `TimeEffectDirector` + `FireClosing` sonrası Alev→`alev_yanki` (`TryScheduleEcho`,
> delay 1s, ratio 0.6) → `CollectDue`/`ApplyEchoDamage` (tekrar echo planlamaz);
> Lav→`lav_kalicilik` zone `RemainingSec × 1.7` (`ZoneDirector.TrySetRemainingSec`).
> (17 Eyl kararsız paket: `delayed_detonation` + `death_delay` da bağlandı.)
> MCP Play: Ateş×2 (Alev/Ateş Topu) Commit → bang `-7,6` + field `alev_yanki`;
> Tick+1.1s → yankı **`-4,5`** (0.6×); Lav (1-4) zone rem **17** (10×1.7).
> `dotnet test` 226 yeşil.

> **16 Eylül — Bağlama 7: ZoneDirector Game'e bağlı.** `ManifestationDirector`:
> `ZoneDirector(Skills.MaxActiveZones)` + `ZoneFieldView` (GroundScarField deseni).
> `FireClosing`'de `ElementOrigin` ↔ `zone_layer.zones[].element` (örn. Kaya→`kaya_duvari`)
> → `TrySpawn`; `PlaceholderFactory.CreateZoneDisk` (Toprak `#877dd9`, alfa 0.6 =
> `ui_rules.zone_display.transparency`). Radius = `ManifestationTuning.ZoneDefaultRadiusM`
> (**3.6**, JSON'da RadiusM yok). `Tick` + soft-cap; süre bitince görsel kapanır.
> MCP Play: Toprak×2 Commit → `Zone_1_Kaya` canlı; Tick(11) → zones=0 / live=0.
> `dotnet test` 226 yeşil.

> **16 Eylül — Bağlama 6: ChainDirector Game'e bağlı.** `ManifestationDirector`:
> `Queue<int>` son N cast elementi (fiil = ilk rün) + `ChainDirector.RegisterCast` her
> `FireClosing`'de. **Links yorumu (JSON net değil, seçilen):** `links[i]` = i. eşleşme
> sonrası **bir sonraki** kapanışın hasar/heal çarpanı (`_pendingChainBonus`); geçmiş
> cast'lere geri yazılmaz. Pattern tamamlanınca Finisher → `ReactionReadout` (ör.
> "kırmızı patlama" / Ateş zincir) + `rules.finisher_mult` (2.0) bir sonraki kapanışa.
> Ara link bonusları da aynı "sonraki cast" kuralıyla. MCP Play: 6× Ateş (dot 1)
> Commit → Finisher **"kırmızı patlama"** ekranda. `dotnet test` yeşil.

> **16 Eylül — Bağlama 5: PassiveDirector Game'e bağlı.** `ManifestationDirector`:
> `PassiveDirector` (`SkillMotor.Passives`) + `PassiveHud` (sol-üst, camgöbeği;
> ReactionReadout/ActiveModeHud'dan ayrı). `FireClosing`'de `TryActivateMode` yanı sıra
> `TryTriggerPassive` (dot dizisi = rünler). `ApplyClosingDamage` `outMult` ×
> `PassiveDirector.DamageMult` (ulti ile aynı nokta, çarpımsal); heal × `HealMult`,
> lifesteal += `LifestealAdd`. Ulti (`ActiveModeDirector`) dokunulmadı.
> MCP Play: [1,2,3,1]→`alev_hiddeti`; +[6,3,6,3]→dual mult **1.38** (1.15×1.2);
> 10s sonra yalnız alev; 15s hepsi kapandı. `dotnet test` 226 yeşil.

> **16 Eylül — Bağlama 4: EnforceCooldown.** `CombatTuning.EnforceCooldown`
> varsayılan **false** (Görev 12 kozmetik radial birebir). `true` → cümle
> başlamadan `PlayerCooldown.CanStart(verbId)` (GCD 0.3s + fiil
> `base_cooldown_sec`); bang'de `TryBeginCast` + radial `BeginTrackedCooldown`
> (CooldownTracker kalanı). Deny: `ReactionReadout` "soğumada" +
> `SyllableFeedback.PlayDenied`. Dodge'a dokunulmaz. `PlayerCooldown` JSON
> `cooldown_rules` (0.3 / 1). MCP Play: A flag=false → TryAllow true; B
> flag=true+CD → deny + radial label `3` (Ateş Dokunuşu 3s); C CD sonrası
> CanStart true. `dotnet test` 226 yeşil.

> **16 Eylül — Bağlama 3: EnforceResourceCost.** `CombatTuning.EnforceResourceCost`
> varsayılan **false** (Bağlama 2 birebir). `true` → cümle başlamadan önce
> `PlayerResource.CanAfford(verb.base_resource_cost)`; yetmezse `OnDotTouched`/
> merkez düz vuruş reddedilir, `ReactionReadout.NoteDenied("yetersiz mana")` +
> `SyllableFeedback.PlayDenied`. Dodge / toparlanma kilidine dokunulmaz. Bang'de
> hâlâ `Consume` (engellemez). `length.resource_cost_mult` çarpılmıyor.
> MCP Play: A flag=false+mana0 → Building; B flag=true+mana0 → deny + HUD
> "yetersiz mana"; C flag=true+mana100 → Building. `dotnet test` 226 yeşil.

> **16 Eylül — Bağlama 2: ResourceTracker Game'e bağlı.** `PlayerResource` (PlayerVitals'a
> paralel; HP'ye dokunulmadı) + `ResourceTracker.Consume` (yetmezse 0'a kilit, cast
> engellemez). `VitalsHud` üçüncü bar: mana, oyuncu camgöbeği/mavi. Cast bang'de
> `base_resource_cost` düşer; `regen_per_sec`/delay JSON varsayılanı. Bağlama 3
> `EnforceResourceCost` eklendi (varsayılan false). MCP Play: Mana 45/100 barı görünür; Consume 76→87 regen;
> Ateş Dokunuşu cost=10. `dotnet test` 225 yeşil.

> **16 Eylül — Bağlama 1: DamageCalculator bayraklı.** `CombatTuning.UseFormulaDamage`
> varsayılan **false** (ClosingDamageMath birebir). `true` → `DamageCalculator`
> (resistance=0, weakness=1; boss direnci ayrı görev). Crit → `DamageNumberHud.ShowDamage(amount, isCrit)`
> sarı+#58 / normal beyaz+#42. ClosingDamageMath **silinmedi**.
> MCP Play: flag=false → 13.09 = ClosingDamageMath; flag=true+crit → 68 (40×0.85×2),
> HUD `-68` #FFEB33 size 58. `dotnet test` 224 yeşil.

> **16 Eylül — Görev 15: AnimationBridge.** `Game/AnimationBridge.cs` — `AnimationFrameNode`
> + `Animator`: `animator_state` → `Animator.Play` (Controller'da yoksa
> `Debug.LogWarning`, fırlatmaz — SafeSetFloat deseni). worldMs frame-timer:
> `frame * (total_duration_ms / total_frames)` → `DamageFrameReached` /
> `SpawnVfxFrameReached` (`every_tick` → active_frames aralığında her kare;
> null → hasar event yok). Bootstrap/ManifestationDirector **bağlanmadı**.
> Unity MCP Play: Quaternius `Player_Quaternius` + `CastPierce`; startMs=1000,
> damage frame 12 → **1200** ms (PASSED). JSON `animator_state` isimleri
> (`Spell_Cast_Projectile` vb.) Quaternius controller'da yok — Faz 6 eşleme
> (Bilinen açıklar).

> **16 Eylül — Görev 16: PlaceholderFactory.** `Game/PlaceholderFactory.cs` —
> `vfx_binding.trail_vfx` / `impact_vfx` için `Resources/Vfx/{Trail|Impact}/{style}`
> yoksa (şu an hiç yok) `element_colors.primary` ile LineRenderer veya küre
> (`PrimitiveMesh`, collider yok). Uyarı stil başına bir kez
> (`asset_missing_handling.log_warning`). Katalog:
> `Resources/Presentation/prezentasyon-katmani.json` (+ gömülü hex yedek).
> Play doğrulama: `PlaceholderFactoryProbe` — Ateş `#c45c26` impact küre +
> straight trail çizgi (MCP Play mode). LivingEffectView dokunulmadı; Bootstrap'e
> bağlı değil. ManifestationDirector VFX spawn yok (Faz 6). `dotnet test` 224 yeşil.

> **16 Eylül — Görev 14: PresentationValidator.** `Core/Presentation/PresentationValidator.cs`
> — `SkillResolution.Hitbox` → `PresentationCatalog.Hitboxes`, `AnimationType` →
> `Animations` (trajectory eşleştirme **yok**, Görev 16). Eşleşmeyen id sessizce
> `HitboxFound`/`AnimationFound=false` (fırlatmaz). **Eksik hitbox:**
> `SkillResolution.Hitbox` değeri **`target_ally`** prezentasyon katmanında yok —
> fiiller: `cc_arindirma`, `hiz_buff`, `kalkan_transferi`, `arindirma`, `kutsal_kalkan`,
> `dirilis` (6/42). Tüm `animation_type` değerleri katalogda. ManifestationDirector
> bağlama yok. `PresentationValidatorTests` 3; `dotnet test` 224 yeşil.

> **16 Eylül — Görev 13: PresentationCatalog.** `Core/Presentation/PresentationCatalog.cs`
> — MiniJson ile `docs/prezentasyon-katmani.json` (binding:false, SkillMotor'a karışmaz).
> `TrajectoryNode` / `HitboxNode` (Raw + GetFloat/GetBool/GetString) +
> `AnimationFrameNode` (`DamageAppliedAtFrame` JsonValue; CancelWindow nullable) +
> `CompatibilityResult` enum (✓/⚠/✗ → Compatible/Special/Incompatible).
> **Sayılar JSON'dan:** Trajectories=16, Hitboxes=16, Animations=10 (changelog "15"/"17"
> yanlış). `PresentationCatalogTests` 3; `dotnet test` 221 yeşil. Game bağlama yok.

> **16 Eylül — Görev 11: EquipmentCatalog + ElementMatchBonus.** `Core/Equipment/` —
> `EquipmentSlot` / `EquipmentItem` / `EquipmentCatalog` (MiniJson,
> `equipment_system.examples` → 18 item, Id=`{slot}:{element}`) +
> `EquipmentBonusResolver` (`rules.element_match_bonus` "+%10 etki" → 1.1; silah×skill
> element eşleşince çarpan, yoksa 1.0; yüzde ParseDefenseDropMult deseni). Envanter UI /
> PlayerVitals / ManifestationDirector **bağlanmadı** (sabit tek ekipman varsayımı).
> `EquipmentCatalogTests` 2; `dotnet test` 218 yeşil.

> **16 Eylül — Görev 12: UI Rules hizalaması (Game).** `HexagonView` + `ManifestationDirector`:
> her skill cast'te fiil rünü etrafında kozmetik radial cooldown (`base_cooldown_sec`) + kalan sn
> (`ui_rules.cooldown_display`). **CooldownTracker / cast engeli yok** (Faz 6).
>
> **Sayı karşılaştırması (değiştirilmedi — sahibi karar verir):**
> | kaynak | alan | değer | |
> |---|---|---|---|
> | `ui_rules.read_as_display.duration_ms` | JSON | **1500** | |
> | `FeelTuning.ReadoutHoldMs` | Core | **900** | **farklı** |
>
> **zone_display notu:** `ui_rules.zone_display` = `in_world`, transparency **0.6** —
> Bağlama 7 `PlaceholderFactory.CreateZoneDisk` alfa olarak uygulandı.

> **16 Eylül — Görev 4: PassiveDirector.** `Core/Combat/PassiveDirector.cs`: `SkillMotor.Passives`
> listesini alır; `TryTrigger(dot[], worldMs)` ile `trigger_combo` birebir eşleşen pasifi açar;
> `DurationSec` dolunca `Tick` düşürür. Cooldown yok; birden fazla pasif aynı anda aktif
> (ActiveModeDirector tek-mod farkı). Effects `JsonValue` + birleşik çarpan/ek accessors.
> **JSON sayısı 10** (gorev-listesi "12" yazıyordu — yanlış; test `EqualTo(10)`).
> ManifestationDirector'a bağlanmadı. 6 yeni test; `dotnet test` 216 yeşil.

> **16 Eylül — Görev 6: StatusReactionTable canlı JSON.** Elle 14 kural listesi kalktı;
> `Rebuild(motor.StatusInteractions)` effect metninden magnitude/süre alanlarını okur.
> `SkillMotorLoader` yüklerken Rebuild çağırır. 3 satır tabloda yok (özel yol): burn+poison
> (Tick), shield+burn (Tick), stun+knockback (Applicator) — StatusKind id'leri eşleşiyor ama
> genellenebilir kural değil. Eşleşmeyen mechanic id bu turda yok. StatusBoard/Applicator
> dokunulmadı. `dotnet test` + StatusBoardTests regresyon yeşil.

> **16 Eylül — Görev 10: RealityEffectDirector.** `Core/Combat/RealityEffectDirector.cs`
> (`manipulation_layers.reality_layer`): `ApplyReviveBlock` / `IsReviveBlocked` (Cehennem,
> worldMs bayrağı — PlayerVitals respawn'a **bağlanmadı**); `ApplyPartialErase` (Karabasan:
> shield/haste/damage_reduction); `ApplyFullErase` (Hiçlik: yalnızca shields).
> `StatusBoard.RemoveKinds` eklendi (`CleanseHostile` değiştirilmedi). **full_erase
> minions/summons:** oyunda minion/summon sistemi yok — uygulanamaz (aşağıdaki açık).
> 7 yeni test. ManifestationDirector'a bağlanmadı (Faz 6).

> **16 Eylül — Görev 9: TimeEffectDirector.** Core/Combat/TimeEffectDirector.cs —
> manipulation_layers.time_layer 4 tipi (worldMs): delayed_detonation / echo
> (damage_ratio) / extend_lifetime (RemainingSec × multiplier, alan yuvası tutmaz) /
> death_delay (beyan anı + delay). Soft-cap max_active_fields=2. CollectDue tetik
> listesini çıkarır; PlayerVitals/BossVitals/Core/Time/* **dokunulmadı** (Faz 6 bağlama).
> 7 yeni test; dotnet test yeşil (TimeEffectDirectorTests 7/7).

> **16 Eylül (Görev 8) — space_layer okuma (SkillMotor), SkillMotionMotor dokunulmadı.**
> SpaceEffectNode + ParseSpaceEffects → SkillMotor.SpaceEffects (JSON'da 5) /
> MaxActiveLinks (3). Opsiyonel alanlar Has* + değer. **Uygulama yok** —
> SkillMotionMotor / SkillMotionTuning hâlâ sabit sayılar; davranış değişmedi.
>
> **Sayı karşılaştırması (otorite sahibi karar verir — bu turda bağlama yok):**
> | JSON effect | alan | JSON | Tuning/kod | |
> |---|---|---|---|---|
> | lev_isinlanma short_blink | distance_m | 3 | ShortBlinkDistanceM=4 | **farklı** |
> | lev_isinlanma | i_frame_ms | 300 | ShortBlink iframeMs=0 (çağrıda) | **farklı** |
> | yildirim_zenitsu phase_blink | distance_m | 8 | ZenitsuEngageRangeM=9 | **farklı** |
> | yildirim_zenitsu | damage_on_pass | true | ZenitsuSlashCommitMult=1 (kesi var) | kavramsal yakın, birim farklı |
> | yildirim_zenitsu | (i_frame yok) | — | ZenitsuIframeMs=220 | JSON'da yok |
> | pus_gecisi stealth_shift | distance_m | 5 | ShortBlink yolu → 4 | **farklı** |
>
> dotnet test yeşil (SpaceEffects.Count == JSON).


> **16 Eylül (Görev 3) — PlayerStateMachine + state_machine okuma.**
> `SkillMotor.ParseStateMachine` → `PlayerStates` (9) / `BossStates` (6).
> `Core/Combat/PlayerStateMachine.cs` anlık durum + `can_draw`/`can_move`/`can_dodge`/
> `i_frames` okur (bool veya string capability → metin: `"true"`/`"false"`/`"partial"`/
> `"based_on_cast_mobility"`/`"limited"`/`"dodge_direction"`). **SentencePhase'e bağlama
> YOK** — ayrı karar. `dotnet test` yeşil.
>
> **SentencePhase ↔ state_machine.player_states fark raporu (bağlama yapılmadı):**
>
> | SentencePhase (cümle) | player_states (dövüş) | Not |
> |---|---|---|
> | `Idle` | `idle` | İsim örtüşür; ikisi de "boşta". |
> | `Building` | `drawing` | **Uyuşmazlık:** JSON `can_draw="partial"`, `can_move=false`; `Building`'de çizim tam açık, hareket SentencePhase ile kilitlenmez. |
> | `Recovering` | `recovering` | İsim örtüşür; JSON `can_draw=true`, `can_move="limited"`, `can_dodge=true`. `SentencePhase.Recovering` girdi kilidi (`IsRecovering` / RemainingRecoveryMs) — dodge/düz vuruş/yeni fiille kesilir; capability metinleri SentenceEngine'de yok. |
> | `Resolved` / `Aborted` | — | Cümle kaydı fazları; dövüş state_machine'de karşılık yok. |
> | — | `casting` | SentencePhase'te yok. `can_move="based_on_cast_mobility"`. |
> | — | `dodging` | SentencePhase'te yok. `i_frames=true`, `can_move="dodge_direction"`; JSON'da `can_dodge` anahtarı yok → parse `"false"`. |
> | — | `stunned` / `rooted` / `channeling` / `dead` | SentencePhase'te yok (CC / kanal / ölüm). |
>
> Örnek görev notu doğrulandı: `drawing` → `can_draw="partial"`; `Building` tam açık.

> **16 Eylül — Görev 7 (ZoneDirector).** `Core/Layers/IZoneDirector.cs` (`ZoneInstance` +
> movement sabitleri) + `Core/Combat/ZoneDirector.cs` (saf C#: TrySpawn/Tick/Remove/MoveZone/
> SetFollowTarget). Soft-cap = `max_active_zones` (JSON 5), dolunca en eski düşer
> (StateBridgeBoard deseni). `RemainingSec` Tick ile azalır, ≤0 silinir. Movement ayrımı:
> `player_directed`→MoveZone, `follow_target`→SetFollowTarget, `static`/boş→ikisi de no-op.
> Görsel spawn yok. **RadiusM JSON'da yok** — spawn parametresi (çağıran verir). 9 yeni test;
> ManifestationDirector'a bağlanmadı.

> **16 Eylül (Görev 2) — ResourceTracker + CooldownTracker (Core only).**
> `Core/Combat/ResourceTracker.cs`: JSON `global_rules.resource_system` (max_mana=100,
> regen_per_sec=8, regen_delay_after_cast_sec=1.5) — `CanAfford`/`Spend`/`Tick`.
> `Core/Combat/CooldownTracker.cs`: JSON `cooldown_rules` (global_cooldown_sec=0.3,
> max_concurrent_casts=1) — verb id → soğuma bitiş (worldMs), `TryStart`/`CompleteCast`.
> **Oyuna bağlanmadı** (PlayerVitals / ManifestationDirector kasıtlı dışı).
> Testler: `ResourceTrackerTests` + `CooldownTrackerTests`. `dotnet test` 177 yeşil (163+14).
>
> **PlayerVitals MaxHp vs JSON:** `PlayerVitals.MaxHp` `PrototypeTuning.PlayerMaxHp` (=**22**)
> üzerinden `Bind` edilir — `global_rules.player_stats.max_hp` (**100**) değil. Boss:
> `CombatTuning.Boss.MaxHp`=120 vs `boss_stats_default.max_hp`=22000 (prototip his sayıları).
>
> **StatusTuning ↔ `global_rules.status_durations`** (StatusTuning.cs **değiştirilmedi** —
> sahibi otorite seçer):
>
> | status (JSON) | JSON duration | StatusTuning | eşleşme |
> |---|---|---|---|
> | stun | 2s → 2000ms | StunMs=800 | **fark** |
> | root | 3s → 3000ms | RootMs=1200 | **fark** |
> | silence | 3s → 3000ms | SilenceMs=1000 | **fark** |
> | slow | 4s → 4000ms | SlowMs=1500 | **fark** |
> | blind | 3s → 3000ms | BlindMs=1400 | **fark** |
> | fear | 2s → 2000ms | FearMs=900 | **fark** |
> | taunt | 3s → 3000ms | TauntMs=1200 | **fark** |
> | burn | 4s → 4000ms | BurnMs=2400 | **fark** |
> | armor_break | 5s → 5000ms | ArmorBreakMs=3000 | **fark** |
> | weaken | 4s → 4000ms | WeakenMs=2500 | **fark** |
> | grievous_wounds | 6s → 6000ms | GrievousMs=2500 | **fark** |
> | poison | 6s → 6000ms | PoisonMs=3000 | **fark** |
> | shield | 5s → 5000ms | ShieldMs=2500 | **fark** |
> | invulnerability | 0.5s → 500ms | StasisMs=700 (isim varsayımı) | **fark** |
> | confuse | 2s → 2000ms | StatusTuning'de yok | **yalnız JSON** |
>
> Magnitude:
>
> | alan | JSON | StatusTuning | not |
> |---|---|---|---|
> | grievous heal | heal_reduction=0.5 | GrievousHealMult=0.5 | süre hariç **aynı** |
> | slow speed | reduction=0.4 → 0.6 | SlowSpeedMult=0.55 | **fark** |
> | weaken | damage_reduction=0.25 → 0.75 | WeakenOutgoingMult=0.85 | **fark** |
> | armor_break | armor_reduction=0.3 | ArmorBreakDamageTakenMult=1.2 | semantik farklı |
> | burn/poison tick | mult 0.08 / 0.05 | DamagePerSec 6 / 4 | birim farklı |
> | shield absorb | 50 | ShieldAbsorb=25 | **fark** |
> | blind accuracy | 0.5 | yok | yalnız JSON |
>
> StatusTuning'de olup JSON'da olmayan: DisarmMs, Haste*, DamageReduction*, Regen*,
> Knockback*, BurnPoisonComboBonusPerSec, ShieldBurnDrainRatio, StunKnockbackDurationAddMs.

> **16 Eylül (6. tur) — DamageCalculator (formulas.damage + crit_system), paralel sınıf.**
> `Core/Combat/DamageCalculator.cs`: `base_damage_value × adj.damage_mult × length.damage_mult ×
> (1-resistance) × weakness_bonus`, sonra `crit_system` (base + `adjective_crit_bonus[id]`,
> `max_crit_chance` tavanı, `crit_multiplier`); ctor seed'li `Random` (deterministik test).
> `FromElementSystemJson` MiniJson ile `crit_system` okur. `SkillResolution` overload'u
> `CritEligible`/`AdjectiveId`/`BaseDamage`/`DamageMult` kullanır.
>
> **Çağrılmıyor:** ManifestationDirector hâlâ `ClosingDamageMath` kullanıyor. İki yol nasıl
> birleşir (ClosingDamageMath'i değiştir / DamageCalculator'a geç / hibrit) **sahibine sorulacak**
> — bu görevin kapsamı değil; ClosingDamageMath ve ManifestationDirector'a dokunulmadı.
> `dotnet test` 163 yeşil (156+7 `DamageCalculatorTests`).

> **16 Eylül (5. tur) — SkillMotor okuma katmanı (passives/chains/zones/status + verb alanları).**
> `docs/gorev-listesi` motor tam uyum backlog'unun parse adımı: `VerbNode`/`SkillResolution`'a
> `CritEligible`/`ElementOrigin`/`DamageType`; yeni `PassiveNode`/`ChainNode`/
> `StatusInteractionNode`/`ZoneNode` + `ParsePassives`/`ParseChains`/`ParseStatusInteractions`/
> `ParseZones` (MiniJson, ActiveMode deseni). `SkillMotor.Passives`/`Chains`/
> `StatusInteractions`/`Zones`/`MaxActiveZones`. **Uygulama yok** — yalnızca okuma.
> `dotnet test` 156 yeşil (155+1). JSON sayıları: passives 10, chains 6, status satırları 17,
> zones 11, max_active_zones 5.

> **16 Eylül (4. tur) — v5.3 JSON merge + ulti (active_modes) uçtan uca.** Sahibi kapsamlı
> v5.3 spec'i + ayrı bir "prezentasyon katmanı" JSON'u yapıştırdı, `element-sistemi.json`'ın
> `verbs`/`adjectives`/`elements`/`status_interaction_table`/`atoms_catalog`/
> `atom_kombinasyonlari`'ını v5.3 ile DEĞİŞTİRDİ (additive merge — v5.3'ün redefine etmediği
> `scaling_economy`/`categories`/`verb_families`/`future_layers` gibi bölümler korundu; v5.3
> tek başına eksikti, sıfırdan yazsaydık motor kırılırdı). **2-5 (Zehir/Pus) reverti bu kez
> KABUL edildi** — v5.3 otorite: `aktif_zehirlenme`→`tam_arinma` (İksir), `kisisel_isinlanma`
> →`hiz_gorunmezlik` geri döndü, ikisi de artık gerçek `base_resource_cost` alıyor (20/15).
> `element_families`'a eksik olan `ates_ailesi` eklendi (v5.3'ün `class`/`lore.arketipler`
> alanlarından). Adjective sayısı 41 (42 değil) — v5.3'ün veri gerçeği, uydurma değil.
> 8 test v5.3'e göre güncellendi (skill adı/skill_id değişiklikleri, `Special`/`ZoneEffect`
> artık verb'lerde yok → `IsNull` bekleniyor).
>
> **Ulti sistemi (`active_modes`) uçtan uca yazıldı** — sahibi "v5.2.1 hiç tam aktif olmadı,
> v5.3'e de olmasın" dedi. Aynı elementin dörtlüsü (X-X-X-X) artık gerçekten bir mod açıyor:
> `SkillMotor.ActiveModes` (yeni `ActiveModeNode` + `ParseActiveModes`, JSON'daki 6 modu okur,
> kombo tablosu YAZILMADI — hepsi JSON'dan) → `Core/Combat/ActiveModeDirector.cs` (saf C#,
> tetik/koşul/soğuma/süre durum makinesi) → `ManifestationDirector` dört-aynı-rün kapanışında
> tetikler, `ActorStatus.ModeDirector` üzerinden `damage_mult`/`damage_taken_mult`/
> `move_speed_mult`/`lifesteal`/hareket engeli/`hp_per_sec_percent` maliyeti dünyaya işliyor;
> tek seferlik efektler (`team_full_cleanse`/`team_invulnerability_sec`→Stasis/
> `enemy_blind_sec`→boss Blind/`team_regen_per_sec`→Regen) aktivasyon anında uygulanıyor.
> Yeni `ActiveModeHud.cs` — mod açıkken üst-orta banner isim+geri sayım gösteriyor (Kan
> Çılgınlığı gibi süresizlerde "AKTİF"), `ReactionReadout`'un aksine kendiliğinden SÖNMÜYOR —
> "gerçekten çalışıyor mu" sorusu bir daha sorulmasın diye kalıcı. 10 yeni test
> (`ActiveModeDirectorTests`) + Unity MCP'de derleme/Play mode hatasız doğrulandı.
> **Kapsam dışı bırakılanlar (bkz. Bilinen açıklar):** `cast_time_mult`/`attack_speed_mult`/
> `dash_cooldown_mult`/`afterimage_count` (Fırtına Akışı), `taunt_radius_m` (boss AI hedefleme
> değişimi), `resource_cost` düşümü (oyunda hiç mana/kaynak sistemi yok — önceden de öyleydi),
> `visual.aura`/`screen_edges` (banner var, ekran kenarı VFX yok). `team_has_debuffs`/
> `team_has_wounded` yalnızca oyuncu+ally can oranını okuyor — ally'nin `StatusBoard`'u yok.
> `dotnet test` 155 yeşil (145→155).

> **16 Eylül — bug turu + durum tablosu + boss 2. saldırı (2. oturum):** Sahibi Play mode'da
> beş bug rapor etti, hepsi teşhis edildi ve düzeltildi (Unity MCP ile Play mode'da
> doğrulandı, `dotnet test` 143 yeşil):
> - **Düz vuruş heal basıyordu** — sahnede `BasicStrikeDot` donmuş `5` (eski beşgen
>   SARSINTI) idi, altıgende 5=Aydınlık/`arindirma` (`cleanse` → `IsHealSkill` true
>   sanıyordu). `PrototypeTuning.EnsureRuntimeDefaults()`'a zorla `1` (Ateş) eklendi.
> - **Hasar sayısı görünmüyordu** — sahnede `ShowDamageNumbers` donmuş `0` idi (kod default'u
>   `true` ama sahne ezmişti); aynı yerde zorla `true` yapıldı.
> - **Sol joystick görünmüyordu** — `MoveInput` fonksiyonel olarak zaten çalışıyordu, hiç
>   görseli yoktu. Yeni `JoystickView.cs` (dinamik taban+kabarcık, `HexagonView`'daki
>   `CreateCircleSprite` deseniyle) eklendi.
> - **Kamera 360° dönemiyordu** — `FollowCamera` tamamen sabitti. `OrbitYawDeg` eklendi,
>   yeni `CameraOrbitInput.cs` üçüncü parmak (veya editörde sağ-tık sürükleme) ile yaw
>   döndürüyor; `MoveInput`/`HexagonInput`'un zaten claim ettiği parmaklara dokunmuyor.
> - **Duvarların içine giriliyordu** — Quaternius dungeon mesh'leri collider'sız geliyordu
>   (`ArenaWalkFit`'in "Fizik collider yok" notu); `KinematicMotor` sadece dış kare clamp
>   yapıyordu. Yeni `WallColliderFit.cs` isim eşleşmesiyle (`wall`/`column`/`arch`/...)
>   `BoxCollider` ekliyor (canlı sahnede 52 adet), `KinematicMotor.PushOutOfObstacles()`
>   küre-itme uyguluyor.
>
> **Durum etkileşim tablosu artık var** (`docs/element-sistemi.json` `status_interaction_table`,
> sahibinin 17 kuralı) — `Core/Status/StatusReactionTable.cs` (14 genellenebilir kural) +
> `StatusBoard.Apply/Tick` (reaksiyon uygulama, artık burn/poison/regen tick'i entry'nin
> kendi magnitude'unu okuyor — eskiden global sabitti) + `StatusApplicator` (3 özel durum:
> burn+poison ekstra tick, shield+burn kalkan aşınması, stun+knockback aynı-vuruş süre
> uzaması). `StatusKind.Poison` + `GrievousWounds`'un artık gerçekten kullanılan
> `HealEffectivenessMult`'ı eklendi. 8 yeni test.
>
> **Boss ikinci saldırı:** `docs/bosses/karadul.json`'da speclenmiş ama `implemented:false`
> olan `fire_cone` (Cehennem Nefesi) artık çalışıyor — `BossAttackKind` (Slam/FireCone),
> `BossAttackKindPicker` (aynı desen: üst üste tekrar sınırlı), boss can %50 altına düşünce
> (Faz 2/Öfke) açılıyor. Dar koni (40° yarım açı, `BossAttack.ArcHalfAngleDeg` + gerçek
> `Vector3.SignedAngle` hesap — Slam'in `angleFromForwardDeg` parametresi eskiden hep `0f`
> geçiliyordu, kullanılmıyordu). Hasar/windup spec'ten (18/800ms); radius/arc uydurma
> (docs/element-sistemi.md §10'da işaretli). His deneyi boss hasarını 0'a çekiyor
> (`combat.Boss.Damage=0`) — `FireConeDamage` de aynı satırda 0'landı, tutarlı kalsın.
>
> **HUD (best-effort):** `VitalsHud` bar'ları düz siyah dikdörtgenden yuvarlak köşeli +
> ince kenarlıklı hâle geldi (`RoundedRectSprite`, 9-slice). Diğer HUD elemanlarına
> dokunulmadı — kapsam "modern" tanımı öznel, geri bildirim istiyorum.
>
> **Yapılmadı (kasıtlı, sahibi "sonra yapalım" dedi):** Fab paketleri (Demon Watcher,
> JustCreate karakterler, Modular Dungeon Lava) henüz satın alınmadı — Mixamo/rig
> entegrasyonu ayrı bir tur.

> **16 Eylül (3. tur) — durum tablosu görünmezdi, artık görünüyor.** Sahibi "skilleri
> attığımda bir etkileşim göremiyorum" dedi — teşhis: mekanik (2. turda yazılan
> `StatusReactionTable`) doğru çalışıyordu, ama tetiklendiğinde ekranda **hiçbir sinyal**
> yoktu. `StatusBoard.ReactionTriggered` event'i eklendi; `StatusApplicator.Result` artık
> `TriggeredReactions` taşıyor; `ManifestationDirector.ApplyClosingStatuses` bunu mevcut
> tepki yazısı kanalına (`ReactionReadout.NoteSkill`, "ally +N" ile aynı yol) yazıyor —
> renk mevcut `AcidGreen` vurgusu. Unity MCP'de canlı doğrulandı: Ateş'in
> Kor'u (1-3, `zirh_eritme`, mechanics=[armor_break,burn]) TEK cast'te "Erimiş Zırh"ı
> tetikliyor ve şimdi ekrana yazıyor. 2 yeni test (145 yeşil).

**Önceki:** element-sistemi motor genişletmesi adım 2+ (bkz. `docs/gorev-listesi.md` "Backlog")

> **element-sistemi 4.2.2 + SkillMotor parse genişletmesi (16 Eylül):** Sahibi sohbette v5.2
> (5 ailenin verb/bileşik detayı + `three_runes_examples`) ve v5.2.1 (`atoms_catalog`) verisini
> yapıştırdı; `docs/durum.md`'deki eski açık ("Core/Unity henüz okumuyor") buradan kapandı.
> `docs/element-sistemi.json` (+ `Resources` kopyası) additive merge ile 4.2.2'ye çıktı: 33
> fiile `base_resource_cost`/`special`/`zone_effect`, 28 bileşiğe `identity`/`special_mechanics`,
> `element_families` (5 aile — Ateş kaynakta yoktu), `three_runes_examples`, `atoms_catalog` +
> `all_verbs_atoms` + `atom_kombinasyonlari` (motor OKUMAZ, VFX/animasyon referansı) eklendi.
> **2-5 (Zehir/`aktif_zehirlenme`) ve 3-2 (Pus/`kisisel_isinlanma`) bilerek DEĞİŞTİRİLMEDİ**:
> v5.2 bunları İksir/`tam_arinma` ve hız+görünmezliğe geri almak istiyordu ama bu dosyada zaten
> kasıtlı dönüşüm notu vardı (sahibi kararı: kilitli hâli koru).
> `Core/Grammar/MiniJson.cs` (yeni, bağımsız minimal JSON ağacı) + `SkillMotor.cs` artık
> `animation_type`/`target_mode`/`base_cooldown_sec`/`base_resource_cost`/`target_behaviors`/
> `special`/`zone_effect` (verb) ve `engine_modifiers`'ın TAMAMI (adjective, 20+ alan) okunuyor;
> `SkillResolution` bu alanları taşıyor. `dotnet test` 135 yeşil (127→135, 8 yeni test:
> `SkillMotorTests` + `MiniJsonTests`). Kalan adımlar (`ResourceTracker`/`DamageCalculator`/...)
> `docs/gorev-listesi.md` "Backlog" bölümünde — `formulas`/`global_rules` yok, o sayılar
> gelmeden yazılmayacak.

> **Telefon:** `dovus-prototip.apk` (71 MB, 15 Eyl 20:05) → `adb install -r` Success,
> paket `com.dovus.prototip` açıldı. İçerik: arena×3, boss hasar 0, ally+oyuncu %50,
> Su heal, Ally HUD barı.

> **Heal / HUD:** Sol üstte Ally barı + kafa üstü bar. Mend daha boş olana (eşitse sana).

> **Arena / VFX his:** `ArenaWalkFit` duvar/floor’dan yürüyüş yarım kenarı (inset);
> duvara gömülme soft clamp ile kesilir. Arena-wide kırmızı ember kalktı → `LavaDecor`
> (emissive havuz + ısı partikül + point light). Skill: daha kalın glow çizgi, yassı wisp
> (eski top-sürü değil), kapanış bang burst. Atmosphere sıcak lav tonu + bloom.

> **SkillMotion + StateBridge motoru:** Core `SkillMotionMotor` (Pus blink/Zenitsu,
> Hareket dash, sabitleme→işaret) + `StateBridgeBoard` (iki aynı işaret=portal).
> Game: `SkillMotionDriver`, `StateBridgeView`. Hava+Su yakın boss → Zenitsu kesisi;
> Hava+Su+Toprak → işaret; iki işaret arası caminin içine gir → diğer uç.
> Tuning: `CombatTuning.SkillMotion` (sayılar durum.md sapma — spec yoktu).

> **Karadul his turu:** `docs/bosses/karadul.json` (+ Resources). Raid stub’ları future.
> `ClosingDamageMath`: commit × fiil `base_damage` / 40 × sıfat tax; heal/dash = 0 can.
> `ClosingDamagePerEffect` 1→**3.5** (~5×4-rün jab düşürür). Hasar sayısı **açık**, bar
> turuncu + HP metni. Slam hasarı `ActorStatus` (kalkan/stasis i-frame). Dodge i-frame aynı.

> **element-sistemi 4.2.1 merge:** `docs/` + `Resources` senkron. Fold korundu
> (`secondary_effects.mechanical=true`). Fiil rename (Pus/Zehir/Mühür/Kül/Kum/Obsidiyen),
> her fiile `animation_type` / `target_mode` / `base_cooldown_sec`, `poison`, silüet eksenleri.
> `future_layers.state_bridge` binding:false (motor yok). Core henüz yeni alanları okumuyor.

> **Jab polish:** düz vuruş ayrı `BasicStrike` → `Sword_AttackFast` @1.55 + boss’a bak.
> CastPierce de hızlı kılıç; CastSlam Punch. Skill bang yok (önceki gibi).

> **Anim (Quaternius):** `Player_Quaternius` / `Boss_Quaternius` controller —
> `Assets/Art/Quaternius/Animators/`. Map: Roll→Dodge, Punch/Sword→Cast*,
> Jump→Windup, Bite→Slam, HitRecieve→Stagger. `BossVisual` + motor Speed/Dodge/Hit kancaları.
> Prefab’lara controller atandı. Idle loop için clip kopyaları `Animators/Clips/`.

> **Quaternius bağlandı:** Prefab’lar `Assets/Art/Quaternius/Prefabs/` —
> Player/Boss/Arena → Bootstrap. Warrior ~1.9m, Demon ~2.8m.

> **Sanat deneme:** Fab 24s kilidi → Quaternius CC0 indirildi:
> `unity/Assets/Art/Quaternius/` (Characters + Dungeon + BossCandidates).
> Blend/OBJ/zip → `tools/vendor/Quaternius/`. Öneri: `Warrior.fbx`, `Demon.fbx`.
> Fab sepeti (JustCreate + Lava + Demon Watcher) `docs/alis-sepeti.md`’de bekliyor.

> **SkillMotor katlama:** 1=kök skill; 2=`skill_name` kartı (36); 3=(bileşik)+kök sıfatı
> (`Alev · Yoğunlaştırma`); 4=bileşik+bileşik (`Alev+Alev`). 2'li kart 3/4'e taşınmaz.
> 3/4 hazır `skill_name` yok — formül isim. `secondary_effects` notu güncellendi.

**Önceki omurga:** tek Humanoid + kıyafet; sınıf yok. `ActorVisual` / `CastBodyMapper` hazır.

**Önceki:** 23 Ağustos 2026 · Faz 3.5 bitti — telefonda his turu (soru 3–5) / Faz 4 kapısı

> **T14 kapandı.** Üç fiilin hareket karakteri ayrıldı (İĞNE Zenitsu fırlatış, SÜRÜ
> kademeli bulut, SARSINTI yerden yükselen halka) + düz vuruşun kısa jab silüeti.
> `dotnet test` 92 yeşil. Sırada telefonda §13 soru 3–5 yeniden.
>
> **T13 kapandı.** YERE ÇAKMA üç ritmi: YAKIN / GEÇ / GENİŞ. Tell'ler windup'ta okunur
> (GEÇ ton+poz, GENİŞ disk). `MaxSameVariantStreak=2`.
>
> **T12 kapandı.** Boss canı 120, kapanış ödülü × `ClosingDamagePerEffect` hasar, tür son
> rüne bağlı tepki, ölümde çökme + tam can revive.
>
> **T11.1 kapandı.** Mürekkep cümle sınırında kopuyor, toparlanma kilidi kalıcı HUD'da,
> `BossDirector.TickWindup` null-safe.
>
> **T11 kapandı, his turu kapandı, Faz 3.5 planlandı.** §13'ün 1. ve 2. sorusu telefonda
> **evet** — yani Faz 4'ün eski yasağı kalktı. 3–5. sorular açık ve üçünün tıkandığı yer aynı:
> mekanik **okunuyor** ama temsil soyut, boss tek saldırıyla tekdüze, harcamanın gittiği yer yok.
>
> 5. oturumda (23 Ağustos, masa başı) 4. oturumun "cümle sonucu okunmuyor" teşhisi **düzeltildi**:
> sahibi nokta sayısını ve dwell'i görüyor. Yedi karar alındı, spec güncellendi, dört görev
> yazıldı: **T11.1 → T12 → T13 → T14**, sıra bağlayıcı. Kararların tablosu
> `docs/his-kontrol-listesi.md` "Turda alınan kararlar"da; gerekçeler aşağıda.
>
> **Sanat (Faz 4) hâlâ sırada değil** — ama sebebi artık "§13 cevaplanmadı" değil: T12 ve T13
> bossun kaç saldırısı olduğunu ve nasıl öldüğünü donduruyor, rig'li model/animasyon seti o
> kararlara bağımlı. Faz 3.5'in tamamı primitive.

## Tarihçe (özet)

Beşgen prototip dönemi (T0–T14, 22–23 Ağustos): Core/Unity iskeleti, cümle gramer motoru,
dodge/exchange derecelendirmesi, tezahür (yaşayan etki) katmanı, boss telegrafı + hitstop
+ kamera, HUD, oyun içi ayar paneli, Android build + kare süresi göstergesi, his turu (§13
soru 1–2 evet), silüet keskinleştirme — hepsi bitti. 16 Eylül'de altıgen/element-sistemi
dönemine geçildi (bkz. yukarısı). Görev görev "üretilen API / doğrulama / sapma" detayları
**buradan bilerek silindi** — İĞNE/SÜRÜ/SARSINTI/"beşgen" gibi beşgen terminolojisi güncel
altıgen sistemiyle karışıp kafa karıştırıyordu ve artık işimiz yok. Detay istenirse
`git log --oneline` (T0…T14 commit'leri) veya eski commit'lerdeki bu dosyanın hâli yeterli.

Güncel API yüzeyi için kaynak koddur: `Dovus.Core.*` (saf C#, AGENTS kural 1) +
`Dovus.Game.*` (Unity kabuğu). Element/skill verisi için `docs/element-sistemi.md` +
`docs/element-sistemi.json`. Boss verisi `docs/bosses/*.json`.

## Bilinen açıklar

- **Tarama artıkları (30 Eylül, çözüldü, PR #33 merge).** Top 1-2, Yumruk/Kalkan 1-4, Yumruk 2-2
  ve Top geri tepmesinde ayak havada: 4× `pr33-4x-r2` hepsi geçti. Aşağıdaki "Top geri tepmesi
  havada", "PR #28 Top 1-2" ve "`tools/AtomSim` derlenmiyor" maddeleri artık tarihsel.
- **Kalkan 3-6 süre kararsız (30 Eylül, Play).** Tam taramada bir kez kalıp bang'den 0,16 sn sonra
  (pay 0,15), MCP sorgusunun editör takılmasıyla çakıştı; tek başına 3/3 geçti. Tekrarlarsa bak.
- **PR #29 portal/sınır/takım (30 Eylül, merge yok).** Kılıç 141/144: 9-10 ışını tek karede
  8,34 m (tarama `konum`), 2-6 kalıbı AllyDummy'yi hedefliyor (`tek_sistem`), 11-8 Kılıç başka
  sistem 0,18 m. Sınır 1-2 ile can %15'te açılıp aynı skill'in can çalmasıyla bir karede
  kapanıyor (%31) — tasarım kararı gerekiyor. 9-10 yer değişiminde dost Y=0'a iniyor
  (`Placement` Y 0f).
- **Top geri tepmesi havada (30 Eylül, kod).** 4× Top 1-3, 6-3, 5-8, 6-8, 7-8: `geri_tepme`
  önceki sıçramanın inişini taşıyordu. Yatay faz o karede zemine yapışır; hop/leap/slam/hover
  havada kalır. Play tekrar yok.
- **Kılıç 10-2 süre (30 Eylül, bakıldı, kod yok).** `yutan_girdap` 0,18+3,00 = 3,18 sn. 4×
  Kılıç 3,39 (pay 3,38); Yumruk 3,28, Kalkan/Top 3,20. Silaha özel süre yok. `pr33-4x-r2`'de
  Kılıç 3,22 sn, geçti (sahip kararı: 0,01–0,2 sn uzama kabul).
- **Portal tarama (30 Eylül).** 9-10 ışını işaretli kare. Vakalar arası kapı ve dost yeri sıfırlanır.
  Kalıp bitişindeki genel itme kalktı (1-3, 2-6, 8-12). Unity Play bu turda yok.
- **Portal / Sınır / Takım (29 Eylül, `feat/portal-border-team`).** Üç etiket artık işler.
  Silah kesme hâlâ yalnız etikettir (silahlar PR'ında). 8-1 boss vuruşu 2 sn ×0,7. Taret 1 sn,
  12-6 mesafe kopması ve +%50 hız yedeği, 7-6 zırh süresi `ArmorBreakMs`. Unity Play bu turda yok.
- **3-10 dash bitince oyuncu kapıdan geri döner (30 Eylül, Play, #29 ile master'da).** Kalıp
  bitince varış kapısı oyuncunun ayağına kurulur ama oyuncu o kapının "içinde" sayılmaz; sonraki
  karede `Sense` onu çıkış kapısına geçirir. 8 m'den cast: dash 6,5 m, oyuncu başlangıç kapısının
  1,55 m önüne döner (4,95 m geri). Tarama 3 m'den başladığı için geri dönüş 0,05 m, görünmez.
  Kapıdan geçiş iki yönde çalışıyor (5,75 m). `NotifyTemplateEnded` varış kapısına sahibini
  `_inside`'a eklemeli mi, karar/kod gerekli. Birim testi sahibin varış kapısında durmasını denemiyor.
- **11-10 dostları Y=0'a koyar (30 Eylül, kod okuması).** `GatherTeam` `Placement` Y'si `0f`
  (9-10'un eski hatası gibi). Play'de denenmedi.
- **PR #28 Top 1-2 (30 Eylül).** Emici çekmesi + Top geri tepmesi: oyuncu simülasyondan ~1,6 m
  farklı biter, tam taramada bir kez boss gövdesine 1,03 m girdi (tekrarda 0/3, kararsız).
  Yumruk/Kalkan 1-4 hasarsız, Yumruk 2-2 kalıbı 10,9 sn. PR #33 dört vakayı Play'de düzeltiyor
  (4× hepsi geçti) ama merge edilmedi; bkz. aşağıdaki Top `yerde` maddesi.
- **Top geri tepmesi havada (30 Eylül, Play, master'da).** 4× Top 1-3, 6-3, 5-8, 6-8, 7-8:
  sıçrama/asma kalıbına eklenen `geri_tepme` fazında ayak 0,26–0,83 m yerden (sınır 0,20).
  Master ve PR #33'te aynı; Top ≥143 kuralını bu 5 vaka engelliyor.
- **`tools/AtomSim` derlenmiyor (30 Eylül).** `MechanicVisual.cs` `Dovus.Core.Tuning` /
  `SkillVisualTuning` buluyor ama AtomSim projesi Tuning'i link'lemiyor (5 hata). CoreTests ve
  GameCompile temiz.
- **10 silah taraması (30 Eylül, kod; Play tekrar yok).** `hepsi.csv` 1395/1440 kalanları kodda
  ele alındı: Top geri tepmesi kalıp fazı, düz atış ilk gövdede patlar, Yumruk/Kalkan menzil
  dışı `kapan` ile kapanır, 6-2 cast başı menzili korunur, 2-9 koşucu saate yetişir, Küre 8-11
  bitişi bir sonraki kareye kalır. Küre artık basılı tutma / çift dokunuş dinlemez; HUD silah
  düğmesi (editörde R) yollar veya çağırır. Yumruk 2-6 kalıbı 0,52 sn, vaka saati 9,6 sn'de
  kesildi; kancayı kısaltmak 6-1 varışını da keser. Kalkan kısa kalkanı hâlâ değiştirmeden
  sonraki ilk vuruşta kurulur (JSON). 1440 tarama bu düzeltmelerden sonra Unity Play'de koşulmadı.
  Gerçek dokunma/fare ve F2 tuşu editörde enjekte edilemedi.
- **Yoğunlaştırma pasifi menzili daraltır (30 Eylül, Play).** `hitbox_scale_mult` 0,55 hedef/menzil
  kapısına da biniyor: pasif aktifken 3 m'den atılan 1-4 reddedilir, 1,6 m'den geçer. Tasarım mı
  hata mı karar sahibinde.
- **PR #31 bağlayıcı JSON'u değiştirdi.** `passive_duration_default` rün 9 ve 11 için 0 → 5
  (`docs/element-sistemi.json` ve Resources kopyası). Merge edildi; sayının kaynağı PR'da.

- **Pasif sekme (30 Eylül).** Başka düşman 6 m içindeyse plan ona gider; can yazması yalnız
  boss kimliğine bağlı. Prototipte ikinci düşman canı yok, yalnız boss varken ek vuruş boss'a iner.
- **Odaklama delmesi (30 Eylül).** `ignore_armor` artık %50 zırh delme. Eski Play notundaki
  1-9/5-9 tam zırh yok sayma (360K/260K) bu kuraldan sonra geçersiz; yeni Play yok.

- **Hasar borusu (30 Eylül, kod).** Kritik JSON'da %5 / ×2 durur; canlı vuruş %10 / ×1,5.
  Boss zırhı normal 100, sert 150 (JSON). Canlı dövüş normali kullanır; sert mod anahtarı yok.
  Boss canı 88 milyon; 150–250 milyon için `max_hp` sonra ayarlanır. Ayar paneli sıfırlayınca
  can 22'ye dönebilir (ölçek bağlama yalnız açılışta). Geciken patlama kalkanı vuruş
  hesaplanırken yer. Zafiyet artık yalnız zırh puanını düşürür (`armor_break` ×1,2 binmez).
  Play'deki tek kare sıçrama, tarama beklentisi, düz vuruş ve zırh 30 Eylül ikinci testte
  doğrulandı. Kalan: 3-6 `as` yayı 19,2 m/s — takılan karede (≥31 ms) 0,6 m eşiğini aşıyor
  (iki silahta 0,73 m). 1-9/5-9 delmesi kodda %50'ye indirildi; Zafiyet sonrası ilk düz
  vuruş, etkisi söndüyse de vadesinde menzildeki boss'a iner. İkisi de bu ortamda Play'de doğrulanmadı.
- **Görsel (29 Eylül, duruyor).** Hitbox VFX yer tutucu küre 1-5 dönüşünü kapatır; 12-7'de kum saati yok.
  2-2 akış çizgisi hâlâ oyuncu→dost, kırmızı değil (hasar yolu ayrı bağlandı).
  Bağ/hacim (`_mechanicLinks`) status temizliğinden sonra da Root/Slow uygulayabiliyor.
- **3-6 ayak kayması (29 Eylül, ikinci deneme).** Oynatma tavanı 12× (2,2 m/s klip, 19 m/s yay).
  Play'de oran 1,00 (Kılıç + Asa). 30 Eylül: koşu en çok 2×, üstünde dash pozu tutuluyor (doğrulandı).
- **Emici çekme (29 Eylül).** Varış oyuncunun önü, temas mesafesi. Play'de 5-2/12-2/2-2/6-2 boss
  0,32–0,67 sn'de, kare başı ≤0,13 m ile oyuncuya geliyor; 3-2 sıçramıyor.
- **Vuruş göğüs yüksekliği uydurma (29 Eylül).** `ManifestationTuning.StrikeChestOffsetM`
  = 0.35 m (gövde merkezinin üstü, 2 m gövdede ~1.35 m). Spec'te yok, önerilen.
- **Animasyon kancası (29 Eylül).** Faz anahtarı + `anim_bridge`. Bacaklar kalıp hızından
  yürür; dönüş klip + gövde yaw. Yan kayma klibi yok (Strafe parametresi duruyor). Unity Play yok.
- **Hareket kalıbı bölüm 2–3 (29 Eylül).** Aile 1–42 oynanır (101 kalıp, 144 skill).
  Portal, Sınır modu, Takım kombosu, Silah kesme etiket olarak durur, işlemez.
  Yürüyen balon 4 sn; 10-5 metnindeki 2 sn ayrı gövde değil. Unity Play yok.
- **Hareket kalıbı bölüm 1 (29 Eylül).** Aile 1–14 o turda açıldı. Aile 15–42 sonraki turda
  bağlandı (yukarı). Portal, Sınır modu, Takım kombosu, Silah kesme etiket olarak durur, işlemez.
  Kalıp, bang anında başlar (~0,26 sn toparlanmadan sonra). Kalıp oyuncuyu oynatıyorsa
  gramerin konum ışınlanması atlanır (yukarı). Boss collider yoksa vuruş payı 0,6 m. Ayna klonların
  5 sn'lik tekrarı ve dostu gerçekten çekmek bu bölümde yok. Unity Play yok.
- **Fiil 3/7 genişliği (29 Eylül, sahip onayladı).** `cross_section: "width"` fiil 3 (line)
  ve fiil 7'de durur: ikinci sayı tam genişlik, oyun yarıçapı bunun yarısı. İşaretsiz
  capsule/sphere (fiil 1 kapsülü 0,5 m) yarıçap kalır. Line/box şekilleri de genişlik sayılır.
- **Hareket kalıbı Play bulguları (29 Eylül, düzeltildi, yeniden Play yok).** Kanca düşmana
  iner, sekme vuruşu boss'a değer, saplama kenarda durur, fitil yere çakılır, menzil kapısı
  lunge'u sayar, ilk merkez vuruşu build sonrası yutulmaz. Unity Play bu turda yok.
- **Kalıp konumu (29 Eylül).** Sahip kararı kodda: kalıp oyuncuyu oynatıyorsa
  `yer_degistir` / `hedefin_arkasina` ışınlanmaz; `isaret_geri_don` işareti konur, dönüş
  kalıbın içinde kısa atılmadır. 3-6 Play'de bir kez arkaya inip kalıyor. 3-4 ve 3-9 bu dalda
  kalıpla oynar; Play henüz yok.
- **Derleme testi (29 Eylül).** `python3`, sonra `python`, sonra `py -3`. Hiçbiri yoksa
  test atlanır, düşmez. Windows'ta geçiyor (376/376).
- **Hareket kalıbı animasyonsuz (29 Eylül, Play + kod).** `MotionTemplateBody` ve
  `ManifestationDirector.MotionTemplate.cs` animatöre hiç dokunmuyor; kalıp oynarken
  `KinematicMotor` yürüme hızını 0'a çekiyor. Kanca çekişi (6 m), saplama, sekme, dönüş
  gövdeyi taşır ama bacaklar yürümez: görsel ayak kayması kalıp tasarımından geliyor.
  Dönüş gövdeyi transform ile çevirir. Kalan 28 aile için de geçerli.
- **Eski Adım ışınlanmaları (29 Eylül, Play, Kılıç).** 3-4 Sabit Adım: dash + 2,2 sn sonra
  işarete ışınlanma çalışıyor; yakından (2,5 m) dash boss gövdesine giriyor (merkeze 1,02 m,
  temas 1,35). 3-9 Odaklı Adım: "arkaya ışınlanma" logu düşüyor ama 4,5 m'den oyuncu boss'un
  önünde (1,50 m) kalıyor; 2,5 m'den dash boss'un içinden geçip arkaya çıkıyor. Kalıpları
  gelince yeniden bakılacak.
- **Play taraması (29 Eylül, master).** 3-6 on silahın hepsinde (Balta, Çekiç, Top, Asa,
  Tılsım, Kalkan, Yumruk, Hançer, Mızrak, Kılıç) arkaya iniyor, 3 sn kalıyor, Root 1,5 sn.
  1-1 parmak basılı: bırakmadan duruş 0,35 sn; 1,6 sn basılıyken 1,25 sn (0,34 + 0,9 üst sınır).

- **Motor denetimi — adım 3 (29 Eylül).** Düz vuruş menzili fiil 1 kapsülü (1.5 m);
  saldırı bakışı hedefe kilitlenir, çubuk vuruşun ortasında gövdeyi çevirmez. İkisi de
  `fix/engine-step3`'te, Unity Play henüz yok. Adım 2 (PR #22) master'da: boss CC, dost
  hedefi, karttaki etki, tempo süresi; yavaş/hız süresi eklenmez. Kart süresi hâlâ kısa olanlar:
  Sabit Bağ "3 sn" iken kök süresi 1.5 sn; Sabit/Odaklı/Akan Zaman kartı 5/3/4 sn der,
  tempo alanı 1 sn. Boss'un bugünkü saldırıları (çakma, nefes) yerinde; hücum/sıçrama/atış
  yok, kök onları ancak eklenince keser. Unity Play dumanı yapıldı (yukarıdaki 29 Eylül Play
  kaydı); kök ile yavaşın hazırlığı uzatması Play'de ayrıca ölçülmedi.
- **Yoğun Zaman gücü kartla çelişebilir (29 Eylül, Play).** Kart "hızı %70 düşer" diyor; motor
  `enemy_slow: 0.7`'yi hız çarpanı olarak okuyup boss'u ×0.70'e (yalnız %30 yavaş) indiriyor.
  Mekanik gramer logu aynı skill için `tempo→düşman 0.3` yazıyor. Hangisi doğru, karar gerekli.
- **Yoğun Şifa menzili (29 Eylül).** Play'de Kılıç 0.8 m yüzünden 3.2 m'deki dosta gitmedi.
  Adım 3 bunu 6 m yaptı (`ally_skill_range_m`); Play'de ~5 m henüz bakılmadı.
- **Yoğun Zaman kartı (29 Eylül).** Etki boss'u normal hızın %70'ine indiriyor (%30 yavaş).
  Kart "%70 düşer" diyordu; metin "%30 düşer" olacak şekilde düzeltildi. Güç sayısı değişmedi.
- **Yükselen Zaman süresi (29 Eylül).** Hız artık 3 sn (`tempo_duration_sec` = hasar buff'ı) ve
  kartta "3 sn" yazıyor. Pasif satırı hâlâ "4 sn" — o pasif yuvanın süresi, bu turda değiştirilmedi.
- **Hedefleme Unity Play'de doğrulanmadı (29 Eylül, PR #20).** Core testleri geçti; editörde
  tık, menzil reddi, düz vuruş ve geri yürüme elle bakılacak. Düz vuruş hasarı hâlâ yalnız
  boss'a gider (ikinci düşman yok). Seçili hedef varken saldırı dışında da gövde hedefe
  kilitlenir. Yürüme klibi ileri kliptir; gövde dönmese de ayaklar geri adım animasyonu
  oynamaz.
- **Premium HUD telefonda çakışıyor — editörde telefon DPI taklidiyle giderildi (29 Eylül).**
  Gerçek cihazda dokunma boyutu (rün diski ~51 dp) ve rozet okunurluğu henüz hissedilmedi.
- **Ally HP billboard'u (`AllyDummy.EnsureBillboard`) çok büyük ve kafanın epey üstünde**
  (2.64×0.67 m dünya kutusu, metin görünmüyor); sol üstte vitals'ın altında siyah kutu gibi
  duruyor. Bu turda dokunulmadı.
- **Görsel boy hedefi dolgulu bounds'tan ölçülüyor:** `PlayerVisualHeightM 1.78` iken şövalyenin
  gerçek boyu 1.60 m, boss 5.0 hedefte 4.22 m. Düzeltmek kadrajı/hitbox hissini değiştirir;
  karar bekliyor (`AttachVisual` içinde `posed: true` ile ölçmek yeter).
- **Walk/Run `rotationOffset` (19°/52°) gövdeyi hafif yan döndürmüş gösterebilir** — telefonda
  bakılmadı; kötüyse ayak yönü yerine klip değiştirilmeli.

- **mechanic_grammar dünya atomları — kalanlar (28 Eylül).** Bu turun tam label audit'i ve
  ertelenme sebepleri dosyanın en üstündeki oturum notunda. Özet: düşman projectile sistemi,
  ikinci düşman, gerçek co-op aktörü ve boss hedefleme/kontrol AI'sı olmayan davranışlar
  doğrulanamaz; taşma/savuşturma/ayna ikinci gövde/rampa/glide executor sahipliği ayrı iş.
  Top=Tılsım 9, Çekiç=Tılsım 6 skill'de aynı davranış (Sabit/Yükselen silah yolunu eziyor) —
  bu gramer kararı değişmedi. **Süre tutarsızlığı:** gramer hacimleri `Body.LifeSec` ile
  yaşar; süresiz fiillerde bu 0.2 sn (ör. 2-2 Girdap boss'u yalnız ~0.2 m çeker) ama aynı
  skill'in FieldAura executor'ı 5 sn sürer — hangisinin bağlayıcı olduğu karar bekliyor.
- **Fiil executor'ları (28 Eylül) — kalanlar.** 11-4 taret ve 11-5 halka artık gramer
  aktör profiliyle dünyada; Akan aktörler `channel_sec` boyunca saniyelik doğar ve Akan
  alan `tick_rate_mult` + `flow_tick_fraction` kullanır. `max_targets`, `ignore_armor`,
  `cleanse_count` genel executor'a bağlı değil; 3-7
  görünmez geçiş uzak Sis hacmiyle Stealth/Blind verir; ally buff/yansıma dummy üstünde
  StatusBoard/alan olarak var ama gerçek co-op hareket/hasar hedeflemesi yok; bazı sıfatla
  eklenen CC süreleri hâlâ `StatusTuning`'den; minion/decoy boss aggro'sunu çekmiyor
  (boss AI kapsam dışı). `mobility_cc` temel CC süre/öncelik ve fiil 1/5
  JSON hitbox geçişi üstteki runtime boşlukları turunda bağlandı.
- **`LivingEffectView.EnsureBangBurst` (satır ~162) konsol spam'i:** "Setting the duration
  while system is still playing" — mevcut hata, bu görevde dokunulmadı.

- **Görev 17 (element renk karşılaştırması) — sahibi için tamamlandı, RENK DEĞİŞTİRİLMEDİ.**
  `docs/prezentasyon-katmani.json` `vfx_binding.element_colors` ile `PrototypeTuning`'in
  mevcut `ElementFire/Water/Air/Earth/Light/Dark` alanları **hiç eşleşmiyor** — sadece
  "biraz farklı" değil, **muhtemelen karışmış**: mevcut `ElementEarth` (#9EC76B, yeşilimsi)
  yeni JSON'un `Hava`sına (#87a96b) neredeyse birebir yakın; mevcut `ElementDark` (#7A47C7,
  mor) yeni JSON'un `Toprak`ına (#877dd9) neredeyse birebir yakın. Bu, Rune enum'unun 16
  Eylül 4. turda yeniden adlandırılmasından (Igne/Suru/Kabuk/Zehir/Sarsinti/Toprak →
  Ateş/Su/Hava/Toprak/Aydınlık/Karanlık) ÖNCE atanmış renklerin, rename SIRASINDA doğru
  elemente taşınmamış olabileceğini düşündürüyor — ama bu bir varsayım, kanıtlanmadı.
  **Tam karşılaştırma tablosu:**

  | Element | Mevcut kod (`PrototypeTuning`) | Yeni JSON (`prezentasyon-katmani.json`) |
  |---|---|---|
  | Ateş | `ElementFire` #FF6B9E (sıcak magenta) | `primary` #C45C26 / `light` #FF9A3C (turuncu) |
  | Su | `ElementWater` #47B8FF (parlak mavi) | `primary` #39646A / `light` #5FB5D0 (koyu petrol) |
  | Hava | `ElementAir` muted teal (17 Eyl — prezentasyon bandı) | `primary` #87A96B / `light` #C5E0A8 (yeşilimsi) |
  | Toprak | `ElementEarth` #9EC76B (yeşil) | `primary` #877DD9 / `light` #B8AFEF (mor) |
  | Aydınlık | `ElementLight` #FFF5D1 (krem) | `primary` #C9A227 / `light` #FFE082 (altın sarısı) |
  | Karanlık | `ElementDark` #7A47C7 (mor) | `primary` #5C8A7D / `light` #7FB3A0 (yeşilimsi-gri) |

  **Hava:** 17 Eylül sprintinde kod `ElementAir` prezentasyon teal bandına çekildi
  (yıldırım-mavi değil). Diğer elementler için renk otoritesi hâlâ açık (kod vs JSON).

- **Ulti (`active_modes`) kalan boşluklar** (17 Eyl kararsız paket): `cast_time_mult` /
  `attack_speed_mult` / `dash_cooldown_mult` bang gecikmesi + dodge CD'ye bağlandı.
  `afterimage_count` (Fırtına Akışı) ve `taunt_radius_m` (Aşılmaz Duvar) hâlâ yok.
  Ulti `resource_cost` hâlâ düşülmüyor (verb `base_resource_cost` Bağlama 2'de düşüyor; mod
  maliyeti ayrı). Cast engeli `CombatTuning.EnforceResourceCost` (Bağlama 3, varsayılan false).
  `length.resource_cost_mult` **bağlandı** (17 Eyl LivingEffect dalı: `SkillMobility.ResourceCost`);
  Enforce hâlâ false. `visual.aura`/`screen_edges` okunmuyor — `ActiveModeHud` banner'ı var,
  ekran kenarı VFX yok.
- **`team_full_cleanse` hâlâ yalnızca oyuncu** (17 Eyl): Ally `StatusBoard` var ve
  `team_has_debuffs` sayıyor; cleanse ally board'a uygulanmıyor.
- **`docs/element-sistemi.json`'ın büyük kısmı motor tarafından hâlâ uygulanmıyor** —
  `verbs`/`adjectives`/`elements`/`scaling_economy.lengths`/`active_modes` dünyaya işliyor.
  **Okuma katmanı (5. tur) eklendi:** `passives`/`chain_mechanics`/
  `manipulation_layers.zone_layer` + `status_interaction_table` `SkillMotor`'da listeleniyor;
  verb `crit_eligible`/`element_origin`/`damage_type` parse+Resolve'da.
  **Görev 8:** `manipulation_layers.space_layer` de okunuyor (`SpaceEffects`/`MaxActiveLinks`);
  SkillMotionMotor hâlâ bağlanmadı (sayı sapmaları yukarıda — otorite sahibi).
  **`formulas`/`crit_system` kodu var ama bağlı değil (6. tur):** `DamageCalculator` yazıldı;
  canlı hasar hâlâ `ClosingDamageMath`. **ChainDirector (Görev 5 + Bağlama 6)** —
  Game'e bağlı: `Queue` + Finisher `ReactionReadout` + Links/finisher_mult → sonraki
  kapanış (`docs/durum.md` Bağlama 6 Links yorumu). Finisher'ın dünya efekti
  (patlama/heal/stealth vb. string) henüz simüle edilmiyor — yalnızca bildirim + çarpan.
  **Zone yaşam döngüsü Core'da var (Görev 7)** — `ZoneDirector` soft-cap/süre/movement;
  Game/Manifestation'a bağlı değil (görsel/hasar yok).
  **TimeEffectDirector (Görev 9 + Bağlama 8 + kararsız paket)** — echo / extend_lifetime /
  delayed_detonation / death_delay Game'e bağlı.
  **RealityEffectDirector (Görev 10) + Bağlama 11** — Game'e bağlı: ElementOrigin ↔
  revive_block (PlayerVitals kapısı) / partial_erase / full_erase(shields);
  minions/summons sistem yok.   **PassiveDirector (Görev 4 + Bağlama 5 + kararsız paket)** — Game'e bağlı: tetik/süre/
  DamageMult/HealMult/LifestealAdd/CritChanceAdd/DamageTakenMult/ArmorAdd/ReflectRatioAdd/
  DashCooldownMult + PassiveHud. Kalan: `reveal_radius_mult`.
  `global_rules.resource_system`/`cooldown_rules` → Core sınıflar var; Bağlama 2–4 bağladı;
  `status_durations` ↔ StatusTuning fark listesi (Görev 2, yukarıda; otorite açık).
  **Görev 3:** `state_machine` okunuyor (`PlayerStates`/`BossStates` + `PlayerStateMachine`;
  SentencePhase bağlanmadı).   **Görev 11 + Bağlama 9:** `equipment_system` Core + Game —
  sabit Alev Kılıcı; `ApplyClosingDamage` outMult × match bonus; envanter seçimi yok.   **Görev 13:** `prezentasyon-katmani.json` Core'da okunuyor
  (`PresentationCatalog` 16×16×10). **Görev 14:** `PresentationValidator` hitbox+animation
  doğrular; **`target_ally` hitbox prezentasyonda yok** (6 fiil). **Görev 16:**
  `PlaceholderFactory` trail/impact placeholder üretir; ManifestationDirector spawn
  bağlama yok (Faz 6). **Görev 15 + Bağlama 10:** `AnimationBridge` `ShoutSkill`'de
  bağlı (`PresentationValidator` + katalog); `PulseRune` duruyor. JSON
  `animator_state` (`Spell_Cast_*` / `Melee_*` / …) Quaternius
  (`CastPierce` / `BasicStrike` / …) ile örtüşmüyor — eşleme Faz 6 (eksik state
  sessiz atlanır). Görev 17 (element_colors audit) sırada.
- **`SkillResolution.Hitbox` = `target_ally` prezentasyon katmanında yok (Görev 14):**
  `prezentasyon-katmani.json` hitbox_library'de `target_ally` id'si yok. Element fiilleri:
  `cc_arindirma`, `hiz_buff`, `kalkan_transferi`, `arindirma`, `kutsal_kalkan`, `dirilis`.
  Validator `HitboxFound=false` döner; sessiz atlama — hitbox ekleme / eşleme kararı açık.
  `status_interaction_table`
  uygulama artık `StatusReactionTable.Rebuild(motor.StatusInteractions)` — 3 özel satır
  (burn+poison / shield+burn / stun+knockback) hâlâ StatusBoard.Tick / Applicator'da.
  `atoms_catalog` /
  `all_verbs_atoms` / `atom_kombinasyonlari` kasıtlı okunmuyor.
- **Zone `RadiusM` JSON'da yok (Görev 7 / Bağlama 7)** — `ManifestationTuning.ZoneDefaultRadiusM=3.6`
  (`ClosingBangRadiusM` ile aynı); JSON'a yarıçap eklenirse tuning yerine okunmalı.
- **`ui_rules.zone_display`** — `in_world` + transparency 0.6 Bağlama 7'de uygulandı
  (`CreateZoneDisk`). Zone CC (root/slow): Bağlama 11 — `skill.Mechanics` →
  `ZoneInstance.CcKind`, boss Approach StatusBoard dinler. Fiziksel wall collider yok
  (teknoloji soft-clamp).
- **`read_as_display.duration_ms` (1500) ≠ `FeelTuning.ReadoutHoldMs` (900)** — Görev 12'de
  not düşüldü, değiştirilmedi (sahibi otorite seçer).
- **space_layer (Görev 8 + 17 Eyl hat/yarığı):** blink/zenitsu/stealth_shift →
  `SkillMotionMotor`; `karabasan_hat` / `hiclik_yarik` → `SpaceDirector` +
  `SpaceDirectorHost` (link tick/kopma, tear cross). Portal köprüsü `StateBridgeBoard`.
- **`reality_layer` full_erase minions/summons (Görev 10):** oyunda minion/summon
  sistemi yok — uygulanamaz. Yalnızca `shields` uygulandı; uydurma minion sistemi
  kurulmadı.
- **His deneme (geçici):** arena `ArenaVisualScale=3`, boss hasar 0, `AllyDummy` %50 —
  kalıcı tasarım değil; his bittiğinde geri alınacak.
- **SkillMotion selective hedef UI yok** — Zenitsu = boss menzildeyse; ally blink yok.
- **Karadul faz 2+:** `shadow_cut`/`chain-rift-lock` JSON'da hâlâ `implemented:false`,
  BossDirector'da yok (Slam + FireCone var). Adaptation spam cezası yok.
- **Ateş ailesi'nin bazı fiillerinde `base_resource_cost` yok** — v5.2/v5.3 kaynağı her fiil
  için doldurmadı; eksik olduğu fiillerde `VerbNode.BaseResourceCost` `0` döner (gerçek değer
  değil, "yok" anlamına gelir).
- **Co-op / ikinci oyuncu açık** (§13 soru 5) — ikinci bir oyuncu gerekiyor, tek başına kodla
  kapatılamaz; bkz. dosya başı "Sıradaki".
- **Rune enum adları artık gerçek element adlarıyla eşleşiyor** (bu turda düzeltildi —
  bkz. yukarıdaki not) — eskiden `Rune.Igne`/`Suru`/`Kabuk`/`Zehir`/`Sarsinti`/`Toprak` gibi
  beşgen kalıntısı adlar gerçek elementle (`DisplayName`) uyuşmuyordu.

### Build notları

Taşındı → `docs/unity-notlari.md` (sahne/derleme/Android build tuzakları — tasarım kararı
yok, hepsi "şunu yapma, çalışmıyor" cinsinden, Unity/cihaza dokunacak görevde okunur).

## master ayrışması (16 Eylül, 4. tur sonu)

`origin/master` 29 Ağustos'tan beri **bu sohbetten habersiz, paralel bir hatta** ilerlemiş:
kendi "proje sadeleştirme"si + 13 Eylül'de kilitlenen ayrı bir "v4.2 element spec" +
tamamen başka bir çözücü (`Core/Elements/ElementCatalog.cs`/`ElementResolver.cs`). Sahibine
soruldu: **bu sohbetteki hat (v5.3/`SkillMotor`/ulti) esas alındı**, `origin/master`'ın
paralel içeriği atıldı (`git merge -s ours` ile tarihçe bağlandı ama dosyalar hiç girmedi).
Tek kurtarılan parça `docs/unity-notlari.md` (operasyonel Unity/build notları, gerçekten
yeni ve kullanışlıydı). **Ders: bundan sonraki her oturum işe `git fetch && git log
origin/master` ile başlamalı** — bu ayrışma günler önce fark edilebilirdi.
