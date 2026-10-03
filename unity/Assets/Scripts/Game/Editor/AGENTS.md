# Game/Editor (ajan notu)

Kök `AGENTS.md`. Runtime oyun kodu `Game/` (Editor altı hariç).

- **Menüler:** `Dovus/...` `[MenuItem]` — asset bağlama, capture, Play Sweep, Android build (`PlaySweep.cs`, `*Bind.cs`, `FeelCapture*.cs`).
- **Unity MCP ile build tetikleme YASAK** (2026-10-02: editör donması). CI ve doğrulama: `tools/verify.ps1`, `dotnet`/`python` başsız araçlar.
- **Capture script'leri:** Tek seferlik görsel/anim kaydı; runtime davranışa bağlama. PLAN 0.4'te temizlik adayı.
- **Play Sweep:** Editörde 144/1440 kombo; başsız eşdeğeri `tools/SweepV2`.
- **Yeni Editor kodu:** `GameCompile` Editor klasörünü derlemez; runtime'a taşımadan önce `check.py` ile runtime tarafını doğrula.
