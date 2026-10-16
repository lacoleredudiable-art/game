# Ajanlar için kurallar

Mobil co-op boss baskın oyunu, alfa. Hedef: kural kitabı v4.5'in dilimi (Kılıç + Yay, rünler 1-2-3-4-6-9,
Ağların Kraliçesi) telefonda doğru çalışsın.

## Tasarım kaynağı
- Tasarım kaynağı `docs/tasarim/00-OKU-BENI.md`. Bir kombo üzerinde çalışmadan önce o hücreyi oku.
- Hangi işin sırada olduğunu `docs/tasarim/14-kodlama-plani.md` söyler (aşamalar). Her aşama PR'ının açıklamasına
  önce/sonra Gözcü satır sayısını yaz.
- Ölçü, kamera, dodge veya arena işinde `10-olculer.md`'deki madde kimliğini (ör. O-7.2) PR'a ve test adına yaz.
- Belgede olmayan davranışı ya da ölçüyü uydurma, sor.
- Sayılar `kural-motoru-v4.json`'dadır. Belgeler çelişirse `00-OKU-BENI.md`'deki öncelik sırası geçerlidir.
- Dilim cümle grameri: `unity/Assets/Resources/RuleEngineV4/slice-sentence-grammar.json`; kural sayıları:
  `unity/Assets/Resources/RuleEngineV4/kural-motoru-v4.json`.

## Tasarım konuşması kuralları
Kullanıcı tasarım konuşuyorsa (fikir, kıyas, "hangisi daha iyi") bu bölüm geçerlidir; kod konuşuyorsa aşağısı.
- Çıktı yalnız değerlendirmedir. Koda, belgeye dokunma; dosya, görev ya da işlem **teklif etme**. Cevap
  değerlendirmeyle biter, "istersen şunu yazayım" ile değil.
- Her fikre önce karşı sav: hangi oyunda denendi, ne tuttu ne tutmadı, bizde neden farklı olabilir.
- Gerekçesiz katılma yasak. Katıldığın yerde gerekçe, katılmadığın yerde en güçlü itiraz yazılır.
- Öneri projeye girmez. Karar kullanıcınındır; ajan **[Ö]** diye yazar, kullanıcı onaylayınca **[K]** olur.
- Her sayı için kaynak ve sınıf: **(kâğıt)** kuralla belirlenir, **(ölçüm)** oynanarak bulunur, yazılan değer
  başlangıç tahminidir. Kaynak yoksa "tahmin" denir, sayı uydurulmaz.
