# Ajan Görev Listesi

> **Görev tablosu boş.** Eski T0–T14 ve önceki dövüş turu arşivde (`docs/arsiv/`).
> Yeni görevler `docs/element-sistemi.json` (v4.2+) üzerinden doğar — sıfırdan.

Bu dosyada kalan şey **çalışma düzeni** (tasarımdan bağımsız).

---

## Nasıl çalıştırılır

1. **Her göreve ayrı ajan, ayrı dal.** Testler yeşilse ajan merge eder; PR yalnızca **karar**
   gereken şeyde açık kalır.
2. **Sırayı bozma.**
3. Bitince: **"kabul kriterlerinden hangisini doğrulayamadın?"**
4. Değişmez kurallar `AGENTS.md`'de. Durum: `docs/durum.md`. Spec: `docs/element-sistemi.json`.
5. Her prompt **"ÖNCE OKU"** ile başlar; yalnızca gereken bölümleri sayar.
6. Unity gereken işlerde editör + MCP. Saf Core: `dotnet test`.
7. Denetim turu ayrı görev; denetçi kod yazmaz, bulguyu `docs/durum.md`'ye yazar.

## Hangi görev hangi modelle

| Görev tipi | Model |
|---|---|
| Core / gramer / çok katman | Opus |
| Speci net, tek yüzey | Sonnet |
| Mekanik, hızlı doğrulanır | Composer |
| Denetim | Yazan modelden farklı |

## Görev tablosu

*(boş — ilk görevler element-sistemi.json v4.2'ye göre yazılacak)*
