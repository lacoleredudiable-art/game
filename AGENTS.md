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
- **Başka görevin dosyalarına dokunma.** Eksik/yanlış bir şey görürsen düzeltme,
  `docs/durum.md`'nin "Bilinen açıklar" bölümüne yaz.
- **Sayı uydurma.** Bir element/skill değeri gerekiyorsa `docs/element-sistemi.json`'dan al.
 Bir his değeri (hitstop, dodge, kamera vb.) gerekiyorsa ilgili `Core/Tuning/*.cs` sınıfının
 mevcut varsayılanından al. Hiçbirinde yoksa varsayılan koy, yoruma referans bırak,
 `docs/durum.md`'ye de geç.
- **Bitince `docs/durum.md`'yi güncelle** — bir sonraki ajan repoyu taramak zorunda kalmasın.
- **Kapanışta söyle:** kabul kriterlerinden hangisini doğrulayamadın.
- Küçük ve anlamlı commit'ler; her görev kendi dalında.
- **Dalı kendin kapat.** `dotnet test` yeşilse `master`'a merge edip push et; kimseye sorma.
  PR'ı yalnızca **karar** gerektiren bir şey çıktıysa açık bırak — spec'te cevabı olmayan bir
  soru, ya da doğrulayamadığın bir kabul kriteri. Onun dışında commit trafiği sahibine sorulmaz.

## Dosya haritası

| Dosya | Ne için |
|---|---|
| `docs/durum.md` | Nerede kaldık, ne üretildi. **İlk buraya bak.** |
| `docs/gorev-listesi.md` | Görevler ve prompt'lar |
| `docs/element-sistemi.json` | Element/skill verisi — motor bunu okur, sayılar burada |
| `docs/element-sistemi.md` | Element sisteminin insan-okunur speci (gramer, aile/rol, bilinen açıklar) |

> 16 Eylül 2026: `dovus-sistemi.md` / `tasarim-ozeti.md` / `teknoloji-kararlari.md` /
> `his-kontrol-listesi.md` / `t0-kurulum.md` / `alis-sepeti.md` / `animasyon-omurgasi.md`
> silindi — beşgen/3-rün alfa prototipine aitti, altıgen/6-element sistemine geçildikten
> sonra kafa karıştırıyordu. Git geçmişinde duruyor.