- Belgelerdeki eski fikir (ör. README'deki prototip kalıntısı) kullanıcının sözüne karşı kanıt değildir;
  kullanıcı "o eskiydi" derse belge güncellenir, fikir geri getirilmez.
- Son üç mesaj oyunu değil yöntemi/aracı konuşuyorsa konu kaymıştır; ajan bunu söyler ve açık kalan oyun
  sorusuna döner. Kullanıcının "konuya dön" demesi yeterlidir, açıklama istenmez.
- Konuşma sonunda, kullanıcı isterse, hasat: `docs/tasarim/13-vizyon-ve-cerceve.md` biçiminde [K]/[Ö]/[AÇIK]/
  [RED] satırları; reddedilenler ve nedenleri de yazılır.

Kodda takılınca tasarım değişikliği önermek yasaktır. Önce üçünden hangisi olduğu log ya da ölçümle gösterilir:
(a) kod hatası, (b) sayı yanlış (`kural-motoru-v4.json`), (c) kural eksik ya da çelişik. Yalnız (c) kullanıcıya
tasarım sorusu olarak döner.

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
- İş aşamalarla yürür (`14-kodlama-plani.md`). Her aşamanın dalı `asama/N-<ad>`; aşama içindeki PR'lar bu dala girer,
  master'a aşama başına bir kez, telefon testi geçince girilir.
- Aşama içinde bir PR = bir hata ya da bir özellik. Birden çok iş verilirse ilkini yap, diğerlerini PR'da "sonra" diye yaz.
- Önce kanıt: hatanın sebebini log ya da ölçümle göster, sonra yalnız o sebebi düzelt.
- "Bitti" telefonda görülür: her aşamanın master PR'ında dilim APK'sından ekran kaydı ya da logcat çıktısı.
  Editör testi tek başına yeterli değildir.
- Her hata düzeltmesi önce hatayı oyunun GERÇEK yolundan (sahte dokunuş/çizim → görünen sonuç: oynayan klip,
  konum, çarpışma) yeniden üreten bir test yazar. Test düzeltmeden önce kırmızı, sonra yeşil olmalı.
  Hatayı yakalamayan testle "düzelttim" denmez.
- Her aşamanın master PR'ı, görsel duman testinin kontakt sayfasını PR'a ekler, sayfaya bakıp gözlemini yazar
  ve Gözcü satırlarının sayısını raporlar. Resim PR açıklamasına eklenir, repoya kaydedilmez.
- Fonksiyonu doğrudan çağırıp geçen test, oynanış hatasının kanıtı sayılmaz.
- Testler gevşetilmez; bir test kırılırsa nedeni PR'da yazılır.
- Yerelde yalnız değiştirdiğin alana ait testleri çalıştır (`dotnet test --filter`); tam set CI'ın işi.
- Görevin adlandırmadığı dosyalara dokunma; repoyu baştan tarama.

## Testler
- Aşama içindeki PR: CI (OYUN.md, CoreTests, IntegrationTests, GameCompile) + değişen alanın `dotnet test --filter`
  testi. Telefon ve Unity testi istemez.
- Aşama sonunda, master PR'ından önce bir kez: yerelde O_ PlayMode testleri + Gözcü duman testi + dilim APK + telefon
  testi; PR açıklamasına sonuç (geçen/kalan/atlanan sayısı ve Gözcü tür tablosu) yapıştırılır. Yapıştırılmamış PR
  master'a merge edilmez.
- Kırmızı bir test görürsen önce master'da çalıştır; master'da da kırmızıysa düzeltme, PR'a yaz ve geç.
- Yalnız ilgili test sınıflarını `-testFilter` ile çalıştır.
- Başlamadan önce hangi testleri çalıştıracağını ve tahmini süreyi yaz.
- 20 dakikayı geçen testi durdur ve kullanıcıya yaz.
- Test çalıştırmadan önce bu projeyi açık tutan bir Unity editörü olup olmadığına bak; varsa kullanıcıya söyle.

## Okuma
- Önce: kural kitabı v4.5 ilgili bölüm → `kural-motoru-v4.json` → görevin adlandırdığı dosyalar.
- Asla okuma: `unity/Library/`, `unity/Temp/`, `unity/obj/`, `unity/Logs/`.

## Komutlar (repo kökünden)
- `dotnet test tools/CoreTests --filter "FullyQualifiedName~<Sınıf>"` (alan testi) · `dotnet test tools/IntegrationTests`
- `python tools/GameCompile/check.py`
- Dilim APK: AndroidBuilder `-dovusV4Slice`
- Görsel duman testi (Unity batchmode): `-runTests -testPlatform PlayMode -testFilter GozcuSmoke -gozcuOut <klasör>`
  → `kontakt.png`, `gozcu.txt`, `senaryo.txt`, `cirlik.txt` (Gözcü tür tablosu; eşikler `tools/gozcu-esik.txt`)
  (varsayılan tools/verify-out/gozcu, git dışı)
- PR öncesi zorunlu set: `-runTests -testPlatform PlayMode -testFilter "\.O_;GozcuSmoke" -testResults <xml>
  -gozcuOut <klasör>`

## CI
Her PR ve master push, hepsi engelleyici: OYUN.md kontrolü → CoreTests
→ IntegrationTests → GameCompile.
