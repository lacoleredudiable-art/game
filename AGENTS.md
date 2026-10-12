# Ajanlar için kurallar

Mobil co-op boss baskın oyunu, alfa. Hedef: kural kitabı v4.5'in dilimi (Kılıç + Yay, rünler 1-2-3-4-6-9,
Ağların Kraliçesi) telefonda doğru çalışsın.

## Tasarım kaynağı
- Tasarım kaynağı `docs/tasarim/00-OKU-BENI.md`. Bir kombo üzerinde çalışmadan önce o hücreyi oku.
- Hangi işin sırada olduğunu `docs/tasarim/11-onarim-plani.md` söyler. Her PR açıklamasına önce/sonra Gözcü satır
  sayısını yaz.
- Ölçü, kamera, dodge veya arena işinde `10-olculer.md`'deki madde kimliğini (ör. O-7.2) PR'a ve test adına yaz.
- Belgede olmayan davranışı ya da ölçüyü uydurma, sor.
- Sayılar `kural-motoru-v4.json`'dadır. Belgeler çelişirse `00-OKU-BENI.md`'deki öncelik sırası geçerlidir.
- `element-sistemi.json` (v6.1.1) ve eski skill motoru (MechanicGrammar, SkillExecutorRouter, eski executor'lar,
  24 el ayarı) DONDURULDU: düzeltilmez, genişletilmez, yeni kod onlara bağlanmaz. Taşıma bitince silinecek.

## Değişmez kurallar
0. Tek dil: kod tanımlayıcıları İngilizce, yorumlar Türkçe olabilir.
1. `Core` saf C#: `using UnityEngine` yasak; zaman parametre olarak geçer.
2. Sahne koddan kurulur (`GameBootstrapHost`). `.unity` / `.prefab` YAML dosyaları asla elle düzenlenmez.
3. Sayı koda yazılmaz: oynanış sayıları `kural-motoru-v4.json` ya da `*Defaults` const'larından gelir.
   Sayı uydurma; kaynakta yoksa PR'da "kaynakta yok" diye yaz.
4. Sessiz geri dönüş yasak: v4 bayrağı açıkken yeni yol başarısız olursa eski yola düşülmez;
   `Debug.LogError("[V4-FALLBACK] <yer>: <sebep>")` yazılır.
5. Skill sırasında konumu tek bir sahip yazar (PositionOwnership'e kayıtlı). v4 komutları (KendiniTasi, It, dodge)
   bu sahiplik kuralına uyar; ikinci bir gizli konum yazan yol eklenmez.

## Çalışma biçimi
- Bir PR = bir hata ya da bir özellik. Birden çok iş verilirse ilkini yap, diğerlerini PR'da "sonra" diye yaz.
- Önce kanıt: hatanın sebebini log ya da ölçümle göster, sonra yalnız o sebebi düzelt.
- "Bitti" telefonda görülür: oynanışı değiştiren her PR'da dilim APK'sından ekran kaydı ya da logcat çıktısı.
  Editör testi tek başına yeterli değildir.
- Her hata düzeltmesi önce hatayı oyunun GERÇEK yolundan (sahte dokunuş/çizim → görünen sonuç: oynayan klip,
  konum, çarpışma) yeniden üreten bir test yazar. Test düzeltmeden önce kırmızı, sonra yeşil olmalı.
  Hatayı yakalamayan testle "düzelttim" denmez.
- Oynanışı değiştiren her PR, görsel duman testinin kontakt sayfasını PR'a ekler, sayfaya bakıp gözlemini yazar
  ve Gözcü satırlarının sayısını raporlar. Resim PR açıklamasına eklenir, repoya kaydedilmez.
- Fonksiyonu doğrudan çağırıp geçen test, oynanış hatasının kanıtı sayılmaz.
- Testler gevşetilmez; bir test kırılırsa nedeni PR'da yazılır.
- Eski motor testleri (`[Category("EskiMotor")]`) dondurulmuştur: bakımı yapılmaz, gevşetilmez, yenisi eklenmez.
  Eski motor silinince onlarla birlikte silinir.
- Yerelde yalnız değiştirdiğin alana ait testleri çalıştır (`dotnet test --filter`); tam set CI'ın işi.
- Görevin adlandırmadığı dosyalara dokunma; repoyu baştan tarama.

## Testler
- Kırmızı bir test görürsen önce master'da çalıştır; master'da da kırmızıysa düzeltme, PR'a yaz ve geç.
- Yalnız ilgili test sınıflarını `-testFilter` ile çalıştır.
- Başlamadan önce hangi testleri çalıştıracağını ve tahmini süreyi yaz.
- 20 dakikayı geçen testi durdur ve kullanıcıya yaz.
- Test çalıştırmadan önce bu projeyi açık tutan bir Unity editörü olup olmadığına bak; varsa kullanıcıya söyle.

## Okuma
- Önce: kural kitabı v4.5 ilgili bölüm → `kural-motoru-v4.json` → görevin adlandırdığı dosyalar.
- Asla okuma: `unity/Library/`, `unity/Temp/`, `unity/obj/`, `unity/Logs/`, `docs/play-sweep/*.csv`.

## Komutlar (repo kökünden)
- `dotnet test tools/CoreTests --filter "FullyQualifiedName~<Sınıf>"` (alan testi) · `dotnet test tools/IntegrationTests`
- `python tools/GameCompile/check.py`
- Dilim APK: AndroidBuilder `-dovusV4Slice`
- Görsel duman testi (Unity batchmode): `-runTests -testPlatform PlayMode -testFilter GozcuSmoke -gozcuOut <klasör>`
  → `kontakt.png`, `gozcu.txt`, `senaryo.txt` (varsayılan tools/verify-out/gozcu, git dışı)

## CI
Her PR ve master push, hepsi engelleyici: OYUN.md kontrolü → CoreTests (`--filter "TestCategory!=EskiMotor"`)
→ IntegrationTests → GameCompile.
Haftalık (schedule) ve elle (workflow_dispatch), engellemez: EskiMotor testleri → SweepV2 hash kapısı;
sonuç job summary'de. Eski motoru korur; taşıma bitince kaldırılacak.
