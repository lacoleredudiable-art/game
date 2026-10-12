# Bölüm 8: Açık sorular (yalnız açık maddeler: AS-09, AS-11, AS-17, AS-25, AS-34, AS-36, AS-37)

*Kaynak: `karakterler/v4/acik-sorular.md`*

## Açık sorular: v4 belgeleri arası çelişki, eski metin ve boşluk listesi

*9 Ekim 2026 ~21:17 (UTC+3). Hazırlayan: A (executor). K 21:05–21:17b ile 17 madde + A-13 kapandı. Kaynaklar yalnız `v3-kararlar.md`, `v4/` ve `run-ajanlari/BRIEF.md`; `arsiv/` kullanılmadı. Satır numaraları bu saatteki dosyalara göredir; `animasyon-kurallari.md` ve `efekt-motoru.md` bu sırada animatör tarafından değiştiriliyordu (20:45–20:50), satırlar kayabilir.*

**Bu dosya karar vermez.** Her maddede "Önerilen çözüm" yalnız öneridir; karar kullanıcınındır. Kullanıcının kuralı gereği her değişiklik önerisinde bedel (yeniden yazılacak yer, gün) yazıldı; gün tahminleri kabadır [tahmin].

**Öncelik:** 🔴 kodu ya da sabahki animasyon işini durdurur · 🟠 kod başlamadan karar gerekir · 🟡 belge temizliği / dengeleme.

**Sayı:** 37 madde; **30 kapandı** (21:30'da A belge temizliğiyle 13 madde daha: AS-02, 05, 06, 10, 16, 19–24, 30, 31). Kalan 7: AS-09, 11, 17, 25, 34, 36, 37. Önceki sayım: **17 kapandı** (K 21:05–21:17b / K 20:37–20:42). Kalan açık ≈ 20 (🔴 0 · 🟠/🟡 karışık). A-13 de kapandı (K 21:12).


**AS-09 🟡 Küre ve Top Hareket'i birbirine benziyor.**
- Nerede: `silah-fiil-120.md:87` (Küre "patlamanın itişiyle yol boyunca savrulur"), `:121` (Top "patlama seni yol boyunca fırlatır"). GameDesign'ın 4. sorusu.
- Önerilen çözüm: K 20:29'a göre ayrım ritim ve görüntüyle yeterli (Küre 25 m/s + 0,4 s yavaşlama; Top 30 m/s gülle yayı). Kullanıcı yalnız "yeterli mi" diye görsün; değişiklik önerilmiyor.

**AS-25 🟡 Hareket görünümleri bütün Hareket sıfatları için mi?**
- Nerede: K 20:35 (`v3-kararlar.md:251`) "İşaretli-Hareket silah görünümleri". `animasyon-kurallari.md` S-13; `sabah-paketi/acik-kararlar.md` K-17 (varsayılan "bütün ATIL").
- Önerilen çözüm: Bütün Hareket'te aynı ATIL (silah başına tek klip). Kullanıcı onaylasın.

**AS-34 🟠 Sohbetin görünmeyen kısmı taranamadı.**
- Nerede: Bu görevde sohbet geçmişi yalnız kesik bir özet (≈ 37 bin karakter atlanmış) ve t716u–t723u olarak görüldü. Sohbet dökümünü okuyan bir araç bu ajanda yok.
- Ayrıca belirsiz: `v3-kararlar.md:179` "7 yeni Ejderha Dili kökü onaylı" (hangi kökler yazmıyor); `v3-kararlar.md:244` kör test "A kararıyla kabul" (kullanıcı onayı mı, A'nın mı belli değil).
- Önerilen çözüm: Ana ajan sohbet dökümünü 15:00–20:45 arası tarayıp `v3-kararlar.md`'de karşılığı olmayan onayları eklesin; sonra kural kitabının 10. bölümü yeniden eşlensin.

**AS-36 🟡 Kural kitabının eski A-soruları (hâlâ açık olanlar).**
- Nerede: `kural-kitabi-v4.md:744–765`. Açık: A-5 (otomatik hedef rol tablosu), A-7 (eşit ağırlık = KG-1), A-8 (birlikte çekilen arkadaş = KG-30), A-9 (kayıt direği sınırı), A-10 (Tetikli Zaman kayıt anı), A-11 (İşaretli Zaman), A-12 (işaretsiz İşaretli), ~~A-13~~ KAPANDI (K 21:12 — yaratık canı var, düşmanlar hedef alabilir), A-14 (Çizgisel hattının başı), A-15 (boşa giden Hareket hücreleri), A-16 (yapıya işaret = KG-14), A-17 (çarpma hasarı = KG-21/22), A-20 (rün eşleşmesi onayı), A-21 (iki tasma = KG-6), A-22 (Arındırma çarpanları), A-23 (Tetikli işaretine "kapan" adı). A-2 → AS-28 KAPANDI, A-6 → AS-29 KAPANDI. Kapalı: A-1, A-2 (K 21:09), A-3, A-4, A-6 (K 21:10), A-13 (K 21:12), A-18, A-19 (K 21:10b).
- Önerilen çözüm: Kod iskeleti bunların hepsini ayar bayrağı olarak taşıyabilir. A-13 kapandı (K 21:12). Dış analizin kodu durduran boşlukları (mana/cooldown, kenardan ölçüm, Güdümlü sonsuz kilidi, darbe ve azalma tanımı) K 22:15 ile kapandı. Kodu durduran açık madde: A-20 (rün → kod eşlemesi); öncelikli sorulsun.

**AS-37 🟡 Kalkan durumu hâlâ oyunda var mı?**
- Nerede: `kural-kitabi-v4.md:166` (F2 "Fazla dolum yalnız Yoğun ile kalkana döner"), 2-1, 1.13 (element "kalkan" durumu), 1-8 (Fedakarlık-Zarar "hedefin kalkanına takılmaz"). Koruma kalkan vermiyor (K 16:49).
- Sorun: Kodda kalkan nesnesi gerekiyor mu? Gerekiyorsa yalnız Yoğun Şifa taşması ve element için. Düşmanların (boss) kalkanı var mı, yazılı değil.
- Önerilen çözüm: Kalkan, sınırlı bir "can üstü emici" durum olarak kalsın (yalnız bu iki kaynak + düşman verisi). Dengeleme (12:17) bekliyor.

---

<a id="b9"></a>
