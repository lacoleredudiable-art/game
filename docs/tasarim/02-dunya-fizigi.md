# Bölüm 2: Dünya fiziği (22:40)

*Kaynak: `karakterler/v4/dunya-fizigi.md`*

## Dünya fiziği (v4)

*Dünya fiziği botu. Sürüm 4, 9 Ekim 2026, 21:51 sonrası (UTC+3). Önceki sürümün yedeği: `dunya-fizigi.md.bak-2151`. Bu sürüm gövdeyi baştan yazar ve `v3-kararlar.md` dosyasındaki bütün kararları 21:17b kaydına kadar işler. Oyun reposuna ve başka hiçbir dosyaya dokunulmadı.*

*Ek 22:16 (yedek `dunya-fizigi.md.bak-2216`): grup sohbetindeki kullanıcı kararları (**K 22:15**) ve A'nın kullanıcıyla anlaşıp uyguladığı maddeler (**A 22:1x**, son kontrolde K 22:15 kaydına bağlandı) işlendi. Ek 22:18: AÇ-7 kapandı (itmeler menzille kısılmaz, 5-1 dahil; R-7 GameDesign düzenlemesi korundu); Tılsım toparlanma 0,45 s, Küre yavaşlatması = 0,4 s toparlanmada yürüme ×0,5, Yay otomatik germe 0,35 s (erken bırakma yok), hasar ritimle ölçeklenir (fizik dışı, bilgi) [K 22:15; itme K 22:17]. Değişen yerler: §1.1, §2.1, §3.1, §4.3, §4.5, §4.6, §5, §6.1, §7, §8.2, §16, §17 ve §15.3.*

Bu belge dünyanın fizik kurallarını tek yerde toplar: şeyler ve varlıklar, ağırlık, menzil, teslim sınıfı (yakın vuruş, mermi, alan), itme ve çarpma, kendini taşıma ve yolculuk, Güdümlü kilidi, tasma ve zincir, yapılar ve duvarlar, çağrılan yaratık, Ortak yükü, dünyanın zamanı ve Zaman kaydı, boss, yerçekimi.

Kurallar fiile göre değil, dünyadaki şeylerin özelliğine göre yazıldı. İstisna yazılmaz (K 07.10, K 17:44: "Uymayan yerde kural ayrıntılandırılır, istisna yok"). Her kural `fizik-komutlari.md`'deki komutlardan birine bağlanır; komut adları köşeli parantezde geçer (ör. [K04 `MermiFirlat`]).

**Sayılar:** Bütün sayılar ayarlanabilir başlangıç değeridir. Kodda ayar alanı olarak durur; onay kurala verilir, sayıya değil (K 17:28). Kaynağında sayı olmayan değerler **ÖNERİ** etiketlidir.

**Karar verilmeyenler:** Kaynağı olmayan ya da kaynakları çelişen her nokta son bölümdeki **Açık** listesindedir. Bu belge hiçbirine karar vermez.

---

### 0. Kaynaklar, öncelik, etiketler

#### 0.1 Kaynaklar (yalnız bunlar)
| Kısa | Dosya |
|---|---|
| **K hh:mm** | `karakterler/v3-kararlar.md`, o saatteki kullanıcı kaydı. 7 Ekim maddeleri **K 07.10 m.n**, sıfat kararları **K Sn**, fiil kararları **K Fn**, "9 Ekim öğleden sonra" bölümü **K 9E** diye geçer. |
| **Kitap §n** | `v4/kural-kitabi-v4.md`. Bölüm numarasıyla anılır, satır numarası verilmez. |
| **FK Knn** | `v4/fizik-komutlari.md`, komut numarasıyla. |
| **Silahlar**, **SF** | `v4/silahlar.md`, `v4/silah-fiil-120.md` |
| **AS-n** | `v4/acik-sorular.md` |
| **BRIEF** | `run-ajanlari/BRIEF.md` |

`arsiv/` klasörü kullanılmadı.

#### 0.2 Öncelik
En yeni kullanıcı kaydı kazanır (Kitap başlığı). Bu belgede geçen kayıtların sırası (yeniden eskiye):

K 21:17b > 21:17 > 21:15 > 21:13 > 21:12 > 21:11 > 21:10b > 21:10 > 21:09 > 21:07 > 21:05 > 20:37–20:42 > 20:35 > 20:29 > 19:48 > 19:41 > 18:42 > 18:39 > 17:44 > 17:42 > 17:41 > 17:40 > 17:36 > 17:35 > 17:33 > 17:28 > 17:27 > 17:21 > 17:15 > 17:05 > 16:52 > 16:49 > 16:40 > 16:36 > 15:55 > 15:53 > 15:51 > 15:47 > 15:46 > 15:41 > 9E > 12:17 > 11:46 > 11:17 > 07.10

A'nın (takım lideri ajan) kararları kullanıcı kaydı değildir. Kayıtla çelişirlerse kayıt kazanır.

#### 0.3 Etiketler
| Etiket | Anlamı |
|---|---|
| [K hh:mm] | Kullanıcı kaydı. |
| [K 22:15] | Kullanıcının grup sohbetindeki kararı (A iletti). `v3-kararlar.md`'ye henüz işlenmemiş olabilir. |
| **[A 22:1x]** | Kullanıcıyla anlaşılan ve A'nın uyguladığı madde (22:40 itibarıyla hepsi K 22:15 kaydına girdi, etiket artık kullanılmıyor). |
| [Kitap §n] | Kitap kuralı. Kaynağı kitapta yazılıdır (K, T3 ya da V4). |
| **[A kararı hh:mm — kullanıcı onayı bekliyor]** | Takım lideri ajan A'nın çözümü. Geçici kural; kullanıcı kaydı değildir. |
| (türetildi) | Kayıtta satırı yok; bir genel kuraldan doğrudan çıkarıldı. Onay bekler. |
| **ÖNERİ** | Karar değil. |

---

### 1. Dünyadaki şeyler

**Varlık**, dünyada yeri olan, bir şeye değen ve kendisine değilebilen şeydir. Varlık mermiye çarpar, duvara takılır, araya girebilir. **Varlık olmayan** şey bir bölge ya da bağıntıdır: hiçbir şeye çarpmaz, hiçbir şey ona çarpmaz.

| Şey | Varlık | Can | Ağırlık | Kaynak |
|---|---|---|---|---|
| Oyuncu, küçük düşman | Evet | Var | Hafif | Kitap §1.8 |
| Boss | Evet | Var | Ağır | Kitap §1.6, §1.8 |
| **Çağrılan yaratık** | **Evet** | **Var**; düşmanlar onu hedef alıp vurabilir | Kaynakta yok (Açık AÇ-3) | [K 21:12] |
| **Mermi** | **Evet** | Yok | Hafif | [K 16:52], Kitap §1.8 |
| **Yol alan beden** (Hareket ya da yolculuk sırasındaki oyuncu) | **Evet** (oyuncudur) | Var | Hafif | [K 21:07] |
| Yapı (yalnız Sabit diker) | Evet | Var | Çapalı | [K 07.10 m.5], Kitap §1.12 |
| Arena nesnesi (duvar, sütun, kaya) | Evet (türetildi) | Kaynakta yok (AÇ-19) | Çapalı | [K 15:51] "çapalıya (duvar/yapı/zemin işareti)" |
| **Alan** | **Hayır**: yerde bir bölgedir | — | — | [K 16:52] |
| **Yakın vuruş** | Hayır: silahın temas anındaki teslimidir | — | — | [K 21:05] (sınıf), [K 16:52] ("yakın vuruş gibi") |
| İşaret, zincir, Tetikli'nin kapanı, Ortak'ın yükü, Güdümlü'nün kilidi | Hayır; yapı değildir, canları yoktur | — | Taşıyanınki (işaret) | Kitap §1.10 |

#### 1.1 Gövde yarıçapları ve kenardan ölçme
| # | Kural | Başlangıç değeri (ayar) | Kaynak |
|---|---|---|---|
| D-1 | Her varlığın bir gövde yarıçapı vardır. | oyuncu 0,35 m · boss 2,5 m (parçalı boss'larda parça başına, boss belgesinde; Kül Ejderi `14` KE-2.5) · yaratık 0,5 m · yapı 1 m | [K 22:15] |
| D-2 | **Bütün mesafeler gövde kenarından ölçülür:** menzil, halka, sekme, tasma uzunluğu, itme. | — | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) |
| D-3 | **Canlılar birbirinin içinden geçemez.** Kenarda dururlar. | — | [K 22:15] (KG-38 kapandı) |
| D-4 | Yörünge yarıçapı taşıyanın gövde yarıçapına eklenir: parçalar taşıyanın kenarından 2 m uzakta döner. | 2 m + taşıyan yarıçapı | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) |

