# game

Mobil, kooperatif, aksiyon tabanlı boss dövüşü oyunu. Referans: Dragon Nest.

20-30 kişilik bir arkadaş grubu için yapılıyor. Ticari hedef yok — amaç, teknik ve görsel
olarak gerçekten etkileyici bir oyun çıkarmak.

**Durum:** Dövüş sistemi tasarlandı, prototip görevlerine bölündü. Henüz kod yok.

## Belgeler

Sırayla okunmalı:

1. [Tasarım Özeti](docs/tasarim-ozeti.md) — projenin tam bağlamı. Yeni bir sohbete
   başlarken önce bu okunur.
2. [Dövüş Sistemi](docs/dovus-sistemi.md) — dövüşün alfa spesifikasyonu ve tek doğruluk
   kaynağı. Bütün sayılar burada.
3. [Teknoloji Kararları](docs/teknoloji-kararlari.md) — stack, mimari, neyin kapsam dışı olduğu.
4. [Ajan Görev Listesi](docs/gorev-listesi.md) — sırayla çalıştırılacak görevler ve her biri
   için hazır prompt.

Çalışma sırasında: [Durum](docs/durum.md) nerede kaldığımızı tutar, [T0 Kurulum](docs/t0-kurulum.md)
ilk kurulumu anlatır, [AGENTS.md](AGENTS.md) ajanların uyduğu değişmez kuralları içerir.

## Dövüş sistemi, tek paragrafta

Sol başparmak karakteri yürütür. Sağ başparmak, beşgen dizilmiş 5 noktanın üzerinde
sürüklenerek **cümle kurar**: ilk nokta fiil, sonrakiler sıfat. Cümle çizilirken sonuç zaten
dünyada olur ve elinin altında şekil değiştirir. Beşgenin ortasına tıklamak dodge'dur ve
cümleyi iptal eder. Tam zamanında sıyırırsan yavaş çekim penceresi açılır ve normalde
sığmayacak uzunlukta bir cümle kurabilirsin.

Ezberlenecek kombo yoktur — öğrenilecek beş kelime ve dört kural vardır, gerisi cümle kurmak.

## Sıradaki adım

[Görev listesindeki](docs/gorev-listesi.md) T0'dan başla: Unity 6 projesini kur, sonra
Faz 1'i (T1-T4) sırayla ajanlara ver. Faz 1 Unity gerektirmez, `dotnet test` ile doğrulanır.

Prototipin cevapladığı sorular ve hangi sırayla sorulacakları:
[Dövüş Sistemi §13](docs/dovus-sistemi.md#13-prototipin-cevapladığı-sorular).
