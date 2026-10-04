# CI ve yerel doğrulama

## GitHub Actions (`sweep-v2.yml`)

Her PR ve `master` push’unda yalnızca **dotnet** araçları koşar:

| Adım | Araç |
|------|------|
| Core testleri | `dotnet test tools/CoreTests` |
| Integration testleri | `dotnet test tools/IntegrationTests` |
| Gramer derlemesi | `dotnet build tools/AtomSim` |
| Başsız Play taraması | `SweepV2 --all --gate` (1440 kombo, 144/144 silah, normalize CSV SHA256, bilinen Play farkları) |

Unity Editor veya `Unity.exe` **CI’da çalıştırılmaz**: lisans ve headless grafik ortamı gerektirir; kapı mekanikleri `SweepV2` ile doğrulanır.

## Yerel tam doğrulama (`tools/verify.ps1`)

Sırayla: **GameCompile** → **UnityCompile** (yerel Unity 6 DLL ile script derlemesi) → **CoreTests** → **IntegrationTests** → **AtomSim** → **SweepV2** (`--gate`, ham `verify.csv` SHA256 `726197…2BB4`, normalize hash `@docs/play-sweep/headless-baseline.sha256`).

Unity’ye özgü ek kontroller (CI dışı, batchmode):

- `tools/UnityCompile/check.ps1` — Editor script’leri dahil derleme
- `Dovus.Game.Editor.MissingScriptAudit.Run` — eksik script denetimi
- PlayMode smoke — `PrototypeSceneSmokeTests` (`-smokeOut`)
- Sabit açılı PNG — `FixedAngleCapture.CaptureFromCommandLine` (`tools/capture/README.md`)
