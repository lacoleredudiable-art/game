# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Kısa tut. Sıradaki ajan repoyu
> taramadan nerede kaldığımızı anlasın.

**Son güncelleme:** 13 Eylül 2026  
**Faz:** v4.2 **kilitli**; Core element resolver + katalog testleri yeşil  
**Sıradaki:** Unity/oyun yüzeyi (rün girişi → resolver → efekt); skill_job satırları playtest

---

## 1. Karar

Eski `dovus-sistemi.md` / önceki durum notları **arşive alındı**, bağlayıcı değil.
Bağlayıcı dövüş: **`docs/element-sistemi.json` v4.2** (`locked: true`, `2026-09-13`).

- 2'li → bileşik kimlik skill'i (`skill_job`)
- 3'lü → fiil kapısı + ana sıfat (`verb_unlocks_job` + adjective)
- 4'lü → dar şekil / commit anahtarı (hasar ultisi değil)
- Uzunluk hasar/poise çarpmaz; sıfat işlevi varsa `damage_mult ≤ 0.9`

## 2. Kod

| Parça | Yol |
|---|---|
| Tipler | `unity/Assets/Scripts/Core/Elements/ElementTypes.cs` |
| Katalog | `…/ElementCatalog.cs` |
| Çözümleyici | `…/ElementResolver.cs` |
| Test yükleyici | `tools/CoreTests/ElementCatalogLoader.cs` |
| Testler | `tools/CoreTests/ElementResolverTests.cs` (14) |

`dotnet test` — 106 yeşil (ElementResolver dahil).

Eski alfa gramer/cümle kodu repoda duruyor; yeni JSON ile uyumlu sayılmaz.
Stack: `docs/teknoloji-kararlari.md` · Unity: `docs/unity-notlari.md`

## 3. Bilinen açıklar

- Resolver henüz Unity sahne / input'a bağlı değil
- 36 bileşik için `skill_job` / `verb_unlocks_job` / 4'lü dar şekil satırları taslak; playtest ile budanacak
- Sayılar `[draft]`
- Boss repertuarı / run yapısı tanımsız (JSON dışı, sonra)

## 4. Arşiv

Okunmaz, bağlayıcı değil: `docs/arsiv/` (eski durum, eski dövüş spec, T0–T14).