---

### 2. Ağırlık

| # | Kural | Kaynak |
|---|---|---|
| W-1 | Üç sınıf vardır: **Hafif** (oyuncu, küçük düşman, mermi) · **Ağır** (boss) · **Çapalı** (zemin, yapı, zemine konmuş işaret, duvar). | Kitap §1.8; [K 15:51] |
| W-2 | **Kimin gideceğini ağırlık seçer:** hafif olan ağıra gider, çapalıya hep sen gidersin. | [K 07.10 m.8], [K 9E] |
| W-3 | Hafif ile ağır ya da çapalı arasında mesafenin tamamını hafif olan kat eder. Ağır ya da çapalı olan 0 m kıpırdar. | Kitap §1.8 |
| W-4 | Fiil ağırlığı kendiliğinden tetiklemez. Ağırlık yalnız bir yer değiştirmenin kimde olacağını seçer. | [K 07.10 DÜZELTME] |
| W-5 | Yalnız hafif itilir. Boss itilmez, çekilmez; onun yerine denge hasarı alır [K28 `DengeVer`]. | Kitap §1.6, §1.8 |
| W-6 | Ağırlık hiçbir durumla (sendeleme, güçlendirme, zayıflatma) değişmez. | (türetildi; değiştiren kural yok) |
| W-7 | Canlıya konan işaret o canlıyla yürür ve onun ağırlığını taşır. | Kitap §1.10 (türetildi) |
| W-8 | Çekim alanı yoktur. Mıknatıslı kalktı. | [K 17:21], Kitap §1.8 |

Komut: fizik katmanının "Ağırlık" servisi (FK §4.1).

---

### 3. Menzil, hedefe ulaşma, boşa gitme

#### 3.1 Menzil
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| R-1 | Menzili silah belirler. Yön, hedef, alan, süre ve biçim sıfattadır; sıfat şekli silahın menzili içinde çizer. | `menzil = range_mult × 6 m` | [K 15:41], Kitap §1.3 |
| R-2 | Silah menzilleri | Yumruk 3 · Kalkan 3 · Kılıç 6 · Çekiç 6 · Küre 7 · Kitap 9 · Yay 14 · Asa 15 · Tılsım 17 · Top 18 m | Kitap §4.2 |
| R-3 | Dost etkisinde menzil | min(silah menzili, 6 m) | Silahlar (15:42), Kitap §1.3 |
| R-4 | Hareket'te menzil, en fazla yer değiştirmedir. | silah menzili | Silahlar (15:42) |
| R-5 | **Şeklin sınırı:** skillin çıktığı noktadan (sen ya da işaret) etkinin ulaştığı en uzak nokta silah menzilini aşamaz. | — | Kitap §1.3 |
| R-5b | Menzil, kullanan ile hedefin gövde kenarları arasında ölçülür (D-2). | — | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) |
| R-6 | **Sıfatın iç ölçüleri silah menziliyle kısılır:** halka yarıçapı, sekme mesafesi vb. Ayar bayrağı: `menzil_ic_olcuyu_keser`. | Yayılan halkası = min(4 m, menzil); Sıçrayan sekmesi = min(6 m, menzil); örn. Yumruk'ta halka 3 m | [K 21:10] |
| R-7 | İtme ölçüsü menzille kısılmaz; menzil yalnız erişimi kısaltır. 5-1 dahil istisna yok. | 5-1: 3 m × 1,35 | [K 22:17] |
| R-8 | İşaret koyma: silah menzili (dosta min(silah, 6 m)). Kullanma: 25 m içindeki takım işaretleri. Skill işaretten silah menziliyle çıkar. | 25 m | [K 15:47] |

Komut: kural motoru adım 4 (FK §1.2).

#### 3.2 Hedef menzil dışındaysa: karakter yürür
| # | Kural | Kaynak |
|---|---|---|
| R-9 | Hedef var ama menzil dışındaysa karakter menziline kadar **yürür**, sonra atar. Saldırı ve destek için aynıdır. | [K 21:17] |
| R-10 | Bu yürüme normal yürümedir; Hareket fiili değildir, hız devrine girmez (türetildi). Yürüyen karakter bir varlıktır: hasar alır, mermiye araya girer, duvara takılır (türetildi, K 21:07 ile aynı). | (türetildi) |

Komut eşlemesi: FK'de bu yürüme için ayrı bir komut yok (uyumsuzluk U-9).

#### 3.3 Yedek hâl yok; skill ne zaman boşa gider
| # | Kural | Kaynak |
|---|---|---|
| R-11 | **Yedek hâl yoktur.** Skillin koşulu yoksa skill boşa gider; oyuncu bilinçli atmalıdır. | [K 21:15] |
| R-12 | Skill yalnız şu iki hâlde boşa gider: (a) geçerli hedef hiç yoksa (örn. şifa atacak dost kalmamış), (b) süresi dolduysa (örn. Ortak 5 s, Tetikli 5 s). | [K 21:17] (K 21:15'i inceltir) |
| R-13 | **Iska ve boşa gitme yalnız skillshot'larda** olur. Skillshot'un tanımı kaynakta yok → AÇ-35. | [K 21:17] |
| R-14 | Kayıtta adı geçen hücreler: 2-8 dost yoksa boşa; 3-9 işaret yoksa boşa; 4-11 dost skill atmazsa boşa; 10-7 düşman 5 s saldırmazsa boşa; 10-9 işaret ya da dost yoksa boşa. | [K 21:15] |
| R-15 | Eski yedekler düştü: Ortak'ın "5 s skill yoksa ×0,5 kendi otomatik hedefi" yedeği (K 17:15) ve Tetikli'nin "5 s'de kendiliğinden işler" hâli. | [K 21:15, 21:17] (en yeni kayıt) |

---

### 4. Teslim sınıfı: yakın vuruş, mermi, alan

#### 4.1 Silaha göre sınıf
| Sınıf | Silahlar | Komut | Kaynak |
|---|---|---|---|
| **Yakın vuruş** | Yumruk, Kılıç, Çekiç, Kalkan | [K03 `YakinVurus`] | [K 21:05] |
| **Mermi** | Yay, Top, Kitap | [K04 `MermiFirlat`] | [K 21:05] |
| **Alan** | Asa ışını (anlık hat), Küre konisi, Tılsım gecikmeli mührü | [K05 `AlanAc`] | [K 21:05] |

- **Sıfat şekli bu sınıfın üstüne biner.** Yayılan halkası her zaman alandır [K 21:05].
- Diğer sıfat şekilleri mermi silahlarında hangi sınıfa girer (ör. Yay ile Çizgisel hattı, Top ile Sıçrayan sekmesi)? → AÇ-31.

#### 4.2 Mermi (varlık) [K04 `MermiFirlat`]
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| M-1 | Mermi bir varlıktır ve uçar. | hız 30 m/s, yarıçap 0,25 m (ÖNERİ, FK K04) | [K 16:52] |
| M-2 | **İlk değdiği** canlıya ya da yapıya çarpar; sonrakilere ulaşmaz. | — | [K 16:52] |
| M-3 | **Araya giren vuruşu alır.** Mermi ile hedefi arasına giren canlı (yol alan beden ve çağrılan yaratık dahil) vuruşu alır. | — | [K 16:52], [K 21:07], [K 21:12] (türetildi: yaratık bir canlı varlıktır) |
| M-4 | **Sabit duvar mermiyi tutar.** | — | [K 16:52] |
| M-5 | **Yansıma mermiyi kaynağına geri yollar.** | — | [K 16:52] |
| M-6 | Mermi hafiftir. Arındırma mermiyi silmez. | — | Kitap §1.8, §2 |
| M-7 | Mermi hasarı 3 s azalmasına girmez; etkileri girer. | — | [K 16:36] |
| M-8 | Mermi en çok silah menzili kadar uçar. | silah menzili | [K 15:41], FK K04 |

#### 4.3 Alan (varlık değil) [K05 `AlanAc`]
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| A-1 | Alan yerde bir bölgedir; içindeki herkese işler. "Herkes" taraf kuralıyla süzülür. | en çok 5 hedef | [K 16:52]; taraf: Kitap §1.2 |
| A-2 | **Duvar alanı tutmaz.** | — | [K 16:52] |
| A-3 | **Alana araya girilemez.** | — | [K 16:52] |
| A-4 | **Alan hasarı yakın vuruş gibi işler; yönü alan merkezinden hedefe doğrudur.** | — | [K 16:52] |
| A-5 | Koruma'nın şekli bu yöne göre tutar: Çizgisel önü merkeze dönükse tam tutar, arkası dönükse tutmaz; Yayılan %50 tutar; Yoğun ilk anı tam durdurur. | — | [K 16:52] |
| A-6 | Dostu alandan korumanın yolu, alanı kuranı kesmektir (Kontrol ya da Kuvvet). | — | [K 16:52] |
| A-7 | Alan **itmesinin** yönü merkezden değil, kullanıcıdan dışadır (§5). A-4 yalnız hasarın yönüdür. | — | (türetildi; K 9E + K 16:52) |
| A-8 | **Tılsım mührü:** hedefin ayağına düşer, belirli bir gecikmeden sonra açılır. Yolu yoktur, gecikme kaçılabilir. | `gecikme_sn` 0,5 | [K 18:39] |
| A-9 | **Asa ışını** anlık bir hattır (ömür 0). | uzunluk silah menzili, genişlik 1 m (ÖNERİ, FK K05) | [K 21:05] |
| A-10 | **Küre konisi** | koni açısı kaynakta yok (ÖNERİ, FK K05 `koni`) | [K 21:05] |
| A-11 | **Yayılan halkası** her zaman alandır. | yarıçap min(4 m, menzil), ×0,85, en çok 5 hedef | [K 21:05], [K 21:10], Kitap §3.2 |
| A-12 | Yayılan'ın otomatik merkezi: yalnız seni etkileyen fiilde (Hareket), fiilin etkilediği tarafın karşısındaki en yakın canlı (Hareket'te en yakın düşman, ondan dışa kaçış). Taraf fiillerinde değişmez (merkez sensin). | — | [K 18:42] |
| A-13 | Alan hasarı azalmaya girmez; alanın etkileri girer; bir alanın kendi tikleri hiç azalmaz. | 3 s | [K 16:36], [K 11:46 m.3] |
| A-15 | **Alan tikleri:** Koruma için her tik ayrı bir darbedir. İtme benzeri etkiler yalnız alanın **ilk teması**nda bir kez işler; sonraki tikler itmez. | — | [K 22:15] |
| A-14 | Arındırma alanı silmez; alanın dosta verdiği durumu söker. | — | Kitap §2 |

