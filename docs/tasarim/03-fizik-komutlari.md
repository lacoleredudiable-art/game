# Bölüm 3: Fizik komutları (22:40)

*Kaynak: `karakterler/v4/fizik-komutlari.md`*

## Fizik komutları: kural motoru → komutlar → fizik (Unity)

*9 Ekim 2026 ~21:55, K 22:15/22:17 güncellemesi ~22:40 (UTC+3; yedek `fizik-komutlari.md.bak-2233`). Hazırlayan: A (executor). Kaynak: `kural-kitabi-v4.md` 4.4, `kural-ekleri-dongu.md` Tablo F/S, `silahlar.md`, `dunya-fizigi.md`, `v3-kararlar.md` K 21:05–21:17b. Oyun reposuna dokunulmadı; kod yazılmadı. Yedek: `fizik-komutlari.md.bak-2154`.*

> **Bütün sayılar ayarlanabilir başlangıç değeridir** (kural kitabı "Sayılar"; K 17:28). Kodda her biri ayrı bir ayar alanıdır (`komut-ayarlari.json` önerisi), test ve denge aşamasında kod değişmeden değiştirilir. Kaynakta sayısı olmayan değerler **[ÖNERİ]** etiketlidir, karar değildir.
> **Bu belge karar vermez.** Kaynakta karşılığı olmayan her davranış ya [ÖNERİ] ya da `acik-sorular.md` maddesi (AS-n) olarak işaretlendi. K 21:05–21:17b ile bağlanan maddeler artık karardır (teslim sınıfı, iç ölçü kesimi, Yansıma hedefi, toparlanma yürüme, yaratık, yedek yok, skillshot, MenzileYuru).

---

### 0. Neden üç katman

K (17:44): "Motor 1440 skilli kurallardan üretir." Bu yüzden 1440 skill elle kodlanmaz. Her skill, aynı 29 standart komutun farklı dizilişidir.

| Katman | Ne yapar | Ne yapmaz | Unity'de |
|---|---|---|---|
| **1. Kural motoru** | `SkillSpec(fiil, sıfat, silah, girdi)` alır; otomatik hedefi çözer; taraf, menzil, ritim ve sıfat parametrelerini birleştirir; sıralı **komut listesi** (`KomutPlani`) üretir | Fizik hesaplamaz, sahneye dokunmaz | Saf C# (MonoBehaviour değil), veri: kural tabloları (JSON). Deterministik: aynı girdi → aynı plan (ağ uyumu) |
| **2. Komutlar** | Plandaki her komutu dünyada yürütür: gövde açar (mermi, alan, yapı…), etkiyi hedefe uygular (hasar, itme, tasma…), süreyi sayar | Hangi komutun seçileceğine karar vermez | Komut başına bir sınıf (`IKomut.Calistir(Dunya, ctx)`), sunucu yetkili |
| **3. Fizik** | Ağırlık, çarpışma (mermi / alan / yakın vuruş), konum önceliği, azalma tablosu, irade halkası, kayıt tamponu, yapı sınırı | Fiil ya da sıfat bilmez | Kinematic hareket + kendi çarpışma sorguları (`Physics.OverlapSphere`, `SphereCast`); Rigidbody dinamiği yok [ÖNERİ] |

Sunum (animasyon, VFX) bu üç katmanın dışındadır: komutlar olay yayar (`OlayYayinla`), `animasyon-kurallari.md` ve `efekt-motoru.md` onları dinler. Sunum oyun sonucu üretmez (kural kitabı 8).

---

### 1. Kural motoru (katman 1)

#### 1.1 Girdi
| Alan | Kaynak | Not |
|---|---|---|
| `fiil` (1–12), `sifat` (1–12) | Çizim (önce fiil, sonra sıfat; 1.1) | Tek çizim, parmak kalkınca biter |
| `silah` (1–10) | Elindeki silah | Menzil, ritim, yolculuk hızı, teslim sınıfı |
| `secim` | Basılı tutup kaydırma (~0,2 s sonra) | Canlı, yapı, boş nokta ya da yok (otomatik) |
| `sahip` | Oyuncu | Konum, bakış yönü, can, işaretleri, zinciri |

