# Durum

> **Görevi bitiren ajan burayı güncellemekle yükümlü.** Kısa tut. Sıradaki ajan repoyu
> taramadan nerede kaldığımızı anlasın.

**Son güncelleme:** 13 Eylül 2026  
**Faz:** **sıfırdan başlangıç** — bağlayıcı dövüş tasarımı = `docs/element-sistemi.json` (v4.2)  
**Sıradaki:** v4.2'ye göre yeni Core/gramer iskeleti; eski pentagon cümle prototipi bağlayıcı değil

---

## 1. Karar

Eski `dovus-sistemi.md` / önceki durum notları **arşive alındı**, bağlayıcı değil.
Bundan sonra dövüş sistemi **element rün dilidir** (`docs/element-sistemi.json`).

- 2'li → bileşik kimlik skill'i (`skill_job`)
- 3'lü → fiil kapısı + ana sıfat (`verb_unlocks_job` + adjective)
- 4'lü → dar şekil / commit anahtarı (hasar ultisi değil)
- Uzunluk hasar/poise çarpmaz; sıfat işlevi varsa `damage_mult ≤ 0.9`

## 2. Kod

Alfa prototip kodu repoda duruyor ama **bu turda tasarım sıfırdan**. Eski gramer/cümle
davranışı yeni JSON ile uyumlu sayılmaz; görevler JSON'dan doğar, eski envantere
uydurulmaz.

Stack / mimari kabuk: `docs/teknoloji-kararlari.md`  
Unity tuzakları: `docs/unity-notlari.md`

## 3. Bilinen açıklar

- v4.2 henüz koda bağlanmadı
- 36 bileşik için `skill_job` / `verb_unlocks_job` / 4'lü dar şekil satırları taslak; playtest ile budanacak
- Sayılar `[draft]`
- Boss repertuarı / run yapısı tanımsız (JSON dışı, sonra)

## 4. Arşiv

Okunmaz, bağlayıcı değil: `docs/arsiv/` (eski durum, eski dövüş spec, T0–T14).
