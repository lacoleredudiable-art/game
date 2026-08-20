# Ajanlar için kurallar

Mobil kooperatif boss dövüşü oyunu. Şu an alfa prototip aşaması.

> Bu dosya her ajanın bağlamına otomatik giriyor, yani her satırı her görevde ödüyoruz.
> O yüzden kısa. Detay burada değil, görevinin işaret ettiği bölümde.

## Değişmez kurallar

Bunlar görevden bağımsız, hepsi geçerli. İhlali geri dönüşü pahalı hatalardır.

1. **`Assets/Scripts/Core` saf C#** — `using UnityEngine` yasak. Zaman parametre olarak geçer.
2. **Sahne koddan kurulur.** `.unity` / `.prefab` YAML dosyaları elle düzenlenmez.
3. **Ayarlanabilir her şey veri.** His sayıları koda gömülmez; ScriptableObject/config alanı olur.
4. **Hiçbir fiil anlık vurmaz.** Her etki dünyada yaşar (yol alır/sürer), yoksa sıfat kabul edemez.
5. **Sıfat silüeti değiştirir, sayıyı değil.** "%30 daha fazla hasar" diye bir sıfat olamaz.
6. **Kırmızı-turuncu yalnızca boss tehdidi.** Oyuncu efektleri camgöbeği/mor.
7. **Kombo tablosu yazılmaz.** Hiçbir dizi elle tanımlanmaz; her şey gramerden doğar.

## Çalışma düzeni

- **Repoyu tarama.** Sadece görevinin "ÖNCE OKU" satırındaki dosya ve bölümleri oku.
  Belgeler uzun; ilgisiz bölümü okumak bağlamı doldurur ve kapsam dışı "iyileştirme" riskini artırır.
- **Asla okumayacağın yerler:** `unity/Library/`, `unity/Temp/`, `unity/obj/`, `unity/Logs/`.
- **Başka görevin dosyalarına dokunma.** Eksik/yanlış bir şey görürsen düzeltme, PR açıklamasına yaz.
- **Sayı uydurma.** Bir his değeri gerekiyorsa `docs/dovus-sistemi.md`'den al. Orada yoksa
  varsayılan koy ve belgeye referansla yorum bırak, PR'da belirt.
- **Bitince `docs/durum.md`'yi güncelle** — bir sonraki ajan repoyu taramak zorunda kalmasın.
- **Kapanışta söyle:** kabul kriterlerinden hangisini doğrulayamadın.
- Küçük ve anlamlı commit'ler; her görev kendi dalı ve kendi PR'ı.

## Dosya haritası

| Dosya | Ne için |
|---|---|
| `docs/durum.md` | Nerede kaldık, ne üretildi. **İlk buraya bak.** |
| `docs/gorev-listesi.md` | Görevler ve prompt'lar |
| `docs/dovus-sistemi.md` | Dövüşün speci, bütün sayılar |
| `docs/teknoloji-kararlari.md` | Stack, mimari, kapsam dışı olanlar |
| `docs/tasarim-ozeti.md` | Projenin genel bağlamı (nadiren gerekir) |
