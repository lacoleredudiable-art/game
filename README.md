# game

Mobil, kooperatif, aksiyon tabanlı boss dövüşü oyunu. Referans: Dragon Nest.

20-30 kişilik bir arkadaş grubu için yapılıyor. Ticari hedef yok — amaç, teknik ve görsel
olarak gerçekten etkileyici bir oyun çıkarmak.

**Durum:** Alfa prototip çalışıyor ve telefonda 60 fps'te ölçüldü (tek sahne, tek boss,
primitive görsel, sanat varlığı yok). Dövüş tasarımı — rünler ve mekanikler — 29 Ağustos
2026'da yeniden açıldı; prototip kodu duruyor, değişiklikler onun üstüne gelecek.
    10|
## Belgeler

| Dosya | Ne için |
|---|---|
| [Durum](docs/durum.md) | Nerede kaldık, elimizde ne var, ne açık. **İlk buraya bak.** |
| [Dövüş Sistemi](docs/dovus-sistemi.md) | Pazarlıksız kısıtlar, kodda çalışan mekanikler, bütün sayılar, yeni tasarımın yazılacağı yer |
| [Teknoloji Kararları](docs/teknoloji-kararlari.md) | Stack, mimari, neyin kapsam dışı olduğu |
| [Unity Notları](docs/unity-notlari.md) | Editör/build/telefon tuzakları |
| [Görev Listesi](docs/gorev-listesi.md) | Ajanlarla çalışma düzeni; görev tablosu şu an boş |
    20|| [Tasarım Özeti](docs/tasarim-ozeti.md) | Projenin tam bağlamı: sütunlar, referans analizi, zorluk felsefesi |
| [AGENTS.md](AGENTS.md) | Ajanların uyduğu değişmez kurallar |

Kapanmış turların kaydı [`docs/arsiv/`](docs/arsiv/README.md) altında — bağlayıcı değil,
rutin olarak okunmaz.

## Dövüş sistemi, tek paragrafta

Sol başparmak karakteri yürütür. Sağ başparmak, beşgen dizilmiş 5 noktanın üzerinde
sürüklenerek **cümle kurar**: ilk nokta fiil, sonrakiler sıfat. Cümle çizilirken sonuç zaten
    30|dünyada olur ve elinin altında şekil değiştirir. Beşgenin ortasına tıklamak düz vuruştur —
cümle kuruluyken aynı tıklama cümleyi erkenden kapatır ve ödemesini alır. Dodge beşgenin
dışındaki ayrı düğmedir ve cümle sürerken basılırsa yatırımı iptal eder. Bossu doğru okuyup
tam zamanında sıyırırsan yavaş çekim penceresi açılır ve normalde sığmayacak uzunlukta bir
cümle kurabilirsin.

Ezberlenecek kombo yoktur — öğrenilecek beş kelime ve üç kural vardır, gerisi cümle kurmak.

## Nasıl çalıştırılır

    40|Dövüş mantığı Unity'den ayrı, saf C#:

```bash
cd tools/CoreTests && dotnet test      # 92 test
```

Unity tarafı: `unity/` projesini Unity 6 ile aç, **Dovus → Create Prototype Scene**, sonra
play. Telefona build: **Dovus → Build Android APK**.

Rün seti denemek için Unity gerekmiyor:

    50|```bash
cd tools/skill-preview && python3 preview.py 5-1-2-4
```