#### 1.2 Çözüm adımları (sabit sıra)
1. **Taraf** (1.2): fiilden. `dusman` (Zarar, Zayıflatma, Çağırma, Kontrol, Kuvvet), `dost` (Şifa, Güçlendirme, Arındırma, Zaman), `sen` (Hareket, Koruma, Yansıma*). *Yansıma varsayılan sana kurulur; basılı tutup sürüklenirse seçilen dosta (K 21:09). Odaklı-Yansıma en çok hasar alan dostu seçer. Koruma yalnız kullanıcı.
2. **Gövde** (Tablo F `govde`): `canli`, `sen`, `savunma`, `yardimci`.
3. **Otomatik hedef** (3.2, Tablo S): sıfatın kolu + fiilin ölçüsü (Odaklı). Seçim varsa otomatik kapanır. Geçerli hedef yoksa skill boşa gider (K 21:15/21:17); yedek hâl yok.
4. **Menzil** (1.3): `menzil = silah.range_mult × 6 m`; dost etkisinde `min(menzil, 6 m)`; Hareket'te en fazla yer değiştirme. Sıfat iç ölçüleri (halka yarıçapı, sekme mesafesi vb.) silah menziliyle kısılır: `min(iç_ölçü, menzil)` — ayar bayrağı (K 21:10).
5. **Menzile yürü** (K 21:17): hedef var ama menzil dışındaysa plana `MenzileYuru` eklenir; karakter menzile kadar yürür, sonra atar. Saldırı ve destek aynı.
6. **Ön süre** (4.1): `silah.on_sure + sifat.sarj` (Yoğun şarjı yalnız düşman fiilinde, K 19:41), tavan 1,0 s.
7. **Teslim sınıfı** (K 21:05): Yumruk/Kılıç/Çekiç/Kalkan = yakın vuruş; Yay/Top/Kitap = mermi; Asa ışını (anlık hat), Küre konisi, Tılsım gecikmeli mühür = alan. Sıfat şekli bunun üstüne biner (Yayılan halka her zaman alan).
8. **Komut planı**: §3'teki fiil ve sıfat eşlemesinden. Skillshot (§1.4) ise ıska mümkündür; Güdümlü asla ıskalamaz.
9. **Bedel/özel**: Fedakarlık ise `BedelOde` başa eklenir ve bütün etki komutlarına `fedakarlik=true` bayrağı geçer.

#### 1.3 Örnek planlar
- **Yoğun Zarar · Kılıç** (`AK-VORN`): `OnSure(0,20 + 0,35)` → `YakinVurus(hedef=en yakın düşman, menzil 6)` → `HasarVer(×1,35, denge ×1,35)`.
- **Yayılan Kontrol · Asa** (`NETH-BRAN`): `OnSure(0,30)` → `AlanAc(halka, merkez=sen, r=4)` → her düşmana (≤5) `KontrolUygula(1,5 s)` + `TasmaBagla(uç=sen, 1,5 s)`.
- **İşaretli Hareket · Yay** (`SHEV-ZUL`): `IsaretSec(en yakın, 25 m)` → `KendiniTasi(hedef=işaret, en çok 14 m, hız 14 m/s, yolda_skill=evet)`.
- **Sabit Zaman · Çekiç** (`KHOR-KETH`): `OnSure(0,55)` → `YapiKur(direk, 6 s)` + `KayitAl(3 m içindeki dostlar)` → yapı yıkılınca/bitince `KaydaDon(kayıt, dirilt=evet)`.
- **Tetikli Kuvvet · Tılsım** (`KRAG-TZEK`): `KapanKur(en yakın düşman, 5 s)` → tetikte `AlanAc(gecikme 0,5, tek hedef)` → `It(3 m, senden dışa)`.


#### 1.4 Skillshot ve boşa gitme (K 21:15, 21:17)

| Kavram | Kural |
|---|---|
| **Yedek hâl yok** | Skillin koşulu yoksa (dost yok, işaret yok, dost skill atmadı, düşman saldırmadı) skill boşa gider. |
| **Boşa gitme** | Yalnız iki hâl: (a) geçerli hedef hiç yok, (b) süre doldu (Ortak 5 s, Tetikli 5 s). |
| **Skillshot** | Iskalayabilenler yalnız şunlar: **İşaretli** atma noktası; **Çizgisel** hat (hatta kimse yoksa boşa); **mermi silahları** (Yay, Top, Kitap — ilk değdiği şeye çarpar, araya giren/duvar hedefe engel olabilir). |
| **Güdümlü** | Asla ıskalamaz; hedefe kilitlenir. |
| **Sabit** | Yapı yere dikilir; yanlış yer işe yaramaz ama skill boşa gitmez, yapı orada durur. |
| **Menzil dışı** | Skillshot değildir: karakter `MenzileYuru` ile menzile gelir, sonra atar. |

#### 1.5 K 22:15 ve K 22:17 kararları (aynen, `v3-kararlar.md`)

