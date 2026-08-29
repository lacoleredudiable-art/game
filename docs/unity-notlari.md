# Unity / build / telefon notları

> Prototip turunda pahalıya öğrenilmiş operasyonel tuzaklar. Tasarım kararı yok, hepsi
> "şunu yapma, çalışmıyor" cinsinden. Unity'ye ya da cihaza dokunacak görevde okunur.

## Sahne ve derleme

- **Sahne koddan kurulur.** `.unity` / `.prefab` YAML'ine elle dokunulmaz. Sahneyi yeniden
  üretmek için: **Dovus → Create Prototype Scene** (`PrototypeSceneCreator`). Sahnede tek bir
  boş GameObject + `PrototypeBootstrap` bulunur.
    10|- **Yerleşik mesh gerekiyorsa `PrimitiveMesh.Get(...)` çağır**, `GameObject.CreatePrimitive`
  değil. İkincisi bir kare yaşayan collider üretir; projede collider yok, vuruş tespiti
  matematikle yapılıyor.
- **Serileşmiş alanlar bayat gelir.** Açık sahnenin bellekteki hâli assembly reload'dan sonra
  eski alan değerlerini korur; **yeni eklenen int/float alanlar 0 gelir** (C# initializer
  deserialize'da uygulanmaz). `PrototypeTuning.EnsureRuntimeDefaults` bunu `TuningVersion`
  damgasıyla bir kez yamalar — yeni alan eklerken versiyonu artır. Ölçüm almadan önce sahneyi
  **diskten yeniden aç** (`EditorSceneManager.OpenScene(path, OpenSceneMode.Single)`), sonra play.
- **Yükleme sırası:** `EnsureRuntimeDefaults` **önce**, `TuningConfig.TryLoad` (JSON) **sonra**.
  Ters olursa telefonda kaydedilen ayar varsayılanlarla ezilir.
    20|- **Ayar nesnelerinin kimliği korunmalı.** `DodgeState`/`SentenceEngine`/`PentagonInput`
  iç ayar nesnelerinin referansını `Bind` sırasında bir kez alıp saklıyor; `CopyFrom` alan alan
  yazar. Yeni bir ayar nesnesi atarsan panelin slider'ları sessizce hiçbir şeyi değiştirmez.
- **Kurulumda okunan yerleşim canlı ayarı yutar.** `ReactionReadout`/`VitalsHud`
  `ApplyTuningLayout` deseninde her karede uygulanan değeri karşılaştırıp değiştiyse yeniden
  yazıyor. Yeni bir HUD ögesi eklerken aynı deseni kullan, yoksa slider ekranda çalışmaz.
- **Canvas `ConstantPixelSize`:** her `...Dp` ölçüsü `PentagonLayoutScreen.DpToPixels`'ten
  geçmek zorunda. Geçmeyen ölçü yüksek yoğunluklu telefonda ~2.5 kat küçük çıkar.
- **Alfa 0 bir `Graphic` yine de geometri üretip harmanlanır.** Görünmeyen HUD ögesi için renk
  saydamlaştırmak yetmez, `enabled = false` gerekir (mobil overdraw).
    30|- **Saydam materyal:** önce `Sprites/Default` denenir (gerçek alfa harman), bulunamazsa URP
  Unlit'e düşülüp `_Surface`/`_Blend`/blend modu/render queue elle saydama çevrilir. URP Unlit
  varsayılan **opak**tır; doğrudan kullanılırsa sönme animasyonu ekrana hiç yansımaz.

## Test

```
cd tools/CoreTests && dotnet test
```

Core kaynaklarını joker ile link'ler, Unity gerektirmez. Şu an **92 test yeşil**. Bir görev
    40|Core'a dokunuyorsa bu komut yeşil olmadan dal kapatılmaz.

## Build (Android)

- **Player ayarları kodla yazılır** (`AndroidBuilder`): IL2CPP · ARM64 · min SDK 24 ·
  `com.dovus.prototip` · yalnızca yatay · app bundle kapalı. Build Settings penceresinde elle
  tıklanan kutu ertesi gün başka sonuç verir.
- **`Shader.Find` ile üretilen materyaller player build'ine girmez** — sahne koddan kurulduğu
  için hiçbir sahne referansı yoktur ve dünya macenta çıkar. `AndroidBuilder` URP Lit /
  URP Unlit / `Sprites/Default`'u **Always Included** listesine yazıyor; yeni shader eklenirse
  o listeye de eklenmeli.
    50|- **Batchmode build, Unity Editor açıkken çalışmaz** ("another Unity instance"). Kapatılmış bir
  editörden kalan bayat `unity/Temp/UnityLockfile` de aynı hatayı verir.
- **MCP ile build tetiklerken** komut uzun build'de zaman aşımına uğrar ama build editörde
  devam eder. Çaresi: build'i `EditorApplication.delayCall` içine koy, MCP komutu hemen dönsün,
  sonucu `Unity_GetConsoleLogs` ile oku.
- **MCP `System.Reflection` kullanımını reddediyor**; `Dovus.Game.EditorTools` doğrudan `using`
  ile çağrılabiliyor.

## Ekran görüntüsü / ölçüm

    60|- **`Unity_Camera_Capture` işe yaramaz** — URP kamera stack'ini kompoze etmiyor (Base'de UI
  yok, Overlay tek başına beyaz). Oyun modunda
  `ScreenCapture.CaptureScreenshot(Path.GetFullPath("Temp/x.png"))` kullan: Game view'ı UI ve
  Overlay stack dahil kompoze yakalar. `unity/Temp/` git tarafından yok sayılıyor.
- Kısa süreli bir HUD penceresini yakalamak için ilgili `...HoldMs` değerini geçici olarak
  büyüt (probda, commit'e girmeden).
- **`PlayerSettings.runInBackground` kapalı:** editörde odak kaybında play mode duruyor,
  MCP ile ölçüm alırken yanıltır.
- Kare süresi göstergesi `PrototypeTuning.ShowFrameTimeHud` ile açılır; ölçüm
  **ölçeklenmemiş** saatte (yavaş çekim kare süresini değil dünya zamanını ölçekler).
    70|
## Telefon (Xiaomi / HyperOS)

- **`adb shell input tap` `INJECT_EVENTS` ile reddediliyor** — panel/oyun uzaktan tıklanamaz.
  Ayrı bir geliştirici seçeneği gerekiyor: "USB debugging (Security settings)".
- Uzaktan ayar değiştirmenin yolu: `tuning.json`'ı push et + uygulamayı yeniden başlat.
- `adb install -r` bu cihazda çalışıyor; `INSTALL_FAILED_USER_RESTRICTED` görülürse
  "USB üzerinden yükle" seçeneğine bakılmalı.
