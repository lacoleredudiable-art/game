# Ajan Görev Listesi — Alfa Prototip

> Sırayla çalıştırılacak, birbirinden bağımsız doğrulanabilir görevler. Her görevin altında
> **kopyala-yapıştır hazır prompt** var; Composer gibi hızlı modellerle çalışacak şekilde
> yazıldı: dar kapsam, net dosya yolları, sayılabilir kabul kriteri, açık yasaklar.
>
> Kaynaklar: [Dövüş Sistemi](dovus-sistemi.md) · [Teknoloji Kararları](teknoloji-kararlari.md)

---

## Nasıl çalıştırılır

1. **Sırayı bozma.** Her görev, kendinden öncekilerin bittiğini varsayar.
2. **Her göreve ayrı ajan, ayrı dal.** Görev bitmeden sonrakini başlatma. Ajan testler
   yeşilse dalı kendi merge eder; PR yalnızca karar gereken bir şey çıktıysa açık kalır.
3. Ajan bitirdiğinde tek soruyu sor: **"kabul kriterlerinden hangisini doğrulayamadın?"**
   Doğrulanmamış kriter varsa görev bitmemiştir.
4. **Prompt'a ek bilgi yapıştırmana gerek yok.** Değişmez kurallar ve "repoyu tarama"
   talimatı `AGENTS.md`'de ve her ajanın bağlamına otomatik giriyor. Ajan nerede kaldığımızı
   `docs/durum.md`'den öğrenir ve bitirince orayı güncellemekle yükümlüdür.
4. **Faz 1 (T1–T4) Unity gerektirmez** — `dotnet test` ile doğrulanır, en hızlı kısım burası.
   **Faz 2 (T5–T10) için Unity editörü açık ve MCP bağlı olmalı.**
5. Prompt'ların hepsi "önce şu belgeyi oku" ile başlıyor. Bu satırı silme; ajanın bağlamı o.

