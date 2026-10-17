# game

Mobil co-op boss baskın oyunu — alfa. Beş arkadaşın akşam kahvehanede ~3 saat oynadığı, masa etrafında
LoL teamfight yoğunluğu + Dragon Nest **Kalahan** baskın zorluğu hedeflenen bir deneyim.

## Oyun hikâyesi ve vizyon

- **Kim:** 5 arkadaş, gürültülü masa, telefon; oyun masayı konuşturur (wipe sebebi tek cümle, diriliş yok,
  teorik 4v boss mucizesi mümkün).
- **Ne:** Altıgen rün çizimi → fiil + sıfat (dilimde 2 rün; tam oyunda 2/3/4 rünlü bileşik gramer).
  **12 fiil × 12 sıfat = 144** temel hücre; **10 silah** görünümü; dünya fiziği kuralları etkileşim üretir.
- **Çeşitlilik:** Dikey güç (level/item) **yok**; herkes eşit. Build = **6 rün + 2 silah** (şampiyon seçimi).
  Dilim: Kılıç + Yay, rünler 1-2-3-4-6-9 → **36** ikili kombo.
- **Zorluk:** Boss %5'te planlı wipe; "neden öldük?" okunur. **Mükemmel dodge yok** — "Kalahan affetmez,
  dodge affeder" (`docs/tasarim/13-vizyon-ve-cerceve.md` §3).
- **Yetenek tavanı:** Zaman ekonomisi, **iki silah arası animasyon iptali (swap cancel)**, parry (Koruma),
  just-frame (Tetikli) — tam vizyon `13`, kod sırası `14`.

## Hedef yol haritası ve bugünkü durum (17 Ekim 2026)

| Aşama | Ne | Durum |
|-------|-----|--------|
| **A — Alfa dilimi** | v4.5 tek yol: `RuleEngineV4SentenceHost` + `kural-motoru-v4.json`; Kılıç+Yay; Ağların Kraliçesi | **Aşama 1–3 ✅** (dodge, temiz ekran, eski motor silindi; `DepMap = 0`) |
| **B — Kül Ejderi + Krater** | `17`–`20`: 18 m krater, 3 faz ejderha, 1 oyuncu + 2 AI, ~3 dk dikey dilim | Tasarım onaylı; Core iskelet (`KulEjderiBrain`) hazır; sahne entegrasyonu **B** hattında |
| **C — Tam oyun** | 5 kişi co-op, 12 rün × 10 silah, wipe hakemi, masa rekabeti | `13` + `01` kural kitabı; kod sonra |

**Sıradaki kod (`docs/tasarim/14-kodlama-plani.md`):** Aşama **4–8** — skiller kurala göre (Zarar/Şifa/Hareket/Koruma → Kontrol/Arındırma/Güdümlü → sabit yapılar → swap cancel + Kılıç zinciri → ayar masası).

Kısa oynanış özeti ve sayılar: `docs/OYUN.md` (üretilen blok: `tools/gen-game-overview.py`).

## Unity

1. Unity **6000.4.4f1**, proje `unity/`
2. Sahne `Assets/Scenes/Prototype.unity` (`GameBootstrapHost`)
3. Play veya Android: `docs/unity-notlari.md`

## Doğrulama (repo kökü)

```bash
python3 tools/gen-game-overview.py --check
dotnet test tools/CoreTests
dotnet test tools/IntegrationTests
python3 tools/GameCompile/check.py
```

Windows: `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1`

## Belge haritası

| Belge | Ne için |
|-------|---------|
| [docs/tasarim/00-OKU-BENI.md](docs/tasarim/00-OKU-BENI.md) | Tasarım kaynağı, öncelik, yol haritası |
| [docs/tasarim/13-vizyon-ve-cerceve.md](docs/tasarim/13-vizyon-ve-cerceve.md) | Vizyon, zorluk, masa deneyimi |
| [docs/tasarim/14-kodlama-plani.md](docs/tasarim/14-kodlama-plani.md) | **Güncel iş sırası** (aşamalar 4–8) |
| [docs/OYUN.md](docs/OYUN.md) | Dilim özeti + üretilmiş sayılar |
| [docs/MAP.md](docs/MAP.md) | Konu → kod dosyası |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Katmanlar, v4.5 yürütme, isimlendirme |
| [AGENTS.md](AGENTS.md) | Ajan ve CI kuralları |

Eski PLAN.md ve onarım planı kaldırıldı; git geçmişi tasarım kanıtı değildir.