#### 4.4 Yakın vuruş [K03 `YakinVurus`]
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| Y-1 | Silah temasıyla, silah menzili içinde verilir. | menzil = silah menzili; yay 90° (ÖNERİ, FK K03) | [K 21:05], FK K03 |
| Y-2 | Yumruk serisi toplam hasarı değiştirmez, böler. Seri **tek darbe** sayılır (Koruma ve itme için). | 3 parça, 0,30 s içinde | Silahlar, FK K03; darbe: [K 22:15] |
| Y-3 | Duvarın arkasındaki hedefe yakın vuruş işler mi? Kaynakta yok. FK'deki ÖNERİ: işlemez. | — | → AÇ-37 |

#### 4.5 Darbe yönü ve Koruma [K17 `DurusAc`]
- **Koruma yalnız kullanıcıyı korur.** Kalkan vermez, can eklemez; kısa bir savunma duruşudur [K 16:49]. 12 Koruma hücresi onaylıdır [K 17:36].
- Duruşun yön maskesi şu yönlere göre tutar:

| Darbe | Yön | Kaynak |
|---|---|---|
| Alan hasarı | Alan merkezinden hedefe | [K 16:52] |
| Yakın vuruş | Vurandan hedefe | (türetildi) |
| Mermi | Merminin uçuş yönü | (türetildi) |

- **Darbe tanımı** [K 22:15]:
  - Bir vuruş bir darbedir.
  - Yumruk'un 3 parçalı serisi de tek darbedir.
  - Boss'un bir saldırı dizisi de tek darbedir.
  - Alan tiklerinin her biri ayrı darbedir (A-15).
  - Yapının (Sabit sütun) ve dönen parçanın (Yörüngeli) her vuruşu yeni bir darbedir; skillin kendi tekrarı olduğu için azalma uygulanmaz [K 22:54, K 11:46-3]. Bu yüzden Kuvvet-Sabit 2 s'de, Kuvvet-Yörüngeli 1 s'de bir yeniden iter (P-5c ile tutarlı: her darbe bir itme).
  - Koruma'nın "tek darbe / bir sonraki darbe" sayımı (Yoğun, Tetikli) bu tanımla yapılır.
- Durdurulan zarar kimseye geçmez (Kitap §2.1).
- Dostu mermiden korumanın fizik yolu, gövdeyle araya girmektir (M-3). Alandan korumanın yolu ise kuranı kesmektir (A-6).

