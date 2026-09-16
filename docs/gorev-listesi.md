# Ajan Görev Listesi — Alfa Prototip

> Sırayla çalıştırılacak, birbirinden bağımsız doğrulanabilir görevler. Her görevin altında
> **kopyala-yapıştır hazır prompt** var; Composer gibi hızlı modellerle çalışacak şekilde
> yazıldı: dar kapsam, net dosya yolları, sayılabilir kabul kriteri, açık yasaklar.
>
> Kaynaklar: [Element Sistemi](element-sistemi.md) · `element-sistemi.json`

> **16 Eylül 2026:** `dovus-sistemi.md` ve `teknoloji-kararlari.md` silindi. Aşağıdaki T0-T14
> görev prompt'ları **tamamlandı** (durum tablosuna bak) ve bu iki dosyaya onlarca yerde
> referans veriyor — bu tarihi kayıttır, yeni görev yazarken örnek alınmaz. Yeni görevler
> [Element Sistemi](element-sistemi.md) + `element-sistemi.json`'a referans vermeli.

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
| 3 | T11 | evet + telefon | §13'ün 1–2. sorusu (eski `dovus-sistemi.md`, silindi) — **evet, kapandı** |
| 3.5 | T11.1–T14 | evet + telefon | §13'ün 3–5. sorusu: soyutluk, tekdüzelik, harcamanın karşılığı |
| 4 | görsellik | — | Faz 3.5 kapanmadan başlanmaz |

## Hangi görev hangi modelle

Ölçüt tek soru: görev `AGENTS.md`'deki **değişmez kurallara** dokunuyor mu (Core saflığı, sayı
uydurmama, kombo tablosu yasağı, silüet ≠ sayı), yoksa speci verilmiş bir yüzey mi?

- **Core'a, gramere veya birden çok katmana aynı anda dokunan görevler → Opus.** T6.2 ve T8
  bu sınıfta: ikisi de motoru/zamanı değiştiriyor ve önceki görevlerden devralınan açıkları
  kapatıyor, yani bağlamı bir arada tutmak gerekiyor.
- **Speci net, tek katmanlı yüzey görevleri → Sonnet.** T9 (HUD) ve T10 (ayar paneli) böyle:
  hacim var, incelik yok. T11'in ajana düşen kısmı da (fps göstergesi, his kontrol listesi
  iskeleti) burada; build ve cihaz ölçümü insan işi.
- **Mekanik, saniyede doğrulanabilen işler → Composer.** `.meta` düzeni, yeniden adlandırma,
  gömülü sayıyı `PrototypeTuning`'e taşıma, test iskeleti. Proje büyüdükçe Composer'ın hata
  kalıbı sabit: sayı uydurmak, tablo/dizi yazmaya kaçmak ve "ÖNCE OKU" listesinin dışına
  taşıp başka görevin dosyasına dokunmak. Dövüş mantığına sokulmaz.
- **Denetim turları → yazan modelden farklı bir model (Grok).** T6.1, T7.1, T7.2 ve T7.4'ün
  hepsi denetimden doğdu; bu projeyi taşıyan şey o turlar. Denetçi **kod yazmaz**: bulgularını
  `docs/durum.md`'ye yazar, düzeltmeyi T#.1 görevi olarak yazan model yapar.

Faz 3.5 dağılımı bu ölçüte göre: **T11.1** Composer (üç küçük, gözle doğrulanabilir düzeltme),
**T12** Opus (Core'daki ödül tablosunu hasara bağlıyor ve kapanış türünü ayırıyor — gramere
dokunan tek görev), **T13** ve **T14** Sonnet (biri boss döngüsü, biri görünüm katmanı; ikisi
de tek katman ve speci verilmiş).

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

> **T6.1 notu (tekrarlama):** Kapalı rün geri bildirimi bitti — 3/4 motora gitmiyor,
> `PentagonView` soluk çiziyor. "Tanımsız rüne kapalı geri bildirim" maddesini yeniden
> yapma. Katmanlama (Overlay canvas vs telegraf) ve dodge yer değiştirmesi T8 / bilinen açık.

```
Rolün: Cümlenin dünyadaki karşılığını kuran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §8 (tezahür kuralları — beş kuralın hepsi zorunlu),
§4 (rün kabul testi ve "Prototip seti: üç rün"), §5 (kapanış vuruşu).

GÖREV
Prototip setindeki ÜÇ fiili dünyada YAŞAYAN etki olarak kur (prosedürel, sanat varlığı yok):
İĞNE delici atılış · SÜRÜ yayılan dalga · SARSINTI yere çakma şok dalgası.
KABUK ve ZEHİR bu görevin kapsamı DIŞINDA (gerekçesi §4'te).
Kapalı rün geri bildirimi T6.1'de çözüldü — burada yeniden yazma.

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

### T6.2 — Düz vuruş, dodge düğmesi, toparlanma kilidi

**Sıra: T8'den ÖNCE.** T8 dodge'un yer değiştirmesini yazacak; düğmenin yeri ve toparlanma
kilidi ondan önce oturmalı, yoksa T8 iki kere yapılır. Karar sahibiyle verildi (22 Ağustos):
merkez artık dodge değil.

```
Rolün: Girdi düzenini ve cümle tempo kilidini kuran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §1, §2, §5 (özellikle "Düz vuruş ve erken kapanış" ve
"Toparlanma girdi kilididir") · docs/durum.md T6/T6.1 ve T7.1 bölümleri ·
Core/Grammar/SentenceEngine.cs · Game/PentagonInput.cs.

BAĞLAM
Beşgenin merkezi dodge'du; düz vuruş hiç yoktu. Merkez artık düz vuruş / erken kapanış,
dodge ise beşgenin dışında ekrana sabit ayrı bir düğme. Ayrıca §5'teki toparlanma süresi
bugüne kadar sadece bir poz süresiydi; artık gerçek bir girdi kilidi ve kesilebilir olacak.