- Taban hasar silahın toplam ritim süresiyle ölçeklenir (saniyedeki hasar eşit, yavaş silah sert vurur). Silah kanunu değil, ritmin sonucu.
- Tılsım toparlanması 0,45 s.
- Yansıma taban oranı 0,85 (Yoğun ×1,35, tavan 1). ŞART: boss'un tek atan (öldürücü) büyük vuruşunda yansıtma sınırlı; dönen hasar tavanı ayarlanabilir (başlangıç: boss azami canının %5'i). Dünya fiziğindeki "hasar azalmaz" satırı buna göre değişir.
- Alan tiki: Koruma her tiki ayrı darbe sayar; itme gibi etkiler alanın yalnız ilk değişinde işler. Yumruk serisi ve boss atış dizisi tek darbe.
- Canlılar birbirinin içinden geçemez; kenarda durur (Yoğun-Hareket kenarda frenler).
- Gövde yarıçapları (ayarlanabilir başlangıç): oyuncu 0,35 m, boss 2,5 m, yaratık 0,5 m, yapı 1 m. Mesafeler gövde kenarından ölçülür.
- Güdümlü sigortaları: hedef ölür/kaybolursa kilit biter; bütün kilitler (düşman dahil) en çok 8 s (K 22:54). Temel vuruş ve dodge kilidi bozmaz.
- Mana/cooldown: cooldown skill işlediği an başlar; boşa giden skill mana harcar, cooldown'ın yarısını yer; Ortak/Tetikli'de cooldown atıldığı an başlar; Fedakarlık mana ödemez.
- Yay germe otomatik 0,35 s, erken bırakma yok.
- Azalma: ×0,5 etkinin kendi ölçüsüne (süre/mesafe/yüzde), aynı tür = aynı fiil; Güçlendirme ile Zayıflatma toplanır.
- Fedakarlık-Güçlendirme 2 kaynak sınırına uyar (8-8 düzeltilir).
- Sonra: Fedakarlık boss'ta dakikada 5 kez riski.
- K 22:17: Kuvvet itme mesafesi silah menziliyle KISALMAZ; menzil yalnız erişimi kısaltır, itme mesafesi sıfattan gelir. 5-1 buna göre düzeltilir (21:17b'deki "ölçü menzille kısılır" notu 5-1 için geçersiz). İstisna yok.

Komutlara etkisi: K01 (`hasar_carpani`, Tılsım 0,45, Yay otomatik germe), K13 (sigortalar), K17 (alan tiki = ayrı darbe), K18 (itme menzille kısılmaz; alanda yalnız ilk değiş), K23 (oran 0,85, boss tek atma tavanı), §1.6 mana/cooldown, §4.1 gövde yarıçapı, kenardan ölçüm, birbirinin içinden geçmez, azalma tanımı.

#### 1.6 Mana ve cooldown (K 22:15)
| Durum | Cooldown | Mana |
|---|---|---|
| Skill işledi | işlediği an başlar, tam | harcanır |
| Skill boşa gitti | yarısı | harcanır |
| Ortak / Tetikli | atıldığı an başlar | harcanır |
| Fedakarlık | normal | **ödenmez** (yalnız can) |

---

### 2. Komutlar (katman 2) — 29 komut

Sütunlar: parametreler ve başlangıç değerleri. "Kaynak" o değerin geldiği yer; kaynak yoksa [ÖNERİ].

#### 2.1 Zamanlama, bedel ve menzile yürüme

**K01 `OnSure`** — dokunuştan etkinin çıkışına kadar bekleme; şarjı da kapsar.
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `on_sure_sn` | silahtan: Yumruk 0,10 · Kalkan 0,30 · Kılıç 0,20 · Çekiç 0,55 · Küre 0 · Kitap 0,60 · Yay 0,35 · Asa 0,30 · Tılsım 0 · Top 0,40 | `silahlar.md` ritim |
| `sarj_sn` | Yoğun 0,35 (yalnız düşman fiili) | 3.2, K 19:41 |
| `toplam_tavan_sn` | 1,0 | `silahlar.md` |
| `yururken` | Yumruk, Kitap evet; diğerleri hayır | `silahlar.md` |
| `toparlanma_sn` | Yumruk 0,15 · Kalkan 0,45 · Kılıç 0,20 · Çekiç 0,50 · Küre 0,40 · Kitap 0,10 · Yay 0,20 · Asa 0,20 · Tılsım 0,45 · Top 0,35 | `silahlar.md`, K 22:15 (Tılsım) |
| `toplam_sn` (ritim) | Kılıç 0,40 · Küre 0,40 · Tılsım 0,45 · Asa 0,50 · Yumruk 0,55 · Yay 0,55 · Kitap 0,70 · Kalkan 0,75 · Top 0,75 · Çekiç 1,05 | `silahlar.md` |
| `hasar_carpani` | toplam / 0,55: Kılıç 0,73 · Küre 0,73 · Tılsım 0,82 · Asa 0,91 · Yumruk 1,00 (3 parçaya bölünür) · Yay 1,00 · Kitap 1,27 · Kalkan 1,36 · Top 1,36 · Çekiç 1,91; sıfat şarjı girmez | K 22:15, `silahlar.md` |
| `yay_germe` | otomatik 0,35 s (= Yay `on_sure_sn`); erken bırakma yok | K 22:15 |
| `toparlanma_yurume_carpani` | 1,0 (Küre 0,5 ≈ "0,4 s yavaşlama") | K 21:10b (ritim; silah kanunu değil) |

**K02 `BedelOde`** — Fedakarlık bedeli.
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `can_orani` | mevcut canın 0,30'u | 1.5 (K 11:17) |
| `ayni_hedef_bekleme_sn` | 3 | 1.5 |
| `boss_bekleme_sn` | 60, kişi başı | 1.5 (K 11:46-2) |
| bayraklar | `azalma_yok`, `irade_yok`, `delme` (yalnız Zarar: kalkan + araya giren + yansıma), `durus_kirilmaz` (Koruma), `kaynak_siniri_uyar` (Güçlendirme: 2 kaynak sınırına uyar, K 22:15), `hareket_kesilmez` (Hareket) | 1-8, 4-8, 8-8, 3-8; `kural-ekleri-dongu` E9 |


**K29 `MenzileYuru`** — hedef menzil dışındaysa karakter menzile kadar yürür, sonra skill atılır (K 21:17).
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `hedef` | otomatik / seçilen canlı | katman 1 |
| `menzil_m` | silah menzili (dostta min(silah, 6 m)) | 1.3 |
| `yurume_hizi` | temel yürüyüş (atılma değil) | — |
| `iptal` | yürürken geçerli hedef yok olursa skill boşa gider; süreyle bitmez | K 21:17 |
| `not` | Güdümlü kovalamasından ayrıdır (K13); skillshot değildir | K 21:17 |

#### 2.2 Gövde (teslim) komutları

**K03 `YakinVurus`** — silah temasıyla vuruş (yakın silahlar).
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `menzil_m` | silah menzili | 1.3 |
| `yay_derece` | 90 | [ÖNERİ] |
| `parca_sayisi` | Yumruk 3 (0,30 s içinde), diğerleri 1; toplam hasar değişmez | `silahlar.md` |
| `duvardan_gecer` | hayır | [ÖNERİ], KG-37 |

**K04 `MermiFirlat`** — varlık (1.16): uçar, ilk değdiği canlıya ya da yapıya çarpar; duvar tutar; araya giren alır; Yansıma geri yollar.
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `hiz_mps` | 30 | [ÖNERİ] |
| `yaricap_m` | 0,25 | [ÖNERİ] |
| `menzil_m` | silah menzili | 1.3 |
| `gudumlu_hedef` | yok / kilitli hedef (Güdümlü: hedefi kovalar, durmaz) | 3.2 |
| `yansitilabilir`, `tutulabilir` | evet, evet (Fedakarlık-Zarar: araya giren ve yansıtma yok sayılır) | 1.16, 1-8, KG-36 |

**K05 `AlanAc`** — varlık değil (1.16): yerde bölge, içindeki herkese işler, duvar tutmaz, araya girilemez; yön merkezden hedefe.
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `sekil` | `daire` (Yayılan halka: taban r=4, `min(4, menzil)` — K 21:10 ayar bayrağı) · `hat` (Çizgisel: uzunluk = silah menzili, genişlik 1; skillshot) · `nokta` (tek hedef) · `koni` (Küre fışkırması [ÖNERİ]) | 3.2, K 21:10 |
| `merkez` | sen / seçilen nokta / Yayılan K 18:42 kuralı / kilitli hedef (Güdümlü: hedefle yürür; kendi süresi yok, 8 s tavanı geçerli (K 22:54), K 17:42) | 3.2 |
| `en_cok_hedef` | 5 | 3.2 |
| `carpan` | Yayılan ×0,85 · Çizgisel ×0,85 | 3.2 |
| `gecikme_sn` | Tılsım 0,5 (ayağa düşer, sonra açılır) | K 18:39 |
| `omur_sn` | 0 (anlık); Yansıma hattı 2 | 3.2 |
| `menzille_kisil` | evet (ayar bayrağı; halka/hat iç ölçüleri) | K 21:10 |

**K06 `Sek`** — Sıçrayan zinciri.
| `ilk_carpan` 1,0 · `sekme_sayisi` 2 · `sekme_carpani` 0,5 · `arama_yaricapi_m` min(6, menzil) (K 21:10 ayar bayrağı) · `ayni_hedefe_tekrar` hayır · uygun hedef yoksa ve fiil yapıya işliyorsa yapıya seker (yoksa boşa) · `menzille_kisil` evet | 3.2, E30, K 21:10 |
|---|---|

**K07 `YorungeKur`** — Yörüngeli parçaları.
| `tasiyici` sen / seçilen (düşman yalnız düşman fiilinde) · `yaricap_m` taşıyanın gövde yarıçapı + 2 (K 22:15) · `parca` 3 · `omur_sn` 3 · `degme_bekleme_sn` 1 (Kontrol ve Zaman 3) · `deger_carpani` 0,35 · `mermi_tutar` Koruma'da evet, alanı tutmaz · boss'a değme: irade harcamaz, denge ×0,35 | 3.2, 4-12, 1.6 |
|---|---|

**K08 `YapiKur`** — yalnız Sabit (1.12).
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `tur` | `duvar` (Koruma, Yansıma) 4 × 2 × 0,5 m · `direk` (diğer 10) 1 m taban | 1.12 ÖNERİ [test] |
| `is_alani_m` | 3 | 3.2 |
| `omur_sn` | 6 | 3.2 |
| `can` | 300 | [ÖNERİ] (kaynakta sayı yok) |
| `nabiz_sn` | 1 (Zarar, Şifa, Arındırma); Kuvvet 2 | 1-4, 2-4, 9-4, 5-4 |
| `kisi_siniri` / `takim_siniri` | 5 / 25; aşınca en eskisi söner | 1.12 (K 11:17) |
| `yikilinca` | üstündeki işaret düşer, tasma aynı karede kopar | 1.12, 1.7 |

**K09 `IsaretKoy`** — yalnız İşaretli-Arındırma (9-9).
| `koyma_menzili` silah menzili, dosta min(silah, 6 m) · `omur_sn` 20 · `kisi_basi` 4 (5. en eskiyi siler) · `tasiyici` dost / düşman (boss dahil) / zemin; canlıyla yürür · `kullanma_menzili_m` 25 (takım) · kullanılınca silinmez | 1.10 |
|---|---|

**K10 `ZincirKullan`** — İşaretli zincir (kendi işaretlerin, konuluş sırası 1→2→3→4).
| `genislik_m` 1 · `komsu_arasi_en_cok_m` 12 · `omur_sn` 4 · `en_cok_hedef` 5 · `carpan` 0,85 · değmeler azalmaya girmez · zincir seçimi: en yakın kendi zincirin, basılı tutarak başkasınınki | 1.10 (K 17:33, 17:35) |
|---|---|

**K11 `KapanKur`** — Tetikli'nin işareti (ad önerisi "kapan", A-23).
| `tetikler` saldırı başlatma, skill/büyü başlatma, atılma (ilk kare); yürüme/dönme/durma tetiklemez · `bekleme_sn` 5 (içinde tetiklenmezse **boşa gider**; yedek/kendiliğinden işleme yok — K 21:15/21:17) · `carpan` 1,0 · Koruma'da tetik = sana gelen bir sonraki darbe | 3.2, 4-7, K 21:15 |
|---|---|

**K12 `YukBindir`** — Ortak.
| `alici` en yakın dost / seçilen · `bekleme_sn` 5 (dost skill atmazsa **boşa gider**; yedek yok — K 21:15/21:17; eski ×0,5 yedeği düştü) · oyuncu başına 1 yük · Ortak yükü başka Ortak'a binmez · binince: dostun skillinin şekli ve hedefleriyle, taraf kuralı süzer · alıcı yoksa skill boşa gider | 3.2 (K 17:05), K 21:15 |
|---|---|

**K13 `Kilitlen`** — Güdümlü (K 17:42 esas).
| `kilit_menzili` silah menzili · `kilitli_ilk_sn` 3 (yön değiştiremez, skill çizemez; hafif kontrol kesemez) · `bitis` hedefe vurunca / dünya durdurunca (duvar, araya giren, Yansıma) / 3 s'den sonra yeni skill çizilince (kilit bozulur) / **hedef ölür ya da kaybolursa** (K 22:15) · kendi süresi yok; `kilit_tavan_sn` 8 (bütün Güdümlü kilitleri, düşman dahil, en çok 8 s; K 22:15, genelleştirme K 22:54) · temel vuruş ve dodge kilidi bozmaz (K 22:15) · asla ıskalamaz (skillshot değil) · Zarar'da `kilit_vurusu` 3, ara 0,3 s, çarpanlar 0,6 / 0,6 / 1,0 · Kuvvet'te 1 m/s sürekli itme; sürüklemenin tamamı **tek darbe** sayılır, saniyelik ayrı itme yok (K 22:37) · menzile kovalama K29'dan ayrıdır | 3.2 (K 17:21, 17:40–17:42), 1-6, 5-6 |
|---|---|

**K14 `YardimciCagir`** — Çağırma.
| `sayi` 1 (Yayılan 3 küçük) · `omur_sn` 5 (Yoğun 2,5) · `guc_carpani` 1,0 (Yoğun 1,35) · `davranis` serbest AI / sekerek (Sıçrayan) / hat hücumu (Çizgisel) / yörünge (Yörüngeli) / kule = yapı (Sabit) / kilitli (Güdümlü) · yalnız düşmana saldırır · `hedef_alinabilir` **evet** · `cani_var` **evet** (varlık; düşmanlar vurabilir — K 21:12) · can [ÖNERİ, ayar] | §2, x-11 hücreleri; K 21:12 |
|---|---|

#### 2.3 Etki komutları

**K15 `HasarVer`** — `miktar` = `base_damage` × `hasar_carpani` (silah, K01; K 22:15) × çarpan · `denge` = `base_poise` × çarpan · azalmaya **girmez** (K 16:36) · dost ve dost yapıya zarar yok · `delme` (Fedakarlık-Zarar) · kaynak: 1.4, 2.

**K16 `SifaVer`** — `miktar` = `base_heal` × çarpan · dost yapıyı da onarır · `tasma_kalkana` yalnız Yoğun (5 s) · azalma: 3 s içinde 2. ×0,5, 3. ×0,25 · kaynak: 2-1, 1.4.

**K17 `DurusAc`** — Koruma (2.1): yalnız sen, kalkan yok, durdurulan zarar kimseye geçmez. Darbe sayımı (K 22:15): bir vuruş bir darbe; Yumruk serisi ve boss atış dizisi tek darbe; alanın her tiki ayrı darbe. Yapının (ör. Sabit sütun) ve dönen parçanın (Yörüngeli) her vuruşu yeni bir darbedir; skillin kendi tekrarı olduğu için azalma uygulanmaz (K 22:54, K 11:46-3).
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `sure_sn` | Yoğun 0,45 · Yayılan 2 · Çizgisel 3 · Odaklı / Fedakarlık / İşaretli 2,5 · Sıçrayan 3 · Tetikli 5'e kadar bekler | 5.4 |
| `yon_maskesi` | 360° · önden koridor (1 m × menzil) · tek yön (en çok vuran / işaret) | 5.4 |
| `azaltma` | 1,0 tam · 0,5 · Sıçrayan: ilk tam + 2 × 0,5 · İşaretli zincir 0,85 | 5.4 |
| `darbe_siniri` | Yoğun 1 · Tetikli 1 | 5.4 |
| `kirilir` | kontrol, kökleme, sersemletme, denge bozma kırar; Fedakarlık'ta kırılmaz | 2.1 |
| `alan_yonu` | alan hasarının yönü merkezden sana | 1.16 |

**K18 `It`** — Kuvvet (1.8): hep kullanıcıdan dışa, çekme yok.
| `mesafe_m` 3 (Yoğun 4,05 = 3 × 1,35; Yayılan/Çizgisel 2,55; sekme 1,5; Yörüngeli 1) · `menzille_kisil` **hayır**: itme mesafesi silah menziliyle kısalmaz, menzil yalnız erişimi kısaltır, 5-1 dahil, istisna yok (K 22:17) · alan taşıyorsa yalnız ilk değişte iter (K 22:15) · `itilme_sn` 0,15 [ÖNERİ, KG-7] · `yon` kullanıcı konumundan hedefe; 0,2 m'den yakınsa bakış yönü · yalnız hafif itilir; boss denge alır · `carpma` itilen bir şeye değerse çarpma hasarı (birim yok → A-17) · zıt itmeler [ÖNERİ: vektör toplamı, KG-9] | 1.8, 5.5 |
|---|---|

**K19 `KontrolUygula`** — çeşit hedefin o anki işinden (1.7).
| `sure_sn` 1,5 (Yoğun 0,75; sekme 0,75) · çeşitler: susturma, kökleme, korkutma, yavaşlatma (−%50 yürüme ve saldırı hızı), sersemletme (sürenin yarısı) · öncelik: saldırı/büyü > atılma > hücum > yürüme > boşta (ÖNERİ, 1.7) · boss: sersemletme/kökleme/korkutma irade halkasından; susturma/yavaşlatma yarı güç; küçük kontrol irade harcamaz, denge verir · azalma tablosu geçerli | 1.6, 1.7 |
|---|---|

**K20 `TasmaBagla`** — Kontrol bağı (K 15:46).
| `sure_sn` 1,5 (sıfat süre çarpanı işler; Güdümlü'de kilit boyunca) · `uzunluk` kuruluştaki mesafe, en çok silah menzili · `uc` Kontrol'ü kullanan oyuncu; Sabit tuzağında yapı · geren durur · boss: çapalıya bağlıysa çivilenir 4 s (irade açık) / dirençliyken denge ×1,5; oyuncuya bağlıysa boss geren oyuncuyu sürükler · aynı düşmana iki tasma → KG-6 | 1.7 |
|---|---|

**K21 `DurumUygula`** — süreli değer etkisi: Zayıflatma, Güçlendirme, element durumu.
| Zayıflatma −%20, 4 s · Güçlendirme +%20 etki, +%15 hız (hız tavanı ×1,5), 4 s, en çok 2 kaynak (1 arkadaş + 1 kendin; Fedakarlık 3. olabilir) · yeniden değme süreyi tazeler · azalma: 3 s içinde 2. ×0,5, 3. ×0,25 · element: tek küçük durum (yanık / yenilenme / hız / kalkan / arınma / zayıflatma) | §2, 1.4, 1.13 |
|---|---|

**K22 `EtkiSok`** — Arındırma: yalnız dosttan zararlı.
| `adet` 1 (Yoğun 2–3 [test]) · `sira` en son konan · düşman mermisini ve alanını silmez, alanın dosta verdiği durumu söker · kısmi sökme (×0,5, ×0,85) → A-22 | §2, 5.9 |
|---|---|

**K23 `Yansit`** — Yansıma: gelen zararlı etkiyi (zarar, kontrol, zayıflatma) kaynağına döndürür.
| `sure_sn` 2 (Yoğun 0,45) · `oran` 0,85 (Yoğun 0,85 × 1,35, en çok 1) (K 22:15) · `boss_tek_atma_tavani` dönen hasar en çok boss azami canının %5'i [ayar] (boss'un tek atan büyük vuruşu; Yoğun da aşamaz) (K 22:15) · `kapsam` varsayılan **sen**; basılı tutup sürüklenirse seçilen **dost** (K 21:09); Odaklı: en çok hasar alan dost; hat / ayna duvarı / yörünge parçası · mermi ve yakın vuruş döner; alan dönmez (KG-35) · Sıçrayan: döndükten sonra 2 düşmana ×0,5 seker · Fedakarlık: azalma yok, boss kontrolü irade halkasını aşar | §2, 5.10, K 21:09 |
|---|---|

**K24 `KendiniTasi`** — Hareket, yol alarak (1.9).
| Parametre | Başlangıç | Kaynak |
|---|---|---|
| `en_cok_mesafe_m` | silah menzili | 1.3 |
| `hiz_mps` | Yumruk 30 · Top 30 · Kılıç 25 · Küre 25 · Çekiç 22 · Kitap 18 · Tılsım 16 (0,5 s gecikmeyle) · Yay 14 · Kalkan 12 · Asa 10 | K 21:11 (Yumruk Top ile eşit; Kılıç>Yumruk reddedildi) |
| `yolda_skill` | Yay, Asa, Tılsım evet; diğerleri hayır | 4.3 |
| `durdurucular` | **bütün silahlarda aynı** (K 21:07): duvar/yapı hızı 0'a indirir; yolda hasar alır; mermiye araya girer | K 21:07 |
| `hiz_devri` | yeni kendini taşıma süreni keser; eski hızın 0,5'i aktarılır; toplam ≤ 1,5 × temel | K 15:53 |
| `kenarda_fren` | Yoğun: hedefin gövde kenarında frenler; canlılar birbirinin içinden geçmez; başka bir canlının kenarı durdurucudur | 3-1, K 22:15 |
| `agirlik` | hafif ağıra / çapalıya gider; işarette çekilme | 1.8, 3-9 |

**K25 `Isinla`** — yalnız Çizgisel-Hareket (K 15:55).
| `en_cok_mesafe_m` silah menzili · hedefte biter, hedefi geçmez (E29) · yol yok, hiçbir şeye değmez · varış hızı 0, hız almaz/aktarmaz | 1.9, 3-10 |
|---|---|

**K26 `KayitAl`** — Zaman kaydı (fizik katmanındaki halka tamponu kullanır).
| `ornek_araligi_sn` 0,1 [ÖNERİ] · `tampon_sn` 20 (işaret ömrü kadar) · kaydedilen: can + konum (etkiler hayır) · anı: varsayılan 3 s önce; Yoğun 1,5 s; Sabit direk dikildiği an; İşaretli işaret konduğu an | 1.11 |
|---|---|

**K27 `KaydaDon`** — Zaman dönüşü.
| `konum` her zaman tam · `can` = (kayıttaki − şimdiki) × çarpan, kayıttakini aşamaz · `dirilt` yalnız kayıt direği (kayıttan sonra ölen; A-9) · varış hızı 0, ışınlanma sayılmaz · yalnız dosta / sana | 1.11, 12-4 |
|---|---|

**K28 `DengeVer`** — denge (poise) hasarı; ağır hedefte itme, küçük kontrol ve dirençli iradenin karşılığı.
| `miktar` = `base_poise` × çarpan · itmede boss: ×1,0 (Yoğun 1,35; Yayılan 0,85; Yörüngeli 0,35/değme) · dirençli iradede tam kontrol yerine ×1,5 · sendelemede boss her şeyi tam yer (KG-28) | 1.6, 1.8 |
|---|---|

**Komut sayısı: 29** (zamanlama/bedel/yürüme 3, gövde 12, etki 14).

---

### 3. Hangi fiil ve sıfat hangi komutu kullanır

#### 3.1 Fiil → etki komutu
| Fiil | Etki komutu | Ek |
|---|---|---|
| 1 Zarar | K15 `HasarVer` | + K28 denge |
| 2 Şifa | K16 `SifaVer` | |
| 3 Hareket | K24 `KendiniTasi` (Çizgisel'de K25 `Isinla`) | |
| 4 Koruma | K17 `DurusAc` (Sabit'te K08 duvar) | |
| 5 Kuvvet | K18 `It` | + K28 (boss) |
| 6 Kontrol | K19 `KontrolUygula` + K20 `TasmaBagla` | + K28 (boss küçük kontrol) |
| 7 Zayıflatma | K21 `DurumUygula` (−) | |
| 8 Güçlendirme | K21 `DurumUygula` (+) | |
| 9 Arındırma | K22 `EtkiSok` (İşaretli'de K09 `IsaretKoy`, fiil işlemez) | |
| 10 Yansıma | K23 `Yansit` | |
| 11 Çağırma | K14 `YardimciCagir` | yardımcının vuruşu K15 |
| 12 Zaman | K26 `KayitAl` + K27 `KaydaDon` | |

#### 3.2 Sıfat → gövde komutu
| Sıfat | Gövde komutu | Ek |
|---|---|---|
| 1 Yoğun | silahın teslimi (K03/K04/K05 nokta) | K01 şarj 0,35 (düşman fiili); ×1,35; süre yarı |
| 2 Odaklı | silahın teslimi | yalnız otomatik hedef ölçüsü (katman 1) |
| 3 Sıçrayan | K06 `Sek` | |
| 4 Sabit | K08 `YapiKur` | |
| 5 Yayılan | K05 `AlanAc(daire)` | |
| 6 Güdümlü | K13 `Kilitlen` + silahın teslimi (yakında sen koşarsın → K24; uzakta K04 kovalar) | |
| 7 Tetikli | K11 `KapanKur` | |
| 8 Fedakarlık | K02 `BedelOde` + silahın teslimi | |
| 9 İşaretli | K10 `ZincirKullan` ya da tek işaretten silahın teslimi; Arındırma'da K09 | |
| 10 Çizgisel | K05 `AlanAc(hat)` (Hareket'te K25) | |
| 11 Ortak | K12 `YukBindir` | |
| 12 Yörüngeli | K07 `YorungeKur` | |

#### 3.3 Fiil × komut kullanım özeti (kaç sıfatta)
Her fiil 12 sıfatın hepsinde kendi etki komutunu kullanır (3.1). Gövde komutlarının kullanım sayısı (144 kombo üzerinden): `AlanAc` 23 (Yayılan 12 + Çizgisel 11; Çizgisel-Hareket ışınlanır) + Tılsım teslimleri; `Sek` 12; `YorungeKur` 12; `YapiKur` 12; `KapanKur` 12; `YukBindir` 12; `Kilitlen` 12; `BedelOde` 12; `ZincirKullan` ≤ 11; `IsaretKoy` 1; `Isinla` 1. Silah teslimi (`YakinVurus` / `MermiFirlat` / `AlanAc`) Yoğun, Odaklı, Fedakarlık, Güdümlü ve tek işaretli İşaretli'de doğrudan kullanılır. `MenzileYuru` (K29) hedef menzil dışındayken plana eklenir (Güdümlü kovalaması hariç; o K13'tedir).

---

### 4. Fizik katmanı (katman 3)

#### 4.1 Servisler (komut değil, komutların çağırdığı kurallar)
| Servis | Kural | Başlangıç | Kaynak |
|---|---|---|---|
| Ağırlık | Hafif (oyuncu, küçük düşman, mermi) · Ağır (boss) · Çapalı (zemin, yapı, zemin işareti). Hafif ağıra gider; ağır/çapalı 0 m | — | 1.8 |
| Çarpışma | mermi (varlık) / alan (bölge) / yakın vuruş; duvar mermiyi tutar, alanı tutmaz; yol alan beden bütün silahlarda hasar alır ve araya girer (K 21:07) | — | 1.16, K 21:05, K 21:07 |
| Konum önceliği | tutma > süreli etki > itme | — | 1.8 |
| Azalma | aynı tür (= aynı fiil) etki aynı hedefe 3 s içinde: 2. ×0,5, 3. ×0,25; çarpan etkinin kendi ölçüsüne uygulanır (Kontrol süre, Kuvvet mesafe, Zayıflatma/Güçlendirme yüzde); hasar girmez; bir skillin kendi tikleri girmez; Güçlendirme ile Zayıflatma aynı kişide toplanır | 3 s | 1.4, K 22:15 |
| Darbe | bir vuruş bir darbe; Yumruk serisi ve boss atış dizisi tek darbe; alan tiki Koruma için ayrı darbe, itme gibi etkiler alanın yalnız ilk değişinde | — | K 22:15 |
| Gövde | canlılar birbirinin içinden geçmez, kenarda durur; yarıçap [ayar]: oyuncu 0,35 m · boss 2,5 m · yaratık 0,5 m · yapı 1 m; bütün mesafeler gövde kenarından ölçülür | — | K 22:15 |
| İrade halkası | Açık: ilk tam kontrol işler, hak harcanır; sonra 45 s Dirençli (tam kontrol → denge ×1,5) | 45 s | 1.6 |
| Kayıt tamponu | her canlı için can + konum halka tamponu | 0,1 s × 20 s [ÖNERİ] | 1.11 |
| Yapı sayacı | kişi 5, takım 25 | — | 1.12 |
| Dünya saati | tek; slow-mo yok; hitstop yalnız görüntü | — | 1.14 |

#### 4.2 Unity uygulama önerisi [ÖNERİ]
- **Sabit adım:** 30 Hz mantık adımı (`FixedUpdate`), sunucu yetkili; istemci yalnız görüntüyü ara değerler.
- **Hareket:** kinematik (`CharacterController` ya da elle `MovePosition`); kuvvet tabanlı Rigidbody yok, çünkü itme mesafesi ve süresi kural sayısıdır.
- **Mermi:** her adımda `SphereCast` (önceki → yeni konum), ilk isabet: canlı ya da yapı. Havuzlanmış nesneler.
- **Alan:** `OverlapSphere` / `OverlapBox` + açı süzgeci; duvar sorgusu yapılmaz.
- **Yakın vuruş:** teslim karesinde `OverlapSphere` + yay süzgeci (duvar → KG-37).
- **Katmanlar:** `Oyuncu`, `Dusman`, `Boss`, `Yapi`, `Mermi`, `Zemin`; dost ateşi süzgeci taraf kuralından.
- **Veri:** `komut-ayarlari.json` (bu belgedeki bütün sayılar), `silahlar.json` (menzil, ritim, yolculuk), `kural-tablolari.json` (Tablo F/S). Kod sayı içermez.

---

### 5. Zayıf yerler ve emin olmadığım noktalar
- **Teslim sınıfı (K 21:05) karar:** Yumruk/Kılıç/Çekiç/Kalkan yakın; Yay/Top/Kitap mermi; Asa ışını / Küre konisi / Tılsım mühür alan. Sıfat şeklinin (Çizgisel hat, Sıçrayan sekme, zincir…) hangi sınıfa girdiği hâlâ açık (`dunya-fizigi` AÇ-31).
- **Mermi hızı, yarıçapı, yakın vuruş yayı, yapı canı, yaratık canı, itilme süresi, kayıt örnek aralığı** kaynakta yok; hepsi [ÖNERİ].
- **`DurumUygula` tek komutta** Zayıflatma, Güçlendirme ve elementi birleştiriyor. Element "kalkan" durumu kalkan nesnesi ister (AS-37); ayrı komut gerekebilir (o zaman 30).
- **`KendiniTasi` durdurucuları** K 21:07 ile bütün silahlara bağlandı (hasar, araya girme, duvar).
- **Ortak yükü** dostun skillinin her komutuna "binme" demek; komut planına sonradan eklenen bir kanca ister (`dunya-fizigi` AÇ-45 / KG-45).
- **Güdümlü** K 17:42 esas (kendi süresi yok, 8 s tavanı geçerli (K 22:54); ilk 3 s kilit; sonra skill kilidi bozar). Alan/tasma süresi ve "ezilir" birimi hâlâ açık (AÇ-46, AÇ-8).
- **MenzileYuru vs Güdümlü kovalama** ayrı komutlar; yürürken iptal / bekleme süresi etkileşimi açık (`dunya-fizigi` AÇ-R10).
- 29 komut sayısı tasarım tercihidir: `DengeVer` ve `KayitAl` ayrı; `MenzileYuru` eklendi.

---

<a id="b4"></a>