#### 4.6 Yansıma [K23 `Yansit`]
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| Ya-1 | Gelen zararlı etkiyi (zarar, kontrol, zayıflatma) kaynağına geri yollar. **Geri dönen değer gelen değerin ×0,85'idir.** Eski "azalmadan döner" okuması düştü. | süre 2 s (Yoğun 0,45 s); oran 0,85 | [K F10]; oran [K 22:15] |
| Ya-1b | **Tek büyük vuruşun yansıması ölçüsüz hasar döndüremez** (kullanıcının şartı; örnek: boss'un tek atışlık vuruşu). A bunu şöyle ifade etti: yansıyan hasarın tavanı boss canının %5'idir (ayar). | tavan: boss canının %5'i | Şart [K 22:15]; %5 ve tavan biçimi [K 22:15] (kayda girdi) |
| Ya-2 | **Varsayılan olarak kullanıcıya kurulur.** Basılı tutup sürükleyince seçilen dosta kurulabilir. | — | [K 21:09] |
| Ya-3 | Odaklı-Yansıma kendiliğinden en çok hasar alan dostu seçer. | — | [K 21:09] |
| Ya-4 | Mermiyi geri yollar. | — | [K 16:52] |
| Ya-5 | Alan Yansıma'ya çarpmaz. Alan hasarı geri yollanır mı? FK'deki ÖNERİ: hayır. | — | → AÇ-35b |

---

### 5. İtme ve çarpma [K18 `It`, K28 `DengeVer`]

| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| P-1 | İtme her zaman Kuvvet'i kullanan oyuncudan dışadır. Yön, oyuncunun o anki konumundan hedefe giden düz çizginin devamıdır. Hedef çok yakınsa oyuncunun baktığı yön kullanılır. | yakınlık eşiği 0,2 m | [K 9E], Kitap §1.8 |
| P-2 | Mesafe | taban 3 m; Yoğun (5-1) 3 m × 1,35 (R-7, K 22:17; menzille kısılmaz); Yayılan ve Çizgisel 2,55 m; sekme 1,5 m; Yörüngeli 1 m | Kitap §1.8, §5.5; FK K18 |
| P-2b | İtmeler silah menziliyle **kısılmaz**; 5-1 dahil istisna yok. Menzil yalnız erişimi kısaltır (R-7). | — | [K 22:17] |
| P-3 | İtilme süresi | 0,15 s (ÖNERİ) | FK K18 → AÇ-7b |
| P-4 | Yalnız hafif ve yalnız düşman itilir. Boss itilmez, denge hasarı alır. Kuvvet çekmez. | boss denge ×1,0 (Yoğun 1,35 · Yayılan 0,85 · Yörüngeli 0,35/değme) | Kitap §1.6, §1.8; FK K28 |
| P-5 | İtme bir **etkidir**. Aynı hedefe 3 s içinde ayrı skillerden gelen aynı tür etkinin 2.'si ×0,5, 3.'sü ×0,25 işler. Bir skillin kendi tikleri azalmaz. | 3 s | [K 20:13], [K 11:46 m.3] |
| P-5b | **Azalma, her etkinin kendi ölçüsüne işler:** Kontrol'de süre, Kuvvet'te mesafe, Zayıflatma'da yüzde. "Aynı tür" = aynı fiil. Güçlendirme ve Zayıflatma toplanır. | — | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) |
| P-5c | **Darbe başına bir itme:** itme benzeri etki bir darbede bir kez işler (§4.5 darbe tanımı). Alanda yalnız ilk temasta işler (A-15). | — | [K 22:15] (KG-8'in tik sorusu kapandı) |
| P-6 | Konum önceliği: tutma > süreli etki > itme. | — | Kitap §1.8 (eski "çekme" basamağı Mıknatıslı ile düştü) |
| P-7 | **Çarpma:** itilen canlı bir şeye değerse çarpma hasarı olur. "Duvara çarparsa ezilir." | birim kaynakta yok → AÇ-21 | [K F5], [K 17:40] |
| P-8 | Çarpma hasarı bir hasardır, azalmaya girmez. | — | [K 16:36] (türetildi) |
| P-9 | Aynı anda gelen birden çok itme | ÖNERİ: vektör toplamı | FK K18 → AÇ-9 |

---

### 6. Kendini taşıma ve yolculuk [K24 `KendiniTasi`, K25 `Isinla`]

#### 6.1 Genel
| # | Kural | Kaynak |
|---|---|---|
| T-1 | Hareket yalnız kullanıcıyı taşır. Düşmana atılmak, senin hareket etmendir. Tek ek: İşaretli-Hareket'te işaretli arkadaş da seninle birlikte çekilir (ayrıntıları → AÇ-30). | [K 9E], [K 15:55] |
| T-2 | **Yol alan beden (bütün silahlarda aynı):** Hareket ya da yolculuk sırasında beden hasar alır, mermiye araya girer (M-3), duvar ya da yapı onu durdurur ve hızı 0 olur. Silaha özel değildir. | [K 21:07] |
| T-2b | Yol alan beden başka bir canlıya değerse onun kenarında durur (D-3). | [K 22:15] |
| T-3 | **Işınlanma yalnız Çizgisel-Hareket'tedir** [K25 `Isinla`]: oyuncu görünmez çizginin ucuna bir anda konur. Hat en çok silah menzili kadardır. Yol almaz, hiçbir şeye değmez; varışta hız 0. | [K 15:55], Kitap §5.3 |
| T-4 | Başka hiçbir kombo ve hiçbir silah ışınlamaz. Kitap'ın "sayfalara dağılması" yalnız görünümdür; beden gerçekten yol alır ve T-2'ye tabidir. | [K 20:35], Kitap §4.3 |
| T-5 | Yolculuk hızı ve yolda skill atılıp atılamayacağı silah ritminden gelir. Yol, yön ve mesafe sıfattan gelir. | [K 20:35], Silahlar |
| T-6 | Mesafe en çok silah menzili kadardır. Kalkan'ın omuz hücumu en çok 3 m'dir ve uzak işarete gidemez. | Silahlar (15:42), [K 20:35] |
| T-7 | Silahın ön süre adımı Hareket değildir (Kalkan'ın ağır adımı). Kılıç'ın VUR'u yerinde iaido'dur; şimşek atılış yalnız Hareket'tedir. | [K 21:05], Kitap §1.9 |
| T-8 | Fedakarlık-Hareket'i yavaşlatma ya da kökleme engelleyemez (`hareket_kesilmez`). | Kitap §5.3 (onay 11:13), FK K02 |
| T-9 | Yoğun-Hareket hedefe hızlı atılır ve **hedefin kenarında frenler**. İçinden geçmez (D-3). Eski "içinden geçip arkasına çıkar" metni düştü. | Kitap §5.3; [K 22:15] |

#### 6.2 Yolculuk hızları (başlangıç değerleri)
| Silah | Hız (m/s) | Yolda skill | Hızın kaynağı |
|---|---|---|---|
| Yumruk | 30 (Top ile eşit) | Yok | [K 21:11] |
| Kılıç | 25 | Yok | [K 21:11] ("Kılıç > Yumruk" reddedildi) |
| Top | 30 ("çok hızlı gülle") | Yok | Nitelik [K 20:35], sayı Kitap §4.3 |
| Küre | 25 ("patlama fırlaması") | Yok | Nitelik [K 20:35], sayı Kitap §4.3 |
| Çekiç | 22 ("Thor uçuşu", varışta çakma görünümü) | Yok | Nitelik [K 20:35], sayı Kitap §4.3 |
| Kitap | 18 | Yok | [K 21:11] |
| Tılsım | 16; mühür yolun ucuna düşer, 0,5 s sonra çeker | Var | [K 21:11], [K 20:35] |
| Yay | 14 (ipte süzülme) | Var (silah değiştirme de) | Nitelik [K 20:35], sayı Kitap §4.3 |
| Kalkan | 12 | Yok | [K 21:11] |
| Asa | 10 (ışında süzülme) | Var | Nitelik [K 20:35], sayı Kitap §4.3 |

- Küre fırlamasından sonra 0,4 s yavaşlama gelir. Bu bir ritim değeridir: `toparlanma_yurume_carpani` varsayılan 1,0, Küre 0,5 [K 20:35], [K 21:10b] [K01 `OnSure`].
- Ritim değerleri [K 22:17]: Tılsım toparlanması 0,45 s; Küre yavaşlaması = 0,4 s toparlanma boyunca yürüme ×0,5; Yay otomatik germe 0,35 s, erken bırakma yok. Hasar ritimle ölçeklenir (fizik kuralı değil, bilgi).
- Bu hızlar yalnız İşaretli-Hareket görünümleri için mi, bütün Hareket sıfatları için mi geçerli? → AÇ-25 (= AS-25).

#### 6.3 İşaretli-Hareket
- Atılmadır (Levi tarzı). Ağırlık kuralı geçerlidir: hafif olan oyuncu işarete doğru çekilir. Oyuncu yol boyunca görünür [K 15:55].
- Mekanik bütün silahlarda aynıdır: işarete yol alarak çekilme, mesafe silah menzili, ışınlanma yok. Görünüm silahtandır [K 20:35].
- İşaret yoksa skill boşa gider (3-9) [K 21:15].

#### 6.4 Hız devri (süren hareketin kesilmesi)
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| H-1 | Süren hareketi yalnız yeni bir kendini taşıma etkisi keser. | — | [K 15:53] |
| H-2 | Eski hızın yarısı yeni yöne aktarılır. | ×0,5 | [K 15:53] |
| H-3 | Toplam hız en çok temel hızın 1,5 katıdır. | ×1,5 | [K 15:53] |
| H-4 | Işınlanma ve Zaman dönüşü hız almaz, aktarmaz; varışta hız 0. | 0 m/s | [K 15:53] |
| H-5 | Yol alan beden duvara ya da yapıya değince durur, hızı 0 olur. | 0 m/s | [K 21:07] |
| H-6 | Yolda skill atılabilen silahlarda (Yay, Asa, Tılsım), kendini taşımayan bir skill hareketi kesmez. | — | [K 15:53] + [K 20:35] (türetildi) |

"Temel hız" ne demek, devir süreyi mi kısaltır mesafeyi mi uzatır → AÇ-24.

---

### 7. Güdümlü: kilitlenme [K13 `Kilitlen`]

| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| G-1 | Çizdiğin an hedefe kilitlenir. Otomatik hedef: en yakın uygun hedef. | — | [K 17:21] |
| G-2 | **Skill bölünemez; kendi süresi yok, bütün kilitlerde 8 s tavanı geçerli (G-16, K 22:54).** Hedefe vurana ya da dünyada bir şey durdurana kadar sürer (ör. hedefin önüne çıkan duvar). | — | [K 17:42] (K 17:21'deki "3 s peşinden gider" ve K 17:41'deki "3 s içinde menzile giremezse biter" ifadelerinin yerine geçer) |
| G-3 | **İlk 3 s** kullanıcı başka bir şey yapamaz: yön değiştiremez, skill çizemez. Hafif kontrol onu kesemez. 3 s'den sonra başka bir skill çizerse kilit bozulur. | `kilitli_ilk_sn` 3 | [K 17:21], [K 17:42] |
| G-4 | **Takip eden şey silahın vuruşudur:** yakın silahta kullanıcı koşar [K24], uzak silahta mermi kovalar [K04 `gudumlu_hedef`]. | — | [K 17:40] |
| G-5 | **Güdümlü-Zarar:** karakter durmaz; hedef silah menziline girene kadar kovalar (yayda neredeyse hiç koşmaz, kılıçta üstüne koşar). Menzile girince 3 kilit vuruşu yapar. | 3 vuruş | [K 17:40], [K 17:41] |
| G-6 | **Kilit vuruşu:** ıskalamaz; karakter otomatik olarak hedefe döner. | ara 0,3 s; çarpanlar ×0,6 / ×0,6 / ×1,0 | [K 17:41] kaydında "A tasarımı" diye geçiyor, kullanıcı kaydına alınmış |
| G-7 | **Güdümlü-Hareket:** hedefe direkt atılıp mesafeyi kapatırsın; vuruş yoktur. | — | [K 17:40] |
| G-8 | **Güdümlü-Kuvvet:** kullanıcı hedefi kovalar ve onu önünde iterek sürer. Hedef duvara çarparsa ezilir (çarpma, P-7). Boss itilmez, denge alır. | 3 s; itme hızı 1 m/s; sürükleme tek darbe [K 22:37] (FK K13) | [K 17:40] |
| G-9 | Güdümlü mermi bir varlıktır: duvar tutar, araya giren alır, Yansıma geri yollar. "İptal edilemez/durmaz", kullanıcının iptal edememesi ve yön değiştirememesi demektir. | — | **[A kararı 17:23 — kullanıcı onayı bekliyor]**. Duvar kısmı [K 17:42] ve [K 21:07] ile kullanıcı kaydına uyuyor. Araya giren ve Yansıma kısmı [K 16:52] genel kuralından geliyor. |
| G-10 | Kilit silah menzili içinde alınır. Sonraki takip menzilden bağımsızdır. | — | **[A kararı 17:23 — kullanıcı onayı bekliyor]**. A kararındaki "3 s takip" ifadesi [K 17:42] ile geçersiz (süreyle bitmez). Menzil kısmı [K 17:41] ve [K 21:17] ile çelişebilir → AÇ-46b |
| G-11 | Kilitli kullanıcı duvara değerse durur ve kilit biter. | — | **[A kararı 17:23]**; [K 17:42] ("dünyada bir şey durdurana kadar", duvar örneği) ve [K 21:07] (duvar ya da yapı durdurur) ile **kullanıcı kaydıyla doğrulandı** |
| G-12 | Güdümlü alan, hedefi merkez alan bir bölge olur ve hedefle birlikte taşınır. Alan yine varlık değildir. | — | **[A kararı 17:23 — kullanıcı onayı bekliyor]**. Kitap §3.2: "alan fiillerinde hedefin altında yürüyen bölge". Ne kadar sürdüğü (A: 3 s; K 17:42: süreyle bitmez) → AÇ-46c |
| G-13 | Güdümlü tasma kilit sürdükçe kalır; süreyi sıfat belirler. | K 17:21 örneği: "3 s kopmaz" | **[A kararı 17:23 — kullanıcı onayı bekliyor]**; [K 17:21] "Kontrol tasması 3 s kopmaz" ile uyumlu. Kilit süreyle bitmediği için tasma süresi → AÇ-46e |
| G-14 | Güdümlü-Hareket bir kendini taşımadır: ağırlık yasası geçerlidir, T-2'ye tabidir. | — | (türetildi) |
| G-15 | **Kilit hedef ölürse ya da kaybolursa biter.** | — | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) |
| G-16 | **Bütün Güdümlü kilitleri (kendine, dosta ve düşmana) en çok 8 s sürer.** Düşmana kilitli Güdümlü-Kontrol tasması da 8 s'de kopar. | 8 s (ayar) | [K 22:15], genelleştirme [K 22:54] |
| G-17 | **Temel saldırı ve dodge kilidi bozmaz.** Duvar durdurma kuralı (G-11) aynen geçerli. | — | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) |

---

### 8. Tasma, işaret ve zincir

#### 8.1 Kontrol tasması [K20 `TasmaBagla`]
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| B-1 | Kontrol işi keser ve bir bağ (tasma) kurar. | süre 1,5 s; sıfatın süre çarpanı işler (Yoğun 0,75 s) | [K 15:46], Kitap §1.7 |
| B-2 | Tasmanın uzunluğu kuruluş anındaki mesafedir, en çok silah menzili kadardır. | — | [K 15:46] |
| B-2b | Tasma uzunluğu gövde kenarları arasında ölçülür (D-2). | — | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) |
| B-3 | Tasmayı geren durur: düşman kaçamaz, tank uzaklaşamaz. Taunt yoktur. | — | [K 15:46] |
| B-4 | Tasmanın öbür ucu: Sabit tuzağında yapıdır, diğer bütün durumlarda Kontrol'ü kullanan oyuncudur. Yapı yıkılınca tasma aynı karede kopar. | — | Kitap §1.7 (A 19:46, K 15:51'e dayanarak) |
| B-5 | **Boss:** çapalıya (duvar, yapı, zemin işareti) bağlanırsa çivilenir; irade Açık iken çivilenir, Dirençli iken denge hasarı alır. Oyuncuya bağlıysa bağı geren boss oyuncuyu sürükler. Boss'u kıpırdatmayan bağ irade harcamaz. | Açık iken 4 s; Dirençli iken denge ×1,5 | [K 15:51] |
| B-6 | Korkutulan düşman kaçar ama tasma sınırında durur. | — | Kitap §1.7 (kuralın sonucu) |
| B-7 | K 21:13'te onaylanan Kontrol hücreleri (6-2, 6-3, 6-5, 6-7, 6-8, 6-10, 6-12) tasmalıdır. Tam metinleri `onay-bekleyen-hucreler.md` C grubunda. | — | [K 21:13] |

