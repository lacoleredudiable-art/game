# v4.5'ten sonraki kararlar (10–12 Ekim 2026)

Bu dosya `01-kural-kitabi-v4.5.md`'den sonra verilen kullanıcı kararlarını tutar ve onlarla çelişen eski
satırların yerine geçer. Her madde **[K]** (kullanıcı kararı) ya da **[AÇIK]** (karar bekliyor) etiketlidir.

## Girdi ve çizim

**[K] Hedef seçme (kural kitabı 1.1'i inceltir).**
1. Parmak 2. noktaya girdiği an (çift ründe aynı noktaya döndüğü an) kombo tanınır; harf flaşı ve ses o an
   çıkar; ön süre o an başlar.
2. Parmak 0,2 s içinde kalkarsa otomatik hedef; skill ön süre bitince çıkar.
3. Parmak 0,2 s'den uzun basılı kalırsa hedef seçme açılır: parmak ekranda nereye kayarsa (canlı, yapı ya da
   boş nokta) o hedef olur; yerde skillin şekli önizlenir.
4. Çıkış anı = en geç(ön süre bitişi, parmak kalkışı).
5. İptal: hedef seçerken parmak altıgenin merkezine geri getirilip kaldırılırsa skill atılmaz; mana ve
   cooldown harcanmaz. Tek noktada merkezde kaldırma da iptaldir.
6. Hedef seçerken karakter yürür.
Bu kural Yansıma'yı dosta sürüklemeyi, Yayılan'da merkez seçmeyi, Fedakarlık'ta seçilen dostu ve başka
oyuncunun zincirini seçmeyi kapsar.

**[K] Çift rün (aynı rünü fiil ve sıfat olarak çizmek, ör. `SIL-ZUL`).** Parmak noktadan çıkar, başka hiçbir
rün noktasına girmeden aynı noktaya geri girer. Ayarlar (`girdi`): çıkış mesafesi = nokta yarıçapı × 1,5;
çıkıştan dönüşe en çok 0,6 s. Kenarda titreyip hemen geri girmek çift rün sayılmaz. Dönerken başka noktaya
değilirse ilk değilen nokta sıfattır.

**[K] Üçüncü nokta** 2 rünlü dönemde yok sayılır (ilk iki nokta geçerli). **Çizerken yürüme** serbesttir.

**[K] Hedef kilidi:** kilitli düşman menzildeyse otomatik "en yakın düşman" seçimi onu alır; kendi ölçüsü olan
sıfatlar (Odaklı) kilide bakmaz.

## Skill hücreleri

**[K] 9-9 İşaretli-Arındırma (`SIL-ZUL`)** = seçilen yere/dosta/düşmana işaret koyar (kural kitabı 9-9 aynen).
Başka hiçbir kombo işaret koymaz.

**[K] 3-9 İşaretli-Hareket (`SHEV-ZUL`)** = işarete yol alarak çekilme. Yay'da ip atıp Levi gibi süzülme; diğer
silahlarda kendi görünümü (kural kitabı 4.3 / K 20:35: Çekiç Thor uçuşu, Kalkan ≤3 m omuz hücumu, Kılıç
Zenitsu şimşek atılışı, Asa ışında süzülme, Küre patlama itişi, Tılsım mühür çekmesi, Top gülle gibi, Kitap
sayfa akışı, Yumruk kısa atılış). İşaret yoksa skill boşa gider.

**[K] İp ve uçuş yalnız 3-9'dadır.** Yay'ın diğer Hareket kombolarında ip, ok ya da uçuş yoktur.

**[K] 3-3 Sıçrayan-Hareket (`SHEV-SKAR`)** = kural kitabı 3-3 aynen: en yakın düşmana silah menzili kadar
atılırsın, sonra sonraki en yakın düşmana menzilin ×0,5'i kadar, iki kez. Geri sıçrama DEĞİLDİR; Levi/ip
görünümü DEĞİLDİR.

**[K] Hareket fiili kimseye vurmaz.** Hareket hücrelerinde silah teslimi (kılıç vuruşu, ok) yoktur; yalnız
kendini taşıma vardır (kural kitabı 2: "Hareket yalnız kullanıcıyı taşır").

