# Sabit açılı sahne yakalama (PLAN 2B.16)

Ajanlar ve geliştiriciler, `Prototype` sahnesinden tutarlı PNG üretmek için Unity batchmode ve `tools/capture/angles.json` önayarlarını kullanır.

## Komut

Repo kökünden `unity` klasörü proje yolu olmalıdır. Örnek (Windows; `Unity.exe` yolunu kendi kurulumunuza göre değiştirin):

```text
Unity.exe -batchmode -projectPath unity ^
  -executeMethod Dovus.Game.Editor.FixedAngleCapture.CaptureFromCommandLine ^
  -logFile -
```

İsteğe bağlı:

- `-captureOut <dizin>` — PNG ve `captures.json` çıktısı (varsayılan: `unity/Temp/captures`)
- `-anglesJson <dosya>` — önayar JSON (varsayılan: `tools/capture/angles.json`)

## Çıktı

Her önayar için `<ad>.png` ve özet `captures.json` (ad, dosya, boyut, sha256).

## Ajanlar için görsel karşılaştırma

1. Aynı Unity sürümü ve aynı `angles.json` ile komutu çalıştırın.
2. `captures.json` içindeki `sha256` değerlerini veya PNG dosyalarını diff edin.
3. `hud` önayarı Play modunda `Camera.main` ile yakalanır (HUD katmanı); diğerleri JSON’daki sabit kamera ile üretilir.

## Önemli: `-nographics` kullanmayın

Batchmode’da `-nographics`, RenderTexture / kamera render yolunu devre dışı bırakabilir; bu araç PNG üretmek için grafik bağlamı gerektirir. CI’da Unity derlemesi koşulmaz; yerel doğrulama için `tools/verify.ps1` ve isteğe bağlı batchmode çalıştırması yeterlidir (bkz. `docs/ci.md`).
