# tools (ajan notu)

Kök `AGENTS.md`. Repo kökünden: `dotnet test tools/CoreTests`, `tools/verify.ps1`.

SweepV2 CI kapısı: 144/144, normalize hash, `known-play-diffs.txt`. GameCompile / AtomSim / gen-game-overview `--check`.

GameCompile `Unity3D.SDK` 2021.1.14.1 ile derler (editör 6000.4.4f1); Unity 6'ya özgü API/asmdef hataları yalnız PC batchmode derlemesinde yakalanır. NuGet'te lisanslı/CI-güvenilir Unity 6000 referans paketi yok (Unity3D.SDK güncellenmiyor; UnityAssemblies kurulum/UnityVersion gerektirir).
