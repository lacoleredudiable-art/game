# tools (ajan notu)

Kök `AGENTS.md`. Tüm komutlar repo kökünden.

| Araç | Ne yapar | Nasıl koşulur |
|------|----------|----------------|
| `GameCompile/check.py` | Unity Editor olmadan Game script derlemesi | `python tools/GameCompile/check.py` |
| `UnityCompile/check.ps1` | Unity 6 DLL ile Core+Game+Editor başsız derleme (yerel; CI yok) | `powershell -File tools/UnityCompile/check.ps1` |
| `CoreTests` | Core + App (+ GameCompile testi); `AppSimulationTests` shim kullanmaz | `dotnet test tools/CoreTests` |
| `IntegrationTests` | JSON→skill, asset guid, tuning round-trip (Core+Game+Shim) | `dotnet test tools/IntegrationTests` |
| `AtomSim` | Gramer / skill simülasyonu, rapor | `dotnet run --project tools/AtomSim` → çıktıda `0 hata` |
| `SweepV2` | Başsız 1440 kombo, CI kapısı; `Shim/` + `PlaySweep` (Unity API taklidi) | `dotnet run --project tools/SweepV2 -c Release -- --all --gate` |
| `build-motion-templates.py` | `motion-templates.json` iki kopya | `python3 tools/build-motion-templates.py` |

**Unity batchmode doğrulama (editör kapalı; açık editörün projesinde çalışmaz):**

- Missing script: `Unity.exe -batchmode -projectPath <unity> -executeMethod Dovus.Game.Editor.MissingScriptAudit.Run -auditOut <dosya> -logFile <log>`
- PlayMode smoke: `Unity.exe -batchmode -projectPath <unity> -runTests -testPlatform PlayMode -testResults <xml> -smokeOut <dosya> -logFile <log>`

**Tek özet doğrulama:** `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1`  
Parametreler: `-Quick` (sweep atla), `-Skip gamecompile,unitycompile,coretests,integration,atomsim,sweep`. Loglar: `tools/verify-out/` (gitignore).
