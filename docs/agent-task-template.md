# Composer görev şablonu

Her ajan görevi bu iskelete göre yazılır. Amaç: ajan repoyu taramasın, yalnız adı verilen dosyaları okusun.
Kopyala, `<...>` alanlarını doldur, gereksiz bölümü sil.

```markdown
# Görev: <tek cümle, ölçülebilir sonuç>   (PLAN maddesi: <ör. 2.3>)

## Bağlam (okunacaklar — başka dosya açma)
- `AGENTS.md` (kök) + `<klasör>/AGENTS.md`
- `docs/MAP.md` → yalnız "<konu>" satırı
- `<yol/Dosya.cs>:<satır–satır>` — <neden>
- (gerekirse) `docs/element-sistemi.json` yalnız `<anahtar>` bölümü

## Yapılacak
1. <somut adım; dosya + metot adı>
2. <...>

## Yapılmayacak (kapsam dışı)
- <dokunulmayacak dosya/sistem>; kapsam dışı hata görürsen düzeltme, PR açıklamasına yaz.

## Kabul kriterleri
- [ ] <davranış / dosya / test>
- [ ] `pwsh tools/verify.ps1` (veya `powershell -File tools/verify.ps1`) → hepsi PASS
- [ ] Davranış-sabit işlerde: CoreTests sayısı = <N>, sweep 1440/1440 ve ozet.md önceki ile aynı

## Teslim
- Dal: `<tür>/<kısa-ad>` (origin/master'dan). Küçük commit'ler.
- `git checkout/restore/reset/stash/clean` YASAK. `.unity`/`.prefab` elle düzenleme yok.
- Push + PR; PR açıklamasına: ne değişti, test sayısı, verify özeti, doğrulanamayanlar.
- Son mesaj: PR URL'si + verify özeti (5–10 satır).
```

## İpuçları
- Satır numarası ver: "ManifestationDirector.cs:1316–1332" ajanın 7.400 satırı okumasını önler.
- Tek görev = tek PR = 1–2 saat. Büyükse alt görevlere böl (2.4a, 2.4b…).
- Unity Editor gerekmeyen işleri headless doğrula (`tools/verify.ps1`). Unity MCP üzerinden build tetikleme.
- Model: Composer (composer-2.5). Daha büyük model yalnız açık kullanıcı onayıyla.
