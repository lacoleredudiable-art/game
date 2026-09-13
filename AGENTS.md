# Ajanlar için kurallar

Mobil kooperatif boss dövüşü oyunu. **Dövüş tasarımı sıfırdan:** bağlayıcı kaynak
`docs/element-sistemi.json` (v4.2+).

> Bu dosya her ajanın bağlamına otomatik giriyor — kısa tut. Detay JSON'da / görevde.

## Değişmez kurallar

1. **`Assets/Scripts/Core` saf C#** — `using UnityEngine` yasak. Zaman parametre olarak geçer.
2. **Sahne koddan kurulur.** `.unity` / `.prefab` YAML dosyaları elle düzenlenmez.
3. **Ayarlanabilir her şey veri.** His / denge sayıları koda gömülmez; JSON / ScriptableObject.
4. **Dövüş dili = element sistemi.** Bağlayıcı spec: `docs/element-sistemi.json`. Eski
   pentagon-cümle / arşiv dövüş belgeleri bağlayıcı değil.
5. **Uzunluk güç değildir.** 2/3/4 hasar veya poise çarpanı üretmez. Uzatma iş/şekil/commit
   değiştirir (`skill_job` → `verb_unlocks_job` → dar silüet).
6. **Sıfat vergisi.** Yeni işlev/şekil ekleyen sıfatta `damage_mult ≤ 0.9`. Ham güç buff'ı
   (crit şansı, execute eşiği, length poise) yok.
7. **Kırmızı-turuncu yalnızca boss tehdidi.** Oyuncu efektleri camgöbeği/mor.
8. **2'li kimlik, 3/4 kural.** 36 bileşik skill paketidir; 3/4 fiil+sıfat ile çözülür.
   Elle 1296'lık kombo tablosu yazılmaz.

## Çalışma düzeni

- **Repoyu tarama.** Sadece görevinin "ÖNCE OKU" satırındaki dosya/bölümleri oku.
- **Asla okumayacağın yerler:** `unity/Library/`, `unity/Temp/`, `unity/obj/`, `unity/Logs/`,
  `docs/arsiv/` (bağlayıcı değil; görev açıkça göndermediyse açma).
- **Başka görevin dosyalarına dokunma.** Eksik görürsen düzeltme; `docs/durum.md`
  "Bilinen açıklar"a yaz.
- **Sayı uydurma.** Değer `docs/element-sistemi.json` içinde yoksa varsayılan koy, yorumla
  `[draft]` işaretle, `docs/durum.md`'ye geç.
- **Bitince `docs/durum.md`'yi güncelle.**
- **Kapanışta söyle:** hangi kabul kriterini doğrulayamadın.
- Küçük anlamlı commit'ler; her görev kendi dalında.
- **Dalı kendin kapat.** `dotnet test` yeşilse `master`'a merge edip push et. PR yalnızca
  **karar** gereken şeyde açık kalır.

## Dosya haritası

| Dosya | Ne için |
|---|---|
| `docs/durum.md` | Nerede kaldık, açıklar. **İlk buraya bak.** |
| `docs/element-sistemi.json` | **Bağlayıcı dövüş sistemi** (rünler, fiiller, sıfatlar, ekonomi) |
| `docs/unity-notlari.md` | Editör/build/telefon tuzakları |
| `docs/gorev-listesi.md` | Çalışma düzeni ve görevler |
| `docs/teknoloji-kararlari.md` | Stack, mimari, kapsam dışı |
| `docs/tasarim-ozeti.md` | Genel ürün bağlamı (nadiren) |