#### 8.2 İşaret ve zincir [K09 `IsaretKoy`, K10 `ZincirKullan`]
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| Z-1 | İşareti yalnız İşaretli-Arındırma koyar; kişi başı en çok 4 işaret, 5. işaret en eskiyi siler. | 4 | [K 9E] |
| Z-2 | İşaret dosta, düşmana (boss dahil) ve zemine konabilir; canlıyla yürür; kullanılınca silinmez. | ömür 20 s | Kitap §1.10 (9-12 master) |
| Z-3 | **Zincir:** işaretler konuluş sırasıyla zincirlenir (1→2→3→4). Skill zincire değen her uygun hedefe bir kez işler. | en çok 5 hedef, ×0,85; halka 1 m genişlik; komşu işaretler arası en çok 12 m; zincir ömrü 4 s | [K 17:33], Kitap §1.10 |
| Z-4 | Her oyuncunun zinciri yalnız kendi işaretlerini bağlar (en çok 5 zincir × 4 işaret). Takım, 25 m içindeki tek işaretleri kullanabilir. Bağ kullanan skill en yakın kendi zincirini seçer; basılı tutarak başka oyuncunun zinciri seçilir. İşaretler oyuncunun renginde görünür. | — | [K 17:35] |
| Z-5 | Zincir değmeleri azalmaya girmez. | — | [K 11:46 m.3] |
| Z-6 | Zincir bir alan gibi mi işler (duvarı geçer, araya girilemez)? | — | → AÇ-31 |

---

### 9. Yapılar, duvarlar ve arena nesneleri [K08 `YapiKur`]

| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| S-1 | Yapıyı yalnız Sabit diker. Koruma da dahil hiçbir fiil tek başına duvar ya da kalkan kurmaz. | — | [K 07.10 m.5], [K 16:49] |
| S-2 | Yapı bir varlıktır: canı vardır, çakılıdır (çapalı); saplanır, kanca atılır, yıkılır. | can 300 (ÖNERİ, FK K08) | [K 07.10 m.5] |
| S-3 | Şekil: duvar (Koruma, Yansıma) ya da direk (diğer 10 fiil). | duvar 4 × 2 × 0,5 m; direk 1 m taban; iş alanı 3 m (ÖNERİ, Kitap §1.12) | Kitap §1.12 |
| S-4 | Ömür | 6 s | Kitap §1.12 |
| S-5 | Sınır: kişi başı 5, takım başı 25 yapı. Aşınca en eskisi söner. | 5 / 25 | [K 11:17] |
| S-6 | Yıkılınca üstündeki işaret düşer, tasma aynı karede kopar. | — | Kitap §1.12 |
| S-7 | **Duvar mermiyi tutar** (M-4), **alanı tutmaz** (A-2), **yol alan bedeni durdurur** (T-2), **kilitli kullanıcıyı durdurur ve kilidi bitirir** (G-11). | — | [K 16:52], [K 21:07], [K 17:42] |
| S-8 | Yapı da yol alan bedeni durdurur. Mermi yapıya çarpar. | — | [K 21:07], [K 16:52] |
| S-9 | Düşman yapıya dayanıklılık hasarı verir; dost yapıya zarar yoktur; Şifa dost yapıyı onarır. | — | [K F1–F2] |
| S-10 | Arena nesneleri (duvar, sütun, kaya) çapalıdır (K 15:51). Canları, işaret ya da saplanma alıp almadıkları, K 16:52'deki "Sabit duvar"ın arena duvarını kapsayıp kapsamadığı kaynakta yok. | — | → AÇ-19 |
| S-11 | Canlılar yapının içinden yürüyebilir mi? | — | → AÇ-20 |

---

### 10. Çağrılan yaratık [K14 `YardimciCagir`]

| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| C-1 | Çağrılan yaratık bir **varlıktır**: canı vardır, düşmanlar onu hedef alıp vurabilir. | can kaynakta yok → AÇ-3 | [K 21:12] |
| C-2 | Yaratık yalnız düşmana saldırır. Korumak, tutmak ve bağlamak Tank fiillerinin işidir. | sayı 1 (Yayılan 3 küçük); ömür 5 s (Yoğun 2,5 s) | Kitap §2, FK K14 |
| C-3 | Bir varlık olduğu için düşman mermisine araya girer ve vuruşu alır; duvar onu durdurur. | — | (türetildi; K 21:12 + K 16:52) |
| C-4 | Ağırlığı, itilip itilemeyeceği, canı biterse ne olacağı → AÇ-3. | — | — |