| Faz | Görevler | Unity gerekli mi | Ne cevaplar |
|---|---|---|---|
| 0 | T0 | insan işi | — |
| 1 | T1–T4 | hayır | dövüş mantığı doğru mu |
| 2 | T5–T10 | evet | his doğru mu |
| 3 | T11 | evet + telefon | [§13'teki sorular](dovus-sistemi.md#13-prototipin-cevapladığı-sorular) |
| 4 | görsellik | — | **henüz başlanmayacak** |

---

## T0 — Unity projesini kur (insan işi)

Ajan yapamaz; Unity Hub gerekiyor.

1. Unity 6 LTS + **Android Build Support** (SDK/NDK dahil) kur
2. Yeni **URP** projesi oluştur, repo kökünde `unity/` klasörüne
3. Project Settings → Editor → **Asset Serialization: Force Text**
4. Unity için standart `.gitignore` ekle (Library, Temp, Obj, Build, Logs)
5. `.NET SDK 8+` kur (`dotnet --version` çalışsın)
6. Commit et: "chore: bos unity 6 urp projesi"

Bittiğinde `unity/Assets`, `unity/ProjectSettings`, `unity/Packages/manifest.json` repoda olmalı.

---

## Faz 1 — Dövüş Mantığı (Unity'siz, testle doğrulanır)

### T1 — Core assembly, ayar veri modeli, dotnet test kancası

**Hedef:** Saf C# katmanının iskeleti ve ajanların Unity açmadan test edebilmesi.

```
Rolün: Unity 6 projesinde saf C# dövüş katmanının iskeletini kuran geliştirici.

ÖNCE OKU: docs/teknoloji-kararlari.md (§3, §6) ve docs/dovus-sistemi.md (§5, §6, §7, §11).

GÖREV
1. unity/Assets/Scripts/Core/Dovus.Core.asmdef oluştur. Kural: bu assembly UnityEngine'e
   ASLA referans vermez. Sadece netstandard uyumlu saf C#.
2. unity/Assets/Scripts/Core/Tuning/ altına ayar veri sınıflarını yaz (POCO, arayüz yok):
   DodgeTuning, SentenceTuning, SlowmoTuning, BossTuning, GradeTuning, FeelTuning.
   Varsayılan değerlerin HEPSİ docs/dovus-sistemi.md §5/§6/§7/§11'deki tablolardan
   birebir alınacak. Sayıyı kendin uydurma; belgede olmayan bir alan gerekiyorsa
   belgeye referansla yorum satırı bırak.
3. Hepsini toplayan CombatTuning sınıfı yaz; salt varsayılanlarla kurulabilir olsun.
4. tools/CoreTests/ altına bir dotnet test projesi (NUnit veya xUnit) kur. Core klasöründeki
   .cs dosyalarını <Compile Include="..."/> ile LINK'le (kopyalama yok).
5. Bir smoke test yaz: CombatTuning varsayılanları belgedeki değerlerle birebir aynı mı.

KABUL KRİTERLERİ
- `cd tools/CoreTests && dotnet test` yeşil geçiyor
- Core klasöründe hiçbir dosyada "using UnityEngine" yok (grep ile doğrula)
- Belgedeki her sayı kodda bir alan olarak mevcut

YASAKLAR
- MonoBehaviour, ScriptableObject, hiçbir Unity tipi kullanma
- Oyun mantığı yazma (bu görev sadece iskelet ve veri)
- unity/Assets dışına, tools/ dışına dosya ekleme

Bitirince `dotnet test` çıktısını özetle ve doğrulayamadığın kriteri açıkça yaz.
```

### T2 — Cümle gramer motoru (sistemin kalbi)

**Hedef:** Nokta dizisini cümlelere çözen, iptal pencerelerini işleten motor.

```
Rolün: Dövüş sisteminin gramer motorunu yazan geliştirici. Bu, projenin en kritik parçası.

ÖNCE OKU: docs/dovus-sistemi.md — §3 (gramer), §4 (rünler), §5 (cümle kuralları).
Tabloları ve "Türetilebilirlik örnekleri" bölümünü satır satır oku.

GÖREV
unity/Assets/Scripts/Core/Grammar/ altına saf C# olarak yaz:
- Rune (5 rün enum'u + hece bilgisi)
- PentagonLayout: hangi nokta hangisine komşu (kısa sıçrama) hangisine uzak (uzun sıçrama)
- SentenceEngine: zamanı parametre olarak alan durum makinesi.
  API kabaca: OnDotTouched(int dot, double worldTimeMs), OnDwell(...), Tick(double dtMs),
  Abort(), ve okunabilir bir Sentence durumu (fiil, sıfatlar, kalan pencere, çözülme).

UYULACAK KURALLAR (belgede tam hali var)
- İlk nokta FİİL, sonrakiler SIFAT. Bu yüzden 5-1 ile 1-5 farklı cümledir.
- Her ön-ek geçerli: fiil dokunulduğu anda başlar, sıfatlar yaşayan etkiyi değiştirir.
- Cümle en fazla 1 fiil + 3 sıfat. Sınırı aşan dokunuş, cümleyi çözer ve YENİ cümle başlatır.
- İptal pencereleri DÜNYA zamanıyla ölçülür: fiil 420ms, 1. sıfat 360ms, 2. sıfat 300ms.
  Pencere kapanırsa cümle kendiliğinden çözülür (kapanış vuruşu üretilir).
- Aynı noktaya dönmek = o kelimeyi tekrar söylemek, bir sıfat yuvası harcar.
- Noktada bekleme (220ms) yoğunlaştırır, yuva harcamaz, en fazla 2 kez.
- Kapanışın TÜRÜ son rüne bağlı; ödül tablosu §5'te (1/2.4/4.4/7.0).
- Abort (dodge veya vurulma) → kapanış YOK, hiçbir ödeme yok.

KABUL KRİTERLERİ (hepsi test olarak yazılacak, tools/CoreTests altına)
1. "5-1" ile "1-5" farklı sonuç üretir
2. "1-2-3-4-5-1-2-3-4-5" tam olarak şu üç cümleye bölünür: (1,2,3,4) / (5,1,2,3) / (4,5…)
3. Pencere dolunca cümle kendiliğinden çözülür ve kapanış üretilir
4. Abort edilen cümle hiçbir ödül üretmez
5. Komşu ve uzak sıçrama doğru sınıflanır (beşgende her noktanın 2 komşusu, 2 uzağı var)
6. Bekletme en fazla 2 kez yoğunlaştırır ve sıfat yuvası harcamaz
7. Ödül toplamları §5 tablosuyla birebir aynı
`cd tools/CoreTests && dotnet test` yeşil olacak.

YASAKLAR
- Unity tipi kullanma, Core saf kalacak
- Görsel/ses/girdi kodu yazma — bu görev sadece mantık
- Kombo tablosu yazma. Hiçbir dizi elle tanımlanmayacak; her şey kuraldan doğacak.
- T1'in ayar sınıflarını değiştirme; eksik alan varsa PR açıklamasında belirt

Bitirince 7 kriterin her biri için hangi testin karşılık geldiğini listele.
```

### T3 — Dodge, boss frame verisi, sıyırma derecelendirmesi

```
Rolün: Dodge ve boss frame verisi mantığını yazan geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md — §6 (dodge ve derecelendirme), §11 (boss).

GÖREV
unity/Assets/Scripts/Core/Combat/ altına saf C# olarak yaz:
- DodgeState: startup/i-frame/süre/glide tail/cooldown; hareket eğrisi s(u)=1-(1-u)^curveExp
  ve süre sonunda sönen artık hız. Konumu değil YER DEĞİŞTİRME oranını döndür (Unity'siz).
- BossAttack: windup/active/recovery frame verisi + etki hacmi testi (mesafe/açı matematiği)
- ExchangeResolver: bossun vuruş anında sonucu çözer.
  gap = vuruş anı - dodge basma anı → derece (MÜKEMMEL ≤110, HARİKA ≤200, TEMİZ ≤320, üstü SIYIRDI)
  reaction = dodge basma anı - telegraf başlangıcı (ekranda gösterilecek sayı)
  Vurulma hâlinde SEBEP döndür: i-frame bitmişse "erken bastın", hiç basılmamışsa "geç kaldın".

KABUL KRİTERLERİ (tools/CoreTests altına test)
1. Her derece eşiği sınır değerlerinde doğru (110/200/320 ms tam sınır dahil)
2. reaction hesabı telegraf başlangıcına göre doğru
3. i-frame penceresi vuruş anını kapsıyorsa sıyırma, kapsamıyorsa vurulma
4. Vurulma sebepleri doğru ayrışıyor (erken bastın / geç kaldın)
5. Dodge yer değiştirme eğrisi monoton artan ve u=1'de tam mesafeye ulaşıyor
`dotnet test` yeşil olacak.

YASAKLAR
- Unity tipi kullanma
- Yavaş çekim mantığı yazma (T4'ün işi), sadece dereceyi döndür
- T2'nin dosyalarını değiştirme

Bitirince eşik testlerinin sınır değerlerini nasıl kurduğunu yaz.
```

### T4 — Zaman yönetmeni (yavaş çekim rampası + hitstop)

```
Rolün: Zaman kontrolü mantığını yazan geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §7 · docs/teknoloji-kararlari.md §5.

GÖREV
unity/Assets/Scripts/Core/Time/TimeDirector.cs — saf C#, Unity'siz.
- İki saat: dünya (ölçeklenmiş) ve gerçek (ölçeklenmemiş). Tick(realDtMs) çağrısı
  scaledDt üretir.
- TriggerSlowmo(factor 0.22, rampDown 55ms, hold 190ms, rampUp 420ms). Rampa yumuşak
  geçişli olacak (ease), çıkış inişten uzun — "yağ gibi dönüş" hissi buradan geliyor.
- TriggerHitstop(ms): kısa süre ölçeği ~0'a çeker, yavaş çekimden önceliklidir.
- Kuyruk davranışı belirli olsun: üst üste gelen tetikler nasıl birleşir, testle sabitle.

KABUL KRİTERLERİ (tools/CoreTests)
1. Rampa iniş/tut/çıkış sürelerinin toplamı beklenen sürede 1.0'a döner
2. Ölçek hiçbir zaman factor'ün altına inmez ve 1.0'ı aşmaz
3. Hitstop yavaş çekim sırasında tetiklenirse hitstop kazanır, sonra yavaş çekim kaldığı
   yerden devam eder
4. Gerçek saat, dünya saatinden bağımsız ilerler
`dotnet test` yeşil olacak.

YASAKLAR
- Time.timeScale'e dokunma (Unity bağlaması T5+ işi)
- T2/T3 dosyalarını değiştirme
```

---

## Faz 2 — Unity Kabuğu (his katmanı)

> Buradan sonrası Unity editörü açık ve MCP bağlı olmalı. Her görevin sonunda play mode'a
> girip konsolun temiz olduğunu doğrula.

### T5 — Bootstrap sahne, kinematik hareket, sanal çubuk

```
Rolün: Unity kabuğunu kuran geliştirici.

ÖNCE OKU: docs/teknoloji-kararlari.md §3, §4, §5.

GÖREV
1. Paketleri kur: com.unity.inputsystem, URP, ugui (TextMeshPro), test-framework.
2. Sahne: içinde TEK boş GameObject + PrototypeBootstrap script'i olan bir sahne.
   Arena, oyuncu kapsülü, boss kapsülü, kamera, ışık — hepsi KODDAN kurulacak.
   .unity YAML dosyasını elle düzenlemeyeceksin.
3. unity/Assets/Scripts/Game/ altına Dovus.Game.asmdef (Dovus.Core'a referans verir).
4. Kinematik hareket: Rigidbody YOK, CharacterController YOK. Update içinde
   ölçeklenmiş dt ile transform. Core'daki TimeDirector'ı kullan.
5. Girdi: InputSystem.EnhancedTouch ile sol yarıda dinamik sanal çubuk. Masaüstü yedeği:
   WASD. Çok parmak aynı anda çalışacak (sol çubuk + sağ yarı boş şimdilik).
6. Kamera: yumuşak takip + hafif önden bakış. Sarsıntı/yumruk için hazır API bırak.

KABUL KRİTERLERİ
- Play mode'da kapsül sanal çubukla ve WASD ile akıcı hareket ediyor
- İki parmak aynı anda ekranda olabiliyor (Device Simulator veya telefonda doğrula)
- Konsol temiz
- Sahne dosyasında tek GameObject var

YASAKLAR
- Dövüş, çizim, boss davranışı yazma (sonraki görevler)
- Core'daki dosyaları değiştirme
- Prefab/sahne hiyerarşisini elle kurma
```

### T6 — Beşgen girdi yüzeyi ve mürekkep izi

```
Rolün: Çizim girdisini kuran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §2, §3, §9 · Core/Grammar (T2'de yazıldı).

GÖREV
1. Ekranın sağ yarısında beşgen 5 nokta + merkez. Ekrana sabit (dünyaya değil),
   yarıçapı ve konumu ayardan gelir, sağ/sol el için aynalanabilir.
2. Sürükleme: parmak bir noktaya girdiğinde SentenceEngine'e bildir. Noktada bekleme
   yoğunlaştırma olarak iletilir.
3. Merkez YALNIZCA kısa dokunmayla dodge: tapMaxMs 180, tapMaxMoveDp 12. Bu eşiklerin
   dışındaki temas çizimdir. Dodge, cümleyi Abort eder.
4. Titreşim + hece sesi: her kaydedilen nokta anında geri bildirim verir (§9 hece tablosu).
   Sesler çalışma anında üretilecek (AudioClip.Create), dosya bağımlılığı yok.
5. Mürekkep izi: noktalar arası iz mor→camgöbeği, kısa süre havada kalır. Mesh/LineRenderer,
   büyük yarı saydam katman kullanmayacaksın.
6. Masaüstü yedeği: fare sürükleme + Space dodge.

KABUL KRİTERLERİ
- 5-1-2 çizince motor doğru cümleyi görüyor (ekrana debug metni bas: fiil + sıfatlar)
- Merkeze tıklamak dodge tetikliyor, sürüklemek tetiklemiyor (ikisi karışmıyor)
- Her nokta için farklı hece duyuluyor, cümle uzayınca perde yükseliyor
- Sol başparmak hareket ederken sağ başparmak çizebiliyor (aynı anda)

YASAKLAR
- Gramer kuralı yazma/değiştirme — sadece Core'daki motora bildir
- Tezahür (dünyadaki etki) yazma, T7'nin işi
```

### T6.1 — T6 denetim düzeltmeleri

```
Rolün: T6 denetiminde çıkan hataları kapatan geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §2, §3, §4 · docs/durum.md "T6 denetimi".
Dal: task/t6-pentagon-input (T6 master'a HENÜZ girmedi, düzeltmeler aynı dala).

GÖREV
1. `MoveInput` aynalamayı bilmiyor. `OnFingerDown` sol yarıyı sabit yazıyor
   (`pos.x > Screen.width * 0.5f`), `PentagonInput` ise `PentagonLayoutScreen.IsRightHalf`
   kullanıyor. `MirrorForLeftHand = true` iken tek parmak hem çubuğu sürüyor hem çiziyor
   (play mode'da doğrulandı). `MoveInput` de aynı yardımcıyı kullansın: çubuk yarısı =
   `!IsRightHalf(...)`. Tuning zaten `MoveInput`'ta var.
2. Dwell dünya zamanına geçsin. `PentagonInput.TickDwell` `_clock.RealDeltaMs` yerine
   `_clock.WorldDeltaMs` biriktirsin. Sebep: `SentenceEngine.FreezeWindowForDwell` pencereye
   `DwellMs`'i dünya zamanı olarak iade ediyor; gerçek zamanla ölçülünce yavaş çekimde bekleme
   ~48 ms dünya zamanı yiyip 220 ms iade alıyor, yani pencereyi sıfırlıyor. §3 beklemenin
   pencereyi uzatmasını yasaklıyor. (Karar verildi — sahibi seçti.)
3. Kapalı rünler. §4: ilk turda yalnızca 1 (İĞNE), 2 (SÜRÜ), 5 (SARSINTI) açık.
   `PrototypeTuning`'e hangi noktaların açık olduğunu tutan bir alan ekle. Kapalı noktaya
   dokunuş `SentenceEngine`'e hiç bildirilmesin; hece, titreşim ve mürekkep de üretmesin.
   `PentagonView` kapalı noktayı soluk göstersin — oyuncu neden tepki almadığını görmeli.
   (Karar verildi.) Bu bir kombo/dizi tablosu DEĞİL, sadece nokta açık/kapalı bayrağı.
4. İptal edilen dokunuş tap sayılmasın. `Touch.onFingerUp` hem `Ended` hem `Canceled` için
   tetikleniyor; `PentagonInput.OnFingerUp` ikisini ayırmıyor. Merkeze basıp 180 ms dolmadan
   sistem tarafından iptal edilen dokunuş (avuç reddi, bildirim çekme) hayalet dodge tetikler.
   İptal olan `EndPointer(cancelled: true)` gitsin. Editörde görünmez, telefonda görünür.
5. Titreşim kısa olsun. `Handheld.Vibrate()` Android'de ~500 ms sabit ve ayarlanamıyor;
   4 noktalı cümlede dördü üst üste binip kesintisiz uğultuya dönüşür — §2'nin istediği
   ayrık onay kanalının tersi. `AndroidJavaObject` ile `VibrationEffect.createOneShot`
   kullan; süre `PrototypeTuning`'de alan olsun (spec'te sayı yok: varsayılan koy, gerekçesini
   durum.md'ye yaz). Android dışı platformda sessizce atla.
6. Hece tablosu Core'a bağlansın. `SyllableFeedback` rün→hece eşlemesini ikinci kez yazıyor;
   Core'da `RuneInfo.Syllable` var ve kullanılmıyor. §4 rün setinin "en ucuz değiştirilecek
   şey" olduğunu söylüyor, iki yerden değişmesi bunu bozar. Frekanslar kalsın (spec'te sayı
   yok), hece adı/sırası `RuneInfo`'dan gelsin.
7. Küçük dayanıklılık:
   - `PentagonInput.OnDisable` `_fingerId`'yi temizlemiyor → disable/enable'dan sonra çizim
     kalıcı olarak ölüyor.
   - `TryRegisterDotAt` `_engine`'i null kontrolsüz kullanıyor (başka her yerde `?.` var).
   - Dodge cooldown'dayken `TriggerDodge` yine `Abort` edip HUD'a "DODGE" yazıyor; dodge
     gerçekleşmediyse HUD yanıltmasın.

KABUL KRİTERLERİ
- `MirrorForLeftHand = true` iken sol yarıya basan parmak ya çubuğu sürüyor ya çiziyor,
  ikisini birden değil
- Kapalı noktaya (3, 4) dokunmak cümleye kelime eklemiyor, ses/titreşim/mürekkep üretmiyor
- 5-1-2 hâlâ doğru cümleyi veriyor; merkez tap dodge / sürükleme çizim ayrımı bozulmadı
- `dotnet test` yeşil (46 test), konsol temiz

YASAKLAR
- Core'daki dosyaları değiştirme — dwell düzeltmesi girdi katmanında, `WorldDeltaMs` ile
- Gramer kuralı veya dizi/kombo tablosu yazma
- Tezahür yazma (T7)
```

### T7 — Tezahür katmanı: cümlenin dünyada canlanması

**Bu görev prototipin can alıcı kısmı.** Spellisimo'nun hatası burada yapılırsa test yanlış
sonuç verir.

```
Rolün: Cümlenin dünyadaki karşılığını kuran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §8 (tezahür kuralları — beş kuralın hepsi zorunlu),
§4 (rün kabul testi ve "Prototip seti: üç rün"), §5 (kapanış vuruşu).

GÖREV
Prototip setindeki ÜÇ fiili dünyada YAŞAYAN etki olarak kur (prosedürel, sanat varlığı yok):
İĞNE delici atılış · SÜRÜ yayılan dalga · SARSINTI yere çakma şok dalgası.
KABUK ve ZEHİR bu görevin kapsamı DIŞINDA (gerekçesi §4'te) — ama gramer motoru beş rünü
tanıdığı için, tanımsız rüne dokunulduğunda temiz bir "kapalı" geri bildirimi ver.

ZORUNLU KURALLAR
- T2: hiçbir fiil anlık vurmaz; hepsi dünyada yol alır/sürer. Sıfat, YAŞAYAN etkiyi değiştirir.
- T3: her sıfat SİLÜETİ değiştirir, sayıyı değil. "5" halka dalga → "5-1" tek hatta toplanır
  → "5-1-2" hat boyunca sürü fışkırır. Değişim, etki havadayken anında görünür olacak.
- T1: karakter her rün için bir hamle yapar (prosedürel squash/stretch/atılma).
- T4: sonuç kalıcı iz bırakır (yerde çatlak, zehir birikintisi kalır).
- T5: cümle sonunda toparlanma pozu; kapanış kısa bir sessizlikten SONRA patlar.
- Kapanışın türü son rüne bağlı (§5).
- Boss fiziksel tepki verir: geri tepme, sarsılma, kabukla kapatılırsa sabitlenme.
- Renk dili §10: kırmızı-turuncu YALNIZCA boss tehdidi. Oyuncu efekti camgöbeği/mor.

KABUL KRİTERLERİ
- "5" çizip bırakınca halka dalga gidiyor; "5-1" yazarken dalga gözle görülür biçimde
  tek hatta TOPLANIYOR (aynı etkinin mutasyonu, yeni bir efekt spawn'ı değil)
- Yarıda kesilen cümlede kapanış hiç gelmiyor
- Yerde kalıcı iz kalıyor
- Hiçbir yerde oyuncu efektinde kırmızı-turuncu kullanılmamış
- Ekranda hasar sayısı YAZMIYOR (bilerek: his kanıtı sayıda olmayacak)

YASAKLAR
- Partikül fırtınası, büyük yarı saydam katman (mobil overdraw)
- Hazır asset indirmek
- Gramer motorunu değiştirmek
```

### T8 — Boss telegrafı, sıyırma, yavaş çekim, kamera yumruğu

```
Rolün: Boss dövüş döngüsünü ve his katmanını kuran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §6, §7, §10, §11 · Core/Combat (T3) · Core/Time (T4).

GÖREV
1. Boss: YERE ÇAKMA saldırısı (windup 640 / active 90 / recovery 720 / radius 5.4).
   Okunabilir telegraf: hazırlık pozu + büyüyen yer göstergesi + yükselen ses.
   Idle'da oyuncuya doğru yavaş yaklaşır (2.2 m/s).
2. Vuruş, aktif pencerenin başında tek karede çözülür; ExchangeResolver'ı kullan.
3. Sıyırma olunca: TimeDirector.TriggerSlowmo + hitstop + kamera yumruğu (FOV sıçraması,
   kısa rotasyon, sarsıntı) + tek karelik impact frame + afterimage izleri.
4. Vurulunca: kırmızı vinyet, güçlü hitstop, sebep bilgisini HUD'a ilet.
5. Ölüm cezası neredeyse sıfır: ölümden sonra en fazla 2 saniyede tekrar dövüşte.
6. İptal pencereleri DÜNYA zamanıyla işlediği için yavaş çekimde parmağa gerçek zamanda
   daha çok süre kalmalı. Bunu doğrula: yavaş çekimde 4 noktalı cümle sığıyor, normalde sığmıyor.

KABUL KRİTERLERİ
- Telegrafı okuyup tam zamanında dodge atmak yavaş çekim açıyor
- 6. maddedeki fark gözle doğrulanabiliyor (normalde 2-3 nokta, yavaş çekimde 4)
- Erken/geç basma farkı hissedilir ve sebebi ekranda yazıyor
- Boss vuruşları kırmızı-turuncu, oyuncu efektleri değil

YASAKLAR
- Yeni boss saldırısı ekleme (tek saldırı yeter)
- Yavaş çekim rampasını Core dışında yeniden yazma
```

### T9 — HUD: parlak tepki yazısı

```
Rolün: Geri bildirim arayüzünü kuran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §6 (gösterim), §7, §10.

GÖREV
1. Sağ kenarda büyük ve PARLAK tepki yazısı: "0.45 sn" + derece ("MÜKEMMEL") +
   alt satırda mesaj ("tepki süren mükemmel"). Katmanlı glow, giriş vuruşu (scale punch),
   sonra sönme.
2. Animasyon ÖLÇEKLENMEMİŞ saatle çalışacak: dünya yavaşken bile keskin görünmeli.
3. Seri sayacı (üst üste sıyırma) ve "en iyi tepki" kaydı.
4. Vurulma sebebi yazısı ("erken bastın" / "geç kaldın").
5. Boss ve oyuncu can göstergesi, sade.
6. Ayarlanabilir: punto, glow şiddeti, bekleme/sönme süresi, hangi kenarda duracağı.

KABUL KRİTERLERİ
- Mükemmel dodge sonrası yazı okunaklı, parlak ve zamanında görünüyor
- Yavaş çekim sırasında yazının animasyonu yavaşlamıyor
- Yazı bossun telegrafını kapatmıyor (§10: telegraf en üst katman)

YASAKLAR
- Hasar sayısı gösterme
- Yeni oyun mekaniği ekleme
```

### T10 — Oyun içi ayar paneli (telefonda his ayarı)

```
Rolün: Ayar panelini kuran geliştirici.

ÖNCE OKU: docs/teknoloji-kararlari.md §6 · docs/dovus-sistemi.md §5, §6, §7.

GÖREV
1. TuningConfig ScriptableObject'leri: Core'daki POCO ayar sınıflarını yansıtır, varsayılanlar
   belgeden gelir.
2. Oyun içi panel: ekranda açılıp kapanan, gruplanmış slider'lar (dodge / cümle / yavaş çekim /
   kamera / yazı / boss). Her slider anında canlı etki eder, yeniden başlatma gerektirmez.
3. Kalıcılık: Application.persistentDataPath altında JSON. Açılışta okunur, ScriptableObject
   varsayılanlarının üstüne yazar. "Sıfırla" ve "JSON'u panoya kopyala" düğmeleri.
4. Hazır setler: "Ağır", "Çevik", "Anime".

KABUL KRİTERLERİ
- Telefonda uygulamayı kapatıp açınca ayarlar korunuyor
- Yavaş çekim çarpanı ve dodge mesafesi oyun sırasında canlı değişiyor
- Panel kapalıyken ekranda hiçbir iz bırakmıyor

YASAKLAR
- Ayarları koda gömme (özetin en kritik kuralı: ayarlanabilir olan her şey veri)
```

---

## Faz 3 — Telefon Turu

### T11 — Android build ve his turu

```
Rolün: Prototipi telefona alıp ölçen geliştirici.

GÖREV
1. Android build al (IL2CPP, ARM64, geliştirme build'i). Hedef: sabit 60 fps.
2. Basit bir kare süresi göstergesi ekle (açıp kapanabilir).
3. docs/his-kontrol-listesi.md dosyasını oluştur: docs/dovus-sistemi.md §13'teki beş sorunun
   her biri için, telefonda elde cevaplanacak biçimde alt maddeler ve boş cevap alanı.
4. Bilinen sorunları ve ölçülen kare sürelerini o dosyaya yaz.

KABUL KRİTERLERİ
- APK telefonda çalışıyor, 60 fps'e yakın
- İki parmak aynı anda sorunsuz (sol çubuk + sağ çizim)
- His kontrol listesi dosyası hazır ve doldurulabilir durumda

YASAKLAR
- Görsellik iyileştirmesi yapma; bu tur ölçüm turu
```

---

## Faz 4 — Görsellik

**Henüz başlanmayacak.** [§13'teki 1. ve 2. soruya](dovus-sistemi.md#13-prototipin-cevapladığı-sorular)
telefonda "evet" cevabı alınmadan görsel katmana geçilmesi, yanlış şeyi cilalamak olur.

O aşamaya gelindiğinde ilk kararlar: zincirlenebilir animasyon seti seçimi (Animancer/Playables),
Asset Store VFX (Hovl Studio, Gabriel Aguiar, Kripto289), ve rig'lenmiş boss modeli.
Bkz. [Teknoloji Kararları §7](teknoloji-kararlari.md#7-animasyon-efekt-ses).
