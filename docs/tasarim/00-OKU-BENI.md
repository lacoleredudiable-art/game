# Tasarım belgeleri — tek kaynak

Bu klasör oyunun tasarım kaynağıdır. Kod bu belgelerin uygulamasıdır; kodla belge çelişirse **belge kazanır**,
kod düzeltilir. Belgede olmayan bir davranışı kod uydurmaz: kaynakta yoksa PR'da "kaynakta yok" diye yazılır
ve kullanıcıya sorulur.

## Oyun hikâyesi, hedef ve bugünkü durum

| Katman | Belge(ler) | Ne anlatır |
|--------|------------|------------|
| **Kimlik ve vizyon** | `13-vizyon-ve-cerceve.md` | 5 arkadaş / kahvehane / 3 saat; LoL teamfight + Kalahan; rün grameri; güç ilerlemesi yok; wipe okunurluğu; swap cancel tavanı |
| **Kural kitabı v4.5** | `01`–`07`, `09`, `10`, `16` | 12×12 hücre, 10 silah, dünya fiziği, ölçüler, görünüm |
| **Kodlama (şimdi)** | `14-kodlama-plani.md`, `12-kusur-envanteri.md` | Aşama 1–3 tamam; sıra Aşama 4–8 |
| **Tasarım hattı (sonra)** | `17`, `18`, `19`, `20` | Kül Ejderi + Krater Arenası; Core'da `KulEjderiBrain` iskeleti hazır |
| **Dilim boss (şimdi)** | `docs/design/aglarin-kralicesi.md` | Ağların Kraliçesi — Aşama 4–8 test arena boss'u |

**İş sırası:** `14-kodlama-plani.md` (aşamalar; telefon testi aşama sonunda). Kusur listesi: `12-kusur-envanteri.md`.
Tasarım hattı sırası: `17-tasarim-hatti.md`. Boss hedef: `18-kul-ejderi.md` (KE-x.y). Arena: `19-krater-arenasi.md` (KA-x.y). Asset: `20-asset-listesi.md` (AL-x.y).

## Öncelik sırası (çelişkide üstteki kazanır)

1. `09-sonraki-kararlar.md` — v4.5'ten sonra kullanıcının verdiği kararlar (en yeni).
2. `01-kural-kitabi-v4.5.md` — 12 fiil, 12 sıfat, **144 hücre** (Bölüm 5), 10 silah × fiil görünümü (Bölüm 6).
3. `02-dunya-fizigi.md` — ağırlık, menzil, mermi/alan/yakın vuruş, itme, tasma, işaret, yapı, zaman kaydı, boss.
4. `03-fizik-komutlari.md` — kural motoru → komut listesi → fizik eşlemesi.
5. `04-silahlar.md`, `05-silah-fiil-120.md` — silah menzili, ritim, yolculuk hızı, 120 görünüm hücresi.
6. `10-olculer.md` — kural dışı ölçüler (`O-x.y`).
7. `16-sanat-incili.md` — görsel dil (`SI-x.y`).
8. `06-animasyon-kurallari.md`, `07-efekt-motoru.md` — görünüm (sonuç değiştirmez).
9. `08-acik-sorular.md` — karar verilmemiş maddeler (koda girmez).

Sıralamanın dışında, **üstünde**: `13-vizyon-ve-cerceve.md` — [K]/[Ö]/[AÇIK]/[RED]. Yalnız **[K]** karardır.
Hiçbir madde dilime kendiliğinden girmez.

Sayılar `unity/Assets/Resources/RuleEngineV4/kural-motoru-v4.json` içindedir; belgedeki sayılar başlangıç
değeridir, JSON günceldir.

## Ajanlar için okuma kuralı

- Kombo işinde **önce hücre:** `01` Bölüm 5 `| fiil-sıfat`, Bölüm 6 silah satırı, `09` ilgili karar.
- PR'da hücre numarası ve ilgili cümle.
- Ölçü işinde `10` madde kimliği PR ve test adında (ör. `O_7_2_ArenaSiniri_18m`).
