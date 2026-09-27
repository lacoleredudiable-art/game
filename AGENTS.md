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
5. **Sıfat davranış ve silüeti değiştirir.** Sayısal karşılığı varsa yalnızca bağlayıcı JSON'daki `engine` / `adjective_mods` verisinden gelir.
6. **Kırmızı-turuncu yalnızca boss tehdidi.** Oyuncu efektleri camgöbeği/mor.
7. **Kombo tablosu yazılmaz.** Hiçbir dizi elle tanımlanmaz; her şey gramerden doğar.

## Bağlayıcı tasarım — v6.1.1

Tek doğruluk kaynağı `docs/element-sistemi.json`'dır. Sistem 12 çift yüzlü ründür
(fiil + sıfat); build 12'den tekrarsız 6 rün seçer ve 2-rün grameri 144 skill üretir.
Element prototipte yalnız VFX/isim katmanıdır. Global slow-mo yoktur; Zaman
`enemy_slow` / `self_haste` uygular. Pasif yuva 0-2, silahlar çarpan + animasyon +
hitbox katmanıdır; skill mekaniğini değiştirmez.
v6.1.1 ekleri: `ana_classes_80`, `skills_prose_144`, `hitbox_vfx`, `mobility_cc`,
`uyumsuz_cizim`, `presentation`, `changelog_v6_1`, `design_warnings`.
Runtime sırası: `ElementSystemJsonLoader` → 12/10/6 SO katalog → `SkillFactory` →
`RuneManager` → `SkillMotor`. Altı ekran slotu `RuneLoadout` ile 12 ründen seçilir.
Radial element UI ile tam hitbox/VFX/presentation henüz stub'dır. 1–8 Unity Play'de
doğrulanmadan eski SO/listeleri silme veya v5 davranışını canlı motora geri ekleme.

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
- **Başlarken `git fetch && git log --oneline origin/master -5`.** 16 Eylül'de `master`
  günlerce fark edilmeden ayrıştı (paralel bir "v4.2 element spec" hattı) — bir oturumluk iş
  boşa gitmesin diye çözüldü ama pahalıydı. Yerel `master` ile `origin/master` arasında commit
  farkı varsa **önce onu** çöz, üstüne inşa etme.
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
| `docs/element-sistemi.json` | **v6.1.1 bağlayıcı** element/skill verisi — sayılar ve kurallar burada |
| `docs/element-sistemi.md` | İnsan-okunur tarihsel/açıklayıcı notlar; JSON bağlayıcıdır |
| `docs/unity-notlari.md` | Unity/sahne/Android build operasyonel tuzakları (tasarım değil) |
| `docs/prezentasyon-katmani.json` | Trajectory/hitbox/animasyon/VFX verisi — element sisteminden bağımsız, motor okur |

> 16 Eylül 2026: `dovus-sistemi.md` / `tasarim-ozeti.md` / `teknoloji-kararlari.md` /
> `his-kontrol-listesi.md` / `t0-kurulum.md` / `alis-sepeti.md` / `animasyon-omurgasi.md`
> silindi — beşgen/3-rün alfa prototipine aitti, altıgen/6-element sistemine geçildikten
> sonra kafa karıştırıyordu. Git geçmişinde duruyor.
