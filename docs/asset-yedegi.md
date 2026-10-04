# Git dışı asset yedeği (PLAN 0.3)

Lisansı public repoda dağıtıma izin vermeyen asset'ler `.gitignore`'da ve **yalnız geliştirici PC'sinde** durur.
Bu dosya onların nerede yedeklendiğini ve nasıl geri yükleneceğini anlatır. **Bu klasörleri asla commit etme.**

## Neler git dışında
| Yol | İçerik | Lisans / kaynak | Boyut (2026-10-04) |
|---|---|---|---|
| `unity/Assets/Synty/` (+ `.meta`) | Synty POLYGON paketleri | Ücretli, Unity Asset Store | 7.334 dosya, 229 MB |
| `unity/Assets/Art/Mixamo/` (+ `.meta`) | Mixamo animasyonları | Adobe Mixamo; `tools/mixamo-download.mjs` ile yeniden indirilir | 321 dosya, 181 MB |
| `unity/Assets/JMO Assets/` (+ `.meta`) | Cartoon FX Remaster Free | Ücretsiz Asset Store (yeniden dağıtım yok) | 1.009 dosya, 38 MB |
| `unity/Assets/UnityTechnologies/` (+ `.meta`) | Unity Particle Pack | Ücretsiz Asset Store (yeniden dağıtım yok) | 886 dosya, 189 MB |
| `tools/vendor/` | Ham indirmeler (Blend/OBJ/zip) | Kaynak dosyalar | 258 dosya, 104 MB |
| `tools/mixamo-jobs/`, `tools/mixamo-download.mjs` | Mixamo indirme işleri | repoda izlenir; büyük animasyonlar `Art/Mixamo/` git dışı |

Oyun bu klasörler olmadan da derlenir ve CI'da çalışır: VfxLibrary boşken prosedürel efektlere düşer.
Eksik referansların taban listesi: `tools/IntegrationTests/known-missing-asset-guids.txt`.

## Yedek
- **Konum (PC):** `C:\Users\lacol\_backup\dovus-gitdisi-assetler-2026-10-04.zip`
  (529.604.440 bayt, 10.038 kayıt; yukarıdaki tüm yollar, `.meta` dosyalarıyla birlikte → GUID'ler korunur).
- **SHA256:** `B964139602FBC99A526B4D2FE940F4134A08D7560A2E4768953C80459FA93A90`
  (yanında `.sha256.txt` ve tam dosya listesi `.files.txt`).
- **Yeniden üretme:** `powershell -File C:\Users\lacol\_backup\make-backup.ps1` (tarih damgalı yeni zip yazar).
- Yedek aynı diskte duruyor. Disk arızasına karşı zip'in bir kopyasını harici diske veya özel bir bulut klasörüne
  (public olmayan) koymak önerilir. Bu karar kullanıcıda.

## Geri yükleme
1. Unity kapalıyken repo kökünde: `tar -x -f C:\Users\lacol\_backup\dovus-gitdisi-assetler-<tarih>.zip`
   (zip'teki yollar repo köküne göredir: `unity/Assets/Synty/...`).
2. Unity'yi aç; `.meta` dosyaları geldiği için sahne/prefab/SO referansları (GUID) aynen bağlanır.
3. Kontrol: `dotnet test tools/IntegrationTests` (asset referans testi) çözülen guid'leri uyarı olarak listeler.

## Git LFS kararı
- **Şimdilik LFS yok.** CC0/kendi yapımı asset'ler (Quaternius 41 MB, Weapons 58 MB, PolyHaven 11 MB, …) zaten
  normal git'te ve toplam ~115 MB. Bunları LFS'e taşımak geçmişi yeniden yazmayı (force-push) gerektirir; değmez.
- Lisanslı paketler LFS'e de konmaz (public repo = dağıtım).
- İleride tek dosyası >50 MB olan CC0/kendi asset gelirse `git lfs track` yalnız o uzantı/klasör için açılabilir.