GÖREV
1. Core — SentenceEngine:
   - public Commit(): yalnızca Building fazında ve en az bir kelime varken çalışır, gövdesi
     mevcut ResolveWithClosing(). Idle/Recovering'de sessizce hiçbir şey yapmaz.
   - Yeni faz: Recovering. Kapanış üreten HER yol (Commit, dördüncü nokta, pencere zaman
     aşımı) cümleyi kapatıp Recovering'e sokar. Kalan süre SentenceTuning.StepForDots(n)
     .RecoverySec'ten gelir (§5 tablosu — sayı uydurma) ve Tick(dtMs) ile DÜNYA zamanında
     erir; bitince Idle.
   - Kesme: Recovering'de OnDotTouched kalan kilidi sıfırlayıp yeni cümleyi başlatır.
     Abort() yalnızca Building'de yatırımı batırır; Recovering'de kilidi keser ama ödenmiş
     kapanışı geri ALMAZ (History'deki kayıt ve LastClosing durur).
   - CompletedSentence.Phase kapanışta Resolved kalmalı (geçmiş/ödül okunuyor); Recovering
     yalnızca State.Phase'in anlık değeri. Kalan kilit State'ten okunabilsin (T9 gösterecek).
2. Girdi — PentagonInput:
   - Merkez kısa dokunma: Building ise Commit(); Idle veya Recovering ise düz vuruş, yani
     OnDotTouched(PrototypeTuning.BasicStrikeDot) + Commit() aynı karede.
   - Dodge artık merkezde DEĞİL: beşgenin dışındaki sabit düğme + Space (masaüstü). Çizim
     parmağı meşgulken ikinci parmak düğmeye basabilmeli (panik dodge). Cooldown'daysa
     Abort da HUD yazısı da yok (T6.1 kuralı).
   - Hit sırası: dodge düğmesi → merkez → nokta. Merkezden ve düğmeden SÜRÜKLEME hâlâ çizim
     (tapMaxMs/tapMaxMoveDp eşikleri ikisi için de geçerli).
3. Yerleşim — PentagonLayoutScreen + PentagonView + PrototypeTuning:
   - Dodge diski: beşgen merkezinden dışarı, sağa-aşağı; MirrorForLeftHand ile aynalanır.
     Ofset/yarıçap ayar alanı olacak (spec'te sayı yok: varsayılan koy, durum.md'ye yaz).
   - Düğme ekranın dışına taşmasın: PentagonCenterXNorm'u gerektiği kadar içeri al.
   - Merkez oyuncu rengine (§10 camgöbeği/mor) çekilir — artık "vur" demek. Dodge diski
     kırmızı-turuncu OLAMAZ (§10: o renk yalnızca boss tehdidi).
   - BasicStrikeDot ayar alanı; varsayılan 5 (SARSINTI, açık rün).
4. ManifestationDirector: düz vuruşta OnDotTouched + Commit aynı karede olduğu için Director
   Update'inde Building fazını hiç görmeyebilir ve spawn yutulur (T7.1'deki "aynı karede iki
   nokta" hatasının aynı sınıfı). Kapanış patlamasının kilit kesilse de kendi zamanlamasıyla
   gelmeye devam ettiğini doğrula (§5).

KABUL KRİTERLERİ
- Cümle yokken merkeze tıklamak dünyada yaşayan bir düz vuruş + kapanış üretiyor (köşede
  pencere beklemeden)
- 5-1 çizip merkeze tıklamak 2 nokta ödemesi yapıyor (Abort DEĞİL); ardından 2 noktanın
  toparlanma kilidi başlıyor
- Kilit dolmadan merkeze tıklamak düz vuruş çıkarıyor ve kalan kilidi kesiyor; kilit dolmadan
  köşeye basmak yeni cümle başlatıp kilidi kesiyor
- Hiçbir şey kesmezse kilit süresi dolana kadar yeni cümle/düz vuruş başlamıyor
- Dodge düğmesi (ve Space): Building'de cümleyi Abort ediyor, Recovering'de kilidi kesiyor
  ama ödenmiş kapanışı silmiyor
- Merkezden ve dodge düğmesinden sürükleme hâlâ çizim; sol çubuk + sağ çizim aynı anda
- `cd tools/CoreTests && dotnet test` yeşil (mevcut 57 + yenileri), play mode konsolu temiz

YASAKLAR
- Altıncı rün ekleme. Merkez bir kelime DEĞİL; gramer beş nokta olarak kalır.
- İptale/kesmeye hasar çarpanı bağlama (§12: ödül kesilen süredir)
- Dodge yer değiştirmesini yazma — T8'in işi (bu görev yalnızca düğme ve i-frame tetiği)
- §5 tablosundaki süre/etki/toparlanma sayılarını değiştirme
- Boss davranışı, telegraf, HUD yazısı yazma
```

### T8 — Boss telegrafı, sıyırma, yavaş çekim, kamera yumruğu

> **T6.2 notu:** Dodge artık beşgenin merkezinde değil, ekrana sabit ayrı bir düğme; merkez
> düz vuruş / erken kapanış. Yer değiştirme hâlâ senin işin — yön: son hareket yönü, o da
> yoksa bossun tersi. Ayrıca `SentenceEngine`'de `Recovering` fazı var; yavaş çekim ölçümünü
> yaparken kilit süresini hesaba kat.

```
Rolün: Boss dövüş döngüsünü ve his katmanını kuran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §6, §7, §8/T2, §10, §11 · Core/Combat (T3) · Core/Time (T4)
· docs/durum.md "Bilinen açıklar" + "T7.1 sapmaları" (katmanlama, dodge yer değiştirme,
iptal penceresinin görünmezliği).

GÖREV
0. Sahiplen (önceki görevlerden açık — hepsi docs/durum.md'de kayıtlı):
   - Dodge: `GetDisplacementRatio` / glide'ı oyuncu transform'una uygula (+ afterimage).
   - Katmanlama: Overlay canvas telegrafı eziyor — §10 "en üst katman" için tek mekanizma.
   - §8/T2'nin bedava kazancı kurulmadı: "iptal penceresini dalganın nerede olduğuna bakarak
     bilirsin". Şu an pencere yalnızca debug metninde; dünyada görünmüyor. Kalan süre
     `LivingEffect.Travel`/`MaxRange` üzerinden okunabilir, görsel bir ipucuna bağlanmalı
     (nabız/solma/renk). Spec sayı vermiyor; koyduğun sayıyı `PrototypeTuning`'e ve
     docs/durum.md'ye yaz.
   - `SentenceEngine.OnDotTouched`/`OnDwell` `worldTimeMs`'i yutuyor; pencere yalnızca
     `Tick(dtMs)` ile eriyor (~16 ms kare yuvarlaması). Yavaş çekimde pencere ölçümü senin
     kabul kriterin olduğu için bu hassasiyeti Core'da düzeltmek T8'in işi.
   - Boss konumu `BossReactor.Home` property'sinin sahipliğinde: yaklaşma hareketini
     `transform.position`'a değil `Home`'a yaz, yoksa geri tepme kalıcılığı (T7.2) bozulur.
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
- `GameObject.CreatePrimitive` çağırma (collider doğurur): mesh gerekiyorsa
  `PrimitiveMesh.Get(PrimitiveType)`
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

## Faz 3.5 — Somutlaştırma ve dövüş yayı

> **Neden bu faz var.** §13'ün 1. ve 2. sorusu telefonda "evet" aldı, yani Faz 4'ün kapısı
> açık. Ama 3–5. sorular boş ve boş kalma sebebi sanat değil: sahibi mekaniği **okuyor**
> (*"dodge atıyorum, yavaş çekim geliyor, 2'li çizince başka 3'lü çizince başka"*) ama
> temsil soyut kalıyor, boss tek saldırıyla tekdüze ve harcamanın gittiği bir yer yok.
>
> Sıra **karar → içerik → temsil → sanat**. Faz 4'ün pahalı kısmı (rig'lenmiş boss, animasyon
> seti, satın alınmış VFX) bossun kaç saldırısı olduğuna ve nasıl öldüğüne doğrudan bağımlı;
> o iki karar dondurulmadan model/animasyon alınırsa "bir alan değiştir" işi "yeniden yaptır"
> işine döner. Bu fazın tamamı hâlâ **primitive** ile yapılır.
>
> Sıra bağlayıcı: T11.1 → T12 → T13 → T14. Kilit HUD'da görünmeden ve mürekkep cümle sınırını
> göstermeden boss canı eklenirse "bedel yok" şikâyeti aynı kalır, çünkü oyuncu neyi ne zaman
> harcadığını hâlâ göremez.

### T11.1 — Telefon turunun bulduğu üç düzeltme

**Hedef:** cümlenin nerede bittiği ve neyin harcandığı ekranda görünsün. Kod hacmi küçük,
etkisi büyük — T12'nin ön koşulu.

```
Rolün: telefon turunda çıkan üç düzeltmeyi kapatan geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §5 ("On nokta çizmek" paragrafı + "Toparlanma girdi
kilididir") · docs/his-kontrol-listesi.md "Turda çıkan yeni sorunlar" · docs/durum.md
"Bilinen açıklar".

GÖREV
1. Mürekkep cümle sınırını göstermiyor. Telefonda yavaş çekimde 6–7 nokta tek kesintisiz
   iz olarak çizilebiliyor; sahibi bunu "tavan tutulmuyor" diye rapor etti ama motor DOĞRU
   çalışıyor: `MaxSentenceDots = 4`, 4. noktada cümle kapanıyor ve 5. nokta YENİ bir fiil
   başlatıyor (§5). Hata izde: `InkTrail` kesintisiz devam ettiği için oyuncu iki cümleyi
   tek cümle sanıyor. §5 "cümlenin nerede bittiği ezberlenmez, GÖRÜLÜR" diyor; şu an
   görülmüyor. `SentenceEngine.SentenceCompleted` olayına bağlanıp izi cümle sınırında
   KOPAR: eski iz kendi ömrüyle sönmeye başlar, yeni cümlenin izi yeni bir şerit olarak
   sıfırdan başlar (§10: mor → camgöbeği geçişi de yeniden başlar).
2. Toparlanma kilidi kalıcı HUD'a çıksın. Kalan kilit şu an yalnızca `SentenceDebugHud`'ın
   debug metninde ("kilit: X ms"). Oyuncu kestiği süreyi görmediği için §5'in kesme beceri
   ekseni görünmez; 4. oturumda sahibi tam bunu söyledi ("skill kullanmanın bedeli yok").
   `ReactionReadout`/`VitalsHud` ile aynı canvas'ta, sade: kalan kilit süresi eriyen bir
   gösterge. Kesildiğinde görünür biçimde kesilsin — kesmenin ödülü budur.
   Renk §10'dan: oyuncu paleti (camgöbeği/mor), kırmızı-turuncu OLAMAZ.
3. `BossDirector.TickWindup` null guard. `_attack` null iken patlıyor (Editor.log'da eski
   prob oturumlarından yüzlerce NullReferenceException). Gerçek sahnede Bootstrap aynı karede
   `Bind` çağırdığı için oyunda görülmüyor, yalnızca MCP probları için tuzak. Tek satır.

KABUL KRİTERLERİ
- Yavaş çekimde 7 nokta çizince ekranda İKİ ayrı mürekkep şeridi görünüyor (4 + 3), tek
  kesintisiz iz değil; sınır gözle sayılabiliyor
- Kalan toparlanma kilidi kalıcı HUD'da görünüyor ve düz vuruş/yeni fiil/dodge ile
  kesildiğinde gözle kesildiği görülüyor
- Kilit göstergesinde kırmızı-turuncu yok
- `cd tools/CoreTests && dotnet test` yeşil (mevcut 79), play mode konsolu temiz

YASAKLAR
- Core'a dokunma. `SentenceEngine` doğru çalışıyor; düzeltme görünüm ve HUD katmanında
- `MaxSentenceDots`'u değiştirme — tavan doğru, görünürlük eksik
- Boss canı/hasar yazma (T12), varyant yazma (T13), silüet değiştirme (T14)
```

### T12 — Boss canı, hasar, ölüm, kapanış türü

**Hedef:** harcamanın gittiği bir yer olsun, ve hasar eklenirken şekil ekseni ölmesin.

```
Rolün: kapanış ödülünü boss canına bağlayan ve kapanış türünü ayıran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §5 ("Kapanış vuruşu ve ödül" + "Etkinin hasara çevrilmesi"),
§11 (boss canı ve ölüm), §12 (tuzak tablosu), §8/T4 · Core/Grammar/SentenceEngine.cs ·
Game/ManifestationDirector.cs · Game/BossReactor.cs · Game/PlayerVitals.cs (desen için).

BAĞLAM
Boss can göstergesi T9'dan beri KOZMETİK: Core/Game'de hiçbir yerde boss HP'si yok ve
"boss nasıl ölür" hiç tanımlı değildi. 23 Ağustos'ta sahibiyle karara bağlandı ve spec
güncellendi. Bu görev o kararı uyguluyor.

GÖREV
1. Core — boss canı ve hasar:
   - `BossTuning.MaxHp = 120` ve `CombatTuning.ClosingDamagePerEffect = 1.0` (ikisi de
     §11/§5'te yazılı, sayı uydurma).
   - `Core/Combat/BossVitals.cs` — saf C#, `PlayerVitals`in Unity'li deseninin aksine
     Core'da: `Hp`, `MaxHp`, `ApplyDamage(float)`, `IsDown`, `Revive()`. Zaman parametre
     olarak geçer, `UnityEngine` yok.
   - Kapanış ödülü (`SentenceStep.TotalEffect`, §5 tablosu: 1.0 / 2.4 / 4.4 / 7.0) ×
     `ClosingDamagePerEffect` = bossun canından düşen sayı. Yarıda kalan cümle 0 verir
     (§5 "yarıda kalan cümle hiç ödeme yapmaz" — mevcut davranış, bozulmayacak).
2. Kapanışın TÜRÜ son rüne bağlı kalsın (§5 + §12 — bu madde atlanamaz, hasarın tek
   başına kalması tuzağın kendisi). `BossReactor` üç tepkiyi zaten taşıyor; kapanış son
   rüne göre birini seçer:
   - 5 SARSINTI → havalandırma (spec §5'te açıkça yazılı)
   - 1 İĞNE → tek yöne toplanmış derin geri tepme (§4: "daralt / odakla"dan türetildi)
   - 2 SÜRÜ → yerinde çok noktalı sarsılma, yer değiştirme az (§4: "çoğalt / yay")
   Kapalı rünler için §5 zaten söylüyor: KABUK sabitleme, ZEHİR birikinti.
   **Hasar miktarı türe göre DEĞİŞMEZ** — tür ve miktar iki ayrı eksen; türün miktarı
   değiştirmesi §12'nin "sıfatın sayıyı değiştirmesi" tuzağıdır.
3. Ölüm (§11): can 0'a düşünce kısa yavaş çekim + çökme pozu, sonra boss TAM canla yeniden
   doğar. `TimeDirector.TriggerSlowmo` kullan, Core dışında yeni rampa yazma.
4. `VitalsHud`'ın boss barı artık gerçek `BossVitals.Hp/MaxHp` okuyor (kozmetik değil).
5. Hasar göstergesi: `TuningPanel`'e VARSAYILAN KAPALI bir satır (`ShowDamageNumbers`),
   `ShowFrameTimeHud` deseninin aynısı — `PanelFields`'a yalnızca bool eklenir (T11
   sapmasındaki gerekçe: eski `tuning.json`'da alan yoksa `JsonUtility` 0 verir, bool için
   0 = false = güvenli). Bu bir ÖLÇÜM aracıdır: açıkken kapanışın verdiği hasarı yazar.

KABUL KRİTERLERİ
- 4 noktalı cümlenin kapanışı bossun canından tam 7.0 düşürüyor; düz vuruş 1.0
- Yarıda dodge'la kesilen cümle bossun canından hiçbir şey düşürmüyor
- Üç açık rünle kapatılan cümleler bossa GÖZLE FARKLI fiziksel tepki veriyor (havalanma /
  geri tepme / yerinde sarsılma) ve üçünün hasarı AYNI
- ~17 tam kapanışta (ya da karışık ~25–30) boss ölüyor, kısa yavaş çekim + çökme geliyor,
  sonra tam canla yeniden doğuyor
- Boss barı gerçekten azalıyor; hasar sayısı varsayılan olarak ekranda YAZMIYOR
- `dotnet test` yeşil (mevcut 79 + yenileri), play mode konsolu temiz

YASAKLAR
- Hasar sayısını varsayılan açık bırakma (§8/T3, §12 — his kanıtı sayıda olmayacak)
- Kapanış türünü hasar çarpanına çevirme; tür ve miktar ayrı eksen kalacak
- Yeni boss saldırısı/varyantı yazma (T13), silüet değiştirme (T14)
- Zafer ekranı, ilerleme, ödül veya loot yazma — §11 ölümün noktalama işareti olduğunu söylüyor
```

### T13 — Çakma varyantları

**Hedef:** telegraf okuma bir kez öğrenilen refleks olmaktan çıksın.

```
Rolün: bossun tek saldırısına üç ritim kazandıran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §11 ("Çakma varyantları" tablosu ve altındaki kısıt), §6,
§10 · docs/tasarim-ozeti.md §4 Sütun 1 ("her ölüm adil olmalı") · Game/BossDirector.cs ·
Game/BossTelegraph.cs.

GÖREV
1. §11'deki üç varyant: YAKIN (windup 640 / radius 5.4), GEÇ (900 / 5.4), GENİŞ (640 / 8.0).
   Hepsi AYNI saldırı — yeni bir desen değil, aynı desenin ritimleri. Sayılar §11'de yazılı,
   `BossTuning`'de alan olacak.
2. **Ayırt edilebilirlik pazarlıksız** (§11 kısıtı): varyant vuruştan ÖNCE, windup sırasında
   okunabilmeli. GEÇ'in tell'i ses (ton daha yavaş yükselir, hazırlık pozu daha uzun tutar),
   GENİŞ'in tell'i disk (baştan itibaren gözle görülür biçimde büyük). Okunamayan varyant
   zar atışıdır ve 1. sütunu çökertir.
3. Varyant seçimi idle'da yapılır, telegraf başlamadan. Aynı varyantın üst üste kaç kez
   gelebileceği ayar alanı olsun — üç kez aynı varyant gelirse oyuncu desen yok sanır.
4. `ExchangeResolver` yolu değişmiyor: vuruş hâlâ aktif pencerenin başında tek karede
   çözülür, yalnızca `radius` ve `windup` varyanttan gelir.

KABUL KRİTERLERİ
- Üç varyant da oyunda gerçekten geliyor ve hangisinin geldiği windup sırasında ayırt
  edilebiliyor (ekran görüntüsü/ses ile doğrula)
- GENİŞ varyantında kaçan dodge artık "MENZİL DIŞI (derece yok)" yerine TEMİZ/SIYIRDI
  üretiyor — §11'in not ettiği yan fayda ölçülerek doğrulanacak
- GEÇ varyantında erken basan oyuncu "erken bastın" alıyor, sebep doğru
- Boss telegrafı hâlâ §10'un en üst ve en okunabilir katmanı
- `dotnet test` yeşil, play mode konsolu temiz

YASAKLAR
- İKİNCİ bir saldırı ekleme. Üçü de aynı YERE ÇAKMA; farkı yalnızca windup ve menzil
- Varyantı gizemli yapma: rastgele ama okunabilir. Tell'i olmayan varyant kabul edilmez
- Oyuncu efektlerine dokunma, silüet değiştirme (T14)
```

### T14 — Silüet keskinleştirme

**Hedef:** üç rün "bir efekt" gibi görünmeyi bıraksın. Faz 3.5'in son görevi.

```
Rolün: üç rünün silüetlerini birbirinden ayıran geliştirici.

ÖNCE OKU: docs/dovus-sistemi.md §8 (beş kuralın hepsi), §4 ("İlk turun sonucu: çeşitlilik
yok" + rün tablosu), §10 · docs/his-kontrol-listesi.md soru 4 ve 5 ·
Game/LivingEffectView.cs · Game/GroundScarField.cs.

BAĞLAM
Telefon turunda sahibi mekaniği okuyabildiğini ama sonucun SOYUT kaldığını söyledi; üç rünün
silüetleri ayrık değil. §13'ün 5. sorusu düzeltmeyi zaten yazmış: rün eklemek değil, türleri
keskinleştirmek. Ayrım HAREKET KARAKTERİNDEN gelecek (sahibinin seçimi), biçimden değil.

GÖREV
Üç fiilin hareket karakteri birbirine benzemeyecek şekilde yeniden kurulacak. Hâlâ prosedürel,
hâlâ `PrimitiveMesh` — sanat varlığı yok:
- **İĞNE** fırlar: tek yön, yüksek hız, ince, VARIŞTA sert durur. Zenitsu kalıbının küçük
  hâli (özet §6): gerilme → 2–3 karelik gidiş → donmuş varış pozu.
- **SÜRÜ** üşüşür: çok gövde, düzensiz, yayılan; tek bir cephe değil dağınık bir bulut.
  Gövdeler aynı anda değil kademeli varır.
- **SARSINTI** yerden yükselir: aşağıdan yukarı, genişleyen halka; hız düşük, kütle yüksek.
Ölçüler ve zamanlamalar `ManifestationTuning`/`PrototypeTuning` alanı olacak (spec'te sayı
yok: varsayılan koy, gerekçesini docs/durum.md'ye yaz).

Ayrıca: **düz vuruşun kendi silüeti olsun.** Şu an `BasicStrikeDot` normal cümlenin SARSINTI
halkasını kullanıyor, yani "vuruş mu, cümle mi" hissi karışıyor (bilinen açık). Kısa, dar,
tek vuruşluk ayrı bir silüet — bir cümle gibi görünmemeli.

KABUL KRİTERLERİ
- Üç fiil, sesi kapatıp ekran görüntüsüne bakan birine hangisi olduğunu söyletebiliyor
- `5` → `5-1` → `5-1-2` mutasyonu hâlâ AYNI etkinin morph'u, yeni spawn değil (T7 kriteri
  bozulmayacak)
- Düz vuruş, 1 noktalı bir cümleden gözle ayırt edilebiliyor
- §10 ayakta: oyuncu efektlerinde hiç kırmızı-turuncu yok, telegraf hâlâ en üstte
- Kare süresi 60 fps'te kalıyor (telefonda ölçülecek; `GroundScarCapCount` tavanı duruyor)
- `dotnet test` yeşil (Core'a dokunulmayacak), play mode konsolu temiz

YASAKLAR
- Partikül fırtınası, büyük yarı saydam katman (mobil overdraw — özet §6'nın uyarısı)
- Hazır asset indirmek, rig'lenmiş model, bloom/post-process — hepsi Faz 4
- `GameObject.CreatePrimitive` çağırmak; mesh gerekiyorsa `PrimitiveMesh.Get(PrimitiveType)`
- Sıfatı sayıya bağlamak (§8/T3) — sıfat silüeti değiştirir
- Gramer, boss davranışı veya hasar matematiğine dokunmak
```

> **Faz 3.5 bitince telefonda yeni bir tur yapılır** ve `docs/his-kontrol-listesi.md`'nin
> 3, 4, 5. soruları doldurulur. "İzliyorum" ve "yazıyorum" cevapları geldiğinde Faz 4'e
> neyi cilalayacağını bilerek girilir.

---

## Faz 4 — Görsellik

**Kapı açıldı ama sıra Faz 3.5'te.** §13'ün 1. ve 2. sorusuna (eski `dovus-sistemi.md`, silindi)
telefonda "evet" cevabı **alındı** (23 Ağustos) — yani eski yasak kalktı. Yerine tek bir
bağımlılık kaldı: Faz 4'ün pahalı kalemleri bossun kaç saldırısı olduğuna ve nasıl öldüğüne
bağlı. T12 ve T13 o iki kararı dondurmadan rig'lenmiş model ya da animasyon seti alınırsa,
sonraki her ayar değişikliği "bir alan değiştir" olmaktan çıkıp "yeniden yaptır" olur.
Faz 3.5'in tamamı primitive ile yapılıyor ve bu yüzden geri dönüşü ucuz.

O aşamaya gelindiğinde ilk kararlar: zincirlenebilir animasyon seti seçimi (Animancer/Playables),
Asset Store VFX (Hovl Studio, Gabriel Aguiar, Kripto289), ve rig'lenmiş boss modeli.
(Eski `teknoloji-kararlari.md` §7'de detaylıydı, silindi.)

**Faz 4'ten sonraki büyük sistem sanat değil, durum etkileşim tablosu.** (Eski
`dovus-sistemi.md` §4 ve "Tasarım Özeti" §5'te detaylıydı, ikisi de silindi — özet
`element-sistemi.md` §7/§10'da.) ıslak / yanıyor / zehirli / sersem / havada / zırhı kırık
durumları ve aralarındaki etkileşim kuralları henüz yok. Altıgen sistem (6 element × 42
fiil/sıfat) zaten çok cümle üretiyor; o cümleler ancak bossun durumu onların anlamını
değiştirdiğinde birbirinden farklı hissedecek. Bu, `element-sistemi.md` §10'daki en büyük
açık.

---

## Backlog — Element sistemi motor tam uyumu (v5.3, 16 Eylül 4. tur)

**Durum:** `SkillMotor` şu an `verbs`/`adjectives`/`elements`/`scaling_economy.lengths`/
`active_modes`'u okuyor ve dünyaya işliyor (ulti sistemi uçtan uca çalışıyor). Geri kalan
JSON kök bölümleri (`formulas`, `crit_system`, `global_rules`, `state_machine`, `passives`,
`chain_mechanics`, `manipulation_layers`, `status_interaction_table`'ın canlı okunması,
verb'lerin `crit_eligible`/`element_origin`/`engine_base_stats.damage_type` alanları) ya hiç
okunmuyor ya da (durum tablosu gibi) elle senkronlanmış bir C# kopyası olarak yaşıyor.
Detaylı döküm: `docs/durum.md` → "Bilinen açıklar" ikinci maddesi.

**Sırala:** Görev 0 **önce** ve **tek başına** biter (hepsi `SkillMotor.cs`'e dokunuyor,
paralel çalışırsa çakışır). Görev 0 bitince 1-7 **paralel** verilebilir — her biri kendi YENİ
dosyasını yazar, `SkillMotor.cs`'e bir daha dokunmaz, sadece `Skills.XxxYyy` gibi zaten
genişletilmiş bir property'yi okur.

---

### Görev 0 — SkillMotor'a eksik tüm parse'ları ekle (sıralı, tek ajan)

```
Rolün: element-sistemi.json'ı okuyan Core motor geliştiricisi.

ÖNCE OKU: docs/durum.md üstündeki "master ayrışması" notu (context için) + docs/durum.md
"Bilinen açıklar" 2. madde + unity/Assets/Scripts/Core/Grammar/SkillMotor.cs (TAMAMI —
ActiveModeNode + ParseActiveModes'u ÖRNEK AL, aynı MiniJson deseniyle yaz) +
docs/element-sistemi.json şu alanlar: verbs[].crit_eligible / element_origin /
engine_base_stats.damage_type, passives, chain_mechanics, status_interaction_table,
manipulation_layers.zone_layer.

GÖREV
1. VerbNode'a CritEligible (bool), ElementOrigin (string), DamageType (string) alanlarını
   ekle; ParseVerbs'te doldur; SkillResolution'a da taşı (ActiveModeNode'daki gibi ctor'a
   opsiyonel parametre ekle, mevcut çağrıları KIRMA).
2. Yeni PassiveNode struct'ı (Id, Element, TriggerCombo int[], DurationSec, Effects
   JsonValue + GetEffect/GetEffectBool) + ParsePassives(root, dst) — passives.list'i okur.
   SkillMotor'a `IReadOnlyList<PassiveNode> Passives` property'si ekle.
3. Yeni ChainNode struct'ı (Element, Pattern, Effect, Links float[], Finisher) +
   ParseChains(root, dst) — chain_mechanics.chains'i okur. `IReadOnlyList<ChainNode> Chains`.
4. Yeni StatusInteractionNode struct'ı (A, B, Name, Effect, ReadAs) + ParseStatusInteractions
   — status_interaction_table'ın TÜM kategorilerini (debuff_debuff, cc_debuff, cc_cc,
   buff_debuff — gerçek anahtar adlarını JSON'dan oku, uydurma) düz bir listeye toplar.
   `IReadOnlyList<StatusInteractionNode> StatusInteractions`.
5. Yeni ZoneNode struct'ı (Id, Element, Movement, DurationSec, opsiyonel Manipulation
   JsonValue) + ParseZones — manipulation_layers.zone_layer.zones'u okur.
   `IReadOnlyList<ZoneNode> Zones`, `int MaxActiveZones` (max_active_zones).

KABUL KRİTERLERİ
- `dotnet test` yeşil (mevcut 155 test kırılmadı)
- Yeni bir test: `motor.Passives.Count`, `.Chains.Count`, `.StatusInteractions.Count`,
  `.Zones.Count` gerçek JSON'daki sayılarla birebir eşleşiyor (uydurma sayı yazma, JSON'u
  say)
- Mevcut hiçbir public API imzası kırılmadı (ManifestationDirector vb. derlenmeye devam eder)

YASAKLAR
- Yeni bir JSON parser yazma — MiniJson zaten var, onu kullan
- Bu alanları herhangi bir yere UYGULAMA (director yazma) — bu görev sadece OKUMA katmanı,
  uygulama Görev 1-7'nin işi
- docs/element-sistemi.json'ı değiştirme

Bitirince dotnet test çıktısını özetle, hangi kabul kriterini doğrulayamadığını söyle.
```

---

### Görev 1 — DamageCalculator + crit sistemi (paralel, Görev 0 sonrası)

```
Rolün: hasar formülü yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json (formulas, crit_system, global_rules.weakness) +
unity/Assets/Scripts/Core/Combat/ClosingDamageMath.cs (MEVCUT hasar yolu — SİLME,
DamageCalculator ONUN YERİNE geçmiyor, PARALEL yazılıyor) + SkillMotor.cs'teki yeni
CritEligible/ElementOrigin/DamageType alanları (Görev 0).

GÖREV
1. Core/Combat/DamageCalculator.cs: formulas.damage'ı BİREBİR uygula:
   base_damage_value × adjective.damage_mult × length.damage_mult × (1-resistance) ×
   weakness_bonus, ardından crit_system (base_crit_chance, adjective_crit_bonus[adj.id],
   max_crit_chance ile sınırlı) × crit_multiplier. Deterministik test için Random ctor'da
   seed alsın.
2. `docs/durum.md`'ye net bir not düş: bu hesap ŞU AN hiçbir yerden çağrılmıyor
   (ManifestationDirector hâlâ ClosingDamageMath kullanıyor) — ikisini nasıl birleştireceği
   ayrı bir karar (sahibine sorulacak), bu görevin kapsamı değil.

KABUL KRİTERLERİ
- dotnet test yeşil, yeni testler: crit_system'deki 3 sıfat bonusunun (keskinlik,
  saflastirma, berraklik id'lerini JSON'dan oku) doğru toplandığını, max_crit_chance
  tavanının çalıştığını, weakness_bonus>1 durumunda hasarın arttığını kanıtlar.

YASAKLAR
- ClosingDamageMath.cs'e veya ManifestationDirector.cs'e dokunma
- SkillMotor.cs'e dokunma
```

---

### Görev 2 — ResourceTracker + CooldownTracker (paralel, Görev 0 sonrası)

```
Rolün: kaynak/soğuma sistemi yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json global_rules (resource_system, cooldown_rules,
status_durations, player_stats, boss_stats) + unity/Assets/Scripts/Core/Tuning/StatusTuning.cs
+ unity/Assets/Scripts/Game/PlayerVitals.cs (mevcut MaxHp nereden geliyor, KARŞILAŞTIR).

GÖREV
1. Core/Combat/ResourceTracker.cs: max_mana, regen_per_sec, regen_delay_after_cast_sec'i
   uygulayan basit bir havuz (Spend/Tick/CanAfford). Oyunda ŞU AN hiçbir yerde mana yok —
   bu görev sadece sınıfı yazar, PlayerVitals'a bağlamaz (ayrı bir karar, ManifestationDirector
   zaten base_resource_cost'u hiç düşmüyor, aynı gerekçe).
2. Core/Combat/CooldownTracker.cs: global_cooldown_sec + max_concurrent_casts'i uygulayan
   basit bir sınıf (verb id → soğuma bitiş zamanı).
3. StatusTuning.cs'teki sabit süre/magnitude değerlerini (StunMs, RootMs, vb.)
   global_rules.status_durations'daki karşılığıyla KARŞILAŞTIR — aynıysa dokunma, farklıysa
   docs/durum.md'ye fark listesi yaz (hangisi otorite olacağına sahibi karar verir, kendin
   değiştirme).

KABUL KRİTERLERİ
- dotnet test yeşil, ResourceTracker/CooldownTracker için ayrı testler
- docs/durum.md'de StatusTuning karşılaştırma tablosu (fark varsa)

YASAKLAR
- StatusTuning.cs'in sayılarını DEĞİŞTİRME, sadece karşılaştır ve raporla
- PlayerVitals.cs'e veya ManifestationDirector.cs'e bağlama
```

---

### Görev 3 — PlayerStateMachine (paralel, Görev 0 sonrası)

```
Rolün: durum makinesi yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json state_machine (player_states, boss_states) +
unity/Assets/Scripts/Core/Grammar/SentenceState.cs + SentenceEngine.cs (mevcut
SentencePhase enum'u state_machine'in player_states'iyle KARŞILAŞTIR, birebir örtüşmüyor).

GÖREV
1. Core/Combat/PlayerStateMachine.cs: state_machine.player_states'teki her state için
   (idle/drawing/casting/recovering/dodging/stunned/rooted/channeling/dead) can_draw/
   can_move/can_dodge/i_frames okuyan bir sınıf (MiniJson ile parse — SkillMotor'a
   ParseStateMachine ekle, motor.PlayerStates/BossStates property'si; bu görev SkillMotor'a
   dokunuyor AMA Görev 0'ın alanlarından FARKLI, çakışma riski düşük — yine de Görev 0
   bittikten SONRA başla).
2. SentencePhase ile state_machine.player_states arasındaki farkı docs/durum.md'ye yaz
   (ör. "drawing" state'i can_draw="partial" diyor, SentencePhase.Building'de tam açık).

KABUL KRİTERLERİ
- dotnet test yeşil, her state için can_draw/can_move/can_dodge/i_frames doğru okunuyor
- SentencePhase'e bağlama YAPILMADI (bu ayrı bir karar) — sadece yeni sınıf + fark raporu

YASAKLAR
- SentenceEngine.cs / SentencePhase enum'una dokunma
```

---

### Görev 4 — PassiveDirector (paralel, Görev 0 sonrası, Görev 0'ın Passives'ine bağımlı)

```
Rolün: pasif tetikleme sistemi yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json passives + unity/Assets/Scripts/Core/Combat/
ActiveModeDirector.cs (AYNI DESENİ TAKİP ET — trigger_combo eşleşmesi, cooldown yok ama
duration_sec var, effects JsonValue).

GÖREV
Core/Combat/PassiveDirector.cs: motor.Passives listesini alır, TryTrigger(dot dizisi,
worldMs) ile trigger_combo eşleşen pasifi bulur, DurationSec kadar aktif tutar (birden
fazla pasif AYNI ANDA aktif olabilir — ActiveModeDirector'dan farkı bu, tek mod değil liste).

KABUL KRİTERLERİ
- dotnet test yeşil: 12 pasifin hepsi doğru trigger_combo ile eşleşiyor, süresi dolunca
  düşüyor, iki pasif aynı anda aktif olabiliyor.

YASAKLAR
- ManifestationDirector.cs'e bağlama (bu görev sadece Core sınıfı)
- SkillMotor.cs'e dokunma (Passives zaten Görev 0'da geldi)
```

---

### Görev 5 — ChainDirector (paralel, Görev 0 sonrası, Görev 0'ın Chains'ine bağımlı)

```
Rolün: zincir mekaniği yazan Core geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json chain_mechanics.

GÖREV
Core/Combat/ChainDirector.cs: motor.Chains listesini alır, son N cast'in pattern'iyle
(chain_mechanics'teki "pattern" alanının formatını JSON'dan çöz, uydurma) eşleşeni bulur,
Links dizisindeki artan bonusu ve son eşleşmede Finisher'ı tetikler.

KABUL KRİTERLERİ
- dotnet test yeşil: 6 zincirin hepsi doğru pattern ile eşleşiyor, Links sırası doğru
  uygulanıyor, Finisher yalnızca tam zincirde tetikleniyor.

YASAKLAR
- Kombo tablosu yazma — pattern eşleştirme JSON'daki veriden türer, elle dizi yazılmaz
- ManifestationDirector.cs'e bağlama
```

---

### Görev 6 — StatusReactionTable'ı JSON'dan canlı oku (paralel, Görev 0 sonrası)

```
Rolün: durum etkileşim tablosu geliştiricisi.

ÖNCE OKU: unity/Assets/Scripts/Core/Status/StatusReactionTable.cs (MEVCUT — elle
kopyalanmış 14 kural) + docs/element-sistemi.json status_interaction_table + Görev 0'ın
motor.StatusInteractions'ı.

GÖREV
StatusReactionTable.cs'in statik elle-yazılmış listesini, motor.StatusInteractions'tan
türeyen bir listeye çevir (TryGetRule aynı imzada kalsın, StatusApplicator/StatusBoard
DEĞİŞMESİN). "a"/"b" alanlarındaki durum id'lerini StatusKindUtil.TryParse ile StatusKind'a
çevir; eşleşmeyen id varsa (yeni bir mechanic) docs/durum.md'ye yaz, sessizce atlama.

KABUL KRİTERLERİ
- dotnet test yeşil, TÜM StatusBoardTests kırılmadan geçiyor (mevcut testler JSON'dan
  gelen kurallarla da aynı sonucu vermeli — bu regresyon testidir)
- JSON'daki kural sayısı ile TryGetRule'un tanıdığı kural sayısı test edilir

YASAKLAR
- StatusBoard.cs / StatusApplicator.cs'e dokunma (imza aynı kalıyor, içerik kaynağı değişiyor)
- Yeni bir mekanik/kural İCAT ETME — sadece JSON'da olanı oku
```

---

### Görev 7 — ZoneDirector (paralel, Görev 0 sonrası, Görev 0'ın Zones'una bağımlı)

```
Rolün: kalıcı alan (zone) sistemi yazan geliştirici.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.zone_layer + docs/durum.md
"Toprak kalıcı alan" sorusunun cevabı (bu sohbette sorulmuştu) + unity/Assets/Scripts/Core/
Combat/StateBridgeBoard.cs (BENZER bir "dünyada yaşayan obje" deseni — örnek al).

GÖREV
1. Core/Layers/IZoneDirector.cs arayüzü (ZoneInstance struct: Id, Element, X, Y, Z,
   RadiusM, RemainingSec, Movement) + Core/Combat/ZoneDirector.cs (saf C#, TrySpawn/Tick/
   Remove/MoveZone, max_active_zones sınırı).
2. Bu SADECE Core mantığı — Unity'de görsel spawn ETMEZ (o ayrı bir görev, Game katmanı).

KABUL KRİTERLERİ
- dotnet test yeşil: max_active_zones dolunca en eskisi düşüyor, RemainingSec sıfırlanınca
  zone kayboluyor, "player_directed"/"follow_target"/"static" movement tipleri ayrı ayrı
  test edilir (davranış farkı en azından bir konum-güncelleme metodunda var olmalı).

YASAKLAR
- unity/Assets/Scripts/Game/ klasörüne dosya ekleme (bu görev saf Core)
- ManifestationDirector.cs'e bağlama
```

---

---

### Görev 8 — SpaceDirector: mevcut hardcoded geçişleri space_layer'a bağla (paralel, Görev 0 sonrası)

```
Rolün: uzay/geçiş mekaniği geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.space_layer + unity/Assets/Scripts/
Core/Combat/SkillMotionMotor.cs (MEVCUT — Hava+Su Zenitsu kesişi, Hava+Su+Toprak işaret
mekaniği ŞU AN ELLE rün kontrolüyle yazılı, space_layer JSON'undan HİÇ okumuyor) +
StateBridgeBoard.cs.

GÖREV
1. SkillMotor'a space_layer.effects'i okuyan SpaceEffectNode + ParseSpaceEffects ekle
   (Id, Element, Type, DistanceM, IFrameMs, DamageOnPass bool, DamageOnCross float,
   DurationSec — alanlar effect tipine göre değişiyor, JsonValue ile opsiyonel oku).
2. SkillMotionMotor.cs'teki mevcut sabit sayıları (distance_m, i_frame_ms vb.) motor.
   SpaceEffects'ten gelen değerlerle KARŞILAŞTIR; aynıysa dokunma, farklıysa
   docs/durum.md'ye yaz (hangisi otorite sahibi karar verir).
3. "Karabasan"/"Hiçlik" gibi henüz karşılığı olmayan space effect'leri (invisible_link,
   tear) için YENİ mekanik YAZMA — sadece motor.SpaceEffects listesinde veri olarak
   dursunlar, docs/durum.md "Bilinen açıklar"a "şu N effect JSON'da var, oyunda karşılığı
   yok" diye ekle.

KABUL KRİTERLERİ
- dotnet test yeşil, motor.SpaceEffects.Count JSON'daki sayıyla eşleşiyor
- SkillMotionMotor'un MEVCUT davranışı (Zenitsu/işaret) hiç değişmedi (regresyon yok)

YASAKLAR
- SkillMotionMotor.cs'in mevcut çalışan mantığını SİLME/yeniden yazma — sadece karşılaştır
- StateBridgeBoard.cs'e dokunma
```

---

### Görev 9 — TimeDirector: gecikme/yankı/kalıcılık (paralel, Görev 0 sonrası)

```
Rolün: zaman katmanı geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.time_layer.

GÖREV
Core/Combat/TimeEffectDirector.cs (saf C#, mevcut Core/Time/ klasöründeki yavaş-çekim
yönetmeninden AYRI — o dünya saatini yönetiyor, bu belirli bir cast'in gecikmeli/yankılı
davranışını): time_layer.effects'teki 4 tipi uygula:
- delayed_detonation (Karabasan): cast X saniye sonra patlar
- echo (Alev): cast'ten damage_ratio kadar bir "yankı" hasarı delay_sec sonra tekrar gelir
- extend_lifetime (Lav): bir zone/effect'in RemainingSec'i multiplier ile çarpılır
- death_delay (Cehennem): ölüm anı delay_sec ertelenir (PlayerVitals/BossVitals'a
  BAĞLAMA — bu görev sadece "ne zaman tetikleneceğini" hesaplayan saf mantık, gerçek
  ölüm/patlama uygulaması Faz 6 "Bağlama" turunun işi)

KABUL KRİTERLERİ
- dotnet test yeşil: 4 tip için de zamanlama hesabı (worldMs bazlı) doğru test edilir.

YASAKLAR
- PlayerVitals.cs / BossVitals.cs / Core/Time/*.cs'e dokunma
```

---

### Görev 10 — RealityDirector: dirilme engeli + silme (paralel, Görev 0 sonrası)

```
Rolün: gerçeklik katmanı geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json manipulation_layers.reality_layer.

GÖREV
Core/Combat/RealityEffectDirector.cs: revive_block (Cehennem, dirilmeyi N sn engeller —
PlayerVitals'ın respawn akışına BAĞLAMA, sadece "engelli mi" bayrağını hesapla),
partial_erase (Karabasan: shield/haste/damage_reduction hedeflerini siler — bu targets
listesi zaten StatusKind'ta var, StatusBoard üzerinde çalışacak bir yardımcı yaz AMA
StatusBoard.cs'e DOKUNMA, dışarıdan Board.CleanseHostile benzeri yeni bir public metod
gerekiyorsa StatusBoard'a EKLE sadece, mevcut metodu değiştirme), full_erase (Hiçlik:
targets = minions/summons/shields — **oyunda minion/summon sistemi YOK**, bu görevin
kapsamı yalnızca "shields" kısmını uygular, diğer ikisini docs/durum.md'ye "sistem yok,
uygulanamaz" diye açık olarak yazar; UYDURMA minion sistemi kurma).

KABUL KRİTERLERİ
- dotnet test yeşil: partial_erase şeffaf/haste/damage_reduction'ı doğru temizliyor,
  full_erase shield'ı temizliyor, revive_block bayrağı süresi doğru hesaplanıyor.

YASAKLAR
- Minion/summon sistemi icat etme
- PlayerVitals.cs'in respawn akışına bağlama (Faz 6 işi)
```

---

### Görev 11 — Equipment sistemi: veri modeli + element eşleşme bonusu (paralel, Görev 0 sonrası)

```
Rolün: ekipman veri modeli geliştiricisi.

ÖNCE OKU: docs/element-sistemi.json equipment_system (TAMAMI — küçük bölüm).

GÖREV
1. Core/Equipment/EquipmentSlot.cs (enum: Weapon, Armor, Accessory) +
   Core/Equipment/EquipmentItem.cs (Id, Name, Slot, Element) — equipment_system.examples'
   taki 6×3=18 örneği SkillMotor deseniyle JSON'dan okuyan bir parser (yeni
   Core/Equipment/EquipmentCatalog.cs, MiniJson kullan).
2. Core/Equipment/EquipmentBonusResolver.cs: element_match_bonus ("+%10 etki") — silahın
   Element'i cast edilen skill'in element'iyle eşleşirse %10 bonus döner (JSON'daki
   "+%10" metninden yüzdeyi türet, SkillMotor.ParseDefenseDropMult'taki sayı-çıkarma
   desenini örnek al, elle "10" yazma).

KABUL KRİTERLERİ
- dotnet test yeşil: 18 örnek item doğru parse ediliyor, eşleşen/eşleşmeyen element için
  bonus doğru (1.1 / 1.0) dönüyor.

YASAKLAR
- Envanter UI'ı yazma (oyuncunun ekipman SEÇMESİ ayrı, çok daha büyük bir görev — bu görev
  sadece veri + hesap katmanı; oyuncunun şu an sabit/tek bir ekipmanı olduğunu VARSAY)
- PlayerVitals.cs veya ManifestationDirector.cs'e bağlama
```

---

### Görev 12 — UI Rules hizalaması (paralel, Görev 0 sonrası, Game katmanı)

```
Rolün: HUD geliştiricisi (bu görev Core değil Game — Unity gerekli).

ÖNCE OKU: docs/element-sistemi.json ui_rules + unity/Assets/Scripts/Game/PentagonView.cs
+ ReactionReadout.cs + Core/Tuning/FeelTuning.cs (ReadoutHoldMs — read_as_display.
duration_ms=1500 ile KARŞILAŞTIR).

GÖREV
1. `read_as_display.duration_ms` (1500) ile `FeelTuning.ReadoutHoldMs`'in mevcut değerini
   karşılaştır; farklıysa docs/durum.md'ye yaz (değiştirme, sahibi karar versin).
2. `cooldown_display` (radial_overlay, her rünün etrafında, sayı göster): PentagonView'daki
   her nokta için `base_cooldown_sec` dolana kadar dairesel bir dolum efekti + kalan saniye
   sayısı çiz. Şu an hiçbir cooldown UI'da görünmüyor — bu görev sadece GÖRSEL, cooldown'un
   GERÇEKTEN engellemesi Görev 2'nin (CooldownTracker) Faz 6'da bağlanmasına bağımlı; bağlı
   değilken bile dolum animasyonu kozmetik olarak (her cast'te base_cooldown_sec kadar
   dolar, hiçbir şeyi engellemez) eklenebilir.
3. `zone_display` (in_world, transparency 0.6): Görev 7 (ZoneDirector) henüz Game'e
   bağlanmadığı için bu maddeyi sadece docs/durum.md'ye not düş, kod yazma.

KABUL KRİTERLERİ
- Unity Play mode'da her rün noktasının etrafında dairesel cooldown dolumu görünüyor
  (Unity MCP ile canlı doğrula)
- dotnet test yeşil (bu görev Game katmanı olsa da Core testleri kırılmamalı)

YASAKLAR
- CooldownTracker'ı gerçekten cast'i ENGELLEYECEK şekilde bağlama (Faz 6 işi)
- ZoneDirector'ı Unity'ye bağlama (Görev 7 henüz Core'da, Game'e geçmedi)
```

---

**Not (auto mode kullanan ajanlara):** Görev 0 bitip merge olmadan 1-12'yi başlatma — hepsi
`Skills.Passives`/`Skills.Chains`/`Skills.StatusInteractions`/`Skills.Zones`/
`Skills.SpaceEffects`/yeni Verb alanlarını okuyor, onlar yoksa derlenmezler. Her görev kendi
dalında, `dotnet test` yeşilse kendi merge eder (AGENTS.md). **Hiçbiri henüz gerçek oynanışa
BAĞLANMIYOR** — bu bilinçli: 13 görevin bağlama sırası birbirine karışırsa (ör. CooldownTracker
cast'i engellemeye başlarken ResourceTracker henüz yokken oyun kilitlenebilir) hata ayıklaması
imkânsızlaşır. Hepsi (Görev 0-12) bitip birleşince **"Faz 6 — Bağlama"** turu yazılacak: her
birini tek tek, sırayla, `ManifestationDirector`/`PrototypeBootstrap`'a gerçekten bağlayıp
Unity Play mode'da canlı doğrulayan görevler. O zaman JSON'un `equipment_system`/`ui_rules`/
`manipulation_layers`'ının TAMAMI da dahil olmak üzere gerçekten oyunda çalışıyor olacak —
sahibinin talebi budur, "motor değil ama JSON'da var, atlanır" diye bir kategori artık yok.
