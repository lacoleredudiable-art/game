# Ajan Görev Listesi

> Eski T0–T14 arşivde (`docs/arsiv/`). Spec: `docs/element-sistemi.json` **v4.2 kilitli**.

Bu dosyada kalan şey **çalışma düzeni** + sıradaki işler.

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

| ID | Durum | Özet |
|---|---|---|
| E0 | **bitti** | v4.2 kilitle + `ElementCatalog`/`ElementResolver` + testler |
| E0b | **bitti** | 1554 dizi exhaustive gramer testi (1 red / 2·3·4 çöz) |
| E1 | sırada | Unity: rün girişi → resolver sonucu (görsel/efekt yoksa stub) |
| E2 | sonra | skill_job / verb_unlocks_job satırlarını playtest ile budama |
