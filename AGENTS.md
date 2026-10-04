# Ajanlar için kurallar

Mobil kooperatif boss dövüşü, alfa prototip. Yalnız sert kurallar.

## Değişmez kurallar
0. **Tek dil:** kod tanımlayıcıları İngilizce; yorumlar Türkçe olabilir; JSON/fiil kimlikleri gibi veri sözcükleri sabit/sözlükten (`docs/ARCHITECTURE.md` isimlendirme + veri sözlüğü).
1. `unity/Assets/Scripts/Core` saf C#: `using UnityEngine` yasak, zaman parametre olarak geçer.
2. Sahne koddan kurulur (`GameBootstrapHost`). `.unity` / `.prefab` YAML dosyaları **asla** elle düzenlenmez.
3. **Tek hareket sistemi:** skill sırasında oyuncuyu/boss'u yalnız hareket kalıbı taşır
   (`Core/Motion/MotionTemplateRunner` + `Game/Actors/MotionTemplateBodyHost`). İkinci hareket yolu ekleme;
   `SkillMotionDriver` / executor hareketi ölü; `SkillMotionMotor` yalnız saf plan — konumu yalnız `MotionTemplateRunner` yazar.
   Kalıp dışı konum `Core/Motion/PositionOwnership`'e kayıtlı olmalı.
4. **Veri ve const (kural 5):** Skill sayısı `element-sistemi.json` `engine` / `adjective_mods`'tan; his sayıları `Core/Tuning/*.cs` varsayılanından.
   Oynanış sayıları `*Defaults` const'larına taşınır (`MagicNumberRatchetTests`); const geçici taşımadır, nihai ayar JSON/tuning.
   Sayı uydurma; yoksa varsayılan + yorum "spec'te yok" + PR notu.
5. Hiçbir fiil anlık vurmaz. Kombo tablosu / skill kimliğiyle beyaz liste yazılmaz; davranış gramerden doğar
   (bilinen istisnalar kodda; hedef: JSON `engine` etiketleri).
6. `docs/element-sistemi.json` bağlayıcıdır (v6.1.1) ve
   `unity/Assets/Resources/ElementSystem/element-sistemi.json` ile **bayt bayt aynı** kalır.
   Metin engine'den türetilir; `SkillTextNumberTests` korur.
   `motion-templates.json` elle düzenlenmez: `python3 tools/build-motion-templates.py`.

## Okuma
- `docs/MAP.md` → `docs/PLAN.md` → `docs/ARCHITECTURE.md` → `docs/OYUN.md` (oyun); görev şablonu `docs/agent-task-template.md`.
- Repoyu tarama; yalnız görevin adlandırdığı dosya/satırlar.
- **`docs/` taraması** yalnız görev açıkça isterse.
- Asla okuma: `unity/Library/`, `unity/Temp/`, `unity/obj/`, `unity/Logs/`, `docs/play-sweep/*.csv`.

## Komutlar (repo kökünden)
- `dotnet test tools/CoreTests` · `dotnet test tools/IntegrationTests`
- `dotnet run --project tools/AtomSim` → "0 hata"
- `dotnet run --project tools/SweepV2 -c Release -- --all --gate`
- `python tools/GameCompile/check.py`
- `python tools/gen-game-overview.py --check` ( `docs/OYUN.md` üretilen bölüm)
- `powershell -NoProfile -ExecutionPolicy Bypass -File tools/verify.ps1`

## CI kapısı
`.github/workflows/sweep-v2.yml`: CoreTests → IntegrationTests → AtomSim build → SweepV2.
Kapı: **144/144** silah başına; oyuncu boss gövdesinde 0; `yerde` hatası silah başına ≤3;
normalize sweep hash `docs/play-sweep/headless-baseline.sha256` (`notlar` sütunu çıkarılmış);
Play CSV farkları yalnız `docs/play-sweep/known-play-diffs.txt`.
Yerel verify aynı kapıyı koşar; ham `tools/verify-out/sweep/verify.csv` SHA256 =
`726197242A3896C178A3E27650AB9B6F1A089E193C3FE6B90E53BD7477182BB4` (CRLF satır sonu).

## Sweep hash doğrulama
1. `tools/verify.ps1` (SweepV2 `--expect-sha256 @docs/play-sweep/headless-baseline.sha256`).
2. Windows ham CSV: `(Get-FileHash tools/verify-out/sweep/verify.csv).Hash` yukarıdaki değerle eşleşmeli.
3. Linux CI normalize hash dosyasını kullanır; satır sonu farkı bilinçli — iki kontrol birlikte.

## Reflection
Play Sweep / SweepV2 oyun içine **string alan adıyla** erişim kullanmaz (`SweepReflectionTests`); erişim `*.PlaySweepAccess.cs` partial'larından.

## Git ve teslim
- Dal başına küçük commit'ler; PR'da test sayıları, sweep hash, doğrulanamayanlar.
- `docs/PLAN.md`'de maddeyi `[x]` işaretle; `dotnet test` + CI yeşilse merge (karar sorusu yoksa).
