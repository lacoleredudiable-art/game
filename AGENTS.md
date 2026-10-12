# Ajanlar için kurallar

Mobil co-op boss baskın oyunu, alfa. Hedef: kural kitabı v4.5'in dilimi (Kılıç + Yay, rünler 1-2-3-4-6-9,
Ağların Kraliçesi) telefonda doğru çalışsın.

## Tasarım kaynağı
- Tek tasarım kaynağı: kural kitabı v4.5 + `kural-motoru-v4.json`. Çelişkide kural kitabı kazanır.
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
- Testler gevşetilmez; bir test kırılırsa nedeni PR'da yazılır.
- Görevin adlandırmadığı dosyalara dokunma; repoyu baştan tarama.

## Okuma
- Önce: kural kitabı v4.5 ilgili bölüm → `kural-motoru-v4.json` → görevin adlandırdığı dosyalar.
- Asla okuma: `unity/Library/`, `unity/Temp/`, `unity/obj/`, `unity/Logs/`, `docs/play-sweep/*.csv`.

## Komutlar (repo kökünden)
- `dotnet test tools/CoreTests` · `dotnet test tools/IntegrationTests`
- `python tools/GameCompile/check.py`
- Dilim APK: AndroidBuilder `-dovusV4Slice`

## CI
CoreTests → IntegrationTests → GameCompile. SweepV2 hash kapısı eski motoru korur; v4 dilim davranışını
değiştiren PR'larda hash farkı beklenir ve kapı bu PR'ları engellemez (taşıma bitince kaldırılacak).
