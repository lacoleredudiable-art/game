# Teknoloji Kararları — Alfa Prototip

> Amaç: prototipin his sorularını en az altyapıyla cevaplamak. Ağ, backend, hesap sistemi,
> sanat varlığı **kapsam dışıdır**.
>
> Tarih: 20 Ağustos 2026 · Durum: **geçerli.** Prototip turu bu kararlarla yapıldı ve hiçbiri
> değişmedi; dövüş tasarımının yeniden açılması bu belgeyi etkilemiyor.

---

## 1. Kurulacaklar (insan işi, ajan yapamaz)

| Ne | Neden | Not |
|---|---|---|
| **Unity 6 LTS** | Motor kararı (özet §8) | Unity Hub üzerinden |
| Unity **Android Build Support** modülü (SDK+NDK dahil) | Telefona build almak | Kurulumda seçilmeli |
| **.NET SDK 8+** | Ajanların dövüş mantığını Unity açmadan test etmesi | `dotnet --version` çalışmalı |
| Bir **Android telefon** + USB kablo | Asıl test aracı | USB debugging açık |
| **Unity MCP** | Ajanın editörle konuşması | Zaten mevcut |
| Git | Zaten mevcut | LFS henüz gerekmiyor |

iOS bilerek dışarıda: imzalama/sertifika süreci prototip hızını öldürür. Android'de APK
yan yükleme saniyeler sürer.

**Başka hiçbir şey gerekmiyor.** Ne asset satın alımı, ne sunucu, ne hesap sistemi.
Ses ve efektler prototipte koddan üretilecek (§6, §7).

---

## 2. Motor ve Paketler

Unity 6 + **URP** (mobil için Forward). Gerekli paketler:

```
com.unity.inputsystem                  → çok parmaklı dokunmatik (zorunlu)
com.unity.render-pipelines.universal   → URP
com.unity.ugui                         → TextMeshPro (parlak tepki yazısı)
com.unity.test-framework               → editör içi testler
com.unity.device-simulator.devices     → ekran oranlarını editörde denemek (opsiyonel)
```

**Legacy `Input` sınıfı kullanılmaz.** Sol başparmak çubuk + sağ başparmak çizim aynı anda
çalışacağı için `InputSystem.EnhancedTouch` zorunludur; eski girdi API'si çok parmakta
güvenilir değil.

---

## 3. Mimarinin Tek Önemli Kararı: Mantık motordan ayrı

```
Assets/Scripts/Core/     → asmdef "Dovus.Core"  · SAF C#, UnityEngine YOK
Assets/Scripts/Game/     → asmdef "Dovus.Game"  · Unity kabuğu (MonoBehaviour, render, girdi)
Assets/Tests/Core/       → asmdef "Dovus.Core.Tests" · NUnit
tools/CoreTests/         → dotnet test projesi, Core kaynaklarını link'ler
```

Gramer, cümle çözümleme, iptal pencereleri, dodge derecelendirme, ödül eğrisi, boss frame
verisi — **hepsi saf C#**. `UnityEngine` kullanmaz, zamanı parametre olarak alır.

Bunun iki büyük getirisi var:

1. **Ajan, Unity açmadan doğrulayabilir.** `tools/CoreTests` klasöründeki dotnet projesi aynı
   `.cs` dosyalarını link'lediği için `dotnet test` saniyeler içinde çalışır. Dövüş
   mantığının doğruluğu göze değil teste bağlanır — ajanla çalışmanın en kritik kaldıracı bu.
2. Sonradan sunucu tarafı gerekirse (özet §8) aynı assembly iki yerde kullanılır.

**Sahne koddan kurulur.** `PrototypeBootstrap.cs` sahneyi çalışma anında inşa eder (arena,
oyuncu, boss, kamera, HUD). Ajanlar `.unity` YAML dosyalarını elle düzenlemez — bu hem
bozulmaya çok müsait hem de birleştirmesi imkânsız. Sahnede tek bir boş GameObject + tek
script bulunur.

---

## 4. Fizik ve Hareket

Rigidbody fizik **kullanılmaz**. Oyuncu ve boss kinematiktir; hareket `Update` içinde
ölçeklenmiş `dt` ile elle uygulanır. Vuruş tespiti collider değil **matematik** (mesafe/açı)
ile yapılır.

Sebep: yavaş çekim `Time.timeScale` ile oynadığı anda Rigidbody davranışı ve
`fixedDeltaTime` işin içine girer; kare bazlı his ayarı bulanıklaşır. Kapsül dövüşünde
collider'ın hiçbir faydası yok, deterministik his ise her şey.

