# Ajanlar için kurallar

Mobil kooperatif boss dövüşü, alfa prototip. Bu dosya her görevde bağlama girer: yalnız sert kurallar.

## Değişmez kurallar
1. `unity/Assets/Scripts/Core` saf C#: `using UnityEngine` yasak, zaman parametre olarak geçer.
2. Sahne koddan kurulur (`PrototypeBootstrap`). `.unity` / `.prefab` YAML dosyaları **asla** elle düzenlenmez.
3. **Tek hareket sistemi:** skill sırasında oyuncuyu/boss'u yalnız hareket kalıbı taşır
   (`Core/Motion/MotionTemplateRunner` + `Game/MotionTemplateBody`). İkinci bir hareket yolu ekleme;
   `SkillMotionDriver` / `SkillMotionMotor` / executor hareketi ölü, canlandırma. Kalıp dışı konum
   yazan her şey `Core/Motion/PositionOwnership`'e kayıtlı olmalı.
4. Ayarlanabilir her şey veri. Skill sayısı `docs/element-sistemi.json` `engine` / `adjective_mods`'tan,
   his sayısı ilgili `Core/Tuning/*.cs` varsayılanından gelir. Sayı uydurma; yoksa varsayılan koy,
   yoruma "spec'te yok" yaz, PR açıklamasına geç.
5. Hiçbir fiil anlık vurmaz. Kombo tablosu / skill kimliğiyle beyaz liste yazılmaz; davranış gramerden doğar.
6. `docs/element-sistemi.json` bağlayıcıdır (v6.1.1) ve
   `unity/Assets/Resources/ElementSystem/element-sistemi.json` ile **bayt bayt aynı** kalır (ikisini birlikte değiştir).
   `motion-templates.json` elle düzenlenmez: `python3 tools/build-motion-templates.py` iki kopyayı birden yazar.

## Okuma
- Repoyu tarama. Yalnız görev prompt'unun adını verdiği dosya ve satırları oku.
- **`docs/` altını görev bir dosyayı açıkça adlandırmadıkça okuma** (`durum.md` dahil).
  `element-sistemi.json`'dan yalnız istenen bölümü oku.
- Asla okuma: `unity/Library/`, `unity/Temp/`, `unity/obj/`, `unity/Logs/`, `docs/archive/`, `docs/play-sweep/*.csv`.
- Başka görevin dosyasına dokunma. Kapsam dışı bir hata görürsen düzeltme, PR açıklamasına yaz.

## Komutlar (hepsi repo kökünden)
- Test: `dotnet test tools/CoreTests` (yeni Core dosyası otomatik link'lenir; test = `tools/CoreTests/<Sistem>Tests.cs`, `namespace CoreTests;`, `[TestFixture]`).
- Gramer kontrolü: `dotnet run --project tools/AtomSim` → "0 hata" olmalı; gramer/`mechanic_grammar` değiştiyse `tools/AtomSim/out/` commit edilir.
- Başsız tarama: `dotnet run --project tools/SweepV2 -c Release -- --all --gate` (~15 sn).
  Alt küme: `--weapon kilic --case 1-11`. Play ile kıyas: `--compare docs/play-sweep/pr35-final-4x.csv`.
- Game katmanı derleme: `python3 tools/GameCompile/check.py` (CoreTests içinden de koşar).

## CI kapısı
`.github/workflows/sweep-v2.yml` her PR'da: CoreTests → AtomSim derlemesi →
`SweepV2 --all --gate --compare docs/play-sweep/pr35-final-4x.csv`.
Kapı: her silah ≥142/144, oyuncu boss gövdesinde 0, `yerde` hatası silah başına ≤3 (2-9 beyaz listede).
Kırmızı CI ile merge yok. Kapı eşiğini veya beyaz listeyi görev açıkça istemeden değiştirme.

## Git ve teslim
- Başlarken `git fetch && git log --oneline origin/master -5`. Yerel `master` geride/ayrışıksa önce onu çöz.
- Her görev kendi dalında, küçük commit'lerle.
- **Sonuçları PR açıklamasına yaz:** ne değişti, test sayısı, başsız tarama skor tablosu
  (`<label>-ozet.md`), doğrulayamadığın kabul kriterleri, spec'te olmayan varsayılanlar.
- **`docs/durum.md`'ye ekleme yapma.** Yalnız görevin o dosyadaki bir açığı kapattıysa o satırı aynı PR'da sil.
- `dotnet test` + CI yeşilse `master`'a merge et. PR'ı yalnız karar gerektiren bir soru ya da
  doğrulanamayan kabul kriteri varsa açık bırak.
