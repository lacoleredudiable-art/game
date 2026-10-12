# Bölüm 4: Silahlar

*Kaynak: `karakterler/v4/silahlar.md`*

## Silahlar (v4 taslağı)

Hazırlayan: GameDesign, 9 Ekim 2026. Silahlar yeniden tasarlanacak; bu dosya bugünkü 10 silahla kural şablonunu doldurur.

### Kural
- **Fiil** ne olacağını, **sıfat** şekli (yön, hedef, alan, süre, biçim) belirler.
- **Silah** üç şeyi belirler:
  1. **Menzil (m):** skillin ulaşabileceği en uzak mesafe. Sıfat şeklini bu menzilin içinde çizer. (Kullanıcı kararı, 9 Ekim 15:41: yumruktan uzun menzil inandırıcı değil.)
  2. **Dövüş tarzı:** her fiili kendi hareketiyle yapar (tarz satırları `silah-fiil-120.md`; Kalkan ağır adım ve kenarla indirme, omuz hücumu yalnız Hareket'te; Çekiç kaldır-bekle-çak, tepede döndürme yalnız Koruma duruşunda; Asa büyü duruşu (yalnız kendi duruşu, kalkan vermez), Yumruk guard ve kroşe/aparkat, Kılıç samuray).
  3. **Ritim:** ön süre şekli (şarj, gecikme, seri, kanal). Toplam süre ve hasar sıfatın bütçesinde kalır.
- Silah fiilin tarafını değiştirmez ve rol vermez; rol fiilden gelir.
- İşaretten çıkan skillde oyuncu hareketi kendi yerinde yapar, hareketin kordan yankısı işarette belirir. Menzil işarete kadar olan mesafeye uygulanır, işaretten sonrası sıfatın şekline aittir.

### Menzil tablosu
Kaynak: `element-sistemi.json` içindeki `range_mult` × 6 m (v3 kural kitabındaki varsayılan erişim). Sayılar test değeri, dengelemede ayarlanır.

*Not (AS-05): Bu tablo yalnız menzil kaynağıdır. Dövüş tarzı için `silah-fiil-120.md` Tarz satırları, ritim için aşağıdaki ritim tablosu geçerlidir; buradaki tarz/ritim sütunları eski kısa özettir. Kılıç: yerinde iaido, kınsız (K 21:05); Küre yavaşlığı ritim değeridir (K 21:10b); Kalkan VUR = ağır adım + kenarla indirme, omuz hücumu Hareket'te (K 21:05).*

| # | Silah | range_mult | Menzil (m) | Dövüş tarzı (eski özet) | Ritim (eski özet) |
|---|---|---|---|---|---|
| 1 | Yumruk | 0,5 | 3 | Boksör: guard, kroşe, aparkat, yan adım | Seri: aynı süre içinde birkaç vuruş |
| 2 | Yay | 2,35 | 14 | Okçu: ger, nişan al, bırak | Otomatik 0,35 s germe |
| 3 | Büyü Kitabı | 1,53 | 9 | Okuyan: kitabı açar, sözü yüksek sesle okur, harfler hedefe uçar | Yürürken okuma: cast uzun ama oyuncu hareket edebilir |
| 4 | Kılıç | 1,0 | 6 | Samuray: yerinde iaido (kınsız), tek temiz kesiş; atılış Hareket'te | Hızlı tek kesiş (0,20 + 0,20 s) |
| 5 | Küre | 1,18 | 7 | Kor sıkan: küreyi avuçta ezer, ışık anında patlar | Anında çıkış; toparlanmada yürüme ×0,5 (ritim, K 21:10b) |
| 6 | Çekiç | 1,0 | 6 | Ağır savaşçı (Gimli/Thor): kaldır, bekle, çak | Uzun kaldırma (0,55 + 0,50 s) |
| 7 | Top | 3,0 | 18 | Topçu: nişan, ateş, düştüğü yerde açılma | Balistik gecikme |
| 8 | Asa | 2,5 | 15 | Büyücü: büyü duruşu, ışın, rün asma | Kısa kanal |
| 9 | Tılsım | 2,8 | 17 | Mühürcü: avuçtan mühür, hedefin ayağına düşer | 0,5 s sonra açılır |
| 10 | Kalkan | 0,5 | 3 | Muhafız: kalkanı öne koyma, kenarla indirme; omuz hücumu Hareket'te | Ağır adım + indirme |

### Kullanıcı kararları (9 Ekim 15:42)
- Kitap ve Küre yeniden tasarlandı: eski ritimleri Tetikli'nin ve İşaretli/Sabit'in işini kopyalıyordu. Yeni ritimleri sıfatın beş sorusuna dokunmuyor.
- Dost etkisinde menzil = en küçüğü (silah menzili, 6 m).
- Hareket'te menzil = en fazla yer değiştirme.

### Açık (A)
- **Tılsım ve mermi varlığı (çözüldü, A 20:36):** mühür hedefin ayağına düşer ve 0,5 s sonra açılır; hedefte belirme yok.


### Ritim ölçüleri (başlangıç değerleri, 9 Ekim 20:29 kararı)
Kullanıcı kararı: silah = menzil + ritim + oynanış tarzı (animasyon); mekanik kanun yok; aynı menzildeki silahları ritim ayırır. Ritim yön, hedef, süre ve şekil değiştirmez. Hepsi test değeri.

- **Ön süre:** dokunuştan etkinin çıkışına kadar geçen süre (silahın kendi payı).
- **Vuruş:** teslimin kaç parçaya bölündüğü; toplam hasar değişmez.
- **Toparlanma:** etki çıktıktan sonra bir sonraki skill ya da dodge'a kadar geçen süre.
- **Ön sürede hareket:** oyuncu ön süre boyunca yürüyebilir mi.
- **Saldırı hızı:** ön süre + toparlanma; dakikadaki üst sınır kabaca 60 / bu toplam.

| Silah | Menzil | Ön süre (s) | Vuruş | Toparlanma (s) | Ön sürede hareket | Toplam (s) | Not |
|---|---|---|---|---|---|---|---|
| Yumruk | 3 m | 0,10 | 3 vuruş, 0,30 s içinde | 0,15 | Evet | 0,55 | En kısa ön süre; seri |
| Kalkan | 3 m | 0,30 | 1 | 0,45 | Ağır adım (Hareket sayılmaz) | 0,75 | Ağır adım |
| Kılıç | 6 m | 0,20 | 1 | 0,20 | Hayır | 0,40 | Hızlı tek kesiş |
| Çekiç | 6 m | 0,55 | 1 | 0,50 | Hayır | 1,05 | En yavaş; uzun kaldırma |
| Küre | 7 m | 0,00 | 1 | 0,40 (bu sürede yürüme ×0,5, K 21:10b) | — | 0,40 | Anında çıkış, sonra bedel |
| Kitap | 9 m | 0,60 | 1 | 0,10 | Evet (okurken yürür) | 0,70 | Uzun okuma, kısa toparlanma |
| Yay | 14 m | 0,35 (otomatik germe) | 1 | 0,20 | Hayır | 0,55 | Germe otomatik; erken bırakma yok (K 22:15) |
| Asa | 15 m | 0,30 (akıtma) | 1 | 0,20 | Hayır | 0,50 | Kısa akıtma |
| Tılsım | 17 m | 0,00 | 1 | 0,45 | — | 0,45 | Gecikme mühürde (0,5 s), ön sürede değil; toparlanma 0,15→0,45 (K 22:15) |
| Top | 18 m | 0,40 (nişan) | 1 | 0,35 | Hayır | 0,75 | Geri tepme yalnız görüntü |

**Çift ayrımı:**
- **Kılıç–Çekiç (ikisi 6 m):** Kılıç 0,20 + 0,20; Çekiç 0,55 + 0,50. Çekiç'in ön süresi 2,75 katı, toplamı 2,6 katı. Kılıç ritmi "hemen kes, hemen devam", Çekiç'inki "kaldır, bekle, çak, toparlan".
- **Yumruk–Kalkan (ikisi 3 m):** Yumruk 0,10 ön süre, 3 parçalı seri, yürürken; Kalkan 0,30 ağır adım, tek darbe, 0,45 toparlanma. Yumruk 0,55 s'de bir, Kalkan 0,75 s'de bir.

**Sıfatla birleşme:** sıfatın kendi ön süresi (ör. Yoğun şarjı 0,35 s) silahın ön süresine eklenir; toplam ön süre tavanı test değeri 1,0 s.

#### Yolculuk hızı ve yolda skill (Hareket ritmi, 9 Ekim 20:36)
Yolculuk hızı yalnız hız; yol, yön ve mesafe sıfattan gelir. Işınlanma yok. İlk beşi K 20:35, son beşi K 21:11. Hız değerleri test değeri.

| Silah | Yolculuk hızı | Yolda skill | Kaynak |
|---|---|---|---|
| Top | Çok hızlı, ~30 m/s (gülle) | Yok | Kullanıcı |
| Yay | Yavaş, ~14 m/s (ipte süzülme) | Var (silah değiştirme de) | Kullanıcı |
| Çekiç | Hızlı, ~22 m/s (Thor) | Yok; varışta yere çakar (görünüm) | Kullanıcı |
| Asa | En yavaş, ~10 m/s (ışında süzülme) | Var | Kullanıcı |
| Küre | Hızlı, ~25 m/s (patlama savrulması) | Yok | Kullanıcı |
| Kılıç | Hızlı, ~25 m/s (şimşek atılış) | Yok; çok kısa sürdüğü için gerek yok | K 21:11 (Kılıç>Yumruk reddedildi) |
| Yumruk | Çok hızlı, ~30 m/s (kısa atılış; Top ile eşit) | Yok | K 21:11 |
| Kalkan | Yavaş, ~12 m/s (ağır omuz hücumu) | Yok | K 21:11 |
| Tılsım | Orta, ~16 m/s (mühür çekmesi, 0,5 s sonra) | Var | K 21:11 |
| Kitap | Orta, ~18 m/s (sayfa olarak akış) | Yok; beden dağılmış durumda | K 21:11 |


- **Kitap Hareket (A 20:36):** beden gerçekten yol alır, ışınlanmaz; hasar alır, duvar ya da araya giren bir şey durdurur. Sayfalara dağılma yalnız görünüm.

#### Teslim sınıfı (K 21:05)
- **Yakın vuruş:** Yumruk, Kılıç, Çekiç, Kalkan
- **Mermi:** Yay, Top, Kitap
- **Alan:** Asa, Küre, Tılsım

Kılıç'ta kın yok (K 21:05).

#### Hasar ritimle ölçeklenir (K 22:15)
Taban hasar silahın toplam süresiyle (ön süre + toparlanma) orantılıdır; saniyedeki hasar bütün silahlarda eşittir. Yavaş silah her vuruşta daha sert vurur. Bu bir silah kanunu değil, ritmin sonucu.
Çarpan = silahın toplam süresi / 0,55 (Yumruk ve Yay = 1,00). Başlangıç değerleri:

| Silah | Toplam (s) | Vuruş başı hasar çarpanı |
|---|---|---|
| Kılıç | 0,40 | 0,73 |
| Küre | 0,40 | 0,73 |
| Tılsım | 0,45 | 0,82 |
| Asa | 0,50 | 0,91 |
| Yumruk | 0,55 | 1,00 (3 parçaya bölünür) |
| Yay | 0,55 | 1,00 |
| Kitap | 0,70 | 1,27 |
| Kalkan | 0,75 | 1,36 |
| Top | 0,75 | 1,36 |
| Çekiç | 1,05 | 1,91 |

Sıfatın ön süresi (ör. Yoğun şarjı) bu çarpana girmez; sıfat kendi bütçesini taşır.

#### Menzil ölçümü (K 22:17)
Bütün mesafeler gövde kenarından ölçülür. İtme ölçüsü menzille kısılmaz; menzil yalnız erişimi kısaltır.

---

<a id="b5"></a>