---

## 5. Zaman Yönetimi

Tek bir `TimeDirector` iki saati birlikte yönetir:

- **Dünya saati** (ölçeklenmiş): oyun simülasyonu, iptal pencereleri, boss frame verisi
- **Gerçek saat** (ölçeklenmemiş): HUD, tepki yazısı animasyonu, girdi eşikleri

Yavaş çekim rampası ve hitstop tek yerden geçer. İptal pencerelerinin dünya saatiyle
ölçülmesi bir detay değil, [yavaş çekim mekaniğinin
kendisidir](dovus-sistemi.md#2-kodda-şu-an-çalışan-mekanikler-envanter).

---

## 6. Ayarlanabilirlik — ve telefonda ayar sorunu

Özetin en kritik mimari kuralı: ayarlanabilir olan her şey veri olacak. Prototipte bu
`TuningConfig` ScriptableObject'leri demek.

Ama bir problem var: **telefonda inspector yok.** His ayarı telefonda elde yapılacağına göre,
her deneme için yeniden build almak 500 denemeyi 20 denemeye düşürür ve oyun asla doğru
hissetmez.

Çözüm: **oyun içi ayar paneli.** Ekranda açılıp kapanan, slider'lı bir katman; değerler
`Application.persistentDataPath` altındaki JSON'a yazılır ve açılışta okunur. ScriptableObject
varsayılanları, JSON kullanıcı üstüne yazar. Ayrıca "JSON'u panoya kopyala" düğmesi —
telefonda bulunan ayar, bilgisayardaki varsayılanlara geri taşınabilir.

Bu panel bir konfor değil, prototipin çalışma yöntemi.

---

## 7. Animasyon, Efekt, Ses

**Animasyon (prototip):** yok. Kapsüller prosedürel hareket eder (squash/stretch, atılma,
toparlanma pozu). Bu bir eksiklik değil — özet §6: doğru hitstop'a ve sese sahip bir küp,
bunları yapmayan güzel bir 3D modelden daha epik hisseder.

**Animasyon (sonrası, şimdiden bilinmesi gereken):** Mecanim durum makineleri iptal
pencereli dövüş için kötü bir araçtır. Gerçek animasyona geçilince **Animancer** ya da
Playables API ile doğrudan kontrol gerekir. Asset Store'dan animasyon alırken aranacak şey
"güzel animasyon" değil **zincirlenebilir** set — bu geri dönüşü pahalı bir karardır.

**Efekt:** prosedürel. Mürekkep izi ve şok dalgası için mesh/LineRenderer, yerde kalıcı iz
için basit decal quad'ları. Büyük yarı saydam katman **yok** (özet §6: mobilde şeffaflık
katildir, overdraw GPU'yu öldürür).

**Ses:** çalışma anında üretilen tonlar (`AudioClip.Create`). Beş hece beş farklı ton/gürültü
zarfı; yavaş çekimde master'a alçak geçiren filtre. Sıfır dosya bağımlılığı, sonra gerçek
kayıtla değiştirilir.

---

## 8. Unity MCP — neye yarar, neye yaramaz

**İyi olduğu yer:** paket kurmak, proje ayarı değiştirmek, sahneye bootstrap objesi eklemek,
play mode'a girip konsol hatalarını okumak, build tetiklemek.

**Yaptırılmaması gereken:** sahne/prefab YAML'ini elle kurmak, karmaşık hiyerarşi inşa etmek.
Bunlar koddan yapılır (§3). MCP'yi "editörü çalıştıran uzaktan kumanda" gibi kullan,
"dosya editörü" gibi değil.

---

## 9. Git

- Unity için standart `.gitignore` (Library, Temp, Logs, Build, obj)
- Proje ayarlarında **Force Text Serialization** açık (birleştirme mümkün olsun)
- `ProjectSettings/` ve `Packages/manifest.json` commit edilir
- Git LFS henüz gerekmiyor; ilk binary sanat varlığı girdiğinde açılır

---

## 10. Kapsam Dışı (bilinçli olarak)

Ağ katmanı, backend, veritabanı, hesap/davet sistemi, envanter, ekonomi, birden fazla sınıf,
birden fazla boss, gerçek 3D varlık, mağaza, replay kaydı.

Bunların hiçbiri §13'teki soruları cevaplamaya katkı vermez. Özet §9'daki uyarı burada
geçerli: altyapıyı fazla mühendislik yapmak 6 ay geçirir ve ortada oyun olmaz.
