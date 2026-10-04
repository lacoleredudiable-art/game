# game

Unity C# mobil kooperatif boss dövüşü — alfa prototip.

## Oyun

Sol joystick hareket. Sağ altıgen: **2 rün** (fiil + 1 sıfat); merkeze kısa dokunuş düz vuruş.
Dodge ayrı düğme (2 hak, 6 sn dolum; çift basış birleşik kaçış). Mükemmel dodge yalnız kısa **görsel donma** —
dünya ve CD akmaya devam eder. CD ve mana zorunlu (`CombatTuning`). Aktif boss: sahnede `aglarin_kralicesi`.

## Unity

1. Unity **6000.4.4f1**, proje `unity/`
2. Sahne `Assets/Scenes/Prototype.unity` (`GameBootstrapHost`)
3. Play veya Android: `docs/unity-notlari.md`

## Belgeler

| Belge | Ne için |
|-------|---------|
| [docs/OYUN.md](docs/OYUN.md) | Oyun özeti; sayılar üretilir (`tools/gen-game-overview.py`) |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Mimari hedef + bugün + isimlendirme/sözlük |
| [docs/PLAN.md](docs/PLAN.md) | Tek iş listesi |
| [docs/MAP.md](docs/MAP.md) | Konu → dosya → giriş |
| [AGENTS.md](AGENTS.md) | Katkı kuralları ve CI |
| [docs/unity-notlari.md](docs/unity-notlari.md) | Build, sahne, cihaz |

Doğrulama (repo kökü): `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1`

Eski beşgen prototip belgeleri kaldırıldı; güncel tasarım için git geçmişi kaynak kabul edilmez.