**[K] Koruma fiili saldırmaz.** Koruma hücrelerinde silah teslimi yoktur; yalnız savunma duruşu (2.1) ya da
Sabit'te duvar vardır.

## Silah ve sayılar

**[K] Silah = menzil + ritim + tarz** bilinçli tercihtir; silah başına ayrı mekanik kanun yoktur.

**[K] Taban sayılar** `kural-motoru-v4.json` → `scale` bloğundadır (mana 100, skill bedeli 25, cooldown 4 s,
dolum 10/s; can ve hasar tabanı aynı blokta). Hasar silahın ritim çarpanıyla **bir kez** ölçeklenir.

## Hasarsızlık ve süper zırh (12 Ekim)

**[K] I-frame (hasarsızlık penceresi):** saldırı içinden geçer, hiç hasar alınmaz.
- Dodge: tek basış 0,26 s, takla 0,52 s (`10-olculer` O-4.6). **Güncel:** test değeri `T_tek` 0,5 s / `T_zincir` 1,3 s, kesin değer ayar masasında (`15` §3.4, [K] 14 Ekim). Pencere boyunca karakterde kor turuncusu parlama
  görünür (O-4.6a).
- **Hareket fiili skilleri yolculuk boyunca hasarsızdır** (kalkıştan varışa; 3-3 sekmeleri, 3-9 çekilmesi dahil).
  Hareket kimseye vurmaz; karşılığı doğru zamanda kullanılınca saldırıdan kaçırmasıdır.

**[K] Süper zırh:** hasar alınır ama sendeleme ve kesilme olmaz; animasyon sürer.
- Yalnız ağır silahlar (Çekiç, Kalkan), yalnız skill ön süresinde. Hafif silahlarda (Kılıç, Yay ve diğerleri) yok.
- 9 Ekim'de silinen AS-03 bu kapsamla geri gelir; "yakın-dodge iptali" kısmı silinmiş kalır.
- Süper zırhtayken karakter kenarında ince kül grisi parıltı (kesilmeyeceği görünsün).

## Açık (karar bekliyor)

**[AÇIK] Zaman fiili.** Karar: "zaman alanı ve ışınlanma 3–4 rünlü kombolara kaldı." Zaman fiilinin 2 rünlü
12 hücresi (geri sarma, kayıt direği; kural kitabı 5.12) de mi kalktı, yoksa yalnız zaman alanı mı? Kodda şu an
Zaman `min_combo_len: 3` ile 2 rünlü kombolarda kapalı. Kullanıcı netleştirene kadar dokunulmaz.

**[AÇIK] Rün 7, 8, 10, 12 sıfat eşleşmesi** (Tetikli, Fedakarlık, Çizgisel, Yörüngeli) kural kitabında "A'nın
önerisi" olarak geçiyor (A-20); kullanıcı onayı kayıtlı değil.

## Bilinen sapmalar (12 Ekim kod incelemesi, düzeltilecek)

Dilimin 36 kombosu planlanıyor ama yürütücü bazı komutları çalıştırmıyor ve bazı sayılar yanlış:
- `KontrolUygula`, `TasmaBagla`, `EtkiSok`, `Kilitlen` komutlarının Unity yürütücüsünde karşılığı yok:
  Kontrol rünü (6-x) kesmiyor/tasmalamıyor, Arındırma (9-1…9-6) sökmüyor, Güdümlü kilidi uygulanmıyor.
- Ritim hasar çarpanı iki kez uygulanıyor (planlayıcı + yürütücü).
- Yoğun şarjı iki kez bekleniyor (ön süre zaten şarjı içeriyor).
- Yoğun Koruma 0,45 × 0,5 = 0,225 s oluyor; kural 0,45 s.
- Hareket ve Koruma hücrelerine silah teslimi ekleniyor (yukarıdaki iki [K] kuralına aykırı).
- Sabit (x-4) yalnız yapı + anlık etki; diken tiki, şifa kaynağı, duvar, tuzak, sıçrama taşı yok.
- Güdümlü Zarar'da 3 kilit vuruşu, İşaretli'de zincir yok.
