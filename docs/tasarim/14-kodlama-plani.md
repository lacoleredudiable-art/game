# 14 — Kodlama planı (aşamalar)

Sürüm 1.0 · 14 Ekim 2026 (Mustafa onayladı) · Kullanıcı: Mustafa · master `3c4c345` (#183 sonrası)

Bu plan `11-onarim-plani.md`'nin yerine geçer. Eski plan kurallar (`13`, yeni `AGENTS.md`/`CLAUDE.md`)
konmadan önce çıkmıştı: her PR tek küçük düzeltme ve her PR'da ~30 dk yerel test (O_ PlayMode + Gözcü + APK).
Bu planda iş **aşamalara** bölünür; **her aşama telefonda görülen bir oyun gelişmesiyle biter ve telefon testi
yalnız aşama sonunda yapılır.**

Kaynaklar: `13` [K] satırları, `09` [K] kararları, `10` ölçüleri, `12` envanterinin kalan maddeleri. Bu planda
yeni davranış ya da sayı uydurulmaz; her aşama belgedeki bir maddeye bağlıdır (madde kimlikleri parantezde).

---

## 1. Test düzeni [K] (14 Ekim)

| Ne zaman | Ne koşulur | Kim / nerede |
|---|---|---|
| Aşama içinde, her PR | CI: OYUN.md kontrolü, CoreTests, IntegrationTests, GameCompile (değişmedi) + değişen alanın `dotnet test --filter` testi | ajan, bulutta; telefon yok |
| Aşama sonunda, bir kez | O_ PlayMode + Gözcü duman testi (`\.O_;GozcuSmoke`), dilim APK'sı | Mustafa'nın bilgisayarı |
| Aşama sonunda, bir kez | **Telefon testi**: aşamanın "Telefonda göreceğin" listesi + logcat | Mustafa |

- Her aşamanın kendi dalı olur (`asama/N-<ad>`). Aşama içindeki PR'lar bu dala girer; master'a **aşama başına bir
  kez**, telefon testi geçince girer. Master'ın koruması (O_ + Gözcü sonucu yapıştırılmadan merge yok) aynen kalır,
  yalnız PR başına değil aşama başına uygulanır.
- Aşama içindeki PR'lar hâlâ tek iş yapar (inceleme kolay olsun), ama her biri ayrı telefon testi istemez.
- Telefon testi başarısızsa aşama dalında düzeltilir; master bozulmaz.
- Gözcü önce/sonra sayısı aşama PR'ında bir kez yazılır.

Bu düzen için değişen satırlar (planla aynı PR'da):
- `AGENTS.md` Çalışma biçimi / Testler: "her PR'dan önce O_ + Gözcü" → "her aşama sonunda"; "oynanışı değiştiren her
  PR'da APK kaydı" → "her aşama PR'ında".
- `CLAUDE.md`: "Her oturum tek görev" kalır; görev = aşamanın bir maddesi ya da aşamanın tamamı.
- `00-OKU-BENI.md` ve `13` başlığı: "İş sırası `11`" → "`14`". `11` arşiv notuyla kalır.

---

## 2. Bitmiş olanlar (envanterde işaretsiz duranlar)
**B2** silah değişince yürüme klibi (#181), **C1** düz vuruş yerinde (#177), **D1** kamera boss yakını (#179).
`12`'de işaretlendi.

---

## 3. Aşamalar

Sıra, telefonda en çok fark edilecek olandan en az bağımlılığı olana doğru. Her aşama bir öncekinin üstüne kurulur.

### Aşama 1 — Dodge: "Kalahan affetmez, dodge affeder"
Kaynak: `13` §3 [K] (dodge'a bağımlı boss; mükemmel dodge [RED]), `09` [K] i-frame, `10` O-4.4–O-4.11.
1. Mükemmel/kusursuz dodge kodda kaldırılır: hak iadesi, ×1,3 vuruş, 0,2 s ağır çekim, özel ses/titreşim
   (`PerfectDodgeRule`, `ExchangeResolver`, `DodgeTuning`, `FeelTuning`, `CombatFeelDirector`, panel). Belgede
   O-4.8, O-10.5, README satırı, `ARCHITECTURE.md` O2 silinir (A-13.13'ün kod/belge listesi bu PR'da çıkar).
2. İki faz: patlama 0,25 s + süzülme 0,40 s (O-4.4a; `GlideTailMs` 120 → 400) — envanter D2.
3. Takla ayağı zemin üstünde (O-4.5; Gözcü `ayak_zemin_alti` 0), iz yalnız süzülmede ve arkada (O-4.10).
4. I-frame görünür: kor turuncusu parlama (O-4.6a), Hareket skillerinin yolculuğunda da (`09` [K] 12 Ekim).

**Telefonda göreceğin:** dodge'a bir bas → yay çizen zıplama, hızlı çıkış + yavaşlayan süzülme; havadayken tekrar
bas → takla, ayak yere gömülmüyor; kaçış boyunca karakterin kenarı turuncu parlıyor; boss'un Bacak Çakması
dairesinden dodge ile geçince hasar yok; artık "kusursuz dodge" ağır çekimi ve hak iadesi yok.

### Aşama 2 — Temiz ekran: eski motor dilimden çıkıyor
Kaynak: `11` Adım 3–4 (K3, K4), envanter B3, C2, C3, C4, D9, D10, Y12, Y14, Y15, Y16.
1. `MechanicWorldRuntime` dilimde alan/duvar üretmez (C2); eski motorun ezdiği değerler okunmaz (C3).
2. Kalan sessiz yedekler `[V4-FALLBACK]` yazar, yedek görüntü oynatmaz (B3; Y12, Y14 MeshCollider, Y15, Y16).
3. Boss efektleri ekranın %25'inden fazlasını kaplamaz (O-9.1, D10).
4. Yürürken minyon ve koza da engel (D9).
5. Bağımlılık haritası: dilimin eriştiği eski motor dosyası sayısı önce/sonra (C4).

**Telefonda göreceğin:** 3 dk Kılıç + Yay ile dövüş; ekranı kaplayan dev efekt yok, kamera hiçbir duvarın içine
girmiyor, minyon ve kozanın içinden yürünmüyor; logcat'te `[V4-FALLBACK]` satırı 0.

### Aşama 3 — Deneme döngüsü: öl, kazan, tekrar dene
Kaynak: `13` §3 [K] (diriliş yok, zor zindan, tekrar deneme), `10` O-17.1–O-17.3, Y19.
1. Boss uyuyor, 6 m'ye yaklaşınca ya da vurunca 2 s'de uyanır (O-17.1).
2. Ölünce 2 s → "Tekrar" / "Silah değiştir"; tekrar 3 s içinde arenada (O-17.2).
3. Boss ölünce 2 s sonra sonuç: süre, skill sayısı, en çok hasar veren kombo, deneme sayısı; boss canı yüzdesi
   ölüm ekranında da büyük yazılır (O-17.3; `13` §5 "boss canı büyük yazılır" [Ö] ise onay gerekir).
4. Test APK'sında ölümsüzlük (DevHp, Y19) varsayılan kapalı, panelden açılır.

**Telefonda göreceğin:** gerçek bir deneme: boss uyanıyor, ölebiliyorsun, ekranda "Tekrar" çıkıyor ve 3 s'de
yeniden dövüşüyorsun; boss'u öldürürsen sonuç kartı ve deneme sayısı.

### Aşama 4 — Skiller kurala göre I: Zarar, Şifa, Hareket, Koruma
Kaynak: `09` [K] (Hareket kimseye vurmaz, Koruma saldırmaz, hasar ritimle bir kez), envanter D3, D4, Y9, Y11, Y17.
1. Ritim çarpanı bir kez, Yoğun şarjı bir kez, Yoğun Koruma 0,45 s (D3).
2. Hareket ve Koruma hücrelerinden silah teslimi kalkar; Hareket yolculuğu i-frame (D4).
3. Boss sayıları belgeye: zehir tiki küsuratı (Y9), Zehir Nefesi konisi 8 m / 60° (Y11, O-6.17).

**Telefonda göreceğin:** kontrol listesi (her satır: çiz → gör): 1-1, 1-3, 2-x Şifa, 3-1/3-3/3-9 Hareket
(vurmuyor, yolda turuncu parlıyor, 3-9 yalnız Yay'da ip), 4-x Koruma (duruş, saldırı yok), hasar sayıları
JSON'daki değer.

### Aşama 5 — Skiller kurala göre II: Kontrol, Arındırma, Güdümlü
Kaynak: `09` "Bilinen sapmalar", envanter D5, D6, D7.
1. `KontrolUygula` + `TasmaBagla` yürütücüde (6-x): boss'un işini keser, sana tasma.
2. `EtkiSok` (9-1…9-6): dosttan zararlı etki (yavaşlama, zehir) söker.
3. `Kilitlen` (x-6): Güdümlü kilit, 8 s tavanı.

**Telefonda göreceğin:** Kontrol çiz → hedefin o anki işi kesiliyor (boss'ta irade halkası kuralıyla, `01` §1.6)
ve aranızda tasma görünüyor;
Ağ Tükürüğü yavaşlatınca Arındırma ile yavaşlama kalkıyor; Güdümlü atış dönen hedefi takip ediyor.

### Aşama 6 — Sabit yapıları
Kaynak: `01` §1.12, envanter D8.
Dilimdeki 6 hücre: 1-4 dikenli direk, 2-4 şifa kaynak, 3-4 sıçrama taşı (bugün katı küp), 4-4 koruma duvarı,
6-4 kontrol tuzak, 9-4 arınma taşı. Yapı sınırı ve ömrü `01` §1.12.

**Telefonda göreceğin:** x-4 kombolarıyla yere 6 s'lik yapı kuruluyor ve hücre metnindeki gibi çalışıyor:
direğin 1 m içindeki boss saniyede bir hasar alıyor, şifa kaynağında can doluyor, sıçrama taşına basınca dikildiği
yöne fırlıyorsun, duvar Ağ Tükürüğü mermisini tutuyor, tuzağa basan minyon kökleniyor.

### Aşama 7 — Zaman ekonomisi: silah değiştirerek iptal ve Kılıç zinciri
Kaynak: `13` §9 [K] (iki silah animasyon iptali, zor kombo), O-4.12, O-4.14, Y24, D13.
**Kararlar (16 Ekim, `13` §9 ve §14):** dodge her fazda keser, hak/CD iadesi yok, toparlanma silah değişimiyle kesilir (düz vuruşla kesilmez), skill düz vuruşu vuruş karesinden sonra keser. Kalan [Ö] (silah değişiminin kendi toparlanması, weave sayacı, kuyruk önceliği) kodlanacak varsayılan; telefonda beğenilmezse değişir.
Kılıç `Kilic_VUR_b`, `_c` klipleri gelmeden 3 kesiş zinciri yapılmaz.
1. Toparlanma silah değişimiyle kesilir (onaylandı, 16 Ekim).
2. Girdi tamponu 0,15 s (O-4.12); düz vuruş sırası (Y24).
3. Kılıç 3 kesiş zinciri (klipler gelince).

**Telefonda göreceğin:** Kılıç skilli biterken Yay'a geçince toparlanma kesilip Yay skilli hemen başlıyor;
art arda dokunuşta kesişler sırayla geliyor.

### Aşama 8 — Ayar masası
Kaynak: `11` Adım 6, Y4, Y10, Y18, D11 (ortam ışığı).
Görünüş/his ayarları telefonda kaydırıcıyla, A/B, Kaydet, Dışa aktar.

**Telefonda göreceğin:** yürüme hızını, kamera açısını ve dodge süresini telefonda değiştirip kaydedebiliyorsun;
uygulamayı kapatıp açınca değer kalıyor.

---

## 4. Arka plan işi (telefon testi istemez, aşamaların yanında yürür)
- Y20 tek oyun saati (Gözcü'yü deterministik yapar), D12 batchmode'da takılan 3 test. Hangi aşamanın dalında
  yürüyeceği o aşama başında söylenir.

## 5. Bu planda olmayan (karar bekliyor, sonra)
- Boss'un `13`'e göre yeniden tasarımı (3 faz, tek atma mekanikleri, hedef kuralı sütunu, planlı %5, wipe sebebi
  ekranı): boss tasarım oturumu + A-13.1, A-13.2, A-13.14.
- Çok oyunculu (O-14.x): A-13.9, A-13.10.
- Parry (Koruma), just-frame (Tetikli), kombo kademesi: `13` §8–9 [Ö].
- Zaman fiili 2 rünlü hücreleri: `09` [AÇIK].
