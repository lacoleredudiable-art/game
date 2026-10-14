# Tasarım belgeleri — tek kaynak

Bu klasör oyunun tasarım kaynağıdır. Kod bu belgelerin uygulamasıdır; kodla belge çelişirse **belge kazanır**,
kod düzeltilir. Belgede olmayan bir davranışı kod uydurmaz: kaynakta yoksa PR'da "kaynakta yok" diye yazılır
ve kullanıcıya sorulur.

**İş sırası:** `14-kodlama-plani.md` (aşamalar; her aşama telefonda görülen bir gelişmeyle biter). Ayrıntılı kusur
listesi: `12-kusur-envanteri.md`. `11-onarim-plani.md` arşivdir (14 Ekim'de `14` yerine geçti).

## Öncelik sırası (çelişkide üstteki kazanır)

1. `09-sonraki-kararlar.md` — v4.5'ten sonra kullanıcının verdiği kararlar (en yeni).
2. `01-kural-kitabi-v4.5.md` — 12 fiil, 12 sıfat, **144 hücre** (Bölüm 5), 10 silah × fiil görünümü (Bölüm 6).
3. `02-dunya-fizigi.md` — ağırlık, menzil, mermi/alan/yakın vuruş, itme, tasma, işaret, yapı, zaman kaydı, boss.
4. `03-fizik-komutlari.md` — kural motoru → 29 komut → fizik eşlemesi.
5. `04-silahlar.md`, `05-silah-fiil-120.md` — silah menzili, ritim, yolculuk hızı, 120 görünüm hücresi.
6. `10-olculer.md` — kural dışı bütün ölçüler: karakter, silah boyu ve tutuşu, animasyon evreleri, dodge,
   kamera, boss, arena, sanat yönü, efekt sınırları, ses, ekran, performans, asset hattı, oyun akışı. Her
   maddenin kimliği (`O-x.y`) ve doğrulama yolu var. §18'deki çift değerler bu belgenin değerine bağlanır.
7. `06-animasyon-kurallari.md`, `07-efekt-motoru.md` — görünüm (oyun sonucunu değiştirmez).
8. `08-acik-sorular.md` — karar verilmemiş maddeler (bunlara göre kod yazılmaz; sorulur).

Sıralamanın dışında, **üstünde**: `13-vizyon-ve-cerceve.md` — kim oynar, ne kadar, zorluk, yetenek katmanı,
zaman ekonomisi; [K]/[Ö]/[AÇIK]/[RED] etiketli. Yalnız **[K]** satırları karardır ve yukarıdaki sırada 1 ile aynı
güçtedir; **[Ö]** satırları öneridir, koda girmez. Bu belgedeki hiçbir madde dilime kendiliğinden girmez.

Sayılar `unity/Assets/Resources/RuleEngineV4/kural-motoru-v4.json` içindedir; belgedeki sayılar başlangıç
değeridir, JSON'daki güncel değer geçerlidir.

## Ajanlar için okuma kuralı

- Bir kombo üzerinde çalışırken **önce o hücreyi oku**: `01-kural-kitabi-v4.5.md` Bölüm 5'te `| <fiil>-<sıfat>`
  satırı (ör. `| 3-9 `), sonra Bölüm 6'da silahın satırı, sonra `09-sonraki-kararlar.md`'de o hücreye dair karar.
- PR açıklamasında üzerinde çalışılan hücrenin numarasını ve metnin ilgili cümlesini yaz.
- Hücre metniyle uyuşmayan davranış "tasarım tercihi" diye eklenmez; kullanıcıya sorulur.
- Ölçü, kamera, dodge, arena, görünüm işinde `10-olculer.md`'deki madde kimliğini PR'a yaz; test adına da koy
  (ör. `O_7_2_ArenaSiniri_18m`). Belgede değeri olmayan ölçüyü uydurma; kullanıcıya sor.