---

### 11. Ortak yükü [K12 `YukBindir`]

| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| O-1 | Düğüm yoktur; dünyada Ortak'a ait bir nesne ya da bölge bulunmaz. | — | [K 17:05] |
| O-2 | Ortak etkini seçilen dostun (otomatik: en yakın dost) bir sonraki skilline yükler. Dost bekleme süresi içinde hangi skilli atarsa, senin fiilin onun skillinin şekli ve hedefleriyle birlikte işler. Taraf kuralı geçerlidir. | bekleme 5 s | [K 17:05] |
| O-3 | Dost 5 s içinde skill atmazsa skill boşa gider. Yedek yoktur. | — | [K 21:15], [K 21:17] (K 17:15'teki ×0,5 yedeği düştü) |
| O-4 | Yük, dostun skillinin teslim sınıfına uyar: mermiye binerse ilk değdiğinde durur ve duvar onu tutar; alana binerse duvarı geçer. | — | (türetildi; K 17:05 + K 16:52) → AÇ-45 |
| O-5 | İtme yükte de yükleyenden dışadır. Tasma yükleyene kurulur. | — | Kitap §5.5, §5.6 |

---

### 12. Dünyanın zamanı ve Zaman kaydı

#### 12.1 Dünya saati
- Dünyanın zamanı tektir; kimse için yavaşlamaz ya da hızlanmaz. Slow-mo ve genel zaman ölçeği yoktur. Hitstop yalnız görüntüdedir (Kitap §1.14; FK §4.1 "Dünya saati"). Bunun kullanıcı kaydında kendi satırı yok → AÇ-26.
- Zaman fiilinde yavaşlatma, hızlandırma, dondurma ve ileri sarma yoktur [K 9E].
- Yavaşlatma Kontrol'ün çeşididir (yürüme ve saldırı hızı −%50); dost hızlandırma Güçlendirme'nin işidir (+%15, en çok ×1,5). Bunlar yalnız tek bir canlının hızını değiştirir [K 21:48], [K F12 21:38], [K21 `DurumUygula`].
- Animasyon ve efekt oyun sonucu üretmez; komutların olaylarını dinler [K 19:48], FK §0.

#### 12.2 Zaman kaydı [K26 `KayitAl`, K27 `KaydaDon`]
| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| Ti-1 | Hedef, kaydın alındığı ana döner: **can ve konum**. Etkilere dokunulmaz. | — | [K 9E] |
| Ti-2 | Kayıt anını sıfat belirler: varsayılan 3 s önce; Sabit'te direğin dikildiği an; İşaretli'de işaretin konduğu an. | 3 s; Yoğun 1,5 s | [K 9E], FK K26 |
| Ti-3 | Yalnız dost ve sen; boss'a işlemez. | — | [K 9E] |
| Ti-4 | Ölü dostu yalnız kayıt direği diriltir. | — | [K 11:46 m.4] |
| Ti-5 | Konum her zaman tam döner. Can farkına çarpan işler: dönen can = (kayıttaki − şimdiki) × çarpan; kayıttakini aşamaz. | — | [K 17:28], Kitap §1.11 |
| Ti-6 | Zaman dönüşü hız almaz ve aktarmaz; varışta hız 0. Işınlanma sayılmaz. | 0 m/s | [K 15:53] |
| Ti-7 | Kayıt tamponu | örnek aralığı 0,1 s, tampon 20 s (ÖNERİ, FK K26) | FK K26 |
| Ti-8 | Grup 4 Zaman hücreleri onaylıdır. | — | [K 17:28] |
| Ti-9 | **Zaman alanı yoktur** (K 07.10 m.9'daki zaman alanı). | — | **[A kararı 21:30 (AS-21) — kullanıcı onayı bekliyor]**: A, 9 Ekim kaydının 7 Ekim'den yeni olmasına dayanarak kapattı. Kullanıcı kaydı yok → AÇ-25 |

---

### 13. Boss

| # | Kural | Başlangıç değeri | Kaynak |
|---|---|---|---|
| Bo-1 | Ağırdır: itilmez, çekilmez; onun yerine denge hasarı alır. | — | Kitap §1.6, §1.8 |
| Bo-2 | İrade halkası: Açık iken ilk tam kontrol işler, sonra Dirençli olur. Dirençli iken tam kontrol yerine denge hasarı alır. | Dirençli 45 s; denge ×1,5 | Kitap §1.6 |
| Bo-3 | Sersemletme, kökleme ve korkutma irade halkasından geçer. Susturma ve yavaşlatma her zaman işler ama yarı güçtedir. Küçük kontrol irade harcamaz, denge verir. | — | [K 21:48], [K 11:46 m.1] |
| Bo-4 | Tasma: çivileme ya da sürükleme (B-5). | — | [K 15:51] |
| Bo-5 | Güdümlü-Kuvvet ve Güdümlü-Hareket'te boss itilmez, denge alır; Güdümlü-Hareket'te sen boss'a gidersin. | — | [K 17:40], W-2 |
| Bo-6 | Sendelemede boss her şeyi tam yer. Bu itmeyi de kapsar mı? | — | Kitap §1.6 → AÇ-28 |

---

### 14. Yerçekimi

İzin verilen kaynaklarda dikey fizik kuralı yoktur. Kontrol çeşitleri arasında havaya kaldırma yoktur [K 21:48]. Dodge'da zıplama ve havada takla görünümü vardır [K 19:48]: dodge bir kez → zıplama; havadayken tekrar → takla. Bunun fizik sonucu (havadayken mermi ve alan) → AÇ-23.

---

### 15. `fizik-komutlari.md` ile eşleme ve uyumsuzluklar

#### 15.1 Eşleme (bu belge → komut)
| Bölüm | Komut |
|---|---|
| §3 Menzil, iç ölçü | Kural motoru adım 4 (`menzil_ic_olcuyu_keser`) |
| §4.2 Mermi | K04 `MermiFirlat` |
| §4.3 Alan | K05 `AlanAc` (`daire`, `hat`, `koni`, `nokta`; Tılsım `gecikme_sn`) |
| §4.4 Yakın vuruş | K03 `YakinVurus` |
| §4.5 Koruma | K17 `DurusAc` (`alan_yonu`) |
| §4.6 Yansıma | K23 `Yansit` |
| §5 İtme, çarpma | K18 `It`, K28 `DengeVer` |
| §6 Kendini taşıma | K24 `KendiniTasi`, K25 `Isinla` |
| §7 Güdümlü | K13 `Kilitlen` (+ K24 / K04) |
| §8 Tasma, işaret, zincir | K20 `TasmaBagla`, K09 `IsaretKoy`, K10 `ZincirKullan` |
| §9 Yapılar | K08 `YapiKur` |
| §10 Yaratık | K14 `YardimciCagir` |
| §11 Ortak | K12 `YukBindir` |
| §12 Zaman | K26 `KayitAl`, K27 `KaydaDon` |
| §13 Boss | K19 `KontrolUygula`, K28 `DengeVer` |
| Tetikli | K11 `KapanKur` |
| Fedakarlık | K02 `BedelOde` |

#### 15.2 Uyumsuzluklar — KAPANDI (son kontrol 22:40)

> **KAPANDI:** U-1…U-10 `fizik-komutlari.md` 21:56 sürümünde, U-11 (Güdümlü süresi) K 22:15 sigortalarıyla (bütün kilitler en çok 8 s, K 22:53; hedef ölür/kaybolursa biter) giderildi; G-12/G-13 süre kısmı A kararı onayına bağlı. Aşağıdaki tablo yalnız tarihsel kayıttır, FK'nin eski hâlini anlatır.

*(Eski başlık: Uyumsuzluklar, FK, saat 21:00 civarında yazılmış)*
| # | FK'de | Kullanıcı kaydı |
|---|---|---|
| U-1 | K24 `hiz_mps`: Kılıç 35 · Yumruk 20 | [K 21:11]: Yumruk 30 (Top ile eşit), Kılıç 25; "Kılıç > Yumruk" reddedildi |
| U-2 | K24 `durdurucular`: "yolda hasar ve mermiye araya girme: Kitap evet, diğerleri → AS-33" | [K 21:07]: bütün silahlarda aynı |
| U-3 | K14 `hedef_alinabilir`: hayır | [K 21:12]: yaratık varlıktır, canı var, hedef alınır |
| U-4 | K12 `yedek_carpan` 0,5 | [K 21:15/21:17]: yedek yok, 5 s'de boşa gider |
| U-5 | K11 `bekleme_sn` 5: "sonra kendiliğinden işler" | [K 21:15/21:17]: süre dolunca boşa gider (10-7) |
| U-6 | §1.2 adım 1 "Yansıma dosta kurulur mu → AS-28" | [K 21:09]: varsayılan sen; sürükleyerek dosta |
| U-7 | §1.2 adım 4 ve 6, §5: iç ölçü kesimi (AS-29) ve teslim sınıfı (AS-27) "öneri" | [K 21:10] ve [K 21:05] ile karar oldu |
| U-8 | K01 `toparlanma_yurume_carpani` [ÖNERİ] | [K 21:10b] ile karar: 1,0, Küre 0,5 |
| U-9 | "Menziline kadar yürü, sonra at" için komut ya da plan adımı yok; skillshot kavramı yok | [K 21:17] |
| U-10 | K06 `arama_yaricapi_m` 6, K05 `daire` r=4 sabit sayı | [K 21:10]: menzille kısılır (min(…, menzil)) |
| U-11 | K13: "3 s'den sonra yeni skill çizilince" bitiş doğru; ama K20'de "Güdümlü'de kilit boyunca" tasma, K05 Güdümlü alanı süresiz | A kararı 17:23 ile K 17:42 arasındaki süre sorusu açık (AÇ-46c, AÇ-46e) |

---

#### 15.3 K 22:15 / K 22:17 için `fizik-komutlari.md`'de gereken değişiklikler — KAPANDI (son kontrol 22:40)

> **KAPANDI:** F-1…F-11 `fizik-komutlari.md`'ye işlendi (§1.5 aynen karar, §1.6 mana/cooldown, K01, K07, K13, K15, K17, K18, K23, K24, §4.1). Tablo tarihsel kayıttır.
| # | Komut / yer | Gereken değişiklik |
|---|---|---|
| F-1 | K23 `Yansit` | `oran` "≤ 1" yerine 0,85. Yeni ayar: `yansima_tavani` = boss canının 0,05'i (A'nın ifadesi; kullanıcı şartı "tek büyük vuruş ölçüsüz dönmez"). |
| F-2 | §4.1 Servisler (yeni ya da "Çarpışma" servisine ek) | Gövde yarıçapları: oyuncu 0,35 · boss 2,5 · yaratık 0,5 · yapı 1 m. Canlılar iç içe geçmez, kenarda durur. |
| F-3 | §1.2 adım 4, K03, K05, K06, K18, K20, K29 | Bütün mesafeler (menzil, halka, sekme, itme, tasma uzunluğu) gövde kenarından ölçülür. |
| F-4 | K07 `YorungeKur` | `yaricap_m` = 2 + taşıyanın gövde yarıçapı. |
| F-5 | K24 `KendiniTasi` | `icinden_gecer` (Yoğun) kalkar; yerine "hedef kenarında frenler". Yeni durdurucu: başka bir canlının kenarı. |
| F-6 | K05 `AlanAc`, K17 `DurusAc` | Alan tiklerinin her biri Koruma için ayrı darbedir. İtme benzeri etkiler yalnız ilk temasta işler. |
| F-7 | K17 `darbe_siniri`, K11 (Koruma tetiği), K03 | Darbe tanımı: bir vuruş = bir darbe; Yumruk 3 parça = 1 darbe; boss saldırı dizisi = 1 darbe. |
| F-8 | K18 `It`, K13 (Kuvvet'te 1 m/s itme) | Darbe başına bir itme. Güdümlü-Kuvvet'in sürekli 1 m/s sürüklemesi tek darbe [K 22:37]; K13 işlendi. |
| F-9 | §4.1 "Azalma" servisi, K19, K21 | ×0,5 / ×0,25 her etkinin kendi ölçüsüne işler (Kontrol süre, Kuvvet mesafe, Zayıflatma yüzde). "Aynı tür" = aynı fiil. Güçlendirme ve Zayıflatma toplanır (K21'deki azalma satırıyla birleşimi → AÇ-47b). |
| F-10 | K13 `Kilitlen` | `bitis`'e eklenecek: hedef öldü ya da kayboldu. Yeni ayar: bütün kilitler en çok 8 s (K 22:54). Temel saldırı ve dodge kilidi bozmaz. Duvar durdurması kalır. |
| F-11 | K18 `It` `mesafe_m` | `mesafe_m` menzille kısılmaz (K 22:17); FK'deki her min(itme, menzil) ifadesi kaldırılmalı. 5-1 = 3 m × 1,35. |

### 16. Kapanan maddeler (eski KG numaraları korunarak)

| Madde | Kapatan | Sonuç |
|---|---|---|
| KG-2 Kontrol bağı | K 15:46 | Tasma (B-1…B-3) |
| KG-4 Boss bağlama | K 15:51 | B-5 |
| KG-12 İşaretli-Hareket | K 15:55 | Atılma; ışınlanma yalnız Çizgisel (T-3) |
| KG-16 İşaret erişimi | K 15:47 | R-8 |
| KG-10, KG-39 (çekim) | K 17:21 | Mıknatıslı kalktı, konu yok |
| KG-29 Sıfat iç ölçüsü ve menzil | K 21:10 | R-6 |
| KG-31 Teslim sınıfı | K 21:05 | §4.1 (sıfat şekilleri için kalan: AÇ-31) |
| KG-41 Güdümlü takip ve menzil | A 17:23 + K 17:42 | Süreyle bitmez (K); kilit menzilde alınır (A, onay bekliyor) |
| KG-43 Kilitli kullanıcı ve duvar | A 17:23 → K 17:42, K 21:07 | Duvar ya da yapı durdurur, kilit biter (G-11) |
| KG-40 Güdümlü mermi | **[A kararı 17:23 — kullanıcı onayı bekliyor]** | G-9 |
| KG-42 Güdümlü alan | **[A kararı 17:23 — kullanıcı onayı bekliyor]** | G-12 (süre: AÇ-46c) |
| KG-44 Güdümlü tasma | **[A kararı 17:23 — kullanıcı onayı bekliyor]** | G-13 (süre: AÇ-46e) |
| KG-25 Zaman alanı | **[A kararı 21:30 (AS-21) — kullanıcı onayı bekliyor]** | Ti-9; kullanıcı onayı için AÇ-25'te |
| KG-24 (ana kural) | K 15:53 | §6.4 |
| H-6 (eski öneri: yol alan hareket duvarda durur) | K 21:07 | T-2, H-5 |
| KG-8 Güdümlü-Kuvvet | K 17:40 + [K 22:15] | G-8; darbe başına bir itme, alanda yalnız ilk temas (P-5c). Kalan tek soru süre (K 22:15'te Güdümlü süreleri uygulanıyor). |
| KG-38 Canlı içinden geçme | [K 22:15] | Geçemez, kenarda durur; Yoğun-Hareket kenarda frenler (D-3, T-9). |
| AÇ-46(d) kısmen (hedef ölür / kaybolur) | [K 22:15] (kullanıcı onayı, `v3-kararlar.md`) | Kilit biter (G-15). |

---

### 17. Açık (karar verilmedi)

**Ağırlık ve bağ**
- **AÇ-1 (KG-1) · Eşit ağırlık.** İki hafif ya da iki ağır bir bağla ilişkilenirse kim gider? Tasma için cevap var (geren durur). İşaretli-Hareket'te hafif bir canlıdaki işarete gitmek için yok. Seçenekler: ortada buluşmak, kimse kıpırdamaz, bağı kuran gider.
- **AÇ-3 (KG-3) · Yaratık ve elit düşman.** Yaratığın canı kaç (sayı yok)? Ağırlığı ne (FK'de yok)? İtilir mi? Canı biterse ne olur? Elit düşmanın ağırlığı ne?
- **AÇ-5 (KG-5) · Ağırı itmeye çalışan geri seker mi?** Kaynakta geri sekme yok.
- **AÇ-6 (KG-6) · Çoklu tasma.** Aynı düşmana iki oyuncu tasma atarsa ne olur? Uzak bir tuzaktan kurulan tasmanın uzunluğu nasıl ölçülür? Dünyada aynı anda en çok kaç bağ olabilir?
- **AÇ-13 (KG-13) · Zincirde itme yönü.** Kuvvet'in "hep dışarı"sı ile ağırlık yasası çelişirse hangisi kazanır? Kitap: fiilin yönü.
- **AÇ-30 (KG-30) · İşaretli-Hareket'te birlikte çekilen arkadaş.** Hangi arkadaş? Kaç kişi? Aynı hızda mı gelir? Yolda hasar ve araya girme (T-2) onu da kapsar mı?

**İtme ve çarpma**
- **AÇ-7b · İtilme süresi.** Kaynakta yok (ÖNERİ 0,15 s). *(AÇ-7'nin menzil kısmı K 22:17 ile kapandı: itmeler menzille kısılmaz, istisna yok — R-7, P-2b.)*
- **AÇ-8 (KG-8 kalan) · Güdümlü-Kuvvet süresi.** Tik sorusu [K 22:15] ile kapandı (darbe başına bir itme). Kalan: 3 s mi (K 17:40), vurana ya da duvara kadar mı (K 17:42)? Kilit sürelerini A 22:1x'te uyguluyor.
- **AÇ-9 (KG-9) · Aynı anda gelen itmeler.** Vektör toplamı mı (ÖNERİ)?
- **AÇ-21 (KG-21/22) · Çarpma birimi.** 1 m kalan itme kaç hasar eder? "Ezilir" (K 17:40) ek bir şey mi demek? Çarpılan yapı hasar alır mı?
- **AÇ-24 (KG-24) · Hız devri boşlukları.** "Temel hız" ne? Devir süreyi mi kısaltır, mesafeyi mi uzatır? Dış itme ya da tasma süren atılmayı keser mi? Kendini taşıma azalması mesafeye mi süreye mi işler?

**Teslim, mermi, alan**
- **AÇ-31 (KG-31 kalan) · Sıfat şekli ve teslim sınıfı.** Yayılan dışındaki şekiller hangi sınıfa girer? Yay, Top ya da Kitap ile Çizgisel hattı: mermi mi (ilk değdiğinde durur), alan mı (FK: `AlanAc(hat)`)? Sıçrayan sekmesi, İşaretli zinciri, Yörüngeli parçaları, Tetikli patlaması, Sabit direğin iş alanı hangisi?
- **AÇ-32 (KG-32) · Dost mermisi.** Kendi takımının mermisinin önüne dost girerse ya da dost yapı varsa mermi söner mi, geçer mi?
- **AÇ-33 (KG-33) · Mermi neye çarpar?** Arena nesnesi, yörünge parçası, zincir, başka mermi (SF'de "havada düşürür" görüntüleri)? Duvara çarpan mermi saplanır mı (K 07.10 m.5)?
- **AÇ-34 (KG-34) · Darbe yönünün uç durumları.** Hedef merkezdeyse yön ne olur? Çizgisel önü yana dönükse nasıl tutar? Mermi ve yakın vuruş yönü türetildi: onaylanmalı. "Yoğun ilk anı tam durdurur": ilk tik mi, penceredeki ilk tik mi?
- **AÇ-35 · Skillshot tanımı** (K 21:17). Hangi skiller skillshot'tur ve ıskalayabilir: mermiler mi, seçilen noktaya atılanlar mı, Tılsım gecikmesi mi?
- **AÇ-35b (KG-35) · Yansıma ve alan.** Alan hasarı geri yollanır mı, kaynağı merkez mi kuran mı? Yansıma ayna duvarı alanı görebilir mi?
- **AÇ-36 (KG-36) · Fedakarlık-Zarar ve araya giren.** K 16:40 "araya giren korumaya ve yansıtmaya takılmaz, doğrudan cana gider". K 16:52 "mermide araya giren vuruşu alır". FK bunu `delme` bayrağıyla araya gireni de aşan bir şey olarak okuyor. Mermi silahında hangisi geçerli? Duvar onu tutar mı?
- **AÇ-37 (KG-37) · Yakın vuruş ve duvar.** Yakın vuruş duvarın arkasındaki hedefe işler mi?

**Yapı, arena**
- **AÇ-14/15/17/18 (KG-14, 15, 17, 18).** Yapıya işaret konur mu? Taşıyanı yok olan işaret ne olur? Kanca nedir? Yapı ölçüleri ve canı (300 ÖNERİ)?
- **AÇ-19 (KG-19) · Arena nesneleri.** Canları var mı? İşaret ve saplanma alırlar mı? K 16:52'deki "Sabit duvar tutar" arena duvarını kapsar mı? K 15:55 duvarı "ağır nesne" diyor, K 15:51 "çapalı" diyor.
- **AÇ-20 (KG-20) · Yapının içinden yürüme.** Canlılar yapının içinden yürüyebilir mi (dost ve düşman)?
- ~~AÇ-38 (KG-38) · Canlı içinden geçme~~ → kapandı, [K 22:15] (D-3).

**Kendini taşıma, Güdümlü, Ortak**
- **AÇ-25b (AS-25) · Yolculuk hızları.** Bütün Hareket sıfatlarına mı uygulanır, yalnız İşaretli-Hareket görünümlerine mi?
- **AÇ-R10 · Menzile yürüme.** K 21:17'deki yürüme Güdümlü'nün kovalaması ile aynı şey mi? Yürüme sırasında skill çizilirse ilk skill iptal mi olur? Yürüme süresince skill "atılmış" sayılır mı (bekleme süresi)?
- **AÇ-46 (KG-46) · Güdümlü kalanları.**
  - (a) Yansıma'nın geri yolladığı Güdümlü mermi kaynağa kilitli mi döner?
  - (b) Kilit "çizdiğin an" (K 17:21) mı alınır, yoksa "silah menzili içinde" (A 17:23) mı? Menzil dışındaki hedefi kovalamak (K 17:41, K 21:17) ile nasıl birleşir? Takip hızı nedir?
  - (c) Güdümlü alan ne kadar sürer (kendi süresi yok; 8 s tavanı geçerli, K 22:54)? Yönü (merkez hedefin kendisi) nasıl hesaplanır?
  - (d) Hedef ölür ya da kaybolursa kilit biter (G-15, K 22:15). Kalan: hedef ışınlanır ya da kayda dönerse "kaybolma" sayılır mı? Kilit bitince hız devri uygulanır mı?
  - (e) Güdümlü tasma ne kadar sürer: 3 s (K 17:21 örneği) mi, kilit boyunca mı (süresiz)? Boss sürüklemesi ne kadar sürer?
- **AÇ-45 (KG-45) · Ortak yükü.** Yük, dostun skillinin teslim sınıfına uyar mı (O-4 türetildi)? Dostun skilli Güdümlü, Ortak ya da Hareket ise ne olur? İki oyuncu aynı dosta yük koyarsa ne olur?
- **AÇ-11 (KG-11) · "Hafif kontrol".** Güdümlü'nün ilk 3 s'sinde kesemeyen hafif kontrol hangisidir? K 11:46'daki "küçük kontrol" mü?
- **AÇ-İ · İşaretsiz İşaretli.** K 9E'ye göre işaret yoksa skill senden çıkar. K 21:15'e göre yedek yok (3-9 ve 10-9 boşa gider). K 21:17'ye göre geçerli hedef varsa boşa gitmez. Kayıtta adı geçmeyen İşaretli hücreleri işaret yokken boşa mı gider, senden mi çıkar?

**Zaman, dünya**
- **AÇ-25 (KG-25) · Zaman alanı.** A 21:30'da kapattı (Ti-9). Kullanıcı onayı bekliyor.
- **AÇ-26 (KG-26) · Slow-mo yasağı.** Kayıtta kendi satırı yok; yalnız Kitap §1.14'te ve görev metninde geçiyor. Kayda geçirilsin mi?
- **AÇ-27 (KG-27) · Kayda dönüş ayrıntıları.** Kayıttaki konum kapalıysa ne olur? Zemin işareti kimin kaydını tutar? Kayıt anında ölü olan dost dirilir mi?
- **AÇ-23 (KG-23) · Dikey fizik.** Dodge'daki zıplama ve takla (K 19:48) sırasında mermi ve alan karaktere değer mi? Mermi yüksekliği var mı?

**Boss**
- **AÇ-28 (KG-28) · Sendelemede itme ve denge sayıları.** Sendelemede boss itilir mi? Denge çubuğu ve sendeleme süresi sayıları kaynakta yok.
- **AÇ-47 · Yeni kuralların açtığı küçük sorular.**
  - (a) Yansıma tavanı (%5) yalnız boss vuruşuna mı uygulanır, her kaynağın kendi canına mı?
  - (b) Güçlendirme ve Zayıflatma "toplanır" (K 22:15), ama azalma kuralı da var. Aynı hedefe gelen ikinci Zayıflatma önce ×0,5'e mi düşer, sonra mı toplanır? Güçlendirme'nin "en çok 2 kaynak" sınırı yerinde mi kalıyor?
  - (c) Boss'un saldırı dizisi tek darbe: dizinin bittiği an nasıl belirlenir?

---

<a id="b3"></a>
